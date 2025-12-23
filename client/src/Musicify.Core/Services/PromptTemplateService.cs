using Musicify.Core.Models;

namespace Musicify.Core.Services;

/// <summary>
/// 提示词模板服务实现
/// </summary>
public class PromptTemplateService : IPromptTemplateService
{
    private readonly Dictionary<string, string> _systemPrompts = new()
    {
        ["coach"] = """
            你是一位资深的歌词创作导师,擅长引导用户进行深度创作。
            
            你的任务是:
            1. 理解用户的创作意图和歌曲主题
            2. 通过提问引导用户挖掘更深层的情感和故事
            3. 在充分讨论后,协助创作符合要求的歌词
            4. 提供专业的修改建议和写作技巧
            
            创作原则:
            - 尊重用户的原创思路
            - 鼓励情感真实表达
            - 注重韵律和节奏感
            - 符合目标受众和平台要求
            """,
            
        ["express"] = """
            你是一位高效的歌词创作专家,擅长快速创作高质量歌词。
            
            你的任务是:
            1. 基于用户提供的主题和情感,快速创作歌词
            2. 确保歌词符合指定的歌曲类型、风格和时长
            3. 自动优化韵律和节奏
            4. 生成结构完整的歌词(Verse/Chorus/Bridge)
            
            创作原则:
            - 高效直接,减少互动
            - 保持专业性和艺术性
            - 符合商业音乐标准
            - 适配目标发布平台
            """,
            
        ["hybrid"] = """
            你是一位灵活的歌词创作助手,结合引导和执行能力。
            
            你的任务是:
            1. 先进行简短的创作讨论(1-2 轮)
            2. 快速理解用户意图和核心需求
            3. 基于讨论结果创作歌词初稿
            4. 根据反馈进行迭代优化
            
            创作原则:
            - 平衡效率和质量
            - 适度引导,快速产出
            - 保持创作灵活性
            - 支持快速迭代
            """
    };

    public string GetSystemPrompt(string mode)
    {
        return _systemPrompts.TryGetValue(mode, out var prompt) 
            ? prompt 
            : _systemPrompts["express"];
    }

    public string GetUserPrompt(AIRequest request)
    {
        var template = request.Mode switch
        {
            "coach" => GetCoachPrompt(),
            "express" => GetExpressPrompt(),
            "hybrid" => GetHybridPrompt(),
            _ => GetExpressPrompt()
        };

        return FormatPrompt(template, new Dictionary<string, string>
        {
            ["PROJECT_NAME"] = request.Project.Name,
            ["SONG_TYPE"] = request.Spec.SongType,
            ["DURATION"] = request.Spec.Duration?.ToString() ?? "未指定",
            ["STYLE"] = request.Spec.Style ?? "未指定",
            ["LANGUAGE"] = request.Spec.Language,
            ["TARGET_AUDIENCE"] = request.Spec.Audience != null 
                ? $"{request.Spec.Audience.Age}, {request.Spec.Audience.Gender}" 
                : "大众听众",
            ["TARGET_PLATFORM"] = string.Join(", ", request.Spec.TargetPlatform),
            ["USER_INPUT"] = request.UserInput ?? "",
            ["MELODY_INFO"] = FormatMelodyInfo(request.MelodyAnalysis)
        });
    }

    public string FormatPrompt(string template, Dictionary<string, string> variables)
    {
        var result = template;
        foreach (var (key, value) in variables)
        {
            result = result.Replace($"{{{key}}}", value);
        }
        return result;
    }

    private string GetExpressPrompt()
    {
        return """
            # 歌词创作任务
            
            ## 项目信息
            - 项目名称: {PROJECT_NAME}
            - 歌曲类型: {SONG_TYPE}
            - 目标时长: {DURATION}秒
            - 风格基调: {STYLE}
            - 语言: {LANGUAGE}
            - 目标受众: {TARGET_AUDIENCE}
            - 发布平台: {TARGET_PLATFORM}
            
            ## 用户创作意图
            {USER_INPUT}
            
            ## 旋律参考信息
            {MELODY_INFO}
            
            ## 要求
            请基于以上信息创作一首完整的歌词,包含:
            1. [Verse 1] - 主歌第一段
            2. [Chorus] - 副歌
            3. [Verse 2] - 主歌第二段
            4. [Chorus] - 副歌重复
            5. [Bridge] - 桥段 (可选)
            6. [Chorus] - 副歌结尾
            
            注意事项:
            - 确保韵律和节奏符合歌曲类型
            - 情感表达要符合风格基调
            - 歌词长度要匹配目标时长
            - 语言风格要适合目标受众
            """;
    }

    private string GetCoachPrompt()
    {
        return """
            # 歌词创作引导任务
            
            ## 项目信息
            - 项目名称: {PROJECT_NAME}
            - 歌曲类型: {SONG_TYPE}
            - 风格基调: {STYLE}
            - 语言: {LANGUAGE}
            
            ## 初步想法
            {USER_INPUT}
            
            ## 引导流程
            请先通过 2-3 个问题深入了解:
            1. 这首歌想表达的核心情感是什么?
            2. 有没有具体的故事或场景?
            3. 希望听众听完后有什么感受?
            
            在充分讨论后,我们再开始正式创作歌词。
            """;
    }

    private string GetHybridPrompt()
    {
        return """
            # 歌词快速创作任务
            
            ## 项目信息
            - 歌曲类型: {SONG_TYPE}
            - 风格基调: {STYLE}
            - 语言: {LANGUAGE}
            
            ## 创作主题
            {USER_INPUT}
            
            ## 旋律特征
            {MELODY_INFO}
            
            ## 工作流程
            1. 先确认一下创作方向 (1 个问题)
            2. 快速生成歌词初稿
            3. 根据您的反馈优化
            
            请问,您希望这首歌的情感重点是什么? (例如: 怀旧/励志/浪漫/伤感)
            """;
    }

    private string FormatMelodyInfo(MidiAnalysisResult? analysis)
    {
        if (analysis == null)
            return "无旋律参考";

        return $"""
            - 音域: {analysis.NoteRange.Min} - {analysis.NoteRange.Max}
            - 调式: {analysis.ModeInfo.DetectedMode}
            - 节奏特点: {string.Join(", ", analysis.RhythmPatterns.Take(3).Select(kv => kv.Key))}
            """;
    }
}
