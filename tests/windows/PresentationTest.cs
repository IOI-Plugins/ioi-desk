using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

static class PresentationSelectionTest {
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr window,out Native.RectangleNormal rect);
    [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr window,uint command);
    static bool Above(IntPtr app,IntPtr other){for(IntPtr window=GetWindow(other,3);window!=IntPtr.Zero;window=GetWindow(window,3))if(window==app)return true;return false;}
    public static void Run(string path) {
        // Exercise assignment/transfer against an explicit screen. Hosted runners
        // do not have the user's virtual-display driver; driver integration is
        // validated separately on a workstation with that driver installed.
        using(Viewer viewer=new Viewer(Screen.PrimaryScreen))using(Form cover=new Form())using(Form sample=new Form())using(ZoneBoard board=new ZoneBoard(viewer,Native.FindPhysicalSecondary()??Screen.PrimaryScreen,9,new string[]{"Presentation test","Test","Test","Test"},new double[]{.35,.30,.20})) {
            board.ViewerSlot=0;
            sample.Text="IOI Desk · teste de apresentação";sample.BackColor=Color.CornflowerBlue;
            board.Shown+=delegate{board.BeginInvoke((Action)delegate{
                viewer.Show();sample.Show();Native.ShowWindow(sample.Handle,6);
                cover.Bounds=viewer.PresentationBounds;cover.Show();
                ContextMenuStrip menu=board.ShowSelection(0,new WindowChoice[]{new WindowChoice(sample.Handle,Process.GetCurrentProcess().Id,sample.Text)},board.PointToScreen(new Point(20,30)));
                ((ToolStripMenuItem)menu.Items[0]).PerformClick();menu.Close();
                Timer check=new Timer{Interval=400};check.Tick+=delegate{
                    check.Stop();check.Dispose();
                    try {
                        if(viewer.AssignedHandle!=sample.Handle)throw new Exception("Virtual selection not applied.");
                        if(!Above(sample.Handle,cover.Handle))throw new Exception("Previous source window covered new selection.");
                        Native.RectangleNormal actual;GetWindowRect(sample.Handle,out actual);
                        Rectangle rect=Rectangle.FromLTRB(actual.Left,actual.Top,actual.Right,actual.Bottom);
                        if(rect!=viewer.PresentationBounds)throw new Exception("Incorrect source resolution/position: "+rect);
                        if(!board.TransferWindow(1,new WindowChoice(sample.Handle,Process.GetCurrentProcess().Id,sample.Text)))throw new Exception("Transfer back to physical zone failed.");
                        if(viewer.AssignedHandle!=IntPtr.Zero||!board.Has(1,sample.Handle)||board.GetAssignment(0)!=null)throw new Exception("Source remained assigned after transfer.");
                        File.WriteAllText(path,"PASS: virtual selection appears above previous source window: "+rect+"; transfer to physical zone releases origin.");
                    }catch(Exception ex){File.WriteAllText(path,"FAIL: "+ex);Environment.ExitCode=1;}
                    viewer.Close();board.Close();
                };check.Start();
            });};
            Application.Run(board);
        }
    }
}
