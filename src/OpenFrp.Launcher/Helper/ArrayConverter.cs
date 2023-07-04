using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace OpenFrp.Launcher.Helper
{
    public class ArrayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Array array)
            {
                if (array.Length != 0)
                {
                    StringBuilder builder = new StringBuilder();
                    foreach (var item in array)
                    {
                        builder.Append($"{item},");
                    }
                    return builder.ToString().Remove(builder.Length - 1, 1);
                }
                else
                {
                    return "";
                }
            }
            else throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return str.Split(',');
            }
            else
            {
                return new string[0];   
            }
        }
    }
}
