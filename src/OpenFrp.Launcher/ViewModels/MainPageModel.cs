using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using OpenFrp.Core.Helper;
using OpenFrp.Core.Libraries.Api;
using OpenFrp.Core.Libraries.Protobuf;
using OpenFrp.Launcher.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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
    }
}
