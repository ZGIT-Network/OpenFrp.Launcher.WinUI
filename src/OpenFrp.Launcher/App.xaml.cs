using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ModernWpf.Controls.Primitives;
using Newtonsoft.Json;
using OpenFrp.Core.Helper;
using OpenFrp.Launcher.Helper;
using OpenFrp.Launcher.Views;

namespace OpenFrp.Launcher
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// 加载事件 - Redo
        /// </summary>
        protected override async void OnStartup(StartupEventArgs e)
        {
            if (UxThemeHelper.IsSupportDarkMode)
            {
                UxThemeHelper.AllowDarkModeForApp(true);
                UxThemeHelper.ShouldSystemUseDarkMode();
                
            }

            JsonConvert.DefaultSettings = () => new()
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            await Core.Helper.ConfigHelper.ReadConfig();
            CreateWindow();
            Microsoft.Win32.SystemEvents.SessionEnding += OnSystemShutdowning;
        }

        /// <summary>
        /// 创建窗口
        /// </summary>
        private void CreateWindow()
        {
            var wind = new MainPage()
            {
                Width = 1366,
                Height = 768,
                Title = $"{new Random().NextDouble()} - Worker"
            };
            WindowHelper.SetSystemBackdropType(wind, ConfigHelper.Instance.BackdropSet);
            ThemeManager.SetRequestedTheme(wind, ConfigHelper.Instance.ThemeSet);
            wind.Show();
        }

        /// <summary>
        /// Launcher 退出时
        /// </summary>
        protected override async void OnExit(ExitEventArgs e)
        {
            await SaveConfig();
        }

        /// <summary>
        /// 系统正在关闭时
        /// </summary>
        private async void OnSystemShutdowning(object sender, Microsoft.Win32.SessionEndingEventArgs e)
        {
            // 保存 Config
            e.Cancel = true;
            await SaveConfig();
            e.Cancel = false;
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        private async ValueTask SaveConfig() => await ConfigHelper.Instance.WriteConfig();
    }
}
