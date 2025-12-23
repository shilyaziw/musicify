using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Musicify.Core.Services;
using System.Linq;

namespace Musicify.Desktop.Services;

/// <summary>
/// 文件对话框服务实现（AvaloniaUI）
/// </summary>
public class FileDialogService : IFileDialogService
{
    private Window? GetMainWindow()
    {
        return Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
    }

    /// <summary>
    /// 显示打开文件对话框
    /// </summary>
    public async Task<string?> ShowOpenFileDialogAsync(
        string? title = null,
        string? filters = null,
        string? initialDirectory = null)
    {
        var window = GetMainWindow();
        if (window == null)
            return null;

        var storageProvider = window.StorageProvider;
        
        var filePickerOptions = new FilePickerOpenOptions
        {
            Title = title ?? "选择文件",
            AllowMultiple = false
        };

        // 设置初始目录
        if (!string.IsNullOrWhiteSpace(initialDirectory) && 
            System.IO.Directory.Exists(initialDirectory))
        {
            try
            {
                var folder = await storageProvider.TryGetFolderFromPathAsync(
                    new Uri(initialDirectory));
                if (folder != null)
                {
                    filePickerOptions.SuggestedStartLocation = folder;
                }
            }
            catch
            {
                // 忽略错误，使用默认位置
            }
        }

        // 设置文件过滤器
        if (!string.IsNullOrWhiteSpace(filters))
        {
            var fileTypeFilters = ParseFilters(filters);
            filePickerOptions.FileTypeFilter = fileTypeFilters;
        }

        var files = await storageProvider.OpenFilePickerAsync(filePickerOptions);
        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }

    /// <summary>
    /// 显示保存文件对话框
    /// </summary>
    public async Task<string?> ShowSaveFileDialogAsync(
        string? title = null,
        string? defaultFileName = null,
        string? filters = null,
        string? initialDirectory = null)
    {
        var window = GetMainWindow();
        if (window == null)
            return null;

        var storageProvider = window.StorageProvider;
        
        var filePickerOptions = new FilePickerSaveOptions
        {
            Title = title ?? "保存文件",
            SuggestedFileName = defaultFileName
        };

        // 设置初始目录
        if (!string.IsNullOrWhiteSpace(initialDirectory) && 
            System.IO.Directory.Exists(initialDirectory))
        {
            try
            {
                var folder = await storageProvider.TryGetFolderFromPathAsync(
                    new Uri(initialDirectory));
                if (folder != null)
                {
                    filePickerOptions.SuggestedStartLocation = folder;
                }
            }
            catch
            {
                // 忽略错误，使用默认位置
            }
        }

        // 设置文件过滤器
        if (!string.IsNullOrWhiteSpace(filters))
        {
            var fileTypeFilters = ParseFilters(filters);
            filePickerOptions.FileTypeChoices = fileTypeFilters;
        }

        var file = await storageProvider.SaveFilePickerAsync(filePickerOptions);
        return file?.Path.LocalPath;
    }

    /// <summary>
    /// 显示选择文件夹对话框
    /// </summary>
    public async Task<string?> ShowOpenFolderDialogAsync(
        string? title = null,
        string? initialDirectory = null)
    {
        var window = GetMainWindow();
        if (window == null)
            return null;

        var storageProvider = window.StorageProvider;
        
        var folderPickerOptions = new FolderPickerOpenOptions
        {
            Title = title ?? "选择文件夹",
            AllowMultiple = false
        };

        // 设置初始目录
        if (!string.IsNullOrWhiteSpace(initialDirectory) && 
            System.IO.Directory.Exists(initialDirectory))
        {
            try
            {
                var folder = await storageProvider.TryGetFolderFromPathAsync(
                    new Uri(initialDirectory));
                if (folder != null)
                {
                    folderPickerOptions.SuggestedStartLocation = folder;
                }
            }
            catch
            {
                // 忽略错误，使用默认位置
            }
        }

        var folders = await storageProvider.OpenFolderPickerAsync(folderPickerOptions);
        return folders.Count > 0 ? folders[0].Path.LocalPath : null;
    }

    /// <summary>
    /// 解析文件过滤器字符串
    /// 格式：描述|扩展名，如 "文本文件|*.txt" 或 "MIDI 文件|*.mid;*.midi"
    /// </summary>
    private static List<FilePickerFileType> ParseFilters(string filters)
    {
        var result = new List<FilePickerFileType>();
        var parts = filters.Split('|');
        
        if (parts.Length >= 2)
        {
            var description = parts[0].Trim();
            var extensions = parts[1]
                .Split(';')
                .Select(ext => ext.Trim().TrimStart('*'))
                .Where(ext => !string.IsNullOrEmpty(ext))
                .ToList();
            
            if (extensions.Count > 0)
            {
                result.Add(new FilePickerFileType(description)
                {
                    Patterns = extensions.Select(ext => $"*{ext}").ToList()
                });
            }
        }
        
        return result;
    }
}

