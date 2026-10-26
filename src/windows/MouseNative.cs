using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

static class MouseNative {
    [StructLayout(LayoutKind.Sequential)] internal struct DEVICE {public ushort page,usage;public uint flags;public IntPtr target;}
    [DllImport("user32.dll")] internal static extern bool RegisterRawInputDevices(DEVICE[] devices,uint count,uint size);
}
