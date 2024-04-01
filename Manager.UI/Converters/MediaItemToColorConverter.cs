using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Manager.Shared.Entities;

namespace Manager.UI.Converters;

public class MediaItemToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not MediaItem mi) 
            return null;
        return mi switch
        {
            AudioItem => Brush.Parse("#FFBBBB"),
            _ => null
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 