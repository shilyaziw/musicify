using System.IO;
using System.Linq;
using System.Windows.Input;
using Musicify.Core.Abstractions;
using Musicify.Core.Models;
using Musicify.Core.Services;

namespace Musicify.Core.ViewModels;

/// <summary>
/// MIDI 分析界面 ViewModel
/// </summary>
public class MidiAnalysisViewModel : ViewModelBase
{
    private readonly IMidiAnalysisService _midiAnalysisService;
    private readonly IFileSystem _fileSystem;
    private readonly IFileDialogService? _fileDialogService;
    
    private ProjectConfig? _currentProject;
    private MidiFileInfo? _midiFileInfo;
    private MidiAnalysisResult? _analysisResult;
    private bool _isAnalyzing;
    private string? _errorMessage;
    private string? _selectedMidiFilePath;

    public MidiAnalysisViewModel(
        IMidiAnalysisService midiAnalysisService,
        IFileSystem fileSystem,
        IFileDialogService? fileDialogService = null)
    {
        _midiAnalysisService = midiAnalysisService ?? throw new ArgumentNullException(nameof(midiAnalysisService));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _fileDialogService = fileDialogService;

        // 初始化命令
        SelectMidiFileCommand = new RelayCommand(SelectMidiFile);
        AnalyzeMidiFileCommand = new AsyncRelayCommand(AnalyzeMidiFileAsync, CanAnalyze);
        LoadMidiFileCommand = new AsyncRelayCommand(LoadMidiFileAsync);
    }

    #region 属性

    /// <summary>
    /// 当前项目配置
    /// </summary>
    public ProjectConfig? CurrentProject
    {
        get => _currentProject;
        set => SetProperty(ref _currentProject, value);
    }

    /// <summary>
    /// MIDI 文件信息
    /// </summary>
    public MidiFileInfo? MidiFileInfo
    {
        get => _midiFileInfo;
        private set => SetProperty(ref _midiFileInfo, value);
    }

    /// <summary>
    /// MIDI 分析结果
    /// </summary>
    public MidiAnalysisResult? AnalysisResult
    {
        get => _analysisResult;
        private set => SetProperty(ref _analysisResult, value);
    }

    /// <summary>
    /// 是否正在分析
    /// </summary>
    public bool IsAnalyzing
    {
        get => _isAnalyzing;
        private set => SetProperty(ref _isAnalyzing, value);
    }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>
    /// 选中的 MIDI 文件路径
    /// </summary>
    public string? SelectedMidiFilePath
    {
        get => _selectedMidiFilePath;
        set => SetProperty(ref _selectedMidiFilePath, value);
    }

    #endregion

    #region 命令

    /// <summary>
    /// 选择 MIDI 文件命令
    /// </summary>
    public ICommand SelectMidiFileCommand { get; }

    /// <summary>
    /// 分析 MIDI 文件命令
    /// </summary>
    public ICommand AnalyzeMidiFileCommand { get; }

    /// <summary>
    /// 加载 MIDI 文件命令
    /// </summary>
    public ICommand LoadMidiFileCommand { get; }

    #endregion

    #region 公共方法

    /// <summary>
    /// 设置当前项目
    /// </summary>
    public async Task SetProjectAsync(ProjectConfig project)
    {
        CurrentProject = project;
        await LoadMidiFileAsync();
    }

    #endregion

    #region 命令实现

    /// <summary>
    /// 选择 MIDI 文件
    /// </summary>
    private async void SelectMidiFile()
    {
        string? initialDirectory = null;
        
        // 优先使用项目目录
        if (CurrentProject != null && !string.IsNullOrWhiteSpace(CurrentProject.ProjectPath))
        {
            var midiDir = Path.Combine(CurrentProject.ProjectPath, "melody", "midi");
            if (_fileSystem.DirectoryExists(midiDir))
            {
                initialDirectory = midiDir;
            }
            else
            {
                initialDirectory = CurrentProject.ProjectPath;
            }
        }

        // 如果有文件对话框服务，使用它
        if (_fileDialogService != null)
        {
            var selectedPath = await _fileDialogService.ShowOpenFileDialogAsync(
                title: "选择 MIDI 文件",
                filters: "MIDI 文件|*.mid;*.midi|所有文件|*.*",
                initialDirectory: initialDirectory);
            
            if (!string.IsNullOrWhiteSpace(selectedPath))
            {
                SelectedMidiFilePath = selectedPath;
                _ = LoadMidiFileAsync();
            }
        }
        else
        {
            // 回退到自动查找
            if (CurrentProject != null && !string.IsNullOrWhiteSpace(CurrentProject.ProjectPath))
            {
                var midiDir = Path.Combine(CurrentProject.ProjectPath, "melody", "midi");
                if (_fileSystem.DirectoryExists(midiDir))
                {
                    try
                    {
                        var midiFiles = Directory.GetFiles(midiDir, "*.mid", SearchOption.TopDirectoryOnly)
                            .Concat(Directory.GetFiles(midiDir, "*.midi", SearchOption.TopDirectoryOnly))
                            .FirstOrDefault();
                        
                        if (!string.IsNullOrEmpty(midiFiles))
                        {
                            SelectedMidiFilePath = midiFiles;
                            _ = LoadMidiFileAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage = $"查找 MIDI 文件失败: {ex.Message}";
                    }
                }
            }
        }
    }

    /// <summary>
    /// 加载 MIDI 文件信息
    /// </summary>
    private async Task LoadMidiFileAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedMidiFilePath))
        {
            // 尝试从项目目录加载
            if (CurrentProject != null && !string.IsNullOrWhiteSpace(CurrentProject.ProjectPath))
            {
                var midiDir = Path.Combine(CurrentProject.ProjectPath, "melody", "midi");
                if (_fileSystem.DirectoryExists(midiDir))
                {
                    try
                    {
                        var midiFiles = Directory.GetFiles(midiDir, "*.mid", SearchOption.TopDirectoryOnly)
                            .Concat(Directory.GetFiles(midiDir, "*.midi", SearchOption.TopDirectoryOnly))
                            .FirstOrDefault();
                        
                        if (!string.IsNullOrEmpty(midiFiles))
                        {
                            SelectedMidiFilePath = midiFiles;
                        }
                    }
                    catch
                    {
                        // 忽略错误
                    }
                }
            }
        }

        if (string.IsNullOrWhiteSpace(SelectedMidiFilePath) || !_fileSystem.FileExists(SelectedMidiFilePath))
        {
            MidiFileInfo = null;
            AnalysisResult = null;
            return;
        }

        try
        {
            ErrorMessage = null;
            MidiFileInfo = await _midiAnalysisService.GetFileInfoAsync(SelectedMidiFilePath);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"加载 MIDI 文件失败: {ex.Message}";
            MidiFileInfo = null;
        }
    }

    /// <summary>
    /// 分析 MIDI 文件
    /// </summary>
    private async Task AnalyzeMidiFileAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedMidiFilePath))
        {
            ErrorMessage = "请先选择 MIDI 文件";
            return;
        }

        try
        {
            IsAnalyzing = true;
            ErrorMessage = null;

            AnalysisResult = await _midiAnalysisService.AnalyzeAsync(SelectedMidiFilePath);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"分析失败: {ex.Message}";
            AnalysisResult = null;
        }
        finally
        {
            IsAnalyzing = false;
        }
    }

    /// <summary>
    /// 是否可以分析
    /// </summary>
    private bool CanAnalyze()
    {
        return !string.IsNullOrWhiteSpace(SelectedMidiFilePath) && !IsAnalyzing;
    }

    #endregion
}

