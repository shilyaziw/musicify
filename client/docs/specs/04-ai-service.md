# Spec 04: AI 服务接口 (AIService)

**状态**: 🟢 实现中
**优先级**: P0 (核心功能)
**预计时间**: 8 小时
**依赖**: Spec 03 (项目配置服务)

---

## 1. 需求概述

### 1.1 功能目标
实现通用 AI 服务集成,提供**歌词生成、创作引导、流式响应**等 AI 能力,支持三种创作模式。支持多种 AI 提供商：OpenAI、Anthropic、Ollama 等。

### 1.2 核心功能
- ✅ 多 AI 提供商支持 (OpenAI, Anthropic, Ollama)
- ✅ 流式响应处理 (Server-Sent Events)
- ✅ 提示词模板管理
- ✅ 三种创作模式支持 (Coach/Express/Hybrid)
- ✅ 错误处理与重试机制
- ✅ Token 使用统计

### 1.3 与 CLI 版本的区别
- **CLI**: 直接使用环境变量 `ANTHROPIC_API_KEY`
- **Desktop**: 支持 UI 配置,密钥加密存储,支持多种 AI 提供商
- **CLI**: 基于 TypeScript/Anthropic SDK
- **Desktop**: 基于 C#/HttpClient (通用实现)

---

## 2. 技术规格

### 2.1 服务接口设计

```csharp
using Musicify.Core.Models;

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

### 3.1 HttpAIService 实现

```csharp
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Musicify.Core.Models;

namespace Musicify.Core.Services;

/// <summary>
/// 基于 HTTP 的通用 AI 服务实现
/// 支持多种模型提供商: OpenAI, Anthropic, 兼容 OpenAI API 的其他服务
/// </summary>
public class HttpAIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly IPromptTemplateService _promptService;
    private readonly IConfiguration _configuration;
    private TokenUsage _totalUsage = new();
    private readonly string _provider;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    public HttpAIService(
        HttpClient httpClient,
        IConfiguration configuration,
        IPromptTemplateService promptService)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _promptService = promptService ?? throw new ArgumentNullException(nameof(promptService));

        _provider = _configuration["AI:Provider"] ?? "OpenAI";
        _apiKey = _configuration["AI:ApiKey"]
            ?? Environment.GetEnvironmentVariable("AI_API_KEY")
            ?? throw new InvalidOperationException("未配置 AI API Key");

        // 根据提供商设置基础 URL
        _baseUrl = _provider switch
        {
            "OpenAI" => _configuration["AI:BaseUrl"] ?? "https://api.openai.com/v1",
            "Anthropic" => _configuration["AI:BaseUrl"] ?? "https://api.anthropic.com/v1",
            "Ollama" => _configuration["AI:BaseUrl"] ?? "http://localhost:11434/v1",
            _ => _configuration["AI:BaseUrl"] ?? "https://api.openai.com/v1"
        };

        // 配置 HTTP 客户端
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.DefaultRequestHeaders.Clear();

        if (_provider == "Anthropic")
        {
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }
    }

    public async Task<AIResponse> GenerateLyricsStreamAsync(
        AIRequest request,
        Action<string> onChunk,
        CancellationToken cancellationToken = default)
    {
        var systemPrompt = request.SystemPrompt ?? _promptService.GetSystemPrompt(request.Mode);
        var userPrompt = _promptService.GetUserPrompt(request);
        var model = _configuration["AI:DefaultModel"] ?? GetDefaultModel();

        if (_provider == "Anthropic")
        {
            return await GenerateAnthropicStreamAsync(model, systemPrompt, userPrompt, request, onChunk, cancellationToken);
        }
        else
        {
            return await GenerateOpenAICompatibleStreamAsync(model, systemPrompt, userPrompt, request, onChunk, cancellationToken);
        }
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
            // 使用简单的测试请求验证 API Key
            var testRequest = new AIRequest
            {
                Mode = "express",
                Project = new ProjectConfig
                {
                    Name = "test",
                    Type = "musicify-project",
                    Version = "1.0.0",
                    Created = DateTime.UtcNow
                },
                Spec = new SongSpec
                {
                    ProjectName = "test",
                    SongType = "流行",
                    Duration = "3分钟",
                    Style = "抒情",
                    Language = "中文",
                    Audience = new AudienceInfo { Age = "全年龄", Gender = "中性" },
                    TargetPlatform = new List<string> { "Suno" }
                },
                UserInput = "测试",
                MaxTokens = 10
            };

            await GenerateLyricsAsync(testRequest);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public List<string> GetAvailableModels()
    {
        return _provider switch
        {
            "OpenAI" => new List<string>
            {
                "gpt-4o",
                "gpt-4o-mini",
                "gpt-4-turbo",
                "gpt-4",
                "gpt-3.5-turbo"
            },
            "Anthropic" => new List<string>
            {
                "claude-3-5-sonnet-20241022",
                "claude-3-5-haiku-20241022",
                "claude-3-opus-20240229",
                "claude-3-sonnet-20240229",
                "claude-3-haiku-20240307"
            },
            "Ollama" => new List<string>
            {
                "llama3",
                "llama3.1",
                "mistral",
                "mixtral"
            },
            _ => new List<string> { "gpt-4o", "gpt-3.5-turbo" }
        };
    }

    public TokenUsage GetTokenUsage() => _totalUsage;

    private string GetDefaultModel()
    {
        return _provider switch
        {
            "OpenAI" => "gpt-4o",
            "Anthropic" => "claude-3-5-sonnet-20241022",
            "Ollama" => "llama3",
            _ => "gpt-4o"
        };
    }

    /// <summary>
    /// OpenAI 兼容格式的流式生成
    /// </summary>
    private async Task<AIResponse> GenerateOpenAICompatibleStreamAsync(
        string model,
        string systemPrompt,
        string userPrompt,
        AIRequest request,
        Action<string> onChunk,
        CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            max_tokens = request.MaxTokens,
            temperature = request.Temperature,
            stream = true
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/chat/completions", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var fullContent = new StringBuilder();
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
                continue;

            var data = line.Substring(6); // 移除 "data: " 前缀
            if (data == "[DONE]")
                break;

            try
            {
                var jsonDoc = JsonDocument.Parse(data);
                if (jsonDoc.RootElement.TryGetProperty("choices", out var choices) &&
                    choices[0].TryGetProperty("delta", out var delta) &&
                    delta.TryGetProperty("content", out var contentProp))
                {
                    var chunk = contentProp.GetString();
                    if (!string.IsNullOrEmpty(chunk))
                    {
                        fullContent.Append(chunk);
                        onChunk(chunk);
                    }
                }
            }
            catch (JsonException)
            {
                // 忽略无效的 JSON 行
                continue;
            }
        }

        var result = fullContent.ToString();
        var usage = new TokenUsage
        {
            InputTokens = EstimateTokens(systemPrompt + userPrompt),
            OutputTokens = EstimateTokens(result),
            EstimatedCost = CalculateCost(model, EstimateTokens(systemPrompt + userPrompt), EstimateTokens(result))
        };

        _totalUsage = new TokenUsage
        {
            InputTokens = _totalUsage.InputTokens + usage.InputTokens,
            OutputTokens = _totalUsage.OutputTokens + usage.OutputTokens,
            EstimatedCost = _totalUsage.EstimatedCost + usage.EstimatedCost
        };

        return new AIResponse
        {
            Content = result,
            Model = model,
            Usage = usage,
            StopReason = "stop"
        };
    }

    /// <summary>
    /// Anthropic API 格式的流式生成
    /// </summary>
    private async Task<AIResponse> GenerateAnthropicStreamAsync(
        string model,
        string systemPrompt,
        string userPrompt,
        AIRequest request,
        Action<string> onChunk,
        CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            model = model,
            max_tokens = request.MaxTokens,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = userPrompt }
            },
            temperature = request.Temperature,
            stream = true
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/messages", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var fullContent = new StringBuilder();
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("event: "))
                continue;

            var eventType = line.Substring(7);
            var dataLine = await reader.ReadLineAsync();

            if (dataLine == null || !dataLine.StartsWith("data: "))
                continue;

            var data = dataLine.Substring(6);
            if (eventType == "message_stop")
                break;

            try
            {
                var jsonDoc = JsonDocument.Parse(data);
                if (jsonDoc.RootElement.TryGetProperty("type", out var type) &&
                    type.GetString() == "content_block_delta" &&
                    jsonDoc.RootElement.TryGetProperty("delta", out var delta) &&
                    delta.TryGetProperty("text", out var textProp))
                {
                    var chunk = textProp.GetString();
                    if (!string.IsNullOrEmpty(chunk))
                    {
                        fullContent.Append(chunk);
                        onChunk(chunk);
                    }
                }
            }
            catch (JsonException)
            {
                continue;
            }
        }

        var result = fullContent.ToString();
        var usage = new TokenUsage
        {
            InputTokens = EstimateTokens(systemPrompt + userPrompt),
            OutputTokens = EstimateTokens(result),
            EstimatedCost = CalculateCost(model, EstimateTokens(systemPrompt + userPrompt), EstimateTokens(result))
        };

        _totalUsage = new TokenUsage
        {
            InputTokens = _totalUsage.InputTokens + usage.InputTokens,
            OutputTokens = _totalUsage.OutputTokens + usage.OutputTokens,
            EstimatedCost = _totalUsage.EstimatedCost + usage.EstimatedCost
        };

        return new AIResponse
        {
            Content = result,
            Model = model,
            Usage = usage,
            StopReason = "end_turn"
        };
    }

    /// <summary>
    /// 估算 Token 数 (简化版: ~4 字符 = 1 token)
    /// </summary>
    private int EstimateTokens(string text)
    {
        return text.Length / 4;
    }

    /// <summary>
    /// 计算成本 (根据提供商和模型)
    /// </summary>
    private decimal CalculateCost(string model, int inputTokens, int outputTokens)
    {
        return _provider switch
        {
            "OpenAI" => CalculateOpenAICost(model, inputTokens, outputTokens),
            "Anthropic" => CalculateAnthropicCost(model, inputTokens, outputTokens),
            "Ollama" => 0m, // 本地模型，无成本
            _ => 0m
        };
    }

    private decimal CalculateOpenAICost(string model, int inputTokens, int outputTokens)
    {
        var (inputCost, outputCost) = model switch
        {
            "gpt-4o" => (2.5m, 10.0m),
            "gpt-4o-mini" => (0.15m, 0.6m),
            "gpt-4-turbo" => (10.0m, 30.0m),
            "gpt-4" => (30.0m, 60.0m),
            "gpt-3.5-turbo" => (0.5m, 1.5m),
            _ => (2.5m, 10.0m)
        };
        return (inputTokens * inputCost / 1_000_000) + (outputTokens * outputCost / 1_000_000);
    }

    private decimal CalculateAnthropicCost(string model, int inputTokens, int outputTokens)
    {
        var (inputCost, outputCost) = model switch
        {
            "claude-3-5-sonnet-20241022" => (3.0m, 15.0m),
            "claude-3-5-haiku-20241022" => (0.8m, 4.0m),
            "claude-3-opus-20240229" => (15.0m, 75.0m),
            "claude-3-sonnet-20240229" => (3.0m, 15.0m),
            "claude-3-haiku-20240307" => (0.25m, 1.25m),
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
    "Provider": "OpenAI",
    "DefaultModel": "gpt-4o",
    "MaxTokens": 4096,
    "Temperature": 0.7,
    "Timeout": 30000,
    "MaxRetries": 3,
    "BaseUrl": "https://api.openai.com/v1"
  }
}
```

**支持的提供商配置**:

**OpenAI**:
```json
{
  "AI": {
    "Provider": "OpenAI",
    "DefaultModel": "gpt-4o",
    "BaseUrl": "https://api.openai.com/v1"
  }
}
```

**Anthropic**:
```json
{
  "AI": {
    "Provider": "Anthropic",
    "DefaultModel": "claude-3-5-sonnet-20241022",
    "BaseUrl": "https://api.anthropic.com/v1"
  }
}
```

**Ollama (本地)**:
```json
{
  "AI": {
    "Provider": "Ollama",
    "DefaultModel": "llama3",
    "BaseUrl": "http://localhost:11434/v1"
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
- [x] 支持多种AI提供商 (OpenAI, Anthropic, Ollama)
- [x] HTTP客户端配置正确

### 8.2 代码质量
- [x] 遵循 SOLID 原则
- [x] 依赖注入设计
- [x] 完整的异常处理
- [x] 详细的 XML 文档注释
- [x] 使用 HttpClient 进行 API 调用

---

## 9. 实现清单

### 8.1 接口定义
- [x] `IAIService.cs` - 包含流式和一次性AI生成方法
- [x] `IPromptTemplateService.cs` - 包含提示词模板管理功能

### 8.2 数据模型
- [x] `AIRequest.cs` - 包含详细的XML文档注释
- [x] `AIResponse.cs` - 包含详细的XML文档注释
- [x] `TokenUsage.cs` - 包含详细的XML文档注释

### 8.3 实现类
- [x] `HttpAIService.cs` - 支持多种AI提供商(OpenAI, Anthropic, Ollama)
- [x] `PromptTemplateService.cs` - 包含三种创作模式的提示词模板

### 8.4 测试类
- [x] `ClaudeServiceTests.cs` - 15+ 个测试用例

### 8.5 依赖注入
- [x] 在DI容器中注册相关服务

---

## 10. 时间估算

| 任务 | 预计时间 | 实际时间 |
|------|---------|----------|
| 编写接口定义 | 1小时 | 1小时 |
| 实现数据模型 | 1小时 | 1小时 |
| 实现 HttpAIService | 4小时 | 5小时 |
| 实现提示词模板 | 1.5小时 | 1.5小时 |
| 编写单元测试 | 2.5小时 | 2小时 |
| 支持多提供商 | 2小时 | 2.5小时 |
| 流式响应处理 | 2小时 | 2小时 |
| **总计** | **14小时** | **15小时** |

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
