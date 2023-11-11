using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using OpenFrp.Core.Helper;
using System.Diagnostics;
using System.ComponentModel;
using System.IO.Compression;
using System.Security.Principal;
using System.Windows.Threading;
using ModernWpf;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;

namespace OpenFrp.Core
{
    /// <summary>
    /// DownloadWnd.xaml 的交互逻辑
    /// </summary>
    public partial class DownloadWnd : Window
    {
        public DownloadWnd()
        {
            InitializeComponent();
        }

        internal UpdateCheckHelper.UpdateInfo? UpdateInfo { get; set; }

        private string? FileName { get; set; }

        protected override async void OnInitialized(EventArgs e)
        {

            base.OnInitialized(e);

            await ConfigHelper.ReadConfig();
            AppDomain.CurrentDomain.UnhandledException += (s, e) => MessageBox.Show(e.ExceptionObject.ToString());
            Dispatcher.RunOnUIThread(() =>
            {
                Title = $"OpenFrp 下载窗口 ::: {Utils.LauncherVersion}";
                Inst();
            });
            
        }

        void Inst()
        {
            Dispatcher.RunOnUIThread(async () =>
            {
                Btn_Reload.IsEnabled = false;

                if (UpdateInfo is not null && !string.IsNullOrEmpty(UpdateInfo.DownloadUrl))
                {
                    AddIntoView($"下载链接: {UpdateInfo.DownloadUrl}");
                    FileName = Path.Combine(Utils.ApplicationExecutePath, $"{UpdateInfo.DownloadUrl!.GetMD5()}.{UpdateInfo.DownloadUrl?.Split('.').LastOrDefault()}");
                    Download(UpdateInfo.DownloadUrl!, FileName, UpdateInfo);
                }
                else
                {

                    AddIntoView("获取更新中...");
                    var resp = await UpdateCheckHelper.CheckUpdate();
                    if (resp.Level is UpdateCheckHelper.UpdateLevel.None)
                    {
                        //Process.Start(new ProcessStartInfo("explorer", Path.Combine(Utils.ApplicationExecutePath, "OpenFrp.Launcher.exe")));
                        Environment.Exit(0);
                    }
                    else if (resp.Level is UpdateCheckHelper.UpdateLevel.Unsuccessful)
                    {
                        string fallback = $"https://sq.oss.imzzh.cn/launcher/lock/{Utils.FrpcPlatform}.zip";
                        if (File.Exists(Utils.Frpc))
                        {
                            AddIntoView($"fallback frpc url: {fallback}");
                            Download(fallback, Path.Combine(Utils.ApplicationExecutePath, $"{fallback.GetMD5()}.{fallback.Split('.').LastOrDefault()}"),new UpdateCheckHelper.UpdateInfo
                            {
                                Level = UpdateCheckHelper.UpdateLevel.FrpcUpdate,
                                Version = "lock version"
                            });
                        }
                        else
                        {
                            AddIntoView($"获取列表失败,请点击\"重新下载\"按钮重试。{JsonConvert.SerializeObject(resp)}");
                            Dispatcher.RunOnUIThread(() =>
                            {
                                Btn_Reload.IsEnabled = true;
                            });
                        }
                        

                        return;
                    }

                    AddIntoView($"{System.Enum.GetName(typeof(UpdateCheckHelper.UpdateLevel), resp.Level)} - 下载链接: {resp.DownloadUrl}");

                    Download(resp.DownloadUrl!, Path.Combine(Utils.ApplicationExecutePath, $"{resp.DownloadUrl!.GetMD5()}.{resp.DownloadUrl?.Split('.').LastOrDefault()}"), resp);
                }
            });
            
        }

        private async void Download(string url,string filename, UpdateCheckHelper.UpdateInfo level)
        {
            FileName = filename;
            var vl = await UpdateCheckHelper.DownloadWithProgress(url, FileName, (sender, ar) => AddIntoView($"Download Progress: {ar.ProgressPercentage}"));
            if (vl.Item2)
            {

                // Successful
                if (level.Level is UpdateCheckHelper.UpdateLevel.FrpcUpdate)
                {
                    try
                    {
                        string frpcd = Path.Combine(Utils.ApplicationExecutePath, "frpc");
                        try
                        {
                            foreach (var process in Process.GetProcessesByName($"{Utils.FrpcPlatform}.exe"))
                            {
                                if (process.MainModule.FileName == Utils.Frpc)
                                {
                                    process.Kill();
                                }
                            }

                            if (Directory.Exists(frpcd))
                                Directory.Delete(frpcd, true);


                        }
                        catch (UnauthorizedAccessException)
                        {
                            new ProcessStartInfo("cmd", $"/c rd /s /q \"{frpcd}\"").RunAsUAC();
                            await Task.Delay(1500);
                        }

                        Directory.CreateDirectory(frpcd);

                        var dir = new DirectoryInfo(frpcd);
                        var acl = dir.GetAccessControl(System.Security.AccessControl.AccessControlSections.Access);
                        acl.SetAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
                            new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                            System.Security.AccessControl.FileSystemRights.FullControl,
                            System.Security.AccessControl.InheritanceFlags.ObjectInherit,
                            System.Security.AccessControl.PropagationFlags.None,
                            System.Security.AccessControl.AccessControlType.Allow));





                        try
                        {
                            using var zip = new ZipArchive(File.OpenRead(filename));
                            zip.ExtractToDirectory(frpcd);
                            zip.Dispose();

                            if (FileName is not null && File.Exists(FileName)) File.Delete(FileName);

                            ConfigHelper.Instance.FrpcVersion = level.Version;

                            await ConfigHelper.Instance.WriteConfig(true);

                            await Task.Delay(500);

                            Process.Start(new ProcessStartInfo()
                            {
                                FileName = Utils.Frpc,
                                Arguments = $"-v",
                                UseShellExecute = false,
                                CreateNoWindow = true
                            });
                            
                        }
                        catch (System.ComponentModel.Win32Exception)
                        {
                            Process.Start("https://docs.openfrp.net/use/desktop-launcher.html#%E5%8A%A0%E5%85%A5%E7%B3%BB%E7%BB%9F%E7%99%BD%E5%90%8D%E5%8D%95");
                            Environment.Exit(-1);
                        }
                        catch (System.IO.IOException ex)
                        {
                            if (ex.Message.Contains("无法成功完成操作，因为文件包含病毒或潜在的垃圾软件"))
                            {
                                Process.Start("https://docs.openfrp.net/use/desktop-launcher.html#%E5%8A%A0%E5%85%A5%E7%B3%BB%E7%BB%9F%E7%99%BD%E5%90%8D%E5%8D%95");
                            }
                            else
                            {
                                MessageBox.Show(ex.ToString());
                            }
                            
                            Environment.Exit(-1);
                        }
                        Process.Start(new ProcessStartInfo(Path.Combine(Utils.ApplicationExecutePath, "OpenFrp.Launcher.exe"),"--re"));
                        Environment.Exit(0);
                    }
                    catch
                    {
                        new ProcessStartInfo(Path.Combine(Utils.ApplicationExecutePath, "OpenFrp.Core.exe"), "--update frpcDownload").RunAsUAC();
                    }
                }
                else if (level.Level is UpdateCheckHelper.UpdateLevel.LauncherUpdate)
                {
                    Process.Start(new ProcessStartInfo(filename));

                    Environment.Exit(0);
                }
            }
            else
            {
                Dispatcher.RunOnUIThread(() =>
                {
                    AddIntoView("下载失败,请点击\"重新下载\"按钮重试。");
                    AddIntoView(vl.Item1?.ToString() ?? "");
                    Btn_Reload.IsEnabled = true;
                });
            }
        }

        private void AddIntoView(string data) => Dispatcher.RunOnUIThread(() =>
        {
            LogView?.AppendText($"{data}\n");
            LogView?.ScrollToEnd();
        });

        private void Button_Click(object sender, RoutedEventArgs e) => Dispatcher.RunOnUIThread(Inst);

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            e.Cancel = true;
            UpdateCheckHelper._Client.CancelAsync();
            try
            {
                if (FileName is not null && File.Exists(FileName)) File.Delete(FileName);
            }
            catch
            {

            }
            Environment.Exit(0);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            string frpcd = Path.Combine(Utils.ApplicationExecutePath, "frpc");
            Directory.CreateDirectory(frpcd);
            Process.Start("explorer",frpcd);
        }
    }
}
