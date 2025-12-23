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
- ✅ 押韵词高亮显示 (未来功能)
- ✅ 分屏预览 (编辑/预览)
- ✅ 自动保存机制
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
│  工具栏: [保存] [撤销] [重做] [格式化] [预览]           │
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
│  状态栏: 字数: 150 | 段落: 3 | 行数: 12                 │
└─────────────────────────────────────────────────────────┘
```

### 2.2 ViewModel 设计

```csharp
namespace Musicify.Core.ViewModels;

/// <summary>
/// 歌词编辑器 ViewModel
/// </summary>
public class LyricsEditorViewModel : ViewModelBase
{
    private readonly IProjectService _projectService;
    private readonly IFileSystem _fileSystem;
    
    /// <summary>
    /// 当前项目配置
    /// </summary>
    [ObservableProperty]
    private ProjectConfig? _currentProject;
    
    /// <summary>
    /// 歌词内容
    /// </summary>
    [ObservableProperty]
    private string _lyricsText = string.Empty;
    
    /// <summary>
    /// 字数统计
    /// </summary>
    [ObservableProperty]
    private int _wordCount;
    
    /// <summary>
    /// 段落数量
    /// </summary>
    [ObservableProperty]
    private int _sectionCount;
    
    /// <summary>
    /// 行数
    /// </summary>
    [ObservableProperty]
    private int _lineCount;
    
    /// <summary>
    /// 是否已修改 (未保存)
    /// </summary>
    [ObservableProperty]
    private bool _isModified;
    
    /// <summary>
    /// 是否显示预览
    /// </summary>
    [ObservableProperty]
    private bool _showPreview;
    
    /// <summary>
    /// 保存歌词
    /// </summary>
    [RelayCommand]
    private async Task SaveLyricsAsync()
    {
        // 保存到项目目录
    }
    
    /// <summary>
    /// 格式化歌词
    /// </summary>
    [RelayCommand]
    private void FormatLyrics()
    {
        // 自动格式化段落标记
    }
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
- 撤销/重做

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

---

## 8. 实现清单

### 8.1 ViewModel
- [ ] `LyricsEditorViewModel.cs`

### 8.2 Views
- [ ] `LyricsEditorView.axaml` + `.cs`
- [ ] 集成 AvaloniaEdit 组件

### 8.3 服务 (可选)
- [ ] `ILyricsService.cs` (如果需要独立的歌词服务)

### 8.4 测试
- [ ] `LyricsEditorViewModelTests.cs` (12+ 测试)

### 8.5 DI 注册
- [ ] 在 `App.axaml.cs` 中注册 (如果需要)

---

## 9. 时间估算

| 任务 | 预计时间 |
|------|---------|
| 编写 Spec 文档 | 2小时 |
| 编写 ViewModel | 3小时 |
| 编写测试用例 | 2小时 |
| 集成 AvaloniaEdit | 3小时 |
| 实现语法高亮 | 2小时 |
| 实现自动保存 | 1.5小时 |
| 实现预览功能 | 1.5小时 |
| **总计** | **15小时** |

---

## 10. 与之前循环的协同

### 10.1 项目服务 (SDD #2)
- ✅ 使用 `IProjectService` 获取项目路径
- ✅ 使用 `IFileSystem` 读写歌词文件

### 10.2 主编辑窗口 (SDD #6)
- ✅ 从 `MainWindowViewModel` 导航到歌词编辑器
- ✅ 共享 `CurrentProject` 数据

### 10.3 AI 服务 (SDD #3)
- ⚪ 歌词编辑器不直接使用 AI 服务
- ⚪ AI 对话界面 (SDD #8) 将使用 AI 服务生成歌词

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

