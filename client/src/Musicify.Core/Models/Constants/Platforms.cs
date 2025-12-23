namespace Musicify.Core.Models.Constants;

/// <summary>
/// 目标平台常量
/// 定义歌曲可以发布的各个平台
/// </summary>
public static class Platforms
{
    // 音乐平台
    
    /// <summary>QQ音乐 - 腾讯音乐,主流平台</summary>
    public const string QQMusic = "QQ音乐";
    
    /// <summary>网易云音乐 - 年轻用户,评论活跃</summary>
    public const string NetEaseMusic = "网易云音乐";
    
    /// <summary>酷狗音乐 - 下沉市场</summary>
    public const string KuGou = "酷狗音乐";
    
    /// <summary>Apple Music - 高品质,国际化</summary>
    public const string AppleMusic = "Apple Music";
    
    // 短视频平台
    
    /// <summary>抖音 - 最大流量,病毒传播</summary>
    public const string Douyin = "抖音";
    
    /// <summary>快手 - 下沉市场,真实感</summary>
    public const string Kuaishou = "快手";
    
    /// <summary>B站 - 年轻用户,高质量</summary>
    public const string Bilibili = "B站";
    
    // 国际平台
    
    /// <summary>Spotify - 国际主流音乐平台</summary>
    public const string Spotify = "Spotify";
    
    /// <summary>YouTube Music - 全球覆盖</summary>
    public const string YouTubeMusic = "YouTube Music";
    
    /// <summary>
    /// 获取所有平台的只读列表
    /// </summary>
    public static IReadOnlyList<string> All => new List<string>
    {
        // 音乐平台
        QQMusic, NetEaseMusic, KuGou, AppleMusic,
        // 短视频平台
        Douyin, Kuaishou, Bilibili,
        // 国际平台
        Spotify, YouTubeMusic
    };
}
