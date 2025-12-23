using Avalonia.Controls;
using Musicify.Core.Services;
using Musicify.Desktop.Views;

namespace Musicify.Desktop.Services;

/// <summary>
/// 导航服务实现
/// </summary>
public class NavigationService : INavigationService
{
    private readonly Stack<Window> _navigationStack = new();

    public void NavigateTo(string viewName, object? parameter = null)
    {
        Window? window = viewName switch
        {
            "WelcomeWindow" => new WelcomeWindow(),
            "CreateProjectView" => new Window { Content = new CreateProjectView() },
            "MainWindow" => new Window(), // TODO: 实现主窗口
            _ => null
        };

        if (window != null)
        {
            window.DataContext = parameter;
            _navigationStack.Push(window);
            window.Show();
        }
    }

    public bool GoBack()
    {
        if (_navigationStack.Count > 1)
        {
            var current = _navigationStack.Pop();
            current.Close();
            return true;
        }
        return false;
    }

    public bool CanGoBack => _navigationStack.Count > 1;

    public void CloseCurrentWindow()
    {
        if (_navigationStack.TryPeek(out var window))
        {
            window.Close();
            _navigationStack.Pop();
        }
    }

    public Task<object?> ShowDialogAsync(string dialogName, object? parameter = null)
    {
        // TODO: 实现对话框
        return Task.FromResult<object?>(null);
    }
}
