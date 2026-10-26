using System;
using System.IO;
using System.Windows.Forms;

static class WorkspaceScreens {
    public static bool Swapped {get{return File.Exists(Path.Combine(AppPaths.Workspace,"inverter-monitores.flag"));}}
    public static Screen Presentation {get{return Swapped?Screen.PrimaryScreen:(Native.FindPhysicalSecondary()??Screen.PrimaryScreen);}}
    public static Screen Worker {get{return Swapped?(Native.FindPhysicalSecondary()??Screen.PrimaryScreen):Screen.PrimaryScreen;}}
}
