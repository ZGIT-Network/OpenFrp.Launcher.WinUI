
using OpenFrp.Core.Helper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OpenFrp.Core
{
    public class Utils
    {
        /// <summary>
        /// 应用的数据文件存放处
        /// </summary>
        public static string ApplicatioDataPath { get => Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData).CombinePath("OpenFrp.Launcher"); }
        /// <summary>
        /// 配置文件
        /// </summary>
        public static string ConfigFile { get => ApplicatioDataPath.CombinePath("config.json"); }

        /// <summary>
        /// 管道的名称
        /// </summary>
        public static string PipesName { get => $"{ApplicatioDataPath.GetMD5()}_Of2023_rel.app".GetMD5().ToUpper(); }
        /// <summary>
        /// 是否以系统服务模式运行
        /// </summary>
        public static bool IsWindowsService { get => !Environment.UserInteractive; }
        /// <summary>
        /// 应用主窗口
        /// </summary>
        public static Window MainWindow { get => Application.Current.MainWindow;  }

        public static void Log(object message,bool debug = false)
        {
            try
            {
                if (Console.WindowWidth is not -1)
                {
                    Console.WriteLine($"{(debug ? "[DEBUG] " : "")}{message}");
                    return;
                }
            }
            catch { return; }
            Debug.WriteLine($"{(debug ? "[DEBUG] " : "")}{message}");
        }



    }
}
