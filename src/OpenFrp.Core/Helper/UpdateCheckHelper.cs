using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using OpenFrp.Core.Libraries.Api;
using OpenFrp.Core.Libraries.Api.Models;
using Windows.UI.Xaml;

namespace OpenFrp.Core.Helper
{
    public class UpdateCheckHelper
    {
        internal static WebClient _Client { get; set; } = new();

        public static async ValueTask<(Exception?,bool)> DownloadWithProgress(string url, string file, DownloadProgressChangedEventHandler onChanged)
        {
            try
            {
                if (ConfigHelper.Instance.BypassProxy)
                {
                    _Client.Proxy = null;
                }
                
                _Client.DownloadProgressChanged += onChanged;
                await _Client.DownloadFileTaskAsync(url, file);
                return (null,true);
            }
            catch (Exception ex)
            {
                return (ex,false);
            }
            
        }

        /// <summary>
        /// 检查更新
        /// </summary>
        public static async ValueTask<UpdateInfo> CheckUpdate()
        {
            var resp = await ApiRequest.GET<ResponseBody.SoftwareResponse>(ApiUrls.SoftwareSupport);

            if (resp?.Success is true && resp.Data is not null)
            {
                // 启动器更新有优先级
                if (resp.Data.Launcher is not null && Utils.LauncherVersion != resp.Data.Launcher.Latest)
                {
                    // 启动器有了更新。
                    return new()
                    {
                        Level = UpdateLevel.LauncherUpdate,
                        Content = resp.Data.Launcher.Description ?? "启动器有更新啦",
                        Version = resp.Data.Launcher.Latest,
                        DownloadUrl = resp.Data.Launcher?.DonwloadUrl
                    };
                }
                else if (ConfigHelper.Instance.FrpcVersion != resp.Data.Latest ||
                    !System.IO.File.Exists(Utils.Frpc))
                {
                    // 当配置文件中 FRPC 版本与现版不同,进行更新。
                    return new()
                    {
                        Level = UpdateLevel.FrpcUpdate,
                        Content = resp.Data.FrpcCommonDetails,
                        Version = resp.Data.Latest,
                        DownloadUrl = $"{resp.Data.Launcher?.BaseUrl}client/{resp.Data.Latest}/{Utils.FrpcPlatform}.zip"
                    };
                }
                else {
                    return new()
                    {
                        Level = UpdateLevel.None,
                        DownloadUrl = $"{resp.Data.Launcher?.BaseUrl}client/{resp.Data.Latest}/{Utils.FrpcPlatform}.zip".GetMD5()
                    };
                }
            }
            else
            {
                return new()
                {
                    Level = UpdateLevel.Unsuccessful,
                };
            }
            return new();
        }
        public class UpdateInfo
        {
            public UpdateLevel Level { get; set; }
            public string? DownloadUrl { get; set; }
            public string Content { get; set; } = string.Empty;
            public string? Version { get; set; }
        }
        public enum UpdateLevel
        {
            None = 0,
            FrpcUpdate = 1,
            LauncherUpdate = 2,
            Unsuccessful = 3
        }
    }
}
