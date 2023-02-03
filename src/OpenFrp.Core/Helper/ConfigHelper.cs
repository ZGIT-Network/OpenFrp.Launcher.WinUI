using ModernWpf.Controls.Primitives;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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

        /// <summary>
        /// 主题设置
        /// </summary>
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
        /// <summary>
        /// 背景设置
        /// </summary>
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
        /// <summary>
        /// 绕过代理
        /// </summary>
        [JsonProperty("bypassProxy")]
        public bool BypassProxy { get; set; }


        [JsonProperty("account")]
        public UserAccount Account { get; set; } = new();

        public class UserAccount
        {

            public UserAccount(string? userName, string? password)
            {
                UserName = userName;
                Password = password;
            }
            public UserAccount() { }

            [JsonIgnore]
            public bool HasAccount { get => !string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Password); }

            public void ClearAccount() { UserName = Password = default; }

            [JsonProperty("username")]
            public string? UserName { get; set; }

            [JsonProperty("password")]
            private List<byte> _Password { get; set; } = new();

            [JsonIgnore]
            public string? Password
            {
                get
                {
                    if (_Password.Count <= 0) { return default; }
                    else return ProtectedData.Unprotect(_Password.ToArray(), Utils.ConfigFile.GetBytes(), DataProtectionScope.CurrentUser).GetString();
                }
                set
                {
                    if (value is null)
                    {
                        _Password.Clear();
                        return;
                    }
                    else _Password = ProtectedData.Protect(value.GetBytes(), Utils.ConfigFile.GetBytes(), DataProtectionScope.CurrentUser).ToList();
                }
            }
        }
        /// <summary>
        /// 读取配置
        /// </summary>
        public static async ValueTask ReadConfig()
        {
            if (File.Exists(Utils.ConfigFile))
            {
                Instance = (await Utils.ConfigFile.GetStreamReader().ReadToEndAsync())
                    .PraseJson<ConfigHelper>() ?? new()
                    {
                        BackdropSet = GetSupportMax()
                    };
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
                Utils.Log(ex, true);
            }
        }

        static BackdropType GetSupportMax()
        {
            if (MicaHelper.IsSupported(BackdropType.Tabbed))
            {
                return BackdropType.Tabbed;
            }
            else if (MicaHelper.IsSupported(BackdropType.Mica))
            {
                return BackdropType.Mica;
            }
            else if (AcrylicHelper.IsAcrylicSupported())
            {
                return BackdropType.Acrylic;
            }
            return BackdropType.None;
        }

    }
}
