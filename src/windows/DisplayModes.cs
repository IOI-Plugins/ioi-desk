using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

static class DisplayModes {
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
    public struct Mode {
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)] public string device;
        public ushort specVersion,driverVersion,size,extra;
        public uint fields;
        public int x,y;
        public uint orientation,fixedOutput;
        public short color,duplex,yResolution,ttOption,collate;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)] public string form;
        public ushort logPixels;
        public uint bits,width,height,flags,frequency,icmMethod,icmIntent,media,dither,reserved1,reserved2,panningWidth,panningHeight;
    }
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern bool EnumDisplaySettings(string device,int index,ref Mode mode);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int ChangeDisplaySettingsEx(string device,ref Mode mode,IntPtr window,uint flags,IntPtr parameter);
    public static Mode Current(string device) {
        Mode mode=new Mode();mode.size=(ushort)Marshal.SizeOf(mode);
        if(!EnumDisplaySettings(device,-1,ref mode))throw new Exception("Não foi possível ler a resolução da tela virtual.");
        return mode;
    }
    public static List<Mode> Available(string device) {
        List<Mode> result=new List<Mode>();
        for(int i=0;i<2048;i++){Mode mode=new Mode();mode.size=(ushort)Marshal.SizeOf(mode);if(!EnumDisplaySettings(device,i,ref mode))break;result.Add(mode);}
        return result;
    }
    public static Mode Select(string device,int width,int height) {
        Mode? best=null;
        foreach(Mode mode in Available(device)) {
            if(mode.width!=width||mode.height!=height||mode.orientation!=0)continue;
            if(!best.HasValue || (mode.frequency<=60 && (best.Value.frequency>60||mode.frequency>best.Value.frequency)))best=mode;
        }
        if(!best.HasValue)throw new Exception("O driver não oferece "+width+" × "+height+". A resolução atual foi mantida.");
        Mode selected=best.Value;
        selected.fields=0x00080000|0x00100000|0x00400000; // Width, height, refresh only; never position or physical display layout.
        return selected;
    }
    public static void Apply(string device,Mode mode) {
        int test=ChangeDisplaySettingsEx(device,ref mode,IntPtr.Zero,2,IntPtr.Zero);
        if(test!=0)throw new Exception("O Windows recusou esse perfil (teste "+test+").");
        int result=ChangeDisplaySettingsEx(device,ref mode,IntPtr.Zero,0,IntPtr.Zero);
        if(result!=0)throw new Exception("Não foi possível aplicar o perfil (código "+result+").");
    }
    public static void MoveVirtualLeft(string report) {
        var target=Native.FindVirtual();if(target==null||!Native.IsVirtual(target))throw new Exception("Tela virtual não encontrada.");
        Mode previous=Current(target.DeviceName);int left=0;
        var physical=new Dictionary<string,Mode>();
        foreach(var screen in System.Windows.Forms.Screen.AllScreens)if(!Native.IsVirtual(screen)){physical[screen.DeviceName]=Current(screen.DeviceName);left=Math.Min(left,screen.Bounds.Left);}
        if(physical.Count==0)throw new Exception("Nenhum monitor físico encontrado.");
        System.IO.File.WriteAllText(report,"device="+target.DeviceName+" original="+previous.x+","+previous.y+" size="+previous.width+"x"+previous.height+Environment.NewLine);
        Mode next=previous;next.x=left-(int)previous.width;next.y=0;next.fields=0x20;
        int test=ChangeDisplaySettingsEx(target.DeviceName,ref next,IntPtr.Zero,2,IntPtr.Zero);if(test!=0)throw new Exception("Posição recusada: "+test);
        int result=ChangeDisplaySettingsEx(target.DeviceName,ref next,IntPtr.Zero,1,IntPtr.Zero);if(result!=0)throw new Exception("Falha ao mover: "+result);
        try{
            Mode actual=Current(target.DeviceName);
            if(actual.x!=next.x||actual.y!=next.y||actual.width!=previous.width||actual.height!=previous.height)throw new Exception("A posição não foi confirmada.");
            foreach(var entry in physical){Mode actualPhysical=Current(entry.Key);Mode old=entry.Value;if(actualPhysical.x!=old.x||actualPhysical.y!=old.y||actualPhysical.width!=old.width||actualPhysical.height!=old.height||actualPhysical.orientation!=old.orientation)throw new Exception("Monitor físico mudou inesperadamente.");}
            System.IO.File.AppendAllText(report,"PASS: virtual="+actual.x+","+actual.y+"; physical positions, resolutions and orientations unchanged.");
        }catch{previous.fields=0x20;ChangeDisplaySettingsEx(target.DeviceName,ref previous,IntPtr.Zero,1,IntPtr.Zero);throw;}
    }
    public static string Describe(string device) {
        Mode current=Current(device);string text="current="+current.width+"x"+current.height+"@"+current.frequency+Environment.NewLine;
        HashSet<string> seen=new HashSet<string>();
        foreach(Mode m in Available(device)){string value=m.width+"x"+m.height+"@"+m.frequency;if(seen.Add(value))text+=value+Environment.NewLine;}
        return text;
    }
    public static void Test() {
        if(Marshal.SizeOf(typeof(Mode))!=220 || Marshal.OffsetOf(typeof(Mode),"width").ToInt32()!=172 || Marshal.OffsetOf(typeof(Mode),"frequency").ToInt32()!=184)throw new Exception("DEVMODE layout");
    }
}

static class MiddleButton {
    public static bool IsDown(int message){return message==0x207;}
    public static bool IsUp(int message){return message==0x208;}
    public static bool IsRawDown(ushort flags){return (flags&0x10)!=0;}
    public static void Test() {
        if(!IsDown(0x207)||IsDown(0x200)||!IsUp(0x208)||!IsRawDown(0x10)||IsRawDown(0x400))throw new Exception("middle button vs wheel mapping");
    }
}

static class MouseExitNative {
    public delegate IntPtr Callback(int code,IntPtr message,IntPtr data);
    [DllImport("user32.dll")] public static extern IntPtr SetWindowsHookEx(int type,Callback callback,IntPtr module,uint thread);
    [DllImport("user32.dll")] public static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] public static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr message,IntPtr data);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr handle,uint message,IntPtr wparam,IntPtr lparam);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] public static extern IntPtr GetModuleHandle(string module);
}
