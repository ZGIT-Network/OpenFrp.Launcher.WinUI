using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OpenFrp.Core.Helper
{
    public class DESCrypto
    {
        public static byte[] KeyIV
        {
            get
            {
                var strs = $"{Utils.PipesName}_{Utils.ConfigFile}".GetMD5();
                if (!string.IsNullOrEmpty(strs) && strs.Length > 0)
                {
                    return strs.Remove(8, strs.Length - 8).GetBytes();
                }
                else
                {
                    return "74A956QA".GetBytes();
                }
        
            }
        }

        public static string EncryptString(string str)
        {
            DESCryptoServiceProvider des = new();
            des.Key = des.IV = KeyIV;
            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, des.CreateEncryptor(),CryptoStreamMode.Write))
                {
                    var bytes = str.GetBytes();
                    cs.Write(bytes, 0, bytes.Length);
                    cs.FlushFinalBlock();
                }
                var sb = new StringBuilder();
                foreach (var bi in ms.ToArray())
                {
                    sb.Append(bi.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        public static string Descrypt(string str)
        {
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();

            byte[] bytes = new byte[str.Length / 2];

            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)Convert.ToInt32(str.Substring(i * 2, 2), 16);
            }
            des.Key = des.IV = KeyIV;

            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(bytes, 0, bytes.Length);
                    cs.FlushFinalBlock();
                }

                return ms.ToArray().GetString();
            }

        }

    }
}
