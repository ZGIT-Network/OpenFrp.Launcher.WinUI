using ModernWpf;
using ModernWpf.Controls.Primitives;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
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
                if (Utils.MainWindow is Window wind && !Utils.IsWindowsService)
                    try { ModernWpf.Controls.Primitives.WindowHelper.SetSystemBackdropType(wind, value); }
                    catch { }
                _backdropSet = value;
            }
        }
        /// <summary>
        /// 绕过代理
        /// </summary>
        [JsonProperty("bypassProxy")]
        public bool BypassProxy { get; set; } = true;

        /// <summary>
        /// 是否为系统服务模式
        /// </summary>
        [JsonProperty("serviceMode")]
        public bool IsServiceMode { get; set; }

        /// <summary>
        /// 账户设置
        /// </summary>
        [JsonProperty("account")]
        public UserAccount Account { get; set; } = new();
        /// <summary>
        /// 字体设置
        /// </summary>
        [JsonProperty("fontSet")]
        public FontSetting FontSet { get; set; } = new();
        /// <summary>
        /// 自启动隧道列表
        /// </summary>
        [JsonProperty("autoStart")]
        public int[] AutoStartupList { get; set; } = new int[] { };

        [JsonProperty("frpc_ver")]
        public string? FrpcVersion { get; set; }


        [JsonProperty("pullMode")]
        public TnMode MessagePullMode { get; set; }

        [JsonProperty("debug")]
        public bool DebugMode { get; set; } = false;

        [JsonProperty("force_tls")]
        public bool ForceTLS { get; set; } = false;

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
            private string _Password { get; set; } = string.Empty;

            [JsonIgnore]
            public string? Password
            {
                get
                {
                    try
                    {
                        if (_Password.Length <= 0) { return default; }
                        else return DESCrypto.Descrypt(_Password);//ProtectedData.Unprotect(_Password.ToArray(), Utils.ConfigFile.GetBytes(), DataProtectionScope.CurrentUser).GetString();
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Add(0, $"由于密码解析错误,可能本次登录无法成功。\nException: {ex}", TraceLevel.Error, true);
                        return default;
                    }
                }
                set
                {
                    if (value is null)
                    {
                        _Password = "";
                        return;
                    }
                    else _Password = DESCrypto.EncryptString(value);//ProtectedData.Protect(value.GetBytes(), Utils.ConfigFile.GetBytes(), DataProtectionScope.CurrentUser).ToList();
                }
            }
        }

        public class FontSetting
        {
            public string? FontFamily { get; set; }

            public double FontSize { get; set; } = 14;
        }

        /// <summary>
        /// 读取配置
        /// </summary>
        public static async ValueTask ReadConfig()
        {
            try
            {
                if (File.Exists(Utils.ConfigFile))
                {
                    
                    Instance = JsonConvert.DeserializeObject<ConfigHelper>(await Task.Run(() => File.ReadAllText(Utils.ConfigFile)))
                            ?? throw new Exception("配置文件解析得到 NULL.");
                    if (Instance.BackdropSet is BackdropType.None or 0)
                        Instance.BackdropSet = GetSupportMax();
                    if (Instance.MessagePullMode is TnMode.None or 0)
                        Instance.MessagePullMode = GetTnMode();
                }
                else Directory.CreateDirectory(Utils.ApplicatioDataPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"配置文件解析出错了捏。\nException Object: \n{ex}","OpenFrp.Launcher.UI",MessageBoxButton.OK,MessageBoxImage.Error);
            }

        }


        /// <summary>
        /// 写配置
        /// </summary>
        public async ValueTask WriteConfig(bool fastWrite = false)
        {
            try
            {
                var dir = new DirectoryInfo(Utils.ApplicatioDataPath);
                var acl = dir.GetAccessControl(System.Security.AccessControl.AccessControlSections.Access);
                acl.SetAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
                    new SecurityIdentifier(WellKnownSidType.LocalServiceSid, null),
                    System.Security.AccessControl.FileSystemRights.FullControl,
                    System.Security.AccessControl.InheritanceFlags.ObjectInherit,
                    System.Security.AccessControl.PropagationFlags.None,
                    System.Security.AccessControl.AccessControlType.Allow));
                dir.SetAccessControl(acl);

                if (!fastWrite)
                {
                    string str = Instance.JSON();

                    var reader = Utils.ConfigFile.GetStreamWriter(autoFlush: true);
                    await reader.WriteLineAsync(str);
                    await reader.FlushAsync();
                    reader.Close();
                }
                else File.WriteAllText(Utils.ConfigFile, ConfigHelper.Instance.JSON());

            }
            catch (Exception ex)
            {
                LogHelper.Add(0, ex.ToString(), System.Diagnostics.TraceLevel.Warning, true);
            }
        }



        static BackdropType GetSupportMax()
        {
            if (!Utils.IsWindowsService)
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
            }
            return BackdropType.None;

        }

        static TnMode GetTnMode() => OSVersionHelper.IsWindows10OrGreater? TnMode.Toast : TnMode.Notifiy;

        public enum TnMode
        {
            None,
            Notifiy,
            Toast
        }

    }
}
