using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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


        internal static Process? deamon;
        /// <summary>
        /// 加载事件 - Redo
        /// </summary>
        protected override async void OnStartup(StartupEventArgs e)
        {
            if (OSVersionHelper.IsWindows10OrGreater && ToastNotificationManagerCompat.WasCurrentProcessToastActivated())
            {
                Environment.Exit(0);
                return;
            }
            CheckMultiLauncher();


            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Clipboard.SetText(e.ExceptionObject.ToString());
                if (MessageBox.Show($"错误内容已复制，按下Ctrl+V | 粘贴 来显示内容。点击\"确定\"按钮，会打开到兔小巢反馈问题。", "OpenFrp Launcher Throw Out!!", MessageBoxButton.OKCancel, MessageBoxImage.Error) is MessageBoxResult.OK)
                {
                    Process.Start("https://support.qq.com/product/424684#label=show");
                }
                Environment.Exit(-1);
            };
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) =>
            {
                if (errors is not System.Net.Security.SslPolicyErrors.None)
                {
                    return MessageBox.Show($"来自服务器的证书无效:::" +
                    $"\n Name:{certificate.Issuer}" +
                    $"\n Reason:{Enum.GetName(typeof(System.Net.Security.SslPolicyErrors), errors)} " +
                    $"\n是否允许访问?", "OpenFrp Launcher", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation) is MessageBoxResult.OK ? true
                 : false;
                }
                return true;
            };
            JsonConvert.DefaultSettings = () => new()
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            await ConfigHelper.ReadConfig();

            if (File.Exists(Utils.Frpc))
            {
                try
                {
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = Utils.Frpc,
                        Arguments = $"-v",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });

                    if (UxThemeHelper.IsSupportDarkMode)
                    {
                        UxThemeHelper.AllowDarkModeForApp(true);
                        UxThemeHelper.ShouldSystemUseDarkMode();
                    }

                    CheckDeamon();

                    Microsoft.Win32.SystemEvents.SessionEnding += async (sender, e) =>
                    {
                        // 保存 Config
                        e.Cancel = true;
                        await ConfigHelper.Instance.WriteConfig(true);
                        e.Cancel = false;
                    };

                    if (OSVersionHelper.IsWindows10OrGreater)
                    {
                        ToastNotificationManagerCompat.OnActivated += (e) =>
                        {
                            if (!string.IsNullOrEmpty(e.Argument))
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
                            }
                            
                        };
                    }
                    #region TaskBar Icon
                    AppShareHelper.TaskbarIcon = new()
                    {
                        MenuActivation = H.NotifyIcon.Core.PopupActivationMode.RightClick,
                        NoLeftClickDelay = true,
                        IconSource = new BitmapImage(new Uri("pack://application:,,,/OpenFrp.Launcher;component/Resourecs/main.ico"))
                        //Icon =
                        //  new System.Drawing.Icon(GetResourceStream(new Uri("")).Stream)
                    };
                    AppShareHelper.TaskbarIcon.TrayLeftMouseUp += (sender, args) =>
                    {
                        if (App.Current.MainWindow is Window wind)
                        {
                            wind.Visibility = Visibility.Visible;
                            if (wind.WindowState is WindowState.Minimized && !wind.IsActive)
                            {
                                wind.WindowState = WindowState.Normal;
                            }
                            wind.Activate();
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
                    AppShareHelper.TaskbarIcon.ContextMenu.MinWidth = 150;
                    AppShareHelper.TaskbarIcon.ForceCreate(false);
                    #endregion
                    AutoLogin();
                    PipeIOStart();
                    if (e.Args.Contains("-noeffect")) ConfigHelper.Instance.BackdropSet = BackdropType.None;
                    CreateWindow();
                }
                catch (UnauthorizedAccessException)
                {
                    ConfigHelper.Instance.FrpcVersion = "unset";
                    await ConfigHelper.Instance.WriteConfig(true);
                    await Task.Delay(500);
                    new ProcessStartInfo(Path.Combine(Utils.ApplicationExecutePath, "OpenFrp.Core.exe"), "--update frpcDownload").RunAsUAC();
                    App.Current.Shutdown();
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    if (ex.Message.Contains("垃圾软件") || ex.Message.Contains("拒绝访问"))
                    {
                        Process.Start("https://docs.openfrp.net/use/desktop-launcher.html#%E5%8A%A0%E5%85%A5%E7%B3%BB%E7%BB%9F%E7%99%BD%E5%90%8D%E5%8D%95");
                    }
                    else
                    {
                        MessageBox.Show($"可能是因为杀软的原因,FRPC 版本检查失败\nException Object:\n{ex}");
                    }
                    App.Current.Shutdown();
                }
            }
            else
            {
                ConfigHelper.Instance.FrpcVersion = "unset";

                new ProcessStartInfo(Path.Combine(Utils.ApplicationExecutePath, "OpenFrp.Core.exe"), "--update frpcDownload").RunAsUAC();

                App.Current.Shutdown();
            }



        }

        /// <summary>
        /// 创建窗口
        /// </summary>
        private void CreateWindow()
        {
            var wind = new MainPage()
            {
                Width = SystemParameters.FullPrimaryScreenWidth <= 1366 ? 1024 : 1366,
                Height = SystemParameters.FullPrimaryScreenHeight <= 768 ? 576 : 768,
                Title = $"OpenFrp Launcher - {Path.Combine(Utils.ApplicationExecutePath, "OpenFrp.Launcher.exe").GetMD5()}"
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

                    if (request is not null)
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
                            // 2023.5.29 修复了提示"未连接到**"但是可以打开隧道页面的问题
                            ResponseBase response = request.Action switch
                            {
                                RequestType.ServerUpdateTunnels => new Func<ResponseBase>(() =>
                                {
                                    
                                    try
                                    {
                                        if (App.Current?.MainWindow is MainPage wind)
                                        {
                                            if (wind.Of_nViewFrame.Content is Tunnels tunnels)
                                            {
                                                tunnels.Model.RefreshUserProxies();
                                            }
                                        }
                                    }
                                    catch
                                    {

                                    }


                                    return new ResponseBase()
                                    {
                                        Success = true
                                    };
                                })(),
                                RequestType.ServerSendNotifiy => new Func<ResponseBase>(() =>
                                {
                                    try
                                    {
                                        var tunnel = request.NotifiyRequest.TunnnelJson.PraseJson<ResponseBody.UserTunnelsResponse.UserTunnel>();
                                        if (ConfigHelper.Instance.MessagePullMode is ConfigHelper.TnMode.Toast)
                                        {
                                            if (OSVersionHelper.IsWindows10OrGreater)
                                            {
                                                if (request.NotifiyRequest.Flag)
                                                {
                                                    new ToastContentBuilder()
                                                        .AddText($"隧道 {tunnel?.TunnelName} 启动成功!")
                                                        .AddText($"点击复制按钮，将链接发给你的朋友吧！")
                                                        .AddAttributionText($"{tunnel?.TunnelType},{tunnel?.LocalAddress}:{tunnel?.LocalPort}")
                                                        .AddButton(new ToastButton() { ActivationType = ToastActivationType.Foreground }
                                                                .SetContent("复制连接")
                                                                .AddArgument("--cl", tunnel?.ConnectAddress))
                                                        .AddButton("确定", ToastActivationType.Foreground, "")
                                                        .Show();
                                                }
                                                else
                                                {
                                                    new ToastContentBuilder()
                                                            .AddText($"隧道 {tunnel?.TunnelName} 启动失败")
                                                            .AddText(request.NotifiyRequest.Content)
                                                            .AddAttributionText($"远程端口: {tunnel?.RemotePort}")
                                                            .AddButton("确定", ToastActivationType.Foreground, "")

                                                            .Show();
                                                }
                                            }
                                            else
                                            {
                                                ConfigHelper.Instance.MessagePullMode = ConfigHelper.TnMode.Notifiy;
                                                AppShareHelper.TaskbarIcon?.ShowNotification($"OpenFrp 隧道 {tunnel?.TunnelName} 启动{(request.NotifiyRequest.Flag ? "成功" : "失败")}", "",
                                                request.NotifiyRequest.Flag ? H.NotifyIcon.Core.NotificationIcon.Info : H.NotifyIcon.Core.NotificationIcon.Warning);
                                            }

                                        }
                                        else if (ConfigHelper.Instance.MessagePullMode is ConfigHelper.TnMode.Notifiy)
                                        {
                                            AppShareHelper.TaskbarIcon.ShowNotification(
                                                $"OpenFrp 隧道 {tunnel?.TunnelName} 启动{(request.NotifiyRequest.Flag ? "成功" : "失败")}", 
                                                $"{tunnel?.LocalAddress}:{tunnel?.LocalPort} 转发到 {tunnel?.ConnectAddress}",
                                                request.NotifiyRequest.Flag ? H.NotifyIcon.Core.NotificationIcon.Info : H.NotifyIcon.Core.NotificationIcon.Warning);
                                            //AppShareHelper.TaskbarIcon?.ShowNotification(,
                                            //    request.NotifiyRequest.Flag ? H.NotifyIcon.Core.NotificationIcon.Info : H.NotifyIcon.Core.NotificationIcon.Warning);
                                        }
                                    }
                                    catch
                                    {

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
                            LogHelper.Add(0, "Service IO Closed.".ToString(), System.Diagnostics.TraceLevel.Warning, true);
                            // 在PipeServer被关闭时，会发送一个 长度为 0 的数据包
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Add(0, ex.ToString(), System.Diagnostics.TraceLevel.Warning, true);
                        break;
                    }
                }
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
                    var processes = Process.GetProcessesByName("OpenFrp.Core");
                    if (processes.Length is 0)
                    {
                        bool request = true;
                        foreach (var process in processes)
                        {
                            if (process.MainModule.FileName == Path.Combine(Utils.ApplicationExecutePath, "OpenFrp.Core.exe"))
                            {
                                request = false;
                            }
                        }
                        if (request)
                        {
                            deamon = Process.Start(new ProcessStartInfo(Path.Combine(Utils.ApplicationExecutePath, "OpenFrp.Core.exe"), "-ps")
                            {
                                CreateNoWindow = !Debugger.IsAttached,
                                UseShellExecute = false,
                                WorkingDirectory = Utils.ApplicationExecutePath,
                            });
                            deamon.EnableRaisingEvents = true;
                            deamon.Exited += async delegate
                            {
                                await Task.Delay(1500);
                                CheckDeamon();
                            };
                        }

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
            await ConfigHelper.Instance.WriteConfig(true);
            if (OSVersionHelper.IsWindows10OrGreater) ToastNotificationManagerCompat.History.Clear();
            AppShareHelper.TaskbarIcon?.Dispose();
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
        public static async void ExitLauncher()
        {
            await ConfigHelper.Instance.WriteConfig(true);
            App.Current.Shutdown();
        }

        [RelayCommand]
        public static async void ExitAll()
        {
            var request_tun2 = new RequestBase()
            {
                Action = RequestType.ClientGetRunningtunnelsid,
            };
            var response = await Helper.AppShareHelper.PipeClient.Request(request_tun2);

            if (response.Success)
            {
                ConfigHelper.Instance.AutoStartupList = response.RunningCount.ToArray();
            }
            else ConfigHelper.Instance.AutoStartupList = new int[0];

            if (deamon is not null)
                deamon.EnableRaisingEvents = false;
            var resp = await AppShareHelper.PipeClient.Request(new()
            {
                Action = RequestType.ClientCloseIo
            });
            if (resp.Success)
            {
                if (ConfigHelper.Instance.IsServiceMode)
                {
                    new ProcessStartInfo("sc", "stop \"OpenFrp Launcher Service\"").RunAsUAC();
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

                if (deamon is not null) { deamon.EnableRaisingEvents = false; }
                if (deamon?.HasExited is false) { deamon.Kill(); }

                foreach (var process in Process.GetProcessesByName("OpenFrp.Core.exe"))
                {
                    if (process.MainModule.FileName == Path.Combine(Utils.ApplicationExecutePath, "OpenFrp.Core.exe"))
                    {
                        process.Kill();
                    }
                };
            }
            catch
            {

            }

            ExitLauncher();
        }

        private void CheckMultiLauncher()
        {
            try
            {
                var processes = Process.GetProcessesByName("OpenFrp.Launcher");
                if (processes.Length > 1)
                {
                    foreach (var process in processes)
                    {
                        if (process.MainModule.FileName == Path.Combine(Utils.ApplicationExecutePath, "OpenFrp.Launcher.exe"))
                        {
                            IntPtr hwnd = WindHelper.FindWindowA(null, $"OpenFrp Launcher - {process.MainModule.FileName.GetMD5()}");
                            if (hwnd != IntPtr.Zero)
                            {
                                WindHelper.ShowWindow(hwnd, 9);
                                WindHelper.SetForegroundWindow(hwnd);
                                Environment.Exit(0);
                            }
                        }
                    }

                }
            }
            catch
            {

            }
            
        }
    }

    public static class ExtendsUI
    {
        /// <summary>
        /// 修复了一个窗口可以弹出两个的问题
        /// </summary>
        public async static ValueTask<ContentDialogResult> ShowDialogFixed(this ContentDialog dialog)
        {
            if(App.Current?.MainWindow is Window wind)
            {
                var dialogWind = ContentDialog.GetOpenDialog(wind);

                if (dialog is not null)
                {
                    dialogWind?.Hide();
                }
                return await dialog!.ShowAsync();
            }
            return ContentDialogResult.None;
        }

        public static void VoidAsync(System.Windows.Threading.DispatcherOperation operation)
        {

        }
    }
}