namespace Musicify.Core.Models.Constants;

/// <summary>
/// 语言常量
/// 定义歌词支持的语言类型
/// </summary>
public static class Languages
{
    /// <summary>中文(普通话)</summary>
    public const string Chinese = "中文";
    
    /// <summary>英文</summary>
    public const string English = "英文";
    
    /// <summary>粤语</summary>
    public const string Cantonese = "粤语";
    
    /// <summary>日语</summary>
    public const string Japanese = "日语";
    
    /// <summary>韩语</summary>
    public const string Korean = "韩语";
    
    /// <summary>中英混合</summary>
    public const string ChineseEnglish = "中英混合";
    
    /// <summary>其他语言</summary>
    public const string Other = "其他";
    
    /// <summary>
    /// 所有有效语言的集合
    /// </summary>
    private static readonly HashSet<string> ValidLanguages = new()
    {
        Chinese, English, Cantonese, Japanese, Korean, ChineseEnglish, Other
    };
    
    /// <summary>
    /// 验证语言是否有效
    /// </summary>
    public static bool IsValid(string language) => ValidLanguages.Contains(language);
    
    /// <summary>
    /// 获取所有语言的只读列表
    /// </summary>
    public static IReadOnlyList<string> All => new List<string>
    {
        Chinese, English, Cantonese, Japanese, Korean, ChineseEnglish, Other
    };
}
