namespace Musicify.Core.Models;

/// <summary>
/// 项目信息摘要 (用于主窗口侧边栏显示)
/// </summary>
/// <param name="ProjectName">项目名称</param>
/// <param name="Status">项目状态</param>
/// <param name="SongType">歌曲类型</param>
/// <param name="CreatedAt">创建时间</param>
/// <param name="UpdatedAt">更新时间</param>
/// <param name="HasMidiFile">是否包含 MIDI 文件</param>
/// <param name="HasLyrics">是否包含歌词</param>
public record ProjectSummary(
    string ProjectName,
    string Status,
    string SongType,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool HasMidiFile,
    bool HasLyrics
);

