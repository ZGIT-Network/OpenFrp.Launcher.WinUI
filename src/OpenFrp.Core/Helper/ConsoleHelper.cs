using CommunityToolkit.WinUI.Notifications;
using Google.Protobuf;
using OpenFrp.Core.Libraries.Api;
using OpenFrp.Core.Libraries.Pipe;
using OpenFrp.Core.Libraries.Protobuf;
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
                            Arguments = $"-n -u {ApiRequest.UserInfo!.UserToken} -p {tunnel.TunnelId}",

                        }
                    };
                    Utils.Log($"传入参数: -u {ApiRequest.UserInfo!.UserToken} -p {tunnel.TunnelId}", level: TraceLevel.Warning);
                    process.OutputDataReceived += (sender, args) => Output(tunnel.TunnelId,args.Data);
                    process.ErrorDataReceived += (sender, args) => Output(tunnel.TunnelId, args.Data, TraceLevel.Error);
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


        static async void Output(int id,object data,TraceLevel level = TraceLevel.Info)
        {
            
            if (data?.ToString().Contains("[E]") is true) level = TraceLevel.Error;
            else if (data?.ToString().Contains("[W]") is true) level = TraceLevel.Warning;
            else if (data?.ToString().Contains("面板强制下线") is true)
            {
                if (Wrappers.ContainsKey(id))
                {
                    Kill(Wrappers[id].Tunnel!);
                    if (Program.PushClient is not null && Program.PushClient.Pipe?.IsConnected is true)
                    {
                        LogHelper.Add(0, $"隧道 {id} 已从面板强制下线，已向客户端发送请求包。",TraceLevel.Warning);
                        await Program.PushClient.SendAsync(new RequestBase()
                        {
                            Action = RequestType.ServerUpdateTunnels
                        }.ToByteArray());
                    }

                }
            }
            else if (!Utils.IsWindowsService && (data?.ToString().Contains("启动成功") is true || data?.ToString().Contains("启动失败") is true))
            {
                Console.WriteLine(Enum.GetName(typeof(ConfigHelper.TnMode), ConfigHelper.Instance.MessagePullMode));

                if (Program.PushClient is not null)
                {
                    var req = new RequestBase()
                    {
                        Action = RequestType.ServerSendNotifiy,
                        NotifiyRequest = new()
                        {
                            TunnnelJson = Wrappers[id].Tunnel?.JSON(),
                            Flag = data?.ToString().Contains("启动成功") ?? false
                        }
                    };
                    if (!req.NotifiyRequest.Flag) { req.NotifiyRequest.Content = data?.ToString() ?? "请前往日志查看。"; }
                    await Program.PushClient.SendAsync(req.ToByteArray());
                }
                    
                // 逻辑需在客户端处理
            }
            LogHelper.Add(id, data ?? "", level);
            LogHelper.Add(0, data ?? "", level);
        }
    }
}
