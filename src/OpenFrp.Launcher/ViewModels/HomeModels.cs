using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenFrp.Core.Helper;
using OpenFrp.Core;
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
using System.IO;
using System.Windows.Media.Imaging;
using OpenFrp.Launcher.Helper;
using System.Diagnostics;
using OpenFrp.Launcher.Views;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace OpenFrp.Launcher.ViewModels
{
    public partial class HomeModels : ObservableObject
    {
        
        public HomeModels()
        {

        }


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
        /// <summary>
        /// 小屏模式
        /// </summary>
        [ObservableProperty]
        public bool smallDisplayMode;
        /// <summary>
        /// 用户信息 - 列表化
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<UserInfoListItem> userInfoListItems = new();
        /// <summary>
        /// 大图版内容
        /// </summary>
        [ObservableProperty]
        public Core.Libraries.Api.Models.ResponseBody.HomePageResponse previewContent = new();

        public MainPageModel MainPageModel
        {
            get => (MainPageModel)App.Current.MainWindow.DataContext;
            set => App.Current.MainWindow.DataContext = value;
        }

        public Views.Home? MainPage { get; set; }

        [ObservableProperty]
        public UIElement? broadCastContent;

        public class UserInfoListItem
        {
            public char Symbol { get; set; } = new char();

            public string Title { get; set; } = "Simple";

            public string Content { get; set; } = "内容";

            public string ASRContent { get; set; } = "讲述人专用属性";
        }

        public void UpdateProperty(string name) => OnPropertyChanged(name);

        [RelayCommand]
        public async void Signin()
        {
            var loader = new ElementLoader()
            {
                Width = 375,
                Height = 150,
                ProgressRingSize = 75,
                IsLoading = true
            };
            var dialog = new ContentDialog()
            {
                Title = "签到",
                PrimaryButtonText = "确定",
                DefaultButton = ContentDialogButton.Primary,
                Content = loader,
                IsPrimaryButtonEnabled = false,
            };
            dialog.Loaded += async (sender, args) =>
            {
                var signinResult = await ApiRequest.UniversalPOST<ResponseBody.BaseResponse>(ApiUrls.Signin);
                loader.Content = signinResult.Data is null ? signinResult.Message : signinResult.Data;
                loader.ShowContent();
                dialog.IsPrimaryButtonEnabled = true;
            };
            await dialog.ShowDialogFixed();
            RefreshUserInfoView();
        }

        [RelayCommand]
        void OpenInWeb() => Process.Start("https://www.openfrp.net");

        public async void RefreshUserInfoView(bool redownload = false)
        {
            if (MainPage is null) return;
            MainPage.OfApp_UserInfoXLoader.ShowLoader();
            if (ApiRequest.HasAccount)
            {
                while (ApiRequest.UserInfo?.UserName is "未登录" or null)
                {
                    await Task.Delay(250);
                }
                UserInfoListItems.Clear();
                new List<UserInfoListItem>()
                {
                    new()
                    {
                        Symbol = '\xe715',
                        Title = "邮箱",
                        Content = UserInfo.Email,
                    },
                    new()
                    {
                        Symbol = '\xe77b',
                        Title = "昵称",
                        Content = UserInfo.UserName,
                    },
                    new()
                    {
                        Symbol = '\xe8a4',
                        Title = "隧道数",
                        Content = $"{UserInfo.UsedProxies} / {UserInfo.MaxProxies}",
                        ASRContent = $"已使用 {UserInfo.UsedProxies} 条,共可用 {UserInfo.MaxProxies} 条"
                    },
                    new()
                    {
                        Symbol = '\xeafc',
                        Title = "可用流量",
                        Content = $"{Math.Round(UserInfo.Traffic / 1024d,2)} Gib",
                    },
                    new()
                    {
                        Symbol = '\xe780',
                        Title = "实名状态",
                        Content = $"{(UserInfo.isRealname ? "已": "未")}实名",
                    },
                    new()
                    {
                        Symbol = '\xe902',
                        Title = "所在组",
                        Content = UserInfo.GroupCName,
                    },
                    new()
                    {
                        Symbol = '\xec05',
                        Title = "带宽速率 (Mbps)",
                        Content = $"{(UserInfo.InputLimit / 1024) * 8} / {(UserInfo.OutputLimit / 1024) * 8}",
                        ASRContent = $"上行 {(UserInfo.InputLimit / 1024) * 8},下行 {(UserInfo.OutputLimit / 1024) * 8}"
                    },
                }.ForEach(x=>
                {
                    MainPage.OfApp_UserInfoXLoader.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, () =>
                    {
                        UserInfoListItems.Add(x);
                    });
                });
                if (!redownload) await Task.Delay(500);
                else
                {
                    var request = await ApiRequest.UniversalPOST<ResponseBody.UserInfoResponse>(ApiUrls.UserInfo);
                    if (!string.IsNullOrEmpty(request.Message))
                    {
                        await AppShareHelper.LoginAndGetUserInfo(ConfigHelper.Instance.Account.UserName, ConfigHelper.Instance.Account.Password);
                    }
                    else if (request.Success) ApiRequest.UserInfo = request.Data;


                }

                MainPage.OfApp_UserInfoXLoader.ShowContent();
            }
            else
            {
                if (!AppShareHelper.HasDeamonProcess)
                {
                    MainPage.OfApp_UserInfoXLoader.PushMessage(async () =>
                    {
                        await Task.Delay(500);
                        RefreshUserInfoView(redownload);
                    }, "未连接到守护进程。", "重试");
                    MainPage.OfApp_UserInfoXLoader.ShowError();
                }
                // 这里是没有账户的情况 
                else if (ConfigHelper.Instance.Account.HasAccount)
                {
                    var request = await AppShareHelper.LoginAndGetUserInfo(ConfigHelper.Instance.Account.UserName, ConfigHelper.Instance.Account.Password);
                    // 登录失败
                    if (!request.Success)
                    {
                        MainPage.OfApp_UserInfoXLoader.PushMessage(async () =>
                        {
                            await Task.Delay(500);
                            RefreshUserInfoView(redownload);
                        }, request.Message, "登录");
                        MainPage.OfApp_UserInfoXLoader.ShowError();
                    }
                    else RefreshUserInfoView(false);
                }
                else
                {
                    // 没有登录
                    MainPage.OfApp_UserInfoXLoader.PushMessage(MainPageModel.AccountInfo, "登录即可查看更多信息。", "登录");
                    MainPage.OfApp_UserInfoXLoader.ShowError();
                }
            }

        }

        [RelayCommand]
        public async void RefreshPreview() 
        {
            if (MainPage is null) return;
            MainPage.OfApp_PreviewXLoader.ShowLoader();
            var response = await ApiRequest.GETAny<ResponseBody.HomePageResponse>($"{ApiUrls.LauncherBaseUrl}api/v2/news");
            if (response != null)
            {
                PreviewContent = response;
                if (response.Image != null)
                {
                    if (response.ForceImage || !false)
                    {
                        string fileName = $"{Utils.ApplicatioDataPath}\\statics\\{response.Image.GetMD5()}.png";
                        Directory.CreateDirectory($"{Utils.ApplicatioDataPath}\\statics\\");
                        if (!File.Exists(fileName))
                        {
                            if (!await ApiRequest.DownloadFile($"{response.Image}", fileName))
                            {
                                MainPage.OfApp_PreviewXLoader.ShowContent();
                                return;
                            }
                            MainPage.Xbg_.ImageSource = new BitmapImage(new Uri(fileName));
                        }
                        else MainPage.Xbg_.ImageSource = new BitmapImage(new Uri(fileName));


                        await Task.Delay(500);
                    }
                }
                
            }
            MainPage.OfApp_PreviewXLoader.ShowContent();
        }

        public async void RefreshBroadCast()
        {
            if (MainPage is null) return;
            MainPage.OfApp_BroadCastXLoader.ShowLoader();
            var response = await ApiRequest.UniversalGET<ResponseBody.BaseResponse>(ApiUrls.BroadCast,false);
            if (response.Success)
            {
                await Task.Delay(250);
                MainPage.OfApp_BroadCastXLoader.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, () =>
                {
                    BroadCastContent = (UIElement)System.Windows.Markup.XamlReader.Parse(response.Data?.ToString().Replace("## 旧版启动器已不受支持,这是当然的", ""));
                }).GetHashCode();
                MainPage.OfApp_BroadCastXLoader.ShowContent();
            }
            else
            {
                // MainPage?.OfApp_BroadCastXContent.Children.Add("")
            }
        }
    }
}
