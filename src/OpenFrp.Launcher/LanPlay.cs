using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Awe.AppModel
{
    /// <summary>
    /// 岚游 - AppModel
    /// </summary>
    public sealed class LanPlay
    {

        /// <summary>
        /// 岚游 - 游戏房间
        /// </summary>
        public class LanPlayRoom
        {
            // TODO: 重写 LanPlayRoom 结构。
            //   通过时间检查，来检测客户端是否存在
            //   也就是需要进行 HealthCheck (检查方法: FirstTime - UpdateTime)
            //   如果与上次检测时间相同，那么即客户端已离线
            //   如果客户端再次发送 Health包，那么会恢复使用。
            //   如果时间超过一定时间，那么直接判定 Closed.

            /// <summary>
            /// 初始时间
            /// </summary>
            public DateTimeOffset CreateTime { get; set; }

            /// <summary>
            /// 远程更新时间 (Health Time)
            /// </summary>
            public DateTimeOffset UpdateTime { get; set; }

            /// <summary>
            /// 与初始更新的时间差
            /// </summary>
            public int UpdateOffset { get; set; }

        }
    }
}
