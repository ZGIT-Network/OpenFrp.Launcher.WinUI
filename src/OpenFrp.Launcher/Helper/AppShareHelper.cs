using OpenFrp.Core.Libraries.Pipe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenFrp.Launcher.Helper
{
    public class AppShareHelper
    {
        /// <summary>
        /// 是否有 ContentDialog 正在显示
        /// </summary>
        public static bool HasDialog { get; set; }


        /// <summary>
        /// 管道 - 客户端
        /// </summary>
        public static PipeClient PipeClient { get; set; } = new();
    }
}
