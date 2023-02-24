using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenFrp.Launcher.Helper
{
    public class WindHelper
    {
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindowA(string? lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern long SetWindowLongA(IntPtr hWnd, int nIndex, long dwNewLong);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}
