using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using OpenFrp.Core;
using OpenFrp.Core.Helper;

namespace OpenFrp.Launcher.Views
{
    /// <summary>
    /// About.xaml 的交互逻辑
    /// </summary>
    public partial class About : Controls.ViewPage
    {
        public About()
        {
            InitializeComponent();
        }

        private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.deamon is Process deamon)
            {
                deamon.EnableRaisingEvents = false;
                deamon.Kill();

                new ProcessStartInfo("cmd", "/c taskkill /f /im OpenFrp.Core.exe").RunAsUAC();

                CheckDeamon();

            }
        }

        private void CheckDeamon()
        {
            if (ConfigHelper.Instance.IsServiceMode = Win32Helper.IsServiceInstalled())
            {
                if (!Win32Helper.CheckServiceIsRunning())
                    // 有服务的模式下 
                    if (!new ProcessStartInfo("sc", "start \"OpenFrp Launcher Service\"").RunAsUAC())
                    {
                        Environment.Exit(0);
                    }
            }
            else
            {
                try
                {
                    if (Process.GetProcessesByName("OpenFrp.Core").Length is 0)
                    {
                        App.deamon = Process.Start(new ProcessStartInfo(Path.Combine(Utils.ApplicationExecutePath, "OpenFrp.Core.exe"), "-ps")
                        {
                            CreateNoWindow = !Debugger.IsAttached,
                            UseShellExecute = false,
                            WorkingDirectory = Utils.ApplicationExecutePath,
                        });
                        App.deamon.EnableRaisingEvents = true;
                        App.deamon.Exited += (sender, args) => CheckDeamon();
                    }
                    return;
                }
                catch
                {

                }
                Environment.Exit(0);
            }
        }
    }
}
