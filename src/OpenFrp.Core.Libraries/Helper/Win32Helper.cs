using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace OpenFrp.Core.Helper
{
    public class Win32Helper
    {
        public const string Kernal32 = "Kernel32.dll";

        [DllImport(Kernal32)]
        public static extern bool SetConsoleCtrlHandler(ConsoleExitEvent handler, bool add);

        public delegate bool ConsoleExitEvent(CtrlBreakType sig);

        public enum CtrlBreakType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        public static async ValueTask<ProcessNetworkInfo[]> GetAliveNetworkLink()
        {
            var process = Process.Start(new ProcessStartInfo("netstat.exe", "-ano")
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            });
            var dic = new Dictionary<string, string>();
            var pool = new List<Task<ProcessNetworkInfo>>();
            foreach (var str in (await process.StandardOutput.ReadToEndAsync()).Split('\n'))
            {
                var args = str.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                if (args.Length < 3 || !(args[0] is "TCP" || args[0] is "UDP"))
                {
                    continue;
                }
                if (args[1][0] is '[') continue;


                pool.Add(Task.Run<ProcessNetworkInfo>(() =>
                {
                    string pid = args[0] is "UDP" ? args[3] : args[4];
                    if (!dic.ContainsKey(pid))
                    {
                        dic[pid] = "[拒绝访问]";
                        try
                        {
                            dic[pid] = Process.GetProcessById(Convert.ToInt32(pid)).ProcessName;
                        }
                        catch { }
                    }
                    return new()
                    {
                        ProcessName = $"{(dic.ContainsKey(pid) ? dic[pid] : $"PID: {pid}")}",
                        Address = args[1].Split(':').First(),
                        Port = Convert.ToInt32(args[1].Split(':').Last())
                    };
                }));
            }

            return await Task.WhenAll(pool);
        }

        public class ProcessNetworkInfo
        {
            public string? ProcessName { get; set; }

            public string? Address { get; set; }

            public int Port { get; set; }
        }

        public static bool IsServiceInstalled()
        {
            ServiceController[] services = ServiceController.GetServices();
            foreach (var service in services)
            {
                if (service.ServiceName == "OpenFrp Launcher Service") return true;
            }
            return false;
        }

        public static bool CheckServiceIsRunning()
        {
            using var service = new ServiceController("OpenFrp Launcher Service");
            if (!service.CanStop)
            {
                return false;
            }
            return true;
        }

        [DllImport("User32.dll",EntryPoint = "ShowWindow")]
        public static extern bool ShowWindow(IntPtr hWnd, int type);

    }
}
