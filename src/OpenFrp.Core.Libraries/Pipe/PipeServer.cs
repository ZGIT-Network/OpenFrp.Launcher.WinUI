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
    public class PipeServer : IDisposable
    {
        public NamedPipeServerStream? _server { get; set; }

        public PipeWorker? Worker { get; set; }

        public bool IsRunning { get; private set; }

        public void Log(string message) => Console.WriteLine(message);

        void IDisposable.Dispose() => _server?.Dispose();

        public void Disponse()
        {
            _server?.Dispose();
            Worker?.Dispose();
        }

        public void Start()
        {
            _server = CreatePipeServer(OnConnected);
        }

        private NamedPipeServerStream? CreatePipeServer(AsyncCallback callback)
        {
            if (IsRunning) return null;
            try
            {
                var security = new PipeSecurity();
                security.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().User, PipeAccessRights.FullControl, AccessControlType.Allow));
                security.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null), PipeAccessRights.ReadWrite, AccessControlType.Allow));
                security.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.NetworkSid, null), PipeAccessRights.FullControl, AccessControlType.Deny));


                var server = new NamedPipeServerStream(Utils.PipesName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous, PipeWorker.MaxbufferSize, PipeWorker.MaxbufferSize, security);

                server.ReadMode = PipeTransmissionMode.Message;

                server.BeginWaitForConnection(callback, server);

                return server;
            }
            catch
            {
                Disponse();
                return null;
            }
        }

        private async void OnConnected(IAsyncResult iar)
        {
            try
            {
                Log("Get Connection");
                NamedPipeServerStream server = (NamedPipeServerStream)iar.AsyncState;
                server.EndWaitForConnection(iar);
                IsRunning = true;
                Worker = new(server, new byte[server.InBufferSize]);

                while (IsRunning)
                {
                    if (!await OnDataReviced(Worker)) break;
                }

                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            Start();
        }

        private async ValueTask<bool> OnDataReviced(PipeWorker worker)
        {
            if (_server is not null)
            {
                return await Task.Run(() =>
                {
                    int count;
                    try
                    {
                        count = _server.Read(worker.Buffer, 0, PipeWorker.MaxbufferSize);
                    }
                    catch
                    {
                        return false;
                    }
                    var obj = Protobuf.RequestBase.Parser.ParseFrom(worker.Buffer, 0, worker.ReadMessage(count));
                    Console.WriteLine(obj);
                    return true;
                });
            }
            return false;
            
        }


        
    }
}
