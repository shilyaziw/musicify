namespace Musicify.Core.Models.Constants;

/// <summary>
/// 歌曲类型常量
/// 定义所有支持的歌曲类型
/// </summary>
public static class SongTypes
{
    /// <summary>流行音乐</summary>
    public const string Pop = "流行";

    /// <summary>摇滚/朋克</summary>
    public const string Rock = "摇滚";

    /// <summary>说唱/Hip-Hop</summary>
    public const string Rap = "说唱";

    /// <summary>民谣/独立音乐</summary>
    public const string Folk = "民谣";

    /// <summary>电子音乐/EDM</summary>
    public const string Electronic = "电子";

    /// <summary>中国风/古风</summary>
    public const string GuoFeng = "古风";

    /// <summary>节奏布鲁斯</summary>
    public const string RnB = "R&B";

    /// <summary>爵士乐</summary>
    public const string Jazz = "爵士";

    /// <summary>乡村音乐</summary>
    public const string Country = "乡村";

    /// <summary>重金属/金属核</summary>
    public const string Metal = "金属";

    /// <summary>
    /// 所有有效类型的集合 (用于快速查找)
    /// </summary>
    private static readonly HashSet<string> ValidTypes = new()
    {
        Pop, Rock, Rap, Folk, Electronic, GuoFeng, RnB, Jazz, Country, Metal
    };

    /// <summary>
    /// 验证歌曲类型是否有效
    /// </summary>
    /// <param name="type">歌曲类型</param>
    /// <returns>是否为有效类型</returns>
    public static bool IsValid(string type) => ValidTypes.Contains(type);

    /// <summary>
    /// 获取所有歌曲类型的只读列表
    /// </summary>
    public static IReadOnlyList<string> All => new List<string>
    {
        Pop, Rock, Rap, Folk, Electronic, GuoFeng, RnB, Jazz, Country, Metal
    };
}
