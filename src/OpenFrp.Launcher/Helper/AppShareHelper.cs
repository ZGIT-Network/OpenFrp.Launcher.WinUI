using OpenFrp.Core.Helper;
using OpenFrp.Core.Libraries.Api;
using OpenFrp.Core.Libraries.Api.Models;
using OpenFrp.Core.Libraries.Pipe;
using OpenFrp.Launcher.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenFrp.Launcher.Helper
{
    public class AppShareHelper
    {
        /// <summary>
        /// 是否有 ContentDialog 正在显示
        /// </summary>
        public static bool HasDialog { get; set; }


        /// <summary>
        /// 管道 - 客户端
        /// </summary>
        public static PipeClient PipeClient { get; set; } = new();

        /// <summary>
        /// 守护进程是否正在运行
        /// </summary>
        public static bool HasDeamonProcess
        {
            get => (App.Current.MainWindow.DataContext as ViewModels.MainPageModel)!.HasDeamonProcess;
            set => (App.Current.MainWindow.DataContext as ViewModels.MainPageModel)!.HasDeamonProcess = value;
        }

        /// <summary>
        /// 登录且获得用户个人信息 
        /// </summary>
        public static async ValueTask<ResponseBody.BaseResponse> LoginAndGetUserInfo(string? username,string? password,CancellationToken token = default)
        {
            if (HasDeamonProcess)
            {
                var loginResult = await ApiRequest.Login(username ?? "", password ?? "", token);
                if (loginResult.Success)
                {
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
                                UserInfoJson = userinfoResult.JSON()
                            },
                        };
                        var response = await PipeClient.Request(request).WithCancalToken(token);

                        if (response is null) { ApiRequest.ClearAuth(); return new("用户已取消操作。"); }
                        else if (!response.Success)
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
                return loginResult;
            }
            else
            {
                return new("未连接到守护进程。");
            }
            
        }
    }
}
