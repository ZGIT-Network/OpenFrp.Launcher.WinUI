using Newtonsoft.Json.Linq;
using OpenFrp.Core.Helper;
using OpenFrp.Core.Libraries.Api;
using OpenFrp.Core.Libraries.Api.Models;
using OpenFrp.Core.Libraries.Pipe;
using OpenFrp.Launcher.ViewModels;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OpenFrp.Launcher.Views
{
    /// <summary>
    /// Tunnels.xaml 的交互逻辑
    /// </summary>
    public partial class Tunnels : Controls.ViewPage
    {
        public TunnelsModel Model
        {
            get => (TunnelsModel)DataContext;
        }

        public Tunnels()
        {
            InitializeComponent();

            Model.TunnelsPage = this;

            Model.RefreshUserProxies();

        }

        private void ScrollViewerEx_PreviewMouseWheel(object sender, MouseWheelEventArgs e) => ((ScrollViewerEx)sender).ExcuteScroll(e);


        /// <summary>
        /// 隧道状态被切换(开启 + 关闭)
        /// </summary>
        private async void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch @switch = (ToggleSwitch)sender;
            if (@switch.DataContext is ResponseBody.UserTunnelsResponse.UserTunnel tunnel && !_bypassToggle)
            {
                _bypassToggle = true;
                if (@switch.IsOn)
                {
                    var request = new Core.Libraries.Protobuf.RequestBase()
                    {
                        Action = Core.Libraries.Protobuf.RequestType.ClientFrpcStart,
                        FrpRequest = new()
                        {
                            UserTunnelJson = tunnel.JSON()
                        }
                    };
                    var response = await Helper.AppShareHelper.PipeClient.Request(request);

                    if (!response.Success)
                    {
                        MessageBox.Show(response.Message);
                        @switch.IsOn = false;
                    }
                    
                }
                else
                {
                    var request = new Core.Libraries.Protobuf.RequestBase()
                    {
                        Action = Core.Libraries.Protobuf.RequestType.ClientFrpcClose,
                        FrpRequest = new()
                        {
                            UserTunnelJson = tunnel.JSON()
                        }
                    };
                    var response = await Helper.AppShareHelper.PipeClient.Request(request);

                    if (!response.Success)
                    {
                        MessageBox.Show(response.Message);
                        @switch.IsOn = true;
                    }
                }
                _bypassToggle = false;
                return;
            }
            else if (!_bypassToggle)
            {
                _bypassToggle = true;
                @switch.IsOn = false;
                _bypassToggle = false;
            }

        }

        private bool _bypassToggle { get; set; }
    }
}
