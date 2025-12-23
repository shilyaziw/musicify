using System.Text.RegularExpressions;
using System.Windows.Input;
using Musicify.Core.Abstractions;
using Musicify.Core.Models;
using Musicify.Core.Services;

namespace Musicify.Core.ViewModels;

/// <summary>
/// 歌词编辑器 ViewModel
/// </summary>
public class LyricsEditorViewModel : ViewModelBase
{
    private readonly IProjectService _projectService;
    private readonly IFileSystem _fileSystem;
    
    private ProjectConfig? _currentProject;
    private string _lyricsText = string.Empty;
    private int _wordCount;
    private int _sectionCount;
    private int _lineCount;
    private bool _isModified;
    private bool _showPreview;
    private string? _errorMessage;
    private System.Timers.Timer? _autoSaveTimer;
    
    // 撤销/重做历史
    private readonly Stack<string> _undoStack = new();
    private readonly Stack<string> _redoStack = new();
    private string _lastSavedText = string.Empty;

    public LyricsEditorViewModel(
        IProjectService projectService,
        IFileSystem fileSystem)
    {
        _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

        // 初始化命令
        SaveLyricsCommand = new AsyncRelayCommand(SaveLyricsAsync, CanSave);
        FormatLyricsCommand = new RelayCommand(FormatLyrics);
        TogglePreviewCommand = new RelayCommand(TogglePreview);
        LoadLyricsCommand = new AsyncRelayCommand(LoadLyricsAsync);

        // 监听歌词文本变化
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(LyricsText))
            {
                UpdateStatistics();
                ScheduleAutoSave();
            }
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
    public string LyricsText
    {
        get => _lyricsText;
        set
        {
            if (SetProperty(ref _lyricsText, value))
            {
                IsModified = true;
            }
        }
    }

    /// <summary>
    /// 字数统计
    /// </summary>
    public int WordCount
    {
        get => _wordCount;
        private set => SetProperty(ref _wordCount, value);
    }

    /// <summary>
    /// 段落数量
    /// </summary>
    public int SectionCount
    {
        get => _sectionCount;
        private set => SetProperty(ref _sectionCount, value);
    }

    /// <summary>
    /// 行数
    /// </summary>
    public int LineCount
    {
        get => _lineCount;
        private set => SetProperty(ref _lineCount, value);
    }

    /// <summary>
    /// 是否已修改 (未保存)
    /// </summary>
    public bool IsModified
    {
        get => _isModified;
        private set => SetProperty(ref _isModified, value);
    }

    /// <summary>
    /// 是否显示预览
    /// </summary>
    public bool ShowPreview
    {
        get => _showPreview;
        set => SetProperty(ref _showPreview, value);
    }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    #endregion

    #region 命令

    /// <summary>
    /// 保存歌词命令
    /// </summary>
    public ICommand SaveLyricsCommand { get; }

    /// <summary>
    /// 格式化歌词命令
    /// </summary>
    public ICommand FormatLyricsCommand { get; }

    /// <summary>
    /// 切换预览命令
    /// </summary>
    public ICommand TogglePreviewCommand { get; }

    /// <summary>
    /// 加载歌词命令
    /// </summary>
    public ICommand LoadLyricsCommand { get; }

    /// <summary>
    /// 撤销命令
    /// </summary>
    public ICommand UndoCommand { get; }

    /// <summary>
    /// 重做命令
    /// </summary>
    public ICommand RedoCommand { get; }

    #endregion

    #region 公共方法

    /// <summary>
    /// 设置当前项目并加载歌词
    /// </summary>
    public async Task SetProjectAsync(ProjectConfig project)
    {
        CurrentProject = project;
        await LoadLyricsAsync();
    }

    #endregion

    #region 命令实现

    /// <summary>
    /// 保存歌词
    /// </summary>
    private async Task SaveLyricsAsync()
    {
        if (CurrentProject == null || string.IsNullOrWhiteSpace(CurrentProject.ProjectPath))
        {
            ErrorMessage = "请先打开项目";
            return;
        }

        try
        {
            ErrorMessage = null;
            var lyricsPath = Path.Combine(CurrentProject.ProjectPath, "lyrics.txt");
            
            // 确保目录存在
            var directory = Path.GetDirectoryName(lyricsPath);
            if (!string.IsNullOrEmpty(directory) && !_fileSystem.DirectoryExists(directory))
            {
                _fileSystem.CreateDirectory(directory);
            }
            
            await _fileSystem.WriteAllTextAsync(lyricsPath, LyricsText);
            IsModified = false;
            
            // 停止自动保存定时器
            _autoSaveTimer?.Stop();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"保存失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 是否可以保存
    /// </summary>
    private bool CanSave()
    {
        return CurrentProject != null && IsModified;
    }

    /// <summary>
    /// 格式化歌词
    /// </summary>
    private void FormatLyrics()
    {
        // 自动格式化段落标记
        // 确保段落标记格式正确: [Verse 1] 而不是 [verse 1] 或 [Verse1]
        var sectionPattern = new Regex(@"\[([^\]]+)\]", RegexOptions.IgnoreCase);
        var formatted = sectionPattern.Replace(LyricsText, match =>
        {
            var sectionName = match.Groups[1].Value.Trim();
            
            // 标准化段落名称
            sectionName = sectionName switch
            {
                var s when s.StartsWith("verse", StringComparison.OrdinalIgnoreCase) => 
                    "Verse " + (s.Length > 5 ? s.Substring(5).Trim() : "1"),
                var s when s.Equals("chorus", StringComparison.OrdinalIgnoreCase) => "Chorus",
                var s when s.Equals("bridge", StringComparison.OrdinalIgnoreCase) => "Bridge",
                var s when s.Equals("intro", StringComparison.OrdinalIgnoreCase) => "Intro",
                var s when s.Equals("outro", StringComparison.OrdinalIgnoreCase) => "Outro",
                var s when s.StartsWith("pre-chorus", StringComparison.OrdinalIgnoreCase) => "Pre-Chorus",
                _ => sectionName
            };
            
            return $"[{sectionName}]";
        });
        
        LyricsText = formatted;
    }

    /// <summary>
    /// 切换预览
    /// </summary>
    private void TogglePreview()
    {
        ShowPreview = !ShowPreview;
    }

    /// <summary>
    /// 加载歌词
    /// </summary>
    private async Task LoadLyricsAsync()
    {
        if (CurrentProject == null || string.IsNullOrWhiteSpace(CurrentProject.ProjectPath))
        {
            return;
        }

        try
        {
            ErrorMessage = null;
            var lyricsPath = Path.Combine(CurrentProject.ProjectPath, "lyrics.txt");
            
            if (_fileSystem.FileExists(lyricsPath))
            {
                LyricsText = await _fileSystem.ReadAllTextAsync(lyricsPath);
                IsModified = false;
            }
            else
            {
                // 如果文件不存在,初始化为空
                LyricsText = string.Empty;
                IsModified = false;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"加载失败: {ex.Message}";
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 更新统计信息
    /// </summary>
    private void UpdateStatistics()
    {
        if (string.IsNullOrEmpty(LyricsText))
        {
            WordCount = 0;
            SectionCount = 0;
            LineCount = 0;
            return;
        }

        var lines = LyricsText.Split('\n');
        LineCount = lines.Length;

        // 统计段落标记
        var sectionPattern = new Regex(@"\[.*?\]", RegexOptions.IgnoreCase);
        var sections = sectionPattern.Matches(LyricsText);
        SectionCount = sections.Count;

        // 统计字数 (排除标记和空行)
        var cleanText = LyricsText;
        cleanText = sectionPattern.Replace(cleanText, "");
        var nonEmptyLines = lines.Where(l => !string.IsNullOrWhiteSpace(l) && !sectionPattern.IsMatch(l));
        cleanText = string.Join("", nonEmptyLines);
        WordCount = cleanText.Length;
    }

    /// <summary>
    /// 安排自动保存
    /// </summary>
    private void ScheduleAutoSave()
    {
        _autoSaveTimer?.Stop();
        _autoSaveTimer?.Dispose();
        
        _autoSaveTimer = new System.Timers.Timer(3000); // 3 秒
        _autoSaveTimer.Elapsed += async (s, e) =>
        {
            if (CanSave())
            {
                await SaveLyricsAsync();
            }
        };
        _autoSaveTimer.AutoReset = false;
        _autoSaveTimer.Start();
    }

    #endregion
}

