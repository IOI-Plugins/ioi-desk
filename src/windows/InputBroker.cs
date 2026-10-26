using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

static class PanelInput {
    public static readonly List<Viewer> Views=new List<Viewer>();
    public static Viewer Active;
    public static Viewer Owner {get{return Views.Count>0?Views[0]:null;}}
    static InputHost host;
    public static bool Ready {get{return host!=null&&host.Ready;}}
    public static bool MiddleReady {get{return host!=null&&host.MiddleReady;}}
    public static bool Join(Viewer view) {
        Views.Add(view);
        if(host==null)host=new InputHost();
        view.SetInputReady(Ready);
        return Ready;
    }
    public static void Activate(Viewer view) {
        if(Active!=null&&Active!=view)Active.ExitFromHub();
        Active=view;Native.ListenMouse(view.Handle);
    }
    public static void Clear(Viewer view){if(Active==view)Active=null;}
    public static void ScheduleTest(Action action){host.ScheduleTest(action);}
    public static void Toggle(bool emergency) {
        if(Active!=null){Active.ExitFromHub();return;}
        if(emergency)return;
        Viewer target=Owner;Point cursor=Cursor.Position;
        foreach(Viewer v in Views)if(v.Bounds.Contains(cursor))target=v;
        if(target!=null)target.EnterFromHub();
    }
    public static void Leave(Viewer view) {
        Clear(view);Views.Remove(view);
        if(Views.Count==0){if(host!=null){host.Dispose();host=null;}Native.StopMouse();}
        else Native.ListenMouse((Active??Views[0]).Handle);
    }
}

class InputHost : NativeWindow,IDisposable {
    public readonly bool Ready;
    public bool MiddleReady {get{return mouseHook!=IntPtr.Zero;}}
    IntPtr mouseHook;
    MouseExitNative.Callback mouseCallback;
    bool reserveUp;
    Action testTick;
    [DllImport("user32.dll")] static extern UIntPtr SetTimer(IntPtr window,UIntPtr id,uint milliseconds,IntPtr callback);
    [DllImport("user32.dll")] static extern bool KillTimer(IntPtr window,UIntPtr id);
    public InputHost() {
        CreateHandle(new CreateParams{Caption="IOI Desk input broker",Parent=new IntPtr(-3)});
        bool first=Native.RegisterHotKey(Handle,1,0x4000,0x77),second=Native.RegisterHotKey(Handle,2,0x4003,0x77);Ready=first&&second;
        mouseCallback=MouseHook;
        mouseHook=MouseExitNative.SetWindowsHookEx(14,mouseCallback,MouseExitNative.GetModuleHandle(null),0);
    }
    public void ScheduleTest(Action action){PanelTests.Trace("schedule begin hwnd="+Handle);testTick=action;UIntPtr id=SetTimer(Handle,new UIntPtr(77),180,IntPtr.Zero);PanelTests.Trace("schedule end id="+id);if(id==UIntPtr.Zero)throw new Exception("test timer");}
    protected override void WndProc(ref Message message){if(message.Msg==0x0312){PanelInput.Toggle(message.WParam.ToInt32()==2);return;}if(message.Msg==0x113&&message.WParam.ToInt32()==77){KillTimer(Handle,new UIntPtr(77));Action action=testTick;testTick=null;if(action!=null)action();return;}base.WndProc(ref message);}
    IntPtr MouseHook(int code,IntPtr message,IntPtr data) {
        if(code>=0) {
            if(MiddleButton.IsDown(message.ToInt32())&&PanelInput.Active!=null){reserveUp=true;MouseExitNative.PostMessage(PanelInput.Active.Handle,0x8002,IntPtr.Zero,IntPtr.Zero);return new IntPtr(1);}
            if(MiddleButton.IsUp(message.ToInt32())&&reserveUp){reserveUp=false;return new IntPtr(1);}
        }
        return MouseExitNative.CallNextHookEx(mouseHook,code,message,data);
    }
    public void Dispose(){if(mouseHook!=IntPtr.Zero)MouseExitNative.UnhookWindowsHookEx(mouseHook);Native.UnregisterHotKey(Handle,1);Native.UnregisterHotKey(Handle,2);DestroyHandle();}
}

