using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenFrp.Core.Libraries.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OpenFrp.Launcher.ViewModels
{
    public partial class MainPageModel : ObservableObject
    {
        [ObservableProperty]
        public bool hasDeamonProcess;

        public Core.Libraries.Api.Models.ResponseBody.UserInfoResponse.UserInfo UserInfo
        {
            get => ApiRequest.UserInfo ?? new()
            {
                UserName = "未登录",
                Email = "点击开始登陆"
            };
        }

        public void UpdateProperty(string name) => OnPropertyChanged(name);

        [RelayCommand]
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
    }
}
