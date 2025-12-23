# Spec: 项目基础设施搭建

**文档版本**: v1.0  
**创建日期**: 2025-12-23  
**状态**: 🟡 进行中  
**负责人**: TBD

---

## 📋 概述

搭建 Musicify Desktop 项目的完整基础设施,包括解决方案结构、依赖管理、配置系统和核心服务框架。

---

## 🎯 用户故事

> 作为 **开发者**,  
> 我想要 **一个结构清晰、配置完善的项目框架**,  
> 以便 **快速开始功能开发并保持代码质量**

---

## 💡 功能需求

### Must Have (必须实现)

- [x] 创建 .NET 8 解决方案
- [x] 设置多项目架构 (Desktop/Core/Audio/AI)
- [x] 安装所有必需的 NuGet 包
- [ ] 配置代码风格规范 (EditorConfig)
- [ ] 实现应用配置系统 (appsettings.json)
- [ ] 设计核心数据模型
- [ ] 实现项目配置服务 (IProjectService)
- [ ] 编写单元测试框架

### Should Have (应该实现)

- [ ] 配置 CI/CD 流程 (GitHub Actions)
- [ ] 设置代码质量检查工具
- [ ] 实现日志系统 (Serilog)

### Could Have (可以实现)

- [ ] 性能监控集成
- [ ] 崩溃报告系统

---

## 🏗 技术规格

### 1. 解决方案结构

```
Musicify.sln
├── src/
│   ├── Musicify.Desktop/          # UI 层 (AvaloniaUI)
│   ├── Musicify.Core/             # 核心业务逻辑
│   ├── Musicify.Audio/            # 音频/MIDI 处理
│   └── Musicify.AI/               # AI 服务集成
└── tests/
    ├── Musicify.Core.Tests/       # 单元测试
    └── Musicify.Integration.Tests/ # 集成测试
```

### 2. 核心数据模型

#### 2.1 项目配置 (ProjectConfig)

```csharp
namespace Musicify.Core.Models;

/// <summary>
/// 项目配置信息
/// </summary>
public sealed class ProjectConfig
{
    /// <summary>
    /// 项目名称
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// 项目类型标识
    /// </summary>
    public required string Type { get; init; }
    
    /// <summary>
    /// 默认歌曲类型
    /// </summary>
    public string? DefaultType { get; init; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime Created { get; init; }
    
    /// <summary>
    /// 项目版本号
    /// </summary>
    public required string Version { get; init; }
    
    /// <summary>
    /// 扩展元数据
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}
```

#### 2.2 歌曲规格 (SongSpec)

```csharp
/// <summary>
/// 歌曲规格定义
/// 对应 CLI 版本的 spec.json
/// </summary>
public sealed class SongSpec
{
    public required string ProjectName { get; init; }
    
    /// <summary>
    /// 歌曲类型: 流行/摇滚/说唱/民谣/电子/古风/R&B/爵士/乡村/金属
    /// </summary>
    public required string SongType { get; init; }
    
    /// <summary>
    /// 目标时长 (格式: "3分30秒")
    /// </summary>
    public required string Duration { get; init; }
    
    /// <summary>
    /// 风格基调: 抒情/激昂/轻快/忧郁/治愈/燃爆/平静/梦幻
    /// </summary>
    public required string Style { get; init; }
    
    /// <summary>
    /// 歌词语言: 中文/英文/粤语/日语/韩语/中英混合/其他
    /// </summary>
    public required string Language { get; init; }
    
    /// <summary>
    /// 目标受众信息
    /// </summary>
    public required AudienceInfo Audience { get; init; }
    
    /// <summary>
    /// 目标发布平台列表
    /// </summary>
    public required List<string> TargetPlatform { get; init; }
    
    /// <summary>
    /// 补充音调描述
    /// </summary>
    public string? Tone { get; init; }
    
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed class AudienceInfo
{
    /// <summary>
    /// 年龄段: 15-20/20-30/30-40/全年龄
    /// </summary>
    public required string Age { get; init; }
    
    /// <summary>
    /// 性别倾向: 女性向/男性向/中性
    /// </summary>
    public required string Gender { get; init; }
}
```

#### 2.3 项目实体 (Project)

```csharp
/// <summary>
/// 完整的项目实体
/// </summary>
public sealed class Project
{
    public required string Name { get; init; }
    public required string Path { get; init; }
    public required ProjectConfig Config { get; init; }
    
    /// <summary>
    /// 歌曲规格 (可能为空)
    /// </summary>
    public SongSpec? Spec { get; set; }
    
    /// <summary>
    /// 项目是否已加载完整数据
    /// </summary>
    public bool IsLoaded { get; set; }
}

/// <summary>
/// 项目简要信息 (用于最近项目列表)
/// </summary>
public sealed class ProjectInfo
{
    public required string Name { get; init; }
    public required string Path { get; init; }
    public DateTime LastOpened { get; init; }
    public string? SongType { get; init; }
}
```

### 3. 核心服务接口

#### 3.1 项目服务 (IProjectService)

```csharp
namespace Musicify.Core.Interfaces;

/// <summary>
/// 项目管理服务
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// 创建新项目
    /// </summary>
    /// <param name="name">项目名称</param>
    /// <param name="songType">默认歌曲类型</param>
    /// <returns>创建的项目实例</returns>
    Task<Result<Project>> CreateProjectAsync(string name, string songType);
    
    /// <summary>
    /// 打开已有项目
    /// </summary>
    /// <param name="path">项目路径</param>
    /// <returns>加载的项目实例</returns>
    Task<Result<Project>> OpenProjectAsync(string path);
    
    /// <summary>
    /// 保存项目配置
    /// </summary>
    Task<Result> SaveProjectAsync(Project project);
    
    /// <summary>
    /// 保存歌曲规格
    /// </summary>
    Task<Result> SaveSpecAsync(Project project, SongSpec spec);
    
    /// <summary>
    /// 加载歌曲规格
    /// </summary>
    Task<Result<SongSpec>> LoadSpecAsync(Project project);
    
    /// <summary>
    /// 获取最近打开的项目列表
    /// </summary>
    Task<List<ProjectInfo>> GetRecentProjectsAsync();
    
    /// <summary>
    /// 验证项目有效性
    /// </summary>
    Task<bool> ValidateProjectAsync(string path);
}
```

#### 3.2 配置服务 (IConfigService)

```csharp
/// <summary>
/// 应用配置服务
/// </summary>
public interface IConfigService
{
    /// <summary>
    /// 获取配置值
    /// </summary>
    T? GetValue<T>(string key);
    
    /// <summary>
    /// 设置配置值
    /// </summary>
    Task SetValueAsync<T>(string key, T value);
    
    /// <summary>
    /// 获取项目数据目录
    /// </summary>
    string GetProjectsDirectory();
    
    /// <summary>
    /// 获取 Python 脚本路径
    /// </summary>
    string GetPythonScriptsPath();
}
```

### 4. Result 类型定义

```csharp
namespace Musicify.Core.Common;

/// <summary>
/// 操作结果包装类 (带返回值)
/// </summary>
public sealed class Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public Exception? Exception { get; init; }
    
    public static Result<T> Success(T data) => new() 
    { 
        IsSuccess = true, 
        Data = data 
    };
    
    public static Result<T> Failure(string error, Exception? ex = null) => new() 
    { 
        IsSuccess = false, 
        Error = error,
        Exception = ex
    };
}

/// <summary>
/// 操作结果包装类 (无返回值)
/// </summary>
public sealed class Result
{
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }
    public Exception? Exception { get; init; }
    
    public static Result Success() => new() { IsSuccess = true };
    
    public static Result Failure(string error, Exception? ex = null) => new() 
    { 
        IsSuccess = false, 
        Error = error,
        Exception = ex
    };
}
```

---

## 🧪 测试用例

### Test Suite: ProjectService

#### Test 1: 创建项目 - 成功场景

```csharp
[Fact]
public async Task CreateProject_ShouldSucceed_WhenValidInputs()
{
    // Arrange
    var service = CreateService();
    var name = "TestSong";
    var type = "流行";
    
    // Act
    var result = await service.CreateProjectAsync(name, type);
    
    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Data.Should().NotBeNull();
    result.Data!.Name.Should().Be(name);
    result.Data.Config.DefaultType.Should().Be(type);
    
    // 验证文件系统
    var projectPath = Path.Combine(GetProjectsDir(), name);
    Directory.Exists(projectPath).Should().BeTrue();
    File.Exists(Path.Combine(projectPath, ".musicify", "config.json")).Should().BeTrue();
}
```

#### Test 2: 创建项目 - 失败场景 (项目已存在)

```csharp
[Fact]
public async Task CreateProject_ShouldFail_WhenProjectExists()
{
    // Arrange
    var service = CreateService();
    await service.CreateProjectAsync("Existing", "流行");
    
    // Act
    var result = await service.CreateProjectAsync("Existing", "流行");
    
    // Assert
    result.IsSuccess.Should().BeFalse();
    result.Error.Should().Contain("已存在");
}
```

#### Test 3: 打开项目 - 成功场景

```csharp
[Fact]
public async Task OpenProject_ShouldSucceed_WhenValidProject()
{
    // Arrange
    var service = CreateService();
    var created = await service.CreateProjectAsync("TestOpen", "流行");
    
    // Act
    var result = await service.OpenProjectAsync(created.Data!.Path);
    
    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Data!.Name.Should().Be("TestOpen");
}
```

#### Test 4: 保存规格 - JSON 兼容性测试

```csharp
[Fact]
public async Task SaveSpec_ShouldBeCompatibleWithCLI()
{
    // Arrange
    var project = await CreateTestProject();
    var spec = new SongSpec
    {
        ProjectName = "Test",
        SongType = "流行",
        Duration = "3分30秒",
        Style = "抒情",
        Language = "中文",
        Audience = new AudienceInfo { Age = "20-30", Gender = "中性" },
        TargetPlatform = new List<string> { "QQ音乐", "网易云音乐" },
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
    
    // Act
    var saveResult = await service.SaveSpecAsync(project, spec);
    
    // Assert
    saveResult.IsSuccess.Should().BeTrue();
    
    // 验证 JSON 格式与 CLI 版本一致
    var jsonPath = Path.Combine(project.Path, "spec.json");
    var jsonContent = await File.ReadAllTextAsync(jsonPath);
    var parsed = JsonDocument.Parse(jsonContent);
    
    parsed.RootElement.GetProperty("song_type").GetString().Should().Be("流行");
    parsed.RootElement.GetProperty("duration").GetString().Should().Be("3分30秒");
}
```

---

## ✅ 验收标准

### 代码质量

- [ ] 所有公开 API 有完整的 XML 文档注释
- [ ] 单元测试覆盖率 > 80%
- [ ] 所有测试通过
- [ ] 无编译警告
- [ ] 通过 SonarLint 代码质量检查

### 功能完整性

- [ ] 项目创建功能正常
- [ ] 项目打开功能正常
- [ ] 配置保存/加载正常
- [ ] JSON 格式与 CLI 版本兼容
- [ ] 最近项目列表功能正常

### 性能要求

- [ ] 项目创建 < 1 秒
- [ ] 项目打开 < 2 秒
- [ ] 配置加载 < 500ms

### 跨平台兼容性

- [ ] Windows 测试通过
- [ ] macOS 测试通过
- [ ] Linux 测试通过

---

## 📅 时间估算

| 任务 | 预计时间 | 实际时间 |
|------|---------|---------|
| 创建解决方案结构 | 2h | - |
| 安装 NuGet 包 | 1h | - |
| 配置项目设置 | 2h | - |
| 设计数据模型 | 4h | - |
| 实现 IProjectService | 6h | - |
| 实现 IConfigService | 3h | - |
| 编写单元测试 | 6h | - |
| 集成测试 | 2h | - |
| 文档编写 | 2h | - |
| **总计** | **28h** | - |

---

## 🔗 依赖关系

### 前置条件
- 安装 .NET 8 SDK
- 安装 Git
- 安装 Visual Studio 或 Rider

### 后续任务
- Task 2.1: 项目管理器 UI
- Task 2.2: 规格编辑器

---

## 📝 变更日志

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2025-12-23 | v1.0 | 初始版本 |

---

## 📎 附录

### A. EditorConfig 配置

```ini
# .editorconfig
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true

[*.cs]
indent_style = space
indent_size = 4

# C# 命名规范
dotnet_naming_rule.interfaces_must_be_prefixed_with_i.severity = warning
dotnet_naming_rule.interfaces_must_be_prefixed_with_i.symbols = interface
dotnet_naming_rule.interfaces_must_be_prefixed_with_i.style = begins_with_i

dotnet_naming_symbols.interface.applicable_kinds = interface
dotnet_naming_style.begins_with_i.required_prefix = I
dotnet_naming_style.begins_with_i.capitalization = pascal_case

# 代码风格
csharp_prefer_braces = true:warning
dotnet_sort_system_directives_first = true
```

### B. appsettings.json 示例

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
    "Temperature": 0.7,
    "ApiKey": ""
  },
  "Python": {
    "ScriptsPath": "../skills/scripts",
    "VirtualEnvPath": "venv",
    "Timeout": 300
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```
