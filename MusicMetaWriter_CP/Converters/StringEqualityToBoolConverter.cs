using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace MusicMetaWriter_CP.Converters
{
    public class StringEqualityToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string selected && parameter is string expected)
            {
                return string.Equals(selected, expected, StringComparison.OrdinalIgnoreCase);
                // or use Ordinal if you want case-sensitive:
                // return selected == expected;
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Usually not needed for IsEnabled → return Avalonia.Data.BindingNotification.Unset
            return Avalonia.Data.BindingNotification.UnsetValue;
        }
    }
}