using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace OpenFrp.Core.Libraries.Pipe
{
    /// <summary>
    /// Stream I/O 简约化
    /// </summary>
    public class PipeWorker : IDisposable
    {
        public static int MaxbufferSize { get => 1048576; }

        public PipeStream? Pipe { get; set; }

        public byte[] Buffer { get; set; } = new byte[] { };

        public PipeWorker(PipeStream pipe, byte[] buffer) 
        {
            Pipe = pipe;
            Buffer = buffer;
        }

        void IDisposable.Dispose() => Pipe?.Dispose();

        public void Dispose() => Pipe?.Dispose();

        public void Send(byte[] data) => Pipe?.Write(data,0, data.Length);

        public int ReadMessage(int read)
        {
            int index = read;
            while (Pipe?.IsMessageComplete == false)
            {
                index += Pipe.Read(Buffer, index, Buffer.Length - index);
                if (index == Buffer.Length)
                {
                    throw new Exception("Data too long");
                }
            }
            return index;
        }
    }
}
