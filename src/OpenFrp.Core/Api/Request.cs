using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Google.Protobuf.WellKnownTypes;
using OpenFrp.Core.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


namespace OpenFrp.Core.Libraries.Api
{
    public class ApiRequest
    {
        public static string? Authorization { get; set; }

        public static string? SessionId { get; set; }

        public static Models.ResponseBody.UserInfoResponse.UserInfo? UserInfo { get; set; }

        public static void ClearAuth()
        {
            UserInfo = (string.IsNullOrEmpty(Authorization = SessionId = default) ? null : null);
            if (System.Windows.Application.Current?.Windows is not null && !Utils.IsWindowsService)
            {
                WeakReferenceMessenger.Default.Send(new PropertyChangedMessage<bool>(new object(), "HasAccount ", HasAccount,false ));
            }
        }

        public static bool HasAccount { get => !string.IsNullOrEmpty(Authorization) && !string.IsNullOrEmpty(SessionId); }

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="username">账户</param>
        /// <param name="password">密码</param>
        /// <param name="token">(可选) 取消命牌</param>
        public static async ValueTask<Models.ResponseBody.BaseResponse> Login(string username, string password,CancellationToken token = default)
        {
            var response = await POST<Models.ResponseBody.BaseResponse>(ApiUrls.UserLogin, new Models.RequestsBody.LoginRequest(username, password).ToJSONBody());
            if (response is not null && !token.IsCancellationRequested)
            {
                SessionId = response.Data?.ToString();
                return response;
            }
            else if (token.IsCancellationRequested)
            {
                Authorization = null;
                return new("用户已取消操作。");
            }
            else
            {
                // response 是 Null，且没有取消操作。
                return new("API 返回值错误。");
            }
        }

        
        /// <summary>
        /// 统一获取
        /// </summary>
        public static async ValueTask<T> UniversalPOST<T>(string url,object? postData = default) where T : Models.ResponseBody.BaseResponse
        {
            if (HasAccount)
            {
                return (await POST<T>(url, postData?.ToJSONBody() ?? new Models.RequestsBody.BaseRequest().ToJSONBody()))!;
            }
            else return UnloginMessage<T>();
        }
        /// <summary>
        /// 同意获取
        /// </summary>
        public static async ValueTask<T> UniversalGET<T>(string url,bool needAccount = true) where T : Models.ResponseBody.BaseResponse
        {
            if (HasAccount || !needAccount)
            {
                return (await GET<T>(url))!;
            }
            else
            {
                return UnloginMessage<T>();
            }
        }

        /// <summary>
        /// 没有登录
        /// </summary>
        private static T UnloginMessage<T>() where T : Models.ResponseBody.BaseResponse
        {
            if (typeof(T) == typeof(Models.ResponseBody.BaseResponse))
            {
                return (new Models.ResponseBody.BaseResponse("用户暂未登录") as T)!;
            }
            else if (typeof(T) == typeof(Models.ResponseBody.UserInfoResponse))
            {
                return (new Models.ResponseBody.UserInfoResponse("用户暂未登录") as T)!;
            }
            else if (typeof(T) == typeof(Models.ResponseBody.UserTunnelsResponse))
            {
                return (new Models.ResponseBody.UserInfoResponse("用户暂未登录") as T)!;
            }
            else if (typeof(T) == typeof(Models.ResponseBody.UserTunnelsResponse))
            {
                return (new Models.ResponseBody.UserTunnelsResponse("用户暂未登录") as T)!;
            }
            else if (typeof(T) == typeof(Models.ResponseBody.NodeListsResponse))
            {
                return (new Models.ResponseBody.NodeListsResponse("用户暂未登录") as T)!;
            }

            else 
            {
                throw new NotImplementedException();
            }
        }


        /// <summary>
        /// POST 请求 (已限定)
        /// </summary>
        public static async ValueTask<T?> POST<T>(string url,StringContent body, CancellationToken token = default) where T : Models.ResponseBody.BaseResponse
        {
            try
            {
                var handler = new HttpClientHandler()
                {
                    UseProxy = ConfigHelper.Instance.BypassProxy
                };
                var httpClient = new HttpClient(handler);
               
                httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(Authorization) ? default : new(Authorization);
                httpClient.Timeout = new TimeSpan(0, 0, 0, 10);
                var response = await httpClient.PostAsync(url, body,token);

                if (response.IsSuccessStatusCode && !token.IsCancellationRequested)
                {
                    if (response.Headers.Contains("Authorization"))
                    {
                        Authorization = response.Headers.GetValues("Authorization").FirstOrDefault();
                    }
                    return (await response.Content.ReadAsStringAsync()).PraseJson<T>();
                }
            }
            catch (Exception ex)
            {
                Utils.Log(ex, false);
                if (typeof(T) == typeof(Api.Models.ResponseBody.BaseResponse))
                {
                    return (T)new Models.ResponseBody.BaseResponse()
                    {
                        Exception = ex,
                        Message = "发生了未知错误"
                    };
                }
            }
            return default;
        }
        /// <summary>
        /// GET 请求 (已限定)
        /// </summary>
        public static async ValueTask<T?> GET<T>(string url, CancellationToken token = default) where T : Models.ResponseBody.BaseResponse
        {
            try
            {
                var handler = new HttpClientHandler()
                {
                    UseProxy = ConfigHelper.Instance.BypassProxy
                };
                var httpClient = new HttpClient(handler);

                httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(Authorization) ? default : new(Authorization);
                httpClient.Timeout = new TimeSpan(0, 0, 0, 10);
                var response = await httpClient.GetAsync(url, token);

                if (response.IsSuccessStatusCode && !token.IsCancellationRequested)
                {
                    if (response.Headers.Contains("Authorization"))
                    {
                        Authorization = response.Headers.GetValues("Authorization").FirstOrDefault();
                    }
                    return (await response.Content.ReadAsStringAsync()).PraseJson<T>();
                }
            }
            catch (Exception ex)
            {
                Utils.Log(ex, false);
                if (typeof(T) == typeof(Api.Models.ResponseBody.BaseResponse))
                {
                    return (T)new Models.ResponseBody.BaseResponse()
                    {
                        Exception = ex,
                        Message = "发生了未知错误"
                    };
                }
            }
            return default;
        }
        /// <summary>
        /// GET 请求
        /// </summary>
        public static async ValueTask<T?> GETAny<T>(string url, CancellationToken token = default)
        {
            try
            {
                var handler = new HttpClientHandler()
                {
                    UseProxy = ConfigHelper.Instance.BypassProxy
                };
                var httpClient = new HttpClient(handler);

                httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(Authorization) ? default : new(Authorization);
                httpClient.Timeout = new TimeSpan(0, 0, 0, 10);
                var response = await httpClient.GetAsync(url, token);

                if (response.IsSuccessStatusCode && !token.IsCancellationRequested)
                {
                    if (response.Headers.Contains("Authorization"))
                    {
                        Authorization = response.Headers.GetValues("Authorization").FirstOrDefault();
                    }
                    return (await response.Content.ReadAsStringAsync()).PraseJson<T>();
                }
            }
            catch (Exception ex)
            {
                Utils.Log(ex, false);
            }
            return default;
        }
        /// <summary>
        /// 下载文件
        /// </summary>
        public static async ValueTask<bool> DownloadFile(string url,string filepath,DownloadProgressChangedEventHandler? action = default)
        {
            try
            {
                var client = new WebClient();
                if (action is not null)
                    client.DownloadProgressChanged += action;
                await client.DownloadFileTaskAsync(url, filepath);
                return true;
            }
            catch (Exception ex)
            {
                if (File.Exists(filepath))
                {
                    File.Delete(filepath);
                }
                Utils.Log(ex);
                return false;
            }

        }
    }
}
