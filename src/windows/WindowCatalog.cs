using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

static class WindowCatalog {
    delegate bool Callback(IntPtr window,IntPtr data);
    [DllImport("user32.dll")] static extern bool EnumWindows(Callback callback,IntPtr data);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr window,System.Text.StringBuilder title,int maximum);
    public static List<WindowChoice> List() {
        List<WindowChoice> windows=new List<WindowChoice>();int own=Process.GetCurrentProcess().Id;
        Dictionary<int,string> names=new Dictionary<int,string>();
        EnumWindows(delegate(IntPtr window,IntPtr data) {
            if(!IsWindowVisible(window))return true;
            uint pid;Native.GetWindowThreadProcessId(window,out pid);if(pid==own)return true;
            System.Text.StringBuilder title=new System.Text.StringBuilder(512);GetWindowText(window,title,title.Capacity);
            if(string.IsNullOrWhiteSpace(title.ToString()))return true;
            if(title.ToString().IndexOf("Barra de controle de compartilhamento",StringComparison.OrdinalIgnoreCase)>=0||title.ToString().IndexOf("Screen sharing control bar",StringComparison.OrdinalIgnoreCase)>=0)return true;
            try {
                string name;
                if(!names.TryGetValue((int)pid,out name)){using(Process process=Process.GetProcessById((int)pid))name=process.ProcessName;names[(int)pid]=name;}
                if((name.Equals("IOIDesk",StringComparison.OrdinalIgnoreCase)||name.IndexOf("Camadas",StringComparison.OrdinalIgnoreCase)>=0)||name=="VDD Control"||name=="ApplicationFrameHost"||name=="dwm"||name=="TextInputHost")return true;
                if(title.ToString()=="Program Manager")return true;
                windows.Add(new WindowChoice(window,(int)pid,title.ToString()));
            }catch{}
            return true;
        },IntPtr.Zero);
        windows.Sort(delegate(WindowChoice a,WindowChoice b){int left=Rank(names.ContainsKey(a.Pid)?names[a.Pid]:""),right=Rank(names.ContainsKey(b.Pid)?names[b.Pid]:"");return left!=right?left.CompareTo(right):string.Compare(a.Title,b.Title,StringComparison.CurrentCultureIgnoreCase);});
        return windows;
    }
    static int Rank(string process){if(process=="brave"||process=="chrome"||process=="msedge"||process=="firefox")return 0;if(process=="Code")return 1;if(process=="WindowsTerminal")return 2;if(process=="ms-teams")return 3;return 10;}
}
