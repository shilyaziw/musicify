using System.Text.Json.Serialization;

namespace Musicify.Core.Models;

/// <summary>
/// 聊天消息
/// </summary>
public record ChatMessage
{
    /// <summary>
    /// 消息类型 (User/AI)
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// 消息内容
    /// </summary>
    [JsonPropertyName("content")]
    public required string Content { get; init; }

    /// <summary>
    /// 时间戳
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; } = DateTime.Now;

    /// <summary>
    /// 是否正在流式生成 (不序列化到文件)
    /// </summary>
    [JsonIgnore]
    public bool IsStreaming { get; init; }

    /// <summary>
    /// Token 使用量 (仅 AI 消息)
    /// </summary>
    [JsonPropertyName("tokenCount")]
    public int? TokenCount { get; init; }
}

