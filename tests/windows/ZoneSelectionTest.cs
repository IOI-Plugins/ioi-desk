using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

static class ZoneSelectionTest {
    public static void Run(string path) {
        Exception failure=null;
        System.Threading.ThreadExceptionEventHandler handler=delegate(object sender,System.Threading.ThreadExceptionEventArgs args){failure=args.Exception;};
        Application.ThreadException+=handler;
        using(Viewer viewer=new Viewer())using(Form previous=new Form())using(Form dummy=new Form())using(ZoneBoard board=new ZoneBoard(viewer,Screen.PrimaryScreen,9,new string[]{"Test","Test","Test","Test"},new double[]{.29,.14,.345})) {
            dummy.Text="IOI Desk selection regression test";dummy.Size=new Size(300,180);
            board.Shown+=delegate{board.BeginInvoke((Action)delegate{
                previous.Show();board.Set(1,new WindowChoice(previous.Handle,Process.GetCurrentProcess().Id,"Previous zone"));board.Arrange();
                previous.Bounds=new Rectangle(50,80,420,210);Rectangle previousBounds=BoundsOf(previous.Handle);
                dummy.Show();Native.ShowWindow(dummy.Handle,6);
                WindowChoice[] choices=new WindowChoice[]{new WindowChoice(dummy.Handle,Process.GetCurrentProcess().Id,dummy.Text)};
                ContextMenuStrip cancel=board.ShowSelection(0,choices,board.PointToScreen(new Point(10,30)));
                bool visibleWhileOpen=Above(previous.Handle,board.Handle)&&BoundsOf(previous.Handle)==previousBounds;
                cancel.Close(ToolStripDropDownCloseReason.Keyboard);
                bool cancellationPreserved=board.Empty(0)&&board.Has(1,previous.Handle)&&BoundsOf(previous.Handle)==previousBounds;
                ContextMenuStrip menu=board.ShowSelection(0,choices,board.PointToScreen(new Point(10,30)));
                ((ToolStripMenuItem)menu.Items[0]).PerformClick();menu.Close();
                Timer verify=new Timer{Interval=250};verify.Tick+=delegate{
                    verify.Stop();verify.Dispose();
                    try {
                        if(failure!=null)throw failure;
                        if(!visibleWhileOpen||!cancellationPreserved)throw new Exception("Opening/cancelling changed another zone.");
                        if(board.Empty(0))throw new Exception("Window selection was not applied.");
                        if(IsIconic(dummy.Handle))throw new Exception("Selected window stayed minimized.");
                        if(!board.Has(1,previous.Handle)||IsIconic(previous.Handle)||!Above(previous.Handle,board.Handle))throw new Exception("Previous zone was hidden by board.");
                        if(BoundsOf(previous.Handle)!=previousBounds)throw new Exception("Selection changed another window position.");
                        Native.SetWindowPos(board.Handle,IntPtr.Zero,0,0,0,0,0x0013);board.KeepBehindAssigned();
                        if(!Above(dummy.Handle,board.Handle)||!Above(previous.Handle,board.Handle))throw new Exception("Background board covered assigned applications: dummy="+Above(dummy.Handle,board.Handle)+", previous="+Above(previous.Handle,board.Handle)+", "+board.Report());
                        Native.ShowWindow(dummy.Handle,0);
                        using(ContextMenuStrip recovery=board.SelectionMenu(0,new WindowChoice[0])){
                            if(recovery.Items.Count<3||!recovery.Items[0].Enabled)throw new Exception("Hidden assigned app cannot be recovered through menu.");
                        }
                        board.Recover(0);
                        if(!IsWindowVisible(dummy.Handle)||IsIconic(dummy.Handle))throw new Exception("Recovery did not show the hidden assigned app.");
                        if(!board.TransferWindow(2,new WindowChoice(dummy.Handle,Process.GetCurrentProcess().Id,dummy.Text)))throw new Exception("Transfer rejected an occupied window.");
                        if(!board.Empty(0)||!board.Has(2,dummy.Handle))throw new Exception("Transfer did not release origin and assign destination.");
                        if(BoundsOf(previous.Handle)!=previousBounds)throw new Exception("Transfer moved an unrelated window.");
                        board.Unassign(dummy.Handle);if(!board.Empty(0)||!board.Empty(2))throw new Exception("Reassignment failed.");
                        File.WriteAllText(path,"PASS: cancellation preserves positions; background stays behind apps; hidden window recovers; occupied window transfers and releases origin without moving other apps.");
                    }catch(Exception ex){File.WriteAllText(path,"FAIL: "+ex);Environment.ExitCode=1;}
                    board.Close();
                };verify.Start();
            });};
            Application.Run(board);
        }
        Application.ThreadException-=handler;
    }
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool IsIconic(IntPtr window);
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr window);
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr window,uint command);
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr window,out Native.RectangleNormal rect);
    static Rectangle BoundsOf(IntPtr window){Native.RectangleNormal rect;GetWindowRect(window,out rect);return Rectangle.FromLTRB(rect.Left,rect.Top,rect.Right,rect.Bottom);}
    static bool Above(IntPtr app,IntPtr board){for(IntPtr h=GetWindow(board,3);h!=IntPtr.Zero;h=GetWindow(h,3))if(h==app)return true;return false;}
}

