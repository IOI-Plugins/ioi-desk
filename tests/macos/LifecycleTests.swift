import AppKit

func runLifecycleTests() throws {
    guard ProcessInfo.processInfo.environment["IOI_DESK_DATA_ROOT"] != nil else {throw NSError(domain:"IOIDesk.Tests",code:1,userInfo:[NSLocalizedDescriptionKey:"Lifecycle test requires isolated IOI_DESK_DATA_ROOT"])}
    let app=NSApplication.shared;app.setActivationPolicy(.accessory);app.finishLaunching()
    let controller=AppController()
    let notification=Notification(name:NSApplication.didFinishLaunchingNotification)
    controller.applicationDidFinishLaunching(notification)
    let count=controller.workspace.boards.count
    precondition(controller.workspace.started && count>0)
    controller.start()
    precondition(controller.workspace.boards.count==count,"Starting twice duplicated zones")
    controller.applicationWillTerminate(notification)
    precondition(!controller.workspace.started && controller.workspace.boards.isEmpty)
    controller.start()
    precondition(controller.workspace.started && controller.workspace.boards.count==count)
    controller.applicationWillTerminate(notification)
    controller.window.close()
    precondition(controller.connection.task?.isRunning != true)
    print("PASS: one application owns settings, zones and connection; repeated start does not duplicate zones; stop/restart cleans lifecycle")
}
