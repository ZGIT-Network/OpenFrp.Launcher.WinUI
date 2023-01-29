using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenFrp.Core.Libraries.Pipe
{
    /// <summary>
    /// Stream I/O 简约化
    /// </summary>
    public abstract class PipeWorker : IDisposable
    {
        public static int MaxbufferSize { get => 1048576; }

        public PipeStream? Pipe { get; set; }

        public byte[] Buffer { get; set; } = new byte[] { };

        void IDisposable.Dispose() => Pipe?.Dispose();

        public void Dispose() => Pipe?.Dispose();

        public void Send(byte[] data) => Pipe?.Write(data,0,data.Length);

        public async ValueTask SendAsync(byte[] data) => await Task.Run(() => Send(data));

        public int Read() => Pipe?.Read(Buffer, 0, Buffer.Length) ?? 0;

        public async ValueTask<int> ReadAsync() => await Task.Run(Read);
        
        public int EnsureMessageComplete(int read)
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

        public abstract void Start(bool isPush = false);

        public abstract void Disconnect();
    }
}
