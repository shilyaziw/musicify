namespace Musicify.Core.Services;

/// <summary>
/// 歌词语法高亮规则接口
/// </summary>
public interface ILyricsSyntaxHighlighting
{
    /// <summary>
    /// 应用语法高亮规则到编辑器
    /// </summary>
    /// <param name="editor">文本编辑器实例</param>
    void ApplyHighlighting(object editor);
}

