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
    /// CreateTunnel.xaml 的交互逻辑
    /// </summary>
    public partial class CreateTunnel : Controls.ViewPage
    {
        public CreateTunnelModel Model
        {
            get => (CreateTunnelModel)DataContext;
        }

        public CreateTunnel()
        {
            InitializeComponent();
            Model.CreateTunnelPage = this;
            Model.RefreshNodeList();
            this.SizeChanged += (sender, args) =>
            {
                Model.IsSmallMode = this.ActualWidth <= 850;
            };
            Configer.Config.TunnelName = default;
        }

        private void ScrollViewerEx_PreviewMouseWheel(object sender, MouseWheelEventArgs e) => ((ScrollViewerEx)sender).ExcuteScroll(e);
    }
}
