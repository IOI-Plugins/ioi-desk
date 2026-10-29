using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

static class ConnectionLifecycleTest {
    public static void Child(string path){
        TcpListener listener=new TcpListener(IPAddress.Loopback,int.Parse(File.ReadAllText(path)));
        listener.Start();File.WriteAllText(path+".ready","ready");Thread.Sleep(20000);listener.Stop();
    }
    public static void Run(string report){
        string folder=Path.Combine(Path.GetTempPath(),"IOIDesk-connection-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        string input=Path.Combine(folder,"port");Process child=null;
        try{
            TcpListener reserve=new TcpListener(IPAddress.Loopback,0);reserve.Start();int port=((IPEndPoint)reserve.LocalEndpoint).Port;reserve.Stop();File.WriteAllText(input,port.ToString());
            for(int cycle=0;cycle<2;cycle++){
                if(File.Exists(input+".ready"))File.Delete(input+".ready");
                child=Process.Start(new ProcessStartInfo(Application.ExecutablePath,"--connection-fixture \""+input+"\""){UseShellExecute=false,CreateNoWindow=true});
                Stopwatch timeout=Stopwatch.StartNew();while(!File.Exists(input+".ready")&&timeout.ElapsedMilliseconds<5000&&!child.HasExited)Thread.Sleep(10);
                if(!File.Exists(input+".ready"))throw new Exception("Fixture failed to acquire its isolated loopback port");
                if(!OwnedProcess.Stop(child,1500)||!child.HasExited)throw new Exception("Replacement could start before previous process exited");
                child.Dispose();child=null;
                TcpListener probe=new TcpListener(IPAddress.Loopback,port);probe.Start();probe.Stop();
            }
            File.WriteAllText(report,"PASS: stop/reconnect twice releases the exact owned process and loopback port before replacement; no real Deskflow or user connection touched.");
        }finally{
            if(child!=null){OwnedProcess.Stop(child,1500);child.Dispose();}
            foreach(string path in new[]{input,input+".ready"})if(File.Exists(path))File.Delete(path);
            Directory.Delete(folder);
        }
    }
}
