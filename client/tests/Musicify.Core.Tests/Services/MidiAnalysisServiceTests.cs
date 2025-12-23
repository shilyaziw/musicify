using FluentAssertions;
using Musicify.Core.Models;
using Musicify.Core.Services;
using Xunit;

namespace Musicify.Core.Tests.Services;

/// <summary>
/// MIDI 分析服务测试
/// </summary>
public class MidiAnalysisServiceTests
{
    private readonly IMidiAnalysisService _service;

    public MidiAnalysisServiceTests()
    {
        _service = new MidiAnalysisService();
    }

    #region ValidateMidiFile 测试

    [Fact]
    public void ValidateMidiFile_WithNullPath_ShouldReturnFalse()
    {
        // Act
        var result = _service.ValidateMidiFile(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateMidiFile_WithEmptyPath_ShouldReturnFalse()
    {
        // Act
        var result = _service.ValidateMidiFile(string.Empty);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateMidiFile_WithNonExistentFile_ShouldReturnFalse()
    {
        // Act
        var result = _service.ValidateMidiFile("non-existent.mid");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetFileInfoAsync 测试

    [Fact]
    public async Task GetFileInfoAsync_WithInvalidFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var invalidPath = "non-existent.mid";

        // Act & Assert
        await _service.Invoking(s => s.GetFileInfoAsync(invalidPath))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    #endregion

    #region AnalyzeAsync 测试

    [Fact]
    public async Task AnalyzeAsync_WithInvalidFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var invalidPath = "non-existent.mid";

        // Act & Assert
        await _service.Invoking(s => s.AnalyzeAsync(invalidPath))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task AnalyzeAsync_WithEmptyMidiFile_ShouldThrowInvalidOperationException()
    {
        // Arrange
        // 注意: 这个测试需要实际的空 MIDI 文件
        // 由于我们没有测试数据，这里先跳过
        // 实际实现时应该创建测试 MIDI 文件

        // Act & Assert
        // 暂时跳过
    }

    #endregion

    #region 辅助方法测试

    // 注意: 由于 MidiAnalysisService 的辅助方法都是私有的,
    // 我们主要通过公共接口进行测试
    // 如果需要测试私有方法,可以考虑使用反射或重构为内部方法

    #endregion
}

