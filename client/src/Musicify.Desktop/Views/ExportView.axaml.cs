using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Musicify.Core.ViewModels;
using Musicify.Desktop;

namespace Musicify.Desktop.Views;

public partial class ExportView : UserControl
{
    public ExportView()
    {
        InitializeComponent();

        // 从 DI 容器获取 ViewModel
        var app = Application.Current as App;
        if (app?.Services != null)
        {
            var viewModel = app.Services.GetService<ExportViewModel>();
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

