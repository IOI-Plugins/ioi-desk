using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

class WindowChoice {
    public IntPtr Handle;public int Pid;readonly string title;
    public WindowChoice(IntPtr h,int p,string t){Handle=h;Pid=p;title=t;}
    public override string ToString(){return title;}
    public string Title {get{return title;}}
}
class BufferedPanel : Panel {public BufferedPanel(){DoubleBuffered=true;ResizeRedraw=true;}}
class SavedWindow {public IntPtr Handle;public uint Pid;public Native.WINDOWPLACEMENT Placement;}
