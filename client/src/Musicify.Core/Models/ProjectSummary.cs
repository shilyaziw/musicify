namespace Musicify.Core.Models;

/// <summary>
/// 项目信息摘要 (用于主窗口侧边栏显示)
/// </summary>
public record ProjectSummary(
    /// <summary>
    /// 项目名称
    /// </summary>
    string ProjectName,
    
    /// <summary>
    /// 项目状态
    /// </summary>
    string Status,
    
    /// <summary>
    /// 歌曲类型
    /// </summary>
    string SongType,
    
    /// <summary>
    /// 创建时间
    /// </summary>
    DateTime CreatedAt,
    
    /// <summary>
    /// 更新时间
    /// </summary>
    DateTime? UpdatedAt,
    
    /// <summary>
    /// 是否包含 MIDI 文件
    /// </summary>
    bool HasMidiFile,
    
    /// <summary>
    /// 是否包含歌词
    /// </summary>
    bool HasLyrics
);

