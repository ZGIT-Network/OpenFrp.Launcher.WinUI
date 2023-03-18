using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using OpenFrp.Core;
using OpenFrp.Core.Helper;
using OpenFrp.Core.Libraries.Api;
using OpenFrp.Core.Libraries.Protobuf;
using OpenFrp.Launcher.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace OpenFrp.Launcher.ViewModels
{
    public partial class MainPageModel : ObservableObject
    {
        public MainPageModel()
        {
            WeakReferenceMessenger.Default.Register<PropertyChangedMessage<bool>>(this, (obj,message) =>
            {
                switch (message.PropertyName)
                {
                    case "HasDeamonProcess": HasDeamonProcess = message.NewValue; break;
                    case "HasAccount": OnPropertyChanged("UserInfo");break;
                }
            });
            HasDeamonProcess = AppShareHelper.HasDeamonProcess;
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor("AccountInfoCommand")]
        public bool hasDeamonProcess;

        [ObservableProperty]
        public bool hasFrpcUpdate;

        public Core.Libraries.Api.Models.ResponseBody.UserInfoResponse.UserInfo UserInfo
        {
            get => ApiRequest.UserInfo ?? new()
            {
                UserName = "未登录",
                Email = "点击开始登陆"
            };
        }

        public void UpdateProperty(string name) => OnPropertyChanged(name);

        bool CanExcuteAccountInfo() => HasDeamonProcess;

        public Core.Helper.UpdateCheckHelper.UpdateInfo? UpdateContent { get; set; }

        [RelayCommand (CanExecute = nameof(CanExcuteAccountInfo))]
        internal async void AccountInfo()
        {
            if (!ApiRequest.HasAccount)
            {
                var frame = ((Views.MainPage)App.Current.MainWindow).Of_nViewFrame;
                (((Views.MainPage)App.Current.MainWindow).Of_nView.SettingsItem as NavigationViewItem)!.IsSelected = true;

                if (frame.SourcePageType != typeof(Views.Setting))
                {
                    frame.Navigate(typeof(Views.Setting));
                    await Task.Delay(50);
                }
                var dialog = new Controls.LoginDialog();
                await dialog.ShowDialogFixed();


                if (frame.SourcePageType == typeof(Views.Setting))
                {
                    var models = ((SettingModel)((Views.Setting)frame.Content).DataContext);
                    models.HasAccount = ApiRequest.HasAccount;
                    models.UpdateProperty("HasAccount");
                }

            }

        }

        [RelayCommand]
        async void WindClosing(CancelEventArgs e)
        {
            e.Cancel = true;

            App.Current.MainWindow.Visibility = Visibility.Collapsed;


            var response = await AppShareHelper.PipeClient.Request(new RequestBase()
            {
                Action = RequestType.ClientGetRunningtunnelsid,
            });
            if (response.Success)
            {
                ConfigHelper.Instance.AutoStartupList = response.RunningCount.ToArray();
            }
        }

        [RelayCommand]
        async void UpdateFrpc()
        {
            var dialog = new ContentDialog()
            {
                Title = "更新 FRPC",
                PrimaryButtonText = "安装",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                Content = new TextBlock()
                {
                    Width = 350,
                    Height = 100,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Text = "FRPC安装包会关闭FRPC进程,请保证您不在使用远程桌面服务!!!",

                }
            };
            if (await dialog.ShowDialogFixed() is ContentDialogResult.Primary)
            {



                Process.Start(Path.Combine(Utils.ApplicationExecutePath, "OpenFrp.Core.exe"), $"--update {UpdateContent?.JSON()}");

                await ConfigHelper.Instance.WriteConfig();

                App.Current?.Shutdown();

                var resp = await AppShareHelper.PipeClient.Request(new()
                {
                    Action = Core.Libraries.Protobuf.RequestType.ClientCloseIo
                });

            }
        }

        [RelayCommand]
        public async void CheckUpdate(object args)
        {
            var ur = await Core.Helper.UpdateCheckHelper.CheckUpdate();
            if (ur.Level is UpdateCheckHelper.UpdateLevel.LauncherUpdate)
            {
                var dialog = new ContentDialog()
                {
                    Title = "启动器有新更新啦!",
                    Content = new TextBlock()
                    {
                        Width = 370,
                        MinHeight = 150,
                        TextWrapping = TextWrapping.Wrap,
                        Inlines =
                        {
                            new Run(ur.Content),
                            new Run($"{Utils.LauncherVersion} 更新到 {ur.Version}")
                            {
                                Foreground = (SolidColorBrush)App.Current.Resources["SystemControlForegroundBaseMediumBrush"]
                            }
                        }
                    },
                    DefaultButton = ContentDialogButton.Primary,
                    PrimaryButtonText = "下载并安装",
                    CloseButtonText = "取消"
                };
                if (await dialog.ShowDialogFixed() is ContentDialogResult.Primary)
                {

                    Process.Start(Path.Combine(Utils.ApplicationExecutePath, "OpenFrp.Core.exe"), $"--update {ur.JSON()}");

                    await ConfigHelper.Instance.WriteConfig();

                    App.Current?.Shutdown();

                    var resp = await AppShareHelper.PipeClient.Request(new()
                    {
                        Action = Core.Libraries.Protobuf.RequestType.ClientCloseIo
                    });
                }
            }
            else if (ur.Level is UpdateCheckHelper.UpdateLevel.FrpcUpdate)
            {
                HasFrpcUpdate = true;
                UpdateContent = ur;
            }
            else
            {
                if (args is bool) return;

                await new ContentDialog()
                {
                    Content = new TextBlock()
                    {
                        Width = 370,
                        MinHeight = 150,
                        TextWrapping = TextWrapping.Wrap,
                        Inlines =
                        {
                            new Run("已经没啥更新了咯。"),
                            
                        }
                    },
                    DefaultButton = ContentDialogButton.Close,
                    CloseButtonText = "确定"
                }.ShowDialogFixed();
            }
        }
    }
}
