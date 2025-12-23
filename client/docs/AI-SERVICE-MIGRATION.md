# AI 服务迁移说明

**迁移时间**: 2024-12-23  
**原因**: Anthropic.SDK 版本兼容性问题，改为支持多模型架构

---

## 📋 变更概述

### 从 Anthropic.SDK 迁移到通用 HTTP 客户端

**之前**:
- 使用 `Anthropic.SDK` NuGet 包
- 仅支持 Claude 模型
- 依赖特定 SDK 版本

**现在**:
- 使用 `HttpClient` 直接调用 API
- 支持多种模型提供商: OpenAI, Anthropic, Ollama 等
- 无第三方 SDK 依赖，更灵活

---

## 🎯 支持的模型提供商

### 1. OpenAI (默认)
- **模型**: gpt-4o, gpt-4o-mini, gpt-4-turbo, gpt-4, gpt-3.5-turbo
- **API 格式**: OpenAI 兼容格式
- **配置**: `AI:Provider = "OpenAI"`

### 2. Anthropic
- **模型**: claude-3-5-sonnet, claude-3-5-haiku, claude-3-opus 等
- **API 格式**: Anthropic 原生格式
- **配置**: `AI:Provider = "Anthropic"`

### 3. Ollama (本地部署)
- **模型**: llama3, llama3.1, mistral, mixtral 等
- **API 格式**: OpenAI 兼容格式
- **配置**: `AI:Provider = "Ollama"`, `AI:BaseUrl = "http://localhost:11434/v1"`

### 4. 其他兼容 OpenAI API 的服务
- 任何兼容 OpenAI API 格式的服务都可以使用
- 只需配置 `AI:BaseUrl` 和 `AI:ApiKey`

---

## ⚙️ 配置方式

### 方式 1: appsettings.json (推荐)

```json
{
  "AI": {
    "Provider": "OpenAI",
    "BaseUrl": "https://api.openai.com/v1",
    "ApiKey": "sk-xxx",
    "DefaultModel": "gpt-4o",
    "MaxTokens": 4096,
    "Temperature": 0.7
  }
}
```

### 方式 2: 环境变量

```bash
# 设置提供商
export AI__Provider="OpenAI"

# 设置 API Key
export AI__ApiKey="sk-xxx"

# 设置默认模型
export AI__DefaultModel="gpt-4o"

# 设置基础 URL (可选)
export AI__BaseUrl="https://api.openai.com/v1"
```

### 方式 3: 代码配置 (App.axaml.cs)

```csharp
var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["AI:Provider"] = "OpenAI",
        ["AI:ApiKey"] = "sk-xxx",
        ["AI:DefaultModel"] = "gpt-4o"
    })
    .Build();
```

---

## 🔧 代码变更

### 服务注册

**之前**:
```csharp
services.AddSingleton<IAIService, ClaudeService>();
```

**现在**:
```csharp
services.AddHttpClient();
services.AddSingleton<IAIService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    var config = sp.GetRequiredService<IConfiguration>();
    var promptService = sp.GetRequiredService<IPromptTemplateService>();
    return new HttpAIService(httpClient, config, promptService);
});
```

### 接口保持不变

`IAIService` 接口没有变化，所有现有代码都可以继续使用：

```csharp
public interface IAIService
{
    Task<AIResponse> GenerateLyricsStreamAsync(
        AIRequest request, 
        Action<string> onChunk,
        CancellationToken cancellationToken = default);
    
    Task<AIResponse> GenerateLyricsAsync(AIRequest request);
    Task<bool> ValidateApiKeyAsync(string apiKey);
    List<string> GetAvailableModels();
    TokenUsage GetTokenUsage();
}
```

---

## 📦 依赖变更

### 移除的包
- ❌ `Anthropic.SDK` (0.4.0)

### 新增的包
- ✅ `Microsoft.Extensions.Http` (8.0.0) - HTTP 客户端工厂
- ✅ `Microsoft.Extensions.Configuration` (8.0.0) - 配置管理

### 保留的包
- ✅ `System.Text.Json` (8.0.4) - JSON 序列化
- ✅ `Microsoft.Extensions.DependencyInjection` (8.0.0) - 依赖注入

---

## 🚀 使用示例

### 切换模型提供商

```csharp
// 在配置中切换
configuration["AI:Provider"] = "Anthropic";
configuration["AI:ApiKey"] = "sk-ant-xxx";
configuration["AI:DefaultModel"] = "claude-3-5-sonnet-20241022";
```

### 使用本地 Ollama

```json
{
  "AI": {
    "Provider": "Ollama",
    "BaseUrl": "http://localhost:11434/v1",
    "ApiKey": "",  // Ollama 不需要 API Key
    "DefaultModel": "llama3"
  }
}
```

### 使用自定义 API 端点

```json
{
  "AI": {
    "Provider": "OpenAI",  // 使用 OpenAI 兼容格式
    "BaseUrl": "https://your-custom-api.com/v1",
    "ApiKey": "your-api-key",
    "DefaultModel": "gpt-4o"
  }
}
```

---

## 🔄 迁移步骤

1. ✅ **已移除 Anthropic.SDK 依赖**
2. ✅ **已创建 HttpAIService 实现**
3. ✅ **已更新依赖注入配置**
4. ⏳ **更新配置文件** (appsettings.json)
5. ⏳ **测试不同模型提供商**
6. ⏳ **更新文档和示例**

---

## 📝 注意事项

### 1. API Key 安全
- 不要将 API Key 提交到版本控制
- 使用环境变量或 User Secrets 存储敏感信息

### 2. 流式响应格式
- OpenAI 格式: Server-Sent Events (SSE)
- Anthropic 格式: Server-Sent Events (SSE) with event types
- 两种格式都已实现

### 3. Token 估算
- 当前使用简化算法: ~4 字符 = 1 token
- 未来可以集成更精确的 tokenizer

### 4. 成本计算
- OpenAI 和 Anthropic 的成本已实现
- Ollama 等本地模型成本为 0
- 其他提供商需要添加成本计算逻辑

---

## 🎯 未来扩展

### 计划支持的功能

1. **更多模型提供商**
   - Google Gemini
   - 阿里云通义千问
   - 腾讯混元
   - 其他国产大模型

2. **模型切换 UI**
   - 在设置界面选择模型提供商
   - 实时切换，无需重启

3. **多模型负载均衡**
   - 自动选择可用模型
   - 故障转移

4. **精确 Token 计算**
   - 集成 tiktoken (OpenAI)
   - 集成其他 tokenizer

---

## 📚 相关文档

- [验证报告](./VALIDATION-REPORT.md)
- [AI 服务规范](./specs/04-ai-service.md)
- [项目进度](./SDD-PROGRESS.md)

---

**迁移完成时间**: 2024-12-23  
**状态**: ✅ 已完成基础实现，待测试验证

