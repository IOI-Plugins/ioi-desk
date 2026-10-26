using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
static class TerminalGrid {
    public static bool Enabled {get{return File.Exists(Path.Combine(AppPaths.Workspace,"terminal-matrix.txt"));}}
    const string Prefix="IOI Desk Ubuntu Zona ";
    const string LegacyPrefix="Camadas Ubuntu Zona ";
    const string Ubuntu="{b7234269-0a17-5a3f-b91d-ac1fb8233500}";
    static readonly HashSet<int> opening=new HashSet<int>();
    static int Number(int slot){return slot==1?1:slot==4?2:3;}
    public static string ArgumentsFor(int slot){int number=Number(slot);return "--focus --window IOI-Desk-Ubuntu-Zona-"+number+" --size 38,28 new-tab -p \""+Ubuntu+"\" --title \""+Prefix+number+"\" --suppressApplicationTitle";}
    public static string Arguments {get{return ArgumentsFor(1);}}
    static WindowChoice Find(int slot){return WindowCatalog.List().Find(w=>(w.Title.Equals(Prefix+Number(slot),StringComparison.OrdinalIgnoreCase)||w.Title.Equals(LegacyPrefix+Number(slot),StringComparison.OrdinalIgnoreCase)));}
    public static void Setup(Viewer viewer){if(!Enabled||ZoneWorkspace.Boards.Count==0)return;ZoneBoard board=ZoneWorkspace.Boards[0];foreach(int slot in new[]{1,4}){if(board.GetAssignment(slot)!=null)continue;WindowChoice existing=Find(slot);if(existing!=null&&!ZoneWorkspace.UsedElsewhere(existing.Handle,board,slot))Attach(viewer,board,slot,existing,null);}WindowChoice third=Find(5);if(third!=null&&viewer.AssignedHandle!=third.Handle&&!ZoneWorkspace.UsedElsewhere(third.Handle,board,-1))Native.ShowWindow(third.Handle,6);}
    public static void Open(Viewer viewer,int slot){
        if(!Enabled||ZoneWorkspace.Boards.Count==0||opening.Contains(slot))return;
        ZoneBoard board=ZoneWorkspace.Boards[0];if(!board.SlotEnabled(slot))return;WindowChoice previous=board.GetAssignment(slot),existing=Find(slot);
        if(existing!=null){Attach(viewer,board,slot,existing,previous);return;}
        try{opening.Add(slot);Process.Start(new ProcessStartInfo("wt.exe",ArgumentsFor(slot)){UseShellExecute=false});}
        catch(Exception ex){opening.Remove(slot);TraceLog.Write("terminal.open.failed","","zone",slot.ToString(),"error",ex.ToString());return;}
        Timer wait=new Timer{Interval=500};int attempts=0;
        wait.Tick+=delegate{
            if(viewer.IsDisposed){opening.Remove(slot);wait.Stop();wait.Dispose();return;}
            WindowChoice window=Find(slot);if(window==null&&++attempts<30)return;
            opening.Remove(slot);wait.Stop();wait.Dispose();if(window!=null)Attach(viewer,board,slot,window,previous);
        };wait.Start();
    }
    static void Attach(Viewer viewer,ZoneBoard board,int slot,WindowChoice window,WindowChoice previous){
        if(!board.TransferWindow(slot,window))return;
        // Keep previous shells alive and accessible through the taskbar/picker.
        if(previous!=null&&previous.Handle!=window.Handle){uint pid;Native.GetWindowThreadProcessId(previous.Handle,out pid);if(pid==(uint)previous.Pid&&Native.IsWindow(previous.Handle))Native.ShowWindow(previous.Handle,6);}
        ZoneWorkspace.SaveProfile(viewer);ZoneWorkspace.WriteStatus();
    }
    public static void Test(){HashSet<string> commands=new HashSet<string>();foreach(int slot in new[]{1,4}){string text=ArgumentsFor(slot);if(text.Contains("split-pane")||!text.StartsWith("--focus")||!commands.Add(text))throw new Exception("Terminal zones must use independent focused windows");}}
}
