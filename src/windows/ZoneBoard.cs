using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

class ZoneBoard : Form {
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr handle,out Native.RectangleNormal rectangle);
    readonly Viewer viewer;
    readonly int number;
    readonly Panel[] slots;
    readonly WindowChoice[] assigned;
    readonly Panel[] rows=new Panel[4];
    readonly Dictionary<string,SplitContainer> innerSplits=new Dictionary<string,SplitContainer>();
    readonly Dictionary<int,Button> headers=new Dictionary<int,Button>();
    readonly ToolTip headerTips=new ToolTip();
    readonly Timer visibilityTimer=new Timer{Interval=500};
    bool parking;
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool IsIconic(IntPtr window);
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr window);
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern IntPtr GetTopWindow(IntPtr window);
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr window,uint command);
    string[] zoneNames;
    bool choosingWindow;
    readonly bool terminalMatrix;
    readonly bool twoTerminalColumns;
    readonly SplitContainer[] splits=new SplitContainer[3];
    readonly double[] initial;
    bool arranging,loaded;
    public int ViewerSlot=-1;
    string ConfigPath {get{return Path.Combine(AppPaths.Workspace,"zonas-"+number+".txt");}}
    protected override bool ShowWithoutActivation {get{return true;}}
    protected override CreateParams CreateParams {get{CreateParams p=base.CreateParams;p.ExStyle|=0x08000000;return p;}}
    protected override void WndProc(ref Message message){
        // A click on a divider/header must never activate the monitor-sized board.
        if(message.Msg==0x21){message.Result=new IntPtr(3);return;}
        base.WndProc(ref message);
        if(message.Msg==0x47||message.Msg==0x06)KeepBehindAssigned();
    }
    public void KeepBehindAssigned(){
        if(parking||!loaded||IsDisposed||Disposing||!IsHandleCreated||viewer.IsDisposed||viewer.Disposing)return;
        HashSet<IntPtr> handles=new HashSet<IntPtr>();
        for(int i=0;i<SlotCount;i++)if(SlotEnabled(i)){
            IntPtr handle=i==ViewerSlot?(viewer.IsHandleCreated?viewer.Handle:IntPtr.Zero):(assigned[i]==null?IntPtr.Zero:assigned[i].Handle);
            if(handle!=IntPtr.Zero&&Native.IsWindow(handle)&&IsWindowVisible(handle)&&!IsIconic(handle))handles.Add(handle);
        }
        bool sawBoard=false,needsLowering=false;IntPtr last=IntPtr.Zero;int limit=0;
        for(IntPtr window=GetTopWindow(IntPtr.Zero);window!=IntPtr.Zero&&limit++<4096;window=GetWindow(window,2)){
            if(window==Handle)sawBoard=true;
            if(handles.Remove(window)){last=window;if(sawBoard)needsLowering=true;if(handles.Count==0)break;}
        }
        if(!needsLowering||last==IntPtr.Zero)return;
        // The board is background chrome, not part of another app's owner group.
        // Relative insertion can reorder owned windows and cover another zone.
        parking=true;try{Native.SetWindowPos(Handle,new IntPtr(1),0,0,0,0,0x0213);}finally{parking=false;}
    }
    public ZoneBoard(Viewer owner,Screen screen,int index,string[] names,double[] fractions,bool subdivided=false) {
        viewer=owner;number=index;initial=fractions;Text="IOI Desk · áreas "+(index+1);
        terminalMatrix=false;
        twoTerminalColumns=subdivided;
        int aiColumns=2,aiParsed;string aiConfig=Path.Combine(AppPaths.Workspace,"colunas-ia.txt");
        if(subdivided&&File.Exists(aiConfig)&&int.TryParse(File.ReadAllText(aiConfig).Trim(),out aiParsed)&&aiParsed>=1&&aiParsed<=3)aiColumns=aiParsed;
        bool agendas=!subdivided&&index==1;
        slots=new Panel[subdivided?6+aiColumns:agendas?5:4];assigned=new WindowChoice[slots.Length];
        FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.Manual;Bounds=screen.WorkingArea;
        BackColor=Color.FromArgb(30,40,54);Font=new Font("Segoe UI",9);ShowInTaskbar=false;
        Control parent=this;
        for(int i=0;i<3;i++) {
            SplitContainer split=new SplitContainer{Dock=DockStyle.Fill,Orientation=Orientation.Horizontal,SplitterWidth=9,Panel1MinSize=70,Panel2MinSize=70,Size=new Size(1000,1800)};
            splits[i]=split;parent.Controls.Add(split);rows[i]=split.Panel1;parent=split.Panel2;
            split.SplitterMoved+=delegate{if(loaded&&!arranging){Arrange();Save();}};
        }
        rows[3]=(Panel)parent;
        for(int i=0;i<4;i++)slots[i]=rows[i];
        if(agendas)Partition(rows[2],2,new int[]{2,4},Orientation.Vertical);
        if(subdivided){
            if(terminalMatrix){slots[1]=rows[1];slots[4]=new Panel{Visible=false,Size=new Size(1,1)};slots[5]=new Panel{Visible=false,Size=new Size(1,1)};rows[1].Controls.Add(slots[4]);rows[1].Controls.Add(slots[5]);}
            else {Partition(rows[1],1,new int[]{1,4},Orientation.Vertical);slots[5]=new Panel{Visible=false,Size=new Size(1,1)};rows[1].Controls.Add(slots[5]);}
            Partition(rows[3],3,new int[]{3,6},Orientation.Vertical);
            if(aiColumns==2)Partition(rows[2],2,new int[]{2,7},Orientation.Vertical);
            if(aiColumns==3)Partition(rows[2],2,new int[]{2,7,8},Orientation.Vertical);
        }
        string[] labels=new string[slots.Length];Array.Copy(names,labels,4);
        if(agendas){labels[2]="Agenda Spread";labels[4]="Agenda Tecnovix";}
        if(subdivided){labels[1]="Terminal 1";labels[4]="Terminal 2";labels[5]="Terminal 3";labels[6]="Outlook";if(aiColumns>1){labels[2]="ChatGPT";labels[7]="Codex";}if(aiColumns>2){labels[8]="Claude AI";}}
        if(subdivided){labels[1]="Ubuntu 1";labels[4]="Ubuntu 2";labels[5]="Ubuntu 3";}
        for(int i=0;i<slots.Length;i++) {
            int slot=i;slots[i].BackColor=Color.FromArgb(15,20,29);
            Button label=new Button{Text=labels[i]+"   ·   Escolher janela  ▾",Dock=DockStyle.Top,Height=25,FlatStyle=FlatStyle.Flat,ForeColor=Color.LightSteelBlue,BackColor=Color.FromArgb(24,31,43),TextAlign=ContentAlignment.MiddleLeft};
            label.AutoEllipsis=true;headers[i]=label;
            DateTime ignoreClickUntil=DateTime.MinValue;
            label.FlatAppearance.BorderSize=0;
            label.MouseDown+=delegate(object sender,MouseEventArgs e){
                if(e.Button!=MouseButtons.Left||(Control.ModifierKeys&Keys.Shift)==0||slot==ViewerSlot)return;
                WindowChoice app=GetAssignment(slot);if(app==null||!Valid(app))return;
                ignoreClickUntil=DateTime.UtcNow.AddSeconds(1);Native.ReleaseCapture();Native.SetForegroundWindow(app.Handle);
                Point cursor=Cursor.Position;long packed=((long)(cursor.Y&0xffff)<<16)|(uint)(cursor.X&0xffff);
                MouseExitNative.PostMessage(app.Handle,0xA1,new IntPtr(2),new IntPtr(packed));
            };
            label.Click+=delegate{if(DateTime.UtcNow<ignoreClickUntil)return;Choose(slot,label);};slots[i].Controls.Add(label);
            Label empty=new Label{Text="Área disponível\nEscolha uma janela na barra acima",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,ForeColor=Color.SlateGray};slots[i].Controls.Add(empty);label.BringToFront();
        }
        zoneNames=labels;FormClosed+=delegate{visibilityTimer.Stop();visibilityTimer.Dispose();headerTips.Dispose();};
        visibilityTimer.Tick+=delegate{ClearClosedAssignments();KeepBehindAssigned();};
        Shown+=delegate {
            double[] values=(double[])initial.Clone();
            try{string[] lines=File.ReadAllLines(ConfigPath);for(int i=0;i<3;i++){double parsed;if(double.TryParse(lines[i],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out parsed)&&parsed>=.08&&parsed<=.80)values[i]=parsed;}}catch{}
            arranging=true;double remaining=1;
            for(int i=0;i<3;i++){int available=splits[i].Height-splits[i].SplitterWidth;int target=(int)(available*values[i]/remaining);splits[i].SplitterDistance=Math.Max(70,Math.Min(available-70,target));remaining-=values[i];if(remaining<.08)remaining=.08;}
            Dictionary<string,double> ratios=new Dictionary<string,double>();
            try{foreach(string line in File.ReadAllLines(InnerPath)){string[] pair=line.Split('|');double value;if(pair.Length==2&&double.TryParse(pair[1],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out value)&&value>.05&&value<.95)ratios[pair[0]]=value;}}catch{}
            foreach(KeyValuePair<string,SplitContainer> entry in innerSplits){SplitContainer split=entry.Value;double ratio;int available=(split.Orientation==Orientation.Horizontal?split.Height:split.Width)-split.SplitterWidth;if(!ratios.TryGetValue(entry.Key,out ratio))ratio=entry.Key.EndsWith("-0")&&aiColumns==3&&entry.Key.StartsWith("2-")?1.0/3:.5;split.SplitterDistance=Math.Max(45,Math.Min(available-45,(int)(available*ratio)));}
            arranging=false;loaded=true;Arrange();visibilityTimer.Start();
        };
    }
    string InnerPath {get{return Path.Combine(AppPaths.Workspace,"subzonas-"+number+".txt");}}
    void Partition(Panel row,int rowIndex,int[] indexes,Orientation orientation){Control parent=row;for(int i=0;i<indexes.Length-1;i++){
        SplitContainer split=new SplitContainer{Size=new Size(1080,1000),Dock=DockStyle.Fill,Orientation=orientation,SplitterWidth=6,Panel1MinSize=45,Panel2MinSize=45};parent.Controls.Add(split);slots[indexes[i]]=split.Panel1;parent=split.Panel2;innerSplits[rowIndex+"-"+i]=split;
        split.SplitterMoved+=delegate{if(loaded&&!arranging){Arrange();Save();}};
    }slots[indexes[indexes.Length-1]]=(Panel)parent;}
    public int SlotCount {get{return slots.Length;}}
    public bool SlotEnabled(int index){return !(twoTerminalColumns&&index==5)&&(!terminalMatrix||(index!=4&&index!=5));}
    public Viewer OwnerViewer {get{return viewer;}}
    public Rectangle ContentBounds(int index){Rectangle area=ZoneBounds(index);return new Rectangle(area.X+3,area.Y+26,Math.Max(1,area.Width-6),Math.Max(40,area.Height-29));}
    public Rectangle ZoneBounds(int index){return slots[index].RectangleToScreen(slots[index].ClientRectangle);}
    public bool Empty(int index){return assigned[index]==null;}
    public int MonitorNumber {get{return number;}}
    public WindowChoice GetAssignment(int index){return index==ViewerSlot?viewer.AssignedWindow:assigned[index];}
    public void ClearAssignments(){for(int i=0;i<assigned.Length;i++)Set(i,null);}
    public bool Has(int index,IntPtr handle){return assigned[index]!=null&&assigned[index].Handle==handle;}
    public string Report(){string result="Monitor "+number+" "+Bounds+Environment.NewLine;for(int i=0;i<slots.Length;i++){if(!SlotEnabled(i))continue;Rectangle zone=slots[i].RectangleToScreen(slots[i].ClientRectangle);WindowChoice app=assigned[i];Native.RectangleNormal actual=new Native.RectangleNormal();if(app!=null)GetWindowRect(app.Handle,out actual);result+=i+" zone="+zone+" app="+(i==ViewerSlot?"Tela virtual":app==null?"Disponível":app.Title)+" actual="+actual.Left+","+actual.Top+","+actual.Right+","+actual.Bottom+Environment.NewLine;}return result;}
    public void Set(int index,WindowChoice app){assigned[index]=app;RefreshHeader(index);}
    void RefreshHeader(int index){Button header;if(!headers.TryGetValue(index,out header)||zoneNames==null)return;WindowChoice app=GetAssignment(index);header.Text=zoneNames[index]+" · "+(app==null?"Escolher janela":app.Title)+"  ▾";headerTips.SetToolTip(header,app==null?"Escolha uma janela para esta zona":app.Title+"\nClique para trocar a janela.");}
    public void RefreshSelectionNames(){for(int i=0;i<SlotCount;i++)RefreshHeader(i);}
    public void ClearClosedAssignments(){bool changed=false;for(int i=0;i<SlotCount;i++){WindowChoice app=GetAssignment(i);if(app==null||Valid(app))continue;if(i==ViewerSlot)viewer.DetachWindow(app.Handle);else Set(i,null);changed=true;}if(changed){RefreshSelectionNames();ZoneWorkspace.SaveProfile(viewer);}}
    void Choose(int index,Control anchor) {
        if(choosingWindow)return;
        choosingWindow=true;viewer.BeginWindowChoice();
        TraceLog.Write("picker.opened","","monitor",number.ToString(),"zone",index.ToString());
        try{using(ContextMenuStrip actions=SelectionMenu(index,WindowCatalog.List()))WindowPicker.Show(viewer,actions,anchor.PointToScreen(new Point(0,anchor is Form?25:anchor.Height)));}
        finally{choosingWindow=false;viewer.EndWindowChoice();TraceLog.Write("picker.closed","","monitor",number.ToString(),"zone",index.ToString());}
    }
    public ContextMenuStrip ShowSelection(int index,IEnumerable<WindowChoice> choices,Point location) {
        ContextMenuStrip menu=SelectionMenu(index,choices);
        // A tiny independent owner can receive menu focus without raising the
        // monitor-sized background board over the user's applications.
        Form host=new Form{FormBorderStyle=FormBorderStyle.None,ShowInTaskbar=false,StartPosition=FormStartPosition.Manual,Bounds=new Rectangle(location,new Size(1,1)),Opacity=0};
        menu.Closed+=delegate{if(IsHandleCreated&&!IsDisposed)BeginInvoke((Action)delegate{host.Close();host.Dispose();});else host.Dispose();};
        host.Show();menu.Show(host,Point.Empty);return menu;
    }
    public void PickPresentation(Control anchor){Choose(ViewerSlot,anchor);}
    public ContextMenuStrip SelectionMenu(int index,IEnumerable<WindowChoice> choices) {
        ContextMenuStrip menu=new ContextMenuStrip();
        menu.ShowItemToolTips=true;
        if(number==0&&(index==1||index==4||index==5)){menu.Items.Add("Abrir Ubuntu nesta zona",null,delegate{BeginInvoke((Action)delegate{TerminalGrid.Open(viewer,index);});});menu.Items.Add(new ToolStripSeparator());}
        WindowChoice selected=GetAssignment(index);
        if(selected!=null){
            ToolStripItem recover=menu.Items.Add("Trazer janela selecionada de volta",null,delegate{BeginInvoke((Action)delegate{Recover(index);});});
            recover.Enabled=Valid(selected);menu.Items.Add(new ToolStripSeparator());
        }
        List<WindowChoice> candidates=new List<WindowChoice>(choices);
        ZoneWorkspace.IncludeAssigned(candidates);
        if(selected!=null&&Valid(selected)&&!candidates.Exists(w=>w.Handle==selected.Handle))candidates.Insert(0,selected);
        foreach(WindowChoice app in candidates){WindowChoice choice=app;
            bool same=selected!=null&&selected.Handle==choice.Handle&&selected.Pid==choice.Pid;
            Size minimum=index==ViewerSlot?Size.Empty:WindowLimits.Minimum(choice.Handle);
            bool elsewhere=ZoneWorkspace.UsedElsewhere(choice.Handle,this,index);
            if(!same&&index!=ViewerSlot&&!WindowLimits.Fits(minimum,ContentBounds(index))){ToolStripMenuItem tooLarge=new ToolStripMenuItem(app.Title+" · não cabe nesta zona"){Enabled=false};tooLarge.ToolTipText="Precisa de "+minimum.Width+" × "+minimum.Height+". Amplie a zona ou use uma área maior.";menu.Items.Add(tooLarge);continue;}
            ToolStripMenuItem option=new ToolStripMenuItem(app.Title+(elsewhere?" · mover para esta zona":"")){Checked=GetAssignment(index)!=null&&GetAssignment(index).Handle==app.Handle};option.ToolTipText=elsewhere?"Move esta janela para cá e libera a zona anterior.":"Encaixar nesta zona.";menu.Items.Add(option);option.Click+=delegate{
            // Let ToolStrip finish closing before moving windows or destroying items.
            BeginInvoke((Action)delegate{
                if(IsDisposed||Disposing)return;
                if(same){Recover(index);return;}
                uint pid;Native.GetWindowThreadProcessId(choice.Handle,out pid);
                if(!Native.IsWindow(choice.Handle)||pid!=(uint)choice.Pid){MessageBox.Show("Essa janela foi fechada. Abra a lista novamente.","IOI Desk");return;}
                TransferWindow(index,choice);
            });
        };}
        menu.Items.Add(new ToolStripSeparator());menu.Items.Add("Reencaixar todas as janelas",null,delegate{BeginInvoke((Action)Arrange);});
        if(index==ViewerSlot){menu.Items.Add("Configurações do IOI Desk",null,delegate{BeginInvoke((Action)viewer.OpenPreferences);});menu.Items.Add("Voltar à aplicação",null,delegate{BeginInvoke((Action)viewer.ShowApplication);});}
        menu.Closed+=delegate{if(IsHandleCreated&&!IsDisposed)BeginInvoke((Action)delegate{menu.Dispose();});};
        return menu;
    }
    public void Unassign(IntPtr handle){for(int i=0;i<assigned.Length;i++)if(assigned[i]!=null&&assigned[i].Handle==handle)Set(i,null);}
    bool Valid(WindowChoice app){uint pid;Native.GetWindowThreadProcessId(app.Handle,out pid);return Native.IsWindow(app.Handle)&&pid==(uint)app.Pid;}
    public bool TransferWindow(int index,WindowChoice choice){
        string operation=TraceLog.Operation();
        TraceLog.Write("window.assign.request",operation,"monitor",number.ToString(),"zone",index.ToString(),"title",choice==null?"":choice.Title,"pid",choice==null?"":choice.Pid.ToString(),"handle",choice==null?"":choice.Handle.ToInt64().ToString());
        if(choice==null||!Valid(choice)||!SlotEnabled(index)){TraceLog.Write("window.assign.rejected",operation,"reason","invalid-window-or-zone");return false;}
        // Validate the destination before releasing any existing assignment.
        if(index!=ViewerSlot&&!WindowLimits.Fits(WindowLimits.Minimum(choice.Handle),ContentBounds(index))){TraceLog.Write("window.assign.rejected",operation,"reason","minimum-size","minimum",WindowLimits.Minimum(choice.Handle).ToString(),"target",ContentBounds(index).ToString());return false;}
        if(index==ViewerSlot){if(!viewer.AssignWindow(choice))return false;Unassign(choice.Handle);}
        else {ZoneWorkspace.Unassign(choice.Handle);Unassign(choice.Handle);viewer.DetachWindow(choice.Handle);Set(index,choice);}
        Place(index);TraceLog.Write("window.assign.complete",operation,"target",ContentBounds(index).ToString(),"actual",ZoneSelectionTestBounds(choice.Handle));RefreshSelectionNames();ZoneWorkspace.RefreshSelectionNames();ZoneWorkspace.SaveProfile(viewer);FocusAfterMenu(choice.Handle);KeepBehindAssigned();return true;
    }
    static string ZoneSelectionTestBounds(IntPtr handle){Native.RectangleNormal rect;return GetWindowRect(handle,out rect)?rect.Left+","+rect.Top+","+rect.Right+","+rect.Bottom:"unavailable";}
    public void Recover(int index){
        WindowChoice app=GetAssignment(index);if(app==null||!Valid(app)){if(index!=ViewerSlot)Set(index,null);return;}
        if(index==ViewerSlot){if(!viewer.AssignWindow(app))return;Place(index);FocusAfterMenu(app.Handle);}
        else {if(!IsWindowVisible(app.Handle))Native.ShowWindow(app.Handle,4);Place(index);FocusAfterMenu(app.Handle);}
        KeepBehindAssigned();
    }
    void FocusAfterMenu(IntPtr handle){BeginInvoke((Action)delegate{if(!Native.IsWindow(handle))return;Native.ShowWindow(handle,9);Native.SetWindowPos(handle,IntPtr.Zero,0,0,0,0,0x0043);Native.SetForegroundWindow(handle);KeepBehindAssigned();});}
    public void Arrange() {
        if(!loaded||arranging)return;arranging=true;
        try{for(int i=0;i<slots.Length;i++)Place(i);}finally{arranging=false;}
    }
    public void Place(int index){
        if(!SlotEnabled(index))return;
        Rectangle area=ContentBounds(index);RefreshHeader(index);
        if(index==ViewerSlot){viewer.Bounds=area;Native.SetWindowPos(viewer.Handle,IntPtr.Zero,area.X,area.Y,area.Width,area.Height,0x0010);return;}
        WindowChoice app=assigned[index];if(app==null)return;
        uint pid;Native.GetWindowThreadProcessId(app.Handle,out pid);if(pid!=(uint)app.Pid)return;
        Size minimum=WindowLimits.Minimum(app.Handle);
        if(!WindowLimits.Fits(minimum,area)){
            if(ZoneWorkspace.Relocate(app,this,index))return;
            Rectangle monitor=Screen.FromRectangle(ZoneBounds(index)).WorkingArea;
            area=new Rectangle(Math.Max(monitor.Left,Math.Min(area.X,monitor.Right-Math.Min(monitor.Width,minimum.Width))),Math.Max(monitor.Top,Math.Min(area.Y,monitor.Bottom-Math.Min(monitor.Height,minimum.Height))),Math.Min(monitor.Width,Math.Max(area.Width,minimum.Width)),Math.Min(monitor.Height,Math.Max(area.Height,minimum.Height)));
            headers[index].ForeColor=Color.Orange;headerTips.SetToolTip(headers[index],"Esta janela precisa de uma zona maior; amplie a divisória.");
        }else headers[index].ForeColor=Color.LightSteelBlue;
        viewer.MoveWindow(app.Handle,area);Native.SetWindowPos(app.Handle,IntPtr.Zero,area.X,area.Y,area.Width,area.Height,0x0010);
    }
    public void Save(){if(!loaded)return;try{string[] values=new string[3];double remaining=1;for(int i=0;i<3;i++){double fraction=remaining*splits[i].SplitterDistance/Math.Max(1.0,splits[i].Height-splits[i].SplitterWidth);values[i]=fraction.ToString(System.Globalization.CultureInfo.InvariantCulture);remaining-=fraction;}File.WriteAllLines(ConfigPath,values);List<string> inner=new List<string>();foreach(KeyValuePair<string,SplitContainer> entry in innerSplits){SplitContainer split=entry.Value;double ratio=split.SplitterDistance/Math.Max(1.0,(split.Orientation==Orientation.Horizontal?split.Height:split.Width)-split.SplitterWidth);inner.Add(entry.Key+"|"+ratio.ToString(System.Globalization.CultureInfo.InvariantCulture));}File.WriteAllLines(InnerPath,inner.ToArray());}catch{}}
}
