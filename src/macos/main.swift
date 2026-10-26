import AppKit
do {
    if CommandLine.arguments.contains("--layout-test") { try runLayoutTests() }
    else if CommandLine.arguments.contains("--lifecycle-test") { try runLifecycleTests() }
    else if CommandLine.arguments.contains("--migration-test") { try AppPaths.test() }
    else if try !runWorkspaceTests() {
        try AppPaths.migrate()
        let app=NSApplication.shared;app.setActivationPolicy(.regular)
        let controller=AppController();app.delegate=controller;app.run()
    }
} catch { fputs("IOI Desk: \(error)\n",stderr);exit(1) }
