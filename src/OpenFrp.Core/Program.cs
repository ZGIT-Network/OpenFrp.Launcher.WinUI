using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using OpenFrp.Core.Libraries.Pipe;

namespace OpenFrp.Core
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // NORMAL 只接收
            var server = new PipeServer();
            server.Start();
            // PUSH 只发送
            var server_push = new PipeServer();
            server_push.Start(true);




            if (!Utils.IsWindowsService)
            {
                Console.WriteLine("我已经注册咯");
                Helper.Win32Helper.SetConsoleCtrlHandler(key =>
                {
                    server.Disconnect();
                    server_push.Disconnect();
                    // 因为被 Handler 拦截，所以需要手动退出并返回。
                    Environment.Exit(0);
                    return true;
                }, true);
            }
            while (Console.ReadLine() is not "exit")
            {

            }


        }
    }
}
