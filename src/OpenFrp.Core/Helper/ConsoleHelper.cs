using OpenFrp.Core.Libraries.Api;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenFrp.Core.Helper
{
    public class ConsoleHelper
    {

        public class ConsoleWrapper
        {
            public Libraries.Api.Models.ResponseBody.UserTunnelsResponse.UserTunnel? Tunnel { get; set; }

            public Process? Process { get; set; }
        }

        public static Dictionary<int, ConsoleWrapper> Wrappers = new();

        public static bool Launch(Libraries.Api.Models.ResponseBody.UserTunnelsResponse.UserTunnel tunnel)
        {
            if (ApiRequest.HasAccount)
            {
                try
                {
                    Process process = new()
                    {
                        EnableRaisingEvents = true,
                        StartInfo = new()
                        {
                            UseShellExecute = false,
                            CreateNoWindow = false,
                            RedirectStandardError = true,
                            RedirectStandardOutput = true,
                            StandardOutputEncoding = Encoding.UTF8,
                            StandardErrorEncoding = Encoding.UTF8,
                            FileName = Utils.Frpc,
                            Arguments = $"-u {ApiRequest.UserInfo!.UserToken} -p {tunnel.TunnelId}",

                        }
                    };
                    Utils.Log($"传入参数: -u {ApiRequest.UserInfo!.UserToken} -p {tunnel.TunnelId}", level: TraceLevel.Warning);
                    process.OutputDataReceived += (sender, args) => Output(tunnel.TunnelId,args.Data);
                    process.ErrorDataReceived += (sender, args) => Output(tunnel.TunnelId,args.Data,level: TraceLevel.Error);
                    process.Start();
                    process.BeginErrorReadLine();
                    process.BeginOutputReadLine();
                    process.Exited += async (sender, args) =>
                    {
                        Output(tunnel.TunnelId,$"[进程已退出,Exit Code: {process.ExitCode},等待 1500ms 后重启。]",TraceLevel.Warning);

                        await Task.Delay(1500);
                         
                        Launch(tunnel);
                    };
                    if (!Wrappers.ContainsKey(tunnel.TunnelId))
                    {
                        Wrappers.Add(tunnel.TunnelId, new()
                        {
                            Tunnel = tunnel,
                            Process = process
                        });
                    }
                    else
                    {
                        Wrappers[tunnel.TunnelId].Process = process;
                    }
                    return true;
                    
                }
                catch (Exception ex)
                {
                    Utils.Log(ex,level: TraceLevel.Warning);
                    LogHelper.Add(0, ex, TraceLevel.Warning, true);
                    return false;
                }
            }
            return false;

        }

        public static bool Kill(Libraries.Api.Models.ResponseBody.UserTunnelsResponse.UserTunnel tunnel)
        {
            try
            {
                if (Wrappers.ContainsKey(tunnel.TunnelId))
                {
                    if (Wrappers[tunnel.TunnelId].Process is Process process && !process.HasExited)
                    {
                        process.EnableRaisingEvents = false;
                        process.Kill();
                    }
                    Wrappers.Remove(tunnel.TunnelId);
                }
                return true;
            }
            catch(Exception ex)
            {
                Utils.Log(ex, level: TraceLevel.Warning);
                LogHelper.Add(0, ex, TraceLevel.Warning, true);
                return false;
            }
        }


        static void Output(int id,object data,TraceLevel level = TraceLevel.Info)
        {
            LogHelper.Add(id, data, TraceLevel.Warning);
            // Utils.Log(data, level: level);
        }
    }
}
