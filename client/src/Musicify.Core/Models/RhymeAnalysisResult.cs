namespace Musicify.Core.Models;

/// <summary>
/// 押韵分析结果
/// </summary>
public record RhymeAnalysisResult
{
    /// <summary>
    /// 押韵模式 (AABB, ABAB, ABCB 等)
    /// </summary>
    public required string Pattern { get; init; }

    /// <summary>
    /// 押韵词列表（按行分组）
    /// </summary>
    public required List<RhymeGroup> RhymeGroups { get; init; }

    /// <summary>
    /// 押韵质量评分 (0-100)
    /// </summary>
    public int QualityScore { get; init; }

    /// <summary>
    /// 改进建议
    /// </summary>
    public List<string> Suggestions { get; init; } = new();

    /// <summary>
    /// 是否有押韵问题
    /// </summary>
    public bool HasIssues => QualityScore < 70 || Suggestions.Count > 0;
}

/// <summary>
/// 押韵组（同一韵脚的字词）
/// </summary>
public record RhymeGroup
{
    /// <summary>
    /// 韵母（如 "ang", "ing"）
    /// </summary>
    public required string Rhyme { get; init; }

    /// <summary>
    /// 押韵词位置列表
    /// </summary>
    public required List<RhymeWord> Words { get; init; }
}

/// <summary>
/// 押韵词位置信息
/// </summary>
public record RhymeWord
{
    /// <summary>
    /// 行号（从0开始）
    /// </summary>
    public required int LineIndex { get; init; }

    /// <summary>
    /// 词在行中的起始位置
    /// </summary>
    public required int StartIndex { get; init; }

    /// <summary>
    /// 词的长度
    /// </summary>
    public required int Length { get; init; }

    /// <summary>
    /// 词的内容
    /// </summary>
    public required string Word { get; init; }

    /// <summary>
    /// 拼音
    /// </summary>
    public required string Pinyin { get; init; }

    /// <summary>
    /// 韵母
    /// </summary>
    public required string Rhyme { get; init; }
}

