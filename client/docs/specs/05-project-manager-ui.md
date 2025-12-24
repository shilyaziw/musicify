# Spec 05: 项目管理器 UI (WelcomeWindow)

**状态**: 🟢 已完成
**优先级**: P0 (核心功能)
**实际时间**: 16 小时
**依赖**: Spec 03 (项目配置服务)
**完成时间**: 2024-12-23

---

## 1. 需求概述

### 1.1 功能目标
实现桌面应用的**欢迎窗口、新建项目向导、最近项目列表**等用户界面,提供友好的项目管理体验。

### 1.2 核心功能
- ✅ 欢迎窗口 (启动入口)
- ✅ 新建项目向导 (完整4步流程)
  - Step 1: 基本信息（项目名、路径）
  - Step 2: 歌曲信息（类型、时长、风格、语言、受众、平台、音调）
  - Step 3: 创作模式（教练/快速/混合 + MIDI文件选择）
  - Step 4: 确认创建（项目信息摘要）
- ✅ 打开已有项目
- ✅ 最近项目列表 (可点击打开)
- ✅ 项目设置管理（独立界面）
- ✅ 响应式布局设计

### 1.3 技术栈
- **UI 框架**: AvaloniaUI 11.1.3
- **架构模式**: MVVM (CommunityToolkit.Mvvm)
- **依赖注入**: Microsoft.Extensions.DependencyInjection
- **样式主题**: FluentTheme (类似 WinUI 3)

---

## 2. 技术规格

### 2.1 MVVM 架构设计

```
┌─────────────────────────────────────────┐
│          View (XAML)                    │
│  - WelcomeWindow.axaml                  │
│  - CreateProjectDialog.axaml            │
└──────────────┬──────────────────────────┘
               │ Data Binding
┌──────────────▼──────────────────────────┐
│        ViewModel (C#)                   │
│  - WelcomeViewModel                     │
│  - CreateProjectViewModel               │
│  + Commands (RelayCommand)              │
│  + Properties (ObservableProperty)      │
└──────────────┬──────────────────────────┘
               │ Service Call
┌──────────────▼──────────────────────────┐
│         Service (C#)                    │
│  - IProjectService                      │
│  - IAIService                           │
└─────────────────────────────────────────┘
```

### 2.2 ViewModelBase 基类

```csharp
namespace Musicify.Desktop.ViewModels;

/// <summary>
/// ViewModel 基类
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
    /// <summary>
    /// 错误消息
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// 是否正在加载
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// 显示错误消息
    /// </summary>
    protected void ShowError(string message)
    {
        ErrorMessage = message;
    }

    /// <summary>
    /// 清除错误消息
    /// </summary>
    protected void ClearError()
    {
        ErrorMessage = null;
    }
}
```

### 2.3 WelcomeViewModel 设计

```csharp
namespace Musicify.Desktop.ViewModels;

/// <summary>
/// 欢迎窗口 ViewModel
/// </summary>
public partial class WelcomeViewModel : ViewModelBase
{
    private readonly IProjectService _projectService;
    private readonly INavigationService _navigationService;

    /// <summary>
    /// 最近项目列表
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ProjectItemViewModel> _recentProjects = new();

    /// <summary>
    /// 选中的项目
    /// </summary>
    [ObservableProperty]
    private ProjectItemViewModel? _selectedProject;

    public WelcomeViewModel(
        IProjectService projectService,
        INavigationService navigationService)
    {
        _projectService = projectService;
        _navigationService = navigationService;

        LoadRecentProjectsAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// 新建项目命令
    /// </summary>
    [RelayCommand]
    private async Task CreateProjectAsync()
    {
        var dialog = new CreateProjectDialog
        {
            DataContext = new CreateProjectViewModel(_projectService)
        };

        var result = await dialog.ShowDialog<ProjectConfig?>(GetWindow());

        if (result != null)
        {
            await OpenProjectAsync(result);
        }
    }

    /// <summary>
    /// 打开项目命令
    /// </summary>
    [RelayCommand]
    private async Task OpenProjectAsync()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "选择项目文件夹"
        };

        var path = await dialog.ShowAsync(GetWindow());

        if (!string.IsNullOrEmpty(path))
        {
            IsLoading = true;
            ClearError();

            try
            {
                var project = await _projectService.LoadProjectAsync(path);

                if (project != null)
                {
                    await OpenProjectAsync(project);
                }
                else
                {
                    ShowError("无法加载项目,请检查项目路径是否正确");
                }
            }
            catch (Exception ex)
            {
                ShowError($"打开项目失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    /// <summary>
    /// 打开选中的最近项目
    /// </summary>
    [RelayCommand]
    private async Task OpenSelectedProjectAsync()
    {
        if (SelectedProject == null) return;

        IsLoading = true;

        try
        {
            var project = await _projectService.LoadProjectAsync(SelectedProject.Path);

            if (project != null)
            {
                await OpenProjectAsync(project);
            }
            else
            {
                ShowError("项目不存在或已损坏");
                await LoadRecentProjectsAsync(); // 刷新列表
            }
        }
        catch (Exception ex)
        {
            ShowError($"打开项目失败: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 加载最近项目
    /// </summary>
    private async Task LoadRecentProjectsAsync()
    {
        try
        {
            var projects = await _projectService.GetRecentProjectsAsync(10);

            RecentProjects.Clear();
            foreach (var project in projects)
            {
                RecentProjects.Add(new ProjectItemViewModel(project));
            }
        }
        catch (Exception ex)
        {
            ShowError($"加载最近项目失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 打开项目主窗口
    /// </summary>
    private async Task OpenProjectAsync(ProjectConfig project)
    {
        await _projectService.AddToRecentProjectsAsync(project.ProjectPath);
        _navigationService.NavigateToMainWindow(project);
    }

    private Window GetWindow()
    {
        // 获取当前窗口的辅助方法
        return Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow!
            : throw new InvalidOperationException("无法获取主窗口");
    }
}

/// <summary>
/// 项目列表项 ViewModel
/// </summary>
public partial class ProjectItemViewModel : ObservableObject
{
    public string Name { get; }
    public string Path { get; }
    public string Status { get; }
    public DateTime LastOpened { get; }

    public ProjectItemViewModel(ProjectConfig config)
    {
        Name = config.ProjectName;
        Path = config.ProjectPath;
        Status = config.Status;
        LastOpened = config.UpdatedAt;
    }

    public string LastOpenedText => LastOpened.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    public string StatusText => Status switch
    {
        "draft" => "草稿",
        "in_progress" => "创作中",
        "completed" => "已完成",
        _ => Status
    };
}
```

### 2.4 CreateProjectViewModel 设计

``csharp
namespace Musicify.Core.ViewModels;

/// <summary>
/// 新建项目向导 ViewModel
/// 4步流程:
/// 1. 基本信息 (项目名、路径)
/// 2. 歌曲信息 (类型、风格、语言、主题)
/// 3. 创作模式 (Coach/Express/Hybrid + MIDI 文件)
/// 4. 确认并创建
/// </summary>
public class CreateProjectViewModel : ViewModelBase
{
    private readonly IProjectService _projectService;
    private readonly INavigationService _navigationService;

    private string _projectName = string.Empty;
    private string _projectPath = string.Empty;
    private SongSpec? _songSpec;
    private string _creationMode = "coach"; // 默认教练模式
    private string _midiFilePath = string.Empty;

    // 歌曲信息属性
    private string _songType = string.Empty;
    private string _duration = "3分30秒";
    private string _style = string.Empty;
    private string _language = string.Empty;
    private string _audienceAge = "20-30岁";
    private string _audienceGender = "中性";
    private List<string> _targetPlatforms = new();
    private string _tone = string.Empty;

    private int _currentStep = 1;
    private readonly int _totalSteps = 4;

    private bool _isCreating;
    private string _errorMessage = string.Empty;
    private Dictionary<string, string> _validationErrors = new();

    public CreateProjectViewModel(
        IProjectService projectService,
        INavigationService navigationService)
    {
        _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        // 初始化命令
        NextStepCommand = new RelayCommand(OnNextStep, CanGoNext);
        PreviousStepCommand = new RelayCommand(OnPreviousStep, CanGoBack);
        CreateProjectCommand = new AsyncRelayCommand(OnCreateProjectAsync, CanCreateProject);
        CancelCommand = new RelayCommand(OnCancel);
        BrowseProjectPathCommand = new RelayCommand(OnBrowseProjectPath);
        SelectMidiFileCommand = new RelayCommand(OnSelectMidiFile);
        ClearErrorCommand = new RelayCommand(OnClearError);
        TogglePlatformCommand = new RelayCommand(() => { }); // 占位，实际由 View 直接调用方法
        ToggleCreationModeCommand = new RelayCommand(() => { }); // 占位，实际由 View 直接调用方法

        // 初始化选项列表
        SongTypes = new List<string>(Models.Constants.SongTypes.All);
        Styles = new List<string>(Models.Constants.Styles.All);
        Languages = new List<string>(Models.Constants.Languages.All);
        Platforms = new List<string>(Models.Constants.Platforms.All);
        AudienceAges = new List<string> { "15-20岁", "20-30岁", "30-40岁", "全年龄" };
        AudienceGenders = new List<string> { "女性向", "男性向", "中性" };

        // 监听属性变化,更新验证和命令状态
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ProjectName) || e.PropertyName == nameof(ProjectPath))
            {
                ValidateStep1();
                (NextStepCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
            else if (e.PropertyName is nameof(SongType) or nameof(Duration) or nameof(Style) or nameof(Language) or nameof(AudienceAge) or nameof(AudienceGender) or nameof(TargetPlatforms))
            {
                BuildSongSpec();
                ValidateStep2();
                (NextStepCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
            else if (e.PropertyName == nameof(CurrentStep))
            {
                UpdateNavigationCommands();
                if (CurrentStep == 2)
                {
                    BuildSongSpec();
                }
            }
        };
    }

    #region 属性

    /// <summary>
    /// 项目名称
    /// </summary>
    public string ProjectName
    {
        get => _projectName;
        set
        {
            if (SetProperty(ref _projectName, value))
            {
                ValidateProjectName();
            }
        }
    }

    /// <summary>
    /// 项目路径
    /// </summary>
    public string ProjectPath
    {
        get => _projectPath;
        set
        {
            if (SetProperty(ref _projectPath, value))
            {
                ValidateProjectPath();
            }
        }
    }

    /// <summary>
    /// 歌曲规格
    /// </summary>
    public SongSpec SongSpec
    {
        get => _songSpec;
        set => SetProperty(ref _songSpec, value);
    }

    /// <summary>
    /// 创作模式 (coach/express/hybrid)
    /// </summary>
    public string CreationMode
    {
        get => _creationMode;
        set
        {
            if (SetProperty(ref _creationMode, value))
            {
                OnPropertyChanged(nameof(CreationModeDescription));
                OnPropertyChanged(nameof(ShowMidiOption));
            }
        }
    }

    /// <summary>
    /// 创作模式描述
    /// </summary>
    public string CreationModeDescription => CreationMode switch
    {
        "coach" => "教练模式 - AI 引导逐步创作,适合深度打磨",
        "express" => "快速模式 - AI 一键生成完整歌词,适合快速创作",
        "hybrid" => "混合模式 - 结合引导和自动生成,灵活创作",
        _ => ""
    };

    /// <summary>
    /// 是否显示 MIDI 选项 (仅教练/混合模式)
    /// </summary>
    public bool ShowMidiOption => CreationMode is "coach" or "hybrid";

    /// <summary>
    /// MIDI 文件路径 (可选)
    /// </summary>
    public string MidiFilePath
    {
        get => _midiFilePath;
        set => SetProperty(ref _midiFilePath, value);
    }

    /// <summary>
    /// 当前步骤 (1-4)
    /// </summary>
    public int CurrentStep
    {
        get => _currentStep;
        set
        {
            if (SetProperty(ref _currentStep, value))
            {
                OnPropertyChanged(nameof(ProgressPercentage));
            }
        }
    }

    /// <summary>
    /// 总步骤数
    /// </summary>
    public int TotalSteps => _totalSteps;

    /// <summary>
    /// 进度百分比
    /// </summary>
    public int ProgressPercentage => (CurrentStep * 100) / TotalSteps;

    /// <summary>
    /// 是否正在创建
    /// </summary>
    public bool IsCreating
    {
        get => _isCreating;
        set => SetProperty(ref _isCreating, value);
    }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>
    /// 验证错误字典
    /// </summary>
    public Dictionary<string, string> ValidationErrors
    {
        get => _validationErrors;
        set => SetProperty(ref _validationErrors, value);
    }

    /// <summary>
    /// 项目摘要 (第4步显示)
    /// </summary>
    public string ProjectSummary => $"""
        项目名称: {ProjectName}
        项目路径: {ProjectPath}

        歌曲类型: {SongSpec?.SongType ?? "未指定"}
        时长: {SongSpec?.Duration ?? "未指定"}
        音乐风格: {SongSpec?.Style ?? "未指定"}
        语言: {SongSpec?.Language ?? "未指定"}
        受众: {SongSpec?.Audience?.Age ?? "未指定"} / {SongSpec?.Audience?.Gender ?? "未指定"}
        目标平台: {(SongSpec?.TargetPlatform?.Count > 0 ? string.Join(", ", SongSpec.TargetPlatform) : "未指定")}
        音调: {(string.IsNullOrEmpty(SongSpec?.Tone) ? "未指定" : SongSpec.Tone)}

        创作模式: {CreationModeDescription}
        {(string.IsNullOrEmpty(MidiFilePath) ? "" : $"参考旋律: {Path.GetFileName(MidiFilePath)}")}
        """;

    /// <summary>
    /// 歌曲类型列表
    /// </summary>
    public List<string> SongTypes { get; }

    /// <summary>
    /// 风格列表
    /// </summary>
    public List<string> Styles { get; }

    /// <summary>
    /// 语言列表
    /// </summary>
    public List<string> Languages { get; }

    /// <summary>
    /// 平台列表
    /// </summary>
    public List<string> Platforms { get; }

    /// <summary>
    /// 受众年龄段列表
    /// </summary>
    public List<string> AudienceAges { get; }

    /// <summary>
    /// 受众性别列表
    /// </summary>
    public List<string> AudienceGenders { get; }

    /// <summary>
    /// 歌曲类型
    /// </summary>
    public string SongType
    {
        get => _songType;
        set => SetProperty(ref _songType, value);
    }

    /// <summary>
    /// 时长
    /// </summary>
    public string Duration
    {
        get => _duration;
        set => SetProperty(ref _duration, value);
    }

    /// <summary>
    /// 风格
    /// </summary>
    public string Style
    {
        get => _style;
        set => SetProperty(ref _style, value);
    }

    /// <summary>
    /// 语言
    /// </summary>
    public string Language
    {
        get => _language;
        set => SetProperty(ref _language, value);
    }

    /// <summary>
    /// 受众年龄段
    /// </summary>
    public string AudienceAge
    {
        get => _audienceAge;
        set => SetProperty(ref _audienceAge, value);
    }

    /// <summary>
    /// 受众性别
    /// </summary>
    public string AudienceGender
    {
        get => _audienceGender;
        set => SetProperty(ref _audienceGender, value);
    }

    /// <summary>
    /// 目标平台列表
    /// </summary>
    public List<string> TargetPlatforms
    {
        get => _targetPlatforms;
        set => SetProperty(ref _targetPlatforms, value);
    }

    /// <summary>
    /// 音调
    /// </summary>
    public string Tone
    {
        get => _tone;
        set => SetProperty(ref _tone, value);
    }

    #endregion

    #region 命令

    /// <summary>
    /// 下一步命令
    /// </summary>
    public ICommand NextStepCommand { get; }

    /// <summary>
    /// 上一步命令
    /// </summary>
    public ICommand PreviousStepCommand { get; }

    /// <summary>
    /// 创建项目命令
    /// </summary>
    public ICommand CreateProjectCommand { get; }

    /// <summary>
    /// 取消命令
    /// </summary>
    public ICommand CancelCommand { get; }

    /// <summary>
    /// 浏览项目路径命令
    /// </summary>
    public ICommand BrowseProjectPathCommand { get; }

    /// <summary>
    /// 选择 MIDI 文件命令
    /// </summary>
    public ICommand SelectMidiFileCommand { get; }

    /// <summary>
    /// 清除错误命令
    /// </summary>
    public ICommand ClearErrorCommand { get; }

    /// <summary>
    /// 切换平台选择命令
    /// </summary>
    public ICommand TogglePlatformCommand { get; }

    /// <summary>
    /// 切换创作模式命令
    /// </summary>
    public ICommand ToggleCreationModeCommand { get; }

    #endregion

    #region 公共方法

    /// <summary>
    /// 是否可以继续下一步
    /// </summary>
    public bool CanGoNext()
    {
        if (CurrentStep >= TotalSteps) return false;

        return CurrentStep switch
        {
            1 => ValidateStep1(),
            2 => ValidateStep2(),
            3 => ValidateStep3(),
            _ => false
        };
    }

    /// <summary>
    /// 是否可以返回上一步
    /// </summary>
    public bool CanGoBack()
    {
        return CurrentStep > 1;
    }

    /// <summary>
    /// 浏览路径请求回调 (由 View 设置)
    /// </summary>
    public Func<Task<string?>>? OnBrowsePathRequested { get; set; }

    /// <summary>
    /// 浏览 MIDI 文件请求回调 (由 View 设置)
    /// </summary>
    public Func<Task<string?>>? OnBrowseMidiRequested { get; set; }

    #endregion

    #region 验证逻辑

    private bool ValidateStep1()
    {
        return !string.IsNullOrWhiteSpace(ProjectName) &&
               !string.IsNullOrWhiteSpace(ProjectPath) &&
               !ValidationErrors.ContainsKey("ProjectName") &&
               !ValidationErrors.ContainsKey("ProjectPath");
    }

    private bool ValidateStep2()
    {
        return !string.IsNullOrWhiteSpace(SongType) &&
               !string.IsNullOrWhiteSpace(Duration) &&
               !string.IsNullOrWhiteSpace(Style) &&
               !string.IsNullOrWhiteSpace(Language) &&
               !string.IsNullOrWhiteSpace(AudienceAge) &&
               !string.IsNullOrWhiteSpace(AudienceGender) &&
               TargetPlatforms != null && TargetPlatforms.Count > 0;
    }

    private bool ValidateStep3()
    {
        return !string.IsNullOrWhiteSpace(CreationMode);
    }

    private void ValidateProjectName()
    {
        var errors = new Dictionary<string, string>(ValidationErrors);

        if (string.IsNullOrWhiteSpace(ProjectName))
        {
            errors["ProjectName"] = "项目名称不能为空";
        }
        else if (ProjectName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            errors["ProjectName"] = "项目名称包含非法字符";
        }
        else
        {
            errors.Remove("ProjectName");
        }

        ValidationErrors = errors;
    }

    private void ValidateProjectPath()
    {
        var errors = new Dictionary<string, string>(ValidationErrors);

        if (string.IsNullOrWhiteSpace(ProjectPath))
        {
            errors["ProjectPath"] = "项目路径不能为空";
        }
        else if (!_projectService.ValidateProjectPath(ProjectPath))
        {
            errors["ProjectPath"] = "项目路径已存在或无效";
        }
        else
        {
            errors.Remove("ProjectPath");
        }

        ValidationErrors = errors;
    }

    #endregion

    #region 命令处理

    private void OnNextStep()
    {
        if (CurrentStep < TotalSteps)
        {
            CurrentStep++;
        }
    }

    private void OnPreviousStep()
    {
        if (CurrentStep > 1)
        {
            CurrentStep--;
        }
    }

    private bool CanCreateProject()
    {
        return CurrentStep == TotalSteps && !IsCreating;
    }

    private async Task OnCreateProjectAsync()
    {
        try
        {
            IsCreating = true;
            ErrorMessage = string.Empty;

            // 确保 SongSpec 已构建
            if (SongSpec == null)
            {
                BuildSongSpec();
            }

            // 创建项目
            var project = await _projectService.CreateProjectAsync(
                ProjectName,
                ProjectPath);

            // 保存 SongSpec 到项目
            if (SongSpec != null && project != null)
            {
                // 更新项目配置中的 Spec
                project = project with { Spec = SongSpec };
                await _projectService.SaveProjectAsync(project);
            }

            // 导航到主窗口
            _navigationService.NavigateTo("MainWindow", project);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"创建项目失败: {ex.Message}";
        }
        finally
        {
            IsCreating = false;
        }
    }

    /// <summary>
    /// 构建 SongSpec 对象
    /// </summary>
    private void BuildSongSpec()
    {
        if (string.IsNullOrWhiteSpace(ProjectName) ||
            string.IsNullOrWhiteSpace(SongType) ||
            string.IsNullOrWhiteSpace(Duration) ||
            string.IsNullOrWhiteSpace(Style) ||
            string.IsNullOrWhiteSpace(Language) ||
            string.IsNullOrWhiteSpace(AudienceAge) ||
            string.IsNullOrWhiteSpace(AudienceGender) ||
            TargetPlatforms == null || TargetPlatforms.Count == 0)
        {
            SongSpec = null;
            return;
        }

        SongSpec = new SongSpec
        {
            ProjectName = ProjectName,
            SongType = SongType,
            Duration = Duration,
            Style = Style,
            Language = Language,
            Audience = new AudienceInfo
            {
                Age = AudienceAge,
                Gender = AudienceGender
            },
            TargetPlatform = TargetPlatforms,
            Tone = string.IsNullOrWhiteSpace(Tone) ? null : Tone,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        OnPropertyChanged(nameof(ProjectSummary));
    }

    private void OnCancel()
    {
        _navigationService.NavigateTo("WelcomeWindow", null);
    }

    private async void OnBrowseProjectPath()
    {
        if (OnBrowsePathRequested != null)
        {
            var selectedPath = await OnBrowsePathRequested();
            if (!string.IsNullOrEmpty(selectedPath))
            {
                ProjectPath = selectedPath;
            }
        }
    }

    private async void OnSelectMidiFile()
    {
        if (OnBrowseMidiRequested != null)
        {
            var selectedPath = await OnBrowseMidiRequested();
            if (!string.IsNullOrEmpty(selectedPath))
            {
                MidiFilePath = selectedPath;
            }
        }
    }

    private void OnClearError()
    {
        ErrorMessage = string.Empty;
    }

    /// <summary>
    /// 切换平台（供 View 调用）
    /// </summary>
    public void TogglePlatform(string platform)
    {
        if (string.IsNullOrEmpty(platform)) return;

        var platforms = new List<string>(TargetPlatforms);
        if (platforms.Contains(platform))
        {
            platforms.Remove(platform);
        }
        else
        {
            platforms.Add(platform);
        }
        TargetPlatforms = platforms;
    }

    /// <summary>
    /// 检查平台是否已选择
    /// </summary>
    public bool IsPlatformSelected(string platform)
    {
        return TargetPlatforms.Contains(platform);
    }

    private void UpdateNavigationCommands()
    {
        (NextStepCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (PreviousStepCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (CreateProjectCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }

    #endregion
}
```

---

## 3. UI 设计

### 3.1 WelcomeWindow.axaml

``xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:Musicify.Desktop.ViewModels"
        x:Class="Musicify.Desktop.Views.WelcomeWindow"
        x:DataType="vm:WelcomeViewModel"
        Title="Musicify - 欢迎"
        Width="900" Height="600"
        MinWidth="800" MinHeight="500"
        WindowStartupLocation="CenterScreen"
        TransparencyLevelHint="AcrylicBlur"
        Background="Transparent">

    <Window.Styles>
        <StyleInclude Source="/Styles/WelcomeWindowStyles.axaml"/>
    </Window.Styles>

    <Panel>
        <!-- 背景渐变 -->
        <Panel.Background>
            <LinearGradientBrush StartPoint="0%,0%" EndPoint="100%,100%">
                <GradientStop Color="#1E1E2E" Offset="0"/>
                <GradientStop Color="#2A2A3E" Offset="1"/>
            </LinearGradientBrush>
        </Panel.Background>

        <!-- 主内容 -->
        <Grid RowDefinitions="Auto,*,Auto" Margin="40">

            <!-- Header -->
            <StackPanel Grid.Row="0" Spacing="8" Margin="0,0,0,30">
                <TextBlock Text="🎵 Musicify Desktop"
                           FontSize="36"
                           FontWeight="Bold"
                           Foreground="#E0E0E0"/>
                <TextBlock Text="AI 驱动的歌词创作工具"
                           FontSize="16"
                           Foreground="#A0A0A0"/>
            </StackPanel>

            <!-- Content -->
            <Grid Grid.Row="1" ColumnDefinitions="*,*" ColumnSpacing="40">

                <!-- 左侧: 快速操作 -->
                <StackPanel Grid.Column="0" Spacing="16">
                    <TextBlock Text="快速开始"
                               FontSize="20"
                               FontWeight="SemiBold"
                               Foreground="#E0E0E0"
                               Margin="0,0,0,12"/>

                    <Button Content="📝 新建项目"
                            Command="{Binding CreateProjectCommand}"
                            Classes="ActionButton Primary"
                            HorizontalAlignment="Stretch"
                            Height="56"/>

                    <Button Content="📂 打开项目"
                            Command="{Binding OpenProjectCommand}"
                            Classes="ActionButton"
                            HorizontalAlignment="Stretch"
                            Height="56"/>

                    <Button Content="⚙️ 设置"
                            Classes="ActionButton"
                            HorizontalAlignment="Stretch"
                            Height="56"/>
                </StackPanel>

                <!-- 右侧: 最近项目 -->
                <StackPanel Grid.Column="1">
                    <TextBlock Text="最近项目"
                               FontSize="20"
                               FontWeight="SemiBold"
                               Foreground="#E0E0E0"
                               Margin="0,0,0,12"/>

                    <!-- 项目列表 -->
                    <Border Classes="ProjectListContainer"
                            Height="400">
                        <ListBox ItemsSource="{Binding RecentProjects}"
                                 SelectedItem="{Binding SelectedProject}"
                                 Background="Transparent"
                                 BorderThickness="0">
                            <ListBox.ItemTemplate>
                                <DataTemplate>
                                    <Border Classes="ProjectItem"
                                            Margin="0,0,0,8">
                                        <Grid RowDefinitions="Auto,Auto,Auto" Margin="16">
                                            <TextBlock Grid.Row="0"
                                                       Text="{Binding Name}"
                                                       FontSize="16"
                                                       FontWeight="SemiBold"
                                                       Foreground="#E0E0E0"/>
                                            <TextBlock Grid.Row="1"
                                                       Text="{Binding Path}"
                                                       FontSize="12"
                                                       Foreground="#808080"
                                                       Margin="0,4,0,0"/>
                                            <Grid Grid.Row="2"
                                                  ColumnDefinitions="*,Auto"
                                                  Margin="0,8,0,0">
                                                <TextBlock Grid.Column="0"
                                                           Text="{Binding LastOpenedText}"
                                                           FontSize="11"
                                                           Foreground="#606060"/>
                                                <Border Grid.Column="1"
                                                        Classes="StatusBadge"
                                                        Padding="8,4">
                                                    <TextBlock Text="{Binding StatusText}"
                                                               FontSize="11"/>
                                                </Border>
                                            </Grid>
                                        </Grid>
                                    </Border>
                                </DataTemplate>
                            </ListBox.ItemTemplate>
                        </ListBox>
                    </Border>

                    <!-- 空状态提示 -->
                    <StackPanel IsVisible="{Binding !RecentProjects.Count}"
                                HorizontalAlignment="Center"
                                VerticalAlignment="Center"
                                Margin="0,100,0,0">
                        <TextBlock Text="📭"
                                   FontSize="48"
                                   HorizontalAlignment="Center"
                                   Margin="0,0,0,16"/>
                        <TextBlock Text="暂无最近项目"
                                   FontSize="16"
                                   Foreground="#808080"
                                   HorizontalAlignment="Center"/>
                    </StackPanel>
                </StackPanel>
            </Grid>

            <!-- Footer -->
            <Grid Grid.Row="2" ColumnDefinitions="*,Auto" Margin="0,20,0,0">
                <TextBlock Grid.Column="0"
                           Text="Version 1.0.0"
                           FontSize="12"
                           Foreground="#606060"
                           VerticalAlignment="Center"/>
                <TextBlock Grid.Column="1"
                           Text="Made with ❤️ by Musicify Team"
                           FontSize="12"
                           Foreground="#606060"
                           VerticalAlignment="Center"/>
            </Grid>
        </Grid>

        <!-- Loading Overlay -->
        <Border IsVisible="{Binding IsLoading}"
                Background="#80000000">
            <StackPanel HorizontalAlignment="Center"
                        VerticalAlignment="Center">
                <ProgressRing IsIndeterminate="True"
                              Width="48" Height="48"
                              Foreground="#4A9EFF"/>
                <TextBlock Text="加载中..."
                           FontSize="14"
                           Foreground="#E0E0E0"
                           Margin="0,16,0,0"/>
            </StackPanel>
        </Border>

        <!-- Error Message -->
        <Border IsVisible="{Binding ErrorMessage, Converter={x:Static StringConverters.IsNotNullOrEmpty}}"
                Background="#40FF0000"
                VerticalAlignment="Top"
                Margin="40,20">
            <TextBlock Text="{Binding ErrorMessage}"
                       Foreground="#FFCCCC"
                       Padding="16,12"
                       TextWrapping="Wrap"/>
        </Border>
    </Panel>
</Window>
```

### 3.2 CreateProjectDialog.axaml

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:Musicify.Desktop.ViewModels"
        x:Class="Musicify.Desktop.Views.CreateProjectDialog"
        x:DataType="vm:CreateProjectViewModel"
        Title="新建项目"
        Width="600" Height="700"
        WindowStartupLocation="CenterOwner"
        CanResize="False">

    <Grid RowDefinitions="Auto,*,Auto" Margin="24">

        <!-- Header -->
        <TextBlock Grid.Row="0"
                   Text="📝 新建音乐项目"
                   FontSize="24"
                   FontWeight="Bold"
                   Margin="0,0,0,20"/>

        <!-- Content -->
        <ScrollViewer Grid.Row="1">
            <StackPanel Spacing="20">

                <!-- 基本信息 -->
                <StackPanel Spacing="12">
                    <TextBlock Text="基本信息"
                               FontSize="16"
                               FontWeight="SemiBold"/>

                    <StackPanel Spacing="8">
                        <TextBlock Text="项目名称 *"/>
                        <TextBox Text="{Binding ProjectName}"
                                 Watermark="例如: 我的第一首歌"
                                 MaxLength="50"/>
                    </StackPanel>

                    <StackPanel Spacing="8">
                        <TextBlock Text="保存位置"/>
                        <Grid ColumnDefinitions="*,Auto">
                            <TextBox Grid.Column="0"
                                     Text="{Binding ProjectPath}"
                                     IsReadOnly="True"/>
                            <Button Grid.Column="1"
                                    Content="浏览"
                                    Command="{Binding BrowseProjectPathCommand}"
                                    Margin="8,0,0,0"/>
                        </Grid>
                    </StackPanel>
                </StackPanel>

                <!-- 歌曲规格 -->
                <StackPanel Spacing="12">
                    <TextBlock Text="歌曲规格"
                               FontSize="16"
                               FontWeight="SemiBold"/>

                    <StackPanel Spacing="8">
                        <TextBlock Text="歌曲类型 *"/>
                        <ComboBox ItemsSource="{Binding SongTypes}"
                                  SelectedItem="{Binding SelectedSongType}"
                                  HorizontalAlignment="Stretch"/>
                    </StackPanel>

                    <StackPanel Spacing="8">
                        <TextBlock Text="风格基调"/>
                        <ComboBox ItemsSource="{Binding Styles}"
                                  SelectedItem="{Binding SelectedStyle}"
                                  HorizontalAlignment="Stretch"/>
                    </StackPanel>

                    <StackPanel Spacing="8">
                        <TextBlock Text="语言"/>
                        <ComboBox SelectedItem="{Binding SelectedLanguage}"
                                  HorizontalAlignment="Stretch">
                            <ComboBoxItem Content="简体中文"/>
                            <ComboBoxItem Content="英文"/>
                            <ComboBoxItem Content="粤语"/>
                        </ComboBox>
                    </StackPanel>

                    <StackPanel Spacing="8">
                        <TextBlock>
                            <Run Text="目标时长: "/>
                            <Run Text="{Binding Duration}"/>
                            <Run Text=" 秒"/>
                        </TextBlock>
                        <Slider Minimum="60"
                                Maximum="600"
                                Value="{Binding Duration}"
                                TickFrequency="30"
                                IsSnapToTickEnabled="True"/>
                    </StackPanel>

                    <StackPanel Spacing="8">
                        <TextBlock Text="目标受众"/>
                        <TextBox Text="{Binding TargetAudience}"
                                 Watermark="例如: 18-25岁年轻人"/>
                    </StackPanel>
                </StackPanel>

                <!-- 发布平台 -->
                <StackPanel Spacing="12">
                    <TextBlock Text="发布平台 (可多选)"
                               FontSize="16"
                               FontWeight="SemiBold"/>

                    <ItemsControl ItemsSource="{Binding Platforms}">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <CheckBox Content="{Binding Name}"
                                          IsChecked="{Binding IsSelected}"
                                          Margin="0,4"/>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                </StackPanel>
            </StackPanel>
        </ScrollViewer>

        <!-- Footer -->
        <Grid Grid.Row="2"
              ColumnDefinitions="*,Auto,Auto"
              Margin="0,20,0,0">
            <TextBlock Grid.Column="0"
                       Text="{Binding ErrorMessage}"
                       Foreground="Red"
                       VerticalAlignment="Center"
                       TextWrapping="Wrap"/>
            <Button Grid.Column="1"
                    Content="取消"
                    Click="OnCancelClick"
                    Margin="0,0,8,0"
                    Width="100"/>
            <Button Grid.Column="2"
                    Content="创建"
                    Command="{Binding CreateProjectCommand}"
                    IsEnabled="{Binding !IsLoading}"
                    Classes="Primary"
                    Width="100"/>
        </Grid>
    </Grid>
</Window>
```

---

## 4. 样式设计

### 4.1 WelcomeWindowStyles.axaml

```xml
<Styles xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- Action Button -->
    <Style Selector="Button.ActionButton">
        <Setter Property="Background" Value="#2A2A3E"/>
        <Setter Property="Foreground" Value="#E0E0E0"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="BorderBrush" Value="#404050"/>
        <Setter Property="CornerRadius" Value="8"/>
        <Setter Property="FontSize" Value="16"/>
        <Setter Property="Cursor" Value="Hand"/>
        <Setter Property="Transitions">
            <Transitions>
                <BrushTransition Property="Background" Duration="0:0:0.2"/>
                <TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.2"/>
            </Transitions>
        </Setter>
    </Style>

    <Style Selector="Button.ActionButton:pointerover">
        <Setter Property="Background" Value="#353545"/>
        <Setter Property="RenderTransform" Value="scale(1.02)"/>
    </Style>

    <Style Selector="Button.ActionButton.Primary">
        <Setter Property="Background">
            <Setter.Value>
                <LinearGradientBrush StartPoint="0%,0%" EndPoint="100%,100%">
                    <GradientStop Color="#4A9EFF" Offset="0"/>
                    <GradientStop Color="#5E7AFF" Offset="1"/>
                </LinearGradientBrush>
            </Setter.Value>
        </Setter>
        <Setter Property="Foreground" Value="#FFFFFF"/>
        <Setter Property="BorderBrush" Value="Transparent"/>
        <Setter Property="FontWeight" Value="SemiBold"/>
    </Style>

    <!-- Project List Container -->
    <Style Selector="Border.ProjectListContainer">
        <Setter Property="Background" Value="#20FFFFFF"/>
        <Setter Property="CornerRadius" Value="12"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="BorderBrush" Value="#30FFFFFF"/>
    </Style>

    <!-- Project Item -->
    <Style Selector="Border.ProjectItem">
        <Setter Property="Background" Value="#2A2A3E"/>
        <Setter Property="CornerRadius" Value="8"/>
        <Setter Property="Cursor" Value="Hand"/>
        <Setter Property="Transitions">
            <Transitions>
                <BrushTransition Property="Background" Duration="0:0:0.2"/>
            </Transitions>
        </Setter>
    </Style>

    <Style Selector="Border.ProjectItem:pointerover">
        <Setter Property="Background" Value="#353545"/>
    </Style>

    <!-- Status Badge -->
    <Style Selector="Border.StatusBadge">
        <Setter Property="Background" Value="#404050"/>
        <Setter Property="CornerRadius" Value="4"/>
    </Style>
</Styles>
```

---

## 5. 导航服务设计

```csharp
namespace Musicify.Desktop.Services;

/// <summary>
/// 导航服务接口
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// 导航到主窗口
    /// </summary>
    void NavigateToMainWindow(ProjectConfig project);

    /// <summary>
    /// 返回欢迎窗口
    /// </summary>
    void NavigateToWelcome();
}

/// <summary>
/// 导航服务实现
/// </summary>
public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void NavigateToMainWindow(ProjectConfig project)
    {
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        var viewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
        viewModel.LoadProject(project);
        mainWindow.DataContext = viewModel;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var currentWindow = desktop.MainWindow;
            desktop.MainWindow = mainWindow;
            mainWindow.Show();
            currentWindow?.Close();
        }
    }

    public void NavigateToWelcome()
    {
        var welcomeWindow = _serviceProvider.GetRequiredService<WelcomeWindow>();
        var viewModel = _serviceProvider.GetRequiredService<WelcomeViewModel>();
        welcomeWindow.DataContext = viewModel;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var currentWindow = desktop.MainWindow;
            desktop.MainWindow = welcomeWindow;
            welcomeWindow.Show();
            currentWindow?.Close();
        }
    }
}
```

---

## 6. 测试用例设计

### 6.1 WelcomeViewModel 测试

```csharp
[Fact]
public async Task LoadRecentProjects_ShouldPopulateList()
{
    // Arrange
    var mockProjects = CreateMockProjects(5);
    _projectServiceMock.Setup(s => s.GetRecentProjectsAsync(10))
        .ReturnsAsync(mockProjects);

    var viewModel = new WelcomeViewModel(_projectServiceMock.Object, _navigationServiceMock.Object);

    // Act
    await Task.Delay(100); // Wait for async loading

    // Assert
    viewModel.RecentProjects.Should().HaveCount(5);
}

[Fact]
public async Task OpenProjectCommand_WithValidPath_ShouldNavigate()
{
    // Arrange
    var project = CreateTestProject();
    _projectServiceMock.Setup(s => s.LoadProjectAsync(It.IsAny<string>()))
        .ReturnsAsync(project);

    var viewModel = new WelcomeViewModel(_projectServiceMock.Object, _navigationServiceMock.Object);

    // Act
    // (需要 UI 测试框架)

    // Assert
    _navigationServiceMock.Verify(n => n.NavigateToMainWindow(project), Times.Once);
}
```

### 6.2 CreateProjectViewModel 测试

```csharp
[Fact]
public void CanCreateProject_WithEmptyName_ShouldReturnFalse()
{
    var viewModel = new CreateProjectViewModel(_projectServiceMock.Object);
    viewModel.ProjectName = "";

    var canCreate = viewModel.CreateProjectCommand.CanExecute(null);

    canCreate.Should().BeFalse();
}

[Fact]
public async Task CreateProject_WithValidData_ShouldSucceed()
{
    var viewModel = new CreateProjectViewModel(_projectServiceMock.Object);
    viewModel.ProjectName = "Test Song";
    viewModel.SelectedSongType = SongTypes.Pop;

    var result = await viewModel.CreateProjectCommand.ExecuteAsync(null);

    result.Should().NotBeNull();
    _projectServiceMock.Verify(s => s.CreateProjectAsync("Test Song", It.IsAny<string>()), Times.Once);
}
```

---

## 7. 验收标准

### 7.1 功能验收
- [x] 欢迎窗口正常显示
- [x] 新建项目向导完整可用
- [x] 最近项目列表正确加载
- [x] 双击项目可打开
- [x] 错误提示友好
- [x] 加载状态显示

### 7.2 UI/UX 验收
- [x] 界面美观现代
- [x] 响应式布局
- [x] 动画流畅自然
- [x] 支持深色主题
- [x] 字体大小适中

---

## 8. 实现清单

### 8.1 ViewModels (4 个)
- [ ] `ViewModelBase.cs`
- [ ] `WelcomeViewModel.cs`
- [ ] `CreateProjectViewModel.cs`
- [ ] `ProjectItemViewModel.cs`

### 8.2 Views (2 个)
- [ ] `WelcomeWindow.axaml` + `.axaml.cs`
- [ ] `CreateProjectDialog.axaml` + `.axaml.cs`

### 8.3 Services (1 个)
- [ ] `INavigationService.cs`
- [ ] `NavigationService.cs`

### 8.4 Styles (1 个)
- [ ] `WelcomeWindowStyles.axaml`

### 8.5 Tests (2 个)
- [ ] `WelcomeViewModelTests.cs`
- [ ] `CreateProjectViewModelTests.cs`

---

## 9. 时间估算

| 任务 | 预计时间 |
|------|---------|
| 编写 ViewModel 基类 | 1小时 |
| 实现 WelcomeViewModel | 2.5小时 |
| 实现 CreateProjectViewModel | 2小时 |
| 设计 UI (XAML) | 3小时 |
| 编写样式 | 1.5小时 |
| 实现导航服务 | 1小时 |
| 编写单元测试 | 2小时 |
| **总计** | **13小时** |

---

## 10. 参考资料

- [AvaloniaUI Documentation](https://docs.avaloniaui.net/)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [Fluent Design System](https://www.microsoft.com/design/fluent/)
- 路线图: `docs/tasks/development-roadmap.md`
