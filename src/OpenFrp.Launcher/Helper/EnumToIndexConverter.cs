using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using OpenFrp.Core.Helper;

namespace OpenFrp.Launcher.Helper
{
    internal class EnumToIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Enum va)
            {
                foreach (var item in Enum.GetValues(va.GetType()))
                {
                    if (item.ToString() == va.ToString())
                    {
                        if (parameter is string nums)
                        {
                            return (int)item + int.Parse(nums);
                        }
                        return (int)item;
                    }
                }
            }
            throw new Exception();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int num)
            {
                return Enum.GetValues(targetType).GetValue(num);
            }
            throw new Exception();
        }
    }
}
