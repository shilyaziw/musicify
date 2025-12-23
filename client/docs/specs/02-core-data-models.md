# Spec: 核心数据模型设计

**文档版本**: v1.0  
**创建日期**: 2025-12-23  
**状态**: 🟡 进行中  
**预计时间**: 4 小时  
**优先级**: P0 (最高)

---

## 📋 概述

设计 Musicify Desktop 的核心数据模型,确保与 CLI 版本的 JSON 格式完全兼容,支持序列化/反序列化,并提供类型安全的 API。

---

## 🎯 用户故事

> 作为 **开发者**,  
> 我想要 **类型安全、不可变的数据模型**,  
> 以便 **在整个应用中安全地传递和持久化数据**

> 作为 **系统**,  
> 我想要 **与 CLI 版本兼容的数据格式**,  
> 以便 **用户可以在 CLI 和 Desktop 之间无缝切换**

---

## 💡 功能需求

### Must Have (必须实现)

- [x] 定义 `ProjectConfig` 模型 (项目配置)
- [x] 定义 `SongSpec` 模型 (歌曲规格)
- [x] 定义 `LyricsContent` 模型 (歌词内容)
- [x] 定义 `Project` 聚合模型
- [x] JSON 序列化兼容性 (snake_case ↔ PascalCase)
- [x] 所有模型使用 `init` 属性 (不可变性)
- [x] 所有模型支持 XML 文档注释

### Should Have (应该实现)

- [ ] 数据验证特性 (Validation Attributes)
- [ ] 自定义 JSON 转换器 (处理日期格式等)
- [ ] Builder 模式 (方便测试)

### Could Have (可以实现)

- [ ] 模型变更追踪
- [ ] 版本迁移支持

---

## 🏗 技术规格

### 1. ProjectConfig 模型

**用途**: 存储在 `.musicify/config.json` 中的项目配置

**JSON 示例** (CLI 格式):
```json
{
  "name": "我的歌曲",
  "type": "musicify-project",
  "ai": "claude",
  "scriptType": "sh",
  "defaultType": "流行",
  "created": "2025-12-23T10:30:00Z",
  "version": "0.1.0"
}
```

**C# 模型定义**:

```csharp
namespace Musicify.Core.Models;

/// <summary>
/// 项目配置信息
/// 对应文件: .musicify/config.json
/// </summary>
public sealed record ProjectConfig
{
    /// <summary>
    /// 项目名称
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }
    
    /// <summary>
    /// 项目类型标识 (固定值: "musicify-project")
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }
    
    /// <summary>
    /// AI 助手类型 (claude/cursor/gemini等)
    /// Desktop 版本固定为 "desktop"
    /// </summary>
    [JsonPropertyName("ai")]
    public string Ai { get; init; } = "desktop";
    
    /// <summary>
    /// 脚本类型 (sh/ps1, Desktop 版本不使用)
    /// </summary>
    [JsonPropertyName("scriptType")]
    public string? ScriptType { get; init; }
    
    /// <summary>
    /// 默认歌曲类型
    /// </summary>
    [JsonPropertyName("defaultType")]
    public string? DefaultType { get; init; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    [JsonPropertyName("created")]
    public DateTime Created { get; init; }
    
    /// <summary>
    /// 项目版本号
    /// </summary>
    [JsonPropertyName("version")]
    public required string Version { get; init; }
    
    /// <summary>
    /// 验证配置有效性
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Name)
            && Type == "musicify-project"
            && !string.IsNullOrWhiteSpace(Version);
    }
}
```

**设计决策**:
1. 使用 `record` 类型实现值语义和不可变性
2. 使用 `required` 关键字标记必需属性
3. 使用 `JsonPropertyName` 保持与 CLI 的 JSON 兼容性
4. 提供 `IsValid()` 方法用于验证

---

### 2. SongSpec 模型

**用途**: 存储在 `spec.json` 中的歌曲规格

**JSON 示例** (CLI 格式):
```json
{
  "project_name": "我的歌曲",
  "song_type": "流行",
  "duration": "3分30秒",
  "style": "抒情",
  "language": "中文",
  "audience": {
    "age": "20-30岁",
    "gender": "中性"
  },
  "target_platform": ["QQ音乐", "网易云音乐", "抖音"],
  "tone": "温暖治愈",
  "created_at": "2025-12-23T10:30:00Z",
  "updated_at": "2025-12-23T10:30:00Z"
}
```

**C# 模型定义**:

```csharp
/// <summary>
/// 歌曲规格定义
/// 对应文件: spec.json
/// </summary>
public sealed record SongSpec
{
    /// <summary>
    /// 项目名称
    /// </summary>
    [JsonPropertyName("project_name")]
    public required string ProjectName { get; init; }
    
    /// <summary>
    /// 歌曲类型
    /// 可选值: 流行/摇滚/说唱/民谣/电子/古风/R&B/爵士/乡村/金属
    /// </summary>
    [JsonPropertyName("song_type")]
    public required string SongType { get; init; }
    
    /// <summary>
    /// 目标时长 (例: "3分30秒")
    /// </summary>
    [JsonPropertyName("duration")]
    public required string Duration { get; init; }
    
    /// <summary>
    /// 风格基调
    /// 可选值: 抒情/激昂/轻快/忧郁/治愈/燃爆/平静/梦幻
    /// </summary>
    [JsonPropertyName("style")]
    public required string Style { get; init; }
    
    /// <summary>
    /// 歌词语言
    /// 可选值: 中文/英文/粤语/日语/韩语/中英混合/其他
    /// </summary>
    [JsonPropertyName("language")]
    public required string Language { get; init; }
    
    /// <summary>
    /// 目标受众信息
    /// </summary>
    [JsonPropertyName("audience")]
    public required AudienceInfo Audience { get; init; }
    
    /// <summary>
    /// 目标发布平台列表
    /// </summary>
    [JsonPropertyName("target_platform")]
    public required List<string> TargetPlatform { get; init; }
    
    /// <summary>
    /// 补充音调描述 (可选)
    /// </summary>
    [JsonPropertyName("tone")]
    public string? Tone { get; init; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }
    
    /// <summary>
    /// 最后更新时间
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; init; }
    
    /// <summary>
    /// 验证规格完整性
    /// </summary>
    public ValidationResult Validate()
    {
        var errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(ProjectName))
            errors.Add("项目名称不能为空");
            
        if (!SongTypes.IsValid(SongType))
            errors.Add($"无效的歌曲类型: {SongType}");
            
        if (string.IsNullOrWhiteSpace(Duration))
            errors.Add("时长不能为空");
            
        if (!Styles.IsValid(Style))
            errors.Add($"无效的风格: {Style}");
            
        if (!Languages.IsValid(Language))
            errors.Add($"无效的语言: {Language}");
            
        if (TargetPlatform == null || TargetPlatform.Count == 0)
            errors.Add("至少选择一个目标平台");
        
        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}

/// <summary>
/// 受众信息
/// </summary>
public sealed record AudienceInfo
{
    /// <summary>
    /// 年龄段 (例: "20-30岁")
    /// 可选值: 15-20岁/20-30岁/30-40岁/全年龄
    /// </summary>
    [JsonPropertyName("age")]
    public required string Age { get; init; }
    
    /// <summary>
    /// 性别倾向
    /// 可选值: 女性向/男性向/中性
    /// </summary>
    [JsonPropertyName("gender")]
    public required string Gender { get; init; }
}

/// <summary>
/// 验证结果
/// </summary>
public sealed record ValidationResult
{
    public required bool IsValid { get; init; }
    public required List<string> Errors { get; init; }
}
```

---

### 3. 常量定义 (Enums/Constants)

**用途**: 定义可选值的常量集合

```csharp
namespace Musicify.Core.Models.Constants;

/// <summary>
/// 歌曲类型常量
/// </summary>
public static class SongTypes
{
    public const string Pop = "流行";
    public const string Rock = "摇滚";
    public const string Rap = "说唱";
    public const string Folk = "民谣";
    public const string Electronic = "电子";
    public const string GuoFeng = "古风";
    public const string RnB = "R&B";
    public const string Jazz = "爵士";
    public const string Country = "乡村";
    public const string Metal = "金属";
    
    private static readonly HashSet<string> ValidTypes = new()
    {
        Pop, Rock, Rap, Folk, Electronic, GuoFeng, RnB, Jazz, Country, Metal
    };
    
    public static bool IsValid(string type) => ValidTypes.Contains(type);
    
    public static IReadOnlyList<string> All => new List<string>
    {
        Pop, Rock, Rap, Folk, Electronic, GuoFeng, RnB, Jazz, Country, Metal
    };
}

/// <summary>
/// 风格基调常量
/// </summary>
public static class Styles
{
    public const string Lyrical = "抒情";
    public const string Passionate = "激昂";
    public const string Cheerful = "轻快";
    public const string Melancholy = "忧郁";
    public const string Healing = "治愈";
    public const string Explosive = "燃爆";
    public const string Calm = "平静";
    public const string Dreamy = "梦幻";
    
    private static readonly HashSet<string> ValidStyles = new()
    {
        Lyrical, Passionate, Cheerful, Melancholy, Healing, Explosive, Calm, Dreamy
    };
    
    public static bool IsValid(string style) => ValidStyles.Contains(style);
    
    public static IReadOnlyList<string> All => new List<string>
    {
        Lyrical, Passionate, Cheerful, Melancholy, Healing, Explosive, Calm, Dreamy
    };
}

/// <summary>
/// 语言常量
/// </summary>
public static class Languages
{
    public const string Chinese = "中文";
    public const string English = "英文";
    public const string Cantonese = "粤语";
    public const string Japanese = "日语";
    public const string Korean = "韩语";
    public const string ChineseEnglish = "中英混合";
    public const string Other = "其他";
    
    private static readonly HashSet<string> ValidLanguages = new()
    {
        Chinese, English, Cantonese, Japanese, Korean, ChineseEnglish, Other
    };
    
    public static bool IsValid(string language) => ValidLanguages.Contains(language);
    
    public static IReadOnlyList<string> All => new List<string>
    {
        Chinese, English, Cantonese, Japanese, Korean, ChineseEnglish, Other
    };
}

/// <summary>
/// 目标平台常量
/// </summary>
public static class Platforms
{
    // 音乐平台
    public const string QQMusic = "QQ音乐";
    public const string NetEaseMusic = "网易云音乐";
    public const string KuGou = "酷狗音乐";
    public const string AppleMusic = "Apple Music";
    
    // 短视频平台
    public const string Douyin = "抖音";
    public const string Kuaishou = "快手";
    public const string Bilibili = "B站";
    
    // 国际平台
    public const string Spotify = "Spotify";
    public const string YouTubeMusic = "YouTube Music";
    
    public static IReadOnlyList<string> All => new List<string>
    {
        QQMusic, NetEaseMusic, KuGou, AppleMusic,
        Douyin, Kuaishou, Bilibili,
        Spotify, YouTubeMusic
    };
}
```

---

### 4. LyricsContent 模型

**用途**: 存储歌词内容

**JSON 示例**:
```json
{
  "project_name": "我的歌曲",
  "mode": "coach",
  "sections": [
    {
      "type": "Verse 1",
      "content": "三两笔着墨迟迟\n不为记事\n随手便成诗",
      "order": 1
    },
    {
      "type": "Chorus",
      "content": "多少往事随风去\n化作云烟散\n只留一曲探故知",
      "order": 2
    }
  ],
  "created_at": "2025-12-23T10:30:00Z"
}
```

**C# 模型定义**:

```csharp
/// <summary>
/// 歌词内容
/// 对应文件: lyrics.json
/// </summary>
public sealed record LyricsContent
{
    [JsonPropertyName("project_name")]
    public required string ProjectName { get; init; }
    
    /// <summary>
    /// 创作模式
    /// 可选值: coach/express/hybrid
    /// </summary>
    [JsonPropertyName("mode")]
    public required string Mode { get; init; }
    
    /// <summary>
    /// 歌词段落列表
    /// </summary>
    [JsonPropertyName("sections")]
    public required List<LyricsSection> Sections { get; init; }
    
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }
    
    /// <summary>
    /// 获取格式化的完整歌词文本
    /// </summary>
    public string ToFormattedText()
    {
        var sb = new StringBuilder();
        foreach (var section in Sections.OrderBy(s => s.Order))
        {
            sb.AppendLine($"[{section.Type}]");
            sb.AppendLine(section.Content);
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }
}

/// <summary>
/// 歌词段落
/// </summary>
public sealed record LyricsSection
{
    /// <summary>
    /// 段落类型 (Verse 1, Chorus, Bridge 等)
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }
    
    /// <summary>
    /// 段落内容
    /// </summary>
    [JsonPropertyName("content")]
    public required string Content { get; init; }
    
    /// <summary>
    /// 段落顺序
    /// </summary>
    [JsonPropertyName("order")]
    public int Order { get; init; }
    
    /// <summary>
    /// 获取行数
    /// </summary>
    public int LineCount => Content.Split('\n').Length;
    
    /// <summary>
    /// 获取字数
    /// </summary>
    public int CharCount => Content.Replace("\n", "").Replace(" ", "").Length;
}

/// <summary>
/// 创作模式常量
/// </summary>
public static class CreationModes
{
    public const string Coach = "coach";
    public const string Express = "express";
    public const string Hybrid = "hybrid";
    
    public static bool IsValid(string mode) => mode is Coach or Express or Hybrid;
}
```

---

### 5. Project 聚合根模型

**用途**: 内存中的完整项目实体

```csharp
/// <summary>
/// 项目聚合根
/// 包含项目的所有相关数据
/// </summary>
public sealed class Project
{
    /// <summary>
    /// 项目名称
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// 项目根目录路径
    /// </summary>
    public required string Path { get; init; }
    
    /// <summary>
    /// 项目配置
    /// </summary>
    public required ProjectConfig Config { get; init; }
    
    /// <summary>
    /// 歌曲规格 (可能为 null)
    /// </summary>
    public SongSpec? Spec { get; set; }
    
    /// <summary>
    /// 歌词内容 (可能为 null)
    /// </summary>
    public LyricsContent? Lyrics { get; set; }
    
    /// <summary>
    /// 项目是否已加载完整数据
    /// </summary>
    public bool IsLoaded { get; set; }
    
    // 辅助属性
    
    /// <summary>
    /// 配置文件路径
    /// </summary>
    public string ConfigPath => System.IO.Path.Combine(Path, ".musicify", "config.json");
    
    /// <summary>
    /// 规格文件路径
    /// </summary>
    public string SpecPath => System.IO.Path.Combine(Path, "spec.json");
    
    /// <summary>
    /// 歌词文件路径
    /// </summary>
    public string LyricsPath => System.IO.Path.Combine(Path, "lyrics.json");
    
    /// <summary>
    /// Workspace 目录路径
    /// </summary>
    public string WorkspacePath => System.IO.Path.Combine(Path, "workspace");
}

/// <summary>
/// 项目简要信息 (用于列表展示)
/// </summary>
public sealed record ProjectInfo
{
    public required string Name { get; init; }
    public required string Path { get; init; }
    public DateTime LastOpened { get; init; }
    public string? SongType { get; init; }
    public string? Thumbnail { get; init; }
}
```

---

## 🧪 测试用例

### Test Suite 1: ProjectConfig 序列化测试

```csharp
public class ProjectConfigTests
{
    [Fact]
    public void SerializeToJson_ShouldMatchCLIFormat()
    {
        // Arrange
        var config = new ProjectConfig
        {
            Name = "测试歌曲",
            Type = "musicify-project",
            Ai = "desktop",
            DefaultType = "流行",
            Created = new DateTime(2025, 12, 23, 10, 30, 0, DateTimeKind.Utc),
            Version = "1.0.0"
        };
        
        // Act
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        
        // Assert
        json.Should().Contain("\"name\": \"测试歌曲\"");
        json.Should().Contain("\"type\": \"musicify-project\"");
        json.Should().Contain("\"defaultType\": \"流行\"");
    }
    
    [Fact]
    public void DeserializeFromJson_ShouldRestoreObject()
    {
        // Arrange
        var json = """
        {
          "name": "测试歌曲",
          "type": "musicify-project",
          "ai": "claude",
          "defaultType": "流行",
          "created": "2025-12-23T10:30:00Z",
          "version": "1.0.0"
        }
        """;
        
        // Act
        var config = JsonSerializer.Deserialize<ProjectConfig>(json);
        
        // Assert
        config.Should().NotBeNull();
        config!.Name.Should().Be("测试歌曲");
        config.DefaultType.Should().Be("流行");
    }
    
    [Fact]
    public void IsValid_ShouldReturnTrue_WhenConfigIsValid()
    {
        // Arrange
        var config = CreateValidConfig();
        
        // Act & Assert
        config.IsValid().Should().BeTrue();
    }
    
    [Fact]
    public void IsValid_ShouldReturnFalse_WhenNameIsEmpty()
    {
        // Arrange
        var config = new ProjectConfig
        {
            Name = "",
            Type = "musicify-project",
            Version = "1.0.0",
            Created = DateTime.UtcNow
        };
        
        // Act & Assert
        config.IsValid().Should().BeFalse();
    }
}
```

### Test Suite 2: SongSpec 验证测试

```csharp
public class SongSpecTests
{
    [Fact]
    public void Validate_ShouldSucceed_WhenAllFieldsValid()
    {
        // Arrange
        var spec = CreateValidSpec();
        
        // Act
        var result = spec.Validate();
        
        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
    
    [Fact]
    public void Validate_ShouldFail_WhenSongTypeInvalid()
    {
        // Arrange
        var spec = CreateValidSpec() with { SongType = "无效类型" };
        
        // Act
        var result = spec.Validate();
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("无效的歌曲类型"));
    }
    
    [Theory]
    [InlineData("流行", true)]
    [InlineData("摇滚", true)]
    [InlineData("古风", true)]
    [InlineData("xxx", false)]
    public void SongTypes_IsValid_ShouldReturnCorrectResult(string type, bool expected)
    {
        // Act & Assert
        SongTypes.IsValid(type).Should().Be(expected);
    }
}
```

### Test Suite 3: LyricsContent 格式化测试

```csharp
public class LyricsContentTests
{
    [Fact]
    public void ToFormattedText_ShouldGenerateCorrectFormat()
    {
        // Arrange
        var lyrics = new LyricsContent
        {
            ProjectName = "测试",
            Mode = CreationModes.Coach,
            Sections = new List<LyricsSection>
            {
                new() { Type = "Verse 1", Content = "第一段\n歌词", Order = 1 },
                new() { Type = "Chorus", Content = "副歌部分", Order = 2 }
            },
            CreatedAt = DateTime.UtcNow
        };
        
        // Act
        var text = lyrics.ToFormattedText();
        
        // Assert
        text.Should().Contain("[Verse 1]");
        text.Should().Contain("[Chorus]");
        text.Should().Contain("第一段");
        text.Should().Contain("副歌部分");
    }
    
    [Fact]
    public void LyricsSection_LineCount_ShouldCountCorrectly()
    {
        // Arrange
        var section = new LyricsSection
        {
            Type = "Verse 1",
            Content = "第一行\n第二行\n第三行",
            Order = 1
        };
        
        // Act & Assert
        section.LineCount.Should().Be(3);
    }
}
```

---

## ✅ 验收标准

### 代码质量
- [ ] 所有模型使用 `record` 类型
- [ ] 所有必需属性标记 `required`
- [ ] 所有公开 API 有 XML 文档注释
- [ ] 使用 `JsonPropertyName` 保持兼容性
- [ ] 常量集中管理,避免魔法字符串

### 功能完整性
- [ ] JSON 序列化/反序列化测试通过
- [ ] 与 CLI 格式兼容性验证通过
- [ ] 所有验证逻辑测试通过
- [ ] 单元测试覆盖率 > 90%

### 性能要求
- [ ] 序列化 < 10ms (小对象)
- [ ] 验证 < 1ms

---

## 📅 时间估算

| 任务 | 预计时间 |
|------|---------|
| 定义 ProjectConfig | 30min |
| 定义 SongSpec + 常量 | 60min |
| 定义 LyricsContent | 30min |
| 定义 Project 聚合根 | 20min |
| 编写单元测试 | 90min |
| 文档完善 | 30min |
| **总计** | **4h** |

---

## 🔗 依赖关系

### 前置条件
- .NET 8 项目已创建
- System.Text.Json 已安装

### 后续任务
- Task 1.5: 实现 IProjectService
- Task 2.2: 规格编辑器 UI

---

## 📝 实现检查清单

- [ ] 在 `src/Musicify.Core/Models/` 创建所有模型文件
- [ ] 在 `src/Musicify.Core/Models/Constants/` 创建常量类
- [ ] 在 `tests/Musicify.Core.Tests/Models/` 创建测试文件
- [ ] 运行 `dotnet test` 验证所有测试通过
- [ ] 运行 `dotnet build` 确保无编译警告
- [ ] 更新此 Spec 文档状态为 ✅ 完成
