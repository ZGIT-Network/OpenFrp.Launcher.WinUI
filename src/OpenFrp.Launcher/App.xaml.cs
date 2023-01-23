using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

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
        protected override void OnStartup(StartupEventArgs e)
        {
            var wind = new Window()
            {
                Width = 1366,
                Height = 768,
                ShowActivated = true,
                Title = $"{new Random().NextDouble()} - Worker"
            };

            ModernWpf.Controls.TitleBar.SetExtendViewIntoTitleBar(wind, true);
            wind.Show();
        }
    }
}
