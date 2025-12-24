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
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
            {
                continue;
            }

            var data = line.Substring(6); // 移除 "data: " 前缀
            if (data == "[DONE]")
            {
                break;
            }

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
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("event: "))
            {
                continue;
            }

            var eventType = line.Substring(7);
            var dataLine = await reader.ReadLineAsync(cancellationToken);

            if (dataLine == null || !dataLine.StartsWith("data: "))
            {
                continue;
            }

            var data = dataLine.Substring(6);
            if (eventType == "message_stop")
            {
                break;
            }

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
    private static int EstimateTokens(string text)
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

    private static decimal CalculateOpenAICost(string model, int inputTokens, int outputTokens)
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

    private static decimal CalculateAnthropicCost(string model, int inputTokens, int outputTokens)
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

