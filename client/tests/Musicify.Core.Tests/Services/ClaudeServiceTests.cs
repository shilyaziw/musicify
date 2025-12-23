using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Musicify.Core.Models;
using Musicify.Core.Services;
using Xunit;

namespace Musicify.Core.Tests.Services;

/// <summary>
/// ClaudeService 单元测试
/// </summary>
public class ClaudeServiceTests
{
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<IPromptTemplateService> _promptServiceMock;

    public ClaudeServiceTests()
    {
        _configMock = new Mock<IConfiguration>();
        _promptServiceMock = new Mock<IPromptTemplateService>();
        
        // 默认配置
        _configMock.Setup(c => c["AI:DefaultModel"]).Returns("claude-3-5-sonnet-20241022");
        _configMock.Setup(c => c["AI:MaxTokens"]).Returns("4096");
        _configMock.Setup(c => c["AI:Temperature"]).Returns("0.7");
    }

    private AIRequest CreateTestRequest(string mode = "express")
    {
        return new AIRequest
        {
            Mode = mode,
            Project = new ProjectConfig
            {
                Name = "test-song",
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
                ProjectName = "test-song",
                SongType = "流行",
                Duration = "240",
                Style = "轻快",
                Language = "中文",
                Audience = new AudienceInfo { Age = "全年龄", Gender = "中性" },
                TargetPlatform = new List<string> { "Suno" },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            UserInput = "一首关于友情的歌",
            MaxTokens = 4096,
            Temperature = 0.7
        };
    }

    #region GetAvailableModels Tests

    [Fact]
    public void GetAvailableModels_ShouldReturnModelList()
    {
        // Arrange
        _configMock.Setup(c => c["AI:ApiKey"]).Returns("sk-ant-test-key");
        _promptServiceMock.Setup(p => p.GetSystemPrompt(It.IsAny<string>())).Returns("test prompt");
        
        var service = new ClaudeService(_configMock.Object, _promptServiceMock.Object);

        // Act
        var models = service.GetAvailableModels();

        // Assert
        models.Should().NotBeEmpty();
        models.Should().Contain("claude-3-5-sonnet-20241022");
        models.Should().Contain("claude-3-5-haiku-20241022");
        models.Should().HaveCountGreaterThan(3);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidApiKey_ShouldSucceed()
    {
        // Arrange
        _configMock.Setup(c => c["AI:ApiKey"]).Returns("sk-ant-test-key");
        _promptServiceMock.Setup(p => p.GetSystemPrompt(It.IsAny<string>())).Returns("test prompt");

        // Act
        var act = () => new ClaudeService(_configMock.Object, _promptServiceMock.Object);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithoutApiKey_ShouldThrowException()
    {
        // Arrange
        _configMock.Setup(c => c["AI:ApiKey"]).Returns((string?)null);
        
        // Mock environment variable as null
        Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", null);

        // Act
        var act = () => new ClaudeService(_configMock.Object, _promptServiceMock.Object);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*API Key*");
    }

    #endregion

    #region TokenUsage Tests

    [Fact]
    public void GetTokenUsage_Initially_ShouldReturnZero()
    {
        // Arrange
        _configMock.Setup(c => c["AI:ApiKey"]).Returns("sk-ant-test-key");
        _promptServiceMock.Setup(p => p.GetSystemPrompt(It.IsAny<string>())).Returns("test prompt");
        var service = new ClaudeService(_configMock.Object, _promptServiceMock.Object);

        // Act
        var usage = service.GetTokenUsage();

        // Assert
        usage.InputTokens.Should().Be(0);
        usage.OutputTokens.Should().Be(0);
        usage.TotalTokens.Should().Be(0);
    }

    #endregion

    #region Request Validation Tests

    [Fact]
    public void CreateRequest_WithAllFields_ShouldBeValid()
    {
        var request = CreateTestRequest();

        request.Mode.Should().Be("express");
        request.Project.ProjectName.Should().Be("test-song");
        request.Spec.SongType.Should().Be("流行");
        request.UserInput.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("coach")]
    [InlineData("express")]
    [InlineData("hybrid")]
    public void CreateRequest_WithDifferentModes_ShouldBeValid(string mode)
    {
        var request = CreateTestRequest(mode);

        request.Mode.Should().Be(mode);
    }

    #endregion

    #region Response Model Tests

    [Fact]
    public void AIResponse_WithValidData_ShouldCalculateTotalTokens()
    {
        var usage = new TokenUsage
        {
            InputTokens = 100,
            OutputTokens = 200,
            EstimatedCost = 0.001m
        };

        usage.TotalTokens.Should().Be(300);
    }

    [Fact]
    public void AIResponse_ShouldHaveCreatedAt()
    {
        var response = new AIResponse
        {
            Content = "Test content",
            Model = "claude-3-5-sonnet-20241022",
            Usage = new TokenUsage
            {
                InputTokens = 10,
                OutputTokens = 20
            },
            StopReason = "end_turn"
        };

        response.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void AIRequest_WithoutMode_ShouldThrowException()
    {
        // Arrange & Act
        var act = () => new AIRequest
        {
            Mode = null!,
            Project = CreateTestRequest().Project,
            Spec = CreateTestRequest().Spec
        };

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AIRequest_WithoutProject_ShouldThrowException()
    {
        var act = () => new AIRequest
        {
            Mode = "express",
            Project = null!,
            Spec = CreateTestRequest().Spec
        };

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AIRequest_WithoutSpec_ShouldThrowException()
    {
        var act = () => new AIRequest
        {
            Mode = "express",
            Project = CreateTestRequest().Project,
            Spec = null!
        };

        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Configuration Tests

    [Theory]
    [InlineData(1024)]
    [InlineData(2048)]
    [InlineData(4096)]
    [InlineData(8192)]
    public void AIRequest_WithDifferentMaxTokens_ShouldBeValid(int maxTokens)
    {
        var request = CreateTestRequest() with { MaxTokens = maxTokens };

        request.MaxTokens.Should().Be(maxTokens);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(0.7)]
    [InlineData(1.0)]
    public void AIRequest_WithDifferentTemperature_ShouldBeValid(double temperature)
    {
        var request = CreateTestRequest() with { Temperature = temperature };

        request.Temperature.Should().Be(temperature);
    }

    #endregion

    #region Prompt Service Integration Tests

    [Fact]
    public void Service_ShouldUsePromptService()
    {
        // Arrange
        _configMock.Setup(c => c["AI:ApiKey"]).Returns("sk-ant-test-key");
        _promptServiceMock.Setup(p => p.GetSystemPrompt("express"))
            .Returns("Test system prompt");
        
        var service = new ClaudeService(_configMock.Object, _promptServiceMock.Object);

        // Act & Assert
        // Service should be created successfully with prompt service
        service.Should().NotBeNull();
    }

    #endregion
}
