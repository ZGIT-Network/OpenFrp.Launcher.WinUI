using OpenFrp.Core.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        /// 应用主窗口
        /// </summary>
        public static Window MainWindow { get => Application.Current.MainWindow;  }

    }
}
