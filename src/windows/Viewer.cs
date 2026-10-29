using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

class Viewer : Form {
    static readonly Color Background=Color.FromArgb(15,20,29), Surface=Color.FromArgb(24,31,43), Muted=Color.FromArgb(153,170,193), Accent=Color.FromArgb(106,166,255), TextColor=Color.FromArgb(229,237,248);
    readonly Panel header=new Panel(), bar=new Panel(), setup=new Panel();
    readonly BufferedPanel canvas=new BufferedPanel();
    readonly Label status=new Label(), sourceLabel=new Label(), speedLabel=new Label();
    readonly ComboBox browsers=new ComboBox();
    readonly Button sessionTab=new Button(), setupTab=new Button(), control=new Button(), actual=new Button(), fit=new Button(), comfort=new Button();
    readonly TrackBar speed=new TrackBar();
    readonly Timer timer=new Timer();
    readonly List<SavedWindow> saved=new List<SavedWindow>();
    readonly Timer chromeTimer=new Timer();
    readonly ToolTip iconTips=new ToolTip();
    Label panelTitle;
    WindowChoice assignedWindow;
    int idleTicks;
    bool pinChrome;
    bool resourcesDisposed;
    readonly Screen source;
    readonly int sourceTaskbarHeight;
    Rectangle sourceArea;
    DisplayModes.Mode? originalMode;
    readonly byte[] rawBuffer=new byte[96];
    readonly Stopwatch watch=Stopwatch.StartNew();
    Bitmap frame;
    bool controlling,ready,rawReady,failed,pixelView=false;
    bool nativePointerInteraction;
    int inputToken;
    int controlCheckTicks;
    Point returnPoint,focusPoint;
    Rectangle imageRect,viewport,capturedBounds;
    DateTime noticeUntil=DateTime.MinValue;
    int ticks,frames;
    long captureMs;
    public Viewer(Screen assigned=null) {
        Text="IOI Desk — apresentação"; FormBorderStyle=FormBorderStyle.None;
        BackColor=Background;ForeColor=TextColor;Font=new Font("Segoe UI",9.5f);
        Padding=new Padding(1);StartPosition=FormStartPosition.Manual;MinimumSize=new Size(480,240);TopMost=false;DoubleBuffered=true;
        List<Screen> availableScreens=PanelWindows.VirtualScreens();
        source=assigned??(availableScreens.Count>0?availableScreens[0]:null);
        if(source!=null){sourceArea=source.Bounds;sourceTaskbarHeight=Math.Max(0,Math.Min(96,source.Bounds.Height-source.WorkingArea.Height));}
        Screen host=WorkspaceScreens.Presentation;Rectangle area=host.WorkingArea;
        Bounds=new Rectangle(area.X+8,area.Y+8,area.Width-16,Math.Min(PreferredHeight(area.Width-16),area.Height-16));
        if(source!=null) focusPoint=sourceArea.Location;
        header.Dock=DockStyle.Top;header.Height=46;header.Width=ClientSize.Width;header.BackColor=Background;
        Label brand=new Label {Text="IOI Desk",Font=new Font("Segoe UI Semibold",14),ForeColor=TextColor,Left=17,Top=10,Width=114,Height=30};
        header.Controls.Add(brand);DragSurface(brand);
        Style(sessionTab,"Navegador",false);sessionTab.SetBounds(135,8,151,34);
        Style(setupTab,"Ambiente",false);setupTab.SetBounds(291,8,122,34);
        header.Controls.Add(sessionTab);header.Controls.Add(setupTab);
        Button minimize=new Button(),close=new Button();Style(minimize,"—",false);Style(close,"×",false);
        minimize.SetBounds(Width-89,8,36,30);close.SetBounds(Width-48,8,36,30);
        minimize.Anchor=close.Anchor=AnchorStyles.Top|AnchorStyles.Right;
        minimize.Click+=delegate{ReleaseControl();WindowState=FormWindowState.Minimized;};close.Click+=delegate{Close();};
        header.Controls.Add(minimize);header.Controls.Add(close);
        bar.Dock=DockStyle.Top;bar.Height=46;bar.Width=ClientSize.Width;bar.BackColor=Surface;
        sourceLabel.SetBounds(16,13,355,25);sourceLabel.ForeColor=Muted;
        UpdateSourceLabel();
        Style(comfort,"Conforto",false);comfort.SetBounds(Width-434,8,90,30);comfort.Anchor=AnchorStyles.Top|AnchorStyles.Right;
        Style(actual,"100%",false);Style(fit,"Ajustar",false);Style(control,"Entrar · F8",true);
        actual.SetBounds(Width-336,8,74,30);fit.SetBounds(Width-254,8,79,30);control.SetBounds(Width-167,8,150,30);
        actual.Anchor=fit.Anchor=control.Anchor=AnchorStyles.Top|AnchorStyles.Right;
        bar.Controls.Add(sourceLabel);bar.Controls.Add(comfort);bar.Controls.Add(actual);bar.Controls.Add(fit);bar.Controls.Add(control);
        status.Dock=DockStyle.Bottom;status.Height=30;status.Padding=new Padding(14,6,0,0);status.ForeColor=Muted;status.BackColor=Surface;
        canvas.Dock=DockStyle.Fill;canvas.BackColor=Color.FromArgb(10,14,20);canvas.Paint+=DrawPreview;
        canvas.MouseDown+=delegate(object s,MouseEventArgs e){if(e.Button==MouseButtons.Left&&imageRect.Contains(e.Location)) EnterControl(e.Location);};
        setup.Dock=DockStyle.Fill;setup.BackColor=Background;BuildSetup();setup.Visible=false;
        Controls.Add(canvas);Controls.Add(setup);Controls.Add(status);Controls.Add(bar);Controls.Add(header);
        BuildCompactChrome();
        sessionTab.Click+=delegate{ShowSetup(false);};setupTab.Click+=delegate{ShowSetup(true);};
        actual.Click+=delegate{SetView(true);};fit.Click+=delegate{SetView(false);};
        comfort.Click+=delegate{ApplyProfile(1366,768);};
        control.Click+=delegate{if(controlling)ReleaseControl();else EnterControl(new Point(imageRect.X+imageRect.Width/2,imageRect.Y+imageRect.Height/2));};
        timer.Interval=33;timer.Tick+=CaptureFrame;
        Shown+=delegate {
            ready=PanelInput.Join(this);rawReady=Native.ListenMouse(Handle);
            if(!ready)Notice("F8 está em uso por outro aplicativo. Feche a versão anterior para habilitar o controle.",60);
            RefreshBrowsers();SetView(false);ShowSetup(false);timer.Start();
        };
        FormClosing+=delegate(object sender,FormClosingEventArgs e){
            ReleaseControl();RestoreResolution();RestoreWindows();
            PanelInput.Leave(this);
        };
        FormClosed+=delegate{CleanupResources();};
        Microsoft.Win32.SystemEvents.SessionSwitch+=SessionChanged;
    }
    void Style(Button b,string text,bool primary) {
        b.Text=text;b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderSize=0;b.FlatAppearance.MouseOverBackColor=Color.FromArgb(49,65,88);
        b.BackColor=primary?Accent:Surface;b.ForeColor=primary?Background:TextColor;b.Cursor=Cursors.Hand;b.Font=new Font("Segoe UI",9.5f);
    }
    Label Description(string text,int x,int y,int width,int height) {return new Label {Text=text,Left=x,Top=y,Width=width,Height=height,ForeColor=Muted};}
    void BuildSetup() {
        setup.Controls.Add(new Label {Text="Aplicativos e ajustes",Left=28,Top=25,Width=600,Height=38,Font=new Font("Segoe UI Semibold",19)});
        setup.Controls.Add(Description("Escolha uma janela aberta para colocar nesta tela.",29,69,900,28));
        setup.Controls.Add(Description("APLICATIVO",29,113,300,22));
        browsers.SetBounds(29,141,630,30);browsers.DropDownStyle=ComboBoxStyle.DropDownList;
        browsers.BackColor=Surface;browsers.ForeColor=TextColor;browsers.FlatStyle=FlatStyle.Flat;
        browsers.DrawMode=DrawMode.OwnerDrawFixed;browsers.ItemHeight=24;
        browsers.DrawItem+=delegate(object s,DrawItemEventArgs e){e.DrawBackground();if(e.Index>=0)TextRenderer.DrawText(e.Graphics,browsers.Items[e.Index].ToString(),Font,e.Bounds,TextColor,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);};
        setup.Controls.Add(browsers);
        Button refresh=new Button(),apply=new Button(),restore=new Button();Style(refresh,"Atualizar lista",false);Style(apply,"Enviar para esta tela",true);Style(restore,"Restaurar posições",false);
        refresh.SetBounds(672,141,145,30);apply.SetBounds(29,187,183,37);restore.SetBounds(226,187,175,37);
        refresh.Click+=delegate{RefreshBrowsers();};apply.Click+=delegate{AssignSelected();};restore.Click+=delegate{ReleaseControl();RestoreWindows();Notice("Posições anteriores restauradas.",7);};
        setup.Controls.Add(refresh);setup.Controls.Add(apply);setup.Controls.Add(restore);
        setup.Controls.Add(Description("VELOCIDADE DO MOUSE",29,294,400,22));
        speed.SetBounds(23,322,330,40);speed.Minimum=5;speed.Maximum=25;speed.Value=12;speed.TickStyle=TickStyle.None;
        speed.SmallChange=1;speed.LargeChange=2;speed.BackColor=Background;
        speedLabel.SetBounds(370,324,160,28);speedLabel.ForeColor=TextColor;
        speed.ValueChanged+=delegate{speedLabel.Text=(speed.Value/10.0).ToString("0.0")+"×";};speedLabel.Text="1,2×";
        setup.Controls.Add(speed);setup.Controls.Add(speedLabel);
        setup.Controls.Add(Description("PERFIL DA TELA VIRTUAL",29,389,500,22));
        Button balanced=new Button(),legible=new Button(),desktop=new Button();
        Style(balanced,"Conforto · 1366 × 768",true);Style(legible,"Mais legível · 800 × 600",false);Style(desktop,"Desktop · 1920 × 1080",false);
        balanced.SetBounds(29,420,230,36);legible.SetBounds(271,420,237,36);desktop.SetBounds(520,420,237,36);
        balanced.Click+=delegate{ApplyProfile(1366,768);};legible.Click+=delegate{ApplyProfile(800,600);};desktop.Click+=delegate{ApplyProfile(1920,1080);};
        setup.Controls.Add(balanced);setup.Controls.Add(legible);setup.Controls.Add(desktop);
        setup.Controls.Add(Description("Os perfis mostram a tela inteira, sem deslocar o recorte. A resolução muda somente na tela virtual.\r\nClique no botão central do mouse para sair. F8 e Ctrl+Alt+F8 continuam disponíveis.",29,477,950,66));
        setup.AutoScroll=true;
    }
    void DragSurface(Control c) {c.MouseDown+=delegate(object s,MouseEventArgs e){if(e.Button==MouseButtons.Left&&!controlling){Native.ReleaseCapture();Native.SendMessage(Handle,0xA1,new IntPtr(2),IntPtr.Zero);}};}
    void ShowSetup(bool value) {
        ReleaseControl();setup.Visible=value;canvas.Visible=!value;bar.Visible=false;header.Visible=value;header.Height=value?32:0;status.Visible=false;
        sessionTab.BackColor=value?Background:Surface;setupTab.BackColor=value?Surface:Background;
        Text=value?"IOI Desk — ambiente":"IOI Desk — navegador";canvas.Invalidate();
        header.BringToFront();
    }
    void SetView(bool pixels) {ReleaseControl();pixelView=pixels;actual.BackColor=pixels?Color.FromArgb(55,76,106):Surface;fit.BackColor=pixels?Surface:Color.FromArgb(55,76,106);canvas.Invalidate();}
    void SessionChanged(object s,Microsoft.Win32.SessionSwitchEventArgs e) {if(!IsDisposed&&IsHandleCreated)BeginInvoke((Action)ReleaseControl);}
    protected override void WndProc(ref Message m) {
        if(m.Msg==0x8002&&!InputSafety.AcceptExit(m.LParam.ToInt32(),inputToken))return;
        if(m.Msg==0x8002){ReleaseControl(m.WParam.ToInt32()==3?"capture-ui-timeout":m.WParam.ToInt32()==2?"emergency-key-poll":m.WParam.ToInt32()==1?"middle-button-poll":"middle-button-hook");Notice("Controle liberado.",5);return;}
        if(m.Msg==0x0312) {PanelInput.Toggle(m.WParam.ToInt32()==2);return;}
        if(m.Msg==0x00FF&&controlling&&rawReady) HandleMouse(m.LParam);
        if(m.Msg==0x84) {
            base.WndProc(ref m);
            if(!controlling&&WindowState==FormWindowState.Normal) {
                Point p=PointToClient(new Point(unchecked((short)(m.LParam.ToInt64()&0xffff)),unchecked((short)((m.LParam.ToInt64()>>16)&0xffff))));
                // Rendered browser pixels, including its first row of tabs,
                // must not become this host window's invisible resize strip.
                if(canvas.Visible&&imageRect.Contains(canvas.PointToClient(PointToScreen(p)))){m.Result=new IntPtr(1);return;}
                const int grip=5;bool left=p.X<grip,right=p.X>=Width-grip,top=p.Y<grip,bottom=p.Y>=Height-grip;
                if(left||right||top||bottom)m.Result=new IntPtr(top?(left?13:right?14:12):bottom?(left?16:right?17:15):left?10:11);
            }
            return;
        }
        base.WndProc(ref m);
    }
    void HandleMouse(IntPtr input) {
        if(InputSafety.ExitRequested)return;
        uint size=(uint)rawBuffer.Length;uint headerSize=(uint)(IntPtr.Size==8?24:16);
        uint bytes=Native.GetRawInputData(input,0x10000003,rawBuffer,ref size,headerSize);
        if(bytes==uint.MaxValue||bytes<headerSize+24||BitConverter.ToUInt32(rawBuffer,0)!=0)return;
        int offset=(int)headerSize;
        ushort buttons=BitConverter.ToUInt16(rawBuffer,offset+4);
        if(InputSafety.RawMiddleDown(buttons)){ReleaseControl("middle-button-raw");return;}
        if((BitConverter.ToUInt16(rawBuffer,offset)&1)!=0)return;
        int dx=BitConverter.ToInt32(rawBuffer,offset+12),dy=BitConverter.ToInt32(rawBuffer,offset+16);
        if(dx==0&&dy==0)return;
        double scale=imageRect.Width>0?(double)viewport.Width/imageRect.Width:1;
        double gain=scale*speed.Value/10.0;
        Point actual=Cursor.Position;
        if(!sourceArea.Contains(actual)){ReleaseControl("pointer-left-source",false);return;}
        bool paused=!PresentationMotion.Compensate(InputSafety.PrimaryButtonDown,Native.NativeInteraction(),buttons);
        if(paused!=nativePointerInteraction){nativePointerInteraction=paused;TraceLog.Write("control.native-interaction","","paused",paused.ToString(),"cursor",actual.ToString(),"foreground",Native.GetForegroundWindow().ToInt64().ToString());}
        if(paused)return;
        Point next=PresentationMotion.Move(actual,dx,dy,gain,sourceArea);
        Native.SetCursorPos(next.X,next.Y);
    }
    void RefreshBrowsers() {
        browsers.Items.Clear();
        foreach(WindowChoice window in WindowCatalog.List())browsers.Items.Add(window);
        if(browsers.Items.Count>0)browsers.SelectedIndex=0;
    }
    void CaptureFrame(object s,EventArgs e) {
        if(controlling)InputSafety.Pulse();
        if(controlling&&++controlCheckTicks%15==0){
            IntPtr foreground=Native.GetForegroundWindow();Native.RectangleRect rect;
            if(foreground!=IntPtr.Zero&&Native.GetWindowRect(foreground,out rect)&&rect.Right>rect.Left&&rect.Bottom>rect.Top&&!sourceArea.IntersectsWith(Rectangle.FromLTRB(rect.Left,rect.Top,rect.Right,rect.Bottom)))
                ReleaseControl("focus-left-presentation");
        }
        if(source==null){control.Enabled=false;Notice("Ative a tela virtual e reabra o IOI Desk.",60);timer.Stop();return;}
        if(!canvas.Visible||WindowState==FormWindowState.Minimized){ReleaseControl();return;}
        if(PanelInput.Active!=null&&PanelInput.Active!=this&&++idleTicks%4!=0)return;
        try {
            if(++ticks%30==0){DisplayModes.Mode current=DisplayModes.Current(source.DeviceName);if(current.width!=sourceArea.Width||current.height!=sourceArea.Height)throw new Exception("A tela virtual mudou. Reabra o IOI Desk.");}
            if(Bounds.IntersectsWith(sourceArea))throw new Exception("Arraste o IOI Desk para um dos monitores físicos.");
            if(controlling&&sourceArea.Contains(Cursor.Position))focusPoint=Cursor.Position;
            Rectangle next=pixelView?ViewMath.Follow(sourceArea,canvas.ClientSize,viewport,focusPoint):sourceArea;
            if(frame==null||frame.Size!=next.Size){if(frame!=null)frame.Dispose();frame=new Bitmap(next.Width,next.Height,PixelFormat.Format32bppRgb);}
            long start=watch.ElapsedMilliseconds;
            using(Graphics g=Graphics.FromImage(frame))g.CopyFromScreen(next.Location,Point.Empty,frame.Size,CopyPixelOperation.SourceCopy);
            capturedBounds=next;viewport=next;
            captureMs+=watch.ElapsedMilliseconds-start;frames++;failed=false;control.Enabled=ready;canvas.Invalidate();
            if(DateTime.UtcNow>=noticeUntil)status.Visible=false;
        }catch(Exception ex){ReleaseControl();if(!failed)TraceLog.Write("capture.failed","","error",ex.Message,"display",SourceDevice);failed=true;control.Enabled=false;Notice(ex.Message,10);canvas.Invalidate();}
    }
    void DrawPreview(object s,PaintEventArgs e) {
        if(frame==null||failed){TextRenderer.DrawText(e.Graphics,"Sua tela virtual aparece aqui",new Font("Segoe UI",15),canvas.ClientRectangle,Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);return;}
        Rectangle available=canvas.ClientRectangle;
        viewport=capturedBounds;
        imageRect=Geometry.Fit(viewport.Size,available);
        e.Graphics.CompositingQuality=CompositingQuality.HighSpeed;
        e.Graphics.InterpolationMode=pixelView?InterpolationMode.NearestNeighbor:InterpolationMode.HighQualityBicubic;
        e.Graphics.PixelOffsetMode=pixelView?PixelOffsetMode.Half:PixelOffsetMode.HighQuality;
        e.Graphics.DrawImage(frame,imageRect,new Rectangle(Point.Empty,frame.Size),GraphicsUnit.Pixel);
        using(Pen border=new Pen(controlling?Color.FromArgb(71,194,145):Color.FromArgb(45,58,77)))e.Graphics.DrawRectangle(border,Rectangle.Inflate(imageRect,1,1));
        if(controlling&&viewport.Contains(Cursor.Position)) {
            Point p=Geometry.ToPreview(Cursor.Position,imageRect,viewport);
            Point[] arrow={p,new Point(p.X+2,p.Y+17),new Point(p.X+7,p.Y+12),new Point(p.X+12,p.Y+12)};
            e.Graphics.FillPolygon(Brushes.White,arrow);e.Graphics.DrawPolygon(Pens.Black,arrow);
        }
    }
    void EnterControl(Point p) {
        if(!ready||failed||source==null||imageRect.IsEmpty||controlling||!canvas.Visible)return;
        returnPoint=canvas.PointToScreen(p);Point destination=Geometry.ToSource(p,imageRect,viewport);
        IntPtr target=Native.GetAncestor(Native.WindowFromPoint(destination),2);uint pid;Native.GetWindowThreadProcessId(target,out pid);
        if(target==IntPtr.Zero||pid==(uint)Process.GetCurrentProcess().Id){Notice("Em Ambiente, escolha o navegador e clique em Aplicar ambiente.",10);return;}
        Native.ReleaseCapture();canvas.Capture=false;
        PanelInput.Activate(this);rawReady=Native.ListenMouse(Handle);
        if(!Native.SetForegroundWindow(target)){PanelInput.Clear(this);Notice("O Windows não liberou o foco. Escolha o aplicativo e tente outra vez.",8);return;}
        Rectangle bounds=sourceArea;
        if(!Native.ClipCursor(ref bounds)){PanelInput.Clear(this);Notice("Não foi possível entrar no controle.",7);return;}
        focusPoint=destination;
        controlling=true;noticeUntil=DateTime.MinValue;status.Visible=false;header.Visible=false;header.Height=0;Native.SetCursorPos(destination.X,destination.Y);control.Text="Sair · F8";
        Rectangle host=Screen.FromPoint(returnPoint).WorkingArea;
        inputToken=InputSafety.Arm(Handle,new Point(Math.Max(host.Left,Math.Min(host.Right-1,returnPoint.X)),Math.Max(host.Top,Math.Min(host.Bottom-1,returnPoint.Y))));
        TraceLog.Write("control.entered","","sourceDisplay",SourceDevice,"assignedTitle",assignedWindow==null?"":assignedWindow.Title);
    }
    void ReleaseControl(){ReleaseControl("application");}
    void ReleaseControl(string reason){ReleaseControl(reason,true);}
    void ReleaseControl(string reason,bool restorePointer) {
        if(!controlling)return;controlling=false;PanelInput.Clear(this);Native.ClipCursor(IntPtr.Zero);
        InputSafety.Clear();TraceLog.Write("control.exited","","source",reason);
        Rectangle host=Screen.FromPoint(returnPoint).WorkingArea;
        if(restorePointer)Native.SetCursorPos(Math.Max(host.Left,Math.Min(host.Right-1,returnPoint.X)),Math.Max(host.Top,Math.Min(host.Bottom-1,returnPoint.Y)));control.Text="Entrar · F8";
    }
    public void MoveWindow(IntPtr handle,Rectangle bounds) {
        if(handle==IntPtr.Zero||!Native.IsWindow(handle))return;
        if(!saved.Exists(w=>w.Handle==handle)){Native.WINDOWPLACEMENT p=new Native.WINDOWPLACEMENT();p.length=Marshal.SizeOf(p);uint pid;Native.GetWindowThreadProcessId(handle,out pid);if(Native.GetWindowPlacement(handle,ref p))saved.Add(new SavedWindow{Handle=handle,Pid=pid,Placement=p});}
        Native.WINDOWPLACEMENT current=new Native.WINDOWPLACEMENT();current.length=Marshal.SizeOf(current);
        if(Native.GetWindowPlacement(handle,ref current)&&(current.showCmd==2||current.showCmd==3||current.showCmd==6||current.showCmd==7)){
            current.showCmd=1;current.flags=0;current.rcNormalPosition=new Native.RectangleNormal{Left=bounds.Left,Top=bounds.Top,Right=bounds.Right,Bottom=bounds.Bottom};
            Native.SetWindowPlacement(handle,ref current);
        }
        if(!Native.SetWindowPos(handle,IntPtr.Zero,bounds.X,bounds.Y,bounds.Width,bounds.Height,0x0014))throw new Exception("O Windows recusou a posição de uma janela.");
    }
    void ApplyLayout() {
        ReleaseControl();AssignSelected();
        if(ZoneWorkspace.Boards.Count>0)ZoneWorkspace.ArrangeAll();
        else if(WorkspaceContext.Current!=null)WorkspaceContext.Current.Tile();
    }
    public void StartEnvironment(){ApplyProfile(1366,768);AssignSelected();}
    public void StartZones(){StartEnvironment();ZoneWorkspace.Open(this);}
    public void PreviewSetup(){ShowSetup(true);}
    public void ShowApplication(){ShowSetup(false);}
    public void OpenPreferences(){ReleaseControl();ShowSetup(true);if(WindowState==FormWindowState.Minimized)WindowState=FormWindowState.Normal;Show();Activate();}
    public void PreviewComfort(){ApplyProfile(1366,768);}
    Rectangle SourceWorkArea() {
        return new Rectangle(sourceArea.X,sourceArea.Y,sourceArea.Width,Math.Max(200,sourceArea.Height-sourceTaskbarHeight));
    }
    void UpdateSourceLabel() {
        sourceLabel.Text=source==null?"Tela virtual desconectada":"Tela virtual  /  "+sourceArea.Width+" × "+sourceArea.Height;
        comfort.BackColor=sourceArea.Width==1366?Color.FromArgb(55,76,106):Surface;
    }
    void ApplyProfile(int width,int height) {
        ReleaseControl();
        if(source==null||source.Primary||!Native.IsVirtual(source)){Notice("Não encontrei uma tela virtual segura para esse perfil.",10);return;}
        try {
            DisplayModes.Mode previous=DisplayModes.Current(source.DeviceName);
            DisplayModes.Mode mode=DisplayModes.Select(source.DeviceName,width,height);
            if(!originalMode.HasValue)originalMode=previous;
            timer.Stop();
            DisplayModes.Apply(source.DeviceName,mode);
            DisplayModes.Mode current=DisplayModes.Current(source.DeviceName);
            sourceArea=new Rectangle(current.x,current.y,(int)current.width,(int)current.height);
            if(WorkspaceContext.Current!=null)WorkspaceContext.Current.Tile();
            viewport=Rectangle.Empty;capturedBounds=Rectangle.Empty;focusPoint=sourceArea.Location;
            if(frame!=null){frame.Dispose();frame=null;}failed=false;
            SetView(false);UpdateSourceLabel();
            if(assignedWindow!=null) {
                uint pid;Native.GetWindowThreadProcessId(assignedWindow.Handle,out pid);
                if(pid==(uint)assignedWindow.Pid)MoveWindow(assignedWindow.Handle,SourceWorkArea());
            }
            ShowSetup(false);Notice("Perfil aplicado: "+width+" × "+height+". Tela inteira, sem deslocar para os lados.",10);
        }catch(Exception ex){Notice(ex.Message,15);}finally{timer.Start();}
    }
    void RestoreResolution() {
        if(source==null||!originalMode.HasValue)return;
        try{DisplayModes.Mode mode=originalMode.Value;mode.fields=0x00080000|0x00100000|0x00400000;DisplayModes.Apply(source.DeviceName,mode);}
        catch(Exception ex){try{File.WriteAllText(Path.Combine(AppPaths.Workspace,"restauracao.txt"),"Não foi possível restaurar a resolução virtual: "+ex.Message);}catch{}}
        originalMode=null;
    }
    public string SourceDevice {get{return source==null?"":source.DeviceName;}}
    public int PreferredHeight(int width){return sourceArea.Width>0?(int)Math.Round((width-2)*(double)sourceArea.Height/sourceArea.Width)+2:600;}
    public void SetInputReady(bool value){ready=value;}
    public void ExitFromHub(){ReleaseControl();}
    public void ExitForComputerSwitch(){ReleaseControl("computer-switch",false);}
    public void EnterFromHub(){ShowSetup(false);EnterControl(new Point(imageRect.X+imageRect.Width/2,imageRect.Y+imageRect.Height/2));}
    void AssignSelected() {
        AssignWindow(browsers.SelectedItem as WindowChoice);
    }
    public IntPtr AssignedHandle {get{return assignedWindow==null?IntPtr.Zero:assignedWindow.Handle;}}
    public WindowChoice AssignedWindow {get{return assignedWindow;}}
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData){
        if(keyData==(Keys.Control|Keys.Shift|Keys.M)){ReleaseControl();ComputerSharing.Open();return true;}
        if(keyData==(Keys.Control|Keys.Shift|Keys.P)){ReleaseControl();ZoneWorkspace.ChoosePresentation(this);return true;}
        if(keyData==(Keys.Control|Keys.Shift|Keys.R)){ReleaseControl();WorkspaceProfile.Reload(this);return true;}
        return base.ProcessCmdKey(ref msg,keyData);
    }
    public Rectangle PresentationBounds {get{return SourceWorkArea();}}
    public void BeginWindowChoice(){ReleaseControl();timer.Stop();}
    public void EndWindowChoice(){if(!IsDisposed&&!Disposing)timer.Start();}
    public void DetachWindow(IntPtr handle){if(AssignedHandle==handle){ReleaseControl();assignedWindow=null;panelTitle.Text="Apresentação · escolha uma janela";}}
    public bool AssignWindow(WindowChoice window) {
        ReleaseControl();
        if(window==null||source==null){Notice("Escolha um aplicativo aberto.",8);return false;}
        uint pid;Native.GetWindowThreadProcessId(window.Handle,out pid);
        if(pid!=(uint)window.Pid){RefreshBrowsers();Notice("A janela mudou. Atualize a escolha.",8);return false;}
        try {
            ZoneWorkspace.Unassign(window.Handle);
            MoveWindow(window.Handle,SourceWorkArea());assignedWindow=window;
            Native.SetWindowPos(window.Handle,IntPtr.Zero,0,0,0,0,0x0053);
            panelTitle.Text=window.Title;Text="IOI Desk — "+window.Title;
            ShowSetup(false);ZoneWorkspace.PlacePresentation(this);status.Visible=false;return true;
        }catch(Exception ex){Notice(ex.Message,10);return false;}
    }
    void BuildCompactChrome() {
        header.Controls.Clear();header.Dock=DockStyle.None;header.SetBounds(1,1,ClientSize.Width-2,4);
        header.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;header.BackColor=Surface;
        panelTitle=new Label{Text="Tela virtual",Left=11,Top=7,Width=Math.Max(140,Width-250),Height=21,ForeColor=TextColor,AutoEllipsis=true,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right};
        header.Controls.Add(panelTitle);DragSurface(panelTitle);DragSurface(header);
        string[] kinds={"plus","apps","settings","tile","more","min","close"};
        string[] names={"Adicionar outra tela","Escolher aplicativo","Ajustes","Encaixar painéis","Mais opções","Minimizar","Fechar painel"};
        for(int i=0;i<kinds.Length;i++) {
            string kind=kinds[i];ChromeIcon icon=new ChromeIcon(kind,names[i]);icon.Location=new Point(header.Width-215+i*30,3);icon.Anchor=AnchorStyles.Top|AnchorStyles.Right;iconTips.SetToolTip(icon,names[i]);
            icon.Click+=delegate {
                if(kind=="plus"){ReleaseControl();if(WorkspaceContext.Current!=null)WorkspaceContext.Current.NewPanel(this);}
                else if(kind=="apps"){ReleaseControl();if(!ZoneWorkspace.ChoosePresentation(icon)){RefreshBrowsers();ShowSetup(true);browsers.Focus();}}
                else if(kind=="settings")ShowSetup(true);
                else if(kind=="tile"){if(WorkspaceContext.Current!=null)WorkspaceContext.Current.Tile();}
                else if(kind=="more")ShowMore(icon);
                else if(kind=="min"){ReleaseControl();WindowState=FormWindowState.Minimized;}
                else if(kind=="close")Close();
            };
            header.Controls.Add(icon);
        }
        foreach(Control c in setup.Controls)c.Top+=32;
        bar.Visible=false;status.Visible=false;header.BringToFront();
        chromeTimer.Interval=150;
        chromeTimer.Tick+=delegate {
            if(setup.Visible)header.BringToFront();
            if(DateTime.UtcNow>=noticeUntil)status.Visible=false;
            // Application pixels never share a hover target with our controls.
            header.Visible=setup.Visible;header.Height=setup.Visible?32:0;
        };
        chromeTimer.Start();
        Resize+=delegate{if(!Disposing&&!IsDisposed&&!header.IsDisposed)header.Width=ClientSize.Width-2;};
    }
    void ShowMore(Control anchor) {
        ContextMenuStrip menu=new ContextMenuStrip();
        menu.Items.Add("Organizar meus aplicativos",null,delegate{WorkspaceProfile.Prepare(this);});
        menu.Items.Add("Organizar oito áreas",null,delegate{ZoneWorkspace.Open(this);});
        menu.Items.Add("Tela inteira",null,delegate{SetView(false);});
        menu.Items.Add("Detalhe 100%",null,delegate{SetView(true);});
        ToolStripMenuItem pinned=new ToolStripMenuItem("Fixar controles"){Checked=pinChrome,CheckOnClick=true};pinned.CheckedChanged+=delegate{pinChrome=pinned.Checked;};menu.Items.Add(pinned);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Conforto · 1366 × 768",null,delegate{ApplyProfile(1366,768);});
        menu.Items.Add("Desktop · 1920 × 1080",null,delegate{ApplyProfile(1920,1080);});
        menu.Items.Add("Mais legível · 800 × 600",null,delegate{ApplyProfile(800,600);});
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Como funciona",null,delegate{MessageBox.Show(this,"Clique na imagem para controlar. Botão central ou F8 para sair.\r\n\r\nCada painel mostra uma tela do mesmo Windows. Os aplicativos podem ser abertos e movidos para ela.\r\n\r\nO + abre outra tela virtual disponível. Para criar telas extras, use o controlador do driver.","IOI Desk",MessageBoxButtons.OK,MessageBoxIcon.Information);});
        menu.Closed+=delegate{if(IsHandleCreated&&!IsDisposed)BeginInvoke((Action)delegate{menu.Dispose();});};menu.Show(anchor,new Point(0,anchor.Height));
    }
    public void PreviewChrome(){pinChrome=true;header.Height=32;}
    void RestoreWindows(){foreach(SavedWindow w in saved){uint pid;Native.GetWindowThreadProcessId(w.Handle,out pid);if(pid==w.Pid&&Native.IsWindow(w.Handle)){Native.WINDOWPLACEMENT p=w.Placement;Native.SetWindowPlacement(w.Handle,ref p);}}saved.Clear();}
    void Notice(string text,int seconds){status.Text=text;status.Visible=true;noticeUntil=DateTime.UtcNow.AddSeconds(Math.Min(4,seconds));}
    public string Diagnostic(){return "version=4 apps="+browsers.Items.Count+" hotkeys="+ready+" rawMouse="+rawReady+" middleExit="+PanelInput.MiddleReady+" nativePixels="+pixelView+" frame="+(frame!=null)+" source="+sourceArea+" header="+header.Bounds+" headerVisible="+header.Visible+" headerZ="+Controls.GetChildIndex(header)+" meanCaptureMs="+(frames>0?(double)captureMs/frames:0).ToString("0.0")+Environment.NewLine+Native.Inventory();}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using(Pen p=new Pen(Color.FromArgb(44,58,78)))e.Graphics.DrawRectangle(p,0,0,ClientSize.Width-1,ClientSize.Height-1);}
    void CleanupResources(){if(resourcesDisposed)return;resourcesDisposed=true;timer.Stop();timer.Dispose();chromeTimer.Stop();chromeTimer.Dispose();iconTips.Dispose();Microsoft.Win32.SystemEvents.SessionSwitch-=SessionChanged;if(frame!=null){frame.Dispose();frame=null;}}
    protected override void Dispose(bool disposing){if(disposing)CleanupResources();base.Dispose(disposing);}
}

