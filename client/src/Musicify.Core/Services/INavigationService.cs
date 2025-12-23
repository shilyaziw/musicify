namespace Musicify.Core.Services;

/// <summary>
/// 导航服务接口
/// 提供应用内页面/窗口导航功能
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// 导航到指定视图
    /// </summary>
    /// <param name="viewName">视图名称</param>
    /// <param name="parameter">导航参数</param>
    void NavigateTo(string viewName, object? parameter = null);

    /// <summary>
    /// 返回上一个视图
    /// </summary>
    /// <returns>如果成功返回返回 true,否则返回 false</returns>
    bool GoBack();

    /// <summary>
    /// 是否可以返回
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// 关闭当前窗口
    /// </summary>
    void CloseCurrentWindow();

    /// <summary>
    /// 显示对话框
    /// </summary>
    /// <param name="dialogName">对话框名称</param>
    /// <param name="parameter">对话框参数</param>
    /// <returns>对话框结果</returns>
    Task<object?> ShowDialogAsync(string dialogName, object? parameter = null);
}
