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
