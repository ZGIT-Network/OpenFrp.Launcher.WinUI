using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenFrp.Core.Libraries.Api.Models
{
    public class RequestsBody
    {

        /// <summary>
        /// 基本请求 - Body (SessionId)
        /// </summary>
        public class BaseRequest
        {
            [JsonProperty("session")]
            public string? Session { get; set; } = ApiRequest.SessionId;
        }
        /// <summary>
        /// 登录请求 - Body
        /// </summary>
        public class LoginRequest
        {
            public LoginRequest(string? userName, string? password)
            {
                UserName = userName;
                Password = password;
            }

            [JsonProperty("user")]
            public string? UserName { get; set; }
            [JsonProperty("password")]
            public string? Password { get; set; }
        }
    }
    public class ResponseBody
    {
        /// <summary>
        /// 基本返回
        /// </summary>
        public class BaseResponse
        {
            public BaseResponse(string message) => Message = message;

            public BaseResponse() { }

            public BaseResponse(Exception ex) => Exception = ex;

            /// <summary>
            /// 数据内容
            /// </summary>
            [JsonProperty("data")]
            public object? Data { get; set; }
            /// <summary>
            /// 是否成功
            /// </summary>
            [JsonProperty("flag")]
            public bool Success { get; set; }
            /// <summary>
            /// 信息
            /// </summary>
            [JsonProperty("msg")]
            public string Message { get; set; } = string.Empty;

            
            /// <summary>
            /// 扩展条件 - 错误信息 (无为Null)
            /// </summary>
            [JsonIgnore]
            public Exception? Exception { get; set; }
        }

        /// <summary>
        /// 用户信息返回
        /// </summary>
        public class UserInfoResponse : BaseResponse
        {
            public UserInfoResponse(string message) : base(message)
            {
            }

            public UserInfoResponse()
            {
            }

            public UserInfoResponse(Exception ex) : base(ex)
            {
            }


            [JsonProperty("data")]
            public new UserInfo? Data { get; set; }

            public class UserInfo
            {
                /// <summary>
                /// 用户邮箱
                /// </summary>
                [JsonProperty("email")]
                public string Email { get; set; } = "TestMail@OpenFrp.cn";

                /// <summary>
                /// 用户名
                /// </summary>
                [JsonProperty("username")]
                public string UserName { get; set; } = "OpenFrp.App";

                /// <summary>
                /// 用户所在组名称
                /// </summary>
                [JsonProperty("friendlyGroup")]
                public string GroupCName { get; set; } = "普通用户";
                /// <summary>
                /// 用户所在组
                /// </summary>
                [JsonProperty("group")]
                public string Group { get; set; } = "normal";

                /// <summary>
                /// 用户 ID
                /// </summary>
                [JsonProperty("id")]
                public int UserID { get; set; }

                /// <summary>
                /// 最大隧道数量
                /// </summary>
                [JsonProperty("proxies")]
                public int MaxProxies { get; set; }
                /// <summary>
                /// 已使用的隧道数
                /// </summary>
                [JsonProperty("used")]
                public int UsedProxies { get; set; }

                /// <summary>
                /// 可用流量
                /// </summary>
                [JsonProperty("traffic")]
                public long Traffic { get; set; }

                /// <summary>
                /// 用户 Token
                /// </summary>
                [JsonProperty("token")]
                public string? UserToken { get; set; }

                /// <summary>
                /// 是否已实名
                /// </summary>
                [JsonProperty("realname")]
                public bool isRealname { get; set; }
                /// <summary>
                /// 入口带宽速率
                /// </summary>
                [JsonProperty("inLimit")]
                public int InputLimit { get; set; }
                /// <summary>
                /// 出口带宽速率
                /// </summary>
                [JsonProperty("outLimit")]
                public int OutputLimit { get; set; }
            }
        }
    }
}
