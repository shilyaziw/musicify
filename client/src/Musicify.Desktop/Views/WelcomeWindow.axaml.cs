using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Diagnostics;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Musicify.Core.Models;
using Musicify.Core.ViewModels;

namespace Musicify.Desktop.Views;

public partial class WelcomeWindow : Window
{
    public WelcomeWindow()
    {
        InitializeComponent();
#if DEBUG
        // DevTools 在 Avalonia 11 中需要不同的方式启用
        // this.AttachDevTools();
#endif
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // 加载最近项目
        if (DataContext is WelcomeViewModel viewModel)
        {
            // 设置文件浏览回调
            viewModel.OnBrowseProjectRequested = BrowseForProjectAsync;

            // 加载最近项目
            await viewModel.LoadRecentProjectsAsync();
        }
    }

    private void OnProjectItemClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ProjectConfig project && DataContext is WelcomeViewModel viewModel)
        {
            viewModel.OpenProjectCommand.Execute(project);
        }
    }

    private async Task<string?> BrowseForProjectAsync()
    {
        if (StorageProvider == null)
        {
            return null;
        }

        var result = await StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
        {
            Title = "选择项目文件夹",
            AllowMultiple = false
        });

        return result.FirstOrDefault()?.Path.LocalPath;
    }
}
