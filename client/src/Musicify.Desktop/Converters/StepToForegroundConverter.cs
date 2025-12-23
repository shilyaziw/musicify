using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Musicify.Desktop.Converters;

/// <summary>
/// 将步骤号转换为前景色（用于文本）
/// 如果当前步骤 >= 参数步骤，返回激活色，否则返回灰色
/// </summary>
public class StepToForegroundConverter : IValueConverter
{
    public static readonly StepToForegroundConverter Instance = new();

    private static readonly SolidColorBrush ActiveBrush = new SolidColorBrush(Color.FromRgb(31, 41, 55)); // #1F2937
    private static readonly SolidColorBrush InactiveBrush = new SolidColorBrush(Color.FromRgb(156, 163, 175)); // #9CA3AF

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int currentStep && parameter is string paramStr && int.TryParse(paramStr, out int targetStep))
        {
            return currentStep >= targetStep ? ActiveBrush : InactiveBrush;
        }

        return InactiveBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

