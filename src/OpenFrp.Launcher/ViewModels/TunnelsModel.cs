using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenFrp.Core.Libraries.Api;
using OpenFrp.Core.Libraries.Api.Models;
using OpenFrp.Core.Libraries.Protobuf;
using OpenFrp.Launcher.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using GridView = ModernWpf.Controls.GridView;

namespace OpenFrp.Launcher.ViewModels
{
    public partial class TunnelsModel : ObservableObject
    {
        public TunnelsModel()
        {
            if (App.Current.MainWindow.DataContext is MainPageModel model)
                model.PropertyChanging += (sender, args) =>
                {
                    OnPropertyChanging("MainPageModel");
                    if (MainPageModel.HasDeamonProcess)
                    {
                        ((Views.MainPage)App.Current.MainWindow).Of_nViewFrame.Navigate(typeof(Views.Setting));
                        ((NavigationViewItem)((Views.MainPage)App.Current.MainWindow).Of_nView.SettingsItem).IsSelected = true;
                    }
                };
        }

        public MainPageModel MainPageModel
        {
            get => (MainPageModel)App.Current.MainWindow.DataContext;
        }

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
            var request_tun2 = new RequestBase()
            {
                Action = RequestType.ClientGetRunningtunnels,
            };
            var response = await Helper.AppShareHelper.PipeClient.Request(request_tun2);

            if (response.Success)
            {
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
                            if (response.RunningCount.Contains(item.TunnelId))
                            {
                                item.IsRunning = true;
                            }
                            fix2.Add(item);
                        }).GetHashCode();
                    }
                    TunnelsPage.OfApp_XLoader.ShowContent();
                    IsCanRefreshing = IsToolsEnabled = true;
                }
                else
                {
                    TunnelsPage.OfApp_XLoader.PushMessage(() =>
                    {
                        RefreshUserProxies();
                        TunnelsPage.OfApp_XLoader.ShowLoader();
                    }, request?.Message ?? "未知错误", "重试");
                    TunnelsPage.OfApp_XLoader.ShowError();
                }
            }
            else
            {
                TunnelsPage.OfApp_XLoader.PushMessage(() =>
                {
                    RefreshUserProxies();
                    TunnelsPage.OfApp_XLoader.ShowLoader();
                }, response.Message, "重试");
                TunnelsPage.OfApp_XLoader.ShowError();
            }

        }

        [RelayCommand]
        void CopyTunnelLink(ResponseBody.UserTunnelsResponse.UserTunnel tunnel)
        {
            try
            {
                System.Windows.Clipboard.SetText(tunnel.ConnectAddress);
            }
            catch
            {

            }
        }

        [RelayCommand]
        async void DeleteTunnel(ResponseBody.UserTunnelsResponse.UserTunnel tunnel)
        {
            var dialog = new ContentDialog()
            {
                Title = "删除隧道",
                Content = new TextBlock()
                {
                    Width = 230,
                    Height = 75,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                    Text = $"真的要删除该隧道吗"
                },
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
            };
            if (await dialog.ShowDialogFixed() is ContentDialogResult.Primary)
            {
                // 删除隧道
                var result = await ApiRequest.UniversalPOST<ResponseBody.BaseResponse>(ApiUrls.RemoveTunnel, new RequestsBody.DeleteTunnelRequest(tunnel.TunnelId));
                if (result.Success)
                {
                    UserTunnels.Remove(tunnel);
                }

            }


        }

        [RelayCommand]
        async void EditTunnel(ResponseBody.UserTunnelsResponse.UserTunnel tunnel)
        {
            var config = new Controls.TunnelConfig()
            {
                Margin = new(-4,0,4,0),
                Width = 495,
                IsCreating = false,
                
            };
            var loader = new Controls.ElementLoader()
            {
                Content = new ScrollViewerEx()
                {
                    Content = config,
                    Height = 300,
                },
                Height = 300,
            };
            var dialog = new ContentDialog()
            {
                Title = "编辑隧道",
                Content = loader,
                PrimaryButtonText = "修改",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
            };
            ((ScrollViewerEx)loader.Content).PreviewMouseWheel += (s, e) => ((ScrollViewerEx)loader.Content).ExcuteScroll(e);
            dialog.PrimaryButtonClick += async (s, e) =>
            {
                dialog.IsPrimaryButtonEnabled = false;
                e.Cancel = true;

                loader.ShowLoader();
                // 执行编辑操作
                var result = await ApiRequest.UniversalPOST<ResponseBody.BaseResponse>(ApiUrls.EditTunnel, config.GetConfig(true));
                if (result.Success)
                {
                    RefreshUserProxies();
                    dialog.Hide();
                }
                else
                {
                    loader.ShowError();
                    loader.PushMessage(() => { loader.ShowContent(); dialog.IsPrimaryButtonEnabled = true; }, result.Message, "继续编辑");
                }

            };
            config.SetConfig(tunnel);
            await dialog.ShowDialogFixed();
        }

        [RelayCommand]
        public void ToCreateTunnel() => ((Views.MainPage)App.Current.MainWindow).Of_nViewFrame.Navigate(typeof(Views.CreateTunnel));
    }
}
 