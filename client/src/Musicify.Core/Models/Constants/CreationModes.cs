namespace Musicify.Core.Models.Constants;

/// <summary>
/// 创作模式常量
/// 定义歌词创作的三种模式
/// </summary>
public static class CreationModes
{
    /// <summary>
    /// 教练模式 - AI 引导用户思考,逐段创作,100% 原创
    /// </summary>
    public const string Coach = "coach";
    
    /// <summary>
    /// 快速模式 - AI 直接生成完整歌词,快速迭代
    /// </summary>
    public const string Express = "express";
    
    /// <summary>
    /// 混合模式 - AI 生成框架,用户填充细节,平衡效率与原创
    /// </summary>
    public const string Hybrid = "hybrid";
    
    /// <summary>
    /// 验证创作模式是否有效
    /// </summary>
    public static bool IsValid(string mode) => mode is Coach or Express or Hybrid;
    
    /// <summary>
    /// 获取所有创作模式的只读列表
    /// </summary>
    public static IReadOnlyList<string> All => new List<string>
    {
        Coach, Express, Hybrid
    };
}
