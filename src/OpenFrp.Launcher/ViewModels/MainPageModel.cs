using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OpenFrp.Launcher.ViewModels
{
    public partial class MainPageModel : ObservableObject
    {
        [ObservableProperty]
        public bool hasDeamonProcess;

        [RelayCommand]
        void AccountInfo()
        {
            MessageBox.Show("www");
        }
    }
}
