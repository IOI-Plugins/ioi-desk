using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
static class GridTests {
    public static void Run(string path){
        using(Viewer viewer=new Viewer())using(ZoneBoard board=new ZoneBoard(viewer,Screen.PrimaryScreen,99,new string[]{"Code","Terminal","AI","Teams"},new double[]{.17,.32,.345},true)){
            board.Shown+=delegate{board.BeginInvoke((Action)delegate{
                try{
                    if(board.SlotCount<7)throw new Exception("Missing subdivisions.");
                    Rectangle outer=board.Bounds;
                    for(int i=0;i<board.SlotCount;i++){
                        if(!board.SlotEnabled(i))continue;
                        Rectangle cell=board.ZoneBounds(i);if(!outer.Contains(cell)||cell.Width<45||cell.Height<45)throw new Exception("Cell outside monitor: "+i+" "+cell);
                        for(int j=i+1;j<board.SlotCount;j++)if(board.SlotEnabled(j)&&cell.IntersectsWith(board.ZoneBounds(j)))throw new Exception("Cells overlap: "+i+","+j);
                    }
                    if(board.SlotEnabled(4)){if(board.SlotEnabled(5)||board.ZoneBounds(1).Right>=board.ZoneBounds(4).Left||board.ZoneBounds(1).Y!=board.ZoneBounds(4).Y||board.ContentBounds(1).Width<478||board.ContentBounds(4).Width<478)throw new Exception("Two terminal columns do not respect native minimum width.");}
                    else {TerminalGrid.Test();if(board.ZoneBounds(1).Height<500)throw new Exception("Terminal matrix too short.");}
                    if(board.ZoneBounds(3).Right>=board.ZoneBounds(6).Left||board.ZoneBounds(3).Y!=board.ZoneBounds(6).Y)throw new Exception("Communication columns not separated.");
                    if(board.SlotCount==8&&(board.ZoneBounds(2).Right>=board.ZoneBounds(7).Left||board.ZoneBounds(2).Y!=board.ZoneBounds(7).Y))throw new Exception("AI columns not separated.");
                    if(WindowLimits.Fits(new Size(616,408),new Rectangle(0,0,531,608)))throw new Exception("Oversized Claude window incorrectly allowed.");
                    TerminalGrid.Test();
                    using(Form closed=new Form()){WindowChoice dead=new WindowChoice(closed.Handle,System.Diagnostics.Process.GetCurrentProcess().Id,"Closed test");board.Set(1,dead);closed.Dispose();board.ClearClosedAssignments();if(!board.Empty(1))throw new Exception("Closed terminal remained assigned");}
                    File.WriteAllText(path,"PASS: two independent terminal columns respect 478px native minimum; third slot disabled; active cells do not overlap; closed window clears assignment.");
                }catch(Exception ex){File.WriteAllText(path,"FAIL: "+ex);Environment.ExitCode=1;}
                board.Close();
            });};Application.Run(board);
        }
    }
}
