import AppKit

final class DeskConnection {
    let directory=AppPaths.connection
    var task:Process?
    var update:((String)->Void)?
    var failures=0
    var retry:Timer?
    var restartPending=false
    let lock=NSLock()
    func status(_ text:String){DispatchQueue.main.async{self.update?(text)}}
    func log(_ text:String){
        lock.lock();defer{lock.unlock()}
        let fm=FileManager.default,folder=directory.appendingPathComponent("Logs"),file=folder.appendingPathComponent("connection.log")
        do{
            try fm.createDirectory(at:folder,withIntermediateDirectories:true)
            for item in try fm.contentsOfDirectory(at:folder,includingPropertiesForKeys:[.contentModificationDateKey]) where item.pathExtension=="log" {if let date=try item.resourceValues(forKeys:[.contentModificationDateKey]).contentModificationDate,date<Date().addingTimeInterval(-7*86400){try fm.removeItem(at:item)}}
            if ((try? fm.attributesOfItem(atPath:file.path)[.size] as? NSNumber)?.intValue ?? 0)>512*1024 {let previous=folder.appendingPathComponent("connection.previous.log");if fm.fileExists(atPath:previous.path){try fm.removeItem(at:previous)};try fm.moveItem(at:file,to:previous)}
            if !fm.fileExists(atPath:file.path){fm.createFile(atPath:file.path,contents:nil)}
            let handle=try FileHandle(forWritingTo:file);defer{try? handle.close()};try handle.seekToEnd();try handle.write(contentsOf:Data((ISO8601DateFormatter().string(from:Date())+" "+String(text.prefix(1800))+"\n").utf8))
        }catch{}
    }
    func start(){
        guard task?.isRunning != true else{status("Conexão já iniciada. Confira o estado no Windows.");return}
        retry?.invalidate();retry=nil;failures=0;restartPending=false
        let fm=FileManager.default,config=directory.appendingPathComponent("Deskflow.conf")
        guard fm.fileExists(atPath:config.path),fm.fileExists(atPath:directory.appendingPathComponent("tls/trusted-servers").path),fm.fileExists(atPath:directory.appendingPathComponent("tls/deskflow.pem").path) else{status("O pareamento deste Mac ainda precisa ser concluído.");return}
        let executable=Bundle.main.bundleURL.appendingPathComponent("Contents/Helpers/Deskflow.app/Contents/MacOS/deskflow-core")
        let process=Process(),pipe=Pipe();process.executableURL=executable;process.arguments=["client","-s",config.path];process.standardOutput=pipe;process.standardError=pipe
        pipe.fileHandleForReading.readabilityHandler={[weak self] handle in let data=handle.availableData;if data.isEmpty{handle.readabilityHandler=nil;return};guard let self else{return};let text=String(decoding:data.prefix(1800),as:UTF8.self);self.log(text);let lower=text.lowercased();DispatchQueue.main.async{
            if lower.contains("failed to connect") || lower.contains("timed out"){self.failures+=1}
            if (self.failures>=20 || lower.contains("too many open files")) && !self.restartPending {self.stop(disable:false);self.restartPending=true;self.status("Windows indisponível. Nova tentativa em 15 segundos.");self.log("retry.guard.restart");self.retry=Timer.scheduledTimer(withTimeInterval:15,repeats:false){[weak self] _ in guard let self,FileManager.default.fileExists(atPath:self.directory.appendingPathComponent("enabled.txt").path) else{return};self.start()};return}
            if lower.contains("disconnected"){self.status("Windows desconectado. Aguardando reconexão.")}else if lower.contains("connected to server") || lower.contains("connected to secure socket"){self.failures=0;self.status("Conexão segura estabelecida com o Windows.")}else if lower.contains("accessibility") || lower.contains("not trusted"){self.status("Confira Acessibilidade e Monitoramento de Entrada do IOI Desk nos Ajustes do Sistema.")}else if lower.contains("no route to host") || lower.contains("timed out"){self.status("Windows não respondeu. Aguardando a conexão pela rede local.")}else if lower.contains("error") || lower.contains("failed") || lower.contains("fatal"){self.status("Não foi possível conectar. Abra Diagnóstico da conexão.")}
        }}
        process.terminationHandler={[weak self] process in pipe.fileHandleForReading.readabilityHandler=nil;self?.log("process.exit \(process.terminationStatus)");self?.status("Conexão encerrada · código \(process.terminationStatus). Veja o diagnóstico.")}
        do{try process.run();task=process;try Data("true".utf8).write(to:directory.appendingPathComponent("enabled.txt"),options:.atomic);status("Conectando ao Windows…");log("process.start")}
        catch{pipe.fileHandleForReading.readabilityHandler=nil;status(error.localizedDescription);log(error.localizedDescription)}
    }
    func stop(disable:Bool){retry?.invalidate();retry=nil;if disable{try? FileManager.default.removeItem(at:directory.appendingPathComponent("enabled.txt"))};task?.terminationHandler=nil;if task?.isRunning == true{task?.terminate()};task=nil;status("Desconectado.")}
}

