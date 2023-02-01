using OpenFrp.Core.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OpenFrp.Core.Libraries.Api
{
    public class ApiRequest
    {
        public static string? Authorization { get; internal set; }

        public static string? SessionId { get; internal set; }

        public static Models.ResponseBody.UserInfoResponse.UserInfo? UserInfo { get; set; }

        public static void ClearAuth() => UserInfo = (string.IsNullOrEmpty(Authorization = SessionId = default) ? null : null);

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
        /// 请求用户信息
        /// </summary>
        public static async ValueTask<Models.ResponseBody.UserInfoResponse> GetUserInfo()
        {
            if (HasAccount)
            {
                return (await POST<Models.ResponseBody.UserInfoResponse>(ApiUrls.UserInfo, new Models.RequestsBody.BaseRequest().ToJSONBody()))!;
            }
            else
            {
                return new("用户暂未登录");
            }
        }

        /// <summary>
        /// POST 请求
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
    }
}
