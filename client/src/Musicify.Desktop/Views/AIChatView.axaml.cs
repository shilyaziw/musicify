using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using Musicify.Core.Models;
using Musicify.Core.ViewModels;
using Musicify.Desktop;
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Input;

namespace Musicify.Desktop.Views;

public partial class AIChatView : UserControl
{
    private ScrollViewer? _messageScrollViewer;

    public AIChatView()
    {
        InitializeComponent();
        
        // 从 DI 容器获取 ViewModel
        var app = Application.Current as App;
        if (app?.Services != null)
        {
            var viewModel = app.Services.GetService<AIChatViewModel>();
            if (viewModel != null)
            {
                DataContext = viewModel;
                
                // 设置内容更新回调（用于流式响应时的滚动）
                viewModel.SetContentUpdateCallback(SmoothScrollToBottom);
                
                // 监听消息变化,自动滚动到底部
                viewModel.Messages.CollectionChanged += (s, e) =>
                {
                    ScrollToBottom();
                };
            }
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _messageScrollViewer = this.FindControl<ScrollViewer>("MessageScrollViewer");
    }

    private void ScrollToBottom()
    {
        if (_messageScrollViewer != null)
        {
            // 延迟滚动,确保 UI 已更新
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _messageScrollViewer.ScrollToEnd();
            }, Avalonia.Threading.DispatcherPriority.Background);
        }
    }
    
    /// <summary>
    /// 平滑滚动到底部（用于流式更新）
    /// </summary>
    private void SmoothScrollToBottom()
    {
        if (_messageScrollViewer != null)
        {
            // 使用较低优先级，避免频繁滚动影响性能
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    _messageScrollViewer.ScrollToEnd();
                }
                catch
                {
                    // 忽略滚动错误
                }
            }, Avalonia.Threading.DispatcherPriority.Background);
        }
    }
}

/// <summary>
/// 消息类型到图标的转换器
/// </summary>
public class MessageTypeToIconConverter : Avalonia.Data.Converters.IValueConverter
{
    public static MessageTypeToIconConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "User" => "👤",
            "AI" => "🤖",
            _ => "💬"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 消息类型到标签的转换器
/// </summary>
public class MessageTypeToLabelConverter : Avalonia.Data.Converters.IValueConverter
{
    public static MessageTypeToLabelConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "User" => "用户",
            "AI" => "AI 助手",
            _ => "未知"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 字符串到画刷的转换器 (用于消息背景色)
/// </summary>
public class StringToBrushConverter : Avalonia.Data.Converters.IValueConverter
{
    public static StringToBrushConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "User" => new SolidColorBrush(Color.FromRgb(59, 130, 246)), // 蓝色
            "AI" => new SolidColorBrush(Color.FromRgb(249, 250, 251)), // 浅灰色
            _ => new SolidColorBrush(Color.FromRgb(255, 255, 255)) // 白色
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 命令可执行性转换器
/// </summary>
public class CommandCanExecuteConverter : Avalonia.Data.Converters.IValueConverter
{
    public static CommandCanExecuteConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ICommand command)
        {
            return command.CanExecute(null);
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 创作模式到标签的转换器
/// </summary>
public class CreationModeToLabelConverter : Avalonia.Data.Converters.IValueConverter
{
    public static CreationModeToLabelConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "coach" => "引导模式",
            "express" => "快速模式",
            "hybrid" => "混合模式",
            _ => value?.ToString() ?? ""
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 非空值转换器
/// </summary>
public class IsNotNullConverter : Avalonia.Data.Converters.IValueConverter
{
    public static IsNotNullConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 布尔值到图标转换器
/// </summary>
public class BoolToIconConverter : Avalonia.Data.Converters.IValueConverter
{
    public static BoolToIconConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? "✅" : "❌";
        }
        return "❌";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 是否有歌词到状态文本转换器
/// </summary>
public class HasLyricsToStatusConverter : Avalonia.Data.Converters.IValueConverter
{
    public static HasLyricsToStatusConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? "已创建" : "未创建";
        }
        return "未知";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 是否有 MIDI 到状态文本转换器
/// </summary>
public class HasMidiToStatusConverter : Avalonia.Data.Converters.IValueConverter
{
    public static HasMidiToStatusConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? "已上传" : "未上传";
        }
        return "未知";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 字典到字符串转换器
/// </summary>
public class DictionaryToStringConverter : Avalonia.Data.Converters.IValueConverter
{
    public static DictionaryToStringConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Dictionary<string, float> dict)
        {
            if (dict.Count == 0)
                return "无";
            
            return string.Join(", ", dict.Select(kvp => $"{kvp.Key}: {kvp.Value:P0}"));
        }
        return "无";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 布尔值取反转换器
/// </summary>
public class BoolToInverseConverter : Avalonia.Data.Converters.IValueConverter
{
    public static BoolToInverseConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 列表包含转换器（用于平台选择）
/// </summary>
public class ListContainsConverter : Avalonia.Data.Converters.IValueConverter
{
    public static ListContainsConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is List<string> list && parameter is string item)
        {
            return list.Contains(item);
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // ConvertBack 用于双向绑定，但这里我们通过事件处理来更新列表
        // 返回 null 表示不进行反向转换
        return null;
    }
}

/// <summary>
/// 字符串相等转换器（用于单选按钮）
/// </summary>
public class StringEqualsConverter : Avalonia.Data.Converters.IValueConverter
{
    public static StringEqualsConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str && parameter is string param)
        {
            return str == param;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isChecked && isChecked && parameter is string param)
        {
            return param;
        }
        return null;
    }
}

/// <summary>
/// 歌词内容到预览文本转换器
/// </summary>
public class LyricsContentToPreviewConverter : Avalonia.Data.Converters.IValueConverter
{
    public static LyricsContentToPreviewConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Musicify.Core.Models.LyricsContent lyrics)
        {
            return lyrics.ToFormattedText();
        }
        return "暂无歌词内容";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 音符范围到字符串转换器
/// </summary>
public class NoteRangeToStringConverter : Avalonia.Data.Converters.IValueConverter
{
    public static NoteRangeToStringConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // NoteRange 是 (int Min, int Max) 元组
        // 使用反射或类型检查来访问
        if (value != null)
        {
            var type = value.GetType();
            if (type.IsValueType && type.IsGenericType)
            {
                // 尝试获取 Min 和 Max 属性
                var minProp = type.GetProperty("Min");
                var maxProp = type.GetProperty("Max");
                
                if (minProp != null && maxProp != null)
                {
                    var min = minProp.GetValue(value);
                    var max = maxProp.GetValue(value);
                    return $"{min} - {max}";
                }
            }
            
            // 尝试直接转换为字符串
            return value.ToString();
        }
        return "未知";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
