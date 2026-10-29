using System;
using System.Drawing;
using System.IO;

static class PresentationMotionTest {
    public static void Run(string report){
        if(PresentationMotion.Compensate(true,false,0)||PresentationMotion.Compensate(false,true,0))throw new Exception("Native drags/capture would be warped");
        foreach(ushort flag in new ushort[]{1,2,4,8})if(PresentationMotion.Compensate(false,false,flag))throw new Exception("A button transition would be warped");
        if(!PresentationMotion.Compensate(false,false,0))throw new Exception("Free pointer motion disabled");
        Rectangle bounds=new Rectangle(-1920,0,1920,1080);
        if(PresentationMotion.Move(new Point(-700,35),10,-10,1,bounds)!=new Point(-700,35))throw new Exception("Native gain altered tab coordinates");
        if(PresentationMotion.Move(new Point(-700,35),10,-10,2,bounds)!=new Point(-690,25))throw new Exception("Compensation did not follow actual post-drag cursor");
        if(PresentationMotion.Move(new Point(-1,0),40,-40,2,bounds)!=new Point(-1,0))throw new Exception("Top/right boundary escaped virtual screen");
        long now=DateTime.UtcNow.Ticks;
        if(InputSafety.Expired(now,now)||!InputSafety.Expired(now,now-TimeSpan.FromSeconds(4).Ticks))throw new Exception("UI stall safety deadline");
        if(InputSafety.AcceptExit(12,13)||!InputSafety.AcceptExit(13,13))throw new Exception("Delayed exit from previous session would cancel re-entry");
        File.WriteAllText(report,"PASS: tab/button transitions, native capture and menu/window drags never warp; movement resumes from real coordinates; negative display/top edge and stalled-UI timeout covered.");
    }
}
