namespace Musicify.Core.Models;

/// <summary>
/// MIDI 分析结果
/// </summary>
public record MidiAnalysisResult(
    /// <summary>
    /// 文件路径
    /// </summary>
    string FilePath,
    
    /// <summary>
    /// 总音符数
    /// </summary>
    int TotalNotes,
    
    /// <summary>
    /// 音符范围 (最低音, 最高音)
    /// </summary>
    (int Min, int Max) NoteRange,
    
    /// <summary>
    /// 节奏型分布 (节奏类型 -> 出现频率)
    /// </summary>
    Dictionary<string, float> RhythmPatterns,
    
    /// <summary>
    /// 音程分布 (音程类型 -> 出现频率)
    /// </summary>
    Dictionary<string, float> IntervalDistribution,
    
    /// <summary>
    /// 调式分析
    /// </summary>
    ModeAnalysis ModeInfo
);

/// <summary>
/// 调式分析结果
/// </summary>
public record ModeAnalysis(
    /// <summary>
    /// 检测到的调式 (如 "C Major", "A Minor")
    /// </summary>
    string DetectedMode,
    
    /// <summary>
    /// 置信度 (0.0 - 1.0)
    /// </summary>
    float Confidence,
    
    /// <summary>
    /// 音阶音符
    /// </summary>
    List<string> ScaleNotes
);
