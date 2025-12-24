using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using Musicify.Core.Models;
using Musicify.Core.Services;
using Musicify.Core.ViewModels;
using Musicify.Desktop.Views;

namespace Musicify.Desktop.Services;

/// <summary>
/// 导航服务实现
/// </summary>
public class NavigationService : INavigationService
{
    private readonly Stack<Window> _navigationStack = new();
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 初始化导航服务实例
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// 导航到指定视图
    /// </summary>
    public void NavigateTo(string viewName, object? parameter = null)
    {
        // 获取当前的应用程序生命周期
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 对于CreateProjectView，使用对话框模式而不是替换主窗口
            if (viewName == "CreateProjectView")
            {
                ShowCreateProjectDialog();
                return;
            }

            Window? newWindow = viewName switch
            {
                "WelcomeWindow" => new WelcomeWindow(),
                "MainWindow" => CreateMainWindow(parameter),
                _ => null
            };

            if (newWindow != null)
            {
                if (viewName != "MainWindow") // MainWindow已经在CreateMainWindow中设置了DataContext
                {
                    newWindow.DataContext = parameter;
                }

                // 将新窗口添加到导航栈
                _navigationStack.Push(newWindow);

                // 保存旧窗口引用
                var oldWindow = desktop.MainWindow;
                
                // 1. 先显示新窗口
                newWindow.Show();
                newWindow.Activate();
                
                // 2. 设置新窗口为主窗口
                desktop.MainWindow = newWindow;
                
                // 3. 最后关闭旧窗口
                // 这确保了应用程序始终有一个主窗口，不会因为窗口切换而退出
                if (oldWindow != null && oldWindow != newWindow)
                {
                    oldWindow.Close();
                }
            }
        }
    }

    private void ShowCreateProjectDialog()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var createProjectView = new CreateProjectView();
            var viewModel = _serviceProvider.GetRequiredService<CreateProjectViewModel>();

            var dialog = new Window
            {
                Content = createProjectView,
                Title = "创建新项目 - Musicify",
                Width = 1200,
                Height = 800,
                MinWidth = 800,
                MinHeight = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ShowInTaskbar = false, // 对话框不显示在任务栏
                CanResize = true
            };

            // 订阅项目创建完成事件
            viewModel.ProjectCreated += (sender, project) =>
            {
                // 关闭对话框
                dialog.Close();

                // 导航到主窗口
                NavigateTo("MainWindow", project);
            };
            
            // 订阅取消事件
            viewModel.Canceled += (sender, args) =>
            {
                // 关闭对话框
                dialog.Close();
            };

            createProjectView.DataContext = viewModel;

            // 设置父窗口
            if (desktop.MainWindow != null)
            {
                dialog.ShowDialog(desktop.MainWindow);
            }
            else
            {
                dialog.Show();
            }
        }
    }

    private Window CreateProjectWindow()
    {
        var createProjectView = new CreateProjectView();
        var viewModel = _serviceProvider.GetRequiredService<CreateProjectViewModel>();
        createProjectView.DataContext = viewModel;

        var window = new Window
        {
            Content = createProjectView,
            Title = "创建新项目 - Musicify",
            Width = 1200,
            Height = 800,
            MinWidth = 800,
            MinHeight = 600,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ShowInTaskbar = true,
            CanResize = true
        };

        return window;
    }

    private Window CreateMainWindow(object? parameter)
    {
        try
        {
            Console.WriteLine("Creating MainWindow...");

            var mainWindow = new MainWindow();
            Console.WriteLine("MainWindow created successfully");

            var viewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
            Console.WriteLine("MainWindowViewModel retrieved from DI");

            // 如果传入了项目配置，设置到ViewModel中
            if (parameter is ProjectConfig project)
            {
                Console.WriteLine($"Setting project: {project.Name}");
                // 设置当前项目
                viewModel.CurrentProject = project;
                // 使用ViewModel的方法创建项目摘要
                viewModel.ProjectSummary = viewModel.CreateProjectSummary(project);
                Console.WriteLine("Project set successfully");
            }

            mainWindow.DataContext = viewModel;
            Console.WriteLine("DataContext set successfully");

            return mainWindow;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in CreateMainWindow: {ex}");
            throw;
        }
    }

    private ProjectSummary CreateProjectSummary(ProjectConfig project)
    {
        return new ProjectSummary(
            ProjectName: project.Name,
            Status: "活跃",
            SongType: project.Spec?.SongType ?? "未指定",
            CreatedAt: DateTime.Now, // ProjectConfig可能没有CreatedAt，使用当前时间
            UpdatedAt: DateTime.Now,
            HasMidiFile: false, // 这里可以后续检查MIDI文件是否存在
            HasLyrics: false // 这里可以后续检查歌词文件是否存在
        );
    }

    /// <summary>
    /// 返回上一个窗口
    /// </summary>
    /// <returns>如果成功返回则为 true，否则为 false</returns>
    public bool GoBack()
    {
        if (_navigationStack.Count > 1)
        {
            _navigationStack.Pop(); // 移除当前窗口

            if (_navigationStack.TryPeek(out var previousWindow))
            {
                // 获取应用程序生命周期并设置主窗口
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    if (desktop.MainWindow != null)
                    {
                        desktop.MainWindow.Close();
                    }

                    desktop.MainWindow = previousWindow;
                    previousWindow.Show();
                    previousWindow.Activate();
                }
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 获取是否可以返回上一个窗口
    /// </summary>
    public bool CanGoBack => _navigationStack.Count > 1;

    /// <summary>
    /// 关闭当前窗口
    /// </summary>
    public void CloseCurrentWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow?.Close();
        }
    }

    /// <summary>
    /// 显示对话框（异步）
    /// </summary>
    /// <param name="dialogName">对话框名称</param>
    /// <param name="parameter">传递给对话框的参数</param>
    /// <returns>对话框的结果</returns>
    public Task<object?> ShowDialogAsync(string dialogName, object? parameter = null)
    {
        // TODO: 实现对话框
        return Task.FromResult<object?>(null);
    }
}
