using FluentAssertions;
using Moq;
using Musicify.Core.Abstractions;
using Musicify.Core.Models;
using Musicify.Core.Services;
using Musicify.Core.ViewModels;
using Xunit;

namespace Musicify.Core.Tests.ViewModels;

/// <summary>
/// 歌词编辑器 ViewModel 测试
/// </summary>
public class LyricsEditorViewModelTests
{
    private readonly Mock<IProjectService> _projectServiceMock;
    private readonly Mock<IFileSystem> _fileSystemMock;

    public LyricsEditorViewModelTests()
    {
        _projectServiceMock = new Mock<IProjectService>();
        _fileSystemMock = new Mock<IFileSystem>();
    }

    private LyricsEditorViewModel CreateViewModel()
    {
        return new LyricsEditorViewModel(
            _projectServiceMock.Object,
            _fileSystemMock.Object);
    }

    private ProjectConfig CreateTestProject()
    {
        return new ProjectConfig
        {
            Name = "test-song",
            Type = "musicify-project",
            Ai = "desktop",
            ScriptType = "coach",
            DefaultType = "pop",
            Created = DateTime.UtcNow,
            Version = "1.0.0",
            ProjectPath = "/test/project",
            UpdatedAt = DateTime.UtcNow,
            Status = "draft",
            Spec = null
        };
    }

    #region 构造函数测试

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange & Act
        var vm = CreateViewModel();

        // Assert
        vm.CurrentProject.Should().BeNull();
        vm.LyricsText.Should().BeEmpty();
        vm.WordCount.Should().Be(0);
        vm.SectionCount.Should().Be(0);
        vm.LineCount.Should().Be(0);
        vm.IsModified.Should().BeFalse();
        vm.ShowPreview.Should().BeFalse();
        vm.ErrorMessage.Should().BeNull();
    }

    #endregion

    #region 属性测试

    [Fact]
    public void LyricsText_WhenSet_ShouldUpdateIsModified()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.LyricsText = "测试歌词";

        // Assert
        vm.IsModified.Should().BeTrue();
    }

    [Fact]
    public void LyricsText_WhenChanged_ShouldUpdateStatistics()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.LyricsText = "[Verse 1]\n第一行\n第二行";

        // Assert
        vm.SectionCount.Should().Be(1);
        vm.LineCount.Should().BeGreaterThan(0);
        vm.WordCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void UpdateStatistics_WithEmptyText_ShouldReturnZero()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.LyricsText = string.Empty;

        // Assert
        vm.WordCount.Should().Be(0);
        vm.SectionCount.Should().Be(0);
        vm.LineCount.Should().Be(0);
    }

    [Fact]
    public void UpdateStatistics_WithMultipleSections_ShouldCountCorrectly()
    {
        // Arrange
        var vm = CreateViewModel();
        var lyrics = @"[Verse 1]
第一段歌词
第二段歌词

[Chorus]
副歌歌词

[Verse 2]
第三段歌词";

        // Act
        vm.LyricsText = lyrics;

        // Assert
        vm.SectionCount.Should().Be(3);
        vm.LineCount.Should().BeGreaterThan(5);
    }

    [Fact]
    public void UpdateStatistics_ShouldExcludeSectionMarkersFromWordCount()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.LyricsText = "[Verse 1]\n测试歌词";

        // Assert
        vm.WordCount.Should().Be(4); // 只统计"测试歌词",不包含"[Verse 1]"
    }

    #endregion

    #region SetProjectAsync 测试

    [Fact]
    public async Task SetProjectAsync_ShouldSetCurrentProject()
    {
        // Arrange
        var vm = CreateViewModel();
        var project = CreateTestProject();
        _fileSystemMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        // Act
        await vm.SetProjectAsync(project);

        // Assert
        vm.CurrentProject.Should().Be(project);
    }

    [Fact]
    public async Task SetProjectAsync_WithExistingLyricsFile_ShouldLoadLyrics()
    {
        // Arrange
        var vm = CreateViewModel();
        var project = CreateTestProject();
        var lyricsContent = "[Verse 1]\n测试歌词";
        
        _fileSystemMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _fileSystemMock.Setup(f => f.ReadAllTextAsync(It.IsAny<string>()))
            .ReturnsAsync(lyricsContent);

        // Act
        await vm.SetProjectAsync(project);

        // Assert
        vm.LyricsText.Should().Be(lyricsContent);
        vm.IsModified.Should().BeFalse();
    }

    #endregion

    #region SaveLyricsAsync 测试

    [Fact]
    public async Task SaveLyricsAsync_WithNoProject_ShouldSetErrorMessage()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        await vm.SaveLyricsAsync();

        // Assert
        vm.ErrorMessage.Should().Contain("请先打开项目");
    }

    [Fact]
    public async Task SaveLyricsAsync_WithValidProject_ShouldSaveToFile()
    {
        // Arrange
        var vm = CreateViewModel();
        var project = CreateTestProject();
        vm.CurrentProject = project;
        vm.LyricsText = "[Verse 1]\n测试歌词";
        
        _fileSystemMock.Setup(f => f.DirectoryExists(It.IsAny<string>())).Returns(true);
        _fileSystemMock.Setup(f => f.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await vm.SaveLyricsAsync();

        // Assert
        _fileSystemMock.Verify(f => f.WriteAllTextAsync(
            It.Is<string>(p => p.Contains("lyrics.txt")),
            It.Is<string>(c => c.Contains("测试歌词"))),
            Times.Once);
        vm.IsModified.Should().BeFalse();
        vm.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task SaveLyricsAsync_WithException_ShouldSetErrorMessage()
    {
        // Arrange
        var vm = CreateViewModel();
        var project = CreateTestProject();
        vm.CurrentProject = project;
        
        _fileSystemMock.Setup(f => f.DirectoryExists(It.IsAny<string>())).Returns(true);
        _fileSystemMock.Setup(f => f.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("写入失败"));

        // Act
        await vm.SaveLyricsAsync();

        // Assert
        vm.ErrorMessage.Should().Contain("保存失败");
    }

    #endregion

    #region FormatLyrics 测试

    [Fact]
    public void FormatLyrics_ShouldNormalizeSectionMarkers()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.LyricsText = "[verse 1]\n歌词\n[chorus]\n副歌";

        // Act
        vm.FormatLyricsCommand.Execute(null);

        // Assert
        vm.LyricsText.Should().Contain("[Verse 1]");
        vm.LyricsText.Should().Contain("[Chorus]");
    }

    [Fact]
    public void FormatLyrics_ShouldPreserveContent()
    {
        // Arrange
        var vm = CreateViewModel();
        var originalContent = "[verse 1]\n测试歌词内容";
        vm.LyricsText = originalContent;

        // Act
        vm.FormatLyricsCommand.Execute(null);

        // Assert
        vm.LyricsText.Should().Contain("测试歌词内容");
    }

    #endregion

    #region TogglePreview 测试

    [Fact]
    public void TogglePreview_ShouldToggleShowPreview()
    {
        // Arrange
        var vm = CreateViewModel();
        var initialValue = vm.ShowPreview;

        // Act
        vm.TogglePreviewCommand.Execute(null);

        // Assert
        vm.ShowPreview.Should().Be(!initialValue);
    }

    #endregion

    #region LoadLyricsAsync 测试

    [Fact]
    public async Task LoadLyricsAsync_WithNoProject_ShouldNotThrow()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act & Assert
        await vm.Invoking(v => v.LoadLyricsAsync())
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task LoadLyricsAsync_WithNonExistentFile_ShouldSetEmptyText()
    {
        // Arrange
        var vm = CreateViewModel();
        var project = CreateTestProject();
        vm.CurrentProject = project;
        
        _fileSystemMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        // Act
        await vm.LoadLyricsAsync();

        // Assert
        vm.LyricsText.Should().BeEmpty();
        vm.IsModified.Should().BeFalse();
    }

    [Fact]
    public async Task LoadLyricsAsync_WithExistingFile_ShouldLoadContent()
    {
        // Arrange
        var vm = CreateViewModel();
        var project = CreateTestProject();
        vm.CurrentProject = project;
        var lyricsContent = "[Verse 1]\n测试歌词";
        
        _fileSystemMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _fileSystemMock.Setup(f => f.ReadAllTextAsync(It.IsAny<string>()))
            .ReturnsAsync(lyricsContent);

        // Act
        await vm.LoadLyricsAsync();

        // Assert
        vm.LyricsText.Should().Be(lyricsContent);
        vm.IsModified.Should().BeFalse();
    }

    #endregion
}

