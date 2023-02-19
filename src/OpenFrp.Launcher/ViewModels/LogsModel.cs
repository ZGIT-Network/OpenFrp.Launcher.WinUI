using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenFrp.Core.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace OpenFrp.Launcher.ViewModels
{
    public partial class LogsModel : ObservableObject
    {
        public ConfigHelper.FontSetting FontSetting
        {
            get => Core.Helper.ConfigHelper.Instance.FontSet;
            set => Core.Helper.ConfigHelper.Instance.FontSet = value;
        }

        public List<Core.Libraries.Api.Models.ResponseBody.UserTunnelsResponse.UserTunnel?>? UserTunnels { get; set; }

        public List<LogHelper.LogContent?>? LogContent { get; set; } 


        [ObservableProperty]
        public int selectLogIndex;

        public ICollectionView LogsHeaders { get => CollectionViewSource.GetDefaultView(UserTunnels); }
        public ICollectionView LogsViewer { get => CollectionViewSource.GetDefaultView(LogContent); }

        private int Count { get; set; }


        public async void GetLogs(bool reset = false)
        {
            var request = new Core.Libraries.Protobuf.RequestBase()
            {
                Action = Core.Libraries.Protobuf.RequestType.ClientGetRunningtunnel,
                LogsRequest = new()
                {
                    Id = UserTunnels?[SelectLogIndex]?.TunnelId ?? 0
                }
            };
            var response = await Helper.AppShareHelper.PipeClient.Request(request);

            if (response.Success)
            {
                var u2serTunnels = response.LogsViewJson.Select(x => x.PraseJson<Core.Libraries.Api.Models.ResponseBody.UserTunnelsResponse.UserTunnel>()).ToList();
                if (reset) LogContent = default;
                UserTunnels ??= new();
                LogContent ??= new();
                if (response.LogsJson.Count > 0)
                {
                    await DispatcherHelper.ExecuteOnUIThreadAsync(() =>
                    {
                        if (u2serTunnels.Count > Count)
                        {
                            u2serTunnels.ForEach(x =>
                            {
                                if (!UserTunnels.Select(a => a?.TunnelId).Contains(x?.TunnelId))
                                {
                                    UserTunnels.Add(x);
                                }

                            });
                            LogsHeaders.Refresh();
                        }
                        else if (u2serTunnels.Count < Count)
                        {
                            var u3sert = UserTunnels.ToList();
                            u3sert.ForEach(x =>
                            {
                                if (!u2serTunnels.Select(a => a?.TunnelId).Contains(x?.TunnelId))
                                {
                                    UserTunnels.Remove(x);
                                    if (SelectLogIndex > Count || !u2serTunnels.Select(a => a?.TunnelId).Contains(u3sert[SelectLogIndex]?.TunnelId))
                                    {
                                        SelectLogIndex = 0;
                                    }
                                }
                            });
                            LogsHeaders.Refresh();
                        }

                        Count = u2serTunnels.Count;

                        response.LogsJson.Select(x => x.PraseJson<LogHelper.LogContent>()).ToList().ForEach(x =>
                        {

                            if (!LogContent.Select(x => x?.HashContent).Contains(x?.HashContent))
                            {
                                LogContent.Add(x);
                                LogsViewer.Refresh();
                                OnPropertyChanged(nameof(LogsViewer));
                            }

                        });



                    }, System.Windows.Threading.DispatcherPriority.Background);
                }
                else
                {
                    LogContent?.Clear();
                    LogsViewer?.Refresh();
                    OnPropertyChanged(nameof(LogsViewer));
                }
                
                OnPropertyChanged(nameof(LogsHeaders));
            }

        }

        [RelayCommand]
        void Refresh() => GetLogs(true);

        [RelayCommand]
        async void ClearLogs()
        {
            var request = new Core.Libraries.Protobuf.RequestBase()
            {
                Action = Core.Libraries.Protobuf.RequestType.ClientClearLogs,
                LogsRequest = new()
                {
                    Id = UserTunnels?[SelectLogIndex]?.TunnelId ?? 0
                }
            };
            var response = await Helper.AppShareHelper.PipeClient.Request(request);

            if (response.Success)
            {
                LogContent?.Clear();
                LogsViewer.Refresh();
                GetLogs(true);
            }
        }

        [RelayCommand]
        async void SaveLogs()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog()
            {
                Filter = "日志文件(*.log)|*.log",
                CheckPathExists = true,
            };
            if (dialog.ShowDialog() is true)
            {
                try
                {
                    var writer = dialog.FileName.GetStreamWriter(autoFlush: true);
                    foreach (LogHelper.LogContent? item in LogContent!)
                    {
                        await writer.WriteLineAsync(item?.Content);
                    }
                    writer.Close();
                    var dialog2 = new ContentDialog()
                    {
                        Title = "保存日志",
                        Content = new TextBlock()
                        {
                            Width = 200,
                            Height = 50,
                            Text = "保存成功!",
                            HorizontalAlignment = System.Windows.HorizontalAlignment.Left
                        },
                        CloseButtonText = "确定",
                        PrimaryButtonText = "打开"
                    };
                    if (await dialog2.ShowDialogFixed() is ContentDialogResult.Primary)
                    {
                        Process.Start(dialog.FileName);
                    }
                }
                catch
                {

                }
            }
        }
    }
}
