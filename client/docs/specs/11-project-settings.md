# Spec 11: 项目设置界面

**状态**: 🟢 已完成（测试待补充）  
**优先级**: P1 (重要功能)  
**预计时间**: 6 小时  
**依赖**: 
- Spec 02 (核心数据模型)
- Spec 03 (项目服务)
- Spec 07 (主编辑窗口)

---

## 1. 需求概述

### 1.1 功能目标
实现项目设置界面，允许用户编辑项目配置和歌曲规格信息，包括项目名称、歌曲类型、风格、语言、受众、平台等。

### 1.2 核心功能
- ✅ 项目基本信息编辑（项目名称）
- ✅ 歌曲类型选择
- ✅ 目标时长设置
- ✅ 风格基调选择
- ✅ 语言选择
- ✅ 受众定位（年龄、性别）
- ✅ 目标平台选择（多选）
- ✅ 歌曲基调选择
- ✅ 保存和重置功能
- ✅ 表单验证

### 1.3 用户流程
1. 用户在主窗口点击"项目设置"
2. 系统加载当前项目的配置信息
3. 显示项目设置界面，包含所有可编辑字段
4. 用户修改设置
5. 点击保存按钮
6. 系统保存配置并更新项目状态

---

## 2. 技术规格

### 2.1 ViewModel 设计

```csharp
namespace Musicify.Core.ViewModels;

/// <summary>
/// 项目设置界面 ViewModel
/// </summary>
public class ProjectSettingsViewModel : ViewModelBase
{
    private readonly IProjectService _projectService;
    private readonly IFileSystem _fileSystem;
    
    // 当前项目
    public ProjectConfig? CurrentProject { get; set; }
    
    // 项目配置属性
    public string ProjectName { get; set; }
    public string SongType { get; set; }
    public string Duration { get; set; }
    public string Style { get; set; }
    public string Language { get; set; }
    public string AudienceAge { get; set; }
    public string AudienceGender { get; set; }
    public List<string> SelectedPlatforms { get; set; }
    public string Tone { get; set; }
    
    // 下拉选项
    public List<string> SongTypes { get; }
    public List<string> Styles { get; }
    public List<string> Languages { get; }
    public List<string> Platforms { get; }
    public List<string> AudienceAges { get; }
    public List<string> AudienceGenders { get; }
    
    // 状态
    public bool IsSaving { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }
    
    // 命令
    public ICommand SaveSettingsCommand { get; }
    public ICommand ResetCommand { get; }
    
    // 方法
    public Task SetProjectAsync(ProjectConfig project);
}
```

### 2.2 数据模型

使用现有的 `ProjectConfig` 和 `SongSpec` 模型：

```csharp
// ProjectConfig 包含基本信息
public record ProjectConfig
{
    public string Name { get; init; }
    public SongSpec? Spec { get; init; }
    // ...
}

// SongSpec 包含歌曲规格
public record SongSpec
{
    public string SongType { get; init; }
    public string Duration { get; init; }
    public string Style { get; init; }
    public string Language { get; init; }
    public AudienceInfo Audience { get; init; }
    public List<string> Platforms { get; init; }
    public string Tone { get; init; }
    // ...
}
```

---

## 3. UI 设计

### 3.1 界面布局

```
┌─────────────────────────────────────────────────────────┐
│  ⚙️ 项目设置                                             │
│  编辑项目配置和歌曲规格信息                              │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  ┌─ 基本信息 ─────────────────────────────────────┐    │
│  │ 项目名称: [________________]                    │    │
│  │ 歌曲类型: [下拉选择 ▼]                         │    │
│  │ 目标时长: [________________]                    │    │
│  └────────────────────────────────────────────────┘    │
│                                                          │
│  ┌─ 风格与语言 ───────────────────────────────────┐    │
│  │ 风格基调: [下拉选择 ▼]                         │    │
│  │ 语言:     [下拉选择 ▼]                         │    │
│  └────────────────────────────────────────────────┘    │
│                                                          │
│  ┌─ 受众定位 ─────────────────────────────────────┐    │
│  │ 年龄:   [下拉选择 ▼]                           │    │
│  │ 性别:   [下拉选择 ▼]                           │    │
│  └────────────────────────────────────────────────┘    │
│                                                          │
│  ┌─ 发布平台 ─────────────────────────────────────┐    │
│  │ ☑ QQ音乐  ☑ 网易云音乐  ☐ 酷狗音乐            │    │
│  │ ☐ 抖音    ☐ 快手       ☐ B站                  │    │
│  │ ☐ Spotify ☐ YouTube Music                      │    │
│  └────────────────────────────────────────────────┘    │
│                                                          │
│  ┌─ 歌曲基调 ─────────────────────────────────────┐    │
│  │ [下拉选择 ▼]                                   │    │
│  └────────────────────────────────────────────────┘    │
│                                                          │
│  [重置]                              [💾 保存设置]      │
└─────────────────────────────────────────────────────────┘
```

### 3.2 表单字段说明

#### 基本信息
- **项目名称**: 文本输入框，必填
- **歌曲类型**: 下拉选择，选项来自 `Constants.SongTypes`
- **目标时长**: 文本输入框，格式如 "3分30秒"

#### 风格与语言
- **风格基调**: 下拉选择，选项来自 `Constants.Styles`
- **语言**: 下拉选择，选项来自 `Constants.Languages`

#### 受众定位
- **年龄**: 下拉选择，选项：儿童、青少年、青年、中年、老年
- **性别**: 下拉选择，选项：男性、女性、中性、不限

#### 发布平台
- **多选复选框**: 选项来自 `Constants.Platforms`
- 支持全选/取消全选

#### 歌曲基调
- **下拉选择**: 选项：积极向上、伤感忧郁、浪漫温馨、激情澎湃、平静舒缓等

---

## 4. 实现细节

### 4.1 数据加载

```csharp
public async Task SetProjectAsync(ProjectConfig project)
{
    CurrentProject = project;
    
    // 加载项目配置
    ProjectName = project.Name;
    
    // 加载歌曲规格
    if (project.Spec != null)
    {
        SongType = project.Spec.SongType;
        Duration = project.Spec.Duration;
        Style = project.Spec.Style;
        Language = project.Spec.Language;
        AudienceAge = project.Spec.Audience.Age;
        AudienceGender = project.Spec.Audience.Gender;
        SelectedPlatforms = project.Spec.Platforms ?? new List<string>();
        Tone = project.Spec.Tone;
    }
}
```

### 4.2 保存设置

```csharp
private async Task SaveSettingsAsync()
{
    if (CurrentProject == null)
    {
        ErrorMessage = "请先打开项目";
        return;
    }

    try
    {
        IsSaving = true;
        ErrorMessage = null;
        
        // 更新项目配置
        var updatedProject = CurrentProject with
        {
            Name = ProjectName,
            Spec = new SongSpec
            {
                SongType = SongType,
                Duration = Duration,
                Style = Style,
                Language = Language,
                Audience = new AudienceInfo
                {
                    Age = AudienceAge,
                    Gender = AudienceGender
                },
                Platforms = SelectedPlatforms,
                Tone = Tone
            },
            UpdatedAt = DateTime.UtcNow
        };
        
        // 保存到文件
        await _projectService.SaveProjectAsync(updatedProject);
        CurrentProject = updatedProject;
        
        SuccessMessage = "设置已保存";
    }
    catch (Exception ex)
    {
        ErrorMessage = $"保存失败: {ex.Message}";
    }
    finally
    {
        IsSaving = false;
    }
}
```

### 4.3 重置功能

```csharp
private void ResetSettings()
{
    if (CurrentProject != null)
    {
        _ = SetProjectAsync(CurrentProject);
    }
}
```

### 4.4 平台选择处理

使用 `PlatformSelectionConverter` 处理多选平台：

```csharp
// 检查平台是否选中
public bool IsPlatformSelected(string platform)
{
    return SelectedPlatforms.Contains(platform);
}

// 切换平台选择状态
public void TogglePlatform(string platform)
{
    if (SelectedPlatforms.Contains(platform))
    {
        SelectedPlatforms.Remove(platform);
    }
    else
    {
        SelectedPlatforms.Add(platform);
    }
    OnPropertyChanged(nameof(SelectedPlatforms));
}
```

---

## 5. 表单验证

### 5.1 必填字段
- **项目名称**: 不能为空
- **歌曲类型**: 必须选择

### 5.2 格式验证
- **目标时长**: 格式验证（可选）
- **平台选择**: 至少选择一个平台（可选）

### 5.3 验证提示
- 实时显示验证错误
- 保存时进行完整验证
- 友好的错误提示信息

---

## 6. 错误处理

### 6.1 常见错误场景

- **项目未打开**: 显示"请先打开项目"
- **保存失败**: 显示具体错误信息
- **数据格式错误**: 显示验证错误

### 6.2 错误处理策略

- 所有错误通过 `ErrorMessage` 属性显示
- 保存过程中捕获异常，不中断应用
- 提供友好的错误提示信息

---

## 7. 性能要求

- ✅ 界面加载时间 < 200ms
- ✅ 保存操作时间 < 500ms
- ✅ UI 响应流畅，不阻塞

---

## 8. 验收标准

### 8.1 功能验收
- [x] 所有字段正确加载和显示
- [x] 保存功能正常工作
- [x] 重置功能正常工作
- [x] 表单验证正确
- [x] 下拉选项完整

### 8.2 UI 验收
- [x] 界面布局美观
- [x] 表单字段清晰
- [x] 状态提示友好
- [x] 响应式设计

### 8.3 代码质量
- [x] 遵循 MVVM 模式
- [x] 依赖注入设计
- [x] 完整的异常处理
- [x] 详细的 XML 文档注释

---

## 9. 实现清单

### 9.1 ViewModel
- [x] `ProjectSettingsViewModel.cs`

### 9.2 Views
- [x] `ProjectSettingsView.axaml` + `.cs`

### 9.3 Converters
- [x] `PlatformSelectionConverter.cs` (多值转换器)

### 9.4 测试
- [ ] `ProjectSettingsViewModelTests.cs` (待补充)

### 9.5 DI 注册
- [x] 在 `App.axaml.cs` 中注册 `ProjectSettingsViewModel`

---

## 10. 时间估算

| 任务 | 预计时间 |
|------|---------|
| 编写 Spec 文档 | 1小时 |
| 实现 ViewModel | 2小时 |
| 实现 UI 界面 | 2小时 |
| 集成和测试 | 1小时 |
| **总计** | **6小时** |

---

## 11. 与之前循环的协同

### 11.1 核心数据模型 (SDD #1)
- ✅ 使用 `ProjectConfig` 和 `SongSpec` 模型
- ✅ 使用 `Constants` 类提供下拉选项

### 11.2 项目服务 (SDD #2)
- ✅ 使用 `IProjectService.SaveProjectAsync` 保存配置
- ✅ 使用 `IProjectService.LoadProjectAsync` 加载配置

### 11.3 主编辑窗口 (SDD #6)
- ✅ 从 `MainWindowViewModel` 导航到项目设置界面
- ✅ 共享 `CurrentProject` 数据

---

## 12. 未来扩展

### 12.1 高级设置
- 导出配置
- 自动保存设置
- 主题设置

### 12.2 设置模板
- 保存常用设置模板
- 快速应用模板

### 12.3 设置验证增强
- 更详细的字段验证
- 实时验证提示
- 设置建议

---

**Spec 完成时间**: 2024-12-23  
**下一步**: 补充测试用例

