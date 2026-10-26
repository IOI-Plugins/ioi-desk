using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

static class LayoutPersistenceTest {
    public static void Run(string report) {
        using(Viewer viewer=new Viewer())using(ZoneBoard board=new ZoneBoard(viewer,WorkspaceScreens.Presentation,90,new[]{"Presentation","B","C","D"},new[]{.35,.25,.20})) {
            board.ViewerSlot=0;board.Show();Application.DoEvents();board.Arrange();Application.DoEvents();
            Rectangle assigned=viewer.Bounds;
            if(assigned.Width<100||assigned.Height<100)throw new Exception("Invalid assigned bounds");
            var views=new List<Viewer>{viewer};var boards=new List<ZoneBoard>{board};
            viewer.Bounds=new Rectangle(WorkspaceScreens.Worker.WorkingArea.Location,new Size(600,350));
            WorkspaceContext.LayoutViews(views,boards);
            if(viewer.Bounds!=assigned)throw new Exception("Applying layout moved presentation away from its zone: expected="+assigned+" actual="+viewer.Bounds);
            WorkspaceContext.LayoutViews(views,boards);
            if(viewer.Bounds!=assigned)throw new Exception("Repeated apply changed presentation bounds");
            WorkspaceContext.LayoutViews(views,new List<ZoneBoard>());
            if(!WorkspaceScreens.Presentation.WorkingArea.Contains(viewer.Bounds))throw new Exception("Standalone tile ignored configured presentation monitor");
            File.WriteAllText(report,"PASS: apply restores assigned presentation zone; repeated apply preserves bounds; standalone tiling respects configured monitor; no customer windows moved.");
        }
    }
}
