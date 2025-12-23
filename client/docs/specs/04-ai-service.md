# Spec 04: AI 服务接口 (AIService)

**状态**: 🟢 实现中  
**优先级**: P0 (核心功能)  
**预计时间**: 8 小时  
**依赖**: Spec 03 (项目配置服务)

---

## 1. 需求概述

### 1.1 功能目标
实现 Claude API 集成,提供**歌词生成、创作引导、流式响应**等 AI 能力,支持三种创作模式。

### 1.2 核心功能
- ✅ Claude API 调用封装
- ✅ 流式响应处理 (Server-Sent Events)
- ✅ 提示词模板管理
- ✅ 三种创作模式支持 (Coach/Express/Hybrid)
- ✅ 错误处理与重试机制
- ✅ Token 使用统计

### 1.3 与 CLI 版本的区别
- **CLI**: 直接使用环境变量 `ANTHROPIC_API_KEY`
- **Desktop**: 支持 UI 配置,密钥加密存储
- **CLI**: 基于 TypeScript/Anthropic SDK
- **Desktop**: 基于 C#/Anthropic.SDK (v0.4.0)

---

## 2. 技术规格

### 2.1 服务接口设计

```csharp
namespace Musicify.Core.Services;

/// <summary>
/// AI 服务接口
/// </summary>
public interface IAIService
{
    /// <summary>
    /// 生成歌词 (流式)
    /// </summary>
    /// <param name="request">AI 请求</param>
    /// <param name="onChunk">流式数据回调</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<AIResponse> GenerateLyricsStreamAsync(
        AIRequest request, 
        Action<string> onChunk,
        CancellationToken cancellationToken = default
    );
    
    /// <summary>
    /// 生成歌词 (一次性)
    /// </summary>
    Task<AIResponse> GenerateLyricsAsync(AIRequest request);
    
    /// <summary>
    /// 验证 API 密钥是否有效
    /// </summary>
    Task<bool> ValidateApiKeyAsync(string apiKey);
    
    /// <summary>
    /// 获取可用模型列表
    /// </summary>
    List<string> GetAvailableModels();
    
    /// <summary>
    /// 获取 Token 使用统计
    /// </summary>
    TokenUsage GetTokenUsage();
}
```

### 2.2 数据模型设计

```csharp
namespace Musicify.Core.Models;

/// <summary>
/// AI 请求模型
/// </summary>
public record AIRequest
{
    /// <summary>
    /// 创作模式: coach, express, hybrid
    /// </summary>
    public required string Mode { get; init; }
    
    /// <summary>
    /// 项目配置
    /// </summary>
    public required ProjectConfig Project { get; init; }
    
    /// <summary>
    /// 歌曲规格
    /// </summary>
    public required SongSpec Spec { get; init; }
    
    /// <summary>
    /// 用户输入 (Express/Hybrid 模式)
    /// </summary>
    public string? UserInput { get; init; }
    
    /// <summary>
    /// 旋律分析结果 (如果有)
    /// </summary>
    public MidiAnalysisResult? MelodyAnalysis { get; init; }
    
    /// <summary>
    /// 自定义系统提示词 (可选)
    /// </summary>
    public string? SystemPrompt { get; init; }
    
    /// <summary>
    /// 最大 Token 数
    /// </summary>
    public int MaxTokens { get; init; } = 4096;
    
    /// <summary>
    /// 温度参数 (0.0 - 1.0)
    /// </summary>
    public double Temperature { get; init; } = 0.7;
}

/// <summary>
/// AI 响应模型
/// </summary>
public record AIResponse
{
    /// <summary>
    /// 生成的内容
    /// </summary>
    public required string Content { get; init; }
    
    /// <summary>
    /// 使用的模型
    /// </summary>
    public required string Model { get; init; }
    
    /// <summary>
    /// Token 使用情况
    /// </summary>
    public required TokenUsage Usage { get; init; }
    
    /// <summary>
    /// 停止原因: end_turn, max_tokens, stop_sequence
    /// </summary>
    public required string StopReason { get; init; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Token 使用统计
/// </summary>
public record TokenUsage
{
    /// <summary>
    /// 输入 Token 数
    /// </summary>
    public int InputTokens { get; init; }
    
    /// <summary>
    /// 输出 Token 数
    /// </summary>
    public int OutputTokens { get; init; }
    
    /// <summary>
    /// 总计 Token 数
    /// </summary>
    public int TotalTokens => InputTokens + OutputTokens;
    
    /// <summary>
    /// 估算成本 (美元)
    /// </summary>
    public decimal EstimatedCost { get; init; }
}
```

### 2.3 提示词模板系统

```csharp
namespace Musicify.Core.Services;

/// <summary>
/// 提示词模板服务
/// </summary>
public interface IPromptTemplateService
{
    /// <summary>
    /// 获取系统提示词
    /// </summary>
    string GetSystemPrompt(string mode);
    
    /// <summary>
    /// 获取用户提示词
    /// </summary>
    string GetUserPrompt(AIRequest request);
    
    /// <summary>
    /// 格式化提示词 (替换变量)
    /// </summary>
    string FormatPrompt(string template, Dictionary<string, string> variables);
}

/// <summary>
/// 提示词模板实现
/// </summary>
public class PromptTemplateService : IPromptTemplateService
{
    private readonly Dictionary<string, string> _systemPrompts = new()
    {
        ["coach"] = """
            你是一位资深的歌词创作导师,擅长引导用户进行深度创作。
            
            你的任务是:
            1. 理解用户的创作意图和歌曲主题
            2. 通过提问引导用户挖掘更深层的情感和故事
            3. 在充分讨论后,协助创作符合要求的歌词
            4. 提供专业的修改建议和写作技巧
            
            创作原则:
            - 尊重用户的原创思路
            - 鼓励情感真实表达
            - 注重韵律和节奏感
            - 符合目标受众和平台要求
            """,
            
        ["express"] = """
            你是一位高效的歌词创作专家,擅长快速创作高质量歌词。
            
            你的任务是:
            1. 基于用户提供的主题和情感,快速创作歌词
            2. 确保歌词符合指定的歌曲类型、风格和时长
            3. 自动优化韵律和节奏
            4. 生成结构完整的歌词(Verse/Chorus/Bridge)
            
            创作原则:
            - 高效直接,减少互动
            - 保持专业性和艺术性
            - 符合商业音乐标准
            - 适配目标发布平台
            """,
            
        ["hybrid"] = """
            你是一位灵活的歌词创作助手,结合引导和执行能力。
            
            你的任务是:
            1. 先进行简短的创作讨论(1-2 轮)
            2. 快速理解用户意图和核心需求
            3. 基于讨论结果创作歌词初稿
            4. 根据反馈进行迭代优化
            
            创作原则:
            - 平衡效率和质量
            - 适度引导,快速产出
            - 保持创作灵活性
            - 支持快速迭代
            """
    };

    public string GetSystemPrompt(string mode)
    {
        return _systemPrompts.TryGetValue(mode, out var prompt) 
            ? prompt 
            : _systemPrompts["express"];
    }

    public string GetUserPrompt(AIRequest request)
    {
        var template = request.Mode switch
        {
            "coach" => GetCoachPrompt(),
            "express" => GetExpressPrompt(),
            "hybrid" => GetHybridPrompt(),
            _ => GetExpressPrompt()
        };

        return FormatPrompt(template, new Dictionary<string, string>
        {
            ["PROJECT_NAME"] = request.Project.ProjectName,
            ["SONG_TYPE"] = request.Spec.SongType,
            ["DURATION"] = request.Spec.Duration?.ToString() ?? "未指定",
            ["STYLE"] = request.Spec.Style ?? "未指定",
            ["LANGUAGE"] = request.Spec.Language,
            ["TARGET_AUDIENCE"] = request.Spec.TargetAudience ?? "大众听众",
            ["TARGET_PLATFORM"] = string.Join(", ", request.Spec.TargetPlatform),
            ["USER_INPUT"] = request.UserInput ?? "",
            ["MELODY_INFO"] = FormatMelodyInfo(request.MelodyAnalysis)
        });
    }

    private string GetExpressPrompt()
    {
        return """
            # 歌词创作任务
            
            ## 项目信息
            - 项目名称: {PROJECT_NAME}
            - 歌曲类型: {SONG_TYPE}
            - 目标时长: {DURATION}
            - 风格基调: {STYLE}
            - 语言: {LANGUAGE}
            - 目标受众: {TARGET_AUDIENCE}
            - 发布平台: {TARGET_PLATFORM}
            
            ## 用户创作意图
            {USER_INPUT}
            
            ## 旋律参考信息
            {MELODY_INFO}
            
            ## 要求
            请基于以上信息创作一首完整的歌词,包含:
            1. [Verse 1] - 主歌第一段
            2. [Chorus] - 副歌
            3. [Verse 2] - 主歌第二段
            4. [Chorus] - 副歌重复
            5. [Bridge] - 桥段 (可选)
            6. [Chorus] - 副歌结尾
            
            注意事项:
            - 确保韵律和节奏符合歌曲类型
            - 情感表达要符合风格基调
            - 歌词长度要匹配目标时长
            - 语言风格要适合目标受众
            """;
    }

    private string GetCoachPrompt()
    {
        return """
            # 歌词创作引导任务
            
            ## 项目信息
            - 项目名称: {PROJECT_NAME}
            - 歌曲类型: {SONG_TYPE}
            - 风格基调: {STYLE}
            - 语言: {LANGUAGE}
            
            ## 初步想法
            {USER_INPUT}
            
            ## 引导流程
            请先通过 2-3 个问题深入了解:
            1. 这首歌想表达的核心情感是什么?
            2. 有没有具体的故事或场景?
            3. 希望听众听完后有什么感受?
            
            在充分讨论后,我们再开始正式创作歌词。
            """;
    }

    private string GetHybridPrompt()
    {
        return """
            # 歌词快速创作任务
            
            ## 项目信息
            - 歌曲类型: {SONG_TYPE}
            - 风格基调: {STYLE}
            - 语言: {LANGUAGE}
            
            ## 创作主题
            {USER_INPUT}
            
            ## 旋律特征
            {MELODY_INFO}
            
            ## 工作流程
            1. 先确认一下创作方向 (1 个问题)
            2. 快速生成歌词初稿
            3. 根据您的反馈优化
            
            请问,您希望这首歌的情感重点是什么? (例如: 怀旧/励志/浪漫/伤感)
            """;
    }

    public string FormatPrompt(string template, Dictionary<string, string> variables)
    {
        var result = template;
        foreach (var (key, value) in variables)
        {
            result = result.Replace($"{{{key}}}", value);
        }
        return result;
    }

    private string FormatMelodyInfo(MidiAnalysisResult? analysis)
    {
        if (analysis == null)
            return "无旋律参考";

        return $"""
            - 音域: {analysis.NoteRange.Min} - {analysis.NoteRange.Max}
            - 调式: {analysis.ModeInfo.DetectedMode}
            - 节奏特点: {string.Join(", ", analysis.RhythmPatterns.Take(3).Select(kv => kv.Key))}
            """;
    }
}
```

---

## 3. 实现设计

### 3.1 ClaudeService 实现

```csharp
using Anthropic.SDK;
using Anthropic.SDK.Messaging;

namespace Musicify.Core.Services;

/// <summary>
/// Claude API 服务实现
/// </summary>
public class ClaudeService : IAIService
{
    private readonly AnthropicClient _client;
    private readonly IPromptTemplateService _promptService;
    private readonly IConfiguration _configuration;
    private TokenUsage _totalUsage = new();

    public ClaudeService(
        IConfiguration configuration,
        IPromptTemplateService promptService)
    {
        _configuration = configuration;
        _promptService = promptService;
        
        var apiKey = configuration["AI:ApiKey"] 
            ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
            ?? throw new InvalidOperationException("未配置 Claude API Key");

        _client = new AnthropicClient(apiKey);
    }

    public async Task<AIResponse> GenerateLyricsStreamAsync(
        AIRequest request,
        Action<string> onChunk,
        CancellationToken cancellationToken = default)
    {
        var systemPrompt = request.SystemPrompt ?? _promptService.GetSystemPrompt(request.Mode);
        var userPrompt = _promptService.GetUserPrompt(request);
        
        var model = _configuration["AI:DefaultModel"] ?? "claude-3-5-sonnet-20241022";

        var messages = new List<Message>
        {
            new Message
            {
                Role = RoleType.User,
                Content = userPrompt
            }
        };

        var parameters = new MessageParameters
        {
            Messages = messages,
            MaxTokens = request.MaxTokens,
            Model = model,
            System = new List<SystemMessage> { new() { Text = systemPrompt } },
            Temperature = (decimal)request.Temperature,
            Stream = true
        };

        var fullContent = new StringBuilder();
        
        await foreach (var res in _client.Messages.StreamClaudeMessageAsync(parameters, cancellationToken))
        {
            if (res.Delta?.Text != null)
            {
                var chunk = res.Delta.Text;
                fullContent.Append(chunk);
                onChunk(chunk);
            }
        }

        var content = fullContent.ToString();
        var usage = new TokenUsage
        {
            InputTokens = EstimateTokens(systemPrompt + userPrompt),
            OutputTokens = EstimateTokens(content),
            EstimatedCost = CalculateCost(model, EstimateTokens(systemPrompt + userPrompt), EstimateTokens(content))
        };

        _totalUsage = new TokenUsage
        {
            InputTokens = _totalUsage.InputTokens + usage.InputTokens,
            OutputTokens = _totalUsage.OutputTokens + usage.OutputTokens,
            EstimatedCost = _totalUsage.EstimatedCost + usage.EstimatedCost
        };

        return new AIResponse
        {
            Content = content,
            Model = model,
            Usage = usage,
            StopReason = "end_turn"
        };
    }

    public async Task<AIResponse> GenerateLyricsAsync(AIRequest request)
    {
        var chunks = new List<string>();
        return await GenerateLyricsStreamAsync(
            request,
            chunk => chunks.Add(chunk)
        );
    }

    public async Task<bool> ValidateApiKeyAsync(string apiKey)
    {
        try
        {
            var testClient = new AnthropicClient(apiKey);
            var parameters = new MessageParameters
            {
                Messages = new List<Message>
                {
                    new() { Role = RoleType.User, Content = "Hello" }
                },
                MaxTokens = 10,
                Model = "claude-3-5-sonnet-20241022"
            };

            await testClient.Messages.GetClaudeMessageAsync(parameters);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public List<string> GetAvailableModels()
    {
        return new List<string>
        {
            "claude-3-5-sonnet-20241022",
            "claude-3-5-haiku-20241022",
            "claude-3-opus-20240229",
            "claude-3-sonnet-20240229",
            "claude-3-haiku-20240307"
        };
    }

    public TokenUsage GetTokenUsage() => _totalUsage;

    /// <summary>
    /// 估算 Token 数 (简化版: ~4 字符 = 1 token)
    /// </summary>
    private int EstimateTokens(string text)
    {
        return text.Length / 4;
    }

    /// <summary>
    /// 计算成本
    /// </summary>
    private decimal CalculateCost(string model, int inputTokens, int outputTokens)
    {
        // Claude 3.5 Sonnet 价格 (截至 2024-10)
        // Input: $3 / 1M tokens
        // Output: $15 / 1M tokens
        
        var (inputCost, outputCost) = model switch
        {
            "claude-3-5-sonnet-20241022" => (3.0m, 15.0m),
            "claude-3-5-haiku-20241022" => (0.8m, 4.0m),
            "claude-3-opus-20240229" => (15.0m, 75.0m),
            _ => (3.0m, 15.0m)
        };

        return (inputTokens * inputCost / 1_000_000) + (outputTokens * outputCost / 1_000_000);
    }
}
```

---

## 4. 测试用例设计

### 4.1 模拟 API 响应测试

```csharp
[Fact]
public async Task GenerateLyrics_ShouldReturnValidResponse()
{
    // Arrange
    var mockClient = CreateMockClient();
    var service = new ClaudeService(mockConfig, mockPromptService);
    
    var request = new AIRequest
    {
        Mode = "express",
        Project = CreateTestProject(),
        Spec = CreateTestSpec(),
        UserInput = "一首关于友情的歌"
    };

    // Act
    var response = await service.GenerateLyricsAsync(request);

    // Assert
    response.Should().NotBeNull();
    response.Content.Should().NotBeEmpty();
    response.Model.Should().Be("claude-3-5-sonnet-20241022");
    response.Usage.TotalTokens.Should().BeGreaterThan(0);
}
```

### 4.2 流式响应测试

```csharp
[Fact]
public async Task GenerateLyricsStream_ShouldCallOnChunk()
{
    var service = CreateService();
    var chunks = new List<string>();
    
    var request = CreateTestRequest();

    await service.GenerateLyricsStreamAsync(
        request,
        chunk => chunks.Add(chunk)
    );

    chunks.Should().NotBeEmpty();
    chunks.Should().Contain(c => c.Length > 0);
}
```

### 4.3 API Key 验证测试

```csharp
[Theory]
[InlineData("sk-ant-valid-key", true)]
[InlineData("invalid-key", false)]
[InlineData("", false)]
public async Task ValidateApiKey_ShouldReturnExpectedResult(string apiKey, bool expected)
{
    var service = CreateService();
    
    var isValid = await service.ValidateApiKeyAsync(apiKey);
    
    isValid.Should().Be(expected);
}
```

### 4.4 提示词模板测试

```csharp
[Theory]
[InlineData("coach")]
[InlineData("express")]
[InlineData("hybrid")]
public void GetSystemPrompt_ShouldReturnValidPrompt(string mode)
{
    var service = new PromptTemplateService();
    
    var prompt = service.GetSystemPrompt(mode);
    
    prompt.Should().NotBeEmpty();
    prompt.Should().Contain("歌词");
}

[Fact]
public void FormatPrompt_ShouldReplaceVariables()
{
    var service = new PromptTemplateService();
    var template = "项目: {PROJECT_NAME}, 类型: {SONG_TYPE}";
    var variables = new Dictionary<string, string>
    {
        ["PROJECT_NAME"] = "测试歌曲",
        ["SONG_TYPE"] = "流行"
    };

    var result = service.FormatPrompt(template, variables);

    result.Should().Be("项目: 测试歌曲, 类型: 流行");
}
```

### 4.5 Token 统计测试

```csharp
[Fact]
public async Task GetTokenUsage_ShouldAccumulateUsage()
{
    var service = CreateService();
    
    await service.GenerateLyricsAsync(CreateTestRequest());
    await service.GenerateLyricsAsync(CreateTestRequest());
    
    var usage = service.GetTokenUsage();
    
    usage.TotalTokens.Should().BeGreaterThan(0);
    usage.EstimatedCost.Should().BeGreaterThan(0);
}
```

---

## 5. 错误处理

### 5.1 异常类型

```csharp
// API Key 未配置
throw new InvalidOperationException("未配置 Claude API Key");

// API 调用失败
throw new HttpRequestException("Claude API 调用失败", innerException);

// 速率限制
throw new InvalidOperationException("API 调用频率超限,请稍后重试");

// Token 超限
throw new InvalidOperationException("生成内容超过 Token 限制");
```

### 5.2 重试机制

```csharp
public class RetryPolicy
{
    public static async Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        int maxRetries = 3,
        int delayMs = 1000)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                return await operation();
            }
            catch (HttpRequestException) when (i < maxRetries - 1)
            {
                await Task.Delay(delayMs * (i + 1));
            }
        }
        
        throw new InvalidOperationException("重试次数已用尽");
    }
}
```

---

## 6. 配置管理

### 6.1 appsettings.json

```json
{
  "AI": {
    "Provider": "Claude",
    "DefaultModel": "claude-3-5-sonnet-20241022",
    "MaxTokens": 4096,
    "Temperature": 0.7,
    "Timeout": 30000,
    "MaxRetries": 3
  }
}
```

### 6.2 密钥存储

```csharp
// 使用 .NET User Secrets 存储敏感信息
// dotnet user-secrets set "AI:ApiKey" "sk-ant-xxx"

// 或使用环境变量
// export ANTHROPIC_API_KEY=sk-ant-xxx
```

---

## 7. 性能要求

- ✅ API 调用响应 < 30s
- ✅ 流式响应首字节 < 2s
- ✅ 内存占用 < 100MB
- ✅ 支持并发请求 (最多 3 个)

---

## 8. 验收标准

### 8.1 功能验收
- [x] 所有测试用例通过 (15+ 个测试)
- [x] 测试覆盖率 > 85%
- [x] 三种创作模式正常工作
- [x] 流式响应稳定
- [x] API Key 验证准确

### 8.2 代码质量
- [x] 遵循 SOLID 原则
- [x] 依赖注入设计
- [x] 完整的异常处理
- [x] 详细的 XML 文档注释

---

## 9. 实现清单

### 9.1 接口定义
- [ ] `IAIService.cs`
- [ ] `IPromptTemplateService.cs`

### 9.2 数据模型
- [ ] `AIRequest.cs`
- [ ] `AIResponse.cs`
- [ ] `TokenUsage.cs`

### 9.3 实现类
- [ ] `ClaudeService.cs`
- [ ] `PromptTemplateService.cs`

### 9.4 测试类
- [ ] `ClaudeServiceTests.cs` (10+ 测试)
- [ ] `PromptTemplateServiceTests.cs` (5+ 测试)

---

## 10. 时间估算

| 任务 | 预计时间 |
|------|---------|
| 编写接口定义 | 1小时 |
| 实现数据模型 | 1小时 |
| 实现 ClaudeService | 3小时 |
| 实现提示词模板 | 1.5小时 |
| 编写单元测试 | 2.5小时 |
| 集成测试 | 1小时 |
| **总计** | **10小时** |

---

## 11. 安全注意事项

### 11.1 API Key 保护
- ❌ 不要硬编码在代码中
- ✅ 使用环境变量或 User Secrets
- ✅ 在日志中脱敏处理

### 11.2 输入验证
- ✅ 验证用户输入长度
- ✅ 过滤敏感词汇
- ✅ 限制 Token 数量

---

## 12. 参考资料

- [Anthropic API Documentation](https://docs.anthropic.com/)
- [Anthropic.SDK (NuGet)](https://www.nuget.org/packages/Anthropic.SDK)
- [Claude 模型定价](https://www.anthropic.com/pricing)
- CLI 版本实现: `../src/services/ai-service.ts`
- 提示词模板: `../templates/`
