namespace Musicify.Core.Models;

/// <summary>
/// MIDI 文件基本信息
/// </summary>
public record MidiFileInfo(
    /// <summary>
    /// 文件路径
    /// </summary>
    string FilePath,
    
    /// <summary>
    /// 音轨数量
    /// </summary>
    int TrackCount,
    
    /// <summary>
    /// 文件时长
    /// </summary>
    TimeSpan Duration,
    
    /// <summary>
    /// 每四分音符的 Tick 数
    /// </summary>
    int TicksPerQuarterNote,
    
    /// <summary>
    /// 速度 (BPM)
    /// </summary>
    int Tempo
);

