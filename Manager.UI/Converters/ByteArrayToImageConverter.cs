using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Manager.UI.Converters;

public class ByteArrayToImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not byte[] bytes || bytes.Length == 0) 
            return null;
        var ms = new System.IO.MemoryStream(bytes);
        ms.Position = 0;
        return new Avalonia.Media.Imaging.Bitmap(ms);

    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}