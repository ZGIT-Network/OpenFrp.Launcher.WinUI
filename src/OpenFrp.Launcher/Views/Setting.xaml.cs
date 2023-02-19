using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using OpenFrp.Core.Helper;

namespace OpenFrp.Launcher.Views
{
    /// <summary>
    /// Setting.xaml 的交互逻辑
    /// </summary>
    public partial class Setting : Controls.ViewPage
    {
        public Setting()
        {
            InitializeComponent();
            this.Unloaded += async (s, e) =>
            {
                await Helper.AppShareHelper.PipeClient.Request(new Core.Libraries.Protobuf.RequestBase()
                {
                    Action = Core.Libraries.Protobuf.RequestType.ClientPushConfig,
                    ConfigJson = ConfigHelper.Instance.JSON()
                });
            };
        }

        public ViewModels.SettingModel Model
        {
            get => (ViewModels.SettingModel)DataContext;
        }


        private void FontFamilys_ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            var list = Fonts.SystemFontFamilies.ToArray();
            if (sender is ComboBox box)
            {
                var lang = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);
                if (list != null)
                {
                    var items = new List<ComboBoxItem>();

                    int count = 0;
                    for (int i = 0; i < list.Length; i++)
                    {
                        string fontName = list[i].FamilyNames.ContainsKey(lang) ? list[i].FamilyNames[lang] : list[i].ToString();
                        if (list[i].ToString() == Model.SettingInstance.FontSet.FontFamily) count = i;
                        items.Add(new ComboBoxItem()
                        {
                            Content = fontName,
                            FontFamily = list[i],
                            IsSelected = (fontName == Model.SettingInstance.FontSet.FontFamily)
                        });
                    }
                    box.ItemsSource = items;
                    box.SelectedIndex = count;
                }
                box.SelectionChanged += (sender, args) =>
                {
                    if (box.SelectedValue is ComboBoxItem item)
                    {
                        if (item.FontFamily.FamilyNames.ContainsKey(lang))
                        {
                            Model.SettingInstance.FontSet.FontFamily = item.FontFamily.FamilyNames[lang];
                        }
                        else
                        {
                            Model.SettingInstance.FontSet.FontFamily = item.FontFamily.ToString();
                        }
                        
                    }
                };
            }
        }

        private void ToggleSwitch_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
