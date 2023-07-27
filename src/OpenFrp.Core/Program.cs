using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Google.Protobuf;
using OpenFrp.Core.Helper;
using OpenFrp.Core.Libraries.Api;
using OpenFrp.Core.Libraries.Pipe;
using OpenFrp.Core.Libraries.Protobuf;

namespace OpenFrp.Core
{
    internal class Program
    {
        public static PipeServer? PushClient { get; set; } 

        static async Task Main(string[] args)
        {
            if (args.Length is 1)
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) =>
                {
                    if (!Utils.IsWindowsService && errors is not System.Net.Security.SslPolicyErrors.None)
                    {
                        return MessageBox.Show($"来自服务器的证书无效:::" +
                        $"\n Name:{certificate.Issuer}" +
                        $"\n Reason:{Enum.GetName(typeof(System.Net.Security.SslPolicyErrors), errors)} " +
                        $"是否允许访问?", "OpenFrp Launcher", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation) is MessageBoxResult.OK ? true
                     : false;
                    }
                    else return false;
                };
                switch (args[0])
                {
                    case "-ps": await PipeService();break;
                    case "--install":await InstallService();break;
                    case "--uninstall":await UninstallService();break;
                    case "--uap":await UninstallAppProgress();break;
                }
            }
            else if (args.Length is 2)
            {
                switch (args[0])
                {
                    case "--update":ShowUpdater(args[1]); break;
                }
            }
            else if (Utils.IsWindowsService)
            {
                ServiceBase.Run(new ServiceWorker());
            }
            else if (Environment.UserInteractive)
            {
                Console.WriteLine("Debug Mode");
                Console.WriteLine("Input \"ps\" to start service");
                if (Console.ReadLine() is "ps")
                {
                    await PipeService();
                }
            }

        }

        #region Console App
        /// <summary>
        /// 本地 管道服务
        /// </summary>
        private static async ValueTask PipeService()
        {

            foreach (var process in Process.GetProcessesByName($"{Utils.FrpcPlatform}.exe"))
            {
                if (process.MainModule.FileName == Utils.Frpc)
                {
                    process.Kill();
                }
            }

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                foreach (var wrapper in ConsoleHelper.Wrappers.Values)
                {
                    if (wrapper.Process is not null && wrapper.Process.HasExited is false) wrapper.Process.Kill();
                }
                Debugger.Launch();
            };

            var server = new PipeServer();
            PushClient = new PipeServer();

            server.Start();
            PushClient.Start(true);

            Log("启动成功 Line#90");

            server.OnDataRecived += OnDataRecived;

            server.OnConnectedEvent += async delegate
            {
                Log("传入链接,");
                await Task.Delay(5000);

                if (PushClient?.Pipe?.IsConnected is false)
                {
                    Log("客户都断链 Line#101");
                    server.Disconnect();
                    
                }
            };

            // PUSH 只发送


            server.OnRestart = () =>
            {
                Log("Console Restart Line#111");
                PushClient.Disponse();
                if (PushClient.Pipe?.IsConnected == true)
                {
                    PushClient.Disconnect();
                }
                PushClient.Start(true);
            };
            LogHelper.Add(0, $"OpenFrp Launcher Release 2023 | 系统服务模式: {Utils.IsWindowsService} | 启动器版本 {Utils.LauncherVersion}", TraceLevel.Warning, false);

            Win32Helper.SetConsoleCtrlHandler(o =>
            {
                Log("Wrapper Exit Line#123");
                if (ConsoleHelper.Wrappers.Count > 0)
                {
                    ConfigHelper.Instance.AutoStartupList = ConsoleHelper.Wrappers.Keys.ToArray();
                    foreach (var x in ConsoleHelper.Wrappers.Values.ToArray())
                    {
                        ConsoleHelper.Kill(x.Tunnel!);
                    }
                }
                else if (ConsoleHelper.Wrappers.Count == 0 && ConfigHelper.Instance.AutoStartupList.Length > 0)
                {
                    ConfigHelper.Instance.AutoStartupList = new int[0];
                }

                if (PushClient.IsRunning && PushClient.Pipe?.IsConnected == false)
                {
                    File.WriteAllText(Utils.ConfigFile, ConfigHelper.Instance.JSON());
                }


                Environment.Exit(0);
                return true;
            }, true);

            while (true)
            {
                await Task.Delay(1000);
            }
        }


        private static void Log(object obj)
        {
            if (Environment.UserInteractive) Console.WriteLine(obj);
        }
        /// <summary>
        /// 管道收到数据
        /// </summary>
        private static void OnDataRecived(PipeWorker worker,RequestBase request)
        {
            ResponseBase response = new() { Success = true };
            try
            {
                switch (request.Action)
                {
                    // 客户端上传了登录状态
                    case RequestType.ClientPushLoginstate:
                        {
                            ApiRequest.Authorization = request.LoginRequest.Authorization;
                            ApiRequest.SessionId = request.LoginRequest.SessionId;
                            ApiRequest.UserInfo = request.LoginRequest.UserInfoJson.PraseJson<Libraries.Api.Models.ResponseBody.UserInfoResponse>()?.Data;
                            ConfigHelper.Instance.Account = request.LoginRequest.AccountJson.PraseJson<ConfigHelper.UserAccount>() ?? new();
                            break;
                        };
                    // 客户端请求清除登录
                    case RequestType.ClientPushClearlogin:
                        {
                            ApiRequest.ClearAuth();
                            break;
                        }
                    // 客户端请求 FRPC 启动
                    case RequestType.ClientFrpcStart:
                        {
                            if (request.FrpRequest is not null)
                            {
                                var tunnel = request.FrpRequest.UserTunnelJson.PraseJson<Libraries.Api.Models.ResponseBody.UserTunnelsResponse.UserTunnel>()!;


                                var result = ConsoleHelper.Launch(tunnel);
                                if (!result.Item1)
                                {
                                    response = new() { Message = "发生了未知错误.",Exception = result.Item2?.ToString() };
                                }
                                break;
                            }
                            response = new() { Message = "FRP Request 不能为空。" };
                            break;

                        }
                    // 客户端请求 FRPC 关闭
                    case RequestType.ClientFrpcClose:
                        {
                            if (request.FrpRequest is not null)
                            {
                                var tunnel = request.FrpRequest.UserTunnelJson.PraseJson<Libraries.Api.Models.ResponseBody.UserTunnelsResponse.UserTunnel>()!;
                                ConsoleHelper.Kill(tunnel);
                                break;
                            }
                            response = new() { Message = "FRP Request 不能为空。" };
                            break;
                        };
                    // 获取日志
                    case RequestType.ClientGetLogs:
                        {
                            if (ConsoleHelper.Wrappers.ContainsKey(request.LogsRequest.Id))
                            {
                                //if (LogHelper.Logs[request.LogsRequest.Id].Count >= 100)
                                //{
                                //    LogHelper.Logs[request.LogsRequest.Id].RemoveAt(0);
                                //}
                                LogHelper.Logs[request.LogsRequest.Id].ForEach(x => response.LogsJson.Add(x.JSON()));
                            }
                            else if (LogHelper.Logs.ContainsKey(0))
                            {
                                //if (LogHelper.Logs[0].Count >= 150)
                                //{ 
                                //    LogHelper.Logs[0].RemoveAt(1);
                                //}
                                LogHelper.Logs[0].ForEach(x => response.LogsJson.Add(x.JSON()));
                            }
                            break;
                        }
                    // 获取运行中的隧道 ID
                    case RequestType.ClientGetRunningtunnelsid:
                        {
                            response.RunningCount.AddRange(ConsoleHelper.Wrappers.Keys);
                            break;
                        }
                    // 客户端请求 获取所有正在运行的隧道
                    case RequestType.ClientGetRunningtunnel:
                        {
                            if (ConsoleHelper.Wrappers.ContainsKey(request.LogsRequest.Id))
                            {
                                //if (LogHelper.Logs[request.LogsRequest.Id].Count >= 250) LogHelper.Logs[request.LogsRequest.Id].RemoveRange(1, LogHelper.Logs[request.LogsRequest.Id].Count - 250);

                                LogHelper.Logs[request.LogsRequest.Id].ForEach(x => response.LogsJson.Add(x.JSON()));
                            }
                            else
                            {
                                //if (LogHelper.Logs[0].Count >= 250) LogHelper.Logs[0].RemoveRange(1, LogHelper.Logs[0].Count - 250);

                                LogHelper.Logs[0].ForEach(x => response.LogsJson.Add(x.JSON()));
                            }
                            response.LogsViewJson.Add(new Libraries.Api.Models.ResponseBody.UserTunnelsResponse.UserTunnel() { TunnelName = "全部日志",TunnelId = 0}.JSON());
                            response.LogsViewJson.AddRange(ConsoleHelper.Wrappers.Values.Select(x => x.Tunnel?.JSON()));
                            break;
                        }
                    // 清除日志
                    case RequestType.ClientClearLogs:
                        {
                            LogHelper.Logs[request.LogsRequest.Id].Clear();
                            break;
                        }
                    case RequestType.ClientPushConfig:
                        {
                            ConfigHelper.Instance = request.ConfigJson.PraseJson<ConfigHelper>() ?? new();
                            break;
                        }

                    // 客户端请求关闭 IO
                    case RequestType.ClientCloseIo:
                        {
                            if (ConsoleHelper.Wrappers.Count > 0)
                            {
                                foreach (var x in ConsoleHelper.Wrappers.Values.ToArray())
                                {
                                    ConsoleHelper.Kill(x.Tunnel!);
                                }
                            }
                            Environment.Exit(0);
                            break;
                        }
                    default: { response = new() { Message = "Action not found" }; }; break;
                }
                
            }
            catch (Exception ex)
            {
                response = new()
                {
                    Exception = ex.ToString(),
                    Message = "在服务端处理时发生了未知的错误"
                };
            }

            worker.Send(response.ToByteArray());
        }

        /// <summary>
        /// 安装服务
        /// </summary>
        private async static ValueTask InstallService()
        {

            if (IsServiceInstalled()) return;

            WindowsPrincipal principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                new ProcessStartInfo(Path.Combine(Utils.ApplicationExecutePath,"OpenFrp.Core.exe"), "--install").RunAsUAC();
                Environment.Exit(0);
            }
            else
            {
                await ConfigHelper.ReadConfig();
                try
                {
                    var dir = new DirectoryInfo(Utils.ApplicationExecutePath);
                    var acl = dir.GetAccessControl(System.Security.AccessControl.AccessControlSections.Access);
                    acl.SetAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
                        new SecurityIdentifier(WellKnownSidType.LocalServiceSid, null),
                        System.Security.AccessControl.FileSystemRights.FullControl,
                        System.Security.AccessControl.InheritanceFlags.ObjectInherit,
                        System.Security.AccessControl.PropagationFlags.None,
                        System.Security.AccessControl.AccessControlType.Allow));
                    dir.SetAccessControl(acl);
                    ManagedInstallerClass.InstallHelper(new string[] { Path.Combine(Utils.ApplicationExecutePath, "OpenFrp.Core.exe") });
                    Process.Start(new ProcessStartInfo("sc", "start \"OpenFrp Launcher Service\""));


                    ConfigHelper.Instance.IsServiceMode = true;

                    await ConfigHelper.Instance.WriteConfig();
                    await Task.Delay(3250);
                    Process.Start(new ProcessStartInfo("explorer", Path.Combine(Utils.ApplicationExecutePath, "OpenFrp.Launcher.exe")));
                }
                catch (Exception ex)
                {
                    LogHelper.Add(0, ex.ToString(), System.Diagnostics.TraceLevel.Error, true);
                }

            }
        }

        /// <summary>
        /// 卸载服务
        /// </summary>
        /// <param name="saveConfig">是否保存配置 (FALSE 一般用在卸载时候)</param>
        private async static ValueTask UninstallService(bool saveConfig = true)
        {
            if (IsServiceInstalled())
            {
                WindowsPrincipal principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
                if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
                {
                    new ProcessStartInfo(Path.Combine(Utils.ApplicationExecutePath, "OpenFrp.Core.exe"), "--uninstall").RunAsUAC();
                    Environment.Exit(0);
                }
                else
                {
                    ManagedInstallerClass.InstallHelper(new string[] { "-u", Path.Combine(Utils.ApplicationExecutePath, "OpenFrp.Core.exe") });
                }
            }
            if (saveConfig)
            {
                await ConfigHelper.ReadConfig();
                ConfigHelper.Instance.IsServiceMode = false;
                await ConfigHelper.Instance.WriteConfig();
                Process.Start(new ProcessStartInfo("explorer", Path.Combine(Utils.ApplicationExecutePath, "OpenFrp.Launcher.exe")));
            }
        }

        /// <summary>
        /// 卸载 App 时所执行的
        /// </summary>
        /// <returns></returns>
        private async static ValueTask UninstallAppProgress()
        {
            await UninstallService(false);
            new ProcessStartInfo("sc", "delete \"OpenFrp Launcher Service\"").RunAsUAC();
            Directory.Delete(Utils.ApplicatioDataPath, true);
        }

        /// <summary>
        /// 服务是否已安装
        /// </summary>
        private static bool IsServiceInstalled()
        {
            ServiceController[] services = ServiceController.GetServices();
            foreach (var service in services)
            {
                if (service.ServiceName == "OpenFrp Launcher Service") return true;
            }
            return false;
        }
        #endregion



        private static void ShowUpdater(string body)
        {


            Win32Helper.ShowWindow(Process.GetCurrentProcess().MainWindowHandle, 0);

            



            Thread tr = new Thread(() =>
            {
                if (body is "frpcDownload")
                {
                    // 直接进行 FRPC 下载操作
                    new DownloadWnd().ShowDialog();
                }
                else
                {
                    var info = body.PraseJson<UpdateCheckHelper.UpdateInfo>();
                    new DownloadWnd()
                    {
                        UpdateInfo = info
                    }.ShowDialog();
                }
            });
            tr.SetApartmentState(ApartmentState.STA);
            tr.Start();

        }
    }
}
