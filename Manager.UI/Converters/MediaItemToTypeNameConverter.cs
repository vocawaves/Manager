using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Manager.Shared.Entities;

namespace Manager.UI.Converters;

public class MediaItemToTypeNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || value is not MediaItem mediaItem)
        {
            return "Unknown";
        }
        
        return mediaItem switch
        {
            _ when mediaItem is VideoItem => "Video",
            _ when mediaItem is AudioItem => "Audio",
            _ when mediaItem is ImageItem => "Image",
            _ when mediaItem is SubtitleItem => "Subtitle",
            _ => "Unknown"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}