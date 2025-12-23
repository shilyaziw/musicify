using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Musicify.Core.ViewModels;
using Musicify.Desktop;
using System.ComponentModel;

namespace Musicify.Desktop.Views;

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

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        
        // 获取 ContentArea 引用
        _contentArea = this.FindControl<ContentControl>("ContentArea");
        
        // 初始化视图
        _projectOverviewView = new ProjectOverviewView();
        _lyricsEditorView = new LyricsEditorView();
        _aiChatView = new AIChatView();
        _midiAnalysisView = new MidiAnalysisView();
        _projectSettingsView = new ProjectSettingsView();
        _exportView = new ExportView();
        
        // 获取 ViewModels (从 DI 容器)
        var app = Application.Current as App;
        if (app?.Services != null)
        {
            _lyricsEditorViewModel = app.Services.GetService<LyricsEditorViewModel>();
            _aiChatViewModel = app.Services.GetService<AIChatViewModel>();
            _midiAnalysisViewModel = app.Services.GetService<MidiAnalysisViewModel>();
            _projectSettingsViewModel = app.Services.GetService<ProjectSettingsViewModel>();
            _exportViewModel = app.Services.GetService<ExportViewModel>();
        }
        
        // 监听 ViewModel 属性变化
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            
            // 当项目加载时,通知子 ViewModels
            viewModel.PropertyChanged += async (s, e) =>
            {
                if (e.PropertyName == nameof(MainWindowViewModel.CurrentProject) && 
                    viewModel.CurrentProject != null)
                {
                    if (_lyricsEditorViewModel != null)
                    {
                        await _lyricsEditorViewModel.SetProjectAsync(viewModel.CurrentProject);
                    }
                    
                    if (_aiChatViewModel != null)
                    {
                        _ = _aiChatViewModel.SetProjectAsync(viewModel.CurrentProject);
                    }
                    
                    if (_midiAnalysisViewModel != null)
                    {
                        _ = _midiAnalysisViewModel.SetProjectAsync(viewModel.CurrentProject);
                    }
                    
                    if (_projectSettingsViewModel != null)
                    {
                        _ = _projectSettingsViewModel.SetProjectAsync(viewModel.CurrentProject);
                    }
                    
                    if (_exportViewModel != null)
                    {
                        _ = _exportViewModel.SetProjectAsync(viewModel.CurrentProject);
                    }
                }
            };
            
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
        if (_contentArea == null) return;

        UserControl? targetView = currentView switch
        {
            "ProjectOverview" => _projectOverviewView,
            "LyricsEditor" => _lyricsEditorView,
            "AIChat" => _aiChatView,
            "MidiAnalysis" => _midiAnalysisView,
            "ProjectSettings" => _projectSettingsView,
            "Export" => _exportView,
            _ => _projectOverviewView
        };

        if (targetView != null)
        {
            _contentArea.Content = targetView;
            
            // 对于特殊视图,使用独立的 ViewModel
            if (targetView is LyricsEditorView && _lyricsEditorViewModel != null)
            {
                targetView.DataContext = _lyricsEditorViewModel;
                
                // 如果主窗口有项目,同步到歌词编辑器
                if (DataContext is MainWindowViewModel mainViewModel && 
                    mainViewModel.CurrentProject != null)
                {
                    _ = _lyricsEditorViewModel.SetProjectAsync(mainViewModel.CurrentProject);
                }
            }
            else if (targetView is AIChatView && _aiChatViewModel != null)
            {
                targetView.DataContext = _aiChatViewModel;
                
                // 如果主窗口有项目,同步到 AI 对话界面
                if (DataContext is MainWindowViewModel mainViewModel && 
                    mainViewModel.CurrentProject != null)
                {
                    _ = _aiChatViewModel.SetProjectAsync(mainViewModel.CurrentProject);
                }
            }
            else if (targetView is MidiAnalysisView && _midiAnalysisViewModel != null)
            {
                targetView.DataContext = _midiAnalysisViewModel;
                
                // 如果主窗口有项目,同步到 MIDI 分析界面
                if (DataContext is MainWindowViewModel mainViewModel && 
                    mainViewModel.CurrentProject != null)
                {
                    _ = _midiAnalysisViewModel.SetProjectAsync(mainViewModel.CurrentProject);
                }
            }
            else if (targetView is ProjectSettingsView && _projectSettingsViewModel != null)
            {
                targetView.DataContext = _projectSettingsViewModel;
                
                // 如果主窗口有项目,同步到项目设置界面
                if (DataContext is MainWindowViewModel mainViewModel && 
                    mainViewModel.CurrentProject != null)
                {
                    _ = _projectSettingsViewModel.SetProjectAsync(mainViewModel.CurrentProject);
                }
            }
            else if (targetView is ExportView && _exportViewModel != null)
            {
                targetView.DataContext = _exportViewModel;
                
                // 如果主窗口有项目,同步到导出界面
                if (DataContext is MainWindowViewModel mainViewModel && 
                    mainViewModel.CurrentProject != null)
                {
                    _ = _exportViewModel.SetProjectAsync(mainViewModel.CurrentProject);
                }
            }
            else
            {
                targetView.DataContext = DataContext;
            }
        }
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // 如果 ViewModel 需要加载项目，在这里处理
        if (DataContext is MainWindowViewModel viewModel)
        {
            // 项目加载逻辑将在导航到主窗口时处理
        }
    }
}

