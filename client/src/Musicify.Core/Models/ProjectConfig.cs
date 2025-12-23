using System.Text.Json.Serialization;

namespace Musicify.Core.Models;

/// <summary>
/// 项目配置信息
/// 对应文件: .musicify/config.json
/// 与 CLI 版本保持 JSON 格式兼容
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
    /// 脚本类型 (sh/ps1, Desktop 版本不使用但保留兼容性)
    /// </summary>
    [JsonPropertyName("scriptType")]
    public string? ScriptType { get; init; }
    
    /// <summary>
    /// 默认歌曲类型
    /// </summary>
    [JsonPropertyName("defaultType")]
    public string? DefaultType { get; init; }
    
    /// <summary>
    /// 创建时间 (UTC)
    /// </summary>
    [JsonPropertyName("created")]
    public DateTime Created { get; init; }
    
    /// <summary>
    /// 项目版本号
    /// </summary>
    [JsonPropertyName("version")]
    public required string Version { get; init; }
    
    /// <summary>
    /// 项目路径 (运行时属性,不序列化到 JSON)
    /// </summary>
    [JsonIgnore]
    public string? ProjectPath { get; init; }
    
    /// <summary>
    /// 项目状态 (运行时属性,不序列化到 JSON)
    /// </summary>
    [JsonIgnore]
    public string? Status { get; init; }
    
    /// <summary>
    /// 更新时间 (运行时属性,不序列化到 JSON)
    /// </summary>
    [JsonIgnore]
    public DateTime? UpdatedAt { get; init; }
    
    /// <summary>
    /// 歌曲规格 (运行时属性,不序列化到 JSON)
    /// </summary>
    [JsonIgnore]
    public SongSpec? Spec { get; init; }
    
    /// <summary>
    /// 验证配置有效性
    /// </summary>
    /// <returns>配置是否有效</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Name)
            && Type == "musicify-project"
            && !string.IsNullOrWhiteSpace(Version);
    }
}
