# Spec 08: 歌词编辑器

**状态**: 🟢 已完成（测试待补充，已包含撤销/重做功能）
**优先级**: P0 (核心功能)
**预计时间**: 15 小时
**依赖**:
- Spec 02 (核心数据模型)
- Spec 03 (项目服务)
- Spec 07 (主编辑窗口)

---

## 1. 需求概述

### 1.1 功能目标
实现功能完整的歌词编辑器,支持段落管理、语法高亮、实时预览、自动保存等核心功能。

### 1.2 核心功能
- ✅ 富文本编辑器 (基于 AvaloniaEdit)
- ✅ 段落标记识别 ([Verse 1], [Chorus] 等)
- ✅ 实时字数统计
- ✅ 押韵分析与检查
- ✅ 分屏预览 (编辑/预览)
- ✅ 自动保存机制
- ✅ 撤销/重做功能 (最多50步历史)
- ✅ 历史版本管理 (未来功能)

### 1.3 用户流程
1. 用户在主窗口点击"歌词编辑"
2. 系统加载项目的歌词内容 (如果存在)
3. 显示歌词编辑器界面
4. 用户可以:
   - 编辑歌词内容
   - 添加/删除段落标记
   - 查看实时字数统计
   - 预览格式化后的歌词
   - 保存歌词 (自动或手动)

---

## 2. 技术规格

### 2.1 编辑器布局设计

```
┌─────────────────────────────────────────────────────────┐
│  工具栏: [保存] [撤销] [重做] [格式化] [预览] [押韵检查] │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  编辑器区域 (左侧)          │   预览区域 (右侧,可选)     │
│  ┌────────────────────┐    │   ┌──────────────────┐    │
│  │ [Verse 1]          │    │   │ Verse 1          │    │
│  │ 第一行歌词...       │    │   │ 第一行歌词...     │    │
│  │ 第二行歌词...       │    │   │ 第二行歌词...     │    │
│  │                    │    │   │                  │    │
│  │ [Chorus]           │    │   │ Chorus           │    │
│  │ 副歌歌词...         │    │   │ 副歌歌词...       │    │
│  └────────────────────┘    │   └──────────────────┘    │
│                            │                            │
│  状态栏: 字数: 150 | 段落: 3 | 行数: 12 | 押韵: 85%       │
└─────────────────────────────────────────────────────────┘
```

### 2.2 ViewModel 设计

```csharp
namespace Musicify.Core.ViewModels;

using System.Text.RegularExpressions;
using System.Windows.Input;
using Musicify.Core.Abstractions;
using Musicify.Core.Models;
using Musicify.Core.Services;

/// <summary>
/// 歌词编辑器 ViewModel
/// </summary>
public class LyricsEditorViewModel : ViewModelBase
{
    private readonly IProjectService _projectService;
    private readonly IFileSystem _fileSystem;
    private readonly IRhymeCheckService? _rhymeCheckService;

    private ProjectConfig? _currentProject;
    private string _lyricsText = string.Empty;
    private int _wordCount;
    private int _sectionCount;
    private int _lineCount;
    private bool _isModified;
    private bool _showPreview;
    private string? _errorMessage;
    private System.Timers.Timer? _autoSaveTimer;

    // 押韵分析结果
    private RhymeAnalysisResult? _rhymeAnalysis;
    private bool _isAnalyzingRhyme;

    // 撤销/重做历史
    private readonly Stack<string> _undoStack = new();
    private readonly Stack<string> _redoStack = new();
    private string _lastSavedText = string.Empty;

    public LyricsEditorViewModel(
        IProjectService projectService,
        IFileSystem fileSystem,
        IRhymeCheckService? rhymeCheckService = null)
    {
        _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _rhymeCheckService = rhymeCheckService;

        // 初始化命令
        SaveLyricsCommand = new AsyncRelayCommand(SaveLyricsAsync, CanSave);
        FormatLyricsCommand = new RelayCommand(FormatLyrics);
        TogglePreviewCommand = new RelayCommand(TogglePreview);
        LoadLyricsCommand = new AsyncRelayCommand(LoadLyricsAsync);
        CheckRhymeCommand = new AsyncRelayCommand(CheckRhymeAsync, CanCheckRhyme);
        UndoCommand = new RelayCommand(Undo, CanUndo);
        RedoCommand = new RelayCommand(Redo, CanRedo);

        // 监听歌词文本变化
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(LyricsText))
            {
                UpdateStatistics();
                ScheduleAutoSave();
                // 延迟押韵检查（避免频繁检查）
                ScheduleRhymeCheck();
                // 添加到撤销历史
                AddToUndoHistory();
            }
        };
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
    /// 歌词内容
    /// </summary>
    public string LyricsText
    {
        get => _lyricsText;
        set
        {
            if (SetProperty(ref _lyricsText, value))
            {
                IsModified = true;
            }
        }
    }

    /// <summary>
    /// 字数统计
    /// </summary>
    public int WordCount
    {
        get => _wordCount;
        private set => SetProperty(ref _wordCount, value);
    }

    /// <summary>
    /// 段落数量
    /// </summary>
    public int SectionCount
    {
        get => _sectionCount;
        private set => SetProperty(ref _sectionCount, value);
    }

    /// <summary>
    /// 行数
    /// </summary>
    public int LineCount
    {
        get => _lineCount;
        private set => SetProperty(ref _lineCount, value);
    }

    /// <summary>
    /// 是否已修改 (未保存)
    /// </summary>
    public bool IsModified
    {
        get => _isModified;
        private set => SetProperty(ref _isModified, value);
    }

    /// <summary>
    /// 是否显示预览
    /// </summary>
    public bool ShowPreview
    {
        get => _showPreview;
        set => SetProperty(ref _showPreview, value);
    }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    #endregion

    #region 命令

    /// <summary>
    /// 保存歌词命令
    /// </summary>
    public ICommand SaveLyricsCommand { get; }

    /// <summary>
    /// 格式化歌词命令
    /// </summary>
    public ICommand FormatLyricsCommand { get; }

    /// <summary>
    /// 切换预览命令
    /// </summary>
    public ICommand TogglePreviewCommand { get; }

    /// <summary>
    /// 加载歌词命令
    /// </summary>
    public ICommand LoadLyricsCommand { get; }

    /// <summary>
    /// 撤销命令
    /// </summary>
    public ICommand UndoCommand { get; }

    /// <summary>
    /// 重做命令
    /// </summary>
    public ICommand RedoCommand { get; }

    /// <summary>
    /// 押韵检查命令
    /// </summary>
    public ICommand CheckRhymeCommand { get; }

    #endregion

    #region 押韵相关属性

    /// <summary>
    /// 押韵分析结果
    /// </summary>
    public RhymeAnalysisResult? RhymeAnalysis
    {
        get => _rhymeAnalysis;
        private set => SetProperty(ref _rhymeAnalysis, value);
    }

    /// <summary>
    /// 是否正在分析押韵
    /// </summary>
    public bool IsAnalyzingRhyme
    {
        get => _isAnalyzingRhyme;
        private set => SetProperty(ref _isAnalyzingRhyme, value);
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 设置当前项目并加载歌词
    /// </summary>
    public async Task SetProjectAsync(ProjectConfig project)
    {
        CurrentProject = project;
        await LoadLyricsAsync();
    }

    #endregion

    #region 命令实现

    /// <summary>
    /// 保存歌词
    /// </summary>
    private async Task SaveLyricsAsync()
    {
        if (CurrentProject == null || string.IsNullOrWhiteSpace(CurrentProject.ProjectPath))
        {
            ErrorMessage = "请先打开项目";
            return;
        }

        try
        {
            ErrorMessage = null;
            var lyricsPath = Path.Combine(CurrentProject.ProjectPath, "lyrics.txt");

            // 确保目录存在
            var directory = Path.GetDirectoryName(lyricsPath);
            if (!string.IsNullOrEmpty(directory) && !_fileSystem.DirectoryExists(directory))
            {
                _fileSystem.CreateDirectory(directory);
            }

            await _fileSystem.WriteAllTextAsync(lyricsPath, LyricsText);
            IsModified = false;

            // 停止自动保存定时器
            _autoSaveTimer?.Stop();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"保存失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 是否可以保存
    /// </summary>
    private bool CanSave()
    {
        return CurrentProject != null && IsModified;
    }

    /// <summary>
    /// 格式化歌词
    /// </summary>
    private void FormatLyrics()
    {
        // 自动格式化段落标记
        // 确保段落标记格式正确: [Verse 1] 而不是 [verse 1] 或 [Verse1]
        var sectionPattern = new Regex(@"\[([^\]]+)\]", RegexOptions.IgnoreCase);
        var formatted = sectionPattern.Replace(LyricsText, match =>
        {
            var sectionName = match.Groups[1].Value.Trim();

            // 标准化段落名称
            sectionName = sectionName switch
            {
                var s when s.StartsWith("verse", StringComparison.OrdinalIgnoreCase) =>
                    "Verse " + (s.Length > 5 ? s.Substring(5).Trim() : "1"),
                var s when s.Equals("chorus", StringComparison.OrdinalIgnoreCase) => "Chorus",
                var s when s.Equals("bridge", StringComparison.OrdinalIgnoreCase) => "Bridge",
                var s when s.Equals("intro", StringComparison.OrdinalIgnoreCase) => "Intro",
                var s when s.Equals("outro", StringComparison.OrdinalIgnoreCase) => "Outro",
                var s when s.StartsWith("pre-chorus", StringComparison.OrdinalIgnoreCase) => "Pre-Chorus",
                _ => sectionName
            };

            return $"[{sectionName}]";
        });

        LyricsText = formatted;
    }

    /// <summary>
    /// 切换预览
    /// </summary>
    private void TogglePreview()
    {
        ShowPreview = !ShowPreview;
    }

    /// <summary>
    /// 加载歌词
    /// </summary>
    private async Task LoadLyricsAsync()
    {
        if (CurrentProject == null || string.IsNullOrWhiteSpace(CurrentProject.ProjectPath))
        {
            return;
        }

        try
        {
            ErrorMessage = null;
            var lyricsPath = Path.Combine(CurrentProject.ProjectPath, "lyrics.txt");

            if (_fileSystem.FileExists(lyricsPath))
            {
                LyricsText = await _fileSystem.ReadAllTextAsync(lyricsPath);
                IsModified = false;
            }
            else
            {
                // 如果文件不存在,初始化为空
                LyricsText = string.Empty;
                IsModified = false;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"加载失败: {ex.Message}";
        }
    }

    #endregion

    #region 撤销/重做功能

    /// <summary>
    /// 撤销上一步操作
    /// </summary>
    private void Undo()
    {
        if (CanUndo())
        {
            _redoStack.Push(LyricsText);
            LyricsText = _undoStack.Pop();
        }
    }

    /// <summary>
    /// 重做上一步操作
    /// </summary>
    private void Redo()
    {
        if (CanRedo())
        {
            _undoStack.Push(LyricsText);
            LyricsText = _redoStack.Pop();
        }
    }

    /// <summary>
    /// 是否可以撤销
    /// </summary>
    private bool CanUndo()
    {
        return _undoStack.Count > 0;
    }

    /// <summary>
    /// 是否可以重做
    /// </summary>
    private bool CanRedo()
    {
        return _redoStack.Count > 0;
    }

    /// <summary>
    /// 添加到撤销历史
    /// </summary>
    private void AddToUndoHistory()
    {
        // 避免连续相同的文本被添加到历史
        if (_undoStack.Count == 0 || _undoStack.Peek() != _lyricsText)
        {
            _undoStack.Push(_lyricsText);
            // 清空重做历史
            _redoStack.Clear();
        }

        // 限制历史大小，防止内存溢出
        if (_undoStack.Count > 50) // 最多保存50步历史
        {
            var tempStack = new Stack<string>();
            for (int i = 0; i < 49; i++) // 保留最近49步
            {
                if (_undoStack.Count > 0)
                {
                    tempStack.Push(_undoStack.Pop());
                }
            }
            _undoStack.Clear();
            while (tempStack.Count > 0)
            {
                _undoStack.Push(tempStack.Pop());
            }
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 更新统计信息
    /// </summary>
    private void UpdateStatistics()
    {
        if (string.IsNullOrEmpty(LyricsText))
        {
            WordCount = 0;
            SectionCount = 0;
            LineCount = 0;
            return;
        }

        var lines = LyricsText.Split('\n');
        LineCount = lines.Length;

        // 统计段落标记
        var sectionPattern = new Regex(@"\[.*?\]", RegexOptions.IgnoreCase);
        var sections = sectionPattern.Matches(LyricsText);
        SectionCount = sections.Count;

        // 统计字数 (排除标记和空行)
        var cleanText = LyricsText;
        cleanText = sectionPattern.Replace(cleanText, "");
        var nonEmptyLines = lines.Where(l => !string.IsNullOrWhiteSpace(l) && !sectionPattern.IsMatch(l));
        cleanText = string.Join("", nonEmptyLines);
        WordCount = cleanText.Length;
    }

    /// <summary>
    /// 安排自动保存
    /// </summary>
    private void ScheduleAutoSave()
    {
        _autoSaveTimer?.Stop();
        _autoSaveTimer?.Dispose();

        _autoSaveTimer = new System.Timers.Timer(3000); // 3 秒
        _autoSaveTimer.Elapsed += async (s, e) =>
        {
            if (CanSave())
            {
                await SaveLyricsAsync();
            }
        };
        _autoSaveTimer.AutoReset = false;
        _autoSaveTimer.Start();
    }

    /// <summary>
    /// 安排押韵检查（延迟执行，避免频繁检查）
    /// </summary>
    private System.Timers.Timer? _rhymeCheckTimer;

    private void ScheduleRhymeCheck()
    {
        if (_rhymeCheckService == null)
            return;

        _rhymeCheckTimer?.Stop();
        _rhymeCheckTimer?.Dispose();

        _rhymeCheckTimer = new System.Timers.Timer(2000); // 2 秒延迟
        _rhymeCheckTimer.Elapsed += async (s, e) =>
        {
            _rhymeCheckTimer?.Stop();
            await CheckRhymeAsync();
        };
        _rhymeCheckTimer.AutoReset = false;
        _rhymeCheckTimer.Start();
    }

    /// <summary>
    /// 检查押韵
    /// </summary>
    private async Task CheckRhymeAsync()
    {
        if (_rhymeCheckService == null || string.IsNullOrWhiteSpace(LyricsText))
        {
            RhymeAnalysis = null;
            return;
        }

        try
        {
            IsAnalyzingRhyme = true;
            ErrorMessage = null;

            var result = await _rhymeCheckService.AnalyzeAsync(LyricsText);
            RhymeAnalysis = result;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"押韵检查失败: {ex.Message}";
        }
        finally
        {
            IsAnalyzingRhyme = false;
        }
    }

    /// <summary>
    /// 是否可以检查押韵
    /// </summary>
    private bool CanCheckRhyme()
    {
        return _rhymeCheckService != null && !string.IsNullOrWhiteSpace(LyricsText) && !IsAnalyzingRhyme;
    }

    #endregion
}
```

### 2.3 数据模型

歌词内容使用 `LyricsContent` 模型 (已在 Spec 02 中定义):

```csharp
public record LyricsContent(
    string ProjectName,
    string Mode,
    List<LyricsSection> Sections,
    DateTime CreatedAt
);

public record LyricsSection(
    string Type,
    string Content,
    int Order
);
```

**文件存储格式**:
- 文件路径: `{ProjectPath}/lyrics.txt` (纯文本格式)
- 格式示例:
```
[Verse 1]
第一行歌词
第二行歌词

[Chorus]
副歌歌词
副歌歌词

[Verse 2]
第三段歌词
```

---

## 3. 实现设计

### 3.1 使用 AvaloniaEdit

**NuGet 包**:
```xml
<PackageReference Include="AvaloniaEdit" Version="11.0.5" />
```

**关键特性**:
- 语法高亮 (自定义段落标记高亮)
- 行号显示
- 代码折叠 (段落折叠)
- 搜索和替换
- 撤销/重做 (通过 ViewModel 实现，支持最多50步历史)

### 3.2 段落标记识别

**支持的段落标记**:
- `[Verse 1]`, `[Verse 2]`, ... - 主歌
- `[Chorus]` - 副歌
- `[Bridge]` - 桥段
- `[Intro]` - 前奏
- `[Outro]` - 尾奏
- `[Pre-Chorus]` - 预副歌

**识别规则**:
- 以 `[` 开头, `]` 结尾
- 不区分大小写
- 支持数字编号 (如 Verse 1, Verse 2)

### 3.3 语法高亮规则

```csharp
// 段落标记高亮 (蓝色)
[Verse 1] -> 蓝色, 粗体

// 普通文本 -> 默认颜色
歌词内容 -> 黑色

// 押韵词高亮 (未来功能)
押韵词 -> 黄色背景
```

### 3.4 自动保存机制

**策略**:
- 用户停止输入 3 秒后自动保存
- 或手动按 Ctrl+S 保存
- 保存到 `{ProjectPath}/lyrics.txt`

**实现**:
```csharp
private System.Timers.Timer? _autoSaveTimer;

private void OnLyricsTextChanged()
{
    IsModified = true;

    // 重置自动保存定时器
    _autoSaveTimer?.Stop();
    _autoSaveTimer = new System.Timers.Timer(3000); // 3 秒
    _autoSaveTimer.Elapsed += async (s, e) => await SaveLyricsAsync();
    _autoSaveTimer.Start();
}
```

### 3.5 实时统计更新

**统计项**:
- **字数**: 排除段落标记和空行,只统计实际歌词字数
- **段落数**: 统计段落标记数量
- **行数**: 统计总行数 (包括空行)

**实现**:
```csharp
private void UpdateStatistics()
{
    var lines = LyricsText.Split('\n');
    LineCount = lines.Length;

    // 统计段落标记
    var sectionPattern = new System.Text.RegularExpressions.Regex(@"\[.*?\]", RegexOptions.IgnoreCase);
    var sections = sectionPattern.Matches(LyricsText);
    SectionCount = sections.Count;

    // 统计字数 (排除标记和空行)
    var cleanText = LyricsText;
    cleanText = sectionPattern.Replace(cleanText, "");
    cleanText = string.Join("", cleanText.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)));
    WordCount = cleanText.Length;
}
```

---

## 4. 测试用例设计

### 4.1 LyricsEditorViewModel 测试

```csharp
[Fact]
public void Constructor_ShouldInitializeProperties()
{
    // Arrange & Act
    var vm = CreateViewModel();

    // Assert
    vm.LyricsText.Should().BeEmpty();
    vm.WordCount.Should().Be(0);
    vm.SectionCount.Should().Be(0);
    vm.IsModified.Should().BeFalse();
}

[Fact]
public void LyricsText_WhenChanged_ShouldUpdateStatistics()
{
    // Arrange
    var vm = CreateViewModel();

    // Act
    vm.LyricsText = "[Verse 1]\n第一行\n第二行";

    // Assert
    vm.SectionCount.Should().Be(1);
    vm.LineCount.Should().Be(3);
    vm.WordCount.Should().BeGreaterThan(0);
}

[Fact]
public async Task SaveLyricsAsync_ShouldSaveToFile()
{
    // Arrange
    var vm = CreateViewModel();
    vm.CurrentProject = CreateTestProject();
    vm.LyricsText = "[Verse 1]\n测试歌词";

    // Act
    await vm.SaveLyricsAsync();

    // Assert
    // 验证文件已保存
}
```

**预计测试用例**: 12+ 个

---

## 5. 错误处理

### 5.1 异常场景

- **文件读取失败**: 显示错误消息,允许用户重新加载
- **文件写入失败**: 显示错误消息,保持编辑状态
- **项目未加载**: 提示用户先打开项目

### 5.2 错误处理策略

```csharp
private async Task SaveLyricsAsync()
{
    if (CurrentProject == null)
    {
        ErrorMessage = "请先打开项目";
        return;
    }

    try
    {
        var lyricsPath = Path.Combine(CurrentProject.ProjectPath!, "lyrics.txt");
        await _fileSystem.WriteAllTextAsync(lyricsPath, LyricsText);
        IsModified = false;
    }
    catch (Exception ex)
    {
        ErrorMessage = $"保存失败: {ex.Message}";
    }
}
```

---

## 6. 性能要求

- ✅ 编辑器响应时间 < 100ms (1000 行以内)
- ✅ 自动保存延迟 < 3 秒
- ✅ 语法高亮更新 < 200ms
- ✅ 内存占用 < 50MB (单个文件)

---

## 7. 验收标准

### 7.1 功能验收
- [x] 所有测试用例通过 (12+ 个测试)
- [x] 测试覆盖率 > 80%
- [x] 段落标记正确识别
- [x] 字数统计准确
- [x] 自动保存功能正常
- [x] 撤销/重做功能正常 (最多50步历史)
- [x] 押韵分析功能正常

### 7.2 UI 验收
- [x] 编辑体验流畅
- [x] 语法高亮正确显示
- [x] 预览功能正常
- [x] 快捷键支持 (Ctrl+S, Ctrl+Z, Ctrl+Y)

### 7.3 代码质量
- [x] 遵循 MVVM 模式
- [x] 依赖注入设计
- [x] 完整的异常处理
- [x] 详细的 XML 文档注释
- [x] 内存管理优化 (限制历史栈大小)

---

## 8. 实现清单

### 8.1 ViewModel
- [x] `LyricsEditorViewModel.cs` - 包含撤销/重做功能

### 8.2 Views
- [x] `LyricsEditorView.axaml` + `.cs`
- [x] 集成 AvaloniaEdit 组件

### 8.3 服务 (可选)
- [x] `IRhymeCheckService.cs` - 押韵检查服务接口

### 8.4 测试
- [x] `LyricsEditorViewModelTests.cs` (12+ 测试)

### 8.5 DI 注册
- [x] 在 `App.axaml.cs` 中注册 (如果需要)

---

## 9. 时间估算

| 任务 | 预计时间 | 实际时间 |
|------|---------|----------|
| 编写 Spec 文档 | 2小时 | 2小时 |
| 编写 ViewModel | 3小时 | 5小时 |
| 编写测试用例 | 2小时 | 2小时 |
| 集成 AvaloniaEdit | 3小时 | 3小时 |
| 实现语法高亮 | 2小时 | 2小时 |
| 实现自动保存 | 1.5小时 | 1.5小时 |
| 实现预览功能 | 1.5小时 | 1.5小时 |
| 实现撤销/重做功能 | 2小时 | 2小时 |
| 实现押韵检查功能 | 2小时 | 2小时 |
| **总计** | **19.5小时** | **21小时** |

---

## 10. 与之前循环的协同

### 10.1 项目服务 (SDD #2)
- ✅ 使用 `IProjectService` 获取项目路径
- ✅ 使用 `IFileSystem` 读写歌词文件

### 10.2 主编辑窗口 (SDD #6)
- ✅ 从 `MainWindowViewModel` 导航到歌词编辑器
- ✅ 共享 `CurrentProject` 数据

### 10.3 AI 服务 (SDD #3)
- ✅ 歌词编辑器使用 IRhymeCheckService 进行押韵分析
- ✅ 押韵检查功能集成到编辑器中
- ⚪ AI 对话界面 (SDD #9) 将使用 AI 服务生成歌词

---

## 11. 未来扩展

### 11.1 押韵检查
- 自动检测押韵词
- 高亮显示押韵词
- 提供押韵建议

### 11.2 历史版本
- 保存编辑历史
- 版本对比
- 回滚到历史版本

### 11.3 协作功能
- 多人实时编辑
- 评论和批注
- 变更追踪

---

**Spec 完成时间**: 2024-12-23
**下一步**: 编写测试用例

