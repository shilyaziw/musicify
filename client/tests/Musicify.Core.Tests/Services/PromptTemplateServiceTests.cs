using FluentAssertions;
using Musicify.Core.Models;
using Musicify.Core.Services;
using Xunit;

namespace Musicify.Core.Tests.Services;

/// <summary>
/// PromptTemplateService 单元测试
/// </summary>
public class PromptTemplateServiceTests
{
    private readonly PromptTemplateService _service;

    public PromptTemplateServiceTests()
    {
        _service = new PromptTemplateService();
    }

    private AIRequest CreateTestRequest(string mode = "express")
    {
        return new AIRequest
        {
            Mode = mode,
            Project = new ProjectConfig
            {
                Name = "测试歌曲",
                Type = "musicify-project",
                Version = "1.0.0",
                Created = DateTime.UtcNow,
                ProjectPath = "/test/path",
                UpdatedAt = DateTime.UtcNow,
                Status = "draft",
                Spec = null
            },
            Spec = new SongSpec
            {
                ProjectName = "测试歌曲",
                SongType = "流行",
                Duration = "240",
                Style = "轻快",
                Language = "中文",
                Audience = new AudienceInfo { Age = "全年龄", Gender = "中性" },
                TargetPlatform = new List<string> { "Suno", "Tunee" },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            UserInput = "一首关于友情的歌"
        };
    }

    #region GetSystemPrompt Tests

    [Theory]
    [InlineData("coach")]
    [InlineData("express")]
    [InlineData("hybrid")]
    public void GetSystemPrompt_WithValidMode_ShouldReturnPrompt(string mode)
    {
        // Act
        var prompt = _service.GetSystemPrompt(mode);

        // Assert
        prompt.Should().NotBeEmpty();
        prompt.Should().Contain("歌词");
    }

    [Fact]
    public void GetSystemPrompt_Coach_ShouldContainGuidanceKeywords()
    {
        var prompt = _service.GetSystemPrompt("coach");

        prompt.Should().Contain("导师");
        prompt.Should().Contain("引导");
        prompt.Should().Contain("提问");
    }

    [Fact]
    public void GetSystemPrompt_Express_ShouldContainEfficiencyKeywords()
    {
        var prompt = _service.GetSystemPrompt("express");

        prompt.Should().Contain("快速");
        prompt.Should().Contain("高效");
    }

    [Fact]
    public void GetSystemPrompt_Hybrid_ShouldContainBalanceKeywords()
    {
        var prompt = _service.GetSystemPrompt("hybrid");

        prompt.Should().Contain("灵活");
        prompt.Should().Contain("讨论");
    }

    [Fact]
    public void GetSystemPrompt_WithInvalidMode_ShouldReturnDefaultPrompt()
    {
        var prompt = _service.GetSystemPrompt("invalid-mode");

        prompt.Should().NotBeEmpty();
        prompt.Should().Be(_service.GetSystemPrompt("express"));
    }

    #endregion

    #region GetUserPrompt Tests

    [Fact]
    public void GetUserPrompt_WithExpressMode_ShouldContainProjectInfo()
    {
        // Arrange
        var request = CreateTestRequest("express");

        // Act
        var prompt = _service.GetUserPrompt(request);

        // Assert
        prompt.Should().Contain("测试歌曲");
        prompt.Should().Contain("流行");
        prompt.Should().Contain("轻快");
        prompt.Should().Contain("zh-CN");
        prompt.Should().Contain("年轻人");
    }

    [Fact]
    public void GetUserPrompt_WithCoachMode_ShouldContainGuidanceQuestions()
    {
        var request = CreateTestRequest("coach");

        var prompt = _service.GetUserPrompt(request);

        prompt.Should().Contain("引导");
        prompt.Should().Contain("?");  // Should contain questions
    }

    [Fact]
    public void GetUserPrompt_WithHybridMode_ShouldContainWorkflow()
    {
        var request = CreateTestRequest("hybrid");

        var prompt = _service.GetUserPrompt(request);

        prompt.Should().Contain("工作流程");
    }

    [Fact]
    public void GetUserPrompt_WithUserInput_ShouldIncludeInput()
    {
        var request = CreateTestRequest() with
        {
            UserInput = "我想写一首关于梦想的歌"
        };

        var prompt = _service.GetUserPrompt(request);

        prompt.Should().Contain("梦想");
    }

    [Fact]
    public void GetUserPrompt_WithMelodyAnalysis_ShouldIncludeMelodyInfo()
    {
        var request = CreateTestRequest() with
        {
            MelodyAnalysis = new MidiAnalysisResult(
                FilePath: "/test/melody.mid",
                TotalNotes: 100,
                NoteRange: (60, 80),
                RhythmPatterns: new Dictionary<string, float>
                {
                    ["quarter"] = 0.5f,
                    ["eighth"] = 0.3f
                },
                IntervalDistribution: new Dictionary<string, float>
                {
                    ["minor_third"] = 0.2f
                },
                ModeInfo: new ModeAnalysis(
                    DetectedMode: "C Major",
                    Confidence: 0.9f,
                    ScaleNotes: new List<string> { "C", "D", "E", "F", "G", "A", "B" }
                )
            )
        };

        var prompt = _service.GetUserPrompt(request);

        prompt.Should().Contain("音域");
        prompt.Should().Contain("60");
        prompt.Should().Contain("80");
        prompt.Should().Contain("C Major");
    }

    [Fact]
    public void GetUserPrompt_WithoutMelodyAnalysis_ShouldShowNoMelody()
    {
        var request = CreateTestRequest() with
        {
            MelodyAnalysis = null
        };

        var prompt = _service.GetUserPrompt(request);

        prompt.Should().Contain("无旋律参考");
    }

    #endregion

    #region FormatPrompt Tests

    [Fact]
    public void FormatPrompt_WithSingleVariable_ShouldReplace()
    {
        // Arrange
        var template = "Hello, {NAME}!";
        var variables = new Dictionary<string, string>
        {
            ["NAME"] = "World"
        };

        // Act
        var result = _service.FormatPrompt(template, variables);

        // Assert
        result.Should().Be("Hello, World!");
    }

    [Fact]
    public void FormatPrompt_WithMultipleVariables_ShouldReplaceAll()
    {
        var template = "项目: {PROJECT_NAME}, 类型: {SONG_TYPE}, 风格: {STYLE}";
        var variables = new Dictionary<string, string>
        {
            ["PROJECT_NAME"] = "测试歌曲",
            ["SONG_TYPE"] = "流行",
            ["STYLE"] = "轻快"
        };

        var result = _service.FormatPrompt(template, variables);

        result.Should().Be("项目: 测试歌曲, 类型: 流行, 风格: 轻快");
    }

    [Fact]
    public void FormatPrompt_WithMissingVariable_ShouldKeepPlaceholder()
    {
        var template = "Hello, {NAME}! You are {AGE} years old.";
        var variables = new Dictionary<string, string>
        {
            ["NAME"] = "John"
        };

        var result = _service.FormatPrompt(template, variables);

        result.Should().Be("Hello, John! You are {AGE} years old.");
    }

    [Fact]
    public void FormatPrompt_WithEmptyTemplate_ShouldReturnEmpty()
    {
        var template = "";
        var variables = new Dictionary<string, string>
        {
            ["TEST"] = "value"
        };

        var result = _service.FormatPrompt(template, variables);

        result.Should().BeEmpty();
    }

    [Fact]
    public void FormatPrompt_WithNoVariables_ShouldReturnOriginal()
    {
        var template = "This is a template without variables.";
        var variables = new Dictionary<string, string>();

        var result = _service.FormatPrompt(template, variables);

        result.Should().Be(template);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void FullWorkflow_ExpressMode_ShouldGenerateCompletePrompt()
    {
        // Arrange
        var request = CreateTestRequest("express");

        // Act
        var systemPrompt = _service.GetSystemPrompt(request.Mode);
        var userPrompt = _service.GetUserPrompt(request);

        // Assert
        systemPrompt.Should().NotBeEmpty();
        userPrompt.Should().NotBeEmpty();
        
        userPrompt.Should().Contain(request.Project.ProjectName);
        userPrompt.Should().Contain(request.Spec.SongType);
        userPrompt.Should().Contain(request.UserInput!);
    }

    [Fact]
    public void FullWorkflow_CoachMode_ShouldGenerateGuidancePrompt()
    {
        var request = CreateTestRequest("coach");

        var systemPrompt = _service.GetSystemPrompt(request.Mode);
        var userPrompt = _service.GetUserPrompt(request.Mode);

        systemPrompt.Should().Contain("导师");
        userPrompt.Should().Contain("?");
    }

    [Theory]
    [InlineData("coach")]
    [InlineData("express")]
    [InlineData("hybrid")]
    public void AllModes_ShouldGenerateValidPrompts(string mode)
    {
        var request = CreateTestRequest(mode);

        var systemPrompt = _service.GetSystemPrompt(mode);
        var userPrompt = _service.GetUserPrompt(request);

        systemPrompt.Should().NotBeEmpty();
        userPrompt.Should().NotBeEmpty();
        
        systemPrompt.Length.Should().BeGreaterThan(50);
        userPrompt.Length.Should().BeGreaterThan(50);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GetUserPrompt_WithEmptyUserInput_ShouldStillGeneratePrompt()
    {
        var request = CreateTestRequest() with
        {
            UserInput = ""
        };

        var prompt = _service.GetUserPrompt(request);

        prompt.Should().NotBeEmpty();
        prompt.Should().Contain(request.Spec.SongType);
    }

    [Fact]
    public void GetUserPrompt_WithNullUserInput_ShouldStillGeneratePrompt()
    {
        var request = CreateTestRequest() with
        {
            UserInput = null
        };

        var prompt = _service.GetUserPrompt(request);

        prompt.Should().NotBeEmpty();
    }

    [Fact]
    public void GetUserPrompt_WithMultiplePlatforms_ShouldListAll()
    {
        var request = CreateTestRequest();

        var prompt = _service.GetUserPrompt(request);

        prompt.Should().Contain("Suno");
        prompt.Should().Contain("Tunee");
    }

    #endregion
}
