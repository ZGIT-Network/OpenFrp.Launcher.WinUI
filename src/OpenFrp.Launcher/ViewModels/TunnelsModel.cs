using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenFrp.Core.Libraries.Api;
using OpenFrp.Core.Libraries.Api.Models;
using OpenFrp.Core.Libraries.Protobuf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenFrp.Launcher.ViewModels
{
    public partial class TunnelsModel : ObservableObject
    {
        public Views.Tunnels? TunnelsPage { get; set; }

        [ObservableProperty]
        public bool isToolsEnabled;

        [ObservableProperty]
        public bool isCanRefreshing;

        [ObservableProperty]
        public ObservableCollection<ResponseBody.UserTunnelsResponse.UserTunnel> userTunnels = new();

        [RelayCommand]
        public async void RefreshUserProxies()
        {
            if (TunnelsPage is null) return;
            TunnelsPage.OfApp_XLoader.ShowLoader();
            IsCanRefreshing = IsToolsEnabled = false;
            var request = await ApiRequest.UniversalPOST<ResponseBody.UserTunnelsResponse>(ApiUrls.UserTunnels);
            if (request?.Success == true && request.Data is not null)
            {
                ObservableCollection<ResponseBody.UserTunnelsResponse.UserTunnel> fix2 = new();

                UserTunnels = fix2;
                foreach (var item in request.Data.List)
                {
                    TunnelsPage.OfApp_XLoader.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, () =>
                    {
                        fix2.Add(item);
                    }).GetHashCode();
                }
                TunnelsPage.OfApp_XLoader.ShowContent();
                IsCanRefreshing = IsToolsEnabled = true;
            }
            else
            {
                TunnelsPage.OfApp_XLoader.PushMessage(RefreshUserProxies, request?.Message ?? "未知错误", "重试");
                TunnelsPage.OfApp_XLoader.ShowError();
            }
        }

        [RelayCommand]
        public void ToCreateTunnel() => ((Views.MainPage)App.Current.MainWindow).Of_nViewFrame.Navigate(typeof(Views.CreateTunnel));
    }
}
 