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
using Windows.UI.WebUI;
using Microsoft.AppCenter.Crashes;

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
        public Awe.AppModel.AppNews previewContent = new();

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
        void OpenInWeb()
        {
            try
            {
                Process.Start("https://console.openfrp.net");
                return;
            }
            catch
            {

            }
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c start https://console.openfrp.net"
                });
            }
            catch
            {

            }
        }

        public async void RefreshUserInfoView(bool redownload = false)
        {
            try
            {


                if (MainPage is null) return;
                MainPage?.OfApp_UserInfoXLoader?.ShowLoader();
                if (ApiRequest.HasAccount && UserInfo != null && UserInfo.UserToken != null &&
                    ApiRequest.UserInfo != null)
                {
                    try
                    {
                        while (ApiRequest.UserInfo.UserName is "未登录" or null)
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
                        Content = $"{(UserInfo.InputLimit / (double)1024) * 8} / {(UserInfo.OutputLimit / (double)1024) * 8}",
                        ASRContent = $"上行 {(UserInfo.InputLimit / (double)1024) * 8},下行 {(UserInfo.OutputLimit / (double)1024) * 8}"
                    },
                }.ForEach(x =>
                {
                    MainPage?.OfApp_UserInfoXLoader?.Dispatcher?.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, () =>
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

                        MainPage?.OfApp_UserInfoXLoader.ShowContent();
                    }
                    catch (Exception ex)
                    {
                        MainPage?.OfApp_UserInfoXLoader.PushMessage(async () =>
                        {
                            await Task.Delay(500);
                            RefreshUserInfoView(redownload);
                        }, "请求出错啦!请等待修复", "重试");
                        MainPage?.OfApp_UserInfoXLoader.ShowError();
                        Crashes.TrackError(ex);
                    }
                }
                else
                {
                    try
                    {
                        if (!AppShareHelper.HasDeamonProcess)
                        {
                            MainPage?.OfApp_UserInfoXLoader.PushMessage(async () =>
                            {
                                await Task.Delay(500);
                                RefreshUserInfoView(redownload);
                            }, "未连接到守护进程。", "重试");
                            MainPage?.OfApp_UserInfoXLoader.ShowError();
                        }
                        // 这里是没有账户的情况 
                        else if (ConfigHelper.Instance.Account.HasAccount)
                        {
                            var request = await AppShareHelper.LoginAndGetUserInfo(ConfigHelper.Instance.Account.UserName, ConfigHelper.Instance.Account.Password);
                            // 登录失败
                            if (request?.Success is false)
                            {
                                MainPage?.OfApp_UserInfoXLoader.PushMessage(async () =>
                                {
                                    await Task.Delay(500);
                                    RefreshUserInfoView(redownload);
                                }, request.Message, "登录");
                                MainPage?.OfApp_UserInfoXLoader.ShowError();
                            }
                            else RefreshUserInfoView(false);
                        }
                        else
                        {
                            // 没有登录
                            MainPage?.OfApp_UserInfoXLoader.PushMessage(MainPageModel.AccountInfo, "登录即可查看更多信息。", "登录");
                            MainPage?.OfApp_UserInfoXLoader.ShowError();
                        }
                    }
                    catch (Exception ex)
                    {
                        MainPage?.OfApp_UserInfoXLoader.PushMessage(async () =>
                        {
                            await Task.Delay(500);
                            RefreshUserInfoView(redownload);
                        }, "请求出错啦!请等待修复", "重试");
                        MainPage?.OfApp_UserInfoXLoader.ShowError();
                        Crashes.TrackError(ex);
                    }
                }
            }
            catch ( Exception ex)
            {
                MainPage?.OfApp_UserInfoXLoader.PushMessage(async () =>
                {
                    await Task.Delay(500);
                    RefreshUserInfoView(redownload);
                }, "请求出错啦!请等待修复", "重试");
                MainPage?.OfApp_UserInfoXLoader.ShowError();
                Crashes.TrackError(ex);
            }

        }

        [RelayCommand]
        public async void RefreshPreview() 
        {
            if (MainPage is null) return;
            MainPage.OfApp_PreviewXLoader.ShowLoader();
            var response = await ApiRequest.GETAny<Awe.AppModel.Response.BaseResponse<Awe.AppModel.AppNews>>($"{ApiUrls.LauncherBaseUrl}api/news?query=openfrpLauncher");
            if (response != null && response.Success && response.Data != null)
            {
                if (response.Data.Content?.Data != null)
                {
                    if (true)
                    {
                        string fileName = $"{Utils.ApplicatioDataPath}\\statics\\{response.Data.Content.Data!.GetMD5()}.png";
                        Directory.CreateDirectory($"{Utils.ApplicatioDataPath}\\statics\\");
                        if (!File.Exists(fileName))
                        {
                            if (!await ApiRequest.DownloadFile($"{response.Data.Content.Data}", fileName))
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
                else if (PreviewContent.Content != null)
                {
                    MainPage.Xbg_.ImageSource = new BitmapImage(new Uri("pack://application:,,,/OpenFrp.Launcher;component/Resourecs/previewImage.jpg"));
                }
                PreviewContent = response.Data;
            }
            else
            {
                PreviewContent = new Awe.AppModel.AppNews()
                {
                    Title = " 一番荷芰生池沼，槛前风送馨香",
                    Subtitle = "打开官网",
                    Url = "https://of.gs"
                };
            }
            MainPage.OfApp_PreviewXLoader.ShowContent();

        }

        public async void RefreshBroadCast()
        {
            if (MainPage is null) return;
            MainPage.OfApp_BroadCastXLoader.ShowLoader();
            var response = await ApiRequest.UniversalGET<ResponseBody.BaseResponse>(ApiUrls.BroadCast,false);
            if (response?.Success is true)
            {
                await Task.Delay(250);
                MainPage.OfApp_BroadCastXLoader.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, () =>
                {
                    BroadCastContent = (UIElement)System.Windows.Markup.XamlReader.Parse(response.Data?.ToString() ?? "<TextBlock>空</TextBlock>");
                }).GetHashCode();
                MainPage.OfApp_BroadCastXLoader.ShowContent();
            }
            else
            {
                MainPage.OfApp_BroadCastXLoader.ShowError();
                MainPage.OfApp_BroadCastXLoader.PushMessage(RefreshBroadCast,response?.Message ?? "加载失败了捏","重新加载"); ;
                // MainPage?.OfApp_BroadCastXContent.Children.Add("")
            }
        }

        [RelayCommand]
        public async void SavePicture()
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "图片文件|*.png"
                };

                if (dialog.ShowDialog() is true)
                {
                    

                    if (PreviewContent.Content?.Data is null)
                    {
                        using var file = new FileStream(dialog.FileName, FileMode.Create);

                        Stream stream = App.GetResourceStream(new Uri("pack://application:,,,/OpenFrp.Launcher;component/Resourecs/previewImage.jpg")).Stream;

                        byte[] buffer = new byte[stream.Length];
                        int count = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if (count > 0)
                        {
                            await file.WriteAsync(buffer, 0, count);
                        }
                    }
                    else
                    {
                        File.Copy($"{Utils.ApplicatioDataPath}\\statics\\{PreviewContent.Content.Data.GetMD5()}.png",dialog.FileName, true);
                    }
                    
                }

            }
            catch { }
        }

       
    }
}
