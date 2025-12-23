using System.Text.Json;
using FluentAssertions;
using Musicify.Core.Models;
using Xunit;

namespace Musicify.Core.Tests.Models;

/// <summary>
/// ProjectConfig 模型测试
/// 验证 JSON 序列化兼容性和数据验证
/// </summary>
public class ProjectConfigTests
{
    [Fact]
    public void SerializeToJson_ShouldMatchCLIFormat()
    {
        // Arrange - 创建一个标准的项目配置
        var config = new ProjectConfig
        {
            Name = "测试歌曲",
            Type = "musicify-project",
            Ai = "desktop",
            DefaultType = "流行",
            Created = new DateTime(2025, 12, 23, 10, 30, 0, DateTimeKind.Utc),
            Version = "1.0.0"
        };
        
        // Act - 序列化为 JSON
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        
        // Assert - 验证 JSON 格式符合 CLI 规范
        json.Should().Contain("\"name\": \"测试歌曲\"");
        json.Should().Contain("\"type\": \"musicify-project\"");
        json.Should().Contain("\"defaultType\": \"流行\"");
        json.Should().Contain("\"ai\": \"desktop\"");
        json.Should().Contain("\"version\": \"1.0.0\"");
    }
    
    [Fact]
    public void DeserializeFromJson_ShouldRestoreObject()
    {
        // Arrange - CLI 格式的 JSON 字符串
        var json = """
        {
          "name": "测试歌曲",
          "type": "musicify-project",
          "ai": "claude",
          "scriptType": "sh",
          "defaultType": "流行",
          "created": "2025-12-23T10:30:00Z",
          "version": "1.0.0"
        }
        """;
        
        // Act - 反序列化
        var config = JsonSerializer.Deserialize<ProjectConfig>(json);
        
        // Assert - 验证所有字段正确还原
        config.Should().NotBeNull();
        config!.Name.Should().Be("测试歌曲");
        config.Type.Should().Be("musicify-project");
        config.Ai.Should().Be("claude");
        config.ScriptType.Should().Be("sh");
        config.DefaultType.Should().Be("流行");
        config.Version.Should().Be("1.0.0");
        config.Created.Should().Be(new DateTime(2025, 12, 23, 10, 30, 0, DateTimeKind.Utc));
    }
    
    [Fact]
    public void RoundTrip_ShouldPreserveData()
    {
        // Arrange
        var original = CreateValidConfig();
        
        // Act - 序列化再反序列化
        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<ProjectConfig>(json);
        
        // Assert - 数据应该完全一致
        restored.Should().BeEquivalentTo(original);
    }
    
    [Fact]
    public void IsValid_ShouldReturnTrue_WhenConfigIsValid()
    {
        // Arrange
        var config = CreateValidConfig();
        
        // Act
        var isValid = config.IsValid();
        
        // Assert
        isValid.Should().BeTrue();
    }
    
    [Fact]
    public void IsValid_ShouldReturnFalse_WhenNameIsEmpty()
    {
        // Arrange
        var config = new ProjectConfig
        {
            Name = "",
            Type = "musicify-project",
            Version = "1.0.0",
            Created = DateTime.UtcNow
        };
        
        // Act
        var isValid = config.IsValid();
        
        // Assert
        isValid.Should().BeFalse();
    }
    
    [Fact]
    public void IsValid_ShouldReturnFalse_WhenTypeIsWrong()
    {
        // Arrange
        var config = new ProjectConfig
        {
            Name = "Test",
            Type = "wrong-type",
            Version = "1.0.0",
            Created = DateTime.UtcNow
        };
        
        // Act & Assert
        config.IsValid().Should().BeFalse();
    }
    
    [Fact]
    public void IsValid_ShouldReturnFalse_WhenVersionIsEmpty()
    {
        // Arrange
        var config = new ProjectConfig
        {
            Name = "Test",
            Type = "musicify-project",
            Version = "",
            Created = DateTime.UtcNow
        };
        
        // Act & Assert
        config.IsValid().Should().BeFalse();
    }
    
    // Helper Methods
    
    private static ProjectConfig CreateValidConfig() => new()
    {
        Name = "测试项目",
        Type = "musicify-project",
        Ai = "desktop",
        DefaultType = "流行",
        Created = DateTime.UtcNow,
        Version = "1.0.0"
    };
}
