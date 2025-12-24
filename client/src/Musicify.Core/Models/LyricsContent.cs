using System.Text;
using System.Text.Json.Serialization;

namespace Musicify.Core.Models;

/// <summary>
/// 歌词内容
/// 对应文件: lyrics.json 或 lyrics.txt
/// </summary>
public sealed record LyricsContent
{
    /// <summary>
    /// 项目名称
    /// </summary>
    [JsonPropertyName("project_name")]
    public required string ProjectName { get; init; }

    /// <summary>
    /// 创作模式
    /// 可选值: coach/express/hybrid
    /// </summary>
    [JsonPropertyName("mode")]
    public required string Mode { get; init; }

    /// <summary>
    /// 歌词段落列表
    /// </summary>
    [JsonPropertyName("sections")]
    public required List<LyricsSection> Sections { get; init; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// 获取格式化的完整歌词文本
    /// </summary>
    public string ToFormattedText()
    {
        var sb = new StringBuilder();
        foreach (var section in Sections.OrderBy(s => s.Order))
        {
            sb.AppendLine($"[{section.Type}]");
            sb.AppendLine(section.Content);
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// 从文本格式解析歌词内容
    /// </summary>
    public static LyricsContent? FromText(string text, string projectName)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var sections = new List<LyricsSection>();
        var lines = text.Split('\n');
        string? currentSectionType = null;
        var currentContent = new StringBuilder();
        int order = 1;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // 检查是否是段落标记
            if (trimmedLine.StartsWith('[') && trimmedLine.EndsWith(']'))
            {
                // 保存前一个段落
                if (currentSectionType != null && currentContent.Length > 0)
                {
                    sections.Add(new LyricsSection
                    {
                        Type = currentSectionType,
                        Content = currentContent.ToString().TrimEnd(),
                        Order = order++
                    });
                    currentContent.Clear();
                }

                // 提取段落类型
                currentSectionType = trimmedLine.Substring(1, trimmedLine.Length - 2);
            }
            else if (!string.IsNullOrWhiteSpace(trimmedLine) || currentContent.Length > 0)
            {
                // 如果是第一个非空行且没有段落标记,默认为 Verse 1
                if (currentSectionType == null)
                {
                    currentSectionType = "Verse 1";
                }

                if (currentContent.Length > 0)
                {
                    currentContent.AppendLine();
                }

                currentContent.Append(trimmedLine);
            }
        }

        // 保存最后一个段落
        if (currentSectionType != null && currentContent.Length > 0)
        {
            sections.Add(new LyricsSection
            {
                Type = currentSectionType,
                Content = currentContent.ToString().TrimEnd(),
                Order = order
            });
        }

        if (sections.Count == 0)
        {
            return null;
        }

        return new LyricsContent
        {
            ProjectName = projectName,
            Mode = "coach", // 默认模式
            Sections = sections,
            CreatedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// 歌词段落
/// </summary>
public sealed record LyricsSection
{
    /// <summary>
    /// 段落类型 (Verse 1, Chorus, Bridge 等)
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// 段落内容 (多行文本)
    /// </summary>
    [JsonPropertyName("content")]
    public required string Content { get; init; }

    /// <summary>
    /// 段落顺序
    /// </summary>
    [JsonPropertyName("order")]
    public int Order { get; init; }
}

