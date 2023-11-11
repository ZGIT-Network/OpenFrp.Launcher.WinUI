using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Google.Protobuf.WellKnownTypes;
using OpenFrp.Core.Helper;
using OpenFrp.Core.Libraries.Api.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.Devices.PointOfService;

namespace OpenFrp.Core.Libraries.Api
{
    public class ApiRequest
    {
        public static string? Authorization { get; set; }

        public static string? SessionId { get; set; }

        private
        static HttpClient httpClient = new HttpClient(new HttpClientHandler()
        {
            UseProxy = !ConfigHelper.Instance.BypassProxy,
            AllowAutoRedirect = true,
        })
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

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
        public static async ValueTask<Models.ResponseBody.OAuthResponse> Login(string username, string password,CancellationToken token = default)
        {
            var response = await POSTAny<Models.ResponseBody.OAuthResponse>(ApiUrls.UserLogin, new Models.RequestsBody.LoginRequest(username, password).ToJSONBody());

            if (response is null)
            {
                return new ResponseBody.OAuthResponse
                {
                    Code = HttpStatusCode.BadRequest,
                    Message = "返回值错误 0x9a"
                };
            }

            return response;
        }

        
        /// <summary>
        /// 统一获取
        /// </summary>
        public static async ValueTask<T> UniversalPOST<T>(string url,object? postData = default) where T : Models.ResponseBody.BaseResponse
        {
            if (HasAccount)
            {
                var response = await POST<T>(url, postData?.ToJSONBody() ?? new Models.RequestsBody.BaseRequest().ToJSONBody());
                if (response?.Message.Contains("TOKEN") is true)
                {
                    var loginr = await Login(ConfigHelper.Instance.Account.UserName!, ConfigHelper.Instance.Account.Password!);
                    if (loginr.Code is HttpStatusCode.OK) return (await POST<T>(url, postData?.ToJSONBody() ?? new Models.RequestsBody.BaseRequest().ToJSONBody()))!;
                    else
                    {
                        return (T)new Models.ResponseBody.BaseResponse()
                        {
                            Success = false,
                            Message = "Token已过期,请重新打开启动器刷新。"
                        };
                    }
                }
                else return response!;
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
        public static async ValueTask<T?> POST<T>(string url,StringContent body, CancellationToken token = default) where T : ResponseBody.BaseResponse
        {
            try
            {
               
               
                httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(Authorization) ? default : new(Authorization);
                //httpClient.Timeout = new TimeSpan(0, 0, 0, 10);
                var response = await httpClient.PostAsync(url, body,token);

                if (response.IsSuccessStatusCode && !token.IsCancellationRequested)
                {
                    if (response.Headers.Contains("Authorization"))
                    {
                        Authorization = response.Headers.GetValues("Authorization").FirstOrDefault();
                    }
                    try
                    {
                        return (await response.Content.ReadAsStringAsync()).PraseJson<T>();
                    }
                    catch (Exception ex)
                    {
                        if (typeof(T) == typeof(Api.Models.ResponseBody.BaseResponse))
                        {
                            return (T)new Models.ResponseBody.BaseResponse()
                            {
                                Exception = new Exception(await response.Content.ReadAsStringAsync(), ex),
                                Message = "发生了未知错误"
                            };
                        }
                    }
                }
                else
                {
                    if (typeof(T) == typeof(Api.Models.ResponseBody.BaseResponse))
                    {
                        return (T)new Models.ResponseBody.BaseResponse()
                        {
                            Exception = new Exception(response.RequestMessage.ToString()),
                            Message = "发生了未知错误"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                
                LogHelper.Add(0,ex.ToString(),System.Diagnostics.TraceLevel.Warning,true);
                if (typeof(T) == typeof(Api.Models.ResponseBody.BaseResponse))
                {
                    return (T)new Models.ResponseBody.BaseResponse()
                    {
                        Exception = ex,
                        Message = "发生了未知错误"
                    };
                }
                else if (typeof(T) == typeof(Api.Models.ResponseBody.UserTunnelsResponse)){
                    return new Api.Models.ResponseBody.UserTunnelsResponse()
                    {
                        Exception = ex,
                        Message = "发生了未知错误"

                    } as T;
                }
            }
            return default;
        }
        /// <summary>
        /// POST 请求 (已限定)
        /// </summary>
        public static async ValueTask<T?> POSTAny<T>(string url, StringContent body, CancellationToken token = default)
        {
            try
            {


                httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(Authorization) ? default : new(Authorization);
                //httpClient.Timeout = new TimeSpan(0, 0, 0, 10);
                var response = await httpClient.PostAsync(url, body, token);

                if (response.IsSuccessStatusCode && !token.IsCancellationRequested)
                {
                    if (response.Headers.Contains("Authorization"))
                    {
                        Authorization = response.Headers.GetValues("Authorization").FirstOrDefault();
                    }
                    try
                    {
                        return (await response.Content.ReadAsStringAsync()).PraseJson<T>();
                    }
                    catch (Exception ex)
                    {
                        return default;
                    }
                }
                else
                {
                    if (response.Content.Headers.ContentType.MediaType.Equals("application/json"))
                    {
                        return (await response.Content.ReadAsStringAsync()).PraseJson<T>();
                    }

                    if (typeof(T) == typeof(Api.Models.ResponseBody.BaseResponse))
                    {
                        return default;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Add(0, ex.ToString(), System.Diagnostics.TraceLevel.Warning, true);
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

                
                

                httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(Authorization) ? default : new(Authorization);
                //httpClient.Timeout = new TimeSpan(0, 0, 0, 10);
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
                LogHelper.Add(0, ex.ToString(), System.Diagnostics.TraceLevel.Warning, true);
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
        public static async ValueTask GET(string url, CancellationToken token = default)
        {
            try
            {




                httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(Authorization) ? default : new(Authorization);
                //httpClient.Timeout = new TimeSpan(0, 0, 0, 10);
                var response = await httpClient.GetAsync(url, token);


                if (response.IsSuccessStatusCode && !token.IsCancellationRequested)
                {
                    if (response.Headers.Contains("Authorization"))
                    {
                        Authorization = response.Headers.GetValues("Authorization").FirstOrDefault();
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Add(0, ex.ToString(), System.Diagnostics.TraceLevel.Warning, true);
            }
            return;
        }

        /// <summary>
        /// GET 请求
        /// </summary>
        public static async ValueTask<T?> GETAny<T>(string url, CancellationToken token = default)
        {
            try
            {
               

                httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(Authorization) ? default : new(Authorization);
                //httpClient.Timeout = new TimeSpan(0, 0, 0, 10);
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
                LogHelper.Add(0, ex.ToString(), System.Diagnostics.TraceLevel.Warning, true);
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
                LogHelper.Add(0, ex.ToString(), System.Diagnostics.TraceLevel.Warning, true);
                return false;
            }

        }
    }
}
