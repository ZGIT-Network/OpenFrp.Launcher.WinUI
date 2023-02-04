using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenFrp.Core;
using OpenFrp.Core.Helper;
using OpenFrp.Core.Libraries.Api;
using OpenFrp.Core.Libraries.Api.Models;
using OpenFrp.Launcher.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.ApplicationModel.Calls.Background;

namespace OpenFrp.Launcher.ViewModels
{
    public partial class CreateTunnelModel : ObservableObject
    {
        public Views.CreateTunnel? CreateTunnelPage { get; set; }

        /// <summary>
        /// 用户信息
        /// </summary>
        public ResponseBody.UserInfoResponse.UserInfo UserInfo
        {
            get => ApiRequest.UserInfo ?? new()
            {
                UserName = "未登录",
                Email = "点击开始登陆"
            };
        }

        [ObservableProperty]
        public bool isSmallMode;

        [ObservableProperty]
        public ObservableCollection<ResponseBody.NodeListsResponse.NodeInfo>? nodeLists;

        [RelayCommand]
        void ToTunnelsPage() => ((Views.MainPage)App.Current.MainWindow).Of_nViewFrame.GoBack();

        [RelayCommand]
        public async void RefreshNodeList()
        {
            if (CreateTunnelPage is null) return;

            CreateTunnelPage.OfApp_List1XLoader.ShowLoader();

            var response = await ApiRequest.UniversalGET<ResponseBody.NodeListsResponse>(ApiUrls.NodeList);
            if (response?.Success == true && response.Data is not null)
            {
                int[] counts = new int[]
                {
                    // classisy 1
                    0,
                    // classisy 2
                    0,
                    // classsisy 3
                    0,
                };
                List<ResponseBody.NodeListsResponse.NodeInfo> resp1 = new()
                {
                    new()
                    {
                        NodeName = "国内节点",
                        Description = "适合游戏联机 / 建站(需要备案)",
                        IsHeader= true,
                        HostName = "not header padding"
                    },
                    new()
                    {
                        NodeName = "中国台湾 / 中国香港节点",
                        Description = "适合建站",
                        IsHeader= true
                    },
                    new()
                    {
                        NodeName = "国外节点",
                        Description = "不一定能用，但必须得在",
                        IsHeader= true,
                        
                    }
                };
                response.Data.List.ForEach(x =>
                {
                    int count = x.NodeClassify switch
                    {
                        ResponseBody.NodeListsResponse.NodeClassify.ChinaMainland => 0,
                        ResponseBody.NodeListsResponse.NodeClassify.ChinaTW_HK => 1,
                        _ => 2,
                    };
                    // 在数组中的位置
                    counts[count] += 1;

                    // x ppp x ppp x ppp
                    resp1.Insert(counts[count] + (x.NodeClassify switch
                    {
                        ResponseBody.NodeListsResponse.NodeClassify.ChinaMainland => 0,
                        ResponseBody.NodeListsResponse.NodeClassify.ChinaTW_HK => counts[0] + count,
                        _ => counts[1] + counts[0] + count,
                    }), x);


                });
                NodeLists = new();
                resp1.ForEach(x =>
                {
                    CreateTunnelPage.OfApp_List1XLoader.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, () =>
                    {
                        NodeLists.Add(x);
                    });
                });

                await Task.Delay(250);
                
                CreateTunnelPage.OfApp_List1XLoader.ShowContent();
            }
        }

        [RelayCommand]
        public async void SelectPortOfProcess()
        {
            if (CreateTunnelPage is null)
            {
                CreateTunnelPage = ((Views.MainPage)App.Current.MainWindow).Of_nViewFrame.Content as Views.CreateTunnel;
            }
            ObservableCollection<Win32Helper.ProcessNetworkInfo> networkLists = new();
            var loader = new ElementLoader()
            {
                IsLoading = true,
                Content = new ListView()
                {
                    Width = 450,
                    MaxHeight = 300,
                    ItemsSource = networkLists,
                    ItemTemplate = (DataTemplate)App.Current.Resources["ProcessListTemplate"],
                }
            };
            var dialog = new ContentDialog()
            {
                Title = "列表 (双击选择)",
                Content = loader,
                CloseButtonText = "关闭",
            };
            ((ListView)loader.Content).MouseDoubleClick += (sender, args) =>
            {
                if (((ListView)loader.Content).SelectedIndex is not -1)
                {
                    var item = networkLists[((ListView)loader.Content).SelectedIndex];
                    CreateTunnelPage?.Configer.SetLocalPort(item.Port);
                    dialog.Hide();
                }
            };

            dialog.Loaded += async (sender, args) =>
            {
                var views = (await Win32Helper.GetAliveNetworkLink()).ToList();
                await Task.Delay(250);
                loader.IsLoading = false;
                views.ForEach(x => App.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,() =>
                {
                    networkLists.Add(x);
                }));
            };
            await dialog.ShowDialogFixed();
        }

        [RelayCommand]
        async void CreateTunnel()
        {
            if (CreateTunnelPage is null) return;
            var loader = new ElementLoader() { IsLoading = true };
            var dialog = new ContentDialog()
            {
                Title = "创建隧道",
                Content = loader,
            };
            dialog.Loaded += async (sender, args) =>
            {
                
                var response = await ApiRequest.UniversalPOST<ResponseBody.BaseResponse>(ApiUrls.CreateTunnel, CreateTunnelPage.Configer.GetConfig(false));

                if (response.Success)
                {
                    dialog.Hide();
                    ToTunnelsPage();
                }
                else
                {
                    dialog.CloseButtonText = "关闭";
                    loader.PushMessage(() =>
                    {
                        MessageBox.Show(response.Exception?.ToString());
                        dialog.Hide();
                    }, response.Message, "显示错误");
                    loader.ShowError();

                }
            };
            await dialog.ShowDialogFixed();
            
        }
    }
}
