using Google.Protobuf;
using OpenFrp.Core.Libraries.Protobuf;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace OpenFrp.Core.Libraries.Pipe
{
    public class PipeServer : PipeWorker,IDisposable
    {
        public NamedPipeServerStream? Server { get; set; }

        public bool IsRunning { get; private set; }

        void IDisposable.Dispose() => Server?.Dispose();
        /// <summary>
        /// 释放该对象。
        /// </summary>
        public void Disponse()
        {
            Server?.Dispose();
            Dispose();
        }
        /// <summary>
        /// 启动管道 - 服务端
        /// </summary>
        /// <param name="isPush">是否为推送管道</param>
        public override void Start(bool isPush = false)
        {
            Server = CreatePipeServer(OnConnected,isPush);
            Console.WriteLine($"Started，{$"{Utils.PipesName}{(isPush ? "_PUSH" : "")}"}");
        }
        /// <summary>
        /// 创建服务端
        /// </summary>
        private NamedPipeServerStream? CreatePipeServer(AsyncCallback callback,bool isPush)
        {
            if (IsRunning) return null;
            try
            {
                var security = new PipeSecurity();
                security.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().User, PipeAccessRights.FullControl, AccessControlType.Allow));
                security.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null), PipeAccessRights.ReadWrite, AccessControlType.Allow));
                security.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.NetworkSid, null), PipeAccessRights.FullControl, AccessControlType.Deny));


                var server = new NamedPipeServerStream($"{Utils.PipesName}{(isPush ? "_PUSH" : "")}", PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous, PipeWorker.MaxbufferSize, PipeWorker.MaxbufferSize, security);

                server.ReadMode = PipeTransmissionMode.Message;
                if (!isPush)
                {
                    server.BeginWaitForConnection(callback, server);
                }
                else
                {
                    server.BeginWaitForConnection((callback) =>
                    {
                        server.EndWaitForConnection(callback);
                        Utils.Log($"客户端已连接到PUSH", true);
                    }, server);
                }
                return server;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Disponse();
                return null;
            }
        }

        private void OnConnected(IAsyncResult iar)
        {
            try
            {
                
                NamedPipeServerStream server = (NamedPipeServerStream)iar.AsyncState;
                Utils.Log("获得连接!", true);
                server.EndWaitForConnection(iar);
                IsRunning = true;
                Pipe = server;
                Buffer = new byte[server.InBufferSize];

                while (IsRunning)
                {
                    var request = OnDataReviced();
                    // 当请求体为 Null 或 不在运行状态 则退出;
                    if (request is null || !IsRunning)
                    {
                        IsRunning = false;
                        break;
                    }
                    var response = ExcuteAction(request);
                    Utils.Log("请求反馈: " + response, true);
                    Send(response.ToByteArray());
                }
            }
            catch (Exception ex)
            {
                Utils.Log("(PipeServer.OnConnected) Try => Catch : " + ex,true);
            }
            Utils.Log("客户端已断开",true);
            Disponse();
            Start();
        }

        private RequestBase? OnDataReviced()
        {
            if (Server is not null)
            {
                int count;
                try
                {
                    count = Read();
                    if (count > 0)
                    {
                        var obj = RequestBase.Parser.ParseFrom(Buffer, 0, EnsureMessageComplete(count));
                        Utils.Log("Get Protobuf Content: " + obj, true);
                        return obj;
                    }
                }
                catch { }
                return default;
            }
            return default;
            
        }

        private ResponseBase ExcuteAction(RequestBase reqeest)
        {
            return reqeest.Action switch
            {
                _ => new()
                {
                    Message = string.IsNullOrEmpty(reqeest.Message) ? "找不到对应 Function" : reqeest.Message,
                    Success = false
                }
            };
        }

        public override void Disconnect()
        {
            if (Server?.IsConnected == true)
            {
                Server?.Disconnect();
            }
        }
    }
}
