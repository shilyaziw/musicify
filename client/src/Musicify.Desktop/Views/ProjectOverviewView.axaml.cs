using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Musicify.Desktop.Views;

public partial class ProjectOverviewView : UserControl
{
    public ProjectOverviewView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

