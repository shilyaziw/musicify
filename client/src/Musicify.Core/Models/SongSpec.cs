using System.Text.Json.Serialization;
using Musicify.Core.Models.Constants;

namespace Musicify.Core.Models;

/// <summary>
/// 歌曲规格定义
/// 对应文件: spec.json
/// 定义歌曲的类型、风格、时长、受众等核心规格
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
    /// 可选值: 流行/摇滚/说唱/民谣/电子/古风/R&amp;B/爵士/乡村/金属
    /// </summary>
    [JsonPropertyName("song_type")]
    public required string SongType { get; init; }
    
    /// <summary>
    /// 目标时长 (格式示例: "3分30秒", "4分钟")
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
    /// 例如: "温暖治愈", "黑暗压抑"等
    /// </summary>
    [JsonPropertyName("tone")]
    public string? Tone { get; init; }
    
    /// <summary>
    /// 创建时间 (UTC)
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }
    
    /// <summary>
    /// 最后更新时间 (UTC)
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; init; }
    
    /// <summary>
    /// 验证规格完整性和有效性
    /// </summary>
    /// <returns>验证结果,包含错误信息列表</returns>
    public ValidationResult Validate()
    {
        var errors = new List<string>();
        
        // 验证必填字段
        if (string.IsNullOrWhiteSpace(ProjectName))
            errors.Add("项目名称不能为空");
            
        // 验证歌曲类型
        if (!SongTypes.IsValid(SongType))
            errors.Add($"无效的歌曲类型: {SongType}");
            
        // 验证时长
        if (string.IsNullOrWhiteSpace(Duration))
            errors.Add("时长不能为空");
            
        // 验证风格
        if (!Styles.IsValid(Style))
            errors.Add($"无效的风格: {Style}");
            
        // 验证语言
        if (!Languages.IsValid(Language))
            errors.Add($"无效的语言: {Language}");
            
        // 验证目标平台
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
/// 定义目标听众的年龄段和性别倾向
/// </summary>
public sealed record AudienceInfo
{
    /// <summary>
    /// 年龄段 (例: "20-30岁", "全年龄")
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
/// 包含验证状态和错误信息列表
/// </summary>
public sealed record ValidationResult
{
    /// <summary>
    /// 验证是否通过
    /// </summary>
    public required bool IsValid { get; init; }
    
    /// <summary>
    /// 错误信息列表 (验证失败时)
    /// </summary>
    public required List<string> Errors { get; init; }
}
