using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Musicify.Desktop.Converters;

/// <summary>
/// 判断值是否小于参数
/// 用于控制 UI 元素的可见性
/// </summary>
public class LessThanConverter : IValueConverter
{
    public static readonly LessThanConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
        {
            return false;
        }

        // 尝试数值比较
        try
        {
            if (value is int intValue && int.TryParse(parameter.ToString(), out int intParam))
            {
                return intValue < intParam;
            }

            if (value is double doubleValue && double.TryParse(parameter.ToString(), out double doubleParam))
            {
                return doubleValue < doubleParam;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

