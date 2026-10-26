using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

static class Native {
    [DllImport("user32.dll")] public static extern uint GetRawInputData(IntPtr input,uint command,byte[] data,ref uint size,uint headerSize);
    [DllImport("user32.dll")] public static extern bool ReleaseCapture();
    [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr handle,uint message,IntPtr wparam,IntPtr lparam);
    public static bool ListenMouse(IntPtr handle) {MouseNative.DEVICE d=new MouseNative.DEVICE{page=1,usage=2,flags=0x100,target=handle};return MouseNative.RegisterRawInputDevices(new[]{d},1,(uint)Marshal.SizeOf(d));}
    public static void StopMouse() {MouseNative.DEVICE d=new MouseNative.DEVICE{page=1,usage=2,flags=1,target=IntPtr.Zero};MouseNative.RegisterRawInputDevices(new[]{d},1,(uint)Marshal.SizeOf(d));}
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] public struct DISPLAY_DEVICE {
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)] public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)] public string DeviceString;
        public int StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)] public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)] public string DeviceKey;
    }
    [StructLayout(LayoutKind.Sequential)] public struct WINDOWPLACEMENT { public int length,flags,showCmd;public Point ptMinPosition,ptMaxPosition;public RectangleNormal rcNormalPosition; }
    [StructLayout(LayoutKind.Sequential)] public struct RectangleNormal { public int Left,Top,Right,Bottom; }
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern bool EnumDisplayDevices(string device,uint index,ref DISPLAY_DEVICE info,uint flags);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr handle,int id,uint modifiers,uint key);
    [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr handle,int id);
    [DllImport("user32.dll")] public static extern bool ClipCursor(ref RectangleRect rectangle);
    // Rectangle uses x,y,width,height; ClipCursor requires left,top,right,bottom.
    [StructLayout(LayoutKind.Sequential)] public struct RectangleRect {public int Left,Top,Right,Bottom;}
    public static bool ClipCursor(ref Rectangle rectangle) {RectangleRect r=new RectangleRect{Left=rectangle.Left,Top=rectangle.Top,Right=rectangle.Right,Bottom=rectangle.Bottom};return ClipCursor(ref r);}
    [DllImport("user32.dll")] public static extern bool ClipCursor(IntPtr rectangle);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y);
    [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(Point point);
    [DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr handle,uint flags);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr handle);
    [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr handle);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr handle,out uint pid);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr handle,int command);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr handle,IntPtr after,int x,int y,int width,int height,uint flags);
    [DllImport("user32.dll")] public static extern bool GetWindowPlacement(IntPtr handle,ref WINDOWPLACEMENT placement);
    [DllImport("user32.dll")] public static extern bool SetWindowPlacement(IntPtr handle,ref WINDOWPLACEMENT placement);
    public static bool IsVirtual(Screen screen) {
        for(uint i=0;i<64;i++) {
            DISPLAY_DEVICE d=new DISPLAY_DEVICE();d.cb=Marshal.SizeOf(d);
            if(!EnumDisplayDevices(null,i,ref d,0)) break;
            if(d.DeviceName!=screen.DeviceName) continue;
            string name=(d.DeviceString??"").ToLowerInvariant();
            if(name.Contains("virtual")||name.Contains("iddsample")) return true;
            DISPLAY_DEVICE monitor=new DISPLAY_DEVICE();monitor.cb=Marshal.SizeOf(monitor);
            if(EnumDisplayDevices(d.DeviceName,0,ref monitor,0)) {
                name=(monitor.DeviceString??"").ToLowerInvariant();
                return name.Contains("vdd")||name.Contains("virtual");
            }
        }
        return false;
    }
    public static Screen FindVirtual() {
        Screen found=null;
        foreach(Screen s in Screen.AllScreens) if(!s.Primary && IsVirtual(s)) {if(found!=null)return null;found=s;}
        return found;
    }
    public static Screen FindPhysicalSecondary() {foreach(Screen s in Screen.AllScreens)if(!s.Primary&&!IsVirtual(s))return s;return null;}
    public static string Inventory() {
        string output="";
        foreach(Screen s in Screen.AllScreens)output+=s.DeviceName+" "+s.Bounds+" primary="+s.Primary+" virtual="+IsVirtual(s)+Environment.NewLine;
        return output;
    }
}

