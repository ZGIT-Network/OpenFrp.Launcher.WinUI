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
    }
}
