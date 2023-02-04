using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

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

        public class EditTunnelData : BaseRequest
        {
            /// <summary>
            /// 隧道名称
            /// </summary>
            [JsonProperty("name")]
            public string? TunnelName { get; set; }
            /// <summary>
            /// 隧道所在的节点ID
            /// </summary>
            [JsonProperty("node_id")]
            public int NodeID { get; set; }
            /// <summary>
            /// 本地链接
            /// </summary>
            [JsonProperty("local_addr")]
            public string? LocalAddress { get; set; } = "127.0.0.1";
            /// <summary>
            /// 本地端口
            /// </summary>
            [JsonProperty("local_port")]
            public int LocalPort { get; set; }
            /// <summary>
            /// 远程端口
            /// </summary>
            [JsonProperty("remote_port")]
            public int? RemotePort { get; set; }
            /// <summary>
            /// 绑定的域名
            /// </summary>
            [JsonProperty("domain_bind")]
            private string _BindDomain
            {
                get => JsonConvert.SerializeObject(BindDomain);
                set => BindDomain = JsonConvert.DeserializeObject<string[]>(value) ?? new string[0];
            }

            [JsonIgnore]
            public string[] BindDomain { get; set; } = new string[0];

            [JsonProperty("dataGzip")]
            public bool GZipMode { get; set; }

            [JsonProperty("dataEncrypt")]
            public bool EncryptMode { get; set; }
            /// <summary>
            /// 自定义参数
            /// </summary>
            [JsonProperty("custom")]
            public string CustomArgs { get; set; } = string.Empty;
            /// <summary>
            /// 请求来源
            /// </summary>
            [JsonProperty("request_from")]
            public string? RequestFrom { get; set; }
            /// <summary>
            /// Host重写
            /// </summary>
            [JsonProperty("host_rewrite")]
            public string? HostRewrite { get; set; }
            /// <summary>
            /// 请求密码
            /// </summary>
            [JsonProperty("request_pass")]
            public string? RequestPass { get; set; }
            /// <summary>
            /// URL 路由
            /// </summary>
            [JsonProperty("url_route")]
            public string? URLRoute { get; set; }

            /// <summary>
            /// 隧道类型
            /// </summary>
            [JsonProperty("type")]
            public string? TunnelType { get; set; }
            /// <summary>
            /// Proxy Protocol Version
            /// </summary>
            [JsonIgnore]
            public int ProxyProtocolVersion { get; set; }

            [JsonProperty("proxy_id")]
            public int? TunnelID { get; set; }

        }
    }
#pragma warning disable IDE0051 // 删除未使用的私有成员
    public class ResponseBody
    {
        /// <summary>
        /// 首页大图返回
        /// </summary>
        public class HomePageResponse
        {
            [JsonProperty("title")]
            public string Title { get; set; } = "剑河风急雪片阔，沙口石冻马蹄脱";

            [JsonProperty("content")]
            public string Content { get; set; } = "打开官网";

            [JsonProperty("link")]
            public string Link { get; set; } = "https://www.openfrp.net";

            [JsonProperty("forceImage")]
            public bool ForceImage { get; set; }

            [JsonProperty("image")]
            public string? Image { get; set; }
        }


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

        /// <summary>
        /// 用户隧道列表返回
        /// </summary>
        public class UserTunnelsResponse : BaseResponse
        {
            public UserTunnelsResponse()
            {
            }

            public UserTunnelsResponse(string message) : base(message)
            {
            }

            public UserTunnelsResponse(Exception ex) : base(ex)
            {
            }

            [JsonProperty("data")]
            public new TotalContain<UserTunnel>? Data { get; set; }

            public class UserTunnel
            {
                /// <summary>
                /// 隧道链接
                /// </summary>
                [JsonProperty("connectAddress")]
                public string? ConnectAddress { get; set; }

                /// <summary>
                /// 远程端口号
                /// </summary>
                [JsonProperty("remotePort")]
                public int? RemotePort { get; set; }
                /// <summary>
                /// 自定义属性
                /// </summary>
                [JsonProperty("custom")]
                public string? CustomArgs { get; set; }
                /// <summary>
                /// 域名列表
                /// </summary>
                [JsonIgnore]
                public string[] Domains { get; set; } = new string[0];
                [JsonProperty("domain")]

                private string _domains
#pragma warning restore IDE0051 // 删除未使用的私有成员
                {
                    get => JsonConvert.SerializeObject(Domains);
                    set => Domains = JsonConvert.DeserializeObject<string[]>(value) ?? new string[0];
                }

                /// <summary>
                /// 节点名称
                /// </summary>
                [JsonProperty("friendlyNode")]
                public string? NodeName { get; set; }
                /// <summary>
                /// 节点ID
                /// </summary>
                [JsonProperty("node")]
                public int NodeID { get; set; }
                /// <summary>
                /// 隧道 ID
                /// </summary>
                [JsonProperty("id")]
                public int TunnelId { get; set; }
                /// <summary>
                /// 在隧道头部加上的X-From-Where
                /// </summary>
                [JsonProperty("locations")]
                public string? URLRoute { get; set; }
                /// <summary>
                /// 请求来源
                /// </summary>
                [JsonProperty("headerXFromWhere")]
                public string? RequestFrom { get; set; }
                /// <summary>
                /// 请求密码
                /// </summary>
                [JsonProperty("sk")]
                public string? RequestPassword { get; set; }
                /// <summary>
                /// 上次开启
                /// </summary>
                [JsonProperty("lastLogin")]
                public long Lastlogin { get; set; }
                /// <summary>
                /// 上次更新
                /// </summary>
                [JsonProperty("lastUpdate")]
                public long LastUpdate { get; set; }
                /// <summary>
                /// 本地IP
                /// </summary>
                [JsonProperty("localIp")]
                public string? LocalAddress { get; set; }
                /// <summary>
                /// 本地端口
                /// </summary>
                [JsonProperty("localPort")]
                public int LocalPort { get; set; }
                /// <summary>
                /// 隧道名称
                /// </summary>
                [JsonProperty("proxyName")]
                public string? TunnelName { get; set; }
                /// <summary>
                /// 隧道类型
                /// </summary>
                public string? TunnelType { get; set; }

                [JsonProperty("proxyType")]
                private string? _tunnelType
                {
                    get { return ""; }
                    set
                    {
                        TunnelType = value?.ToUpper();
                    }
                }
                /// <summary>
                /// 是否在线
                /// </summary>
                [JsonProperty("online")]
                public bool Online { get; set; }
                /// <summary>
                /// 是否启用
                /// </summary>
                [JsonProperty("status")]
                public bool IsEnabled { get; set; }
                /// <summary>
                /// 是否开启数据压缩
                /// </summary>
                [JsonProperty("useCompression")]
                public bool ComperssionMode { get; set; }
                /// <summary>
                /// 是否有加密传输
                /// </summary>
                [JsonProperty("useEncryption")]
                public bool EncryptionMode { get; set; }
                /// <summary>
                /// Host 重写
                /// </summary>
                [JsonProperty("hostHeaderRewrite")]
                public string? HostRewrite { get; set; }
            }
        }
        /// <summary>
        /// 节点列表返回
        /// </summary>
        public class NodeListsResponse : BaseResponse
        {
            public NodeListsResponse()
            {
            }

            public NodeListsResponse(string message) : base(message)
            {
            }

            public NodeListsResponse(Exception ex) : base(ex)
            {
            }
            public NodeListsResponse(List<NodeInfo> nodes)
            {
                Data = new()
                {
                    Total = nodes.Count,
                    List = nodes
                };
            }

            [JsonProperty("data")]
            public new TotalContain<NodeInfo>? Data { get; set; }

            public class NodeInfo
            {
                /// <summary>
                /// 节点名称
                /// </summary>
                [JsonProperty("name")]
                public string? NodeName { get; set; }
                /// <summary>
                /// 节点 ID
                /// </summary>
                [JsonProperty("id")]
                public int NodeID { get; set; }
                /// <summary>
                /// 注释 (实际为 comments)
                /// </summary>
                [JsonProperty("comments")]
                public string? Description { get; set; }
                /// <summary>
                /// 是否需要实名
                /// </summary>
                [JsonProperty("needRealname")]
                public bool NeedRealname { get; set; }

                [JsonProperty("classify")]
                public NodeClassify NodeClassify { get; set; }
                /// <summary>
                /// 节点绑定域名
                /// </summary>
                [JsonProperty("hostname")]
                public string? HostName { get; set; }
                /// <summary>
                /// 支持的协议
                /// </summary>
                [JsonProperty("protocolSupport")]
                public NodeProtocolSupport ProtocolSupport { get; set; } = new();
                /// <summary>
                /// 节点状态
                /// </summary>
                [JsonProperty("status")]
                public int Status { get; set; }
                /// <summary>
                /// 节点所需组
                /// </summary>
                [JsonProperty]
                public string? Group { get; set; }

                [JsonIgnore]
                public bool IsVipNode
                {
                    get => Group?.IndexOf("normal") == -1;
                }
                /// <summary>
                /// 最大端口
                /// </summary>
                [JsonIgnore]
                public int MaxumumPort { get; set; }
                /// <summary>
                /// 最小端口
                /// </summary>
                [JsonIgnore]
                public int MinimumPort { get; set; }

                /// <summary>
                /// 服务器是否满载
                /// </summary>
                [JsonProperty("fullyLoaded")]
                public bool IsFully { get; set; }
                [JsonProperty]
                private string allowPort
                {
                    get { return ""; }
                    set
                    {
                        if (!string.IsNullOrEmpty(value))
                        {
                            string[] ports = value.Substring(1, value.Length - 2).Split(',');
                            MinimumPort = Convert.ToInt32(ports[0]);
                            MaxumumPort = Convert.ToInt32(ports[1]);
                        }
                    }
                }
                /// <summary>
                /// 是否为表头 (内部用)
                /// </summary>
                [JsonIgnore]
                public bool IsHeader { get; set; }
                /// <summary>
                /// 宽带
                /// </summary>
                [JsonProperty("bandwidth")]
                public int NetworkSpeed { get; set; }
                /// <summary>
                /// 速率翻倍
                /// </summary>
                [JsonProperty("bandwidthMagnification")]
                public double SppedMagnification { get; set; }
            }

            /// <summary>
            /// 节点在世界的位置
            /// </summary>
            public enum NodeClassify
            {
                ChinaMainland = 1,
                ChinaTW_HK = 2,
                Other = 3,
            }
            /// <summary>
            /// 支持的协议
            /// </summary>
            public class NodeProtocolSupport
            {
                public ComboBoxItem[] ComboBoxUICollection
                {
                    get
                    {
                        return new ComboBoxItem[]
                        {
                                new(){Content = "TCP",IsEnabled = TCP},
                                new(){Content = "UDP",IsEnabled = UDP},
                                new(){Content = "HTTP",IsEnabled = HTTP},
                                new(){Content = "HTTPS",IsEnabled = HTTPS},
                                new(){Content = "XTCP",IsEnabled = XTCP},
                                new(){Content = "STCP",IsEnabled = STCP},
                        };
                    }
                }

                public string[] UnsupportedMode
                {
                    get
                    {
                        List<string> strs = new();
                        ComboBoxUICollection.ToList().ForEach(item =>
                        {
                            if (!item.IsEnabled) { strs.Add(item.Content.ToString()); }
                        });
                        return strs.ToArray();
                    }
                }

                public int DefualtIndex { get; } = 0;

                [JsonProperty("tcp")]
                public bool TCP { get; set; }
                [JsonProperty("udp")]
                public bool UDP { get; set; }

                [JsonProperty("http")]
                public bool HTTP { get; set; }
                [JsonProperty("https")]
                public bool HTTPS { get; set; }

                [JsonProperty("stcp")]
                public bool STCP { get; set; }
                [JsonProperty("xtcp")]
                public bool XTCP { get; set; }

            }
        }


    }
    public class TotalContain<T>
    {
        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("list")]
        public List<T> List { get; set; } = new();
    }
}
