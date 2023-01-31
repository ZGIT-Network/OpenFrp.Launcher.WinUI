using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ModernWpf.Controls.Primitives;
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
        }


        public int ThemeSet
        {
            get => (int)Core.Helper.ConfigHelper.Instance.ThemeSet;
            set => Core.Helper.ConfigHelper.Instance.ThemeSet = (ElementTheme)value;
        }

        public int BackdropSet
        {
            get => (int)Core.Helper.ConfigHelper.Instance.BackdropSet - 1;
            set => Core.Helper.ConfigHelper.Instance.BackdropSet = (BackdropType)value + 1;
        }

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

        /// <summary>
        /// 登录 / 个人信息
        /// </summary>
        [RelayCommand]
        async void AccountInfo(Button element)
        {
            // 如果未登录
            if (!HasAccount)
            {
                if (!AppShareHelper.HasDialog)
                {
                    AppShareHelper.HasDialog = true;

                    var dialog = new Controls.LoginDialog();
                    await dialog.ShowAsync();

                    HasAccount = ApiRequest.HasAccount;

                    AppShareHelper.HasDialog = false;
                }
            }
            else
            {
                var flyout = new Flyout()
                {
                    Placement = FlyoutPlacementMode.Left,
                    Content = new SimpleStackPanel()
                    {
                        Width = 250,
                        Height = 125,
                    },
                };
                flyout.ShowAt(element);
            }
            OnPropertyChanging(nameof(HasAccount));
            
        }
    }
}
