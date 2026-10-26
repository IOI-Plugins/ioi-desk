using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

class ScreenChoice {
    public readonly Screen Screen;public ScreenChoice(Screen screen){Screen=screen;}
    public override string ToString(){return Screen.DeviceName.Replace("\\\\.\\","")+"  ·  "+Screen.Bounds.Width+" × "+Screen.Bounds.Height;}
}

static class PanelWindows {
    public static List<Screen> VirtualScreens(){List<Screen> screens=new List<Screen>();foreach(Screen s in Screen.AllScreens)if(!s.Primary&&Native.IsVirtual(s))screens.Add(s);screens.Sort((a,b)=>string.CompareOrdinal(a.DeviceName,b.DeviceName));return screens;}
    public static void OpenDriver() {
        foreach(Process p in Process.GetProcesses()){try{if(p.ProcessName=="VDD Control"&&p.MainWindowHandle!=IntPtr.Zero){Native.ShowWindow(p.MainWindowHandle,9);Native.SetForegroundWindow(p.MainWindowHandle);return;}}catch{}finally{p.Dispose();}}
        string setting=Path.Combine(AppPaths.Root,"driver-path.txt");
        string path=File.Exists(setting)?File.ReadAllText(setting).Trim():"";
        if(path.Length>0&&Path.IsPathRooted(path)&&Path.GetFileName(path).Equals("VDD Control.exe",StringComparison.OrdinalIgnoreCase)&&File.Exists(path))Process.Start(new ProcessStartInfo(path){UseShellExecute=true});
        else MessageBox.Show("Abra o Virtual Driver Control instalado para ativar outra tela virtual.","Adicionar tela",MessageBoxButtons.OK,MessageBoxIcon.Information);
    }
}

class ChromeIcon : Button {
    readonly string kind;
    public ChromeIcon(string icon,string name) {
        kind=icon;AccessibleName=name;FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;BackColor=Color.FromArgb(24,31,43);ForeColor=Color.FromArgb(201,211,226);Size=new Size(28,26);Cursor=Cursors.Hand;TabStop=true;
        FlatAppearance.MouseOverBackColor=Color.FromArgb(47,60,80);
    }
    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        float cx=Width/2f,cy=Height/2f;
        using(Pen p=new Pen(ForeColor,1.35f)) {
            if(kind=="plus"){e.Graphics.DrawLine(p,cx-5,cy,cx+5,cy);e.Graphics.DrawLine(p,cx,cy-5,cx,cy+5);}
            else if(kind=="close"){e.Graphics.DrawLine(p,cx-4,cy-4,cx+4,cy+4);e.Graphics.DrawLine(p,cx+4,cy-4,cx-4,cy+4);}
            else if(kind=="min"){e.Graphics.DrawLine(p,cx-5,cy+1,cx+5,cy+1);}
            else if(kind=="apps"){for(int y=-1;y<=1;y+=2)for(int x=-1;x<=1;x+=2)e.Graphics.DrawRectangle(p,cx+x*4-2,cy+y*4-2,4,4);}
            else if(kind=="tile"){e.Graphics.DrawRectangle(p,cx-6,cy-6,12,12);e.Graphics.DrawLine(p,cx-6,cy,cx+6,cy);}
            else if(kind=="settings"){e.Graphics.DrawEllipse(p,cx-4,cy-4,8,8);for(int i=0;i<8;i++){double a=i*Math.PI/4;e.Graphics.DrawLine(p,cx+(float)Math.Cos(a)*5,cy+(float)Math.Sin(a)*5,cx+(float)Math.Cos(a)*7,cy+(float)Math.Sin(a)*7);}}
            else if(kind=="more"){for(int i=-1;i<=1;i++)e.Graphics.FillEllipse(Brushes.LightGray,cx+i*4-1,cy-1,2,2);}
            else if(kind=="play"){e.Graphics.DrawPolygon(p,new[]{new PointF(cx-3,cy-5),new PointF(cx+5,cy),new PointF(cx-3,cy+5)});}
        }
    }
}

