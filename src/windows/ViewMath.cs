using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

static class ViewMath {
    public static Rectangle Follow(Rectangle source,Size size,Rectangle previous,Point cursor) {
        Rectangle initial=Crop(source,size,cursor);
        if(previous.Size!=initial.Size||!source.Contains(previous))return initial;
        int margin=Math.Min(90,Math.Min(previous.Width,previous.Height)/4);
        int x=previous.X,y=previous.Y;
        if(cursor.X<x+margin)x=cursor.X-margin;
        if(cursor.X>=x+previous.Width-margin)x=cursor.X-previous.Width+margin+1;
        if(cursor.Y<y+margin)y=cursor.Y-margin;
        if(cursor.Y>=y+previous.Height-margin)y=cursor.Y-previous.Height+margin+1;
        return new Rectangle(Math.Max(source.Left,Math.Min(source.Right-previous.Width,x)),Math.Max(source.Top,Math.Min(source.Bottom-previous.Height,y)),previous.Width,previous.Height);
    }
    public static Rectangle Crop(Rectangle source,Size size,Point center) {
        int w=Math.Min(source.Width,Math.Max(1,size.Width)),h=Math.Min(source.Height,Math.Max(1,size.Height));
        return new Rectangle(Math.Max(source.Left,Math.Min(source.Right-w,center.X-w/2)),Math.Max(source.Top,Math.Min(source.Bottom-h,center.Y-h/2)),w,h);
    }
    public static void Test() {
        Rectangle source=new Rectangle(-1920,0,1920,1080);
        Rectangle crop=Crop(source,new Size(1040,580),new Point(-10,1070));
        if(crop.Width!=1040||crop.Height!=580||!source.Contains(crop))throw new Exception("1:1 bounds");
        Rectangle fit=Geometry.Fit(source.Size,new Rectangle(0,0,1040,585));
        double compensation=(double)source.Width/fit.Width;
        double screenMovement=40*compensation*fit.Width/source.Width;
        if(Math.Abs(screenMovement-40)>0.001)throw new Exception("pointer scaling");
        Rectangle big=Crop(source,new Size(4000,3000),source.Location);
        if(big!=source)throw new Exception("viewport oversize");
        Rectangle centered=Crop(source,new Size(1040,580),new Point(-1000,500));
        Rectangle still=Follow(source,centered.Size,centered,new Point(centered.X+300,centered.Y+300));
        if(still!=centered)throw new Exception("pointer should move before viewport pans");
    }
}

