using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;

// Independent of the UI/capture thread and WH_MOUSE_LL lifetime.
static class InputSafety {
    sealed class Session {public IntPtr Window;public Point Return;public bool WasDown;public int Requested;}
    static Session active;
    static readonly Timer poll=new Timer(Check,null,Timeout.Infinite,20);
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
    static bool Down(int key){return (GetAsyncKeyState(key)&0x8000)!=0;}
    public static bool Rising(bool previous,bool current){return !previous&&current;}
    public static void Arm(IntPtr window,Point point){
        Interlocked.Exchange(ref active,new Session{Window=window,Return=point,WasDown=Down(4)});
        poll.Change(0,20);TraceLog.Write("control.safety.armed","","pollMs","20");
    }
    public static void Clear(){Interlocked.Exchange(ref active,null);poll.Change(Timeout.Infinite,20);}
    static void Check(object state){
        Session session=Interlocked.CompareExchange(ref active,null,null);if(session==null)return;
        bool middle=Down(4),emergency=Down(0x11)&&Down(0x12)&&Down(0x77);
        bool trigger=Rising(session.WasDown,middle)||emergency;session.WasDown=middle;
        if(!trigger||Interlocked.CompareExchange(ref session.Requested,1,0)!=0)return;
        if(!object.ReferenceEquals(session,Interlocked.CompareExchange(ref active,null,null)))return;
        // Release the real pointer immediately, even if the UI is currently stalled.
        Native.ClipCursor(IntPtr.Zero);Native.SetCursorPos(session.Return.X,session.Return.Y);
        MouseExitNative.PostMessage(session.Window,0x8002,new IntPtr(emergency?2:1),IntPtr.Zero);
        TraceLog.Write("control.safety.exit","","source",emergency?"emergency-key-poll":"middle-button-poll");
    }
    public static bool RawMiddleDown(ushort flags){return (flags&0x0010)!=0;}
    public static void Test(){
        if(!Rising(false,true)||Rising(true,true)||Rising(true,false)||Rising(false,false))throw new Exception("Middle edge detection");
        if(!RawMiddleDown(0x10)||!RawMiddleDown(0x11)||RawMiddleDown(0x20)||RawMiddleDown(0x400))throw new Exception("Raw middle button flags");
    }
}
