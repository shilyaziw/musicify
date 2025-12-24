using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Musicify.Core.ViewModels;
using Musicify.Desktop;

namespace Musicify.Desktop.Views;

public partial class MidiAnalysisView : UserControl
{
    public MidiAnalysisView()
    {
        InitializeComponent();

        // 从 DI 容器获取 ViewModel
        var app = Application.Current as App;
        if (app?.Services != null)
        {
            var viewModel = app.Services.GetService<MidiAnalysisViewModel>();
            if (viewModel != null)
            {
                DataContext = viewModel;
            }
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

