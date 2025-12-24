using System.IO;
using System.Text.Json;
using System.Windows.Input;
using Musicify.Core.Abstractions;
using Musicify.Core.Models;
using Musicify.Core.Models.Constants;
using Musicify.Core.Services;

namespace Musicify.Core.ViewModels;

/// <summary>
/// 项目设置界面 ViewModel
/// </summary>
public class ProjectSettingsViewModel : ViewModelBase
{
    private readonly IProjectService _projectService;
    private readonly IFileSystem _fileSystem;

    // 缓存JsonSerializerOptions实例，避免重复创建
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private ProjectConfig? _currentProject;
    private bool _isSaving;
    private string? _errorMessage;
    private string? _successMessage;

    // 项目基本信息
    private string _projectName = string.Empty;
    private string _songType = string.Empty;
    private string _duration = string.Empty;
    private string _style = string.Empty;
    private string _language = string.Empty;

    // 受众信息
    private string _audienceAge = string.Empty;
    private string _audienceGender = string.Empty;

    // 目标平台
    private List<string> _selectedPlatforms = new();

    // 音调
    private string _tone = string.Empty;

    public ProjectSettingsViewModel(
        IProjectService projectService,
        IFileSystem fileSystem)
    {
        _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

        // 初始化命令
        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync, CanSave);
        ResetCommand = new RelayCommand(Reset);

        // 初始化选项列表
        SongTypes = new List<string>(Musicify.Core.Models.Constants.SongTypes.All);
        Styles = new List<string>(Musicify.Core.Models.Constants.Styles.All);
        Languages = new List<string>(Musicify.Core.Models.Constants.Languages.All);
        Platforms = new List<string>(Musicify.Core.Models.Constants.Platforms.All);
        AudienceAges = new List<string> { "15-20岁", "20-30岁", "30-40岁", "全年龄" };
        AudienceGenders = new List<string> { "女性向", "男性向", "中性" };
    }

    #region 属性

    /// <summary>
    /// 当前项目配置
    /// </summary>
    public ProjectConfig? CurrentProject
    {
        get => _currentProject;
        set
        {
            if (SetProperty(ref _currentProject, value))
            {
                LoadProjectSettings();
            }
        }
    }

    /// <summary>
    /// 项目名称
    /// </summary>
    public string ProjectName
    {
        get => _projectName;
        set => SetProperty(ref _projectName, value);
    }

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
    /// 受众性别倾向
    /// </summary>
    public string AudienceGender
    {
        get => _audienceGender;
        set => SetProperty(ref _audienceGender, value);
    }

    /// <summary>
    /// 选中的目标平台
    /// </summary>
    public List<string> SelectedPlatforms
    {
        get => _selectedPlatforms;
        set
        {
            if (SetProperty(ref _selectedPlatforms, value))
            {
                // 通知 UI 更新
                OnPropertyChanged(nameof(SelectedPlatforms));
            }
        }
    }

    /// <summary>
    /// 检查平台是否被选中
    /// </summary>
    public bool IsPlatformSelected(string platform)
    {
        return SelectedPlatforms.Contains(platform);
    }

    /// <summary>
    /// 切换平台选择状态
    /// </summary>
    public void TogglePlatform(string platform)
    {
        if (SelectedPlatforms.Contains(platform))
        {
            SelectedPlatforms.Remove(platform);
        }
        else
        {
            SelectedPlatforms.Add(platform);
        }
        OnPropertyChanged(nameof(SelectedPlatforms));
    }

    /// <summary>
    /// 音调
    /// </summary>
    public string Tone
    {
        get => _tone;
        set => SetProperty(ref _tone, value);
    }

    /// <summary>
    /// 是否正在保存
    /// </summary>
    public bool IsSaving
    {
        get => _isSaving;
        private set => SetProperty(ref _isSaving, value);
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
    /// 歌曲类型选项
    /// </summary>
    public List<string> SongTypes { get; }

    /// <summary>
    /// 风格选项
    /// </summary>
    public List<string> Styles { get; }

    /// <summary>
    /// 语言选项
    /// </summary>
    public List<string> Languages { get; }

    /// <summary>
    /// 平台选项
    /// </summary>
    public List<string> Platforms { get; }

    /// <summary>
    /// 受众年龄段选项
    /// </summary>
    public List<string> AudienceAges { get; }

    /// <summary>
    /// 受众性别选项
    /// </summary>
    public List<string> AudienceGenders { get; }

    #endregion

    #region 命令

    /// <summary>
    /// 保存设置命令
    /// </summary>
    public ICommand SaveSettingsCommand { get; }

    /// <summary>
    /// 重置命令
    /// </summary>
    public ICommand ResetCommand { get; }

    #endregion

    #region 公共方法

    /// <summary>
    /// 设置当前项目
    /// </summary>
    public void SetProjectAsync(ProjectConfig project)
    {
        CurrentProject = project;
    }

    #endregion

    #region 命令实现

    /// <summary>
    /// 加载项目设置
    /// </summary>
    private void LoadProjectSettings()
    {
        if (CurrentProject == null)
        {
            Reset();
            return;
        }

        ProjectName = CurrentProject.Name;

        if (CurrentProject.Spec != null)
        {
            SongType = CurrentProject.Spec.SongType ?? string.Empty;
            Duration = CurrentProject.Spec.Duration ?? string.Empty;
            Style = CurrentProject.Spec.Style ?? string.Empty;
            Language = CurrentProject.Spec.Language ?? string.Empty;
            Tone = CurrentProject.Spec.Tone ?? string.Empty;

            if (CurrentProject.Spec.Audience != null)
            {
                AudienceAge = CurrentProject.Spec.Audience.Age ?? string.Empty;
                AudienceGender = CurrentProject.Spec.Audience.Gender ?? string.Empty;
            }

            SelectedPlatforms = CurrentProject.Spec.TargetPlatform?.ToList() ?? new List<string>();
        }
        else
        {
            // 如果没有规格，使用默认值
            Reset();
        }
    }

    /// <summary>
    /// 保存设置
    /// </summary>
    private async Task SaveSettingsAsync()
    {
        if (CurrentProject == null)
        {
            ErrorMessage = "项目未加载";
            return;
        }

        try
        {
            IsSaving = true;
            ErrorMessage = null;
            SuccessMessage = null;

            // 验证输入
            if (string.IsNullOrWhiteSpace(ProjectName))
            {
                ErrorMessage = "项目名称不能为空";
                return;
            }

            // 创建或更新 SongSpec
            var audience = new AudienceInfo
            {
                Age = AudienceAge,
                Gender = AudienceGender
            };

            var spec = new SongSpec
            {
                ProjectName = ProjectName,
                SongType = SongType,
                Duration = Duration,
                Style = Style,
                Language = Language,
                Audience = audience,
                TargetPlatform = SelectedPlatforms,
                Tone = Tone,
                CreatedAt = CurrentProject.Spec?.CreatedAt ?? DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // 更新项目配置（注意：ProjectConfig 是 record，需要创建新实例）
            var updatedProject = new ProjectConfig
            {
                Name = ProjectName,
                Type = CurrentProject.Type,
                Ai = CurrentProject.Ai,
                ScriptType = CurrentProject.ScriptType,
                DefaultType = CurrentProject.DefaultType,
                Created = CurrentProject.Created,
                Version = CurrentProject.Version,
                ProjectPath = CurrentProject.ProjectPath,
                UpdatedAt = DateTime.UtcNow,
                Status = CurrentProject.Status,
                Spec = spec
            };

            // 保存项目
            await _projectService.SaveProjectAsync(updatedProject);

            // 保存规格到文件
            if (!string.IsNullOrWhiteSpace(updatedProject.ProjectPath))
            {
                var specPath = Path.Combine(updatedProject.ProjectPath, "spec.json");
                var specJson = JsonSerializer.Serialize(spec, JsonOptions);

                await _fileSystem.WriteAllTextAsync(specPath, specJson);
            }

            CurrentProject = updatedProject;
            SuccessMessage = "设置已保存";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"保存失败: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>
    /// 是否可以保存
    /// </summary>
    private bool CanSave()
    {
        return CurrentProject != null && !IsSaving;
    }

    /// <summary>
    /// 重置设置
    /// </summary>
    private void Reset()
    {
        if (CurrentProject != null)
        {
            LoadProjectSettings();
        }
        else
        {
            ProjectName = string.Empty;
            SongType = string.Empty;
            Duration = string.Empty;
            Style = string.Empty;
            Language = string.Empty;
            AudienceAge = string.Empty;
            AudienceGender = string.Empty;
            SelectedPlatforms = new List<string>();
            Tone = string.Empty;
        }

        ErrorMessage = null;
        SuccessMessage = null;
    }

    #endregion
}

