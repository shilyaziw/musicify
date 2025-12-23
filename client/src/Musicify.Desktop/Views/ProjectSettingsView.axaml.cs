using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
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
        this.AddHandler(CheckBox.CheckedEvent, OnPlatformChecked, RoutingStrategies.Bubble);
        this.AddHandler(CheckBox.UncheckedEvent, OnPlatformUnchecked, RoutingStrategies.Bubble);
    }
    
    private void OnPlatformChecked(object? sender, RoutedEventArgs e)
    {
        if (e.Source is CheckBox checkBox && 
            checkBox.Tag is string platform &&
            DataContext is ProjectSettingsViewModel viewModel)
        {
            if (!viewModel.SelectedPlatforms.Contains(platform))
            {
                viewModel.SelectedPlatforms.Add(platform);
            }
        }
    }
    
    private void OnPlatformUnchecked(object? sender, RoutedEventArgs e)
    {
        if (e.Source is CheckBox checkBox && 
            checkBox.Tag is string platform &&
            DataContext is ProjectSettingsViewModel viewModel)
        {
            viewModel.SelectedPlatforms.Remove(platform);
        }
    }
}
