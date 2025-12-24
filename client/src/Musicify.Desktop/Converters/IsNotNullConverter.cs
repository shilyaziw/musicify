using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Musicify.Desktop.Converters;

/// <summary>
/// 非空值转换器
/// </summary>
public class IsNotNullConverter : IValueConverter
{
    public static readonly IsNotNullConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

