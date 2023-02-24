using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using OpenFrp.Core.Helper;

namespace OpenFrp.Launcher.Helper
{
    public class FontConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            try
            {
                return new FontFamily(ConfigHelper.Instance.FontSet.FontFamily);
            }
            catch
            {

            }
            return new FontFamily("微软雅黑");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FontFamily fam)
            {
                return fam.ToString();
            }
            return "微软雅黑";
        }
    }
}
