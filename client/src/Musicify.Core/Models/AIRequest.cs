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
