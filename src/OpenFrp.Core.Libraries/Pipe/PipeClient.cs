using Google.Protobuf;
using OpenFrp.Core.Helper;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation.Provider;

namespace OpenFrp.Core.Libraries.Pipe
{
    public class PipeClient : PipeWorker
    {
        public NamedPipeClientStream? Client { get; set; }


        public Action<PipeClient> OnPushStart { get; set; } = delegate { };

        public bool IsConnected { get => Client?.IsConnected ?? false; }

        public bool IsPushMode { get; set; }

        public override async void Start(bool isPush = false)
        {
            Client = new(".", $"{Utils.PipesName}{(isPush ? "_PUSH" : "")}", PipeDirection.InOut, PipeOptions.Asynchronous);
            try
            {
                await Client.ConnectAsync();
                if (IsConnected)
                {

                    Client.ReadMode = PipeTransmissionMode.Message;
                    Pipe = Client;
                    Buffer = new byte[MaxbufferSize];
                    if (IsPushMode = isPush) OnPushStart(this);
                }
            }
            catch (Exception ex)
            {
                Utils.Log(ex, true);
            }
            

        }

        public async ValueTask<Protobuf.ResponseBase> Request(Protobuf.RequestBase request)
        {
            if (IsConnected)
            {
                await base.SendAsync(request.ToByteArray());
                int count;
                try
                {
                    count = await base.ReadAsync();
                    if (count >= 0)
                    {
                        return Protobuf.ResponseBase.Parser.ParseFrom(Buffer, 0, EnsureMessageComplete(count));
                    }
                }
                catch (Exception ex)
                {
                    Utils.Log(ex, true);
                }
                return new() { Success = false, Message = "在处理过程中发生了未知的错误。" };
            }
            else
            {
                return new() { Success = false, Message = "未连接到守护进程,大部分功能无法正常使用。" };
            }

        }

        public override void Disconnect() => Client?.Close();
    }
}
