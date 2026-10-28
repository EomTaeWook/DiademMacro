using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Macro.Infrastructure.Converter
{
    public class BooleanToValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return false;
            }
            return value.ToString().Equals(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (System.Convert.ToBoolean(value) == true)
            {
                return Enum.Parse(targetType, parameter.ToString(), true);
            }

            return BindingOperations.DoNothing;
        }
    }
}
