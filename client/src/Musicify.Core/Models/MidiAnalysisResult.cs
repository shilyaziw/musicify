namespace Musicify.Core.Models;

/// <summary>
/// MIDI 分析结果
/// </summary>
/// <param name="FilePath">文件路径</param>
/// <param name="TotalNotes">总音符数</param>
/// <param name="NoteRange">音符范围 (最低音, 最高音)</param>
/// <param name="RhythmPatterns">节奏型分布 (节奏类型 -> 出现频率)</param>
/// <param name="IntervalDistribution">音程分布 (音程类型 -> 出现频率)</param>
/// <param name="ModeInfo">调式分析</param>
public record MidiAnalysisResult(
    string FilePath,
    int TotalNotes,
    (int Min, int Max) NoteRange,
    Dictionary<string, float> RhythmPatterns,
    Dictionary<string, float> IntervalDistribution,
    ModeAnalysis ModeInfo
);

/// <summary>
/// 调式分析结果
/// </summary>
/// <param name="DetectedMode">检测到的调式 (如 "C Major", "A Minor")</param>
/// <param name="Confidence">置信度 (0.0 - 1.0)</param>
/// <param name="ScaleNotes">音阶音符</param>
public record ModeAnalysis(
    string DetectedMode,
    float Confidence,
    List<string> ScaleNotes
);
