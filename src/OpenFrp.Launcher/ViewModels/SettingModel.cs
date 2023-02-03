using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ModernWpf.Controls.Primitives;
using OpenFrp.Core.Helper;
using OpenFrp.Core.Libraries.Api;
using OpenFrp.Launcher.Helper;
using OpenFrp.Launcher.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace OpenFrp.Launcher.ViewModels
{
    public partial class SettingModel : ObservableObject
    {
        public SettingModel()
        {
            if (App.Current.MainWindow.DataContext is MainPageModel model)
            model.PropertyChanging += (sender, args) =>
            {
                OnPropertyChanging("MainPageModel");
            };

            HasAccount = ApiRequest.HasAccount;
        }

        /// <summary>
        /// 主题
        /// </summary>
        public int ThemeSet
        {
            get => (int)Core.Helper.ConfigHelper.Instance.ThemeSet;
            set => Core.Helper.ConfigHelper.Instance.ThemeSet = (ElementTheme)value;
        }
        /// <summary>
        /// 背景
        /// </summary>
        public int BackdropSet
        {
            get => (int)Core.Helper.ConfigHelper.Instance.BackdropSet - 1;
            set => Core.Helper.ConfigHelper.Instance.BackdropSet = (BackdropType)value + 1;
        }
        /// <summary>
        /// 绕过代理
        /// </summary>
        public bool BypassProxy
        {
            get => Core.Helper.ConfigHelper.Instance.BypassProxy;
            set => Core.Helper.ConfigHelper.Instance.BypassProxy = value;
        }

        public MainPageModel MainPageModel
        {
            get => (MainPageModel)App.Current.MainWindow.DataContext;
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

        /// <summary>
        /// 登录 / 个人信息
        /// </summary>
        [RelayCommand]
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
        public void UpdateProperty(string name) => OnPropertyChanged(name);
    }
}
