using System.Text;
using System.Text.Json;
using Musicify.Core.Abstractions;
using Musicify.Core.Models;

namespace Musicify.Core.Services;

/// <summary>
/// 导出服务实现
/// </summary>
public class ExportService : IExportService
{
    private readonly IFileSystem _fileSystem;

    public ExportService(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    /// <summary>
    /// 导出歌词到文本文件
    /// </summary>
    public async Task ExportToTextAsync(LyricsContent lyrics, string filePath, CancellationToken cancellationToken = default)
    {
        var content = lyrics.ToFormattedText();
        await _fileSystem.WriteAllTextAsync(filePath, content);
    }

    /// <summary>
    /// 导出歌词到 JSON 文件
    /// </summary>
    public async Task ExportToJsonAsync(LyricsContent lyrics, string filePath, CancellationToken cancellationToken = default)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var json = JsonSerializer.Serialize(lyrics, options);
        await _fileSystem.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    /// 导出歌词到 Markdown 文件
    /// </summary>
    public async Task ExportToMarkdownAsync(LyricsContent lyrics, string filePath, CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();

        // 标题
        sb.AppendLine($"# {lyrics.ProjectName}");
        sb.AppendLine();

        // 模式信息
        if (!string.IsNullOrEmpty(lyrics.Mode))
        {
            sb.AppendLine($"**模式**: {lyrics.Mode}");
            sb.AppendLine();
        }

        // 创建时间
        if (lyrics.CreatedAt != default)
        {
            sb.AppendLine($"**创建时间**: {lyrics.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine();

        // 歌词段落
        foreach (var section in lyrics.Sections.OrderBy(s => s.Order))
        {
            if (!string.IsNullOrEmpty(section.Type))
            {
                sb.AppendLine($"## {section.Type}");
                sb.AppendLine();
            }

            // 将 Content 按行分割
            var lines = section.Content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                sb.AppendLine(line.Trim());
            }

            sb.AppendLine();
        }

        await _fileSystem.WriteAllTextAsync(filePath, sb.ToString());
    }

    /// <summary>
    /// 导出歌词到 LRC 文件（歌词同步格式）
    /// </summary>
    public async Task ExportToLrcAsync(LyricsContent lyrics, string filePath, CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();

        // LRC 文件头信息
        sb.AppendLine("[ti:未命名]");
        sb.AppendLine("[ar:未知艺术家]");
        sb.AppendLine("[al:未知专辑]");
        sb.AppendLine("[by:Musicify]");
        sb.AppendLine();

        // 歌词内容
        // 注意：LRC 格式需要时间戳，但当前 LyricsContent 模型没有时间信息
        // 这里导出为简单的文本格式，时间戳为 [00:00.00]
        var lineIndex = 0;
        foreach (var section in lyrics.Sections.OrderBy(s => s.Order))
        {
            // 将 Content 按行分割
            var lines = section.Content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (!string.IsNullOrWhiteSpace(trimmedLine))
                {
                    var minutes = lineIndex / 60;
                    var seconds = lineIndex % 60;
                    var milliseconds = 0;
                    sb.AppendLine($"[{minutes:D2}:{seconds:D2}.{milliseconds:D2}]{trimmedLine}");
                    lineIndex += 4; // 假设每行歌词间隔 4 秒
                }
            }
        }

        await _fileSystem.WriteAllTextAsync(filePath, sb.ToString());
    }
}

