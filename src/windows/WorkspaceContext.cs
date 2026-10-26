using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

class WorkspaceContext : ApplicationContext {
    readonly Timer reopen=new Timer{Interval=500};
    public static WorkspaceContext Current;
    readonly List<Viewer> windows=new List<Viewer>();
    readonly NotifyIcon tray;
    readonly ContextMenuStrip trayMenu=new ContextMenuStrip();
    Viewer First {get{return windows.Count==0?null:windows[0];}}
    public WorkspaceContext(Viewer root) {
        Current=this;Track(root);
        trayMenu.Items.Add("Configurações",null,delegate{if(First!=null)First.OpenPreferences();});
        trayMenu.Items.Add("Voltar à aplicação",null,delegate{if(First!=null)First.ShowApplication();});
        trayMenu.Items.Add("Escolher janela de apresentação",null,delegate{if(First!=null){First.ExitFromHub();ZoneWorkspace.ChoosePresentation(First);}});
        trayMenu.Items.Add("Reencaixar janelas",null,delegate{ZoneWorkspace.ArrangeAll();});
        trayMenu.Items.Add("Mouse e teclado · Windows / Mac",null,delegate{ComputerSharing.Open();});
        trayMenu.Items.Add("Diagnóstico · abrir registros",null,delegate{Directory.CreateDirectory(TraceLog.Folder);Process.Start(new ProcessStartInfo(TraceLog.Folder){UseShellExecute=true});});
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("Encerrar IOI Desk",null,delegate{foreach(Viewer view in windows.ToArray())view.Close();});
        tray=new NotifyIcon{Text="IOI Desk",Icon=root.Icon??SystemIcons.Application,ContextMenuStrip=trayMenu,Visible=true};
        tray.DoubleClick+=delegate{if(First!=null)First.OpenPreferences();};
        Application.ApplicationExit+=delegate{ComputerSharing.Stop();};
        root.Show();
        reopen.Tick+=delegate{if(Program.ConsumeShowRequest()&&First!=null)First.OpenPreferences();};reopen.Start();
        ComputerSharing.AutoStart();
    }
    void Track(Viewer view){windows.Add(view);view.FormClosed+=delegate{windows.Remove(view);if(windows.Count==0){reopen.Stop();reopen.Dispose();if(tray!=null){tray.Visible=false;tray.Dispose();}trayMenu.Dispose();ExitThread();}};}
    public Viewer Open(Screen screen){Viewer view=new Viewer(screen);Track(view);view.Show();Tile();return view;}
    public void Tile() {
        LayoutViews(windows,ZoneWorkspace.Boards);
    }
    internal static void LayoutViews(IList<Viewer> views,IList<ZoneBoard> boards) {
        List<Viewer> floating=new List<Viewer>();
        foreach(Viewer view in views)if(!ZoneWorkspace.PlacePresentation(view,boards))floating.Add(view);
        Screen host=WorkspaceScreens.Presentation;if(host==null||floating.Count==0)return;
        Rectangle area=host.WorkingArea;
        int limit=(area.Height-8*(floating.Count+1))/floating.Count,y=area.Y+6;
        for(int i=0;i<floating.Count;i++){int height=Math.Min(floating[i].PreferredHeight(area.Width-12),limit);floating[i].Bounds=new Rectangle(area.X+6,y,area.Width-12,height);y+=height+6;}
    }
    public void NewPanel(IWin32Window owner) {
        using(Form picker=new Form()) {
            picker.Text="Adicionar tela";picker.ClientSize=new Size(520,208);picker.StartPosition=FormStartPosition.CenterParent;picker.FormBorderStyle=FormBorderStyle.FixedDialog;
            picker.MaximizeBox=picker.MinimizeBox=false;picker.BackColor=Color.FromArgb(24,31,43);picker.ForeColor=Color.White;picker.Font=new Font("Segoe UI",10);
            Label label=new Label{Text="Escolha outra tela virtual",Left=20,Top=18,Width=470,Height=28};
            ComboBox screens=new ComboBox{Left=20,Top=54,Width=478,DropDownStyle=ComboBoxStyle.DropDownList};
            Button open=new Button{Text="Abrir painel",Left=353,Top=151,Width=145,Height=34},driver=new Button{Text="Criar no controlador…",Left=20,Top=151,Width=224,Height=34};
            Label hint=new Label{Left=20,Top=94,Width=478,Height=45,ForeColor=Color.LightSteelBlue};
            Action refresh=delegate {
                screens.Items.Clear();foreach(Screen s in PanelWindows.VirtualScreens())if(!ViewsScreen(s.DeviceName))screens.Items.Add(new ScreenChoice(s));
                if(screens.Items.Count>0){screens.SelectedIndex=0;hint.Text="Cada painel usa uma tela diferente, no mesmo Windows.";}else hint.Text="Ative mais uma tela no controlador do driver e volte aqui.";
                open.Enabled=screens.Items.Count>0;
            };
            open.Click+=delegate{ScreenChoice choice=screens.SelectedItem as ScreenChoice;if(choice!=null){picker.Close();Open(choice.Screen);}};
            driver.Click+=delegate{picker.Close();PanelWindows.OpenDriver();};
            picker.Activated+=delegate{refresh();};picker.Controls.AddRange(new Control[]{label,screens,hint,open,driver});refresh();picker.ShowDialog(owner);
        }
    }
    bool ViewsScreen(string name){foreach(Viewer view in windows)if(view.SourceDevice==name)return true;return false;}
}

