using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OpenFrp.Core.Helper
{
    public class ConfigHelper
    {
        /// <summary>
        /// 实例
        /// </summary>
        public static ConfigHelper Instance { get; set; } = new();

        private ModernWpf.ElementTheme _themeSet { get; set; }

        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        [JsonProperty("themeSet")]
        public ModernWpf.ElementTheme ThemeSet
        {
            get => _themeSet;
            set
            {
                if (Utils.MainWindow is Window wind)
                    try { ModernWpf.ThemeManager.SetRequestedTheme(wind, value); }
                    catch { }
                _themeSet = value;
            }
        }

        private ModernWpf.Controls.Primitives.BackdropType _backdropSet { get; set; }

        [JsonProperty("backdropSet")]
        public ModernWpf.Controls.Primitives.BackdropType BackdropSet
        {
            get => _backdropSet;
            set
            {
                if (Utils.MainWindow is Window wind)
                    try { ModernWpf.Controls.Primitives.WindowHelper.SetSystemBackdropType(wind, value); }
                    catch { }
                _backdropSet = value;
            }
        }

        [JsonProperty("bypassProxy")]
        public bool BypassProxy { get; set; }
        /// <summary>
        /// 读取配置
        /// </summary>
        public static async ValueTask ReadConfig()
        {
            if (File.Exists(Utils.ConfigFile))
            {
                Instance = (await Utils.ConfigFile.GetStreamReader().ReadToEndAsync())
                    .PraseJson<ConfigHelper>() ?? new();
            }else Directory.CreateDirectory(Utils.ApplicatioDataPath);
        }
        /// <summary>
        /// 写配置
        /// </summary>
        public async ValueTask WriteConfig()
        {
            try
            {
                string str = Instance.JSON();
                await Utils.ConfigFile.GetStreamWriter().WriteLineAsync(str);
            }
            catch (Exception ex)
            {

            }
        }
    }
}
