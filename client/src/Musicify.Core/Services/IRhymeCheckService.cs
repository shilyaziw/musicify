using Musicify.Core.Models;

namespace Musicify.Core.Services;

/// <summary>
/// 押韵检查服务接口
/// </summary>
public interface IRhymeCheckService
{
    /// <summary>
    /// 分析歌词的押韵模式
    /// </summary>
    /// <param name="lyricsText">歌词文本</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>押韵分析结果</returns>
    Task<RhymeAnalysisResult> AnalyzeAsync(string lyricsText, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查两个词是否押韵
    /// </summary>
    /// <param name="word1">词1</param>
    /// <param name="word2">词2</param>
    /// <returns>是否押韵</returns>
    bool CheckRhyme(string word1, string word2);

    /// <summary>
    /// 获取词的韵母
    /// </summary>
    /// <param name="word">词</param>
    /// <returns>韵母（如 "ang", "ing"）</returns>
    string? GetRhyme(string word);

    /// <summary>
    /// 获取词的拼音
    /// </summary>
    /// <param name="word">词</param>
    /// <returns>拼音</returns>
    string? GetPinyin(string word);
}

