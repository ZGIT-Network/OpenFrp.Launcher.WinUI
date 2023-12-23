using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenFrp.Core.Libraries.Api
{
    /*
     *  API 访问链接
     *  v4 - 2023/1/29
     */
    public abstract class ApiUrls
    {
        /// <summary>
        /// 官方 Api Url
        /// </summary>

        public const string BaseUrl = @"https://of-dev-api.bfsea.xyz";
        //public const string BaseUrl = @"https://console.openfrp.net/web";

        public const string LauncherBaseUrl = @"https://api.mclan.icu/";


        /// <summary>
        /// OAuth (update on 2023-11-3)
        /// </summary>
        public const string OAuthUrl = @"https://openid.17a.ink/";
        /// <summary>
        /// 登录
        /// </summary>
        public const string UserLogin = $"{OAuthUrl}/api/public/login";
        //public const string UserLogin = $"{BaseUrl}/user/login";

        public const string GetAuthorize = $"{BaseUrl}/oauth2/login";
        //public const string Authorize = $"{OAuthUrl}/api/oauth2/authorize?response_type=code&redirect_uri=http:%2F%2Fconsole.openfrp.net%2Foauth_callback&client_id=openfrp";

        public const string OpenFrpCodeImpt = $"{BaseUrl}/oauth2/callback?code=";


        /// <summary>
        /// 用户信息
        /// </summary>
        public const string UserInfo = $"{BaseUrl}/frp/api/getUserInfo";
        /// <summary>
        /// 用户签到
        /// </summary>
        public const string Signin = $"{BaseUrl}/frp/api/userSign";
        /// <summary>
        /// 公告
        /// </summary>
        public const string BroadCast = $"{BaseUrl}/commonQuery/get?key=broadcast";
        /// <summary>
        /// 软件列表
        /// </summary>
        public const string SoftwareSupport = $"{BaseUrl}/commonQuery/get?key=software";
        /// <summary>
        /// 用户隧道
        public const string UserTunnels = $"{BaseUrl}/frp/api/getUserProxies";
        /// <summary>
        /// 节点列表
        /// </summary>
        public const string NodeList = $"{BaseUrl}/frp/api/getNodeList";
        /// <summary>
        /// 创建隧道
        /// </summary>
        public const string CreateTunnel = $"{BaseUrl}/frp/api/newProxy";
        /// <summary>
        /// 移除隧道
        /// </summary>
        public const string RemoveTunnel = $"{BaseUrl}/frp/api/removeProxy";

        /// <summary>
        /// 编辑隧道
        /// </summary>
        public const string EditTunnel = $"{BaseUrl}/frp/api/editProxy";
    }
}
