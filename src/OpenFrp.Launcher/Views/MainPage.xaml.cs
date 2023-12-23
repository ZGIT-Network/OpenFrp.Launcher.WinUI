using ModernWpf.Controls.Primitives;
using OpenFrp.Core;
using OpenFrp.Core.Helper;
using OpenFrp.Core.Libraries.Protobuf;
using OpenFrp.Launcher.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.IO;
using OpenFrp.Launcher.ViewModels;

namespace OpenFrp.Launcher.Views
{
    /// <summary>
    /// Launcher - 页面框架
    /// 理论上不符合 MVVM 概念，凑合着用吧。
    /// </summary>
    public partial class MainPage : Window
    {
        public MainPage() => InitializeComponent();

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            Debug.WriteLine($"OpenFrp Launcher 2023 | Pipe标识符: {Utils.PipesName}");

            Of_nView.ItemInvoked += (sender, args) =>
            {
                Type? pages = args.IsSettingsInvoked switch
                {
                    true => typeof(Setting),
                    false => new Func<Type?>(() =>
                    {
                        return ((NavigationViewItem)sender.SelectedItem).Tag switch
                        {
                            "Home" => typeof(Views.Home),
                            "Tunnels" => typeof(Views.Tunnels),
                            "Logs" => typeof(Views.Logs),
                            "About" => typeof(Views.About),
                            "NewTab" => typeof(Views.NewTab),
                            _ => null
                        };
                    })(),
                };

                if (Of_nViewFrame.SourcePageType == pages) return;
                if (pages is not null) Of_nViewFrame.Navigate(pages);
                
            };
            Of_nViewFrame.Navigating += (sender, args) =>
            {
                if (args.Uri is not null) args.Cancel = true;
            };

            (this.DataContext as MainPageModel)?.CheckUpdate(true);
        }

        protected override void OnActivated(EventArgs e)
        {
            try
            {
                Visibility = Visibility.Visible;
            }
            catch
            {

            }
            
            base.OnActivated(e);
        }
    }
}
