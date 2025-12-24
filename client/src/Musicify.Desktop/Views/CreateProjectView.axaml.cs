using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Musicify.Core.ViewModels;

namespace Musicify.Desktop.Views;

public partial class CreateProjectView : UserControl
{
    public CreateProjectView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (DataContext is CreateProjectViewModel viewModel)
        {
            // 设置文件浏览回调
            viewModel.OnBrowsePathRequested = BrowseForPathAsync;
            viewModel.OnBrowseMidiRequested = BrowseForMidiAsync;

            // 监听平台列表变化，更新 CheckBox 状态
            viewModel.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(CreateProjectViewModel.TargetPlatforms))
                {
                    UpdatePlatformCheckBoxes(viewModel);
                }
            };
        }
    }

    private void UpdatePlatformCheckBoxes(CreateProjectViewModel viewModel)
    {
        // 查找所有平台 CheckBox 并更新状态
        var itemsControl = this.FindControl<ItemsControl>("PlatformsItemsControl");
        if (itemsControl == null)
        {
            return;
        }

        // 这个方法会在 ItemsControl 加载后调用
        // 实际更新在 OnPlatformCheckBoxClick 中处理
    }

    private async Task<string?> BrowseForPathAsync()
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window?.StorageProvider == null)
        {
            return null;
        }

        var result = await window.StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
        {
            Title = "选择项目保存位置",
            AllowMultiple = false
        });

        return result.FirstOrDefault()?.Path.LocalPath;
    }

    private async Task<string?> BrowseForMidiAsync()
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window?.StorageProvider == null)
        {
            return null;
        }

        var result = await window.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = "选择 MIDI 文件",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new Avalonia.Platform.Storage.FilePickerFileType("MIDI 文件")
                {
                    Patterns = new[] { "*.mid", "*.midi" }
                },
                new Avalonia.Platform.Storage.FilePickerFileType("所有文件")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        });

        return result.FirstOrDefault()?.Path.LocalPath;
    }

    private void OnPlatformCheckBoxClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.Tag is string platform && DataContext is CreateProjectViewModel viewModel)
        {
            // 切换平台选择
            viewModel.TogglePlatform(platform);
            // 更新 CheckBox 状态以反映实际选择
            checkBox.IsChecked = viewModel.IsPlatformSelected(platform);
        }
    }

    private void OnCreationModeRadioButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is RadioButton radioButton && radioButton.Tag is string mode && DataContext is CreateProjectViewModel viewModel)
        {
            viewModel.CreationMode = mode;
        }
    }
}
