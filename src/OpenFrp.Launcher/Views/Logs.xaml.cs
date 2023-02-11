using OpenFrp.Core.Helper;
using OpenFrp.Core.Libraries.Protobuf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Logs.xaml 的交互逻辑
    /// </summary>
    public partial class Logs : Controls.ViewPage
    {
        public Logs()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var request = new Core.Libraries.Protobuf.RequestBase()
            {
                Action = Core.Libraries.Protobuf.RequestType.ClientGetLogs,
                LogsRequest = new()
                {
                    Id = 0
                }
            };
            var response = await Helper.AppShareHelper.PipeClient.Request(request);

            foreach (var item in response.LogsJson)
            {
                Debug.WriteLine(item.PraseJson<LogHelper.LogContent>().Content);
            }
        }
    }
}
