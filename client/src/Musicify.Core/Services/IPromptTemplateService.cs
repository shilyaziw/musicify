using Musicify.Core.Models;

namespace Musicify.Core.Services;

/// <summary>
/// 提示词模板服务接口
/// </summary>
public interface IPromptTemplateService
{
    /// <summary>
    /// 获取系统提示词
    /// </summary>
    string GetSystemPrompt(string mode);

    /// <summary>
    /// 获取用户提示词
    /// </summary>
    string GetUserPrompt(AIRequest request);

    /// <summary>
    /// 格式化提示词 (替换变量)
    /// </summary>
    string FormatPrompt(string template, Dictionary<string, string> variables);
}
