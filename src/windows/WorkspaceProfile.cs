using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;

static class WorkspaceProfile {
    static string testPath;
    static string ProfilePath {get{return testPath??Path.Combine(AppPaths.Workspace,"aplicativos.xml");}}
    public static void RunPending(Viewer viewer){string request=Path.Combine(AppPaths.Workspace,"organizar-agora.flag");if(File.Exists(request)){File.Delete(request);Prepare(viewer);}TerminalGrid.Setup(viewer);}
    public static void Reload(Viewer viewer){List<WindowChoice> windows=WindowCatalog.List();ZoneWorkspace.IncludeAssigned(windows);Restore(viewer,ZoneWorkspace.Boards,windows);ZoneWorkspace.ArrangeAll();ZoneWorkspace.RefreshSelectionNames();Save(viewer,ZoneWorkspace.Boards);ZoneWorkspace.WriteStatus();}
    public static void Test(string report){
        testPath=report+".xml";
        try{using(Viewer viewer=new Viewer())using(Form sample=new Form())using(ZoneBoard first=new ZoneBoard(viewer,Screen.PrimaryScreen,0,new string[]{"A","B","C","D"},new double[]{.29,.14,.345},true)){
            WindowChoice window=new WindowChoice(sample.Handle,Process.GetCurrentProcess().Id,"Profile test");
            List<ZoneBoard> boards=new List<ZoneBoard>{first};first.Set(6,window);Save(viewer,boards);first.ClearAssignments();
            Restore(viewer,boards,new List<WindowChoice>{window});if(!first.Has(6,window.Handle))throw new Exception("Subdivision assignment did not survive save/reload.");
            File.WriteAllText(report,"PASS: window-to-zone assignment survives profile save/reload with handle and PID validation.");
        }}catch(Exception ex){File.WriteAllText(report,"FAIL: "+ex);Environment.ExitCode=1;}finally{testPath=null;}
    }
    static string ProcessName(WindowChoice window){try{using(Process p=Process.GetProcessById(window.Pid))return p.ProcessName;}catch{return "";}}
    static WindowChoice Find(List<WindowChoice> windows,string process,string title){foreach(WindowChoice window in windows)if((process==""||ProcessName(window).Equals(process,StringComparison.OrdinalIgnoreCase))&&(title==""||window.Title.IndexOf(title,StringComparison.OrdinalIgnoreCase)>=0))return window;return null;}
    public static void Save(Viewer viewer,IList<ZoneBoard> boards){
        if(boards.Count==0)return;
        try {
            XmlDocument doc=new XmlDocument();XmlElement root=doc.CreateElement("Workspace");doc.AppendChild(root);
            foreach(ZoneBoard board in boards)for(int i=0;i<board.SlotCount;i++){
                if(!board.SlotEnabled(i))continue;
                WindowChoice window=board.GetAssignment(i);if(window==null)continue;
                XmlElement entry=doc.CreateElement("Window");entry.SetAttribute("monitor",board.MonitorNumber.ToString());entry.SetAttribute("zone",i.ToString());entry.SetAttribute("handle",window.Handle.ToInt64().ToString());entry.SetAttribute("pid",window.Pid.ToString());entry.SetAttribute("process",ProcessName(window));entry.SetAttribute("title",window.Title);root.AppendChild(entry);
            }
            doc.Save(ProfilePath+".tmp");File.Copy(ProfilePath+".tmp",ProfilePath,true);File.Delete(ProfilePath+".tmp");
        }catch(Exception ex){TraceLog.Write("profile.save.failed","","error",ex.ToString());}
    }
    public static void Restore(Viewer viewer,IList<ZoneBoard> boards,List<WindowChoice> windows){
        if(!File.Exists(ProfilePath))return;
        try {
            XmlDocument doc=new XmlDocument();doc.XmlResolver=null;doc.Load(ProfilePath);
            HashSet<IntPtr> used=new HashSet<IntPtr>();List<Action> apply=new List<Action>();
            foreach(XmlElement entry in doc.SelectNodes("/Workspace/Window")){
                int monitor,zone,pid;long handle;
                if(!int.TryParse(entry.GetAttribute("monitor"),out monitor)||!int.TryParse(entry.GetAttribute("zone"),out zone)||zone<0)continue;
                int.TryParse(entry.GetAttribute("pid"),out pid);long.TryParse(entry.GetAttribute("handle"),out handle);
                ZoneBoard target=null;foreach(ZoneBoard b in boards)if(b.MonitorNumber==monitor)target=b;if(target==null||zone>=target.SlotCount||!target.SlotEnabled(zone))continue;
                WindowChoice match=windows.Find(w=>w.Handle.ToInt64()==handle&&w.Pid==pid);
                if(match==null)match=windows.Find(w=>ProcessName(w)==entry.GetAttribute("process")&&w.Title==entry.GetAttribute("title"));
                if(match==null||!used.Add(match.Handle))continue;
                WindowChoice selected=match;ZoneBoard board=target;int slot=zone;
                apply.Add(delegate{if(slot==board.ViewerSlot)viewer.AssignWindow(selected);else{viewer.DetachWindow(selected.Handle);board.Set(slot,selected);}});
            }
            if(apply.Count==0)return;foreach(ZoneBoard board in boards)board.ClearAssignments();foreach(Action action in apply)action();
        }catch(Exception ex){TraceLog.Write("profile.restore.failed","","error",ex.ToString());}
    }
    public static void Prepare(Viewer viewer){
        List<WindowChoice> windows=WindowCatalog.List();
        if(Find(windows,"ms-teams","")==null)try{Process.Start(new ProcessStartInfo("ms-teams:"){UseShellExecute=true});}catch{}
        if(Find(windows,"brave","Claude")==null)try{
            Process.Start(new ProcessStartInfo(@"C:\Program Files\BraveSoftware\Brave-Browser\Application\brave.exe","--new-window https://claude.ai/"){UseShellExecute=true});
        }catch{}
        Timer wait=new Timer{Interval=900};int attempts=0;
        wait.Tick+=delegate{
            if(viewer.IsDisposed){wait.Stop();wait.Dispose();return;}
            List<WindowChoice> available=WindowCatalog.List();attempts++;
            if(attempts<15&&(Find(available,"ms-teams","")==null||Find(available,"brave","Claude")==null))return;
            wait.Stop();wait.Dispose();Configure(viewer,available);
        };wait.Start();
    }
    static void Configure(Viewer viewer,List<WindowChoice> windows){
        IList<ZoneBoard> boards=ZoneWorkspace.Boards;if(boards.Count!=2)return;
        ZoneBoard first=boards[0],second=boards[1];first.ClearAssignments();second.ClearAssignments();
        first.Set(0,Find(windows,"Code",""));first.Set(1,Find(windows,"WindowsTerminal",""));first.Set(2,Find(windows,"ChatGPT",""));first.Set(3,Find(windows,"ms-teams",""));
        WindowChoice app=Find(windows,"brave","gabbi");if(app!=null)viewer.AssignWindow(app);
        second.Set(1,Find(windows,"brave","Claude"));
        second.Set(2,Find(windows,"brave","OneDrive")??Find(windows,"brave","SharePoint"));
        second.Set(3,Find(windows,"brave","Processos")??Find(windows,"brave","Manual"));
        ZoneWorkspace.ArrangeAll();Save(viewer,boards);
        File.WriteAllText(Path.Combine(AppPaths.Workspace,"zonas-status.txt"),first.Report()+second.Report());
    }
}
