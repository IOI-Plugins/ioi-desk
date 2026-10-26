using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

static class Geometry {
    public static Rectangle Fit(Size source,Rectangle area) {
        if(source.Width<=0 || source.Height<=0 || area.Width<=0 || area.Height<=0) return Rectangle.Empty;
        double ratio=Math.Min((double)area.Width/source.Width,(double)area.Height/source.Height);
        int w=Math.Max(1,(int)Math.Round(source.Width*ratio));
        int h=Math.Max(1,(int)Math.Round(source.Height*ratio));
        return new Rectangle(area.X+(area.Width-w)/2,area.Y+(area.Height-h)/2,w,h);
    }
    public static Point ToSource(Point p,Rectangle preview,Rectangle source) {
        int x=(int)Math.Round((double)(p.X-preview.X)*Math.Max(0,source.Width-1)/Math.Max(1,preview.Width-1));
        int y=(int)Math.Round((double)(p.Y-preview.Y)*Math.Max(0,source.Height-1)/Math.Max(1,preview.Height-1));
        return new Point(source.X+Math.Max(0,Math.Min(source.Width-1,x)),source.Y+Math.Max(0,Math.Min(source.Height-1,y)));
    }
    public static Point ToPreview(Point p,Rectangle preview,Rectangle source) {
        return new Point(preview.X+(int)Math.Round((double)(p.X-source.X)*Math.Max(0,preview.Width-1)/Math.Max(1,source.Width-1)),
                         preview.Y+(int)Math.Round((double)(p.Y-source.Y)*Math.Max(0,preview.Height-1)/Math.Max(1,source.Height-1)));
    }
    public static void Test() {
        Rectangle view=Fit(new Size(1920,1080),new Rectangle(0,0,1064,610));
        if(view.Width!=1064 || Math.Abs((double)view.Width/view.Height-16.0/9)>0.004) throw new Exception("aspect");
        Rectangle src=new Rectangle(-1920,-1080,1920,1080);
        Point start=ToSource(view.Location,view,src);
        Point end=ToSource(new Point(view.Right-1,view.Bottom-1),view,src);
        if(start!=src.Location || end!=new Point(-1,-1)) throw new Exception("edge mapping");
        Point q=new Point(view.X+view.Width/2,view.Y+view.Height/2);
        Point r=ToPreview(ToSource(q,view,src),view,src);
        if(Math.Abs(q.X-r.X)>1 || Math.Abs(q.Y-r.Y)>1) throw new Exception("roundtrip");
        if(ToSource(new Point(-9999,-9999),view,src)!=src.Location) throw new Exception("clamp");
        if(Fit(Size.Empty,Rectangle.Empty)!=Rectangle.Empty) throw new Exception("empty");
    }
}

