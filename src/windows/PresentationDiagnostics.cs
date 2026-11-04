using System;
using System.Drawing;
using System.Runtime.InteropServices;

// Local input-routing metadata only. No keys, page text, URLs or wheel direction.
static class PresentationDiagnostics {
    public static void Record(string reason,int token,ushort buttons) {
        try {
            Native.GuiThreadInfo info=new Native.GuiThreadInfo();info.size=(uint)Marshal.SizeOf(info);
            bool gui=Native.GetGUIThreadInfo(0,ref info);
            Native.RectangleRect clip;bool clipped=Native.GetClipCursor(out clip);
            Point cursor=System.Windows.Forms.Cursor.Position;
            IntPtr foreground=Native.GetForegroundWindow(),hit=Native.WindowFromPoint(cursor);
            uint foregroundPid=0,hitPid=0;
            if(foreground!=IntPtr.Zero)Native.GetWindowThreadProcessId(foreground,out foregroundPid);
            if(hit!=IntPtr.Zero)Native.GetWindowThreadProcessId(hit,out hitPid);
            TraceLog.Write("control.routing","","reason",reason,"token",token.ToString(),
                "foreground",foreground.ToInt64().ToString(),"foregroundPid",foregroundPid.ToString(),
                "pointerWindow",hit.ToInt64().ToString(),"pointerPid",hitPid.ToString(),
                "guiAvailable",gui.ToString(),"focus",info.focus.ToInt64().ToString(),
                "capture",info.capture.ToInt64().ToString(),"guiFlags",info.flags.ToString(),
                "primaryDown",InputSafety.PrimaryButtonDown.ToString(),"rawFlags",buttons.ToString(),
                "clipAvailable",clipped.ToString(),"clip",clip.Left+","+clip.Top+","+clip.Right+","+clip.Bottom);
        }catch{} // An unavailable window must not affect input.
    }
}
