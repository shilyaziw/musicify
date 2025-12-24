using System.Text;
using System.Text.RegularExpressions;
using Musicify.Core.Models;

namespace Musicify.Core.Services;

/// <summary>
/// 押韵检查服务实现
/// </summary>
public partial class RhymeCheckService : IRhymeCheckService
{
    // 常见韵母表（简化版，实际应该使用完整的拼音库）
    private static readonly Dictionary<string, string> CommonRhymes = new()
    {
        // a 韵
        { "a", "a" }, { "ia", "a" }, { "ua", "a" },
        // ai 韵
        { "ai", "ai" }, { "uai", "ai" },
        // an 韵
        { "an", "an" }, { "ian", "an" }, { "uan", "an" }, { "üan", "an" },
        // ang 韵
        { "ang", "ang" }, { "iang", "ang" }, { "uang", "ang" },
        // ao 韵
        { "ao", "ao" }, { "iao", "ao" },
        // e 韵
        { "e", "e" }, { "ie", "e" }, { "üe", "e" },
        // ei 韵
        { "ei", "ei" }, { "ui", "ei" },
        // en 韵
        { "en", "en" }, { "in", "en" }, { "un", "en" }, { "ün", "en" },
        // eng 韵
        { "eng", "eng" }, { "ing", "eng" }, { "ong", "eng" }, { "iong", "eng" },
        // er 韵
        { "er", "er" },
        // i 韵
        { "i", "i" }, { "yi", "i" },
        // o 韵
        { "o", "o" }, { "uo", "o" },
        // ou 韵
        { "ou", "ou" }, { "iu", "ou" },
        // u 韵
        { "u", "u" }, { "yu", "u" },
    };

    // 拼音到韵母的映射（简化版）
    private static readonly Dictionary<string, string> PinyinToRhyme = new()
    {
        // 示例映射，实际应该使用完整的拼音库
        { "a", "a" }, { "ba", "a" }, { "ca", "a" }, { "da", "a" }, { "fa", "a" },
        { "ga", "a" }, { "ha", "a" }, { "ka", "a" }, { "la", "a" }, { "ma", "a" },
        { "na", "a" }, { "pa", "a" }, { "sa", "a" }, { "ta", "a" }, { "wa", "a" },
        { "ya", "a" }, { "zha", "a" }, { "cha", "a" }, { "sha", "a" },

        { "ai", "ai" }, { "bai", "ai" }, { "cai", "ai" }, { "dai", "ai" },
        { "gai", "ai" }, { "hai", "ai" }, { "kai", "ai" }, { "lai", "ai" },
        { "mai", "ai" }, { "nai", "ai" }, { "pai", "ai" }, { "sai", "ai" },
        { "tai", "ai" }, { "wai", "ai" }, { "zai", "ai" }, { "zhai", "ai" },
        { "chai", "ai" }, { "shai", "ai" },

        { "an", "an" }, { "ban", "an" }, { "can", "an" }, { "dan", "an" },
        { "fan", "an" }, { "gan", "an" }, { "han", "an" }, { "kan", "an" },
        { "lan", "an" }, { "man", "an" }, { "nan", "an" }, { "pan", "an" },
        { "ran", "an" }, { "san", "an" }, { "tan", "an" }, { "wan", "an" },
        { "yan", "an" }, { "zan", "an" }, { "zhan", "an" }, { "chan", "an" },
        { "shan", "an" },

        { "ang", "ang" }, { "bang", "ang" }, { "cang", "ang" }, { "dang", "ang" },
        { "fang", "ang" }, { "gang", "ang" }, { "hang", "ang" }, { "kang", "ang" },
        { "lang", "ang" }, { "mang", "ang" }, { "nang", "ang" }, { "pang", "ang" },
        { "rang", "ang" }, { "sang", "ang" }, { "tang", "ang" }, { "wang", "ang" },
        { "yang", "ang" }, { "zang", "ang" }, { "zhang", "ang" }, { "chang", "ang" },
        { "shang", "ang" },

        { "ao", "ao" }, { "bao", "ao" }, { "cao", "ao" }, { "dao", "ao" },
        { "gao", "ao" }, { "hao", "ao" }, { "kao", "ao" }, { "lao", "ao" },
        { "mao", "ao" }, { "nao", "ao" }, { "pao", "ao" }, { "sao", "ao" },
        { "tao", "ao" }, { "yao", "ao" }, { "zao", "ao" }, { "zhao", "ao" },
        { "chao", "ao" }, { "shao", "ao" },

        { "e", "e" }, { "be", "e" }, { "ce", "e" }, { "de", "e" },
        { "ge", "e" }, { "he", "e" }, { "ke", "e" }, { "le", "e" },
        { "me", "e" }, { "ne", "e" }, { "re", "e" }, { "se", "e" },
        { "te", "e" }, { "ze", "e" }, { "zhe", "e" }, { "che", "e" },
        { "she", "e" },

        { "eng", "eng" }, { "beng", "eng" }, { "ceng", "eng" }, { "deng", "eng" },
        { "feng", "eng" }, { "geng", "eng" }, { "heng", "eng" }, { "keng", "eng" },
        { "leng", "eng" }, { "meng", "eng" }, { "neng", "eng" }, { "peng", "eng" },
        { "reng", "eng" }, { "seng", "eng" }, { "teng", "eng" }, { "weng", "eng" },
        { "zeng", "eng" }, { "zheng", "eng" }, { "cheng", "eng" }, { "sheng", "eng" },

        { "ing", "eng" }, { "bing", "eng" }, { "ding", "eng" }, { "jing", "eng" },
        { "ling", "eng" }, { "ming", "eng" }, { "ning", "eng" }, { "ping", "eng" },
        { "qing", "eng" }, { "ting", "eng" }, { "xing", "eng" }, { "ying", "eng" },
        { "zing", "eng" },

        { "i", "i" }, { "bi", "i" }, { "ci", "i" }, { "di", "i" },
        { "ji", "i" }, { "li", "i" }, { "mi", "i" }, { "ni", "i" },
        { "pi", "i" }, { "qi", "i" }, { "ri", "i" }, { "si", "i" },
        { "ti", "i" }, { "xi", "i" }, { "yi", "i" }, { "zi", "i" },
        { "zhi", "i" }, { "chi", "i" }, { "shi", "i" },

        { "ou", "ou" }, { "dou", "ou" }, { "gou", "ou" }, { "hou", "ou" },
        { "kou", "ou" }, { "lou", "ou" }, { "mou", "ou" }, { "nou", "ou" },
        { "pou", "ou" }, { "sou", "ou" }, { "tou", "ou" }, { "you", "ou" },
        { "zou", "ou" }, { "zhou", "ou" }, { "chou", "ou" }, { "shou", "ou" },

        { "u", "u" }, { "bu", "u" }, { "cu", "u" }, { "du", "u" },
        { "fu", "u" }, { "gu", "u" }, { "hu", "u" }, { "ku", "u" },
        { "lu", "u" }, { "mu", "u" }, { "nu", "u" }, { "pu", "u" },
        { "ru", "u" }, { "su", "u" }, { "tu", "u" }, { "wu", "u" },
        { "yu", "u" }, { "zu", "u" }, { "zhu", "u" }, { "chu", "u" },
        { "shu", "u" },
    };

    public async Task<RhymeAnalysisResult> AnalyzeAsync(string lyricsText, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            if (string.IsNullOrWhiteSpace(lyricsText))
            {
                return new RhymeAnalysisResult
                {
                    Pattern = "N/A",
                    RhymeGroups = new List<RhymeGroup>(),
                    QualityScore = 0,
                    Suggestions = new List<string> { "歌词内容为空" }
                };
            }

            // 解析歌词行
            var lines = ParseLines(lyricsText);
            if (lines.Count == 0)
            {
                return new RhymeAnalysisResult
                {
                    Pattern = "N/A",
                    RhymeGroups = new List<RhymeGroup>(),
                    QualityScore = 0,
                    Suggestions = new List<string> { "未找到有效歌词行" }
                };
            }

            // 提取每行的最后一个字
            var lineEndWords = new List<RhymeWord>();
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var lastWord = ExtractLastWord(line);
                if (lastWord != null)
                {
                    var pinyin = GetPinyin(lastWord);
                    var rhyme = GetRhyme(lastWord);
                    if (!string.IsNullOrEmpty(rhyme))
                    {
                        lineEndWords.Add(new RhymeWord
                        {
                            LineIndex = i,
                            StartIndex = line.LastIndexOf(lastWord),
                            Length = lastWord.Length,
                            Word = lastWord,
                            Pinyin = pinyin ?? string.Empty,
                            Rhyme = rhyme
                        });
                    }
                }
            }

            // 分析押韵模式
            var pattern = DetectRhymePattern(lineEndWords);

            // 分组押韵词
            var rhymeGroups = GroupRhymeWords(lineEndWords);

            // 计算质量评分
            var qualityScore = CalculateQualityScore(pattern, rhymeGroups, lines.Count);

            // 生成建议
            var suggestions = GenerateSuggestions(pattern, rhymeGroups, qualityScore);

            return new RhymeAnalysisResult
            {
                Pattern = pattern,
                RhymeGroups = rhymeGroups,
                QualityScore = qualityScore,
                Suggestions = suggestions
            };
        }, cancellationToken);
    }

    public bool CheckRhyme(string word1, string word2)
    {
        var rhyme1 = GetRhyme(word1);
        var rhyme2 = GetRhyme(word2);

        if (string.IsNullOrEmpty(rhyme1) || string.IsNullOrEmpty(rhyme2))
        {
            return false;
        }

        return rhyme1.Equals(rhyme2, StringComparison.OrdinalIgnoreCase);
    }

    public string? GetRhyme(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return null;
        }

        // 获取最后一个字符
        var lastChar = word[^1].ToString();

        // 获取拼音
        var pinyin = GetPinyin(lastChar);
        if (string.IsNullOrEmpty(pinyin))
        {
            return null;
        }

        // 从拼音中提取韵母
        return ExtractRhymeFromPinyin(pinyin);
    }

    public string? GetPinyin(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return null;
        }

        // 简化实现：使用字符的 Unicode 范围判断是否为中文
        // 实际应该使用拼音库（如 Pinyin4Net, NPinyin 等）
        var lastChar = word[^1];

        if (lastChar >= 0x4E00 && lastChar <= 0x9FFF)
        {
            // 中文字符，这里使用简化映射
            // 实际应该使用完整的拼音库
            return GetPinyinForChar(lastChar);
        }

        return null;
    }

    #region 私有方法

    [System.Text.RegularExpressions.GeneratedRegex(@"\[.*?\]", RegexOptions.IgnoreCase)]
    private static partial System.Text.RegularExpressions.Regex SectionPattern();

    private static List<string> ParseLines(string lyricsText)
    {
        var lines = new List<string>();
        var sectionPattern = SectionPattern();

        foreach (var line in lyricsText.Split('\n'))
        {
            var trimmedLine = line.Trim();
            // 跳过空行和段落标记
            if (string.IsNullOrWhiteSpace(trimmedLine) || sectionPattern.IsMatch(trimmedLine))
            {
                continue;
            }

            lines.Add(trimmedLine);
        }

        return lines;
    }

    private static string? ExtractLastWord(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        // 提取最后一个字符或词（简化：只取最后一个字符）
        var trimmed = line.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        // 尝试提取最后一个词（2-3个字符）
        if (trimmed.Length >= 2)
        {
            var lastTwo = trimmed.Substring(trimmed.Length - 2);
            if (IsValidWord(lastTwo))
            {
                return lastTwo;
            }
        }

        // 否则返回最后一个字符
        return trimmed[^1].ToString();
    }

    private static bool IsValidWord(string word)
    {
        // 简单验证：所有字符都是中文
        return word.All(c => c >= 0x4E00 && c <= 0x9FFF);
    }

    private static string DetectRhymePattern(List<RhymeWord> lineEndWords)
    {
        if (lineEndWords.Count < 2)
        {
            return "N/A";
        }

        // 简化的模式检测
        var rhymes = lineEndWords.Select(w => w.Rhyme).ToList();

        // AABB 模式
        if (rhymes.Count >= 4 &&
            rhymes[0] == rhymes[1] &&
            rhymes[2] == rhymes[3] &&
            rhymes[0] != rhymes[2])
        {
            return "AABB";
        }

        // ABAB 模式
        if (rhymes.Count >= 4 &&
            rhymes[0] == rhymes[2] &&
            rhymes[1] == rhymes[3] &&
            rhymes[0] != rhymes[1])
        {
            return "ABAB";
        }

        // ABCB 模式
        if (rhymes.Count >= 4 &&
            rhymes[1] == rhymes[3] &&
            rhymes[0] != rhymes[1] &&
            rhymes[2] != rhymes[1])
        {
            return "ABCB";
        }

        // AAAA 模式（全押韵）
        if (rhymes.All(r => r == rhymes[0]))
        {
            return "AAAA";
        }

        // 混合模式
        var uniqueRhymes = rhymes.Distinct().Count();
        if (uniqueRhymes <= rhymes.Count / 2)
        {
            return "Mixed";
        }

        return "Irregular";
    }

    private static List<RhymeGroup> GroupRhymeWords(List<RhymeWord> words)
    {
        var groups = words
            .GroupBy(w => w.Rhyme)
            .Select(g => new RhymeGroup
            {
                Rhyme = g.Key,
                Words = g.ToList()
            })
            .OrderByDescending(g => g.Words.Count)
            .ToList();

        return groups;
    }

    private static int CalculateQualityScore(string pattern, List<RhymeGroup> groups, int totalLines)
    {
        if (totalLines == 0)
        {
            return 0;
        }

        var score = 100;

        // 根据模式扣分
        switch (pattern)
        {
            case "AABB":
            case "ABAB":
            case "ABCB":
                score -= 0; // 标准模式不扣分
                break;
            case "AAAA":
                score -= 5; // 全押韵可能过于单调
                break;
            case "Mixed":
                score -= 10;
                break;
            case "Irregular":
                score -= 30;
                break;
            default:
                score -= 20;
                break;
        }

        // 根据押韵覆盖率扣分
        var rhymingLines = groups.Sum(g => g.Words.Count);
        var coverage = (double)rhymingLines / totalLines;
        if (coverage < 0.5)
        {
            score -= (int)((0.5 - coverage) * 40);
        }

        // 根据押韵组数量扣分（太多不同的韵脚不好）
        if (groups.Count > totalLines / 2)
        {
            score -= 10;
        }

        return Math.Max(0, Math.Min(100, score));
    }

    private static List<string> GenerateSuggestions(string pattern, List<RhymeGroup> groups, int qualityScore)
    {
        var suggestions = new List<string>();

        if (qualityScore < 70)
        {
            suggestions.Add("押韵质量较低，建议调整部分词句以改善押韵效果");
        }

        if (pattern == "Irregular")
        {
            suggestions.Add("押韵模式不规律，建议采用 AABB、ABAB 或 ABCB 等标准模式");
        }

        if (groups.Count > 4)
        {
            suggestions.Add($"使用了 {groups.Count} 种不同的韵脚，建议减少到 2-3 种以保持一致性");
        }

        var largestGroup = groups.FirstOrDefault();
        if (largestGroup != null && largestGroup.Words.Count < 2)
        {
            suggestions.Add("大部分行没有形成押韵，建议增加押韵词");
        }

        return suggestions;
    }

    private static string ExtractRhymeFromPinyin(string pinyin)
    {
        if (string.IsNullOrEmpty(pinyin))
        {
            return string.Empty;
        }

        pinyin = pinyin.ToLowerInvariant();

        // 查找匹配的韵母
        foreach (var kvp in PinyinToRhyme.OrderByDescending(kvp => kvp.Key.Length))
        {
            if (pinyin.EndsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Value;
            }
        }

        // 如果没有找到，尝试从常见韵母表中查找
        foreach (var rhyme in CommonRhymes.Keys.OrderByDescending(r => r.Length))
        {
            if (pinyin.EndsWith(rhyme, StringComparison.OrdinalIgnoreCase))
            {
                return CommonRhymes[rhyme];
            }
        }

        return string.Empty;
    }

    private static string? GetPinyinForChar(char ch)
    {
        // 这是一个简化的实现
        // 实际应该使用完整的拼音库（如 NPinyin 或调用在线API）
        // 这里使用基于 Unicode 范围的简化映射

        // 常见汉字的拼音映射（简化版，实际应该使用完整库）
        var charPinyinMap = new Dictionary<char, string>
        {
            // 示例字符（实际应该包含完整映射）
            { '的', "de" }, { '了', "le" }, { '在', "zai" }, { '是', "shi" },
            { '我', "wo" }, { '有', "you" }, { '和', "he" }, { '就', "jiu" },
            { '不', "bu" }, { '人', "ren" }, { '这', "zhe" }, { '中', "zhong" },
            { '大', "da" }, { '为', "wei" }, { '上', "shang" }, { '个', "ge" },
            { '国', "guo" }, { '你', "ni" }, { '说', "shuo" }, { '到', "dao" },
            { '地', "di" }, { '出', "chu" }, { '也', "ye" }, { '以', "yi" },
            { '要', "yao" }, { '他', "ta" }, { '时', "shi" }, { '来', "lai" },
            { '用', "yong" }, { '们', "men" }, { '生', "sheng" }, { '作', "zuo" },
            { '年', "nian" }, { '对', "dui" }, { '下', "xia" }, { '都', "dou" },
            { '而', "er" }, { '能', "neng" }, { '好', "hao" }, { '看', "kan" },
            { '过', "guo" }, { '还', "hai" }, { '想', "xiang" }, { '心', "xin" },
            { '情', "qing" }, { '爱', "ai" }, { '梦', "meng" }, { '天', "tian" },
            { '风', "feng" }, { '雨', "yu" }, { '花', "hua" }, { '月', "yue" },
            { '星', "xing" }, { '海', "hai" }, { '山', "shan" }, { '路', "lu" },
            { '家', "jia" }, { '夜', "ye" }, { '光', "guang" }, { '声', "sheng" },
            { '歌', "ge" }, { '唱', "chang" }, { '听', "ting" }, { '走', "zou" },
            { '回', "hui" }, { '去', "qu" }, { '飞', "fei" }, { '跳', "tiao" },
            { '笑', "xiao" }, { '哭', "ku" }, { '泪', "lei" }, { '痛', "tong" },
            { '伤', "shang" }, { '美', "mei" }, { '丽', "li" }, { '真', "zhen" },
            { '假', "jia" }, { '新', "xin" }, { '旧', "jiu" }, { '高', "gao" },
            { '低', "di" }, { '长', "chang" }, { '短', "duan" }, { '快', "kuai" },
            { '慢', "man" }, { '远', "yuan" }, { '近', "jin" }, { '多', "duo" },
            { '少', "shao" },
        };

        if (charPinyinMap.TryGetValue(ch, out var pinyin))
        {
            return pinyin;
        }

        // 如果没有找到，返回 null，表示无法确定拼音
        // 实际应该使用专业的拼音库
        return null;
    }

    #endregion
}

