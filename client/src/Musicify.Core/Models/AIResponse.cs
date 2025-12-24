namespace Musicify.Core.Models;

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
