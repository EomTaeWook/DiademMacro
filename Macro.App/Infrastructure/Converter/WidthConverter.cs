using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia.Data.Converters;

namespace Macro.Infrastructure.Converter
{
    public class WidthConverter : IValueConverter
    {
        private readonly string _widthPattern = @"^[0-9]*[.]\d{1,2}\*$|^[0-9]\*$";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double == false)
            {
                return value;
            }
            if (Regex.IsMatch(parameter.ToString(), _widthPattern) == false)
            {
                return parameter;
            }
            double sourceWidth = System.Convert.ToDouble(value);
            if (double.IsNaN(sourceWidth) == true)
            {
                sourceWidth = 0;
            }
            string parameterText = parameter.ToString();
            if (double.TryParse(parameterText.Substring(0, parameterText.Length - 1), out double width) == false)
            {
                return null;
            }
            return (int)Math.Truncate(sourceWidth * width);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(value, targetType, parameter, culture);
        }
    }
}
