using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;

// Independent of the UI/capture thread and WH_MOUSE_LL lifetime.
static class InputSafety {
    sealed class Session {public IntPtr Window;public Point Return;public bool WasDown;public int Requested,Token;public long LastPulse;}
    static Session active;
    static readonly object gate=new object();
    static int nextToken;
    static readonly Timer poll=new Timer(Check,null,Timeout.Infinite,20);
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
    static bool Down(int key){return (GetAsyncKeyState(key)&0x8000)!=0;}
    public static bool PrimaryButtonDown {get{return Down(1)||Down(2);}}
    public static bool ExitRequested {get{Session s=Interlocked.CompareExchange(ref active,null,null);return s!=null&&Interlocked.CompareExchange(ref s.Requested,0,0)!=0;}}
    public static int Token {get{Session s=Interlocked.CompareExchange(ref active,null,null);return s==null?0:s.Token;}}
    public static bool AcceptExit(int requested,int current){return requested==0||requested==current;}
    public static bool Expired(long now,long last){return now-last>TimeSpan.FromSeconds(3).Ticks;}
    public static void Pulse(){Session s=Interlocked.CompareExchange(ref active,null,null);if(s!=null)Interlocked.Exchange(ref s.LastPulse,DateTime.UtcNow.Ticks);}
    public static bool Rising(bool previous,bool current){return !previous&&current;}
    public static int Arm(IntPtr window,Point point){
        int token=Interlocked.Increment(ref nextToken);
        lock(gate){Interlocked.Exchange(ref active,new Session{Window=window,Return=point,WasDown=Down(4),LastPulse=DateTime.UtcNow.Ticks,Token=token});
        poll.Change(0,20);
        }TraceLog.Write("control.safety.armed","","pollMs","20","token",token.ToString());return token;
    }
    public static void Clear(){lock(gate){Interlocked.Exchange(ref active,null);poll.Change(Timeout.Infinite,20);}}
    static void Check(object state){
        string source;
        lock(gate){
        Session session=Interlocked.CompareExchange(ref active,null,null);if(session==null)return;
        bool middle=Down(4),emergency=Down(0x11)&&Down(0x12)&&Down(0x77);
        bool stalled=Expired(DateTime.UtcNow.Ticks,Interlocked.Read(ref session.LastPulse));
        bool trigger=Rising(session.WasDown,middle)||emergency||stalled;session.WasDown=middle;
        if(!trigger||Interlocked.CompareExchange(ref session.Requested,1,0)!=0)return;
        if(!object.ReferenceEquals(session,Interlocked.CompareExchange(ref active,null,null)))return;
        // Release the real pointer immediately, even if the UI is currently stalled.
        Native.ClipCursor(IntPtr.Zero);Native.SetCursorPos(session.Return.X,session.Return.Y);
        MouseExitNative.PostMessage(session.Window,0x8002,new IntPtr(stalled?3:emergency?2:1),new IntPtr(session.Token));
        source=stalled?"capture-ui-timeout":emergency?"emergency-key-poll":"middle-button-poll";
        }
        TraceLog.Write("control.safety.exit","","source",source);
    }
    public static bool RawMiddleDown(ushort flags){return (flags&0x0010)!=0;}
    public static void Test(){
        if(!Rising(false,true)||Rising(true,true)||Rising(true,false)||Rising(false,false))throw new Exception("Middle edge detection");
        if(!RawMiddleDown(0x10)||!RawMiddleDown(0x11)||RawMiddleDown(0x20)||RawMiddleDown(0x400))throw new Exception("Raw middle button flags");
    }
}
