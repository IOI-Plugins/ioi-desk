using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

// Independent Deskflow process; pairing material stays in the user's local data.
static class ComputerSharing {
    static SharingForm form;
    public static readonly string Data=AppPaths.Connection;
    public static void Open(){Ensure();form.Show();form.Activate();}
    static void Ensure(){if(form==null||form.IsDisposed)form=new SharingForm();}
    public static void AutoStart(){if(File.Exists(Path.Combine(Data,"enabled.txt"))){Ensure();form.StartConnection();}}
    public static void Stop(){if(form!=null)form.StopConnection(false);}
    public static string EngineFolder {get {
        string bundled=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"vendor","deskflow");
        return File.Exists(Path.Combine(bundled,"deskflow-core.exe"))?bundled:Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"Deskflow");
    }}
}
sealed class SharingForm : Form {
    readonly ComboBox side=new ComboBox();
    readonly Label status=new Label();
    readonly Timer check=new Timer{Interval=1000};
    Process engine;
    string data {get{return ComputerSharing.Data;}}
    public SharingForm():this(true){}
    public SharingForm(bool keepInTray){
        Text="IOI Desk · Mouse e teclado";ClientSize=new Size(540,290);MinimumSize=Size;MaximumSize=Size;StartPosition=FormStartPosition.CenterScreen;
        BackColor=Color.FromArgb(24,31,43);ForeColor=Color.White;Font=new Font("Segoe UI",10);
        Controls.Add(new Label{Text="Windows → Mac mini",Font=new Font(Font,FontStyle.Bold),Location=new Point(20,18),Size=new Size(490,28)});
        Controls.Add(new Label{Text="Use o mouse e o teclado do Windows nos dois computadores.\nO Mac mini já está pareado com esta instalação.",Location=new Point(20,54),Size=new Size(490,48)});
        Controls.Add(new Label{Text="O Mac fica:",Location=new Point(20,118),Size=new Size(104,26)});
        side.DropDownStyle=ComboBoxStyle.DropDownList;side.Items.AddRange(new object[]{"À direita do Windows","À esquerda do Windows"});side.SetBounds(126,114,255,30);Controls.Add(side);
        side.SelectedIndex=File.Exists(Path.Combine(data,"side.txt"))&&File.ReadAllText(Path.Combine(data,"side.txt")).Trim()=="left"?1:0;
        side.SelectedIndexChanged+=delegate{bool running=engine!=null&&!engine.HasExited;if(running)StopConnection(false);SaveConfiguration();if(running)StartConnection();};
        Button connect=new Button{Text="Conectar",Location=new Point(20,160),Size=new Size(145,34)};connect.Click+=delegate{StartConnection();};Controls.Add(connect);
        Button stop=new Button{Text="Desconectar",Location=new Point(178,160),Size=new Size(145,34)};stop.Click+=delegate{StopConnection(true);};Controls.Add(stop);
        Button logs=new Button{Text="Diagnóstico",Location=new Point(336,160),Size=new Size(145,34)};logs.Click+=delegate{Directory.CreateDirectory(TraceLog.Folder);Process.Start(new ProcessStartInfo(TraceLog.Folder){UseShellExecute=true});};Controls.Add(logs);
        status.SetBounds(20,206,490,64);status.Text="Ctrl + Alt + →: Mac · Ctrl + Alt + ←: Windows\nA conexão exige que o IOI Desk também esteja aberto no Mac.";Controls.Add(status);
        check.Tick+=delegate{if(engine!=null&&engine.HasExited){int code=engine.ExitCode;TraceLog.Write("deskflow.exit","","code",code.ToString());engine.Dispose();engine=null;check.Stop();SetStatus("Componente do Windows encerrado · código "+code+". Consulte Diagnóstico.");}};
        FormClosing+=delegate(object sender,FormClosingEventArgs e){if(keepInTray&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();}};
        FormClosed+=delegate{StopConnection(false);check.Dispose();};
        // Create the handle on the UI thread before output callbacks arrive.
        IntPtr handle=Handle;
    }
    void SaveConfiguration(){
        Directory.CreateDirectory(data);
        string direction=side.SelectedIndex==1?"left":"right",reverse=direction=="left"?"right":"left";
        File.WriteAllText(Path.Combine(data,"side.txt"),direction);
        string server="section: screens\n  IOI-Windows:\n  IOI-Mac-mini:\nend\nsection: links\n  IOI-Windows:\n    "+direction+" = IOI-Mac-mini\n  IOI-Mac-mini:\n    "+reverse+" = IOI-Windows\nend\nsection: options\n  switchDelay = 0\n  switchDoubleTap = 0\n  keystroke(Control+Alt+Right) = switchToScreen(IOI-Mac-mini)\n  keystroke(Control+Alt+Left) = switchToScreen(IOI-Windows)\nend\n";
        server=server.Replace("section: screens\n  IOI-Windows:\n  IOI-Mac-mini:\nend\n","");
        File.WriteAllText(Path.Combine(data,"server.conf"),server);
        string path=data.Replace('\\','/');
        string addressFile=Path.Combine(data,"server-address.txt");
        string address=File.Exists(addressFile)?File.ReadAllText(addressFile).Trim():"";
        System.Net.IPAddress parsed;if(address.Length>0&&!System.Net.IPAddress.TryParse(address,out parsed))throw new InvalidDataException("Endereço local inválido.");
        string settings="[core]\ncomputerName=IOI-Windows\ncoreMode=2\nprocessMode=1\nport=24800\ninterface="+address+"\n[security]\ntlsEnabled=true\ncheckPeerFingerprints=true\ncertificate="+path+"/tls/deskflow.pem\n[server]\nexternalConfig=true\nexternalConfigFile="+path+"/server.conf\nprotocol=barrier\nenableClipboard=false\nenableSwitchDelay=false\nenableSwitchDoubleTap=false\n[log]\nlevel=Info\ntoFile=false\n";
        settings+="[computer_IOI-Windows]\nname=IOI-Windows\n[computer_IOI-Mac-mini]\nname=IOI-Mac-mini\n";
        File.WriteAllText(Path.Combine(data,"Deskflow.conf"),settings);
    }
    public void StartConnection(){
        if(engine!=null&&!engine.HasExited){SetStatus("Conexão já iniciada. Aguardando ou conectado ao Mac mini.");return;}
        if(!File.Exists(Path.Combine(data,"tls","deskflow.pem"))||!File.Exists(Path.Combine(data,"tls","trusted-clients"))){SetStatus("O pareamento deste computador ainda precisa ser concluído.");return;}
        try{
            SaveConfiguration();
            ProcessStartInfo info=new ProcessStartInfo(Path.Combine(ComputerSharing.EngineFolder,"deskflow-core.exe"),"server -s \""+Path.Combine(data,"Deskflow.conf")+"\"");
            info.WorkingDirectory=ComputerSharing.EngineFolder;info.UseShellExecute=false;info.CreateNoWindow=true;info.RedirectStandardOutput=true;info.RedirectStandardError=true;
            engine=new Process{StartInfo=info};engine.OutputDataReceived+=Log;engine.ErrorDataReceived+=Log;engine.Start();engine.BeginOutputReadLine();engine.BeginErrorReadLine();
            File.WriteAllText(Path.Combine(data,"enabled.txt"),"true");
            check.Start();SetStatus("Aguardando o Mac mini · conexão criptografada.\nCtrl + Alt + →: Mac · Ctrl + Alt + ←: Windows");
        }catch(Exception ex){if(engine!=null){engine.Dispose();engine=null;}SetStatus("Não foi possível conectar: "+ex.Message);}
    }
    void SetStatus(string text){if(IsDisposed)return;if(InvokeRequired){try{BeginInvoke((Action)delegate{SetStatus(text);});}catch(InvalidOperationException){}return;}status.Text=text;}
    void Log(object sender,DataReceivedEventArgs args){
        if(args.Data==null)return;TraceLog.Write("deskflow.output","","message",args.Data);
        string line=args.Data.ToLowerInvariant();
        if(line.Contains("disconnected"))SetStatus("Mac mini desconectado. Aguardando reconexão.");
        else if(line.Contains("accepted secure socket"))SetStatus("Conexão segura aceita do Mac mini.\nSe o controle não responder, confira as permissões do IOI Desk no Mac.");
        else if(line.Contains("has connected"))SetStatus("Mac mini conectado · mouse e teclado disponíveis.\nCtrl + Alt + →: Mac · Ctrl + Alt + ←: Windows");
        else if(line.Contains("failed")||line.Contains("error")||line.Contains("fatal"))SetStatus("A conexão precisa de atenção. Consulte Diagnóstico.");
    }
    public void StopConnection(bool disable){
        if(disable&&File.Exists(Path.Combine(data,"enabled.txt")))File.Delete(Path.Combine(data,"enabled.txt"));
        if(engine!=null){try{if(!engine.HasExited)engine.Kill();}catch{}engine.Dispose();engine=null;}
        check.Stop();SetStatus("Desconectado. Mouse e teclado permanecem no Windows.");
    }
}
