using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

static class Program {
    static System.Threading.Mutex workspaceMutex;
    static System.Threading.EventWaitHandle showWorkspace;
    public static bool ConsumeShowRequest(){return showWorkspace!=null&&showWorkspace.WaitOne(0);}
    [STAThread] static void Main(string[] args) {
        if(args.Length==2&&args[0]=="--connection-fixture"){ConnectionLifecycleTest.Child(args[1]);return;}
        bool workspace=args.Length==0||(args.Length==1&&(args[0]=="--zones"||args[0]=="--apply"||args[0]=="--organize"));
        if(workspace){
            bool created;workspaceMutex=new System.Threading.Mutex(true,"Local\\IOI.Desk.Workspace",out created);
            if(!created){try{using(var signal=System.Threading.EventWaitHandle.OpenExisting("Local\\IOI.Desk.ShowWorkspace"))signal.Set();}catch(System.Threading.WaitHandleCannotBeOpenedException){}return;}
            showWorkspace=new System.Threading.EventWaitHandle(false,System.Threading.EventResetMode.AutoReset,"Local\\IOI.Desk.ShowWorkspace");
        }
        AppPaths.Initialize();
        Native.SetProcessDPIAware();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        if(args.Length==2&&(args[0]=="--connection-lifecycle-test"||args[0]=="--presentation-motion-test")){
            try{if(args[0]=="--connection-lifecycle-test")ConnectionLifecycleTest.Run(args[1]);else PresentationMotionTest.Run(args[1]);}
            catch(Exception ex){File.WriteAllText(args[1],"FAIL: "+ex);Environment.ExitCode=1;}return;
        }
        if(args.Length==2&&args[0]=="--data-migration-test"){try{AppPaths.Test(args[1]);}catch(Exception ex){File.WriteAllText(args[1],"FAIL: "+ex);Environment.ExitCode=1;}return;}
        if(args.Length==2&&args[0]=="--layout-persistence-test"){try{LayoutPersistenceTest.Run(args[1]);}catch(Exception ex){File.WriteAllText(args[1],"FAIL: "+ex);Environment.ExitCode=1;}return;}
        if(args.Length==2&&args[0]=="--update-test"){try{DeskUpdates.Test(args[1]);}catch(Exception ex){File.WriteAllText(args[1],"FAIL: "+ex);Environment.ExitCode=1;}return;}
        if(args.Length==2&&args[0]=="--virtual-left"){try{DisplayModes.MoveVirtualLeft(args[1]);}catch(Exception ex){File.AppendAllText(args[1],Environment.NewLine+"FAIL: "+ex);Environment.ExitCode=1;}return;}
        if(args.Length==2&&args[0]=="--input-safety-test"){InputSafety.Test();File.WriteAllText(args[1],"PASS: middle-button rising edge and raw-input flags; polling fallback does not depend on the mouse hook.");return;}
        if(args.Length==2&&args[0]=="--trace-test"){TraceLog.Test(args[1]);return;}
        TraceLog.Write("session.start","", "version",Product.Version,"monitors",Native.Inventory());
        if(args.Length==1&&args[0]=="--sharing"){Application.Run(new SharingForm(false));return;}
        Application.ThreadException+=delegate(object sender,System.Threading.ThreadExceptionEventArgs e){
            TraceLog.Write("app.exception","","error",e.Exception.ToString());
            MessageBox.Show("Não foi possível concluir essa ação. O aplicativo continua aberto. O detalhe foi salvo em Diagnóstico → abrir registros.","IOI Desk",MessageBoxButtons.OK,MessageBoxIcon.Warning);
        };
        if(args.Length==2&&args[0]=="--picker-test"){WindowPicker.Test(args[1]);return;}
        if(args.Length==2&&args[0]=="--drag-test"){ZoneDrag.Test(args[1]);return;}
        if(args.Length==2&&args[0]=="--zone-test"){ZoneSelectionTest.Run(args[1]);return;}
        if(args.Length==2&&args[0]=="--presentation-test"){PresentationSelectionTest.Run(args[1]);return;}
        if(args.Length==2&&args[0]=="--profile-test"){WorkspaceProfile.Test(args[1]);return;}
        if(args.Length==2&&args[0]=="--minimums"){WindowLimits.Report(args[1]);return;}
        if(args.Length==2&&args[0]=="--grid-test"){GridTests.Run(args[1]);return;}
        if(args.Length==2&&args[0]=="--panel-test"){PanelTests.Run(args[1]);return;}
        if (args.Length == 2 && args[0] == "--self-test") {
            try { Geometry.Test(); ViewMath.Test(); DisplayModes.Test(); MiddleButton.Test(); File.WriteAllText(args[1], "PASS: geometry, pointer compensation, display mode layout and middle button mapping.\r\n" + Native.Inventory()); }
            catch(Exception ex) { File.WriteAllText(args[1], "FAIL: " + ex); Environment.ExitCode=1; }
            return;
        }
        if(args.Length==2&&args[0]=="--display-modes") {
            try{DisplayModes.Test();var screen=Native.FindVirtual();if(screen==null)throw new Exception("Tela virtual não encontrada.");File.WriteAllText(args[1],DisplayModes.Describe(screen.DeviceName));}
            catch(Exception ex){File.WriteAllText(args[1],ex.ToString());Environment.ExitCode=1;}
            return;
        }
        if (args.Length == 2 && args[0] == "--capture-test") {
            try {
                Screen source=Native.FindVirtual();
                if(source==null) throw new Exception("Tela virtual nao encontrada.");
                using(Bitmap b=new Bitmap(source.Bounds.Width,source.Bounds.Height,PixelFormat.Format32bppRgb)) {
                    using(Graphics g=Graphics.FromImage(b)) g.CopyFromScreen(source.Bounds.Location,Point.Empty,b.Size,CopyPixelOperation.SourceCopy);
                    b.Save(args[1],ImageFormat.Png);
                }
            } catch(Exception ex) { File.WriteAllText(args[1]+".error.txt",ex.ToString()); Environment.ExitCode=1; }
            return;
        }
        Viewer viewer=new Viewer();
        viewer.Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        if(args.Length==0||(args.Length==1&&args[0]=="--zones"))viewer.Shown+=delegate{viewer.BeginInvoke((Action)delegate{viewer.StartZones();WorkspaceProfile.RunPending(viewer);});};
        if(args.Length==1&&args[0]=="--apply")viewer.Shown+=delegate{viewer.StartEnvironment();};
        if(args.Length==1&&args[0]=="--organize")viewer.Shown+=delegate{viewer.BeginInvoke((Action)delegate{viewer.StartZones();WorkspaceProfile.Prepare(viewer);});};
        if(args.Length==2 && (args[0]=="--ui-test"||args[0]=="--ui-test-setup"||args[0]=="--ui-test-comfort"||args[0]=="--ui-test-panel"||args[0]=="--ui-test-chrome")) {
            Timer smoke=new Timer();smoke.Interval=1800;
            smoke.Tick+=delegate {
                smoke.Stop();
                try {
                    File.WriteAllText(args[1]+".txt",viewer.Diagnostic());
                    using(Bitmap snapshot=new Bitmap(viewer.Width,viewer.Height)) {
                        using(Graphics surface=Graphics.FromImage(snapshot))surface.CopyFromScreen(viewer.Location,Point.Empty,viewer.Size,CopyPixelOperation.SourceCopy);
                        snapshot.Save(args[1],ImageFormat.Png);
                    }
                } catch(Exception ex) {File.WriteAllText(args[1]+".error.txt",ex.ToString());Environment.ExitCode=1;}
                viewer.Close();smoke.Dispose();
            };
            viewer.Shown+=delegate{if(args[0]=="--ui-test-setup")viewer.PreviewSetup();if(args[0]=="--ui-test-comfort")viewer.PreviewComfort();if(args[0]=="--ui-test-panel"||args[0]=="--ui-test-chrome")viewer.StartEnvironment();if(args[0]=="--ui-test-chrome")viewer.PreviewChrome();viewer.Activate();smoke.Start();};
        }
        Application.Run(new WorkspaceContext(viewer));
    }
}

