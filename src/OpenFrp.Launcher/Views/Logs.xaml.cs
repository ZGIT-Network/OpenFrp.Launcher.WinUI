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
        public ViewModels.LogsModel Model
        {
            get => (ViewModels.LogsModel)DataContext;
        }


        public Logs() => InitializeComponent();

        protected override async void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            sbbv.FontFamily = new FontFamily(ConfigHelper.Instance.FontSet.FontFamily ?? "Microsoft YaHei UI");
            do
            {
                Model.GetLogs();
                await Task.Delay(1000);
            } while (IsInitialized && IsLoaded);
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => Model.GetLogs(true);

        private void ScrollViewerEx_PreviewMouseWheel(object sender, MouseWheelEventArgs e) => ((ScrollViewerEx)sender).ExcuteScroll(e);
    }
}
