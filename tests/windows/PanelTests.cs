using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

static class PanelTests {
    public static string TracePath;
    public static void Trace(string message){if(TracePath!=null)File.AppendAllText(TracePath,Environment.NewLine+message);}
    public static void Run(string report) {
        TracePath=report;
        File.WriteAllText(report,"Starting viewer constructor");
        Viewer root=new Viewer();WorkspaceContext context=new WorkspaceContext(root);
        File.WriteAllText(report,"Root shown; scheduling test");
        string result="";Exception observed=null;
        System.Threading.ThreadExceptionEventHandler errors=delegate(object sender,System.Threading.ThreadExceptionEventArgs args){observed=args.Exception;Trace(args.Exception.ToString());Application.ExitThread();};
        Application.ThreadException+=errors;
        root.BeginInvoke((Action)delegate {
            File.WriteAllText(report,"Opening second viewer");
            try {
                List<Screen> available=PanelWindows.VirtualScreens();if(available.Count==0)throw new Exception("no virtual screen");
                // Deliberately reuse one source: tests coordination, not driver creation.
                Viewer child=context.Open(available[0]);
                child.BeginInvoke((Action)delegate {
                    if(PanelInput.Views.Count!=2||!PanelInput.Ready){result="FAIL: two initialized panels / hotkeys";MouseExitNative.PostMessage(root.Handle,0x10,IntPtr.Zero,IntPtr.Zero);MouseExitNative.PostMessage(child.Handle,0x10,IntPtr.Zero,IntPtr.Zero);return;}
                    MouseExitNative.PostMessage(root.Handle,0x10,IntPtr.Zero,IntPtr.Zero);
                    PanelInput.ScheduleTest(delegate {
                        try {
                            if(PanelInput.Views.Count!=1||PanelInput.Owner!=child||!PanelInput.Ready)throw new Exception("owner handoff");
                            result="PASS: two initialized viewer forms share global hotkeys; closing the first preserves the second. Test uses one virtual source, not two independent monitors.";
                        }catch(Exception ex){result="FAIL: "+ex;}
                        MouseExitNative.PostMessage(child.Handle,0x10,IntPtr.Zero,IntPtr.Zero);
                    });
                });
            }catch(Exception ex){result="FAIL: "+ex;Trace(result);foreach(Viewer view in new List<Viewer>(PanelInput.Views))MouseExitNative.PostMessage(view.Handle,0x10,IntPtr.Zero,IntPtr.Zero);}
        });
        File.WriteAllText(report,"Entering message loop");
        Application.Run(context);Application.ThreadException-=errors;if(observed!=null)result="FAIL: "+observed;File.WriteAllText(report,result);if(!result.StartsWith("PASS"))Environment.ExitCode=1;
    }
}
