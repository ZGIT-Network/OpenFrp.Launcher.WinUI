using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public static class Extends
    {
        /// <summary>
        /// 序列化
        /// </summary>
        public static string JSON(this object obj) => JsonConvert.SerializeObject(obj);

        /// <summary>
        /// 反序列化
        /// </summary>
        public static T? PraseJson<T>(this string str)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(str);
            }
            catch(Exception ex)
            {
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
        /// <summary>
        /// 获取文本的 MD5 值
        /// </summary>
        public static string GetMD5(this string str)
        {
            StringBuilder builder = new();
            foreach (var item in MD5.Create().ComputeHash(str.GetBytes()))
            {
                builder.Append(item.ToString("x2"));
            }
            return builder.ToString();
        }
    }
}
