using Avalonia.Controls;
using Avalonia.Data.Converters;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Macro.Infrastructure.Converter
{
    public class DependencyObjectWidthConverter : IValueConverter
    {
        private readonly string _widthPattern = @"^[0-9]*[.]\d{1,2}\*$|^[0-9]\*$";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Control control = value as Control;
            if (control == null)
            {
                return null;
            }
            if (Regex.IsMatch(parameter.ToString(), _widthPattern) == false)
            {
                return parameter;
            }
            double controlWidth = control.Width;
            if (double.IsNaN(controlWidth) == true)
            {
                controlWidth = 0;
            }
            string parameterText = parameter.ToString();
            if (double.TryParse(parameterText.Substring(0, parameterText.Length - 1), out double width) == false)
            {
                return null;
            }
            return (int)Math.Truncate(controlWidth * width);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(value, targetType, parameter, culture);
        }
    }
}
