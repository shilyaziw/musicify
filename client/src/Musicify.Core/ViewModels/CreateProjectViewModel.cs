using System.Collections.ObjectModel;
using System.Windows.Input;
using Musicify.Core.Models;
using Musicify.Core.Models.Constants;
using Musicify.Core.Services;

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
