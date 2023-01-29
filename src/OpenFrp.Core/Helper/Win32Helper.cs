using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
    }
}
