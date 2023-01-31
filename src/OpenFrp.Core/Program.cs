using System;
using System.Collections.Generic;
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


            // 暂定,这部分可能偶尔无法正常运行。

            //if (!Utils.IsWindowsService)
            //{
            //    Utils.Log("已注册 Exited 事件,按下 Ctrl+C 可触发。");
            //    Helper.Win32Helper.SetConsoleCtrlHandler(key =>
            //    {
            //        server.Disconnect();
            //        server_push.Disconnect();
            //        // 因为被 Handler 拦截，所以需要手动退出并返回。
            //        Environment.Exit(0);
            //        return true;
            //    }, true);
            //}
            while (Console.ReadLine() is not "exit")
            {

            }


        }

        private static void OnDataRecived(PipeWorker worker,RequestBase request)
        {
            ResponseBase response;
            try
            {
                switch (request.Action)
                {
                    case RequestType.ClientPushLoginstate:
                        {
                            ApiRequest.Authorization = request.LoginRequest.Authorization;
                            ApiRequest.SessionId = request.LoginRequest.Authorization;
                            response = new() { Success = true, Message = "登录成功!" };
                        };break;
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
