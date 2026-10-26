using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

// Bounded local metadata only: no keystrokes, clipboard, screenshots or document contents.
static class TraceLog {
    public const long FileLimit=1024*1024;
    public const int FileCount=8;
    static readonly object gate=new object();
    static readonly string session=Guid.NewGuid().ToString("N");
    public static string Folder=AppPaths.Logs;
    public static string Operation(){return Guid.NewGuid().ToString("N").Substring(0,12);}
    public static string Quote(string value){
        StringBuilder b=new StringBuilder("\"");foreach(char c in (value??"")){if(b.Length>1900)break;if(c=='"'||c=='\\')b.Append('\\').Append(c);else if(c<32)b.Append("\\u").Append(((int)c).ToString("x4"));else b.Append(c);}return b.Append('"').ToString();
    }
    public static void Write(string name,string operation,params string[] fields){
        try{lock(gate){
            Directory.CreateDirectory(Folder);Prune(Folder);
            string path=Path.Combine(Folder,"events.jsonl");
            if(File.Exists(path)&&new FileInfo(path).Length>=FileLimit){
                string oldest=Path.Combine(Folder,"events.7.jsonl");if(File.Exists(oldest))File.Delete(oldest);
                for(int i=6;i>=1;i--){string source=Path.Combine(Folder,"events."+i+".jsonl");if(File.Exists(source))File.Move(source,Path.Combine(Folder,"events."+(i+1)+".jsonl"));}
                File.Move(path,Path.Combine(Folder,"events.1.jsonl"));
            }
            StringBuilder line=new StringBuilder("{\"time\":").Append(Quote(DateTime.UtcNow.ToString("o"))).Append(",\"session\":").Append(Quote(session)).Append(",\"event\":").Append(Quote(name)).Append(",\"operation\":").Append(Quote(operation));
            for(int i=0;i+1<fields.Length;i+=2)line.Append(',').Append(Quote(fields[i])).Append(':').Append(Quote(fields[i+1]));
            File.AppendAllText(path,line.Append("}\n").ToString(),new UTF8Encoding(false));
        }}catch{} // Diagnostics must never prevent moving a window, including disk-full errors.
    }
    static void Prune(string folder){foreach(string path in Directory.GetFiles(folder,"events*.jsonl"))if(File.GetLastWriteTimeUtc(path)<DateTime.UtcNow.AddDays(-7))File.Delete(path);}
    public static void Test(string folder){
        string original=Folder;try{Folder=folder;Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"events.jsonl"),new string('x',(int)FileLimit));File.WriteAllText(Path.Combine(folder,"events.6.jsonl"),"expired");File.SetLastWriteTimeUtc(Path.Combine(folder,"events.6.jsonl"),DateTime.UtcNow.AddDays(-8));
        for(int i=0;i<10;i++){File.WriteAllText(Path.Combine(folder,"events.jsonl"),new string('x',(int)FileLimit));Write("test", "test", "text", "quotes\" newline\n");}
        if(Directory.GetFiles(folder,"events*.jsonl").Length>FileCount)throw new Exception("Retention count");
        if(Quote("\"\n\\")!="\"\\\"\\u000a\\\\\"")throw new Exception("JSON escaping");
        }finally{Folder=original;}
    }
}
