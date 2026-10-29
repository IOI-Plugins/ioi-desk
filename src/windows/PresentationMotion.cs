using System;
using System.Drawing;

// Native clicks, tab drags, window moves and menus own their pointer movement.
static class PresentationMotion {
    public static bool Compensate(bool buttonDown,bool nativeInteraction,ushort buttons) {
        return !buttonDown&&!nativeInteraction&&(buttons&0x0f)==0;
    }
    public static Point Move(Point actual,int dx,int dy,double gain,Rectangle bounds) {
        // Windows already applied its native movement. Never accumulate a stale
        // absolute position across a drag or another application's cursor warp.
        int x=(int)Math.Round(actual.X+dx*(gain-1)),y=(int)Math.Round(actual.Y+dy*(gain-1));
        return new Point(Math.Max(bounds.Left,Math.Min(bounds.Right-1,x)),Math.Max(bounds.Top,Math.Min(bounds.Bottom-1,y)));
    }
}
