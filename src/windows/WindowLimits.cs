using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Drawing;
static class WindowLimits {
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr window,StringBuilder text,int capacity);
    [StructLayout(LayoutKind.Sequential)] struct Point {public int x,y;}
    [StructLayout(LayoutKind.Sequential)] struct Limits {public Point reserved,maxSize,maxPosition,minTrack,maxTrack;}
    [DllImport("user32.dll")] static extern IntPtr SendMessageTimeout(IntPtr window,uint message,IntPtr w,ref Limits limits,uint flags,uint timeout,out IntPtr result);
    public static Size Minimum(IntPtr window){Limits info=new Limits();IntPtr result;return SendMessageTimeout(window,0x24,IntPtr.Zero,ref info,2,150,out result)==IntPtr.Zero?Size.Empty:new Size(Math.Max(0,info.minTrack.x),Math.Max(0,info.minTrack.y));}
    public static bool Fits(Size minimum,Rectangle area){return minimum.Width<=area.Width&&minimum.Height<=area.Height;}
    public static void Report(string path){StringBuilder report=new StringBuilder();foreach(WindowChoice window in WindowCatalog.List()){
        Limits info=new Limits();IntPtr result;if(SendMessageTimeout(window.Handle,0x24,IntPtr.Zero,ref info,2,200,out result)!=IntPtr.Zero)report.AppendLine(window.Title+" | minimum="+info.minTrack.x+"x"+info.minTrack.y+" | handle="+window.Handle.ToInt64()+" | pid="+window.Pid);
    }File.WriteAllText(path,report.ToString());}
}
