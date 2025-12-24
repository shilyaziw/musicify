namespace Musicify.Core.Models;

/// <summary>
/// MIDI 文件基本信息
/// </summary>
/// <param name="FilePath">文件路径</param>
/// <param name="TrackCount">音轨数量</param>
/// <param name="Duration">文件时长</param>
/// <param name="TicksPerQuarterNote">每四分音符的 Tick 数</param>
/// <param name="Tempo">速度 (BPM)</param>
public record MidiFileInfo(
    string FilePath,
    int TrackCount,
    TimeSpan Duration,
    int TicksPerQuarterNote,
    int Tempo
);

