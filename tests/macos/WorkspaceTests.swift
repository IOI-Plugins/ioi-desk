import AppKit
import ApplicationServices

func runWorkspaceTests() throws -> Bool {
if CommandLine.arguments.contains("--trace-test") {
    let folder=FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
    try FileManager.default.createDirectory(at:folder,withIntermediateDirectories:true)
    let expired=folder.appendingPathComponent("events.expired.jsonl")
    try Data("expired".utf8).write(to:expired)
    try FileManager.default.setAttributes([.modificationDate:Date().addingTimeInterval(-8*86400)],ofItemAtPath:expired.path)
    let trace=TraceLog(folder:folder)
    for _ in 0..<10 { try Data(repeating:120,count:TraceLog.limit).write(to:folder.appendingPathComponent("events.jsonl")); trace.write("test",["title":"ação \"quoted\"\nline"],operation:"test") }
    let files=try FileManager.default.contentsOfDirectory(at:folder,includingPropertiesForKeys:nil)
    precondition(files.count <= 8 && !FileManager.default.fileExists(atPath:expired.path))
    let row=try JSONSerialization.jsonObject(with:Data(contentsOf:folder.appendingPathComponent("events.jsonl"))) as! [String:Any]
    precondition(row["title"] as? String == "ação \"quoted\"\nline")
    try FileManager.default.removeItem(at:folder)
    print("PASS: eight-file rotation, seven-day expiry, valid UTF-8 JSON and escaped window titles")
} else if CommandLine.arguments.contains("--self-test") {
    let target = CGRect(x:1083,y:1002,width:1074,height:915)
    precondition(fitsZone(CGRect(x:1085,y:1015,width:1074,height:905),target))
    precondition(!fitsZone(CGRect(x:1083,y:1002,width:1200,height:915),target))
    precondition(!fitsZone(CGRect(x:1083,y:1002,width:534,height:889),target))
    precondition(!fitsZone(CGRect(x:1083,y:970,width:1074,height:915),target))
    let cells = defaultCells([10,20]); precondition(cells.count == 5)
    let area = CGRect(x:-1080,y:-500,width:1080,height:1888)
    for cell in cells { let rect=cell.rect(area); precondition(area.contains(rect) && rect.width>0 && rect.height>0) }
    for i in cells.indices { for j in cells.indices where j>i && cells[i].display == cells[j].display { precondition(!cells[i].rect(area).intersects(cells[j].rect(area))) } }
    let copy = try JSONDecoder().decode(Profile.self,from:JSONEncoder().encode(Profile(cells:cells)))
    precondition(copy.cells.count == cells.count)
    print("PASS: two-monitor geometry, negative origins, non-overlap and profile round trip")
} else if CommandLine.arguments.contains("--chrome-placement-test") {
    let app=NSApplication.shared;app.setActivationPolicy(.accessory);app.finishLaunching()
    guard AXIsProcessTrusted() else{print("BLOCKED: esta compilação de teste ainda não possui Acessibilidade para testar janelas do Chrome; nenhuma janela foi alterada.");exit(2)}
    let controller=WorkspaceController()
    guard let ref=controller.catalog().first(where:{$0.app.bundleIdentifier == "com.google.Chrome" && (attribute($0.element,kAXMinimizedAttribute) as? Bool) != true && (attribute($0.element,"AXFullScreen") as? Bool) != true}),let original=windowRect(ref.element),let screen=NSScreen.screens.first else{fatalError("Nenhuma janela Chrome normal disponível para teste")}
    let visible=screen.visibleFrame
    let target=cocoaToAX(CGRect(x:visible.minX+visible.width/2+3,y:visible.midY,width:visible.width/2-6,height:visible.height/2-30))
    var errors:[String:Int32]=[:]
    let accepted=move(ref.element,target,errors:&errors)
    let actual=windowRect(ref.element)
    let restored=move(ref.element,original)
    let back=windowRect(ref.element)
    print("target=\(rectText(target)) actual=\(rectText(actual)) errors=\(errors) restored=\(restored) restoredBounds=\(rectText(back))")
    precondition(restored && back.map{abs($0.minX-original.minX)<6 && abs($0.minY-original.minY)<6 && abs($0.width-original.width)<6 && abs($0.height-original.height)<6} == true,"Chrome restore failed")
    precondition(accepted && actual.map{abs($0.minX-target.minX)<6 && abs($0.minY-target.minY)<6 && abs($0.width-target.width)<6 && abs($0.height-target.height)<6} == true,"Chrome placement failed")
    print("PASS: real Chrome fits a half-width zone and returns to original bounds")
} else if CommandLine.arguments.contains("--panel-test") {
    let app = NSApplication.shared; app.setActivationPolicy(.accessory); app.finishLaunching()
    let temp = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
    try FileManager.default.createDirectory(at: temp, withIntermediateDirectories: true)
    let controller = WorkspaceController(folder: temp)
    controller.profile = Profile(cells: defaultCells(NSScreen.screens.map(screenID)))
    controller.buildBoards()
    precondition(!controller.boards.isEmpty)
    let count = controller.boards.count
    precondition(controller.boards.allSatisfy { !$0.hidesOnDeactivate && $0.isVisible && !$0.canBecomeKey && !$0.canBecomeMain })
    app.activate(ignoringOtherApps: true)
    RunLoop.current.run(until: Date().addingTimeInterval(0.15))
    app.deactivate()
    RunLoop.current.run(until: Date().addingTimeInterval(0.15))
    precondition(controller.boards.filter { $0.isVisible }.count == count, "Deactivation hid zones")
    controller.boards[0].orderOut(nil)
    controller.menuDidClose(NSMenu())
    precondition(controller.boards.allSatisfy { $0.isVisible }, "Closing picker failed to restore zones")
    controller.boards.forEach { $0.close() }
    try FileManager.default.removeItem(at: temp)
    print("PASS: zones remain visible after deactivation; picker close restores hidden zones; panels never take keyboard focus")
} else if CommandLine.arguments.contains("--integration-test") {
    let app = NSApplication.shared; app.setActivationPolicy(.accessory); app.finishLaunching()
    let test = NSWindow(contentRect: NSRect(x:80,y:80,width:500,height:260), styleMask:[.titled,.resizable],backing:.buffered,defer:false)
    test.isReleasedWhenClosed=false; test.title="Camadas · teste de encaixe"; test.minSize=NSSize(width:480,height:160)
    test.makeKeyAndOrderFront(nil)
    RunLoop.current.run(until:Date().addingTimeInterval(0.3))
    let ax = AXUIElementCreateApplication(ProcessInfo.processInfo.processIdentifier)
    guard let items = attribute(ax,kAXWindowsAttribute) as? [AXUIElement], let element=items.first,
          let original=windowRect(element), let screen=NSScreen.screens.first else { fatalError("Janela de teste AX indisponível") }
    let temp=FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
    try FileManager.default.createDirectory(at:temp,withIntermediateDirectories:true)
    let controller=WorkspaceController(folder:temp); let display=screenID(screen)
    controller.profile=Profile(cells:[Cell(id:"a",name:"A",display:display,x:0,y:0,w:0.5,h:0.5),Cell(id:"b",name:"B",display:display,x:0.5,y:0,w:0.5,h:0.5),Cell(id:"small",name:"Small",display:display,x:0,y:0.7,w:0.1,h:0.1)])
    let ref=WindowRef(element:element,app:NSRunningApplication.current,title:test.title)
    precondition(controller.assign(ref,to:"a",notify:false),"Primeiro encaixe falhou")
    precondition(controller.assign(ref,to:"b",notify:false),"Transferência falhou")
    precondition(controller.windows["a"] == nil && controller.windows["b"] != nil,"Origem não foi liberada")
    let fitted=windowRect(element)!
    precondition(!controller.assign(ref,to:"small",notify:false),"Zona abaixo do mínimo foi aceita")
    precondition(controller.windows["b"] != nil && windowRect(element) == fitted,"Rejeição alterou atribuição/posição")
    move(element,original); test.close()
    try FileManager.default.removeItem(at:temp)
    print("PASS: native AX resize, transfer frees origin, minimum rejected, rollback preserves assignment; user windows untouched")
} else if CommandLine.arguments.contains("--diagnostics") {
    print("Accessibility: \(AXIsProcessTrusted())")
    for screen in NSScreen.screens { print("\(screenID(screen)) \(screen.localizedName) frame=\(screen.frame) usable=\(screen.visibleFrame)") }
} else { return false }
return true

}
