using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// Observe the native move loop; never intercept or suppress the user's mouse.
sealed class ZoneDrag : IDisposable {
    delegate void EventCallback(IntPtr hook,uint kind,IntPtr window,int objectId,int child,uint thread,uint time);
    [DllImport("user32.dll")] static extern IntPtr SetWinEventHook(uint first,uint last,IntPtr module,EventCallback callback,uint process,uint thread,uint flags);
    [DllImport("user32.dll")] static extern bool UnhookWinEvent(IntPtr hook);
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr window,System.Text.StringBuilder text,int capacity);
    readonly Viewer viewer;
    readonly IList<ZoneBoard> boards;
    readonly Func<int,bool> keyDown;
    readonly Func<Point> pointer;
    readonly bool testing;
    readonly EventCallback callback;
    readonly IntPtr hook;
    readonly Timer timer=new Timer{Interval=40};
    readonly List<Hint> hints=new List<Hint>();
    WindowChoice moving;
    Size minimum;
    bool cancelled;
    Hint target;
    public bool Ready {get{return hook!=IntPtr.Zero;}}
    public ZoneDrag(Viewer owner):this(owner,ZoneWorkspace.Boards,Down,delegate{return Cursor.Position;},false){}
    ZoneDrag(Viewer owner,IList<ZoneBoard> targets,Func<int,bool> keys,Func<Point> position,bool test){viewer=owner;boards=targets;keyDown=keys;pointer=position;testing=test;callback=OnEvent;hook=SetWinEventHook(0x000A,0x000B,IntPtr.Zero,callback,0,0,test?0u:2u);timer.Tick+=delegate{Update();};}
    void OnEvent(IntPtr h,uint kind,IntPtr window,int objectId,int child,uint thread,uint time){
        if(window==IntPtr.Zero||objectId!=0||viewer.IsDisposed)return;
        if(kind==0x000A){
            Reset();uint pid;Native.GetWindowThreadProcessId(window,out pid);
            if(pid==0||(!testing&&pid==(uint)Process.GetCurrentProcess().Id)||PanelInput.Active!=null)return;
            System.Text.StringBuilder title=new System.Text.StringBuilder(512);GetWindowText(window,title,title.Capacity);
            if(string.IsNullOrWhiteSpace(title.ToString()))return;
            moving=new WindowChoice(window,(int)pid,title.ToString());minimum=WindowLimits.Minimum(window);timer.Start();Update();
        }else if(kind==0x000B&&moving!=null&&moving.Handle==window){
            Update();Hint destination=target;WindowChoice choice=moving;
            bool apply=!cancelled&&keyDown(0x10)&&destination!=null&&destination.Fits;
            Reset();
            if(apply)destination.Board.BeginInvoke((Action)delegate{if(!destination.Board.IsDisposed){destination.Board.TransferWindow(destination.Index,choice);if(!testing)ZoneWorkspace.WriteStatus();}});
        }
    }
    static bool Down(int key){return (GetAsyncKeyState(key)&0x8000)!=0;}
    void Update(){
        if(moving==null)return;
        if(!Native.IsWindow(moving.Handle)||viewer.IsDisposed){Reset();return;}
        if(keyDown(0x1B))cancelled=true;
        if(cancelled||!keyDown(0x10)){HideHints();return;}
        if(hints.Count==0)foreach(ZoneBoard board in boards)for(int i=0;i<board.SlotCount;i++)if(board.SlotEnabled(i)){
            Hint hint=new Hint(board,i,board.ZoneBounds(i),i==board.ViewerSlot||WindowLimits.Fits(minimum,board.ContentBounds(i)),minimum);hints.Add(hint);hint.Show();
        }
        Point cursor=pointer();target=null;
        foreach(Hint hint in hints){bool selected=hint.Bounds.Contains(cursor);hint.SelectTarget(selected);if(selected)target=hint;}
    }
    void HideHints(){target=null;foreach(Hint hint in hints)hint.Dispose();hints.Clear();}
    void Reset(){timer.Stop();HideHints();moving=null;cancelled=false;}
    public void Dispose(){Reset();timer.Dispose();if(hook!=IntPtr.Zero)UnhookWinEvent(hook);}
    [DllImport("user32.dll")] static extern void NotifyWinEvent(uint kind,IntPtr window,int objectId,int child);
    public static void Test(string report){
        using(Viewer viewer=new Viewer())using(Form sample=new Form{Text="IOI Desk drag test",Size=new Size(220,120)})using(ZoneBoard board=new ZoneBoard(viewer,Screen.PrimaryScreen,91,new[]{"A","B","C","D"},new[]{.25,.25,.25})){
            bool shift=false,escape=false;Point cursor=Point.Empty;int step=0;ZoneDrag drag=null;Timer steps=new Timer{Interval=140};
            Action start=delegate{drag.OnEvent(IntPtr.Zero,0xA,sample.Handle,0,0,0,0);};Action end=delegate{drag.OnEvent(IntPtr.Zero,0xB,sample.Handle,0,0,0,0);};
            board.Shown+=delegate{
                sample.Show();board.Set(0,new WindowChoice(sample.Handle,Process.GetCurrentProcess().Id,sample.Text));board.Arrange();
                Rectangle area=board.ZoneBounds(1);cursor=new Point(area.Left+40,area.Top+50);
                drag=new ZoneDrag(viewer,new List<ZoneBoard>{board},delegate(int key){return key==0x10?shift:key==0x1B&&escape;},delegate{return cursor;},true);
                steps.Start();
            };
            steps.Tick+=delegate{try{
                if(step==0){if(!drag.Ready)throw new Exception("Move hook unavailable");NotifyWinEvent(0xA,sample.Handle,0,0);}
                if(step==1){if(drag.moving==null)throw new Exception("Native start event not received");if(drag.hints.Count!=0)throw new Exception("Hints appeared without Shift");NotifyWinEvent(0xB,sample.Handle,0,0);}
                if(step==2){if(drag.moving!=null)throw new Exception("Native end event not received");if(!board.Has(0,sample.Handle))throw new Exception("Unmodified drag changed assignment");shift=true;start();}
                if(step==3){drag.Update();if(drag.target==null||!drag.target.Fits||drag.hints.Count!=4)throw new Exception("Shift target not detected");end();}
                if(step==4){if(!board.Empty(0)||!board.Has(1,sample.Handle))throw new Exception("Drop did not transfer and release origin");Rectangle area=board.ZoneBounds(2);cursor=new Point(area.Left+40,area.Top+50);start();}
                if(step==5){escape=true;drag.Update();end();}
                if(step==6){if(!board.Has(1,sample.Handle)||!board.Empty(2)||drag.hints.Count!=0)throw new Exception("Escape did not cancel");escape=false;start();drag.HideHints();drag.minimum=new Size(3000,3000);drag.Update();}
                if(step==7){drag.Update();if(drag.target==null||drag.target.Fits)throw new Exception("Oversize target accepted");end();}
                if(step==8){if(!board.Has(1,sample.Handle)||!board.Empty(2))throw new Exception("Rejected drop lost origin");System.IO.File.WriteAllText(report,"PASS: native move start/end events; no-Shift unchanged; Shift highlights and transfers; origin released; Escape cancels; oversized drop preserves origin; overlays removed.");steps.Stop();board.Close();}
                step++;
            }catch(Exception ex){System.IO.File.WriteAllText(report,"FAIL: "+ex);Environment.ExitCode=1;steps.Stop();board.Close();}};
            Application.Run(board);steps.Dispose();if(drag!=null)drag.Dispose();
        }
    }
    sealed class Hint : Form {
        public readonly ZoneBoard Board;public readonly int Index;public readonly bool Fits;
        readonly Size minimum;bool selected;
        public Hint(ZoneBoard board,int index,Rectangle bounds,bool fits,Size required){
            Board=board;Index=index;Fits=fits;minimum=required;
            FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.Manual;Bounds=Rectangle.Inflate(bounds,-4,-4);ShowInTaskbar=false;TopMost=true;BackColor=Color.FromArgb(25,105,185);Opacity=.32;DoubleBuffered=true;
        }
        protected override bool ShowWithoutActivation {get{return true;}}
        protected override CreateParams CreateParams {get{CreateParams p=base.CreateParams;p.ExStyle|=0x080800A0;return p;}}
        protected override void WndProc(ref Message m){if(m.Msg==0x84){m.Result=new IntPtr(-1);return;}base.WndProc(ref m);}
        public void SelectTarget(bool value){if(selected==value)return;selected=value;BackColor=value?(Fits?Color.FromArgb(20,145,100):Color.FromArgb(180,55,45)):Color.FromArgb(25,105,185);Opacity=value?.65:.25;Invalidate();}
        protected override void OnPaint(PaintEventArgs e){
            using(Pen border=new Pen(selected?Color.White:Color.LightSkyBlue,selected?4:2))e.Graphics.DrawRectangle(border,2,2,Width-5,Height-5);
            string text=selected?(Fits?"Solte para encaixar":"Zona pequena · mínimo "+minimum.Width+" × "+minimum.Height):"Shift + arrastar";
            using(Font font=new Font("Segoe UI",12,FontStyle.Bold))TextRenderer.DrawText(e.Graphics,text,font,ClientRectangle,Color.White,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.WordBreak);
        }
    }
}
