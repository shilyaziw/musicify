using System.IO;
using System.Windows.Input;
using Musicify.Core.Abstractions;
using Musicify.Core.Models;
using Musicify.Core.Services;

namespace Musicify.Core.ViewModels;

/// <summary>
/// 导出界面 ViewModel
/// </summary>
public class ExportViewModel : ViewModelBase
{
    private readonly IExportService _exportService;
    private readonly IFileSystem _fileSystem;
    private readonly IFileDialogService? _fileDialogService;
    
    private ProjectConfig? _currentProject;
    private LyricsContent? _lyricsContent;
    private string _selectedFormat = "txt";
    private string? _exportPath;
    private bool _isExporting;
    private string? _errorMessage;
    private string? _successMessage;

    public ExportViewModel(
        IExportService exportService,
        IFileSystem fileSystem,
        IFileDialogService? fileDialogService = null)
    {
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _fileDialogService = fileDialogService;

        // 初始化命令
        SelectExportPathCommand = new RelayCommand(SelectExportPath);
        ExportCommand = new AsyncRelayCommand(ExportAsync, CanExport);
        
        // 初始化格式选项
        ExportFormats = new List<ExportFormat>
        {
            new("txt", "文本文件 (.txt)", "纯文本格式，兼容性好"),
            new("json", "JSON 文件 (.json)", "结构化数据，便于程序处理"),
            new("md", "Markdown 文件 (.md)", "支持格式化，适合文档"),
            new("lrc", "LRC 歌词文件 (.lrc)", "歌词同步格式，支持时间戳")
        };
    }

    #region 属性

    /// <summary>
    /// 当前项目配置
    /// </summary>
    public ProjectConfig? CurrentProject
    {
        get => _currentProject;
        set => SetProperty(ref _currentProject, value);
    }

    /// <summary>
    /// 歌词内容
    /// </summary>
    public LyricsContent? LyricsContent
    {
        get => _lyricsContent;
        set => SetProperty(ref _lyricsContent, value);
    }

    /// <summary>
    /// 选中的导出格式
    /// </summary>
    public string SelectedFormat
    {
        get => _selectedFormat;
        set => SetProperty(ref _selectedFormat, value);
    }

    /// <summary>
    /// 导出路径
    /// </summary>
    public string? ExportPath
    {
        get => _exportPath;
        set => SetProperty(ref _exportPath, value);
    }

    /// <summary>
    /// 是否正在导出
    /// </summary>
    public bool IsExporting
    {
        get => _isExporting;
        private set => SetProperty(ref _isExporting, value);
    }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            SetProperty(ref _errorMessage, value);
            if (!string.IsNullOrEmpty(value))
            {
                SuccessMessage = null;
            }
        }
    }

    /// <summary>
    /// 成功消息
    /// </summary>
    public string? SuccessMessage
    {
        get => _successMessage;
        private set
        {
            SetProperty(ref _successMessage, value);
            if (!string.IsNullOrEmpty(value))
            {
                ErrorMessage = null;
            }
        }
    }

    /// <summary>
    /// 导出格式选项
    /// </summary>
    public List<ExportFormat> ExportFormats { get; }

    #endregion

    #region 命令

    /// <summary>
    /// 选择导出路径命令
    /// </summary>
    public ICommand SelectExportPathCommand { get; }

    /// <summary>
    /// 导出命令
    /// </summary>
    public ICommand ExportCommand { get; }

    #endregion

    #region 公共方法

    /// <summary>
    /// 设置当前项目
    /// </summary>
    public async Task SetProjectAsync(ProjectConfig project)
    {
        CurrentProject = project;
        await LoadLyricsAsync();
    }

    /// <summary>
    /// 设置歌词内容
    /// </summary>
    public void SetLyricsContent(LyricsContent lyrics)
    {
        LyricsContent = lyrics;
    }

    #endregion

    #region 命令实现

    /// <summary>
    /// 加载歌词
    /// </summary>
    private async Task LoadLyricsAsync()
    {
        if (CurrentProject == null || string.IsNullOrWhiteSpace(CurrentProject.ProjectPath))
        {
            LyricsContent = null;
            return;
        }

        try
        {
            var lyricsPath = Path.Combine(CurrentProject.ProjectPath, "lyrics.txt");
            if (_fileSystem.FileExists(lyricsPath))
            {
                var content = await _fileSystem.ReadAllTextAsync(lyricsPath);
                LyricsContent = LyricsContent.FromText(content, CurrentProject.Name);
            }
            else
            {
                LyricsContent = null;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"加载歌词失败: {ex.Message}";
            LyricsContent = null;
        }
    }

    /// <summary>
    /// 选择导出路径
    /// </summary>
    private async void SelectExportPath()
    {
        if (CurrentProject == null || string.IsNullOrWhiteSpace(CurrentProject.ProjectPath))
        {
            ErrorMessage = "请先打开项目";
            return;
        }

        // 构建文件过滤器
        var filter = SelectedFormat.ToLower() switch
        {
            "txt" => "文本文件|*.txt",
            "json" => "JSON 文件|*.json",
            "md" => "Markdown 文件|*.md",
            "lrc" => "LRC 歌词文件|*.lrc",
            _ => "所有文件|*.*"
        };

        var defaultFileName = $"{CurrentProject.Name}_歌词.{SelectedFormat}";
        
        // 如果有文件对话框服务，使用它
        if (_fileDialogService != null)
        {
            var selectedPath = await _fileDialogService.ShowSaveFileDialogAsync(
                title: "导出歌词",
                defaultFileName: defaultFileName,
                filters: filter,
                initialDirectory: CurrentProject.ProjectPath);
            
            if (!string.IsNullOrWhiteSpace(selectedPath))
            {
                ExportPath = selectedPath;
            }
        }
        else
        {
            // 回退到默认路径
            ExportPath = Path.Combine(CurrentProject.ProjectPath, defaultFileName);
        }
    }

    /// <summary>
    /// 导出
    /// </summary>
    private async Task ExportAsync()
    {
        if (LyricsContent == null)
        {
            ErrorMessage = "没有可导出的歌词内容";
            return;
        }

        if (string.IsNullOrWhiteSpace(ExportPath))
        {
            ErrorMessage = "请选择导出路径";
            return;
        }

        try
        {
            IsExporting = true;
            ErrorMessage = null;
            SuccessMessage = null;

            // 根据格式导出
            switch (SelectedFormat.ToLower())
            {
                case "txt":
                    await _exportService.ExportToTextAsync(LyricsContent, ExportPath);
                    break;
                case "json":
                    await _exportService.ExportToJsonAsync(LyricsContent, ExportPath);
                    break;
                case "md":
                    await _exportService.ExportToMarkdownAsync(LyricsContent, ExportPath);
                    break;
                case "lrc":
                    await _exportService.ExportToLrcAsync(LyricsContent, ExportPath);
                    break;
                default:
                    throw new NotSupportedException($"不支持的导出格式: {SelectedFormat}");
            }

            SuccessMessage = $"导出成功: {ExportPath}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"导出失败: {ex.Message}";
        }
        finally
        {
            IsExporting = false;
        }
    }

    /// <summary>
    /// 是否可以导出
    /// </summary>
    private bool CanExport()
    {
        return LyricsContent != null && !string.IsNullOrWhiteSpace(ExportPath) && !IsExporting;
    }

    #endregion
}

/// <summary>
/// 导出格式信息
/// </summary>
public record ExportFormat(
    string Id,
    string Name,
    string Description
);

