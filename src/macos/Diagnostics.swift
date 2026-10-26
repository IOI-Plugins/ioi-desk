import AppKit
import ApplicationServices

final class TraceLog {
    let folder: URL
    let session = UUID().uuidString
    let lock = NSLock()
    static let limit = 1024 * 1024
    init(folder: URL) { self.folder = folder }
    func write(_ event: String, _ fields: [String: Any] = [:], operation: String = "") {
        lock.lock(); defer { lock.unlock() }
        do {
            let fm = FileManager.default
            try fm.createDirectory(at:folder,withIntermediateDirectories:true)
            for file in try fm.contentsOfDirectory(at:folder,includingPropertiesForKeys:[.contentModificationDateKey]) where file.lastPathComponent.hasPrefix("events") && file.pathExtension == "jsonl" {
                if let modified = try file.resourceValues(forKeys:[.contentModificationDateKey]).contentModificationDate, modified < Date().addingTimeInterval(-7*86400) { try fm.removeItem(at:file) }
            }
            let current=folder.appendingPathComponent("events.jsonl")
            if ((try? fm.attributesOfItem(atPath:current.path)[.size] as? NSNumber)?.intValue ?? 0) >= Self.limit {
                let oldest=folder.appendingPathComponent("events.7.jsonl"); if fm.fileExists(atPath:oldest.path) { try fm.removeItem(at:oldest) }
                for i in stride(from:6,through:1,by:-1) { let file=folder.appendingPathComponent("events.\(i).jsonl"); if fm.fileExists(atPath:file.path) { try fm.moveItem(at:file,to:folder.appendingPathComponent("events.\(i+1).jsonl")) } }
                try fm.moveItem(at:current,to:folder.appendingPathComponent("events.1.jsonl"))
            }
            var record=fields.mapValues { value -> Any in if let text=value as? String { return String(text.prefix(1800)) }; return value }
            record["time"]=ISO8601DateFormatter().string(from:Date()); record["session"]=session; record["event"]=event; record["operation"]=operation
            var data=try JSONSerialization.data(withJSONObject:record,options:[.sortedKeys]); data.append(10)
            if !fm.fileExists(atPath:current.path) { fm.createFile(atPath:current.path,contents:nil) }
            let handle=try FileHandle(forWritingTo:current); defer { try? handle.close() }; try handle.seekToEnd(); try handle.write(contentsOf:data)
        } catch { /* Diagnostics must never block the user's work, even with a full disk. */ }
    }
}
