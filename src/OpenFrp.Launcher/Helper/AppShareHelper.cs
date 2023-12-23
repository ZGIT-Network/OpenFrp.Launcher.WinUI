using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.AppCenter.Crashes;
using OpenFrp.Core.Helper;
using OpenFrp.Core.Libraries.Api;
using OpenFrp.Core.Libraries.Api.Models;
using OpenFrp.Core.Libraries.Pipe;
using OpenFrp.Launcher.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenFrp.Launcher.Helper
{
    public class AppShareHelper
    {
        /// <summary>
        /// 管道 - 客户端
        /// </summary>
        public static PipeClient PipeClient { get; set; } = new();
        private static bool hasDeamonProcess;
        /// <summary>
        /// 守护进程是否正在运行
        /// </summary>
        public static bool HasDeamonProcess
        {
            get => hasDeamonProcess;
            set
            {
                WeakReferenceMessenger.Default.Send(new PropertyChangedMessage<bool>(new object(), nameof(HasDeamonProcess), hasDeamonProcess, value));
                hasDeamonProcess = value;
            }
        }

        /// <summary>
        /// 登录且获得用户个人信息 
        /// </summary>
        public static async ValueTask<ResponseBody.BaseResponse?> LoginAndGetUserInfo(string? username,string? password,CancellationToken token = default)
        {
            try
            {
                if (PipeClient?.IsConnected is true)
                {
                    var loginResult = await ApiRequest.Login(username ?? "", password ?? "", token);
                    if (loginResult.Code is System.Net.HttpStatusCode.OK)
                    {
                        var ott = await ApiRequest.GETAny<ResponseBody.BaseResponse>(ApiUrls.GetAuthorize);

                        if (ott is not null && ott.Success && ott.Data is string host)
                        {

                            var oauthCode = await ApiRequest.GETAny<ResponseBody.OAuthResponse<ResponseBody.AuthorizeData>>(
                                host.Replace("/oauth2/authorize", "/api/oauth2/authorize")
                                .Replace("https://of-dev-api.bfsea.xyz/oauth_callback", "https%3A%2F%2Fof-dev-api.bfsea.xyz%2Foauth_callback"));

                            if (oauthCode is not null && oauthCode.Code is System.Net.HttpStatusCode.OK)
                            {
                                var vava = await ApiRequest.GET<ResponseBody.BaseResponse>($"{ApiUrls.OpenFrpCodeImpt}{oauthCode.Data?.Code}");

                                if (vava is not null && vava.Success && vava.Data is not null)
                                {
                                    ApiRequest.SessionId = vava.Data.ToString();
                                }
                                else
                                {
                                    return new ResponseBody.BaseResponse
                                    {
                                        Message = $"[{vava?.Success}] {vava?.Message}",
                                        Exception = vava?.Exception ?? null
                                    };
                                }
                            }
                            else
                            {
                                return new ResponseBody.BaseResponse
                                {
                                    Message = $"[{oauthCode?.Code}] {oauthCode?.Message}",
                                };
                            }
                        }
                        else {
                            return ott ?? new ResponseBody.BaseResponse
                            {
                                Message = "Authorize Url 请求失败"
                            };
                        }

                        var userinfoResult = await ApiRequest.UniversalPOST<ResponseBody.UserInfoResponse>(ApiUrls.UserInfo).WithCancalToken(token);

                        if (userinfoResult is null) { ApiRequest.ClearAuth(); return new("用户已取消操作。"); }
                        else if (!userinfoResult.Success) { ApiRequest.ClearAuth(); return userinfoResult; }
                        else
                        {
                            var request = new Core.Libraries.Protobuf.RequestBase()
                            {
                                Action = Core.Libraries.Protobuf.RequestType.ClientPushLoginstate,
                                LoginRequest = new()
                                {
                                    Authorization = ApiRequest.Authorization,
                                    SessionId = ApiRequest.SessionId,
                                    UserInfoJson = userinfoResult.JSON(),
                                    AccountJson = new ConfigHelper.UserAccount(username, password).JSON()
                                },
                            };
                            var response = await PipeClient.Request(request).WithCancalToken(token);

                            if (response is null && token.IsCancellationRequested) { ApiRequest.ClearAuth(); return new("用户已取消操作。"); }
                            else if (response?.Success is false)
                            {
                                ApiRequest.ClearAuth();
                                return new()
                                {
                                    Exception = new Exception(response.Exception),
                                    Message = response.Message
                                };
                            }
                            else
                            {

                                ApiRequest.UserInfo = userinfoResult.Data;
                                ((ViewModels.MainPageModel)App.Current.MainWindow.DataContext).UpdateProperty("UserInfo");
                                if ((((Views.MainPage)App.Current.MainWindow)).Of_nViewFrame.Content is Setting setting && setting.DataContext is ViewModels.SettingModel settingModel)
                                {
                                    settingModel.HasAccount = true;
                                    settingModel.UpdateProperty("UserInfo");
                                }

                                ConfigHelper.Instance.Account = new(username, password);
                                return new()
                                {
                                    Success = true,
                                };
                            }
                        }
                    }
                    return new ResponseBody.BaseResponse
                    {
                        Message = $"[{loginResult.Code}]{loginResult.Message}"
                    };
                }
                else
                {
                    return new("未连接到守护进程。");
                }
            }
            catch( Exception ex)
            {
                Crashes.TrackError(ex);
                return new("请求已崩溃");
            }
            
        }

        public static H.NotifyIcon.TaskbarIcon? TaskbarIcon { get; set; } 
    }
}
