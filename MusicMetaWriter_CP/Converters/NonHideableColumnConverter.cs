using Avalonia.Data.Converters;
using System;
using System.Globalization;
using System.Linq;

namespace MusicMetaWriter_CP.Converters
{
    public class NonHideableColumnConverter : IValueConverter
    {
        private static readonly string[] NonHideableHeaders = { "#", "path", "reset", "track_name" };

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string header)
            {
                return !NonHideableHeaders.Contains(header.ToLower().Replace(" ", "_"));
            }
            return true; // default: enabled
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}