using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using OpenFrp.Core.Helper;
using OpenFrp.Core.Libraries.Api;
using OpenFrp.Core.Libraries.Pipe;
using OpenFrp.Core.Libraries.Protobuf;

namespace OpenFrp.Core
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // NORMAL 只接收
            var server = new PipeServer();
            server.Start();
            server.OnDataRecived += OnDataRecived;

            // PUSH 只发送
            var server_push = new PipeServer();
            server_push.Start(true);
            server.OnRestart = () =>
            {
                server_push.Disponse();
                if (server_push.Pipe?.IsConnected == true)
                {
                    server_push.Disconnect();
                }
                server_push.Start(true);
            };
            
            string str = Console.ReadLine();
            while (str is not "exit")
            {
                if (str is "session")
                {
                    Utils.Log(ApiRequest.SessionId!);
                    Utils.Log(ApiRequest.Authorization!);
                    Utils.Log(ApiRequest.UserInfo!.JSON());
                }
                else if (str is "debugger")
                {
                    Debugger.Launch();
                }
                else if (str is "waps")
                {
                    foreach (var item in ConsoleHelper.Wrappers.Values)
                    {
                        Utils.Log(item.Tunnel!, level: TraceLevel.Error);
                    }
                }

                str = Console.ReadLine();
            }

        }

        private static void OnDataRecived(PipeWorker worker,RequestBase request)
        {
            ResponseBase response = new() { Success = true };
            try
            {
                switch (request.Action)
                {
                    case RequestType.ClientPushLoginstate:
                        {
                            ApiRequest.Authorization = request.LoginRequest.Authorization;
                            ApiRequest.SessionId = request.LoginRequest.SessionId;
                            ApiRequest.UserInfo = request.LoginRequest.UserInfoJson.PraseJson<Libraries.Api.Models.ResponseBody.UserInfoResponse>()?.Data;
                            break;
                            
                        };
                    case RequestType.ClientPushClearlogin:
                        {
                            ApiRequest.ClearAuth();
                            break;
                        }
                    case RequestType.ClientFrpcStart:
                        {
                            if (request.FrpRequest is not null)
                            {
                                var tunnel = request.FrpRequest.UserTunnelJson.PraseJson<Libraries.Api.Models.ResponseBody.UserTunnelsResponse.UserTunnel>()!;
                                Utils.Log($"从客户端收到了开启请求。{tunnel.TunnelName}", true);
                                if (!ConsoleHelper.Launch(tunnel))
                                {
                                    response = new() { Message = "发生了未知错误." };
                                }
                                break;
                            }
                            response = new() { Message = "FRP Request 不能为空。" };
                            break;

                        }
                    case RequestType.ClientFrpcClose:
                        {
                            if (request.FrpRequest is not null)
                            {
                                var tunnel = request.FrpRequest.UserTunnelJson.PraseJson<Libraries.Api.Models.ResponseBody.UserTunnelsResponse.UserTunnel>()!;
                                Debugger.Break();
                                ConsoleHelper.Kill(tunnel);
                                Utils.Log($"从客户端收到关闭请求。{tunnel.TunnelName}", true);
                                break;
                            }
                            response = new() { Message = "FRP Request 不能为空。" };
                            break;
                        };
                    case RequestType.ClientGetLogs:
                        {
                            if (request.LogsRequest is not null)
                            {
                                if (ConsoleHelper.Wrappers.ContainsKey(request.LogsRequest.Id))
                                {
                                    LogHelper.Logs[request.LogsRequest.Id].ForEach(x => response.LogsJson.Add(x.JSON()));
                                }
                                else
                                {
                                    LogHelper.Logs[0].ForEach(x => response.LogsJson.Add(x.JSON()));
                                }
                            }
                            break;
                        }
                    case RequestType.ClientGetRunningtunnels:
                        {
                            response.RunningCount.AddRange(ConsoleHelper.Wrappers.Keys);
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

            Utils.Log("发回客户端 " + response, true);
            worker.Send(response.ToByteArray());
        }
    }
}
