using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

// Original WinSparkle provides scheduling, signature validation and installer UI.
static class DeskUpdates {
    static bool started;
    static Control dispatcher;
    static Action quit;
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int CanShutdown();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Shutdown();
    static readonly CanShutdown canShutdown=delegate{return dispatcher!=null&&!dispatcher.IsDisposed?1:0;};
    static readonly Shutdown shutdown=delegate{if(dispatcher!=null&&!dispatcher.IsDisposed)try{dispatcher.BeginInvoke((Action)delegate{if(quit!=null)quit();});}catch(InvalidOperationException){}};
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern IntPtr LoadLibraryEx(string path,IntPtr file,uint flags);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern int GetCurrentPackageFullName(ref uint size,StringBuilder name);
    [DllImport("WinSparkle.dll",CallingConvention=CallingConvention.Cdecl)] static extern void win_sparkle_set_appcast_url([MarshalAs(UnmanagedType.LPStr)]string url);
    [DllImport("WinSparkle.dll",CallingConvention=CallingConvention.Cdecl)] static extern int win_sparkle_set_eddsa_public_key([MarshalAs(UnmanagedType.LPStr)]string key);
    [DllImport("WinSparkle.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Unicode)] static extern void win_sparkle_set_app_details(string company,string app,string version);
    [DllImport("WinSparkle.dll",CallingConvention=CallingConvention.Cdecl,CharSet=CharSet.Unicode)] static extern void win_sparkle_set_app_build_version(string build);
    [DllImport("WinSparkle.dll",CallingConvention=CallingConvention.Cdecl)] static extern void win_sparkle_set_automatic_check_for_updates(int enabled);
    [DllImport("WinSparkle.dll",CallingConvention=CallingConvention.Cdecl)] static extern void win_sparkle_set_can_shutdown_callback(CanShutdown callback);
    [DllImport("WinSparkle.dll",CallingConvention=CallingConvention.Cdecl)] static extern void win_sparkle_set_shutdown_request_callback(Shutdown callback);
    [DllImport("WinSparkle.dll",CallingConvention=CallingConvention.Cdecl)] static extern void win_sparkle_init();
    [DllImport("WinSparkle.dll",CallingConvention=CallingConvention.Cdecl)] static extern void win_sparkle_cleanup();
    [DllImport("WinSparkle.dll",CallingConvention=CallingConvention.Cdecl)] static extern void win_sparkle_check_update_with_ui();
    internal static bool Packaged {get{uint size=0;return GetCurrentPackageFullName(ref size,null)!=15700;}}
    public static void Start(Control owner,Action close) {
        if(started||Packaged)return;
        dispatcher=owner;quit=close;
        try {
            string directory=Path.GetDirectoryName(Application.ExecutablePath);
            if(LoadLibraryEx(Path.Combine(directory,"WinSparkle.dll"),IntPtr.Zero,0x00000100|0x00001000)==IntPtr.Zero)throw new Win32Exception(Marshal.GetLastWin32Error());
            if(Convert.FromBase64String(UpdateConfig.PublicKey).Length!=32)throw new InvalidOperationException("Invalid updater public key");
            win_sparkle_set_app_details("IOI","IOI Desk",Product.Version);
            win_sparkle_set_app_build_version(UpdateConfig.Build);
            win_sparkle_set_appcast_url(UpdateConfig.WindowsFeed);
            if(win_sparkle_set_eddsa_public_key(UpdateConfig.PublicKey)!=1)throw new InvalidOperationException("Updater key rejected");
            win_sparkle_set_can_shutdown_callback(canShutdown);
            win_sparkle_set_shutdown_request_callback(shutdown);
            // Default to checks once; do not overwrite the user's later choice.
            if(Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\IOI\IOI Desk\WinSparkle","CheckForUpdates",null)==null)win_sparkle_set_automatic_check_for_updates(1);
            win_sparkle_init();started=true;
        }catch(Exception ex){TraceLog.Write("update.init.failed","","error",ex.Message);}
    }
    public static void Check() {
        if(Packaged){MessageBox.Show("As atualizações desta instalação são gerenciadas pelo Windows.","IOI Desk");return;}
        if(started)win_sparkle_check_update_with_ui();
        else MessageBox.Show("O componente de atualização não está disponível. Reinstale o pacote oficial completo.","IOI Desk");
    }
    public static void Stop(){if(started){win_sparkle_cleanup();started=false;}dispatcher=null;quit=null;}
    public static void SetOwner(Control owner){dispatcher=owner;}
    public static void Test(string report){
        Uri feed=new Uri(UpdateConfig.WindowsFeed);
        if(feed.Scheme!="https"||feed.Host!="github.com"||Convert.FromBase64String(UpdateConfig.PublicKey).Length!=32)throw new Exception("Invalid update trust configuration");
        string directory=Path.GetDirectoryName(Application.ExecutablePath);
        if(LoadLibraryEx(Path.Combine(directory,"WinSparkle.dll"),IntPtr.Zero,0x00000100|0x00001000)==IntPtr.Zero)throw new Win32Exception(Marshal.GetLastWin32Error());
        if(win_sparkle_set_eddsa_public_key(UpdateConfig.PublicKey)!=1)throw new Exception("Original WinSparkle rejected release public key");
        if(win_sparkle_set_eddsa_public_key("invalid")!=0)throw new Exception("Original WinSparkle accepted malformed public key");
        File.WriteAllText(report,"PASS: original WinSparkle loads from application directory, accepts release public key and rejects a malformed key; HTTPS feed configured. No network check or installation performed.");
    }
}
