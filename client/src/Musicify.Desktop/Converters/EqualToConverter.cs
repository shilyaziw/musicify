using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Musicify.Desktop.Converters;

/// <summary>
/// 判断值是否等于参数
/// 用于控制 UI 元素的可见性
/// </summary>
public class EqualToConverter : IValueConverter
{
    public static readonly EqualToConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        // 尝试将参数转换为与值相同的类型
        try
        {
            if (value is int intValue && int.TryParse(parameter.ToString(), out int intParam))
            {
                return intValue == intParam;
            }

            // 字符串比较
            return value.ToString() == parameter.ToString();
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

