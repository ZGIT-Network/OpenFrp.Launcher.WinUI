using OpenFrp.Core.Helper;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenFrp.Core.Libraries.Pipe
{
    public class PipeClient
    {
        public NamedPipeClientStream? _client { get; set; }

        public PipeWorker? Worker { get; set; }

        public void Start(bool isPush = false)
        {
            _client = new(".", $"{Utils.PipesName}{(isPush ? "_PUSH" : "")}", PipeDirection.InOut, PipeOptions.Asynchronous);
            _client.ConnectAsync(5000);
            //_client.ReadMode = PipeTransmissionMode.Message;
            Worker = new(_client, new byte[PipeWorker.MaxbufferSize]);
        }

        
    }
}
