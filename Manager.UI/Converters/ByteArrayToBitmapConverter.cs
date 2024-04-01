using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace Manager.UI.Converters;

public class ByteArrayToBitmapConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is byte[] bytes)
        {
            using var ms = new MemoryStream(bytes);
            ms.Position = 0;
            var desiredHeight = parameter is int height ? height : 150;
            return Bitmap.DecodeToHeight(ms, desiredHeight);
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}