# Musicify Desktop 开发路线图 (SDD 模式)

> 基于 Spec-Driven Development 的详细任务分解

## 📋 总览

| 阶段 | 时间 | 核心目标 | 状态 |
|------|------|----------|------|
| Phase 1 | Week 1-2 | 项目基础搭建 | 🟡 进行中 |
| Phase 2 | Week 3-4 | 核心业务功能 | ⚪ 未开始 |
| Phase 3 | Week 5-6 | AI 服务集成 | ⚪ 未开始 |
| Phase 4 | Week 7-9 | 音乐分析引擎 | ⚪ 未开始 |
| Phase 5 | Week 10-11 | 高级功能 | ⚪ 未开始 |
| Phase 6 | Week 12 | 优化与发布 | ⚪ 未开始 |

---

## Phase 1: 项目基础搭建 (Week 1-2)

### 📝 Spec 文档: `docs/specs/01-project-setup.md`

#### Task 1.1: 创建解决方案结构 ✅
**预计时间**: 2 小时  
**负责人**: TBD  
**优先级**: P0 (最高)

**详细步骤**:
```bash
# 1. 创建 .NET 解决方案
dotnet new sln -n Musicify

# 2. 创建项目结构
dotnet new avalonia.mvvm -n Musicify.Desktop -o src/Musicify.Desktop
dotnet new classlib -n Musicify.Core -o src/Musicify.Core
dotnet new classlib -n Musicify.Audio -o src/Musicify.Audio
dotnet new classlib -n Musicify.AI -o src/Musicify.AI
dotnet new xunit -n Musicify.Core.Tests -o tests/Musicify.Core.Tests

# 3. 添加项目引用
dotnet sln add src/**/*.csproj tests/**/*.csproj
```

**验收标准**:
- [ ] 解决方案编译通过
- [ ] 所有项目引用正确
- [ ] Git 仓库初始化完成

---

#### Task 1.2: 安装核心 NuGet 包 ✅
**预计时间**: 1 小时  
**优先级**: P0

**依赖包列表**:

```xml
<!-- Musicify.Desktop -->
<PackageReference Include="Avalonia" Version="11.1.3" />
<PackageReference Include="Avalonia.Desktop" Version="11.1.3" />
<PackageReference Include="Avalonia.Themes.Fluent" Version="11.1.3" />
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />

<!-- Musicify.Core -->
<PackageReference Include="System.Text.Json" Version="8.0.4" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />

<!-- Musicify.Audio -->
<PackageReference Include="Melanchall.DryWetMidi" Version="7.2.0" />
<PackageReference Include="NAudio" Version="2.2.1" />
<PackageReference Include="Python.Runtime" Version="3.0.4" />

<!-- Musicify.AI -->
<PackageReference Include="Anthropic.SDK" Version="0.4.0" />
<PackageReference Include="System.Net.Http.Json" Version="8.0.0" />

<!-- Musicify.Core.Tests -->
<PackageReference Include="xunit" Version="2.6.6" />
<PackageReference Include="FluentAssertions" Version="6.12.0" />
<PackageReference Include="Moq" Version="4.20.70" />
```

**执行命令**:
```bash
# 添加包到各个项目
cd src/Musicify.Desktop && dotnet add package Avalonia
cd ../Musicify.Audio && dotnet add package Melanchall.DryWetMidi
# ... (其他包)
```

**验收标准**:
- [ ] 所有包成功安装
- [ ] 版本兼容性验证
- [ ] 项目编译无错误

---

#### Task 1.3: 配置项目设置 📋
**预计时间**: 2 小时  
**Spec 文档**: `docs/specs/02-core-services.md`  
**优先级**: P0

**需要创建的配置**:

1. **EditorConfig** (代码风格统一)
```ini
# .editorconfig
root = true

[*.cs]
indent_style = space
indent_size = 4
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

# C# 命名规范
dotnet_naming_rule.interfaces_should_be_prefixed_with_i.severity = warning
dotnet_naming_rule.interfaces_should_be_prefixed_with_i.symbols = interface
dotnet_naming_rule.interfaces_should_be_prefixed_with_i.style = begins_with_i
```

2. **Directory.Build.props** (全局属性)
```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>12.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>
</Project>
```

3. **appsettings.json** (应用配置)
```json
{
  "App": {
    "Name": "Musicify Desktop",
    "Version": "1.0.0",
    "DataDirectory": "~/Documents/Musicify"
  },
  "AI": {
    "Provider": "Claude",
    "DefaultModel": "claude-3-5-sonnet-20241022",
    "MaxTokens": 4096,
    "Temperature": 0.7
  },
  "Python": {
    "ScriptsPath": "../skills/scripts",
    "VirtualEnvPath": "venv"
  }
}
```

**验收标准**:
- [ ] EditorConfig 生效
- [ ] 所有项目使用统一配置
- [ ] appsettings.json 正确加载

---

#### Task 1.4: 设计核心数据模型 📋
**预计时间**: 4 小时  
**Spec 文档**: `docs/specs/02-core-services.md`  
**优先级**: P0

**数据模型设计**:

```csharp
// Musicify.Core/Models/SongSpec.cs
namespace Musicify.Core.Models;

/// <summary>
/// 歌曲规格配置
/// </summary>
public sealed class SongSpec
{
    public required string ProjectName { get; init; }
    public required string SongType { get; init; }
    public required string Duration { get; init; }
    public required string Style { get; init; }
    public required string Language { get; init; }
    public required AudienceInfo Audience { get; init; }
    public required List<string> TargetPlatform { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed class AudienceInfo
{
    public required string Age { get; init; }
    public required string Gender { get; init; }
}

// Musicify.Core/Models/LyricsContent.cs
public sealed class LyricsContent
{
    public required string ProjectName { get; init; }
    public required string Mode { get; init; } // Coach/Express/Hybrid
    public required List<LyricsSection> Sections { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class LyricsSection
{
    public required string Type { get; init; } // Verse/Chorus/Bridge
    public required string Content { get; init; }
    public int Order { get; init; }
}

// Musicify.Core/Models/MidiAnalysisResult.cs
public sealed class MidiAnalysisResult
{
    public required string FilePath { get; init; }
    public int TotalNotes { get; init; }
    public (int Min, int Max) NoteRange { get; init; }
    public required Dictionary<string, float> RhythmPatterns { get; init; }
    public required Dictionary<string, float> IntervalDistribution { get; init; }
    public required ModeAnalysis ModeInfo { get; init; }
}

public sealed class ModeAnalysis
{
    public required string DetectedMode { get; init; }
    public float Confidence { get; init; }
    public required List<string> ScaleNotes { get; init; }
}
```

**验收标准**:
- [ ] 模型与 CLI 版本的 JSON 格式兼容
- [ ] 所有属性有正确的文档注释
- [ ] 添加单元测试验证序列化/反序列化

---

#### Task 1.5: 实现项目配置服务 🔧
**预计时间**: 6 小时  
**Spec 文档**: `docs/specs/02-core-services.md`  
**优先级**: P1

**接口设计**:

```csharp
// Musicify.Core/Interfaces/IProjectService.cs
namespace Musicify.Core.Interfaces;

public interface IProjectService
{
    /// <summary>
    /// 创建新项目
    /// </summary>
    Task<Result<Project>> CreateProjectAsync(string name, string type);
    
    /// <summary>
    /// 打开已有项目
    /// </summary>
    Task<Result<Project>> OpenProjectAsync(string path);
    
    /// <summary>
    /// 保存项目
    /// </summary>
    Task<Result> SaveProjectAsync(Project project);
    
    /// <summary>
    /// 获取最近项目列表
    /// </summary>
    Task<List<ProjectInfo>> GetRecentProjectsAsync();
}

public sealed class Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    
    public static Result<T> Success(T data) => new() { IsSuccess = true, Data = data };
    public static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error };
}

public sealed class Result
{
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }
    
    public static Result Success() => new() { IsSuccess = true };
    public static Result Failure(string error) => new() { IsSuccess = false, Error = error };
}
```

**实现示例**:

```csharp
// Musicify.Core/Services/ProjectService.cs
namespace Musicify.Core.Services;

public sealed class ProjectService : IProjectService
{
    private readonly string _projectsBasePath;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(IConfiguration config, ILogger<ProjectService> logger)
    {
        _projectsBasePath = config["App:DataDirectory"] 
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Musicify");
        _logger = logger;
    }

    public async Task<Result<Project>> CreateProjectAsync(string name, string type)
    {
        try
        {
            var projectPath = Path.Combine(_projectsBasePath, name);
            
            if (Directory.Exists(projectPath))
                return Result<Project>.Failure($"项目 '{name}' 已存在");

            // 创建项目结构
            Directory.CreateDirectory(projectPath);
            Directory.CreateDirectory(Path.Combine(projectPath, ".musicify"));
            Directory.CreateDirectory(Path.Combine(projectPath, "workspace"));

            // 创建配置文件
            var config = new ProjectConfig
            {
                Name = name,
                Type = "musicify-project",
                DefaultType = type,
                Created = DateTime.UtcNow,
                Version = "1.0.0"
            };

            var configPath = Path.Combine(projectPath, ".musicify", "config.json");
            await File.WriteAllTextAsync(configPath, 
                JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));

            var project = new Project
            {
                Name = name,
                Path = projectPath,
                Config = config
            };

            _logger.LogInformation("项目 '{Name}' 创建成功", name);
            return Result<Project>.Success(project);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建项目失败");
            return Result<Project>.Failure(ex.Message);
        }
    }

    // ... 其他方法实现
}
```

**测试用例**:

```csharp
// Musicify.Core.Tests/Services/ProjectServiceTests.cs
public class ProjectServiceTests
{
    [Fact]
    public async Task CreateProject_ShouldSucceed_WhenValidName()
    {
        // Arrange
        var service = new ProjectService(mockConfig, mockLogger);
        
        // Act
        var result = await service.CreateProjectAsync("TestSong", "流行");
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("TestSong");
    }
    
    [Fact]
    public async Task CreateProject_ShouldFail_WhenProjectExists()
    {
        // Arrange & Act & Assert
        // ...
    }
}
```

**验收标准**:
- [ ] 所有接口方法实现
- [ ] 单元测试覆盖率 > 80%
- [ ] 异常处理完善
- [ ] 日志记录完整

---

## Phase 2: 核心业务功能 (Week 3-4)

### 📝 Spec 文档列表

1. `docs/specs/03-project-manager.md` - 项目管理器
2. `docs/specs/04-spec-editor.md` - 规格编辑器
3. `docs/specs/05-lyrics-editor.md` - 歌词编辑器

---

#### Task 2.1: 项目管理器 UI 📋
**预计时间**: 8 小时  
**Spec 文档**: `docs/specs/03-project-manager.md`  
**优先级**: P0

**功能需求**:
- [x] 欢迎页面
- [ ] 新建项目向导
- [ ] 打开项目对话框
- [ ] 最近项目列表
- [ ] 项目设置管理

**UI 结构**:

```xml
<!-- Views/WelcomeWindow.axaml -->
<Window xmlns="https://github.com/avaloniaui"
        Title="Musicify - 欢迎"
        Width="800" Height="600">
    <Grid RowDefinitions="Auto,*,Auto">
        <!-- Header -->
        <StackPanel Grid.Row="0" Background="#1E1E1E" Padding="20">
            <TextBlock Text="🎵 Musicify Desktop" FontSize="28" FontWeight="Bold"/>
            <TextBlock Text="AI 驱动的歌词创作工具" FontSize="14" Opacity="0.7"/>
        </StackPanel>
        
        <!-- Content -->
        <Grid Grid.Row="1" ColumnDefinitions="*,*" Margin="20">
            <!-- 左侧: 快速操作 -->
            <StackPanel Grid.Column="0" Spacing="15">
                <Button Content="📝 新建项目" Command="{Binding CreateProjectCommand}"/>
                <Button Content="📂 打开项目" Command="{Binding OpenProjectCommand}"/>
                <Button Content="⚙️ 设置" Command="{Binding OpenSettingsCommand}"/>
            </StackPanel>
            
            <!-- 右侧: 最近项目 -->
            <StackPanel Grid.Column="1">
                <TextBlock Text="最近项目" FontSize="18" Margin="0,0,0,10"/>
                <ListBox ItemsSource="{Binding RecentProjects}"
                         SelectedItem="{Binding SelectedProject}">
                    <ListBox.ItemTemplate>
                        <DataTemplate>
                            <StackPanel>
                                <TextBlock Text="{Binding Name}" FontWeight="Bold"/>
                                <TextBlock Text="{Binding Path}" FontSize="12" Opacity="0.6"/>
                            </StackPanel>
                        </DataTemplate>
                    </ListBox.ItemTemplate>
                </ListBox>
            </StackPanel>
        </Grid>
    </Grid>
</Window>
```

**ViewModel**:

```csharp
// ViewModels/WelcomeViewModel.cs
public partial class WelcomeViewModel : ViewModelBase
{
    private readonly IProjectService _projectService;
    
    [ObservableProperty]
    private ObservableCollection<ProjectInfo> _recentProjects = new();
    
    [ObservableProperty]
    private ProjectInfo? _selectedProject;

    public WelcomeViewModel(IProjectService projectService)
    {
        _projectService = projectService;
        LoadRecentProjects();
    }

    [RelayCommand]
    private async Task CreateProject()
    {
        var dialog = new CreateProjectDialog();
        var result = await dialog.ShowDialog<ProjectCreationResult>(GetWindow());
        
        if (result?.Success == true)
        {
            // 打开主窗口
            OpenMainWindow(result.Project);
        }
    }
    
    [RelayCommand]
    private async Task OpenProject()
    {
        var dialog = new OpenFolderDialog();
        var path = await dialog.ShowAsync(GetWindow());
        
        if (!string.IsNullOrEmpty(path))
        {
            var result = await _projectService.OpenProjectAsync(path);
            if (result.IsSuccess)
            {
                OpenMainWindow(result.Data!);
            }
        }
    }

    private async void LoadRecentProjects()
    {
        var projects = await _projectService.GetRecentProjectsAsync();
        RecentProjects = new ObservableCollection<ProjectInfo>(projects);
    }
}
```

**验收标准**:
- [ ] UI 布局美观
- [ ] 新建/打开项目功能正常
- [ ] 最近项目列表显示正确
- [ ] 响应式设计(支持不同窗口大小)

---

#### Task 2.2: 规格编辑器实现 📋
**预计时间**: 12 小时  
**Spec 文档**: `docs/specs/04-spec-editor.md`  
**优先级**: P0

**功能需求**:
- [ ] 歌曲类型选择 (10种类型)
- [ ] 时长输入
- [ ] 风格基调选择
- [ ] 语言选择
- [ ] 受众定位
- [ ] 目标平台(多选)
- [ ] 配置预览与保存

**UI 示例** (使用 Tab 分页):

```xml
<!-- Views/SpecEditorView.axaml -->
<UserControl>
    <TabControl>
        <!-- Tab 1: 基本信息 -->
        <TabItem Header="基本信息">
            <StackPanel Spacing="15">
                <TextBlock Text="歌曲类型" FontWeight="Bold"/>
                <ComboBox ItemsSource="{Binding SongTypes}"
                          SelectedItem="{Binding SelectedType}"/>
                
                <TextBlock Text="目标时长" FontWeight="Bold"/>
                <TextBox Text="{Binding Duration}" Watermark="例: 3分30秒"/>
                
                <TextBlock Text="风格基调" FontWeight="Bold"/>
                <ComboBox ItemsSource="{Binding Styles}"
                          SelectedItem="{Binding SelectedStyle}"/>
            </StackPanel>
        </TabItem>
        
        <!-- Tab 2: 受众定位 -->
        <TabItem Header="受众定位">
            <!-- ... -->
        </TabItem>
        
        <!-- Tab 3: 发布平台 -->
        <TabItem Header="发布平台">
            <ItemsControl ItemsSource="{Binding Platforms}">
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <CheckBox Content="{Binding Name}"
                                  IsChecked="{Binding IsSelected}"/>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </TabItem>
    </TabControl>
</UserControl>
```

**验收标准**:
- [ ] 所有字段验证正确
- [ ] 保存/加载功能正常
- [ ] 与 CLI 版本 JSON 格式兼容
- [ ] 表单验证提示友好

---

#### Task 2.3: 歌词编辑器实现 📋
**预计时间**: 16 小时  
**Spec 文档**: `docs/specs/05-lyrics-editor.md`  
**优先级**: P1

**功能需求**:
- [ ] 富文本编辑器
- [ ] 段落标记 ([Verse 1], [Chorus] 等)
- [ ] 实时字数统计
- [ ] 押韵高亮显示
- [ ] 分屏预览(编辑/预览)
- [ ] 导出功能

**技术方案**:
- 使用 AvalonEdit 或 AvaloniaEdit 作为编辑器组件
- 自定义语法高亮规则
- 实现自动保存机制

**验收标准**:
- [ ] 编辑体验流畅
- [ ] 段落标记自动补全
- [ ] 押韵词高亮显示
- [ ] 支持 Ctrl+S 保存

---

## Phase 3: AI 服务集成 (Week 5-6)

### 📝 Spec 文档: `docs/specs/06-ai-integration.md`

#### Task 3.1: Claude API 封装 🔧
**预计时间**: 8 小时  
**优先级**: P0

#### Task 3.2: 提示词系统 🔧
**预计时间**: 6 小时  
**优先级**: P0

#### Task 3.3: 流式响应处理 🔧
**预计时间**: 10 小时  
**优先级**: P1

#### Task 3.4: 三种创作模式实现 🔧
**预计时间**: 12 小时  
**优先级**: P1

---

## Phase 4: 音乐分析引擎 (Week 7-9)

### 📝 Spec 文档: `docs/specs/07-midi-analysis.md`

#### Task 4.1: MIDI 解析器 🔧
**预计时间**: 12 hours  
**优先级**: P1

#### Task 4.2: Python 脚本桥接 🔧
**预计时间**: 10 小时  
**优先级**: P1

#### Task 4.3: 旋律特征可视化 🎨
**预计时间**: 16 小时  
**优先级**: P2

---

## Phase 5: 高级功能 (Week 10-11)

### 📝 Spec 文档: `docs/specs/08-export-system.md`

#### Task 5.1: 导出系统 🔧
**预计时间**: 8 小时  
**优先级**: P1

#### Task 5.2: 押韵检查算法 🔧
**预计时间**: 10 小时  
**优先级**: P2

---

## Phase 6: 优化与发布 (Week 12)

#### Task 6.1: 性能优化 ⚡
#### Task 6.2: UI/UX 优化 🎨
#### Task 6.3: 打包发布 📦
#### Task 6.4: 用户文档 📚

---

## 🎯 下一步行动

### 立即开始的任务 (本周)

1. **✅ Task 1.1**: 创建解决方案结构 (2h)
2. **✅ Task 1.2**: 安装 NuGet 包 (1h)
3. **📋 Task 1.3**: 配置项目设置 (2h)
4. **📋 Task 1.4**: 设计核心数据模型 (4h)
5. **🔧 Task 1.5**: 实现项目配置服务 (6h)

**本周目标**: 完成 Phase 1 的所有基础设施搭建

---

## 📊 进度跟踪

| 任务编号 | 任务名称 | 状态 | 预计时间 | 实际时间 | 负责人 |
|---------|---------|------|---------|---------|--------|
| 1.1 | 创建解决方案 | ✅ 完成 | 2h | - | - |
| 1.2 | 安装依赖包 | ✅ 完成 | 1h | - | - |
| 1.3 | 项目配置 | 🟡 进行中 | 2h | - | - |
| 1.4 | 数据模型 | ⚪ 待开始 | 4h | - | - |
| 1.5 | 配置服务 | ⚪ 待开始 | 6h | - | - |

---

## 📝 开发日志

### 2025-12-23
- ✅ 初始化项目结构
- ✅ 创建 SDD 开发路线图
- 🟡 准备开始 Task 1.3

---

## 🔗 相关链接

- [Spec 文档目录](../specs/)
- [架构设计文档](../architecture/)
- [API 设计文档](../architecture/api-design.md)
