using System;
using System.IO;

static class AppPaths {
    public static string Root {get{return Environment.GetEnvironmentVariable("IOI_DESK_DATA_ROOT")??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),Product.Name);}}
    public static string Workspace {get{return Path.Combine(Root,"Workspace");}}
    public static string Connection {get{return Path.Combine(Root,"Connection");}}
    public static string Logs {get{return Path.Combine(Root,"Logs");}}
    static readonly string[] LegacyFiles={"aplicativos.xml","zonas-0.txt","zonas-1.txt","subzonas-0.txt","subzonas-1.txt","colunas-ia.txt","terminal-matrix.txt","inverter-monitores.flag","organizar-agora.flag"};
    public static void Initialize(){
        string legacy=Environment.GetEnvironmentVariable("IOI_DESK_LEGACY_ROOT")??AppDomain.CurrentDomain.BaseDirectory;
        Migrate(legacy,Workspace);
        // Import the old development installation's driver location once;
        // runtime UI no longer depends on its former workspace structure.
        string configured=Path.Combine(Root,"driver-path.txt"),oldDriver=Path.GetFullPath(Path.Combine(legacy,"..","..","work","virtual-display","app","VDD Control.exe"));
        if(!File.Exists(configured)&&File.Exists(oldDriver))File.WriteAllText(configured,oldDriver);
    }
    internal static void Migrate(string legacy,string destination){
        Directory.CreateDirectory(destination);
        foreach(string name in LegacyFiles){string source=Path.Combine(legacy,name),target=Path.Combine(destination,name);if(File.Exists(source)&&!File.Exists(target))File.Copy(source,target,false);}
    }
    public static void Test(string report){
        string temp=Path.Combine(Path.GetTempPath(),"IOIDesk-migration-"+Guid.NewGuid().ToString("N")),old=Path.Combine(temp,"old"),target=Path.Combine(temp,"new");
        try{Directory.CreateDirectory(old);File.WriteAllText(Path.Combine(old,"aplicativos.xml"),"original");File.WriteAllText(Path.Combine(old,"inverter-monitores.flag"),"true");File.WriteAllText(Path.Combine(old,"unrelated.txt"),"private");
            Migrate(old,target);File.WriteAllText(Path.Combine(target,"aplicativos.xml"),"changed");Migrate(old,target);
            if(File.ReadAllText(Path.Combine(old,"aplicativos.xml"))!="original"||File.ReadAllText(Path.Combine(target,"aplicativos.xml"))!="changed"||!File.Exists(Path.Combine(target,"inverter-monitores.flag"))||File.Exists(Path.Combine(target,"unrelated.txt")))throw new Exception("Migration violated preservation contract");
            File.WriteAllText(report,"PASS: migration copies supported settings, preserves originals and current edits, and excludes unrelated files.");
        }finally{string verified=Path.GetFullPath(temp),parent=Path.GetFullPath(Path.GetTempPath());if(verified.StartsWith(parent,StringComparison.OrdinalIgnoreCase)&&Path.GetFileName(verified).StartsWith("IOIDesk-migration-",StringComparison.Ordinal)&&Directory.Exists(verified))Directory.Delete(verified,true);}
    }
}
