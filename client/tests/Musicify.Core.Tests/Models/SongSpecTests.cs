using System.Text.Json;
using FluentAssertions;
using Musicify.Core.Models;
using Musicify.Core.Models.Constants;
using Xunit;

namespace Musicify.Core.Tests.Models;

/// <summary>
/// SongSpec 模型测试
/// 验证歌曲规格的序列化和验证逻辑
/// </summary>
public class SongSpecTests
{
    [Fact]
    public void SerializeToJson_ShouldMatchCLIFormat()
    {
        // Arrange
        var spec = CreateValidSpec();
        
        // Act
        var json = JsonSerializer.Serialize(spec, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        
        // Assert - 验证 snake_case 命名
        json.Should().Contain("\"project_name\"");
        json.Should().Contain("\"song_type\"");
        json.Should().Contain("\"target_platform\"");
        json.Should().Contain("\"created_at\"");
        json.Should().Contain("\"updated_at\"");
    }
    
    [Fact]
    public void DeserializeFromJson_ShouldRestoreObject()
    {
        // Arrange - CLI 格式的 JSON
        var json = """
        {
          "project_name": "我的歌曲",
          "song_type": "流行",
          "duration": "3分30秒",
          "style": "抒情",
          "language": "中文",
          "audience": {
            "age": "20-30岁",
            "gender": "中性"
          },
          "target_platform": ["QQ音乐", "网易云音乐"],
          "tone": "温暖治愈",
          "created_at": "2025-12-23T10:30:00Z",
          "updated_at": "2025-12-23T10:30:00Z"
        }
        """;
        
        // Act
        var spec = JsonSerializer.Deserialize<SongSpec>(json);
        
        // Assert
        spec.Should().NotBeNull();
        spec!.ProjectName.Should().Be("我的歌曲");
        spec.SongType.Should().Be("流行");
        spec.Duration.Should().Be("3分30秒");
        spec.Style.Should().Be("抒情");
        spec.Language.Should().Be("中文");
        spec.Audience.Should().NotBeNull();
        spec.Audience.Age.Should().Be("20-30岁");
        spec.Audience.Gender.Should().Be("中性");
        spec.TargetPlatform.Should().HaveCount(2);
        spec.Tone.Should().Be("温暖治愈");
    }
    
    [Fact]
    public void Validate_ShouldSucceed_WhenAllFieldsValid()
    {
        // Arrange
        var spec = CreateValidSpec();
        
        // Act
        var result = spec.Validate();
        
        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
    
    [Fact]
    public void Validate_ShouldFail_WhenProjectNameEmpty()
    {
        // Arrange
        var spec = CreateValidSpec() with { ProjectName = "" };
        
        // Act
        var result = spec.Validate();
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("项目名称"));
    }
    
    [Fact]
    public void Validate_ShouldFail_WhenSongTypeInvalid()
    {
        // Arrange
        var spec = CreateValidSpec() with { SongType = "无效类型" };
        
        // Act
        var result = spec.Validate();
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("无效的歌曲类型"));
    }
    
    [Fact]
    public void Validate_ShouldFail_WhenDurationEmpty()
    {
        // Arrange
        var spec = CreateValidSpec() with { Duration = "" };
        
        // Act
        var result = spec.Validate();
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("时长"));
    }
    
    [Fact]
    public void Validate_ShouldFail_WhenStyleInvalid()
    {
        // Arrange
        var spec = CreateValidSpec() with { Style = "无效风格" };
        
        // Act
        var result = spec.Validate();
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("无效的风格"));
    }
    
    [Fact]
    public void Validate_ShouldFail_WhenLanguageInvalid()
    {
        // Arrange
        var spec = CreateValidSpec() with { Language = "火星语" };
        
        // Act
        var result = spec.Validate();
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("无效的语言"));
    }
    
    [Fact]
    public void Validate_ShouldFail_WhenNoPlatformSelected()
    {
        // Arrange
        var spec = CreateValidSpec() with { TargetPlatform = new List<string>() };
        
        // Act
        var result = spec.Validate();
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("目标平台"));
    }
    
    [Fact]
    public void Validate_ShouldCollectMultipleErrors()
    {
        // Arrange - 创建一个包含多个错误的规格
        var spec = new SongSpec
        {
            ProjectName = "",
            SongType = "无效",
            Duration = "",
            Style = "无效",
            Language = "无效",
            Audience = new AudienceInfo { Age = "20-30岁", Gender = "中性" },
            TargetPlatform = new List<string>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        // Act
        var result = spec.Validate();
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(3);
    }
    
    // Helper Methods
    
    private static SongSpec CreateValidSpec() => new()
    {
        ProjectName = "测试歌曲",
        SongType = SongTypes.Pop,
        Duration = "3分30秒",
        Style = Styles.Lyrical,
        Language = Languages.Chinese,
        Audience = new AudienceInfo
        {
            Age = "20-30岁",
            Gender = "中性"
        },
        TargetPlatform = new List<string> { Platforms.QQMusic, Platforms.NetEaseMusic },
        Tone = "温暖",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
