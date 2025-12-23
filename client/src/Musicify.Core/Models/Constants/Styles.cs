namespace Musicify.Core.Models.Constants;

/// <summary>
/// 风格基调常量
/// 定义歌曲的情绪和氛围风格
/// </summary>
public static class Styles
{
    /// <summary>抒情 - 情感细腻,婉转动人</summary>
    public const string Lyrical = "抒情";
    
    /// <summary>激昂 - 热血燃烧,力量爆发</summary>
    public const string Passionate = "激昂";
    
    /// <summary>轻快 - 明快活泼,轻松愉悦</summary>
    public const string Cheerful = "轻快";
    
    /// <summary>忧郁 - 感伤深沉,情绪低落</summary>
    public const string Melancholy = "忧郁";
    
    /// <summary>治愈 - 温暖疗愈,安慰心灵</summary>
    public const string Healing = "治愈";
    
    /// <summary>燃爆 - 热血沸腾,斗志昂扬</summary>
    public const string Explosive = "燃爆";
    
    /// <summary>平静 - 淡然舒缓,宁静致远</summary>
    public const string Calm = "平静";
    
    /// <summary>梦幻 - 虚幻缥缈,想象飞扬</summary>
    public const string Dreamy = "梦幻";
    
    /// <summary>
    /// 所有有效风格的集合
    /// </summary>
    private static readonly HashSet<string> ValidStyles = new()
    {
        Lyrical, Passionate, Cheerful, Melancholy, Healing, Explosive, Calm, Dreamy
    };
    
    /// <summary>
    /// 验证风格是否有效
    /// </summary>
    public static bool IsValid(string style) => ValidStyles.Contains(style);
    
    /// <summary>
    /// 获取所有风格的只读列表
    /// </summary>
    public static IReadOnlyList<string> All => new List<string>
    {
        Lyrical, Passionate, Cheerful, Melancholy, Healing, Explosive, Calm, Dreamy
    };
}
