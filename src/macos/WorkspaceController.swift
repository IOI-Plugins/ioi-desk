import AppKit
import ApplicationServices

final class Header: NSButton {
    var zone = ""; weak var controller: WorkspaceController?
    override func acceptsFirstMouse(for event: NSEvent?) -> Bool { true }
    override var needsPanelToBecomeKey: Bool { false }
    override func accessibilityPerformPress() -> Bool {
        guard let controller else { return false }
        controller.menuFor(zone).popUp(positioning: nil, at: NSPoint(x: 0, y: bounds.height), in: self)
        return true
    }
    func updateTitle(_ text: String) {
        title = text
        attributedTitle = NSAttributedString(string: text, attributes: [.foregroundColor: NSColor(calibratedRed: 0.82, green: 0.90, blue: 1, alpha: 1), .font: NSFont.systemFont(ofSize: 11)])
        toolTip = text
        setAccessibilityLabel(text)
    }
    override func mouseDown(with event: NSEvent) {
        guard let controller else { return }
        let menu = controller.menuFor(zone)
        NSMenu.popUpContextMenu(menu, with: event, for: self)
    }
}
final class BoardPanel: NSPanel {
    override var canBecomeKey: Bool { false }
    override var canBecomeMain: Bool { false }
    override init(contentRect: NSRect, styleMask style: NSWindow.StyleMask, backing backingStoreType: NSWindow.BackingStoreType, defer flag: Bool) {
        super.init(contentRect: contentRect, styleMask: style, backing: backingStoreType, defer: flag)
        // NSPanel otherwise hides every zone when another application activates.
        hidesOnDeactivate = false
        becomesKeyOnlyIfNeeded = true
    }
}
final class WorkspaceController: NSObject, NSMenuDelegate {
    let folder: URL
    lazy var trace = TraceLog(folder:folder.appendingPathComponent("Logs"))
    init(folder: URL? = nil) {
        self.folder = folder ?? AppPaths.workspace
        super.init()
    }
    var profile = Profile(cells: [])
    var windows: [String: WindowRef] = [:]
    var originals: [(AXUIElement, CGRect)] = []
    var boards: [NSPanel] = []; var headers: [String: Header] = [:]
    var status: NSStatusItem!; var monitor: Any?; var timer: Timer?
    var hint: NSPanel?; var dragWindow: WindowRef?; var dragOrigin: CGPoint?
    var editor: NSWindow?
    var openSettings: (() -> Void)?
    var requestQuit: (() -> Void)?
    var started = false
    var restoreAttempted = false
    var cachedCatalog: [WindowRef] = []
    var catalogLoading = false
    var trusted: Bool { AXIsProcessTrusted() }
    var profileURL: URL { folder.appendingPathComponent("workspace.json") }
    func start() {
        guard !started else { return }; started = true
        trace.write("session.start",["version":Product.version,"accessibility":trusted,"displays":NSScreen.screens.map { "\(screenID($0)) \(rectText($0.frame))" }])
        try? FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
        if let data = try? Data(contentsOf: profileURL), let saved = try? JSONDecoder().decode(Profile.self, from: data) { profile = saved }
        if profile.cells.isEmpty { profile.cells = defaultCells(NSScreen.screens.map(screenID)) }
        status = NSStatusBar.system.statusItem(withLength: NSStatusItem.squareLength)
        status.button?.image = NSImage(systemSymbolName: "rectangle.split.2x2", accessibilityDescription: Product.name)
        let menu = NSMenu()
        for (label, selector) in [("Configurações…", #selector(settings)), ("Reencaixar janelas", #selector(arrange)), ("Editar zonas…", #selector(edit)), ("Mouse e teclado…", #selector(sharing)), ("Diagnóstico · abrir registros", #selector(logs)), ("Verificar acesso às janelas", #selector(access)), ("Restaurar posições e sair", #selector(quit))] {
            let item = NSMenuItem(title: label, action: selector, keyEquivalent: ""); item.target = self; menu.addItem(item)
        }
        status.menu = menu
        buildBoards()
        requestCatalog()
        monitor = NSEvent.addGlobalMonitorForEvents(matching: [.leftMouseDown, .leftMouseDragged, .leftMouseUp, .flagsChanged]) { [weak self] event in self?.drag(event) }
        timer = Timer.scheduledTimer(withTimeInterval: 2, repeats: true) { [weak self] _ in self?.refresh() }
        NotificationCenter.default.addObserver(self, selector: #selector(screensChanged), name: NSApplication.didChangeScreenParametersNotification, object: nil)
        save()
    }
    func save() { do { try JSONEncoder().encode(profile).write(to: profileURL, options: .atomic) } catch { trace.write("profile.save.failed",["error":error.localizedDescription]) } }
    @objc func logs() { try? FileManager.default.createDirectory(at:trace.folder,withIntermediateDirectories:true); NSWorkspace.shared.open(trace.folder) }
    func catalog(apps: [NSRunningApplication]? = nil) -> [WindowRef] {
        guard trusted else { return [] }
        var result: [WindowRef] = []
        for app in apps ?? NSWorkspace.shared.runningApplications where app.activationPolicy == .regular && app.processIdentifier != ProcessInfo.processInfo.processIdentifier {
            let element = AXUIElementCreateApplication(app.processIdentifier)
            AXUIElementSetMessagingTimeout(element, 0.12)
            guard let list = attribute(element, kAXWindowsAttribute) as? [AXUIElement] else { continue }
            for window in list {
                AXUIElementSetMessagingTimeout(window, 0.12)
                guard (attribute(window, kAXSubroleAttribute) as? String) == kAXStandardWindowSubrole,
                      let rect = windowRect(window), rect.width > 20, rect.height > 20 else { continue }
                let title = attribute(window, kAXTitleAttribute) as? String ?? app.localizedName ?? "Janela"
                result.append(WindowRef(element: window, app: app, title: title))
            }
        }
        return result.sorted { ($0.app.localizedName ?? "") + $0.title < ($1.app.localizedName ?? "") + $1.title }
    }
    @objc func requestCatalog() {
        guard trusted, !catalogLoading else { return }
        catalogLoading = true
        let apps = NSWorkspace.shared.runningApplications
        DispatchQueue.global(qos: .userInitiated).async { [weak self] in
            guard let self else { return }
            let result = self.catalog(apps: apps)
            DispatchQueue.main.async {
                self.cachedCatalog = result; self.catalogLoading = false
            }
        }
    }
    func content(_ cell: Cell) -> CGRect? {
        guard let screen = NSScreen.screens.first(where: { screenID($0) == cell.display }) else { return nil }
        var rect = cell.rect(screen.visibleFrame); rect.size.height = max(1, rect.height - 24)
        return cocoaToAX(rect)
    }
    func buildBoards() {
        boards.forEach { $0.close() }; boards.removeAll(); headers.removeAll()
        for cell in profile.cells {
            guard let screen = NSScreen.screens.first(where: { screenID($0) == cell.display }) else { continue }
            let rect = cell.rect(screen.visibleFrame)
            let panel = BoardPanel(contentRect: rect, styleMask: [.borderless, .nonactivatingPanel], backing: .buffered, defer: false)
            panel.isReleasedWhenClosed = false
            panel.level = NSWindow.Level(rawValue: Int(CGWindowLevelForKey(.desktopWindow)) + 1)
            panel.collectionBehavior = [.canJoinAllSpaces, .stationary]
            panel.backgroundColor = NSColor(calibratedRed: 0.055, green: 0.075, blue: 0.10, alpha: 1)
            let header = Header(frame: NSRect(x: 0, y: rect.height - 24, width: rect.width, height: 24))
            header.zone = cell.id; header.controller = self; header.isBordered = false
            header.alignment = .left; header.font = .systemFont(ofSize: 11)
            header.contentTintColor = NSColor(calibratedRed: 0.82, green: 0.90, blue: 1, alpha: 1)
            header.cell?.lineBreakMode = .byTruncatingMiddle
            header.autoresizingMask = [.width, .minYMargin]
            panel.contentView?.addSubview(header); headers[cell.id] = header
            let label = NSTextField(labelWithString: "Escolher janela ↑")
            label.textColor = NSColor(calibratedRed: 0.60, green: 0.70, blue: 0.82, alpha: 1); label.font = .systemFont(ofSize: 11)
            label.alignment = .center; label.frame = NSRect(x: 4, y: rect.height / 2, width: rect.width - 8, height: 20)
            panel.contentView?.addSubview(label)
            panel.orderBack(nil); boards.append(panel)
        }
        updateHeaders()
    }
    func updateHeaders() {
        for cell in profile.cells {
            let ref = windows[cell.id]
            let title = ref.flatMap { attribute($0.element, kAXTitleAttribute) as? String } ?? ref?.title
            let selected = ref.map { ($0.app.localizedName ?? "App") + " · " + (title ?? "Janela") } ?? "Escolher janela"
            headers[cell.id]?.updateTitle(cell.name + " · " + selected + "  ▾")
        }
    }
    func menuFor(_ id: String) -> NSMenu {
        trace.write("picker.opened",["zone":id])
        let menu = NSMenu()
        menu.delegate = self
        if windows[id] != nil {
            let item = NSMenuItem(title: "Trazer janela para cá", action: #selector(recover(_:)), keyEquivalent: ""); item.target = self; item.representedObject = id; menu.addItem(item)
            let clear = NSMenuItem(title: "Liberar zona", action: #selector(clear(_:)), keyEquivalent: ""); clear.target = self; clear.representedObject = id; menu.addItem(clear)
            menu.addItem(.separator())
        }
        if !trusted { let item = NSMenuItem(title: "É necessário permitir Acessibilidade", action: #selector(access), keyEquivalent: ""); item.target = self; menu.addItem(item); return menu }
        let available = cachedCatalog.filter { !$0.app.isTerminated }
        requestCatalog()
        trace.write("picker.catalog",["zone":id,"count":available.count,"visibleBoards":boards.filter { $0.isVisible }.count])
        if available.isEmpty {
            let empty = NSMenuItem(title: catalogLoading ? "Atualizando janelas · reabra o seletor em instantes" : "Nenhuma janela disponível · abra um aplicativo", action: nil, keyEquivalent: "")
            empty.isEnabled = false; menu.addItem(empty)
        }
        for ref in available {
            let used = windows.first(where: { CFEqual($0.value.element, ref.element) })?.key
            let suffix = used == nil ? "" : used == id ? " ✓" : " · mover desta outra zona"
            let item = NSMenuItem(title: "\(ref.app.localizedName ?? "App") · \(ref.title)\(suffix)", action: #selector(selectWindow(_:)), keyEquivalent: "")
            item.state = used == id ? .on : .off
            item.target = self; item.representedObject = Selection(zone: id, ref: ref); menu.addItem(item)
        }
        menu.addItem(.separator())
        let reload = NSMenuItem(title: "Atualizar janelas abertas", action: #selector(requestCatalog), keyEquivalent: "")
        reload.target = self; menu.addItem(reload)
        return menu
    }
    func menuDidClose(_ menu: NSMenu) {
        for board in boards where !board.isVisible { board.orderBack(nil) }
        trace.write("picker.closed",["visibleBoards":boards.filter { $0.isVisible }.count,"assigned":windows.count])
    }
    final class Selection: NSObject { let zone: String; let ref: WindowRef; init(zone: String, ref: WindowRef) { self.zone = zone; self.ref = ref } }
    @objc func selectWindow(_ sender: NSMenuItem) { if let selection = sender.representedObject as? Selection { assign(selection.ref, to: selection.zone) } }
    @discardableResult func assign(_ ref: WindowRef, to id: String, notify: Bool = true, source: String = "menu") -> Bool {
        let operation=String(UUID().uuidString.prefix(12))
        let context:[String:Any] = ["zone":id,"source":source,"app":ref.app.bundleIdentifier ?? "","title":String(ref.title.prefix(240)),"pid":ref.app.processIdentifier]
        trace.write("window.assign.request",context,operation:operation)
        guard let cell = profile.cells.first(where: { $0.id == id }), let target = content(cell), let old = windowRect(ref.element) else { trace.write("window.assign.rejected",context.merging(["reason":"window-or-display-unavailable"]){ _,new in new },operation:operation); return false }
        if (attribute(ref.element, "AXFullScreen") as? Bool) == true { trace.write("window.assign.rejected",context.merging(["reason":"native-fullscreen"]){ _,new in new },operation:operation); if notify { message("Saia da tela cheia do aplicativo antes de encaixá-lo.\nRegistro: \(operation)") }; return false }
        AXUIElementSetAttributeValue(ref.element, kAXMinimizedAttribute as CFString, kCFBooleanFalse)
        var errors:[String:Int32]=[:]
        let moved=move(ref.element,target,errors:&errors)
        let received=windowRect(ref.element)
        guard moved, let actual = received, fitsZone(actual,target) else {
            let rollbackAccepted=move(ref.element, old)
            let rollback=windowRect(ref.element)
            let restored=rollbackAccepted && rollback.map{abs($0.minX-old.minX)<6 && abs($0.minY-old.minY)<6 && abs($0.width-old.width)<6 && abs($0.height-old.height)<6} == true
            let sizeRefused=received.map{$0.width>target.width+6 || $0.height>target.height+6} ?? false
            let automationDenied=errors["browserAutomation"] == -1743
            trace.write("window.assign.rejected",context.merging(["reason":automationDenied ? "browser-automation-permission":sizeRefused ? "size-not-accepted":moved ? "position-not-settled":"ax-operation-failed","display":cell.display,"requested":rectText(target),"received":rectText(received),"original":rectText(old),"axErrors":errors,"rollbackSucceeded":restored,"rollbackActual":rectText(rollback)]){ _,new in new },operation:operation)
            if notify { let detail=automationDenied ? "Autorize o IOI Desk a controlar este navegador em Ajustes do Sistema → Privacidade e Segurança → Automação." : sizeRefused ? "O aplicativo não confirmou o tamanho solicitado: ficou com \(Int(received!.width)) × \(Int(received!.height)); a zona oferece \(Int(target.width)) × \(Int(target.height)). Isso pode ser uma falha de ajuste ou um limite do aplicativo." : "A posição da janela não foi confirmada pelo aplicativo. Tente selecionar a janela pelo menu da zona."; message("\(ref.app.localizedName ?? "Aplicativo") · \(cell.name)\n\(detail)\n\(restored ? "A posição anterior foi restaurada." : "Não foi possível restaurar a posição anterior.")\nRegistro: \(operation)") }
            return false
        }
        if !originals.contains(where: { CFEqual($0.0, ref.element) }) { originals.append((ref.element, old)) }
        let origins = windows.filter { CFEqual($0.value.element, ref.element) }.map(\.key)
        for origin in origins { windows.removeValue(forKey: origin); profile.assignments.removeValue(forKey: origin) }
        windows[id] = ref; profile.assignments[id] = ref.saved
        trace.write("window.assign.complete",context.merging(["display":cell.display,"requested":rectText(target),"actual":rectText(windowRect(ref.element)),"releasedZones":origins]){ _,new in new },operation:operation)
        AXUIElementPerformAction(ref.element, kAXRaiseAction as CFString)
        ref.app.activate(options: [.activateIgnoringOtherApps]); updateHeaders(); save(); return true
    }
    @objc func recover(_ sender: NSMenuItem) { if let id = sender.representedObject as? String, let ref = windows[id] { assign(ref, to: id, source:"recover") } }
    @objc func clear(_ sender: NSMenuItem) {
        guard let id = sender.representedObject as? String else { return }
        trace.write("zone.released",["zone":id])
        if let ref = windows[id], let original = originals.first(where: { CFEqual($0.0, ref.element) }) { move(ref.element, original.1) }
        windows.removeValue(forKey: id); profile.assignments.removeValue(forKey: id); updateHeaders(); save()
    }
    func refresh() {
        requestCatalog()
        if trusted && !restoreAttempted {
            restoreAttempted = true; let list = catalog()
            for (id, saved) in profile.assignments {
                if let ref = list.first(where: { $0.app.processIdentifier == saved.pid && $0.app.bundleIdentifier == saved.bundle && $0.title == saved.title }) { assign(ref, to: id, notify: false, source:"restore") }
            }
        }
        let closed = windows.filter { $0.value.app.isTerminated || windowRect($0.value.element) == nil }.map(\.key)
        for id in closed { trace.write("window.closed",["zone":id]); windows.removeValue(forKey: id); profile.assignments.removeValue(forKey: id) }
        updateHeaders()
        if !closed.isEmpty { save() }
        let diagnostics: [String: Any] = ["accessibility": trusted, "zones": profile.cells.count, "assigned": windows.count, "displays": NSScreen.screens.map { ["id": screenID($0), "width": $0.frame.width, "height": $0.frame.height] as [String: Any] }]
        if let data = try? JSONSerialization.data(withJSONObject: diagnostics, options: [.prettyPrinted, .sortedKeys]) { try? data.write(to: folder.appendingPathComponent("status.json"), options: .atomic) }
    }
    @objc func arrange() { for (id, ref) in windows { assign(ref, to: id, source:"rearrange") } }
    @objc func screensChanged() { trace.write("displays.changed",["displays":NSScreen.screens.map { "\(screenID($0)) \(rectText($0.frame))" }]); buildBoards() } // Never replace missing monitor IDs with unrelated monitors.
    func drag(_ event: NSEvent) {
        if event.type == .leftMouseDown {
            dragWindow = nil; dragOrigin = nil
            guard trusted, let app = NSWorkspace.shared.frontmostApplication, app.processIdentifier != ProcessInfo.processInfo.processIdentifier else { return }
            let ax = AXUIElementCreateApplication(app.processIdentifier)
            if let value = attribute(ax, kAXFocusedWindowAttribute), CFGetTypeID(value) == AXUIElementGetTypeID() {
                let window = value as! AXUIElement
                guard (attribute(window,kAXSubroleAttribute) as? String) == kAXStandardWindowSubrole else { return }
                dragWindow = WindowRef(element: window, app: app, title: attribute(window, kAXTitleAttribute) as? String ?? "Janela")
                dragOrigin = windowRect(window)?.origin
            }
        }
        if event.type == .flagsChanged && !event.modifierFlags.contains(.shift) { hint?.orderOut(nil) }
        if event.type == .leftMouseDragged && event.modifierFlags.contains(.shift), let ref = dragWindow, let origin = dragOrigin,
           let current = windowRect(ref.element), hypot(current.minX - origin.x, current.minY - origin.y) > 4 {
            showHint(at: NSEvent.mouseLocation)
        }
        if event.type == .leftMouseUp {
            let ref=dragWindow;let origin=dragOrigin
            hint?.orderOut(nil);dragWindow=nil;dragOrigin=nil
            guard event.modifierFlags.contains(.shift), let ref = ref, let origin = origin,
                  let current = windowRect(ref.element), hypot(current.minX - origin.x, current.minY - origin.y) > 4,
                  let cell = cellAt(NSEvent.mouseLocation) else { return }
            // Let the target application's move loop finish before issuing AX positioning.
            DispatchQueue.main.asyncAfter(deadline:.now()+0.15){[weak self] in self?.assign(ref,to:cell.id,source:"shift-drag")}
        }
    }
    func cellAt(_ point: CGPoint) -> Cell? {
        profile.cells.first { cell in NSScreen.screens.first(where: { screenID($0) == cell.display }).map { cell.rect($0.visibleFrame).contains(point) } ?? false }
    }
    func showHint(at point: CGPoint) {
        guard let cell = cellAt(point), let screen = NSScreen.screens.first(where: { screenID($0) == cell.display }) else { hint?.orderOut(nil); return }
        if hint == nil {
            hint = NSPanel(contentRect: .zero, styleMask: [.borderless, .nonactivatingPanel], backing: .buffered, defer: false)
            hint?.isReleasedWhenClosed = false; hint?.ignoresMouseEvents = true; hint?.level = .floating
            hint?.backgroundColor = NSColor.systemBlue.withAlphaComponent(0.22)
        }
        hint?.setFrame(cell.rect(screen.visibleFrame), display: true); hint?.orderFrontRegardless()
    }
    @objc func access() {
        // The user grants permission in macOS; this app never changes privacy settings itself.
        let options = [kAXTrustedCheckOptionPrompt.takeUnretainedValue() as String: true] as CFDictionary
        let allowed = AXIsProcessTrustedWithOptions(options)
        message(allowed ? "Acesso às janelas disponível." : "Autorize IOI Desk em Ajustes do Sistema → Privacidade e Segurança → Acessibilidade. Depois feche e reabra o IOI Desk.")
    }
    func message(_ text: String) { let alert = NSAlert(); alert.messageText = Product.name; alert.informativeText = text; alert.runModal() }
    @objc func edit() {
        let menu = NSMenu()
        for screen in NSScreen.screens {
            let submenu = NSMenu()
            for (label, grid) in [("Inteiro · leitura", [1, 1]), ("Duas colunas", [2, 1]), ("Duas linhas", [1, 2]), ("Quatro zonas", [2, 2]), ("Seis zonas", [2, 3])] {
                let item = NSMenuItem(title: label, action: #selector(layout(_:)), keyEquivalent: ""); item.target = self; item.representedObject = [Int(screenID(screen)), grid[0], grid[1]]; submenu.addItem(item)
            }
            let item = NSMenuItem(title: screen.localizedName, action: nil, keyEquivalent: ""); item.submenu = submenu; menu.addItem(item)
        }
        menu.addItem(.separator())
        let custom = NSMenuItem(title: "Editar tamanhos em porcentagens…", action: #selector(customLayout), keyEquivalent: ""); custom.target = self; menu.addItem(custom)
        if let button = status.button { menu.popUp(positioning: nil, at: NSPoint(x: 0, y: button.bounds.minY), in: button) }
    }
    @objc func layout(_ sender: NSMenuItem) {
        guard let values = sender.representedObject as? [Int] else { return }
        let display = UInt32(values[0]); let columns = values[1]; let rows = values[2]
        trace.write("layout.selected",["display":display,"columns":columns,"rows":rows])
        let old = profile.cells.filter { $0.display == display }; for cell in old { if let ref = windows[cell.id], let original = originals.first(where: { CFEqual($0.0, ref.element) }) { move(ref.element, original.1) }; windows.removeValue(forKey: cell.id); profile.assignments.removeValue(forKey: cell.id) }
        profile.cells.removeAll { $0.display == display }
        for row in 0..<rows { for col in 0..<columns {
            profile.cells.append(Cell(id: "\(display)-\(row)-\(col)", name: columns * rows == 1 ? "Leitura" : "Zona \(row * columns + col + 1)", display: display, x: Double(col)/Double(columns), y: Double(row)/Double(rows), w: 1/Double(columns), h: 1/Double(rows)))
        } }
        buildBoards(); save()
    }
    @objc func customLayout() {
        let alert = NSAlert(); alert.messageText = "Tamanho da zona"; alert.informativeText = "Escolha a zona e informe X, Y, largura e altura em porcentagens do monitor. A origem fica no canto superior esquerdo."
        let box = NSView(frame: NSRect(x: 0, y: 0, width: 420, height: 100))
        let picker = NSPopUpButton(frame: NSRect(x: 0, y: 65, width: 420, height: 28)); picker.addItems(withTitles: profile.cells.map { "\($0.name) · \($0.id)" }); box.addSubview(picker)
        let field = NSTextField(frame: NSRect(x: 0, y: 25, width: 420, height: 28)); field.placeholderString = "Exemplo: 0, 0, 50, 60"; box.addSubview(field)
        alert.accessoryView = box; alert.addButton(withTitle: "Aplicar"); alert.addButton(withTitle: "Cancelar")
        guard alert.runModal() == .alertFirstButtonReturn, profile.cells.indices.contains(picker.indexOfSelectedItem) else { return }
        let values = field.stringValue.split(separator: ",").compactMap { Double($0.trimmingCharacters(in: .whitespaces)) }
        guard values.count == 4, values.allSatisfy({ $0.isFinite }), values[0] >= 0, values[1] >= 0, values[2] >= 5, values[3] >= 5, values[0]+values[2] <= 100.001, values[1]+values[3] <= 100.001 else { message("Informe quatro porcentagens válidas, dentro do monitor."); return }
        let i = picker.indexOfSelectedItem; var candidate = profile.cells[i]
        candidate.x = values[0]/100; candidate.y = values[1]/100; candidate.w = values[2]/100; candidate.h = values[3]/100
        let boxRect = CGRect(x: candidate.x, y: candidate.y, width: candidate.w, height: candidate.h)
        if profile.cells.enumerated().contains(where: { j, other in j != i && other.display == candidate.display && boxRect.intersection(CGRect(x: other.x,y:other.y,width:other.w,height:other.h)).width > 0.0001 && boxRect.intersection(CGRect(x: other.x,y:other.y,width:other.w,height:other.h)).height > 0.0001 }) { message("Essa área invade outra zona. Reduza a zona vizinha primeiro."); return }
        trace.write("layout.zone.changed",["zone":candidate.id,"display":candidate.display,"x":candidate.x,"y":candidate.y,"width":candidate.w,"height":candidate.h])
        profile.cells[i] = candidate; buildBoards(); save(); arrange()
    }
    @objc func settings() { openSettings?() }
    @objc func sharing() { openSettings?() }
    @objc func quit() { for original in originals { move(original.0, original.1) }; requestQuit?() }
    func stop() {
        guard started else { return }; save(); started = false
        NotificationCenter.default.removeObserver(self)
        if let monitor { NSEvent.removeMonitor(monitor) }; monitor = nil
        timer?.invalidate(); timer = nil
        boards.forEach { $0.close() }; boards.removeAll(); headers.removeAll()
        if let status { NSStatusBar.system.removeStatusItem(status) }; status = nil
    }
}

