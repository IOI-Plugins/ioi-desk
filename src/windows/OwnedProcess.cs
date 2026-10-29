using System;
using System.Diagnostics;

static class OwnedProcess {
    // A replacement must not start until this exact owned process has exited.
    public static bool Stop(Process process,int timeout) {
        if(process==null)return true;
        try {if(!process.HasExited)process.Kill();return process.WaitForExit(timeout);}
        catch(InvalidOperationException){return true;}
        catch(Exception ex){TraceLog.Write("deskflow.stop.failed","","error",ex.Message);return false;}
    }
}
