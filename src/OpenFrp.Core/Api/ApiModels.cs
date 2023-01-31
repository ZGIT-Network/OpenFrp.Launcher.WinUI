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
            public string? Session { get; set; }
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
        /// 基本返回 - Body
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
    }
}
