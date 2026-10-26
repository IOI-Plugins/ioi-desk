using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

static class ZoneWorkspace {
    static readonly List<ZoneBoard> boards=new List<ZoneBoard>();
    static ZoneDrag drag;
    public static void Unassign(IntPtr handle){foreach(ZoneBoard board in boards)board.Unassign(handle);}
    public static void ArrangeAll(){foreach(ZoneBoard board in boards)if(!board.IsDisposed)board.Arrange();}
    public static bool ChoosePresentation(Control anchor){foreach(ZoneBoard board in boards)if(board.ViewerSlot>=0){board.PickPresentation(anchor);return true;}return false;}
    public static bool UsedElsewhere(IntPtr handle,ZoneBoard target,int index){foreach(ZoneBoard board in boards)for(int i=0;i<board.SlotCount;i++)if((board!=target||i!=index)&&board.Has(i,handle))return true;return false;}
    public static void Open(Viewer viewer) {
        if(boards.Count>0){foreach(ZoneBoard board in boards)board.Arrange();return;}
        Screen secondary=Native.FindPhysicalSecondary();if(secondary==null)return;
        Screen workerScreen=WorkspaceScreens.Worker;
        Screen presentationScreen=WorkspaceScreens.Presentation;
        List<WindowChoice> apps=WindowCatalog.List();
        ZoneBoard first=new ZoneBoard(viewer,workerScreen,0,new string[]{"Claude AI","Terminal · Claude Code","GPT · Codex · Claude AI","Teams"},new double[]{.24,.25,.345},true);
        ZoneBoard second=new ZoneBoard(viewer,presentationScreen,1,new string[]{"Código / Aplicação · compartilhar","Área de trabalho","Agenda Spread","Documentos · navegador"},new double[]{.35,.25,.20});
        boards.Add(first);boards.Add(second);
        first.Set(0,Find(apps,"Code"));first.Set(1,Find(apps,"WindowsTerminal"));first.Set(2,Find(apps,"ChatGPT"));first.Set(3,Find(apps,"ms-teams"));
        second.ViewerSlot=0;
        // Existing independent browser windows can occupy the other zones.
        foreach(WindowChoice app in apps) {
            if(app.Handle==viewer.AssignedHandle)continue;
            try {using(Process p=Process.GetProcessById(app.Pid)) {
                if(p.ProcessName!="brave"&&p.ProcessName!="chrome"&&p.ProcessName!="msedge")continue;
                Native.WINDOWPLACEMENT place=new Native.WINDOWPLACEMENT();place.length=System.Runtime.InteropServices.Marshal.SizeOf(place);
                if(!Native.GetWindowPlacement(app.Handle,ref place))continue;
                if(Native.IsVirtual(Screen.FromRectangle(Rectangle.FromLTRB(place.rcNormalPosition.Left,place.rcNormalPosition.Top,place.rcNormalPosition.Right,place.rcNormalPosition.Bottom))))continue;
                if(second.Empty(1))second.Set(1,app);else if(second.Empty(2))second.Set(2,app);
            }}catch{}
        }
        WorkspaceProfile.Restore(viewer,boards,apps);
        first.Show();second.Show();first.Arrange();second.Arrange();
        Timer check=new Timer{Interval=1500};int settling=0;check.Tick+=delegate{
            if(viewer.IsDisposed){check.Stop();check.Dispose();return;}
            ArrangeAll();if(++settling<2)return;
            check.Stop();try{File.WriteAllText(Path.Combine(AppPaths.Workspace,"zonas-status.txt"),first.Report()+second.Report());}finally{check.Dispose();}
        };check.Start();
        drag=new ZoneDrag(viewer);
        viewer.FormClosing+=delegate{if(drag!=null){drag.Dispose();drag=null;}WorkspaceProfile.Save(viewer,boards);foreach(ZoneBoard board in boards.ToArray()){board.Save();board.Close();}boards.Clear();};
    }
    static WindowChoice Find(List<WindowChoice> apps,string name){foreach(WindowChoice app in apps)try{using(Process p=Process.GetProcessById(app.Pid))if(p.ProcessName.Equals(name,StringComparison.OrdinalIgnoreCase))return app;}catch{}return null;}
    public static IList<ZoneBoard> Boards {get{return boards.AsReadOnly();}}
    public static bool PlacePresentation(Viewer viewer,IList<ZoneBoard> candidates){foreach(ZoneBoard board in candidates)if(board.OwnerViewer==viewer&&board.ViewerSlot>=0&&!board.IsDisposed){board.Place(board.ViewerSlot);return true;}return false;}
    public static bool PlacePresentation(Viewer viewer){return PlacePresentation(viewer,boards);}
    public static void SaveProfile(Viewer viewer){WorkspaceProfile.Save(viewer,boards);}
    public static void RefreshSelectionNames(){foreach(ZoneBoard board in boards)board.RefreshSelectionNames();}
    public static void RecoverAssigned(IntPtr handle){foreach(ZoneBoard board in boards)for(int i=0;i<board.SlotCount;i++){WindowChoice app=board.GetAssignment(i);if(app!=null&&app.Handle==handle){board.Recover(i);return;}}}
    public static void IncludeAssigned(List<WindowChoice> windows){foreach(ZoneBoard board in boards)for(int i=0;i<board.SlotCount;i++){WindowChoice app=board.GetAssignment(i);if(app==null||windows.Exists(w=>w.Handle==app.Handle))continue;uint pid;Native.GetWindowThreadProcessId(app.Handle,out pid);if(Native.IsWindow(app.Handle)&&pid==(uint)app.Pid)windows.Add(app);}}
    public static void WriteStatus(){string report="";foreach(ZoneBoard board in boards)report+=board.Report();File.WriteAllText(Path.Combine(AppPaths.Workspace,"zonas-status.txt"),report);}
    public static bool Relocate(WindowChoice app,ZoneBoard origin,int index){
        Size minimum=WindowLimits.Minimum(app.Handle);
        foreach(ZoneBoard board in boards)for(int i=0;i<board.SlotCount;i++)if(board.SlotEnabled(i)&&(board!=origin||i!=index)&&i!=board.ViewerSlot&&board.Empty(i)&&WindowLimits.Fits(minimum,board.ContentBounds(i))){origin.Set(index,null);board.Set(i,app);board.Place(i);SaveProfile(viewer:board.OwnerViewer);return true;}
        return false;
    }
}

