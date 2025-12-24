using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Musicify.Core.ViewModels;

namespace Musicify.Desktop.Views;

/// <summary>
/// 主窗口
/// </summary>
public partial class MainWindow : Window
{
    private ContentControl? _contentArea;
    private ProjectOverviewView? _projectOverviewView;
    private LyricsEditorView? _lyricsEditorView;
    private AIChatView? _aiChatView;
    private MidiAnalysisView? _midiAnalysisView;
    private ProjectSettingsView? _projectSettingsView;
    private ExportView? _exportView;
    private LyricsEditorViewModel? _lyricsEditorViewModel;
    private AIChatViewModel? _aiChatViewModel;
    private MidiAnalysisViewModel? _midiAnalysisViewModel;
    private ProjectSettingsViewModel? _projectSettingsViewModel;
    private ExportViewModel? _exportViewModel;

    /// <summary>
    /// 初始化 MainWindow 实例
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 使用指定的 ViewModel 初始化 MainWindow 实例
    /// </summary>
    /// <param name="viewModel">主窗口 ViewModel</param>
    public MainWindow(MainWindowViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        Console.WriteLine("InitializeComponent called");
        AvaloniaXamlLoader.Load(this);
        Console.WriteLine("AvaloniaXamlLoader.Load completed");

        // 获取 ContentArea 引用
        _contentArea = this.FindControl<ContentControl>("ContentArea");
        Console.WriteLine($"ContentArea found: {_contentArea != null}");

        // 监听 DataContext 变化
        this.DataContextChanged += OnDataContextChanged;
        Console.WriteLine("DataContextChanged event handler set");

        Console.WriteLine("InitializeComponent completed");
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        Console.WriteLine($"DataContext changed to: {DataContext?.GetType().Name}");

        if (DataContext is MainWindowViewModel viewModel)
        {
            Console.WriteLine("Setting up MainWindowViewModel property changed handler");
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            Console.WriteLine($"Current view: {viewModel.CurrentView}");
            UpdateContentView(viewModel.CurrentView);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.CurrentView) && sender is MainWindowViewModel viewModel)
        {
            UpdateContentView(viewModel.CurrentView);
        }
    }

    private void UpdateContentView(string currentView)
    {
        if (_contentArea == null)
        {
            return;
        }

        Console.WriteLine($"UpdateContentView called with: {currentView}");

        UserControl? targetView = null;

        // 延迟创建视图，只在需要时创建
        try
        {
            switch (currentView)
            {
                case "ProjectOverview":
                    Console.WriteLine("Creating ProjectOverviewView...");
                    if (_projectOverviewView == null)
                    {
                        _projectOverviewView = new ProjectOverviewView();
                    }

                    targetView = _projectOverviewView;
                    Console.WriteLine("ProjectOverviewView created successfully");
                    break;
                case "LyricsEditor":
                    Console.WriteLine("Creating LyricsEditorView...");
                    if (_lyricsEditorView == null)
                    {
                        _lyricsEditorView = new LyricsEditorView();
                    }

                    targetView = _lyricsEditorView;
                    Console.WriteLine("LyricsEditorView created successfully");
                    break;
                case "AIChat":
                    Console.WriteLine("Creating AIChatView...");
                    if (_aiChatView == null)
                    {
                        _aiChatView = new AIChatView();
                    }

                    targetView = _aiChatView;
                    Console.WriteLine("AIChatView created successfully");
                    break;
                case "MidiAnalysis":
                    Console.WriteLine("Creating MidiAnalysisView...");
                    if (_midiAnalysisView == null)
                    {
                        _midiAnalysisView = new MidiAnalysisView();
                    }

                    targetView = _midiAnalysisView;
                    Console.WriteLine("MidiAnalysisView created successfully");
                    break;
                case "ProjectSettings":
                    Console.WriteLine("Creating ProjectSettingsView...");
                    if (_projectSettingsView == null)
                    {
                        _projectSettingsView = new ProjectSettingsView();
                    }

                    targetView = _projectSettingsView;
                    Console.WriteLine("ProjectSettingsView created successfully");
                    break;
                case "Export":
                    Console.WriteLine("Creating ExportView...");
                    if (_exportView == null)
                    {
                        _exportView = new ExportView();
                    }

                    targetView = _exportView;
                    Console.WriteLine("ExportView created successfully");
                    break;
                default:
                    Console.WriteLine("Creating default ProjectOverviewView...");
                    if (_projectOverviewView == null)
                    {
                        _projectOverviewView = new ProjectOverviewView();
                    }

                    targetView = _projectOverviewView;
                    Console.WriteLine("Default ProjectOverviewView created successfully");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating view: {ex}");
            return;
        }

        if (targetView != null)
        {
            Console.WriteLine("Setting content area...");
            _contentArea.Content = targetView;
            Console.WriteLine("Content area set successfully");

            // 简化 ViewModel 设置，只在需要时获取
            var app = Application.Current as App;
            if (app?.Services != null)
            {
                try
                {
                    Console.WriteLine("Setting ViewModel...");
                    if (targetView is LyricsEditorView)
                    {
                        if (_lyricsEditorViewModel == null)
                        {
                            _lyricsEditorViewModel = app.Services.GetService<LyricsEditorViewModel>();
                        }

                        targetView.DataContext = _lyricsEditorViewModel;
                    }
                    else if (targetView is AIChatView)
                    {
                        if (_aiChatViewModel == null)
                        {
                            _aiChatViewModel = app.Services.GetService<AIChatViewModel>();
                        }

                        targetView.DataContext = _aiChatViewModel;
                    }
                    else if (targetView is MidiAnalysisView)
                    {
                        if (_midiAnalysisViewModel == null)
                        {
                            _midiAnalysisViewModel = app.Services.GetService<MidiAnalysisViewModel>();
                        }

                        targetView.DataContext = _midiAnalysisViewModel;
                    }
                    else if (targetView is ProjectSettingsView)
                    {
                        if (_projectSettingsViewModel == null)
                        {
                            _projectSettingsViewModel = app.Services.GetService<ProjectSettingsViewModel>();
                        }

                        targetView.DataContext = _projectSettingsViewModel;
                    }
                    else if (targetView is ExportView)
                    {
                        if (_exportViewModel == null)
                        {
                            _exportViewModel = app.Services.GetService<ExportViewModel>();
                        }

                        targetView.DataContext = _exportViewModel;
                    }
                    else
                    {
                        targetView.DataContext = DataContext;
                    }
                    Console.WriteLine("ViewModel set successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error setting ViewModel: {ex.Message}");
                    // 如果 ViewModel 创建失败，使用主 ViewModel
                    targetView.DataContext = DataContext;
                }
            }
            else
            {
                Console.WriteLine("App.Services is null, using main DataContext");
                targetView.DataContext = DataContext;
            }
        }
        Console.WriteLine("UpdateContentView completed");
    }

    /// <summary>
    /// 窗口打开时的处理
    /// </summary>
    /// <param name="e">事件参数</param>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // 如果 ViewModel 需要加载项目，在这里处理
        if (DataContext is MainWindowViewModel viewModel)
        {
            // 项目加载逻辑将在导航到主窗口时处理
        }
    }
}

