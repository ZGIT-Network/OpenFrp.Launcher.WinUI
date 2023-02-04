using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Windows.ApplicationModel.VoiceCommands;

namespace OpenFrp.Launcher.Controls
{
    public partial class TunnelConfig : UserControl
    {



        public bool IsCreating
        {
            get { return (bool)GetValue(IsCreatingProperty); }
            set { SetValue(IsCreatingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for isCreating.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsCreatingProperty =
            DependencyProperty.Register("IsCreating", typeof(bool), typeof(TunnelConfig), new PropertyMetadata(true));



        public Core.Libraries.Api.Models.RequestsBody.EditTunnelData Config
        {
            get { return (Core.Libraries.Api.Models.RequestsBody.EditTunnelData)GetValue(ConfigProperty); }
            set { SetValue(ConfigProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ProxyConfig.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ConfigProperty =
            DependencyProperty.Register("Config", typeof(Core.Libraries.Api.Models.RequestsBody.EditTunnelData), typeof(TunnelConfig), new PropertyMetadata(new Core.Libraries.Api.Models.RequestsBody.EditTunnelData() { }));




        public Core.Libraries.Api.Models.ResponseBody.NodeListsResponse.NodeInfo NodeInfo
        {
            get { return (Core.Libraries.Api.Models.ResponseBody.NodeListsResponse.NodeInfo)GetValue(NodeInfoProperty); }
            set { SetValue(NodeInfoProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NodeInfo.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NodeInfoProperty =
            DependencyProperty.Register("NodeInfo", typeof(Core.Libraries.Api.Models.ResponseBody.NodeListsResponse.NodeInfo), typeof(TunnelConfig), new PropertyMetadata());

        public Core.Libraries.Api.Models.RequestsBody.EditTunnelData GetConfig(bool isEditMode = false)
        {
            if (NodeInfo is not null || isEditMode)
            {
                if (!isEditMode)
                {
                    string item = ((ComboBoxItem)((ComboBox)GetTemplateChild("Of_Protocol_ComboBox")).SelectedItem).Content.ToString();
                    Config.TunnelType = item.ToLower();


                    var bo1x = (NumberBox)GetTemplateChild("Of_RemotePort_Box");
                    if (bo1x.Value is double.NaN or 0)
                    {
                        if (NodeInfo is not null)
                        {
                            if (NodeInfo.MinimumPort != NodeInfo.MaxumumPort)
                            {
                                bo1x.Value = (Config.RemotePort = new Random().Next(NodeInfo.MinimumPort, NodeInfo.MaxumumPort)).Value;
                            }
                            else
                            {
                                bo1x.Value = (Config.RemotePort = new Random().Next(1000, 65535)).Value;
                            }
                        }
                        else
                        {
                            bo1x.Value = (Config.RemotePort = new Random().Next(1000, 65535)).Value;
                        }
                    }
                    else
                    {
                        bo1x.Value = (Config.RemotePort = (int)Math.Round(bo1x.Value)).Value;
                    }
                }



                var bo2x = (NumberBox)GetTemplateChild("Of_LocalPort_Box");
                if (bo2x.Value is double.NaN or 0)
                {
                    bo2x.Value = Config.LocalPort = 25565;
                }
                else
                {
                    bo2x.Value = Config.LocalPort = (int)Math.Round(bo2x.Value);
                }

                Config.NodeID = NodeInfo?.NodeID ?? 0;

                var custom = new StringBuilder();
                if (Config.ProxyProtocolVersion is not 0)
                {
                    custom.Append($"proxy_protocol_version = {(Config.ProxyProtocolVersion is 1 ? "v1" : "v2")} \n");
                }
                var custom_inp = (TextBox)GetTemplateChild("Of_CustomArgs_Box");

                custom.Append(custom_inp.Text.Replace("\\r\\n","\\n"));

                Config.CustomArgs = custom.ToString();

                if (string.IsNullOrEmpty(Config.TunnelName))
                {
                    Config.TunnelName = ((TextBox)GetTemplateChild("Of_TunnelName_Box")).Text =
                    $"OfApp_{new Random().Next(25565, 89889)}";
                };
            }
            return Config;

        }



        public void SetRemotePort(int? port)
        {
            var bo1x = (NumberBox)GetTemplateChild("Of_RemotePort_Box");
            if (bo1x is not null && port is not null) bo1x.Value = port.Value;
        }
        public void SetLocalPort(int port)
        {
            var bo1x = (NumberBox)GetTemplateChild("Of_LocalPort_Box");
            if (bo1x is not null) bo1x.Value = port;
        }
        public void SetLocalAddress(string str)
        {
            var bo1x = (TextBox)GetTemplateChild("Of_TunnelName_Box");
            if (bo1x is not null) Config.LocalAddress = str;
        }

        public async void SetConfig(Core.Libraries.Api.Models.RequestsBody.EditTunnelData createProxy)
        {
            Config = createProxy;
            var builder = new StringBuilder();
            if (!string.IsNullOrEmpty(createProxy.CustomArgs))
            {
                var args = createProxy.CustomArgs.Split(new char[] { '\n' },StringSplitOptions.RemoveEmptyEntries);
               
                foreach (var arg in args)
                {
                    var args2 = arg.Split('=');
                    if (args2.FirstOrDefault().Replace(" ", "") is "proxy_protocol_version")
                    {
                        Config.ProxyProtocolVersion = args2.LastOrDefault().Replace(" ", "") switch
                        {
                            "v1" => 1,
                            _ => 2
                        };
                        continue;
                    }
                    
                    builder.Append(arg + "\n");
                    
                }
                
            }

            await Task.Delay(500);

            SetLocalPort(createProxy.LocalPort);
            SetRemotePort(createProxy.RemotePort);
            SetLocalAddress(createProxy.LocalAddress ?? "127.0.0.1");
            if (GetTemplateChild("Of_Protocol_ComboBox") is ComboBox box)
            {
                box.SelectedIndex = 0;
            }
            if (GetTemplateChild("Of_CustomArgs_Box") is TextBox custom_inp)
            {
                custom_inp.Text = Config.CustomArgs = builder.ToString();
            }
                
        }
    }
}
