using Google.Protobuf;
using OpenFrp.Core.Libraries.Protobuf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Ribbon;

namespace OpenFrp.Core.Libraries.Pipe
{
    // 2023-10-3 紧急补充:
    // 部分源码借鉴于
    // https://github.com/natfrp/launcher-windows/

    public class PipeServer : PipeWorker,IDisposable
    {
        public NamedPipeServerStream? Server { get; set; }

        public bool IsRunning { get; private set; }

        public Action OnRestart { get; set; } = delegate { };

        public Action OnConnectedEvent { get; set; } = delegate { };

        public Action<PipeWorker, RequestBase> OnDataRecived { get; set; } = delegate { };

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
                        Pipe = server;
                        Buffer = new byte[server.InBufferSize];
                        server.EndWaitForConnection(callback);

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
                server.EndWaitForConnection(iar);
                IsRunning = true;
                Pipe = server;
                Buffer = new byte[server.InBufferSize];

                OnConnectedEvent();

                while (IsRunning)
                {
                    var request = OnDataReviced();
                    try
                    {
                        Console.WriteLine($"handle recived data: {request?.Action}");
                    }
                    catch
                    {

                    }
                    // 当请求体为 Null 或 不在运行状态 则退出;
                    if (request is null || !IsRunning)
                    {
                        IsRunning = false;
                        break;
                    }
                    
                    OnDataRecived(this, request);
                    try
                    {
                        Console.WriteLine($"feed back message: {request.Action}");
                    }
                    catch
                    {

                    }
                }
            }
            catch {

            }

            Disponse();
            OnRestart();
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

                        return obj;
                    }
                }
                catch { }
                return default;
            }
            return default;
            
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
