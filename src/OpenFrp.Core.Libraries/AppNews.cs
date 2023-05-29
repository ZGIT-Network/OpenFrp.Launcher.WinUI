using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Awe.AppModel
{
    /// <summary>
    /// 软件新闻 - Model
    /// </summary>
    public class AppNews
    {
        /// <summary>
        /// 标题
        /// </summary>
        [JsonProperty("title")]
        public string? Title { get; set; } = "Unknown Title";

        /// <summary>
        /// 副标题
        /// </summary>
        [JsonProperty("subtitle")]
        public string? Subtitle { get; set; }

        /// <summary>
        /// 内容
        /// </summary>
        [JsonProperty("content")]
        public AppNewsContent? Content { get; set; }

        /// <summary>
        /// 链接
        /// </summary>
        [JsonProperty("url")]
        public string? Url { get; set; }

        /// <summary>
        /// 软件新闻 - Content
        /// </summary>
        public class AppNewsContent
        {
            /// <summary>
            /// 内容类别
            /// </summary>
            public enum AppNewsContentType
            {
                /// <summary>
                /// 字符串
                /// </summary>
                String,
                /// <summary>
                /// XAML 文件 (URL重定向)
                /// </summary>
                XAMLRefUrl,
                /// <summary>
                /// XAML (直接解析文本)
                /// </summary>
                XAML,
            }

            /// <summary>
            /// 内容类型
            /// </summary>
            [JsonProperty("type")]
            public AppNewsContentType Type { get; set; }

            /// <summary>
            /// 内容
            /// </summary>
            [JsonProperty("data")]
            public string? Data { get; set; }
        }
    }

    public sealed class Response
    {
        /// <summary>
        /// 基本返回类
        /// </summary>
        /// <typeparam name="T">Data值的类型</typeparam>
        public class BaseResponse<T>
        {
            /// <summary>
            /// 初始化值 (不进行更改)
            /// </summary>
            public BaseResponse()
            {

            }

            /// <summary>
            /// 初始化值 (直接填入 Message)
            /// </summary>
            public BaseResponse(string message)
            {
                Message = message;
            }

            /// <summary>
            /// 返回值(可能为 Null)
            /// </summary>
            [JsonProperty("data")]

            public T? Data { get; set; }

            /// <summary>
            /// 是否请求成功
            /// </summary>
            [JsonProperty("success")]
            public bool Success { get; set; }
            /// <summary>
            /// 信息
            /// </summary>
            [JsonProperty("msg")]
            public string? Message { get; set; }
        }
    }


}
