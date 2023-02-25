using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenFrp.Core.Helper;
using OpenFrp.Core.Libraries.Api;
using OpenFrp.Core.Libraries.Api.Models;
using OpenFrp.Core.Libraries.Pipe;
using OpenFrp.Core.Libraries.Protobuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace OpenFrp.Core
{
    public class ServiceWorker : ServiceBase
    {
        /// <summary>
        /// 服务被启用时
        /// </summary>
        /// <param name="args"></param>
        protected override async void OnStart(string[] args)
        {
            // Debugger.Launch();
            


            await ConfigHelper.ReadConfig();
            await Task.Delay(1000);

            var server = new PipeServer();
            server.Start();
            server.OnDataRecived += OnDataRecived;

            // PUSH 只发送
            Program.PushClient = new PipeServer();
            Program.PushClient.Start(true);
            server.OnRestart = () =>
            {
                Program.PushClient.Disponse();
                if (Program.PushClient.Pipe?.IsConnected == true)
                {
                    Program.PushClient.Disconnect();
                }
                Program.PushClient.Start(true);
            };



            Microsoft.Win32.SystemEvents.SessionEnding += async (se, ev) => await ConfigHelper.Instance.WriteConfig();



            LogHelper.Add(0, $"OpenFrp Launcher 2023 | Pipe标识符: {Utils.PipesName} | Service Mode v1", TraceLevel.Warning, true);

            if (ConfigHelper.Instance.Account.HasAccount)
            {
                JsonConvert.DefaultSettings = () => new()
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                int tryCount = 0;
                ResponseBody.BaseResponse response;
                while (tryCount <= 5 && !(response = await TryLogin()).Success)
                {
                    tryCount++;
                    LogHelper.Add(0, $"登录失败,重试 {tryCount} 次，共5次。等待 {5 * tryCount}s.", TraceLevel.Warning);

                    await Task.Delay(5000 * tryCount);
                }
                
                if (tryCount > 5)
                {
                    LogHelper.Add(0, "五次登录都失败了，是否已连接到互联网???", TraceLevel.Error);
                    if (ConfigHelper.Instance.AutoStartupList.Length > 0)
                        LogHelper.Add(0, "本次自动启动取消。", TraceLevel.Error);
                }
                else if (ConfigHelper.Instance.AutoStartupList.Length > 0)
                {

                    var tun_response = await ApiRequest.UniversalPOST<Core.Libraries.Api.Models.ResponseBody.UserTunnelsResponse>(ApiUrls.UserTunnels);
                    if (tun_response.Success)
                    {
                        foreach (ResponseBody.UserTunnelsResponse.UserTunnel tunnel in tun_response.Data?.List ?? new())
                        {
                            if (ConfigHelper.Instance.AutoStartupList.Contains(tunnel.TunnelId))
                            {
                                ConsoleHelper.Launch(tunnel);
                            }
                        }
                        
                    }
                    else LogHelper.Add(0, $"由于以下原因,本次开机自动启动失败: {tun_response.Message}", TraceLevel.Error);
                }
            }else LogHelper.Add(0, $"没有找到账户凭证，已跳过登录。Account: {ConfigHelper.Instance.Account.JSON()}", TraceLevel.Warning, true);

            

        }

        private async ValueTask<ResponseBody.BaseResponse> TryLogin()
        {
            var loginResult = await ApiRequest.Login(ConfigHelper.Instance.Account.UserName ?? "", ConfigHelper.Instance.Account.Password ?? "");
            if (loginResult.Success)
            {
                var userinfoResult = await ApiRequest.UniversalPOST<ResponseBody.UserInfoResponse>(ApiUrls.UserInfo);
                if (!userinfoResult.Success) { ApiRequest.ClearAuth(); return userinfoResult; }
                ApiRequest.UserInfo = userinfoResult.Data;
                return new()
                {
                    Success = true,
                };
            }
            return loginResult;
        }

        /// <summary>
        /// 当服务被关闭时
        /// </summary>
        protected async override void OnStop()
        {
            if (ConsoleHelper.Wrappers.Count > 0)
            {
                ConfigHelper.Instance.AutoStartupList = ConsoleHelper.Wrappers.Keys.ToArray();
                foreach (var x in ConsoleHelper.Wrappers.Values.ToArray())
                {
                    ConsoleHelper.Kill(x.Tunnel!);
                }
            }
            else if (ConsoleHelper.Wrappers.Count == 0 && ConfigHelper.Instance.AutoStartupList.Length > 0)
            {
                ConfigHelper.Instance.AutoStartupList = new int[0];
            }
            await ConfigHelper.Instance.WriteConfig();
        }

        public static void OnDataRecived(PipeWorker worker, RequestBase request)
        {
            ResponseBase response = new() { Success = true };

            try
            {
                switch (request.Action)
                {
                    // 客户端上传了登录状态
                    case RequestType.ClientPushLoginstate:
                        {
                            ApiRequest.Authorization = request.LoginRequest.Authorization;
                            ApiRequest.SessionId = request.LoginRequest.SessionId;
                            ApiRequest.UserInfo = request.LoginRequest.UserInfoJson.PraseJson<Libraries.Api.Models.ResponseBody.UserInfoResponse>()?.Data;
                            break;

                        };
                    // 客户端请求清除登录
                    case RequestType.ClientPushClearlogin:
                        {
                            ApiRequest.ClearAuth();
                            break;
                        }
                    // 客户端请求 FRPC 启动
                    case RequestType.ClientFrpcStart:
                        {
                            if (request.FrpRequest is not null)
                            {
                                var tunnel = request.FrpRequest.UserTunnelJson.PraseJson<Libraries.Api.Models.ResponseBody.UserTunnelsResponse.UserTunnel>()!;
                                LogHelper.Add(0, $"从客户端收到了开启请求。{tunnel.TunnelName}", System.Diagnostics.TraceLevel.Warning, true);
                                if (!ConsoleHelper.Launch(tunnel))
                                {
                                    response = new() { Message = "发生了未知错误." };
                                }
                                break;
                            }
                            response = new() { Message = "FRP Request 不能为空。" };
                            break;

                        }
                    // 客户端请求 FRPC 关闭
                    case RequestType.ClientFrpcClose:
                        {
                            if (request.FrpRequest is not null)
                            {
                                var tunnel = request.FrpRequest.UserTunnelJson.PraseJson<ResponseBody.UserTunnelsResponse.UserTunnel>()!;
                                ConsoleHelper.Kill(tunnel);
                                LogHelper.Add(0, $"从客户端收到关闭请求。{tunnel.TunnelName}", System.Diagnostics.TraceLevel.Warning, true);
                                break;
                            }
                            response = new() { Message = "FRP Request 不能为空。" };
                            break;
                        };
                    // 获取日志
                    case RequestType.ClientGetLogs:
                        {
                            if (ConsoleHelper.Wrappers.ContainsKey(request.LogsRequest.Id))
                            {
                                LogHelper.Logs[request.LogsRequest.Id].ForEach(x => response.LogsJson.Add(x.JSON()));
                            }
                            else
                            {
                                LogHelper.Logs[0].ForEach(x => response.LogsJson.Add(x.JSON()));
                            }
                            break;
                        }
                    // 获取运行中的隧道 ID
                    case RequestType.ClientGetRunningtunnelsid:
                        {
                            response.RunningCount.AddRange(ConsoleHelper.Wrappers.Keys);
                            break;
                        }
                    // 客户端请求 获取所有正在运行的隧道
                    case RequestType.ClientGetRunningtunnel:
                        {
                            if (ConsoleHelper.Wrappers.ContainsKey(request.LogsRequest.Id))
                            {
                                LogHelper.Logs[request.LogsRequest.Id].ForEach(x => response.LogsJson.Add(x.JSON()));
                            }
                            else
                            {
                                LogHelper.Logs[0].ForEach(x => response.LogsJson.Add(x.JSON()));
                            }
                            response.LogsViewJson.Add(new Libraries.Api.Models.ResponseBody.UserTunnelsResponse.UserTunnel() { TunnelName = "全部日志", TunnelId = 0 }.JSON());
                            response.LogsViewJson.AddRange(ConsoleHelper.Wrappers.Values.Select(x => x.Tunnel?.JSON()));
                            break;
                        }
                    // 清除日志
                    case RequestType.ClientClearLogs:
                        {
                            LogHelper.Logs[request.LogsRequest.Id].Clear();
                            break;
                        }
                    default: { response = new() { Message = "Action not found" }; }; break;
                }
            }
            catch (Exception ex)
            {
                response = new()
                {
                    Exception = ex.ToString(),
                    Message = "在服务端处理时发生了未知的错误"
                };
            }

            worker.Send(response.ToByteArray());
        }
    }

    /// <summary>
    /// 兼容安装器
    /// </summary>
    [RunInstaller(true)]
    public class WindowsServicesInstaller : Installer
    {
        public WindowsServicesInstaller()
        {
            Installers.AddRange(new Installer[]  {
                new ServiceProcessInstaller()
                {
                    Account = ServiceAccount.LocalService,
                    Username = null,
                    Password = null,
                },
                new ServiceInstaller()
                {
                    ServiceName = "OpenFrp Launcher Service",
                    DisplayName = "OpenFrp Launcher Deamon Service",
                    Description = "OpenFrp 启动器 后台进程。",
                    StartType = ServiceStartMode.Automatic,
                }
            });
        }
    }
}
