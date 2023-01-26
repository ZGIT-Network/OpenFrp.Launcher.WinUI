using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ModernWpf.Controls.Primitives;
using OpenFrp.Launcher.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenFrp.Launcher.ViewModels
{
    public partial class SettingModel : ObservableObject
    {

        public int ThemeSet
        {
            get => (int)Core.Helper.ConfigHelper.Instance.ThemeSet;
            set => Core.Helper.ConfigHelper.Instance.ThemeSet = (ElementTheme)value;
        }

        public int BackdropSet
        {
            get => (int)Core.Helper.ConfigHelper.Instance.BackdropSet - 1;
            set => Core.Helper.ConfigHelper.Instance.BackdropSet = (BackdropType)value + 1;
        }

        /// <summary>
        /// 登录 / 个人信息
        /// </summary>
        [RelayCommand]
        async void AccountInfo()
        {
            // 如果未登录
            if (!false)
            {
                if (!AppShareHelper.HasDialog)
                {
                    var dialog = new Controls.LoginDialog();
                    await dialog.ShowAsync();
                }
            }
        }
    }
}
