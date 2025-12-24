using Musicify.Core.Models;

namespace Musicify.Core.Services;

/// <summary>
/// 导出服务接口
/// </summary>
public interface IExportService
{
    /// <summary>
    /// 导出歌词到文本文件
    /// </summary>
    /// <param name="lyrics">歌词内容</param>
    /// <param name="filePath">输出文件路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ExportToTextAsync(LyricsContent lyrics, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 导出歌词到 JSON 文件
    /// </summary>
    /// <param name="lyrics">歌词内容</param>
    /// <param name="filePath">输出文件路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ExportToJsonAsync(LyricsContent lyrics, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 导出歌词到 Markdown 文件
    /// </summary>
    /// <param name="lyrics">歌词内容</param>
    /// <param name="filePath">输出文件路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ExportToMarkdownAsync(LyricsContent lyrics, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 导出歌词到 LRC 文件（歌词同步格式）
    /// </summary>
    /// <param name="lyrics">歌词内容</param>
    /// <param name="filePath">输出文件路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ExportToLrcAsync(LyricsContent lyrics, string filePath, CancellationToken cancellationToken = default);
}

