import Foundation

enum AppPaths {
    static var root:URL { ProcessInfo.processInfo.environment["IOI_DESK_DATA_ROOT"].map{URL(fileURLWithPath:$0)} ?? FileManager.default.homeDirectoryForCurrentUser.appendingPathComponent("Library/Application Support/IOI Desk") }
    static var workspace:URL {root.appendingPathComponent("Workspace")}
    static var connection:URL {root.appendingPathComponent("Connection")}
    static func migrate(from legacy:URL? = nil,to destination:URL? = nil) throws {
        let fm=FileManager.default
        let source=(legacy ?? fm.homeDirectoryForCurrentUser.appendingPathComponent("Library/Application Support/Camadas")).appendingPathComponent("workspace.json")
        let directory=destination ?? workspace,target=directory.appendingPathComponent("workspace.json")
        try fm.createDirectory(at:directory,withIntermediateDirectories:true)
        guard !fm.fileExists(atPath:target.path),fm.fileExists(atPath:source.path) else{return}
        let data=try Data(contentsOf:source)
        _ = try JSONDecoder().decode(Profile.self,from:data)
        let staging=directory.appendingPathComponent(".migration-"+UUID().uuidString)
        defer{try? fm.removeItem(at:staging)}
        try data.write(to:staging,options:.atomic)
        try fm.moveItem(at:staging,to:target)
    }
    static func test() throws {
        let fm=FileManager.default,temp=fm.temporaryDirectory.appendingPathComponent(UUID().uuidString)
        defer{try? fm.removeItem(at:temp)}
        let old=temp.appendingPathComponent("old"),new=temp.appendingPathComponent("new")
        try fm.createDirectory(at:old,withIntermediateDirectories:true)
        let profile=Profile(cells:defaultCells([1,2]),assignments:["reading":SavedWindow(pid:123,bundle:"test.app",title:"Sessão")])
        let data=try JSONEncoder().encode(profile)
        try data.write(to:old.appendingPathComponent("workspace.json"))
        try migrate(from:old,to:new)
        let target=new.appendingPathComponent("workspace.json")
        let migrated=try JSONDecoder().decode(Profile.self,from:Data(contentsOf:target))
        precondition(migrated.assignments["reading"]?.title=="Sessão")
        try Data("edited".utf8).write(to:target);try migrate(from:old,to:new)
        let current=try Data(contentsOf:target),original=try Data(contentsOf:old.appendingPathComponent("workspace.json"))
        precondition(current==Data("edited".utf8))
        precondition(original==data)
        print("PASS: workspace migration preserves assignments, original data and subsequent changes")
    }
}
