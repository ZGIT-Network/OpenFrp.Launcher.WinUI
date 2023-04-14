using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using ModernWpf.Controls.Primitives;
using OpenFrp.Core.Helper;
using OpenFrp.Core.Libraries.Api;
using OpenFrp.Launcher.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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

        private CancellationTokenSource _cancellationTokenSource = new();

        private RoutedEventHandler handle = delegate { };

        /// <summary>
        /// 点击了登录按钮
        /// </summary>
        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            OfApp_Output_InfoBar.ActionButton.Click -= handle;
            args.Cancel = true;
            sender.IsPrimaryButtonEnabled = false;
            _of_LoginLoader.ShowLoader();
            OfApp_Output_InfoBar.ActionButton.Visibility = Visibility.Collapsed;



            // 模拟操作
            var loginResult = await AppShareHelper.LoginAndGetUserInfo(OfApp_Input_UserName.Text, OfApp_Input_Password.Password,_cancellationTokenSource.Token);
            // 如果 API 返回成功,那么接下去发给服务端。
            if (loginResult.Success)
            {
                // 如果取消了 那么清除后返回
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    ApiRequest.ClearAuth();
                }
                sender.Hide();
                return;
            }
            // 失败的情况

            if (loginResult.Exception is not null)
            {
                handle = (sender, args) => MessageBox.Show(loginResult.Exception.ToString());
                OfApp_Output_InfoBar.ActionButton.Click += handle;
                OfApp_Output_InfoBar.ActionButton.Visibility = Visibility.Visible;
            }
            ShowException(loginResult.Message);

            // 显示弹窗

            void ShowException(string reason)
            {
                OfApp_Output_InfoBar.IsOpen = true;
                OfApp_Output_InfoBar.Message = reason;
                _of_LoginLoader.ShowContent();
                sender.IsPrimaryButtonEnabled = true;
            }

        }

        /// <summary>
        /// 点击了取消按钮
        /// </summary>
        private void ContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) => _cancellationTokenSource.Cancel();


    }
}
