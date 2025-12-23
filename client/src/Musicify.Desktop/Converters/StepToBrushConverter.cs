using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Musicify.Desktop.Converters;

/// <summary>
/// 将步骤号转换为背景色（用于进度条）
/// 如果当前步骤 >= 参数步骤，返回激活色，否则返回灰色
/// </summary>
public class StepToBrushConverter : IValueConverter
{
    public static readonly StepToBrushConverter Instance = new();

    private static readonly SolidColorBrush ActiveBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246)); // #3B82F6
    private static readonly SolidColorBrush InactiveBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235)); // #E5E7EB

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

