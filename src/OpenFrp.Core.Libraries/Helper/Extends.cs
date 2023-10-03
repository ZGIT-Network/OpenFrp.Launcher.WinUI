using Newtonsoft.Json;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OpenFrp.Core.Helper
{
    /*
     *  这里都是一些扩展方法
     *  便于使用。
     *  Author: AYue
     */
    public static partial class Extends
    {
        /// <summary>
        /// 序列化
        /// </summary>
        public static string JSON(this object obj) => JsonConvert.SerializeObject(obj);

        /// <summary>
        /// 反序列化
        /// </summary>
        public static T? PraseJson<T>(this string str,bool allowThrow = false)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(str);
            }
            catch
            {
                if (allowThrow)
                {
                    throw;
                }
                // Write error to logs
                return default;
            }
        }

        /// <summary>
        /// 将 <see cref="string"/> 转为 byte[]
        /// </summary>
        public static byte[] GetBytes(this string str) => Encoding.UTF8.GetBytes(str);

        /// <summary>
        /// 将 <see cref="byte[]"/> 转为文本串
        /// </summary>
        public static string GetString(this byte[] bytes) => Encoding.UTF8.GetString(bytes);

        /// <summary>
        /// 从 URL 获取 输入流
        /// </summary>
        public static StreamReader GetStreamReader(this string str) => new StreamReader(str);
        /// <summary>
        /// 从 URL 获取 输出流
        /// </summary>
        public static StreamWriter GetStreamWriter(this string str, int bufferSize = 2048,bool autoFlush = true) => new StreamWriter(str, false, Encoding.UTF8, bufferSize) { AutoFlush = autoFlush};

        /// <summary>
        /// 将目录拼成一个完整路径
        /// </summary>
        public static string CombinePath(this string str,params string[] strs) => Path.Combine(str, Path.Combine(strs));

        
        private static HMACMD5 hamcMD5 { get; set; } = new HMACMD5()
        {
            Key = new byte[] { 19, 45, 49, 51 }
        };
        /// <summary>
        /// 获取文本的 MD5 值
        /// </summary>
        public static string GetMD5(this string str)
        {
            StringBuilder builder = new();
       
            
            foreach (var item in hamcMD5.ComputeHash(str.GetBytes()))
            {
                builder.Append(item.ToString("x2"));
            }
            return builder.ToString();
        }
        /// <summary>
        /// 将 <see cref="object"/> 转为 <see cref="StringBuilder"/>
        /// </summary>
        public static StringContent ToJSONBody(this object obj) => new(obj.JSON(), Encoding.UTF8, "application/json");

        public static async ValueTask<T?> WithCancalToken<T>(this Task<T> task,CancellationToken _token)
        {
            var result = await task;
            if (_token.IsCancellationRequested)
            {
                return default;
            }
            return result;
        }
        public static async ValueTask<T?> WithCancalToken<T>(this ValueTask<T> task, CancellationToken _token)
        {
            var result = await task;
            if (_token.IsCancellationRequested)
            {
                return default;
            }
            return result;
        }

        public static bool RunAsUAC(this ProcessStartInfo info)
        {
            try
            {
                info.Verb = "runas";
                
                Process.Start(info);
                return true;
            }
            catch { }
            return false;
        }
    }
}
