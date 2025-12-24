using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Musicify.Core.ViewModels;
using Musicify.Desktop;

namespace Musicify.Desktop.Views;

public partial class ProjectSettingsView : UserControl
{
    public ProjectSettingsView()
    {
        InitializeComponent();

        // 从 DI 容器获取 ViewModel
        var app = Application.Current as App;
        if (app?.Services != null)
        {
            var viewModel = app.Services.GetService<ProjectSettingsViewModel>();
            if (viewModel != null)
            {
                DataContext = viewModel;
            }
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        // 处理平台复选框的点击事件
        this.AddHandler(CheckBox.IsCheckedChangedEvent, OnPlatformCheckedChanged, RoutingStrategies.Bubble);
    }

    private void OnPlatformCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (e.Source is CheckBox checkBox &&
            checkBox.Tag is string platform &&
            DataContext is ProjectSettingsViewModel viewModel)
        {
            if (checkBox.IsChecked == true)
            {
                if (!viewModel.SelectedPlatforms.Contains(platform))
                {
                    viewModel.SelectedPlatforms.Add(platform);
                }
            }
            else
            {
                viewModel.SelectedPlatforms.Remove(platform);
            }
        }
    }
}
