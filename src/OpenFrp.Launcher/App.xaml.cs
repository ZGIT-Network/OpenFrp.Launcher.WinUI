using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Google.Protobuf;
using ModernWpf.Controls.Primitives;
using Newtonsoft.Json;
using OpenFrp.Core;
using OpenFrp.Core.Helper;
using OpenFrp.Core.Libraries.Pipe;
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
            await ConfigHelper.ReadConfig();
            Microsoft.Win32.SystemEvents.SessionEnding += async (sender, e) =>
            {
                // 保存 Config
                e.Cancel = true;
                await ConfigHelper.Instance.WriteConfig();
                e.Cancel = false;
            };
            
            CreateWindow();

            PipeIOStart();

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

        private void PipeIOStart()
        {
            AppShareHelper.PipeClient.Start();
            // 服务端推送到客户端
            var pushClient = new PipeClient();
            pushClient.Start(true);
            pushClient.OnPushStart = async worker =>
            {
                AppShareHelper.HasDeamonProcess = true;
                while (worker.IsConnected && worker.IsPushMode)
                {
                    int count;
                    try
                    {
                        count = await worker.ReadAsync();
                        if (count > 0)
                        {
                            var request = Core.Libraries.Protobuf.RequestBase.Parser.ParseFrom(worker.Buffer, 0, worker.EnsureMessageComplete(count));
                            Core.Libraries.Protobuf.ResponseBase response = request.Action switch
                            {
                                _ => new()
                                {
                                    Message = "Action not found",
                                    Success = false
                                },
                            };
                            await worker.SendAsync(response.ToByteArray());
                        }
                        else
                        {
                            AppShareHelper.PipeClient.Disconnect();
                            AppShareHelper.HasDeamonProcess = false;
                            Utils.Log("Service IO was Closed.", true);
                            // 在PipeServer被关闭时，会发送一个 长度为 0 的数据包
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Utils.Log(ex, true);
                        break;
                    }
                }
                worker.Client?.Close();
                worker.IsPushMode = false;
                // 先等待1500秒
                await Task.Delay(1500);
                PipeIOStart();
            };
        }


        /// <summary>
        /// Launcher 退出时
        /// </summary>
        protected override async void OnExit(ExitEventArgs e)
        {
            await ConfigHelper.Instance.WriteConfig();
        }
    }
}
