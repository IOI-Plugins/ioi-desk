import AppKit
import ServiceManagement
import ApplicationServices

// One application delegate owns UI, workspace and connection lifecycle.
final class AppController: NSObject, NSApplicationDelegate {
    let directory=AppPaths.workspace
    let workspace=WorkspaceController()
    let updates=DeskUpdates()
    var window:NSWindow!
    var pickers:[(UInt32,NSPopUpButton)]=[]
    var state=NSTextField(labelWithString:"")
    var timer:Timer?
    var busy=false
    var badges:[NSPanel]=[]
    let connection=DeskConnection()
    let connectionState=NSTextField(wrappingLabelWithString:"Desconectado")
    let login=NSButton(checkboxWithTitle:"Abrir IOI Desk ao iniciar a sessão",target:nil,action:nil)
    func applicationDidFinishLaunching(_ notification:Notification) {
        let menu=NSMenu(); let appItem=NSMenuItem(); let appMenu=NSMenu()
        updates.addMenu(to:appMenu)
        let quit=NSMenuItem(title:"Encerrar IOI Desk",action:#selector(stopAll),keyEquivalent:"q");quit.target=self;appMenu.addItem(quit);appItem.submenu=appMenu;menu.addItem(appItem);NSApp.mainMenu=menu
        window=NSWindow(contentRect:NSRect(x:0,y:0,width:740,height:900),styleMask:[.titled,.closable,.miniaturizable,.resizable],backing:.buffered,defer:false)
        window.minSize=NSSize(width:680,height:880)
        window.setFrameAutosaveName("IOIDeskSettings")
        window.title="IOI Desk · Configurações"; window.isReleasedWhenClosed=false;window.center();window.collectionBehavior=[.moveToActiveSpace]
        let content=window.contentView!
        let stack=NSStackView();stack.orientation = .vertical;stack.alignment = .leading;stack.spacing=16;stack.translatesAutoresizingMaskIntoConstraints=false
        content.addSubview(stack)
        NSLayoutConstraint.activate([stack.leadingAnchor.constraint(equalTo:content.leadingAnchor,constant:24),stack.trailingAnchor.constraint(equalTo:content.trailingAnchor,constant:-24),stack.topAnchor.constraint(equalTo:content.topAnchor,constant:24),stack.bottomAnchor.constraint(lessThanOrEqualTo:content.bottomAnchor,constant:-24)])
        func text(_ value:String,_ size:CGFloat=13,_ bold:Bool=false)->NSTextField{let t=NSTextField(wrappingLabelWithString:value);t.font=bold ? .boldSystemFont(ofSize:size):.systemFont(ofSize:size);return t}
        func row(_ views:[NSView])->NSStackView{let r=NSStackView(views:views);r.orientation = .horizontal;r.spacing=12;r.alignment = .centerY;return r}
        func action(_ name:String,_ selector:Selector)->NSButton{let b=NSButton(title:name,target:self,action:selector);b.bezelStyle = .rounded;return b}
        stack.addArrangedSubview(text("IOI Desk",27,true));stack.addArrangedSubview(text("Seu espaço de trabalho integrado.",14))
        stack.addArrangedSubview(text("Telas e layouts",18,true))
        for (index,screen) in NSScreen.screens.enumerated() {
            let id=(screen.deviceDescription[NSDeviceDescriptionKey("NSScreenNumber")] as? NSNumber)?.uint32Value ?? 0
            let title=NSStackView(views:[text("MC · Tela \(index+1) · \(screen.localizedName)",13,true),text("\(Int(screen.frame.width)) × \(Int(screen.frame.height))",11)]);title.orientation = .vertical;title.alignment = .leading
            let picker=NSPopUpButton()
            picker.addItems(withTitles:["Manter layout atual","Inteira · leitura","2 faixas · largura inteira","3 faixas · largura inteira","2 colunas","4 zonas · 2 × 2","6 zonas · 2 × 3"])
            let screenRow=row([title,NSView(),picker]);stack.addArrangedSubview(screenRow);screenRow.widthAnchor.constraint(equalTo:stack.widthAnchor).isActive=true;pickers.append((id,picker))
        }
        stack.addArrangedSubview(text("Prefira faixas de largura inteira para aplicativos que não cabem em colunas estreitas.",12))
        stack.addArrangedSubview(row([action("Identificar telas",#selector(identify)),action("Aplicar layouts",#selector(apply)),action("Abrir registros",#selector(logs))]))
        let separator=NSBox();separator.boxType = .separator;stack.addArrangedSubview(separator);separator.widthAnchor.constraint(equalTo:stack.widthAnchor).isActive=true
        stack.addArrangedSubview(text("Permissões e inicialização",18,true))
        stack.addArrangedSubview(text("A autorização de Acessibilidade é concedida por você nos Ajustes do Sistema. O IOI Desk verifica o estado automaticamente.",12))
        stack.addArrangedSubview(action("Permissões do macOS…",#selector(permissions)))
        login.target=self;login.action=#selector(loginChanged);login.state=SMAppService.mainApp.status == .enabled ? .on:.off;stack.addArrangedSubview(login)
        stack.addArrangedSubview(row([action("Iniciar organizador",#selector(start)),action("Encerrar IOI Desk",#selector(stopAll))]))
        stack.addArrangedSubview(text("Mouse e teclado",18,true))
        stack.addArrangedSubview(text("Use os periféricos do Windows neste Mac. Ctrl + Alt + → traz o controle ao Mac; Ctrl + Alt + ← retorna ao Windows.",12))
        stack.addArrangedSubview(row([action("Conectar ao Windows",#selector(connect)),action("Desconectar",#selector(disconnect)),action("Diagnóstico da conexão",#selector(connectionLogs))]))
        connectionState.font = .systemFont(ofSize:12);stack.addArrangedSubview(connectionState)
        connection.update={[weak self] text in self?.connectionState.stringValue=text}
        state.font = .systemFont(ofSize:12);state.maximumNumberOfLines=2;stack.addArrangedSubview(state)
        show();refresh()
        timer=Timer.scheduledTimer(withTimeInterval:2,repeats:true){[weak self] _ in self?.refresh()}
        start()
        if FileManager.default.fileExists(atPath:connection.directory.appendingPathComponent("enabled.txt").path){connect()}
        updates.start()
    }
    func label(_ text:String,x:Int,y:Int,w:Int,h:Int,size:CGFloat,bold:Bool=false){
        let label=NSTextField(wrappingLabelWithString:text);label.font=bold ? .boldSystemFont(ofSize:size):.systemFont(ofSize:size);label.frame=NSRect(x:x,y:y,width:w,height:h);window.contentView?.addSubview(label)
    }
    func button(_ title:String,x:Int,y:Int,w:Int,action:Selector){let b=NSButton(title:title,target:self,action:action);b.frame=NSRect(x:x,y:y,width:w,height:32);b.bezelStyle = .rounded;window.contentView?.addSubview(b)}
    func show(){window.makeKeyAndOrderFront(nil);NSApp.activate(ignoringOtherApps:true)}
    func applicationShouldHandleReopen(_ sender:NSApplication,hasVisibleWindows flag:Bool)->Bool{show();return true}
    func refresh(){
        if busy{return}
        let data=try? Data(contentsOf:directory.appendingPathComponent("status.json"))
        let status=data.flatMap{try? JSONSerialization.jsonObject(with:$0) as? [String:Any]}
        state.stringValue = !workspace.started ? "Organizador encerrado. Use Iniciar organizador para mostrar as zonas." : "Organizador aberto · Acessibilidade: \((status?["accessibility"] as? Bool) == true ? "OK":"pendente") · \(status?["zones"] as? Int ?? 0) zonas"
        let report:[String:Any]=["visible":window.isVisible,"workspaceRunning":workspace.started,"title":window.title,"singleProcess":true,"version":Product.version,"nativeLayout":true,"connection":connectionState.stringValue]
        if let data=try? JSONSerialization.data(withJSONObject:report){try? data.write(to:directory.appendingPathComponent("settings-status.json"),options:.atomic)}
    }
    @objc func start(){
        workspace.openSettings = { [weak self] in self?.show() }
        workspace.requestQuit = { [weak self] in self?.stopAll() }
        workspace.start(); refresh()
    }
    @objc func permissions(){
        let options=[kAXTrustedCheckOptionPrompt.takeUnretainedValue() as String:true] as CFDictionary
        _ = AXIsProcessTrustedWithOptions(options)
        if let url=URL(string:"x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility"){NSWorkspace.shared.open(url)}
        alert("Em Acessibilidade, autorize somente IOI Desk. Organização de janelas e conexão pertencem ao mesmo aplicativo.")
    }
    @objc func loginChanged(){
        do{if login.state == .on{try SMAppService.mainApp.register()}else{try SMAppService.mainApp.unregister()};if SMAppService.mainApp.status == .requiresApproval{SMAppService.openSystemSettingsLoginItems()}}
        catch{login.state=SMAppService.mainApp.status == .enabled ? .on:.off;alert("O macOS não concluiu a alteração: \(error.localizedDescription)")}
    }
    @objc func logs(){NSWorkspace.shared.open(directory.appendingPathComponent("Logs"))}
    @objc func identify(){
        badges.forEach{$0.close()};badges=[]
        for(index,screen) in NSScreen.screens.enumerated(){
            let p=NSPanel(contentRect:NSRect(x:screen.visibleFrame.midX-160,y:screen.visibleFrame.midY-70,width:320,height:140),styleMask:[.borderless,.nonactivatingPanel],backing:.buffered,defer:false)
            p.isReleasedWhenClosed=false;p.level = .floating;p.backgroundColor=NSColor.windowBackgroundColor
            let label=NSTextField(labelWithString:"MC · Tela \(index+1)");label.font = .boldSystemFont(ofSize:38);label.alignment = .center;label.frame=NSRect(x:0,y:45,width:320,height:60);p.contentView?.addSubview(label);p.orderFrontRegardless();badges.append(p)
        }
        DispatchQueue.main.asyncAfter(deadline:.now()+3){[weak self] in self?.badges.forEach{$0.close()};self?.badges=[]}
    }
    func alert(_ text:String){let a=NSAlert();a.messageText="IOI Desk";a.informativeText=text;a.runModal()}
    static func transformed(_ input:[String:Any],changes:[UInt32:Int])->[String:Any]{
        var profile=input;var cells=input["cells"] as? [[String:Any]] ?? [];var assignments=input["assignments"] as? [String:Any] ?? [:]
        for (display,layout) in changes where layout>0 {
            for cell in cells where (cell["display"] as? NSNumber)?.uint32Value == display {if let id=cell["id"] as? String{assignments.removeValue(forKey:id)}}
            cells.removeAll{($0["display"] as? NSNumber)?.uint32Value == display}
            let columns=layout==4 || layout==5 || layout==6 ? 2:1
            let rows=layout==2 || layout==5 ? 2:layout==3 || layout==6 ? 3:1
            for row in 0..<rows {for col in 0..<columns {
                cells.append(["id":"\(display)-\(row)-\(col)","name":rows*columns==1 ? "Leitura":"Zona \(row*columns+col+1)","display":display,"x":Double(col)/Double(columns),"y":Double(row)/Double(rows),"w":1.0/Double(columns),"h":1.0/Double(rows)])
            }}
        }
        profile["version"]=1;profile["cells"]=cells;profile["assignments"]=assignments;return profile
    }
    @objc func apply(){
        let changes=Dictionary(uniqueKeysWithValues:pickers.filter{$0.1.indexOfSelectedItem>0}.map{($0.0,$0.1.indexOfSelectedItem)})
        guard !changes.isEmpty else{alert("Escolha um novo layout em pelo menos uma tela.");return}
        guard !busy else{return};busy=true;defer{busy=false}
        do {
            let raw=try JSONEncoder().encode(workspace.profile)
            guard let old=try JSONSerialization.jsonObject(with:raw) as? [String:Any] else{throw NSError(domain:"IOIDesk",code:1)}
            let updated=Self.transformed(old,changes:changes)
            let profile=try JSONDecoder().decode(Profile.self,from:JSONSerialization.data(withJSONObject:updated))
            try raw.write(to:directory.appendingPathComponent("workspace-before-settings.json"),options:.atomic)
            // Change the workspace in this process, preserving unaffected window references.
            let retained=Set(profile.cells.map{$0.id})
            workspace.windows=workspace.windows.filter{retained.contains($0.key) && profile.assignments[$0.key] != nil}
            workspace.profile=profile;workspace.buildBoards();workspace.save();workspace.arrange()
            pickers.forEach{$0.1.selectItem(at:0)}
        }catch{alert("Não foi possível aplicar o layout: \(error.localizedDescription)")}
    }
    @objc func connect(){connection.start()}
    @objc func disconnect(){connection.stop(disable:true)}
    @objc func connectionLogs(){try? FileManager.default.createDirectory(at:connection.directory.appendingPathComponent("Logs"),withIntermediateDirectories:true);NSWorkspace.shared.open(connection.directory.appendingPathComponent("Logs"))}
    func applicationWillTerminate(_ notification:Notification){timer?.invalidate();connection.stop(disable:false);workspace.stop()}
    @objc func stopAll(){NSApp.terminate(nil)}
}

