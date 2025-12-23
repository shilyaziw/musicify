using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Musicify.Core.ViewModels;

/// <summary>
/// ViewModel 基类
/// 实现 INotifyPropertyChanged 接口,提供属性变化通知
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    /// <summary>
    /// 属性变化事件
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 触发属性变化通知
    /// </summary>
    /// <param name="propertyName">属性名称 (自动填充)</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 设置属性值并触发变化通知
    /// </summary>
    /// <typeparam name="T">属性类型</typeparam>
    /// <param name="field">字段引用</param>
    /// <param name="value">新值</param>
    /// <param name="propertyName">属性名称 (自动填充)</param>
    /// <returns>如果值发生变化返回 true,否则返回 false</returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// 触发多个属性的变化通知
    /// </summary>
    /// <param name="propertyNames">属性名称列表</param>
    protected void OnPropertiesChanged(params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            OnPropertyChanged(propertyName);
        }
    }
}
