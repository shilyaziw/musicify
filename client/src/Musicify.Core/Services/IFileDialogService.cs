namespace Musicify.Core.Services;

/// <summary>
/// 文件对话框服务接口
/// </summary>
public interface IFileDialogService
{
    /// <summary>
    /// 显示打开文件对话框
    /// </summary>
    /// <param name="title">对话框标题</param>
    /// <param name="filters">文件过滤器（格式：描述|扩展名，如 "MIDI 文件|*.mid;*.midi"）</param>
    /// <param name="initialDirectory">初始目录</param>
    /// <returns>选中的文件路径，如果取消则返回 null</returns>
    Task<string?> ShowOpenFileDialogAsync(
        string? title = null,
        string? filters = null,
        string? initialDirectory = null);

    /// <summary>
    /// 显示保存文件对话框
    /// </summary>
    /// <param name="title">对话框标题</param>
    /// <param name="defaultFileName">默认文件名</param>
    /// <param name="filters">文件过滤器（格式：描述|扩展名，如 "文本文件|*.txt"）</param>
    /// <param name="initialDirectory">初始目录</param>
    /// <returns>保存的文件路径，如果取消则返回 null</returns>
    Task<string?> ShowSaveFileDialogAsync(
        string? title = null,
        string? defaultFileName = null,
        string? filters = null,
        string? initialDirectory = null);

    /// <summary>
    /// 显示选择文件夹对话框
    /// </summary>
    /// <param name="title">对话框标题</param>
    /// <param name="initialDirectory">初始目录</param>
    /// <returns>选中的文件夹路径，如果取消则返回 null</returns>
    Task<string?> ShowOpenFolderDialogAsync(
        string? title = null,
        string? initialDirectory = null);
}

