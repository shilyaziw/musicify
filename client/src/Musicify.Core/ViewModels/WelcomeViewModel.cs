using System.Collections.ObjectModel;
using System.Windows.Input;
using Musicify.Core.Models;
using Musicify.Core.Services;

namespace Musicify.Core.ViewModels;

/// <summary>
/// 欢迎窗口 ViewModel
/// 功能:
/// - 显示最近项目
/// - 创建新项目
/// - 打开现有项目
/// - 浏览项目文件夹
/// </summary>
public class WelcomeViewModel : ViewModelBase
{
    private readonly IProjectService _projectService;
    private readonly INavigationService _navigationService;

    private ObservableCollection<ProjectConfig> _recentProjects = new();
    private bool _isLoading;
    private string _errorMessage = string.Empty;

    public WelcomeViewModel(
        IProjectService projectService,
        INavigationService navigationService)
    {
        _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        // 初始化命令
        CreateNewProjectCommand = new RelayCommand(OnCreateNewProject);
        OpenProjectCommand = new AsyncRelayCommand<ProjectConfig>(OnOpenProjectAsync, CanOpenProject);
        BrowseProjectCommand = new AsyncRelayCommand(OnBrowseProjectAsync);
        ClearErrorCommand = new RelayCommand(OnClearError);

        // 监听集合变化,更新 HasRecentProjects
        _recentProjects.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(HasRecentProjects));
        };
    }

    public WelcomeViewModel()
    {
        throw new NotImplementedException();
    }

    #region 属性

    /// <summary>
    /// 最近项目列表
    /// </summary>
    public ObservableCollection<ProjectConfig> RecentProjects
    {
        get => _recentProjects;
        set => SetProperty(ref _recentProjects, value);
    }

    /// <summary>
    /// 是否有最近项目
    /// </summary>
    public bool HasRecentProjects => RecentProjects.Count > 0;

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
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    #endregion

    #region 命令

    /// <summary>
    /// 创建新项目命令
    /// </summary>
    public ICommand CreateNewProjectCommand { get; }

    /// <summary>
    /// 打开项目命令
    /// </summary>
    public ICommand OpenProjectCommand { get; }

    /// <summary>
    /// 浏览项目命令
    /// </summary>
    public ICommand BrowseProjectCommand { get; }

    /// <summary>
    /// 清除错误命令
    /// </summary>
    public ICommand ClearErrorCommand { get; }

    #endregion

    #region 公共方法

    /// <summary>
    /// 加载最近项目
    /// </summary>
    public async Task LoadRecentProjectsAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var projects = await _projectService.GetRecentProjectsAsync();
            RecentProjects.Clear();

            foreach (var project in projects)
            {
                RecentProjects.Add(project);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"加载最近项目失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 浏览项目请求回调 (由 View 设置)
    /// </summary>
    public Func<Task<string?>>? OnBrowseProjectRequested { get; set; }

    #endregion

    #region 命令处理

    private void OnCreateNewProject()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("OnCreateNewProject called!");
            _navigationService.NavigateTo("CreateProjectView", null);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnCreateNewProject: {ex.Message}");
            ErrorMessage = $"创建项目失败: {ex.Message}";
        }
    }

    private bool CanOpenProject(ProjectConfig? project)
    {
        return project != null;
    }

    private async Task OnOpenProjectAsync(ProjectConfig? project)
    {
        if (project == null || string.IsNullOrEmpty(project.ProjectPath))
        {
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            System.Diagnostics.Debug.WriteLine($"Opening project: {project.ProjectPath}");

            var loadedProject = await _projectService.LoadProjectAsync(project.ProjectPath);
            if (loadedProject != null)
            {
                System.Diagnostics.Debug.WriteLine($"Project loaded successfully: {loadedProject.Name}");
                _navigationService.NavigateTo("MainWindow", loadedProject);
            }
            else
            {
                ErrorMessage = "无法加载项目配置";
                System.Diagnostics.Debug.WriteLine("Failed to load project config");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"打开项目失败: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Exception in OnOpenProjectAsync: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task OnBrowseProjectAsync()
    {
        if (OnBrowseProjectRequested == null)
        {
            return;
        }

        try
        {
            var selectedPath = await OnBrowseProjectRequested();
            if (string.IsNullOrEmpty(selectedPath))
            {
                return;
            }

            var project = await _projectService.LoadProjectAsync(selectedPath);
            _navigationService.NavigateTo("MainWindow", project);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"打开项目失败: {ex.Message}";
        }
    }

    private void OnClearError()
    {
        ErrorMessage = string.Empty;
    }

    #endregion
}

#region 辅助类

/// <summary>
/// 简单的同步 RelayCommand 实现
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// 异步 RelayCommand 实现
/// </summary>
public class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private bool _isExecuting;

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter)
    {
        await ExecuteAsync(parameter);
    }

    public async Task ExecuteAsync(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        try
        {
            _isExecuting = true;
            RaiseCanExecuteChanged();

            await _execute();
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// 泛型异步 RelayCommand 实现
/// </summary>
public class AsyncRelayCommand<T> : ICommand
{
    private readonly Func<T?, Task> _execute;
    private readonly Func<T?, bool>? _canExecute;
    private bool _isExecuting;

    public AsyncRelayCommand(Func<T?, Task> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        if (_isExecuting)
        {
            return false;
        }

        if (parameter is T typedParam)
        {
            return _canExecute?.Invoke(typedParam) ?? true;
        }

        return parameter == null && (_canExecute?.Invoke(default) ?? true);
    }

    public async void Execute(object? parameter)
    {
        await ExecuteAsync(parameter);
    }

    public async Task ExecuteAsync(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        try
        {
            _isExecuting = true;
            RaiseCanExecuteChanged();

            T? typedParam = parameter is T t ? t : default;
            await _execute(typedParam);
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

#endregion
