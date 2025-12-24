using System.IO;
using System.Linq;
using System.Windows.Input;
using Musicify.Core.Abstractions;
using Musicify.Core.Models;
using Musicify.Core.Services;

namespace Musicify.Core.ViewModels;

/// <summary>
/// 主编辑窗口 ViewModel
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    private readonly IProjectService _projectService;
    private readonly IFileSystem _fileSystem;
    private readonly INavigationService? _navigationService;

    private ProjectConfig? _currentProject;
    private string _currentView = "ProjectOverview";
    private ProjectSummary? _projectSummary;
    private bool _isLoading;
    private string? _errorMessage;

    /// <summary>
    /// 当前项目配置
    /// </summary>
    public ProjectConfig? CurrentProject
    {
        get => _currentProject;
        set => SetProperty(ref _currentProject, value);
    }

    /// <summary>
    /// 当前视图名称 (用于 ContentControl 切换)
    /// </summary>
    public string CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    /// <summary>
    /// 项目信息摘要
    /// </summary>
    public ProjectSummary? ProjectSummary
    {
        get => _projectSummary;
        set => SetProperty(ref _projectSummary, value);
    }

    /// <summary>
    /// 是否正在加载
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>
    /// 初始化 MainWindowViewModel 实例
    /// </summary>
    /// <param name="projectService">项目服务</param>
    /// <param name="fileSystem">文件系统服务</param>
    /// <param name="navigationService">导航服务（可选）</param>
    public MainWindowViewModel(
        IProjectService projectService,
        IFileSystem fileSystem,
        INavigationService? navigationService = null)
    {
        _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _navigationService = navigationService;

        // 初始化命令
        NavigateToLyricsEditorCommand = new RelayCommand(NavigateToLyricsEditor);
        NavigateToAIChatCommand = new RelayCommand(NavigateToAIChat);
        NavigateToMidiAnalysisCommand = new RelayCommand(NavigateToMidiAnalysis);
        NavigateToProjectSettingsCommand = new RelayCommand(NavigateToProjectSettings);
        NavigateToProjectOverviewCommand = new RelayCommand(NavigateToProjectOverview);
        NavigateToExportCommand = new RelayCommand(NavigateToExport);
        SaveProjectCommand = new AsyncRelayCommand(SaveProjectAsync);
    }

    #region 命令

    /// <summary>
    /// 导航到歌词编辑器命令
    /// </summary>
    public ICommand NavigateToLyricsEditorCommand { get; }

    /// <summary>
    /// 导航到 AI 对话界面命令
    /// </summary>
    public ICommand NavigateToAIChatCommand { get; }

    /// <summary>
    /// 导航到 MIDI 分析界面命令
    /// </summary>
    public ICommand NavigateToMidiAnalysisCommand { get; }

    /// <summary>
    /// 导航到项目设置命令
    /// </summary>
    public ICommand NavigateToProjectSettingsCommand { get; }

    /// <summary>
    /// 导航到项目概览命令
    /// </summary>
    public ICommand NavigateToProjectOverviewCommand { get; }

    /// <summary>
    /// 导航到导出界面命令
    /// </summary>
    public ICommand NavigateToExportCommand { get; }

    /// <summary>
    /// 保存项目命令
    /// </summary>
    public ICommand SaveProjectCommand { get; }

    #endregion

    /// <summary>
    /// 加载项目
    /// </summary>
    public async Task LoadProjectAsync(string projectPath)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var project = await _projectService.LoadProjectAsync(projectPath);
            CurrentProject = project;
            ProjectSummary = CreateProjectSummary(project);

            // 通知歌词编辑器加载项目 (如果已打开)
            // 这将在 View 层处理
        }
        catch (FileNotFoundException ex)
        {
            ErrorMessage = $"项目文件未找到: {ex.Message}";
        }
        catch (System.Text.Json.JsonException ex)
        {
            ErrorMessage = $"项目文件格式错误: {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"加载项目失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    #region 命令实现

    /// <summary>
    /// 导航到歌词编辑器
    /// </summary>
    private void NavigateToLyricsEditor()
    {
        CurrentView = "LyricsEditor";
    }

    /// <summary>
    /// 导航到 AI 对话界面
    /// </summary>
    private void NavigateToAIChat()
    {
        CurrentView = "AIChat";
    }

    /// <summary>
    /// 导航到 MIDI 分析界面
    /// </summary>
    private void NavigateToMidiAnalysis()
    {
        CurrentView = "MidiAnalysis";
    }

    /// <summary>
    /// 导航到项目设置
    /// </summary>
    private void NavigateToProjectSettings()
    {
        CurrentView = "ProjectSettings";
    }

    /// <summary>
    /// 导航到项目概览
    /// </summary>
    private void NavigateToProjectOverview()
    {
        CurrentView = "ProjectOverview";
    }

    /// <summary>
    /// 导航到导出界面
    /// </summary>
    private void NavigateToExport()
    {
        CurrentView = "Export";
    }

    /// <summary>
    /// 保存项目
    /// </summary>
    private async Task SaveProjectAsync()
    {
        if (CurrentProject == null)
        {
            ErrorMessage = "没有可保存的项目";
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            await _projectService.SaveProjectAsync(CurrentProject);

            // 更新项目摘要
            ProjectSummary = CreateProjectSummary(CurrentProject);

            // 显示保存成功提示 (可以通过事件或消息系统)
        }
        catch (Exception ex)
        {
            ErrorMessage = $"保存项目失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 创建项目摘要
    /// </summary>
    public ProjectSummary CreateProjectSummary(ProjectConfig? project)
    {
        if (project == null)
        {
            return new ProjectSummary(
                ProjectName: "未知项目",
                Status: "draft",
                SongType: "未知",
                CreatedAt: DateTime.Now,
                UpdatedAt: DateTime.Now,
                HasMidiFile: false,
                HasLyrics: false
            );
        }
        // 检查是否有 MIDI 文件
        var hasMidiFile = false;
        if (!string.IsNullOrWhiteSpace(project.ProjectPath))
        {
            var midiDir = Path.Combine(project.ProjectPath, "melody", "midi");
            if (_fileSystem.DirectoryExists(midiDir))
            {
                // 检查是否有 .mid 或 .midi 文件
                try
                {
                    var allFiles = _fileSystem.GetFiles(midiDir, "*.*", SearchOption.TopDirectoryOnly);
                    hasMidiFile = allFiles.Any(f =>
                        f.EndsWith(".mid", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".midi", StringComparison.OrdinalIgnoreCase));
                }
                catch
                {
                    // 忽略错误
                }
            }
        }

        // 检查是否有歌词文件
        var hasLyrics = false;
        if (!string.IsNullOrWhiteSpace(project.ProjectPath))
        {
            var lyricsPath = Path.Combine(project.ProjectPath, "lyrics.txt");
            hasLyrics = _fileSystem.FileExists(lyricsPath);
        }

        return new ProjectSummary(
            ProjectName: project.Name,
            Status: project.Status ?? "draft",
            SongType: project.Spec?.SongType ?? "未知",
            CreatedAt: project.Created,
            UpdatedAt: project.UpdatedAt ?? project.Created,
            HasMidiFile: hasMidiFile,
            HasLyrics: hasLyrics
        );
    }

    #endregion
}

