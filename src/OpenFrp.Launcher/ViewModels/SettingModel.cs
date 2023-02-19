using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.Mvvm.Messaging;
using ModernWpf.Controls.Primitives;
using OpenFrp.Core;
using OpenFrp.Core.Helper;
using OpenFrp.Core.Libraries.Api;
using OpenFrp.Launcher.Helper;
using OpenFrp.Launcher.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace OpenFrp.Launcher.ViewModels
{
    public partial class SettingModel : ObservableObject
    {
        public SettingModel()
        {
            WeakReferenceMessenger.Default.Register<PropertyChangedMessage<bool>>(this, (obj, message) =>
            {
                switch (message.PropertyName)
                {
                    case "HasDeamonProcess": HasDeamonProcess = message.NewValue;break;
                    case "HasAccount": HasAccount = ApiRequest.HasAccount;break;
                }
            });

            HasAccount = ApiRequest.HasAccount;
            HasDeamonProcess = AppShareHelper.HasDeamonProcess;
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor("AccountInfoCommand", "ControlSerivceModeCommand")]
        public bool hasDeamonProcess;
        /// <summary>
        /// 设置示例
        /// </summary>
        public ConfigHelper SettingInstance
        {
            get => ConfigHelper.Instance;
        }

        public bool AutoStartupLauncher
        {
            get => File.Exists(Utils.AutoLaunchLink);
            set
            { 
                if (value)
                {
                    var shortcut = (IWshRuntimeLibrary.IWshShortcut)new IWshRuntimeLibrary.WshShell().CreateShortcut(Utils.AutoLaunchLink);
                    shortcut.TargetPath = Path.Combine(Utils.ApplicationExecutePath,"OpenFrp.Launcher.exe");
                    shortcut.Arguments = "--minimize";
                    shortcut.Description = "OpenFrp Launcher 开机自启动";
                    shortcut.Save();
                }
                else
                {
                    File.Delete(Utils.AutoLaunchLink);
                }
            }
        }



        [ObservableProperty]
        public bool hasAccount;

        public bool IsSupportBackdrop
        {
            get => OSVersionHelper.OSVersion >= new Version(10, 0, 21996);
        }
        public bool IsSupportBackdropMicaAlt
        {
            get => OSVersionHelper.OSVersion >= new Version(10, 0, 22523);
        }
        public bool IsSupportToast
        {
            get => OSVersionHelper.IsWindows10OrGreater;
        }

        /// <summary>
        /// 登录 / 个人信息
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExcuteDeamonControl))]
        async void AccountInfo(Button element)
        {
            // 如果未登录
            if (!HasAccount)
            {
                var dialog = new Controls.LoginDialog();
                await dialog.ShowDialogFixed();
            }
            else
            {
                var request = new Core.Libraries.Protobuf.RequestBase()
                {
                    Action = Core.Libraries.Protobuf.RequestType.ClientPushClearlogin,
                };
                var response = await AppShareHelper.PipeClient.Request(request);

                if (response.Success)
                {
                    ApiRequest.ClearAuth();
                    ConfigHelper.Instance.Account.ClearAccount();
                }
                else
                {
                    MessageBox.Show($"在推送给服务端的时候发生了错误: {(response.HasException ? response.Exception : response.Message)}", "OpenFrp Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            HasAccount = ApiRequest.HasAccount;
            UpdateProperty(nameof(HasAccount));
            
        }

        /// <summary>
        /// 安装 / 卸载系统服务模式
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExcuteDeamonControl))]
        async void ControlSerivceMode()
        {
            if (!SettingInstance.IsServiceMode)
            {
                var dialog = new ContentDialog()
                {
                    Title = "安装服务",
                    Content = new TextBlock()
                    {
                        Width = 350,
                        Height = 100,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Inlines =
                        {
                            new Run("如果您正在使用远程桌面之类的服务,请谨慎操作."),
                            new LineBreak(),
                            new Run("安装需要一定的时间,请耐心等待。"),
                            new LineBreak(),
                            new Run("稳定性良好，您需要忍受偶然发生的BUG.")
                        }
                    },
                    PrimaryButtonText = "安装",
                    CloseButtonText = "取消",
                    DefaultButton = ContentDialogButton.Primary,
                };
                if (await dialog.ShowDialogFixed() is ContentDialogResult.Primary)
                {
                    var resp = await AppShareHelper.PipeClient.Request(new()
                    {
                        Action = Core.Libraries.Protobuf.RequestType.ClientCloseIo
                    });
                    if (!resp.Success)
                    {
                        new ProcessStartInfo(Path.Combine(Utils.ApplicationExecutePath, "OpenFrp.Core.exe"), "--install").RunAsUAC();
                    }
                    else
                    {
                        MessageBox.Show("由于管道未完全退出,本次安装失败。", "OpenFrp.Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    App.Current.Shutdown();
                    // 开始安装
                }
            }
            else
            {
                var dialog = new ContentDialog()
                {
                    Title = "卸载服务",
                    Content = new TextBlock()
                    {
                        Width = 350,
                        Height = 100,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Inlines =
                        {
                            new Run("如果您正在使用远程桌面之类的服务,请谨慎操作."),
                            new LineBreak(),
                            new Run("卸载需要一定的时间,请耐心等待。"),
                            new LineBreak(),
                            new Run("如换成守护进程模式,开机自启需要进入桌面才可进行。"),
                        }
                    },
                    PrimaryButtonText = "卸载",
                    CloseButtonText = "取消",
                    DefaultButton = ContentDialogButton.Primary,
                };
                if (await dialog.ShowDialogFixed() is ContentDialogResult.Primary)
                {
                    var resp = await AppShareHelper.PipeClient.Request(new()
                    {
                        Action = Core.Libraries.Protobuf.RequestType.ClientCloseIo
                    });
                    if (!resp.Success)
                    {
                        new ProcessStartInfo(Path.Combine(Utils.ApplicationExecutePath, "OpenFrp.Core.exe"), "--uninstall").RunAsUAC();
                    }
                    else
                    {
                        MessageBox.Show("由于管道未完全退出,我们会尝试强制卸载，您的个人配置将会丢失。", "OpenFrp.Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
                        new ProcessStartInfo(Path.Combine(Utils.ApplicationExecutePath, "OpenFrp.Core.exe"), "--uap").RunAsUAC();
                    }
                    App.Current.Shutdown();
                }
            }
        }

        bool CanExcuteDeamonControl() => HasDeamonProcess;

        public void UpdateProperty(string name) => OnPropertyChanged(name);
    }
}
