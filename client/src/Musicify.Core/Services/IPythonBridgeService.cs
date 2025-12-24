using Musicify.Core.Models;

namespace Musicify.Core.Services;

/// <summary>
/// Python 脚本桥接服务接口
/// </summary>
public interface IPythonBridgeService
{
    /// <summary>
    /// 检查 Python 环境是否可用
    /// </summary>
    /// <returns>Python 环境信息，如果不可用则返回 null</returns>
    Task<PythonEnvironmentInfo?> CheckPythonEnvironmentAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行 MIDI 分析脚本
    /// </summary>
    /// <param name="midiFilePath">MIDI 文件路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分析结果（JSON 格式）</returns>
    Task<string> AnalyzeMidiAsync(string midiFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行音频转 MIDI 脚本
    /// </summary>
    /// <param name="audioFilePath">音频文件路径（MP3/WAV）</param>
    /// <param name="outputDirectory">输出目录（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理结果（JSON 格式）</returns>
    Task<string> ConvertAudioToMidiAsync(string audioFilePath, string? outputDirectory = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行自定义 Python 脚本
    /// </summary>
    /// <param name="scriptPath">Python 脚本路径</param>
    /// <param name="arguments">脚本参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>脚本输出（标准输出）</returns>
    Task<string> ExecutePythonScriptAsync(string scriptPath, IEnumerable<string>? arguments = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取 Python 脚本路径
    /// </summary>
    /// <param name="scriptName">脚本名称（如 "midi_analyzer.py"）</param>
    /// <returns>脚本完整路径，如果不存在则返回 null</returns>
    string? GetPythonScriptPath(string scriptName);
}

/// <summary>
/// Python 环境信息
/// </summary>
public record PythonEnvironmentInfo
{
    /// <summary>
    /// Python 可执行文件路径
    /// </summary>
    public required string PythonPath { get; init; }

    /// <summary>
    /// Python 版本
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// 是否可用
    /// </summary>
    public bool IsAvailable { get; init; }

    /// <summary>
    /// 已安装的依赖包
    /// </summary>
    public List<string> InstalledPackages { get; init; } = new();

    /// <summary>
    /// 缺失的依赖包
    /// </summary>
    public List<string> MissingPackages { get; init; } = new();
}

