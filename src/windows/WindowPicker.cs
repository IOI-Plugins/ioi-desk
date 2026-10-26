using System;
using System.Drawing;
using System.Windows.Forms;

static class WindowPicker {
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool IsWindowEnabled(IntPtr window);
    public static void Test(string report){
        using(Form owner=new Form{Text="IOI Desk picker test"}){
            owner.Shown+=delegate{owner.BeginInvoke((Action)delegate{
                try{for(int round=0;round<2;round++)using(ContextMenuStrip actions=new ContextMenuStrip())using(Timer cancel=new Timer{Interval=100}){
                    bool checkedDialog=false;actions.Items.Add("Janela de teste");
                    cancel.Tick+=delegate{cancel.Stop();Form dialog=null;foreach(Form f in Application.OpenForms)if(f.Text=="IOI Desk — escolher janela")dialog=f;if(dialog==null)throw new Exception("Picker missing");checkedDialog=dialog.TopMost&&!IsWindowEnabled(owner.Handle);dialog.Close();};
                    cancel.Start();Show(owner,actions,new Point(20,20));if(!checkedDialog||!IsWindowEnabled(owner.Handle))throw new Exception("Picker did not release owner after cancellation");
                }System.IO.File.WriteAllText(report,"PASS: picker stays above apps; cancellation releases owner; reopening and closing works.");}
                catch(Exception ex){System.IO.File.WriteAllText(report,"FAIL: "+ex);Environment.ExitCode=1;}finally{owner.Close();}
            });};Application.Run(owner);
        }
    }
    sealed class Choice {
        public ToolStripItem Action;
        public override string ToString(){ToolStripMenuItem item=Action as ToolStripMenuItem;return (item!=null&&item.Checked?"✓  ":"    ")+Action.Text;}
    }
    public static void Show(Form owner,ContextMenuStrip actions,Point location){
        using(Form picker=new Form()){
            picker.Text="IOI Desk — escolher janela";picker.FormBorderStyle=FormBorderStyle.FixedToolWindow;picker.ShowInTaskbar=false;picker.StartPosition=FormStartPosition.Manual;picker.TopMost=true;picker.KeyPreview=true;
            picker.KeyDown+=delegate(object sender,KeyEventArgs e){if(e.KeyCode==Keys.Escape){e.Handled=true;e.SuppressKeyPress=true;picker.Close();}};
            picker.ClientSize=new Size(730,350);picker.BackColor=Color.FromArgb(24,31,43);picker.ForeColor=Color.White;picker.Font=new Font("Segoe UI",10);
            Rectangle area=Screen.FromPoint(location).WorkingArea;picker.Location=new Point(Math.Max(area.Left,Math.Min(location.X,area.Right-picker.Width)),Math.Max(area.Top,Math.Min(location.Y,area.Bottom-picker.Height)));
            ListBox windows=new ListBox{Dock=DockStyle.Fill,BorderStyle=BorderStyle.None,BackColor=Color.FromArgb(24,31,43),ForeColor=Color.White,DrawMode=DrawMode.OwnerDrawFixed,ItemHeight=28,HorizontalScrollbar=true,IntegralHeight=false,AccessibleName="Janelas disponíveis"};
            foreach(ToolStripItem item in actions.Items)if(!(item is ToolStripSeparator))windows.Items.Add(new Choice{Action=item});
            windows.DrawItem+=delegate(object sender,DrawItemEventArgs e){if(e.Index<0)return;e.DrawBackground();Choice entry=(Choice)windows.Items[e.Index];TextRenderer.DrawText(e.Graphics,entry.ToString(),picker.Font,e.Bounds,entry.Action.Enabled?Color.White:Color.Gray,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);};
            Action choose=delegate{Choice choice=windows.SelectedItem as Choice;if(choice==null||!choice.Action.Enabled)return;choice.Action.PerformClick();picker.Close();};
            windows.MouseClick+=delegate(object sender,MouseEventArgs e){int row=windows.IndexFromPoint(e.Location);if(row>=0){windows.SelectedIndex=row;choose();}};
            windows.KeyDown+=delegate(object sender,KeyEventArgs e){if(e.KeyCode==Keys.Enter){e.Handled=true;e.SuppressKeyPress=true;choose();}else if(e.KeyCode==Keys.Escape){e.Handled=true;picker.Close();}};
            picker.Controls.Add(windows);picker.Shown+=delegate{picker.Activate();windows.Focus();};picker.ShowDialog(owner);
        }
    }
}
