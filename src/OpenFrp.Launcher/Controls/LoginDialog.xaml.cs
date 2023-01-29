using Google.Protobuf;
using OpenFrp.Launcher.Helper;
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

namespace OpenFrp.Launcher.Controls
{
    /// <summary>
    /// LoginDialog.xaml 的交互逻辑
    /// </summary>
    public partial class LoginDialog : ContentDialog
    {
        public LoginDialog()
        {
            InitializeComponent();
            
            
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;
            sender.IsPrimaryButtonEnabled = false;
            _of_LoginLoader.ShowLoader();

            // 模拟操作
            await Task.Delay(750);

            if (bool.TrueString == "True")
            {
                AppShareHelper.HasDialog = false;
                sender.Hide();

                var request = new OpenFrp.Core.Libraries.Protobuf.RequestBase()
                {
                    Action = Core.Libraries.Protobuf.RequestType.ClientTest
                };
                

                var response = await AppShareHelper.PipeClient.Request(request);
                MessageBox.Show(response.Message);

                return;
            }

            
            _of_LoginLoader.ShowContent();
            sender.IsPrimaryButtonEnabled = false;



        }
    }
}
