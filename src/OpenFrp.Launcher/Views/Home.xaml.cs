using OpenFrp.Core;
using OpenFrp.Core.Helper;
using OpenFrp.Core.Libraries.Api;
using OpenFrp.Launcher.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
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
    /// Home.xaml 的交互逻辑
    /// </summary>
    public partial class Home : Controls.ViewPage
    {
        public HomeModels Model
        {
            get => (HomeModels)DataContext;
        }

        public Home()
        {
            InitializeComponent();

            Model.MainPage = this;

        }

        protected async override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            await Task.Delay(500);

            Model.RefreshPreview();
            Model.RefreshUserInfoView();
            Model.RefreshBroadCast();
        }


        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var grid = (Grid)sender;
            if (Model.SmallDisplayMode = !(grid.ActualWidth > 735))
            {
                grid.RowDefinitions[0].MaxHeight = double.MaxValue;
                grid.RowDefinitions[0].Height = new GridLength(grid.Height = grid.ActualWidth / 16 * 9);
                grid.ClearValue(HeightProperty);
            }
            else
            {
                grid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
                grid.Height = grid.ColumnDefinitions[0].ActualWidth / 16 * 9;
            }
        }
        
        private new void PreviewMouseWheel(object sender, MouseWheelEventArgs e) => this.ExecuteScroll(e);
    }
}
