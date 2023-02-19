using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Notifications;
using Google.Protobuf;
using ModernWpf.Controls.Primitives;
using Newtonsoft.Json;
using OpenFrp.Core;
using OpenFrp.Core.Helper;
using OpenFrp.Core.Libraries.Api;
using OpenFrp.Core.Libraries.Api.Models;
using OpenFrp.Core.Libraries.Pipe;
using OpenFrp.Core.Libraries.Protobuf;
using OpenFrp.Launcher.Helper;
using OpenFrp.Launcher.Views;
using Windows.UI.Notifications;

namespace OpenFrp.Launcher
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {


        private Process? deamon;
        /// <summary>
        /// 加载事件 - Redo
        /// </summary>
        protected override async void OnStartup(StartupEventArgs e)
        {
            CheckDeamon();

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

            if (OSVersionHelper.IsWindows10OrGreater)
            {
                ToastNotificationManagerCompat.History.Clear();
                ToastNotificationManagerCompat.OnActivated += (e) =>
                {
                    var args = ToastArguments.Parse(e.Argument);
                    App.Current.RunOnUIThread(() =>
                    {
                        try
                        {
                            Clipboard.SetText(args["--cl"]);
                        }
                        catch
                        {

                        }
                    });

                };
            }
            AppShareHelper.TaskbarIcon.Icon = 
                new System.Drawing.Icon(GetResourceStream(new Uri("pack://application:,,,/OpenFrp.Launcher;component/Resourecs/main.ico")).Stream);
            AppShareHelper.TaskbarIcon.TrayLeftMouseUp += (sender, args) =>
            {
                if (App.Current.MainWindow is Window wind)
                {
                    wind.Visibility = Visibility.Visible;
                    wind.Activate();
                    if (wind.WindowState is WindowState.Minimized)
                    {
                        wind.WindowState = WindowState.Normal;
                    }
                }
            };
            AppShareHelper.TaskbarIcon.ContextMenu = new()
            {
                Items =
                {
                    new MenuItem()
                    {
                        Icon = new SymbolIcon(Symbol.ShowResults),
                        Header = "显示窗口",
                        Command = ShowWindowCommand
                    },
                    new Separator(),
                    new MenuItem()
                    {
                        Icon = new FontIcon(){ Glyph = "\ue89f" },
                        Header = "退出启动器",
                        Command = ExitLauncherCommand
                    },
                    new MenuItem()
                    {
                        Icon = new FontIcon(){Glyph = "\ue8bb"},
                        Header = "彻底退出",
                        Command = ExitAllCommand

                    }

                }
            };
            AutoLogin();
            PipeIOStart();
            CreateWindow();
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
        
        private async void AutoLogin()
        {
            if (ConfigHelper.Instance.Account.HasAccount)
            {
                if (/* 系统服务模式 不需要等待 */ !false) await Task.Delay(1500);
                var resp = await AppShareHelper.LoginAndGetUserInfo(ConfigHelper.Instance.Account.UserName!, ConfigHelper.Instance.Account.Password!);

                if (resp.Success)
                {
                    var response = await Helper.AppShareHelper.PipeClient.Request(new RequestBase()
                    {
                        Action = RequestType.ClientGetRunningtunnelsid,
                    });
                    var request = await ApiRequest.UniversalPOST<ResponseBody.UserTunnelsResponse>(ApiUrls.UserTunnels);

                    if (request.Success && response.Success)
                    {
                        if (ConfigHelper.Instance.AutoStartupList.Length > 0)
                        {
                            foreach (ResponseBody.UserTunnelsResponse.UserTunnel tunnel in request.Data?.List ?? new())
                            {
                                if (ConfigHelper.Instance.AutoStartupList.Contains(tunnel.TunnelId) && !response.RunningCount.Contains(tunnel.TunnelId))
                                {
                                    await AppShareHelper.PipeClient.Request(new RequestBase()
                                    {
                                        Action = RequestType.ClientFrpcStart,
                                        FrpRequest = new()
                                        {
                                            UserTunnelJson = tunnel.JSON()
                                        }
                                    });
                                }
                            }
                        }
                    }


                }

            }
        }

        private void PipeIOStart(bool restartup = false)
        {
            AppShareHelper.PipeClient.Start();
            // 服务端推送到客户端
            var pushClient = new PipeClient();
            pushClient.Start(true);
            pushClient.OnPushStart = async worker =>
            {
                AppShareHelper.HasDeamonProcess = true;
                if (ConfigHelper.Instance.Account.HasAccount && restartup)
                {
                    var resp =  await AppShareHelper.LoginAndGetUserInfo(ConfigHelper.Instance.Account.UserName, ConfigHelper.Instance.Account.Password);
                }
                while (worker.IsConnected && worker.IsPushMode)
                {
                    int count;
                    try
                    {
                        count = await worker.ReadAsync();
                        if (count > 0)
                        {
                            var request = RequestBase.Parser.ParseFrom(worker.Buffer, 0, worker.EnsureMessageComplete(count));
                            ResponseBase response = request.Action switch
                            {
                                RequestType.ServerUpdateTunnels => new Func<ResponseBase>(() =>
                                {
                                    if (App.Current?.MainWindow is MainPage wind)
                                    {
                                        if (wind.Of_nViewFrame.Content is Tunnels tunnels)
                                        {
                                            tunnels.Model.RefreshUserProxies();
                                        }
                                    }


                                    return new ResponseBase()
                                    {
                                        Success = true
                                    };
                                })(),
                                RequestType.ServerSendNotifiy => new Func<ResponseBase>(() =>
                                {
                                var tunnel = request.NotifiyRequest.TunnnelJson.PraseJson<ResponseBody.UserTunnelsResponse.UserTunnel>();
                                if (ConfigHelper.Instance.MessagePullMode is ConfigHelper.TnMode.Toast && OSVersionHelper.IsWindows10OrGreater)
                                {


                                        new ToastContentBuilder()
                                            .AddText($"隧道 {tunnel?.TunnelName} 启动成功!")
                                            .AddText($"点击复制按钮，分享给你的朋友吧!")
                                            .AddAttributionText($"{tunnel?.TunnelType},{tunnel?.LocalAddress}:{tunnel?.LocalPort}")
                                            .AddButton(new ToastButton(){ ActivationType = ToastActivationType.Foreground }
                                                    .SetContent("复制连接")
                                                    .AddArgument("--cl", tunnel?.ConnectAddress))

                                            .AddButton("确定", ToastActivationType.Foreground, "")
                                            .Show();
                                    }

                                    return new ResponseBase()
                                    {
                                        Success = true
                                    };
                                })(),
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
                            ApiRequest.ClearAuth();
                            ((ViewModels.MainPageModel)App.Current.MainWindow.DataContext).UpdateProperty("UserInfo");
                            if((((Views.MainPage)App.Current.MainWindow)).Of_nViewFrame.Content is Setting setting && setting.DataContext is ViewModels.SettingModel settingModel)
                            {
                                settingModel.HasAccount = false;
                            }
                            Utils.Log("Service IO Closed.", true);
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
                PipeIOStart(true);
            };

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
                        deamon = Process.Start(new ProcessStartInfo(Path.Combine(Utils.ApplicationExecutePath, "OpenFrp.Core.exe"), "-ps")
                        {
                            CreateNoWindow = !Debugger.IsAttached,
                            UseShellExecute = false,
                            WorkingDirectory = Utils.ApplicationExecutePath,
                        });
                        deamon.EnableRaisingEvents = true;
                        deamon.Exited += (sender, args) => CheckDeamon();
                    }
                    return;
                }
                catch
                {
                    
                }
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// Launcher 退出时
        /// </summary>
        protected override async void OnExit(ExitEventArgs e)
        {
            await ConfigHelper.Instance.WriteConfig();
            AppShareHelper.TaskbarIcon.Dispose();
        }
        /// <summary>
        /// 链接被点击时
        /// </summary>
        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            var h = (Hyperlink)sender;
            if (h.NavigateUri is not null &&
                h.Parent.GetType() != typeof(HyperlinkButton))
            {
                Process.Start(h!.NavigateUri.ToString());
            }
        }

        [RelayCommand]
        void ShowWindow() => Current.MainWindow.Visibility = Visibility.Visible;

        [RelayCommand]
        void ExitLauncher() => App.Current.Shutdown();

        [RelayCommand]
        async void ExitAll()
        {
            deamon.EnableRaisingEvents = false;
            var resp = await AppShareHelper.PipeClient.Request(new()
            {
                Action = RequestType.ClientCloseIo
            });
            if (resp.Success)
            {
                
                if (!ConfigHelper.Instance.IsServiceMode || !new ProcessStartInfo("sc", "stop \"OpenFrp Launcher Service\"").RunAsUAC())
                {
                    try
                    {
                        foreach (var process in Process.GetProcessesByName("OpenFrp.Core.exe"))
                        {
                            process.Kill();
                        };
                    }
                    catch
                    {

                    }
                }

            }
            try
            {
                foreach (var frpc in Process.GetProcessesByName($"{Utils.FrpcPlatform}.exe"))
                {
                    if (frpc.MainModule.FileName.Equals(Utils.Frpc))
                    {
                        frpc.Kill();
                    }
                }
            }
            catch
            {

            }
            App.Current.Shutdown();
        }
    }

    public static class ExtendsUI
    {
        /// <summary>
        /// 修复了一个窗口可以弹出两个的问题
        /// </summary>
        public async static ValueTask<ContentDialogResult> ShowDialogFixed(this ContentDialog dialog)
        {
            if (!AppShareHelper.HasDialog)
            {
                AppShareHelper.HasDialog = true;
                var result =  await dialog.ShowAsync();
                AppShareHelper.HasDialog = false;
                return result;
            }
            else
            {
                return ContentDialogResult.None;
            }
        }
    }
}