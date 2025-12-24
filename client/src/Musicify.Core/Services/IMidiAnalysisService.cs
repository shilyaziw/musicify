using Musicify.Core.Models;

namespace Musicify.Core.Services;

/// <summary>
/// MIDI 分析服务接口
/// </summary>
public interface IMidiAnalysisService
{
    /// <summary>
    /// 分析 MIDI 文件
    /// </summary>
    /// <param name="midiFilePath">MIDI 文件路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分析结果</returns>
    Task<MidiAnalysisResult> AnalyzeAsync(
        string midiFilePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证 MIDI 文件是否有效
    /// </summary>
    /// <param name="midiFilePath">MIDI 文件路径</param>
    /// <returns>文件是否有效</returns>
    bool ValidateMidiFile(string midiFilePath);

    /// <summary>
    /// 获取 MIDI 文件基本信息
    /// </summary>
    /// <param name="midiFilePath">MIDI 文件路径</param>
    /// <returns>基本信息 (总音轨数、时长等)</returns>
    Task<MidiFileInfo> GetFileInfoAsync(string midiFilePath);
}

