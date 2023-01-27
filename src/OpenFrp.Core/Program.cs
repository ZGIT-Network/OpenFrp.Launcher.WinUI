using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenFrp.Core.Libraries.Pipe;

namespace OpenFrp.Core
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var server = new PipeServer();
            server.Start();
            while (Console.ReadLine() is "exit")
            {

            }
        }
    }
}
