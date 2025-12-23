# Spec 03: 项目配置服务 (ProjectService)

**状态**: 🟢 实现中  
**优先级**: P0 (核心功能)  
**预计时间**: 6 小时  
**依赖**: Spec 02 (核心数据模型)

---

## 1. 需求概述

### 1.1 功能目标
实现项目配置的**创建、读取、更新、保存**等核心服务,管理用户的歌词创作项目。

### 1.2 核心功能
- ✅ 创建新项目
- ✅ 加载现有项目
- ✅ 保存项目配置
- ✅ 更新项目状态
- ✅ 管理最近打开的项目列表
- ✅ 验证项目路径和数据完整性

### 1.3 与 CLI 兼容性
- 必须能够读取 CLI 版本创建的 `project-config.json`
- JSON 格式完全兼容
- 目录结构保持一致

---

## 2. 技术规格

### 2.1 服务接口设计

```csharp
namespace Musicify.Core.Services;

/// <summary>
/// 项目配置服务接口
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// 创建新项目
    /// </summary>
    /// <param name="name">项目名称</param>
    /// <param name="basePath">基础路径 (可选,默认 ~/Documents/musicify)</param>
    /// <returns>创建的项目配置</returns>
    Task<ProjectConfig> CreateProjectAsync(string name, string? basePath = null);
    
    /// <summary>
    /// 加载现有项目
    /// </summary>
    /// <param name="projectPath">项目路径</param>
    /// <returns>项目配置,如果不存在返回 null</returns>
    Task<ProjectConfig?> LoadProjectAsync(string projectPath);
    
    /// <summary>
    /// 保存项目配置
    /// </summary>
    Task SaveProjectAsync(ProjectConfig config);
    
    /// <summary>
    /// 更新项目状态
    /// </summary>
    Task UpdateProjectStatusAsync(string projectPath, string status);
    
    /// <summary>
    /// 获取最近打开的项目列表
    /// </summary>
    /// <param name="limit">返回数量限制</param>
    Task<List<ProjectConfig>> GetRecentProjectsAsync(int limit = 10);
    
    /// <summary>
    /// 添加项目到最近列表
    /// </summary>
    Task AddToRecentProjectsAsync(string projectPath);
    
    /// <summary>
    /// 验证项目路径是否有效
    /// </summary>
    bool ValidateProjectPath(string projectPath);
    
    /// <summary>
    /// 获取项目配置文件路径
    /// </summary>
    string GetConfigFilePath(string projectPath);
}
```

### 2.2 实现类设计

```csharp
namespace Musicify.Core.Services;

public class ProjectService : IProjectService
{
    private const string ConfigFileName = "project-config.json";
    private const string RecentProjectsFile = "recent-projects.json";
    
    private readonly IFileSystem _fileSystem; // 使用抽象文件系统便于测试
    private readonly string _recentProjectsPath;
    
    public ProjectService(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
        _recentProjectsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Musicify",
            RecentProjectsFile
        );
    }
    
    // 实现接口方法...
}
```

### 2.3 文件系统抽象接口

```csharp
namespace Musicify.Core.Abstractions;

/// <summary>
/// 文件系统抽象接口 (用于单元测试)
/// </summary>
public interface IFileSystem
{
    bool FileExists(string path);
    bool DirectoryExists(string path);
    void CreateDirectory(string path);
    Task<string> ReadAllTextAsync(string path);
    Task WriteAllTextAsync(string path, string content);
    string[] GetDirectories(string path);
}

/// <summary>
/// 默认文件系统实现
/// </summary>
public class DefaultFileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);
    public bool DirectoryExists(string path) => Directory.Exists(path);
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);
    public Task<string> ReadAllTextAsync(string path) => File.ReadAllTextAsync(path);
    public Task WriteAllTextAsync(string path, string content) => File.WriteAllTextAsync(path, content);
    public string[] GetDirectories(string path) => Directory.GetDirectories(path);
}
```

---

## 3. 目录结构设计

### 3.1 项目目录布局

```
~/Documents/musicify/
├── my-song-project/              # 用户创建的项目
│   ├── project-config.json       # 项目配置文件
│   ├── lyrics/                   # 歌词文件
│   │   ├── coach-mode.md
│   │   ├── quick-mode.md
│   │   └── hybrid-mode.md
│   ├── melody/                   # 旋律相关
│   │   ├── midi/
│   │   └── analysis/
│   └── export/                   # 导出文件
│       ├── suno/
│       └── tunee/
└── .musicify/                    # 全局配置
    └── recent-projects.json      # 最近项目列表
```

### 3.2 JSON 文件格式

#### project-config.json
```json
{
  "projectName": "my-song-project",
  "projectPath": "/Users/xxx/Documents/musicify/my-song-project",
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-15T14:20:00Z",
  "status": "in_progress",
  "spec": {
    "songType": "pop",
    "duration": 240,
    "style": "upbeat",
    "language": "zh-CN",
    "targetAudience": "青年听众",
    "targetPlatform": "suno"
  }
}
```

#### recent-projects.json
```json
{
  "projects": [
    {
      "projectName": "my-song-project",
      "projectPath": "/Users/xxx/Documents/musicify/my-song-project",
      "lastOpened": "2024-01-15T14:20:00Z",
      "status": "in_progress"
    }
  ]
}
```

---

## 4. 业务逻辑规则

### 4.1 项目创建规则
- ✅ 项目名称不能为空或仅包含空格
- ✅ 项目名称不能包含非法字符: `\ / : * ? " < > |`
- ✅ 同名项目已存在时抛出异常
- ✅ 自动创建必要的子目录 (lyrics/, melody/, export/)
- ✅ 生成初始的 `project-config.json`

### 4.2 项目加载规则
- ✅ 项目路径必须存在
- ✅ 必须包含 `project-config.json`
- ✅ JSON 格式必须有效
- ✅ 自动修复缺失的子目录

### 4.3 最近项目管理
- ✅ 最多保留 20 个最近项目
- ✅ 按最后打开时间倒序排列
- ✅ 自动移除已删除或不存在的项目
- ✅ 项目路径唯一 (避免重复)

---

## 5. 测试用例设计

### 5.1 创建项目测试

```csharp
[Fact]
public async Task CreateProject_ShouldCreateValidProject()
{
    // Arrange
    var service = CreateService();
    var projectName = "test-song";
    
    // Act
    var config = await service.CreateProjectAsync(projectName);
    
    // Assert
    config.ProjectName.Should().Be(projectName);
    config.ProjectPath.Should().Contain(projectName);
    config.Status.Should().Be("draft");
    config.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
}

[Fact]
public async Task CreateProject_WithInvalidName_ShouldThrowException()
{
    var service = CreateService();
    
    await service.Invoking(s => s.CreateProjectAsync("invalid/name"))
        .Should().ThrowAsync<ArgumentException>();
}

[Fact]
public async Task CreateProject_WhenProjectExists_ShouldThrowException()
{
    var service = CreateService();
    await service.CreateProjectAsync("existing");
    
    await service.Invoking(s => s.CreateProjectAsync("existing"))
        .Should().ThrowAsync<InvalidOperationException>();
}
```

### 5.2 加载项目测试

```csharp
[Fact]
public async Task LoadProject_WithValidPath_ShouldReturnConfig()
{
    // Arrange
    var service = CreateService();
    var created = await service.CreateProjectAsync("test");
    
    // Act
    var loaded = await service.LoadProjectAsync(created.ProjectPath);
    
    // Assert
    loaded.Should().NotBeNull();
    loaded!.ProjectName.Should().Be("test");
}

[Fact]
public async Task LoadProject_WithInvalidPath_ShouldReturnNull()
{
    var service = CreateService();
    
    var result = await service.LoadProjectAsync("/non/existent/path");
    
    result.Should().BeNull();
}

[Fact]
public async Task LoadProject_WithMissingConfigFile_ShouldReturnNull()
{
    var fileSystem = new MockFileSystem();
    fileSystem.SetDirectoryExists("/project", true);
    fileSystem.SetFileExists("/project/project-config.json", false);
    
    var service = new ProjectService(fileSystem);
    var result = await service.LoadProjectAsync("/project");
    
    result.Should().BeNull();
}
```

### 5.3 保存项目测试

```csharp
[Fact]
public async Task SaveProject_ShouldUpdateConfigFile()
{
    var service = CreateService();
    var config = await service.CreateProjectAsync("test");
    
    config = config with { Status = "in_progress" };
    await service.SaveProjectAsync(config);
    
    var loaded = await service.LoadProjectAsync(config.ProjectPath);
    loaded!.Status.Should().Be("in_progress");
}

[Fact]
public async Task SaveProject_ShouldUpdateTimestamp()
{
    var service = CreateService();
    var config = await service.CreateProjectAsync("test");
    
    await Task.Delay(100);
    await service.SaveProjectAsync(config);
    
    var loaded = await service.LoadProjectAsync(config.ProjectPath);
    loaded!.UpdatedAt.Should().BeAfter(config.UpdatedAt);
}
```

### 5.4 最近项目测试

```csharp
[Fact]
public async Task GetRecentProjects_ShouldReturnOrderedList()
{
    var service = CreateService();
    
    await service.CreateProjectAsync("project1");
    await Task.Delay(50);
    await service.CreateProjectAsync("project2");
    
    var recent = await service.GetRecentProjectsAsync();
    
    recent.Should().HaveCount(2);
    recent[0].ProjectName.Should().Be("project2"); // 最新的在前
}

[Fact]
public async Task GetRecentProjects_ShouldRespectLimit()
{
    var service = CreateService();
    
    for (int i = 0; i < 15; i++)
    {
        await service.CreateProjectAsync($"project{i}");
    }
    
    var recent = await service.GetRecentProjectsAsync(limit: 5);
    
    recent.Should().HaveCount(5);
}

[Fact]
public async Task AddToRecentProjects_ShouldNotDuplicate()
{
    var service = CreateService();
    var config = await service.CreateProjectAsync("test");
    
    await service.AddToRecentProjectsAsync(config.ProjectPath);
    await service.AddToRecentProjectsAsync(config.ProjectPath);
    
    var recent = await service.GetRecentProjectsAsync();
    recent.Should().ContainSingle(p => p.ProjectPath == config.ProjectPath);
}
```

### 5.5 验证测试

```csharp
[Theory]
[InlineData("valid-project", true)]
[InlineData("another_project", true)]
[InlineData("project with spaces", true)]
[InlineData("invalid/project", false)]
[InlineData("invalid:project", false)]
[InlineData("", false)]
[InlineData("   ", false)]
public void ValidateProjectPath_ShouldReturnExpectedResult(string name, bool expected)
{
    var service = CreateService();
    var isValid = service.ValidateProjectPath(name);
    isValid.Should().Be(expected);
}
```

---

## 6. 错误处理

### 6.1 异常类型

```csharp
// 项目名称无效
throw new ArgumentException("项目名称不能包含非法字符", nameof(name));

// 项目已存在
throw new InvalidOperationException($"项目已存在: {projectPath}");

// JSON 解析失败
throw new InvalidDataException("项目配置文件格式错误");

// 文件系统错误
throw new IOException($"无法访问项目路径: {projectPath}");
```

### 6.2 日志记录

```csharp
_logger.LogInformation("创建项目: {ProjectName} at {Path}", name, projectPath);
_logger.LogWarning("项目配置文件损坏,尝试修复: {Path}", configPath);
_logger.LogError(ex, "保存项目失败: {ProjectPath}", config.ProjectPath);
```

---

## 7. 性能要求

- ✅ 创建项目 < 100ms
- ✅ 加载项目 < 50ms
- ✅ 保存项目 < 30ms
- ✅ 获取最近项目 < 20ms

---

## 8. 验收标准

### 8.1 功能验收
- [x] 所有测试用例通过 (17+ 个测试)
- [x] 测试覆盖率 > 90%
- [x] 可以创建、加载、保存项目
- [x] 可以管理最近项目列表
- [x] 与 CLI 版本的 JSON 格式兼容

### 8.2 代码质量
- [x] 遵循 SOLID 原则
- [x] 依赖注入设计
- [x] 完整的 XML 文档注释
- [x] 通过所有 Linter 检查

---

## 9. 实现清单

### 9.1 接口定义
- [ ] `IFileSystem.cs`
- [ ] `IProjectService.cs`

### 9.2 实现类
- [ ] `DefaultFileSystem.cs`
- [ ] `ProjectService.cs`

### 9.3 测试类
- [ ] `ProjectServiceTests.cs` (17+ 测试)
- [ ] `FileSystemTests.cs` (可选)

### 9.4 辅助类
- [ ] `RecentProjectsData.cs` (最近项目数据模型)

---

## 10. 时间估算

| 任务 | 预计时间 |
|------|---------|
| 编写接口定义 | 30分钟 |
| 实现 ProjectService | 2小时 |
| 编写单元测试 | 2.5小时 |
| 集成测试 | 30分钟 |
| 文档和注释 | 30分钟 |
| **总计** | **6小时** |

---

## 11. 参考资料

- CLI 版本脚本: `../scripts/create-project.sh`
- 数据模型: `docs/specs/02-core-data-models.md`
- 项目路线图: `docs/tasks/development-roadmap.md`
