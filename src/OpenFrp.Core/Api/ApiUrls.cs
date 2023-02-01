﻿using System;
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
        /// <summary>
        /// 登录
        /// </summary>
        public const string UserLogin = $"{BaseUrl}/user/login";
        /// <summary>
        /// 用户信息
        /// </summary>
        public const string UserInfo = $"{BaseUrl}/frp/api/getUserInfo";
    }
}
