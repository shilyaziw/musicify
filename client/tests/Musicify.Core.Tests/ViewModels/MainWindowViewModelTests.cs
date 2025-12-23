using FluentAssertions;
using Moq;
using Musicify.Core.Models;
using Musicify.Core.Services;
using Musicify.Core.ViewModels;
using Xunit;

namespace Musicify.Core.Tests.ViewModels;

/// <summary>
/// 主编辑窗口 ViewModel 测试
/// </summary>
public class MainWindowViewModelTests
{
    private readonly Mock<IProjectService> _projectServiceMock;
    private readonly Mock<INavigationService> _navigationServiceMock;

    public MainWindowViewModelTests()
    {
        _projectServiceMock = new Mock<IProjectService>();
        _navigationServiceMock = new Mock<INavigationService>();
    }

    private MainWindowViewModel CreateViewModel()
    {
        return new MainWindowViewModel(
            _projectServiceMock.Object,
            _navigationServiceMock.Object);
    }

    private ProjectConfig CreateTestProject()
    {
        return new ProjectConfig(
            Name: "test-song",
            Type: "lyrics",
            Ai: "claude",
            ScriptType: "coach",
            DefaultType: "pop",
            Created: DateTime.UtcNow,
            Version: "1.0.0",
            ProjectPath: "/test/project",
            UpdatedAt: DateTime.UtcNow,
            Status: "draft",
            Spec: new SongSpec(
                ProjectName: "test-song",
                SongType: "pop",
                Duration: "3:30",
                Style: "modern",
                Language: "zh",
                Audience: new AudienceInfo("18-30", "all"),
                TargetPlatform: new List<string> { "suno" },
                CreatedAt: DateTime.UtcNow,
                UpdatedAt: DateTime.UtcNow,
                Tone: "happy"
            )
        );
    }

    #region 构造函数测试

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange & Act
        var vm = CreateViewModel();

        // Assert
        vm.CurrentProject.Should().BeNull();
        vm.CurrentView.Should().Be("ProjectOverview");
        vm.ProjectSummary.Should().BeNull();
        vm.IsLoading.Should().BeFalse();
        vm.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithNullNavigationService_ShouldNotThrow()
    {
        // Arrange & Act
        var vm = new MainWindowViewModel(
            _projectServiceMock.Object,
            null);

        // Assert
        vm.Should().NotBeNull();
    }

    #endregion

    #region LoadProjectAsync 测试

    [Fact]
    public async Task LoadProjectAsync_WithValidProject_ShouldLoadProject()
    {
        // Arrange
        var project = CreateTestProject();
        _projectServiceMock.Setup(s => s.LoadProjectAsync(It.IsAny<string>()))
            .ReturnsAsync(project);

        var vm = CreateViewModel();

        // Act
        await vm.LoadProjectAsync("/test/project");

        // Assert
        vm.CurrentProject.Should().NotBeNull();
        vm.CurrentProject!.Name.Should().Be("test-song");
        vm.ProjectSummary.Should().NotBeNull();
        vm.ProjectSummary!.ProjectName.Should().Be("test-song");
        vm.ProjectSummary.Status.Should().Be("draft");
        vm.ProjectSummary.SongType.Should().Be("pop");
        vm.IsLoading.Should().BeFalse();
        vm.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task LoadProjectAsync_WithFileNotFoundException_ShouldSetErrorMessage()
    {
        // Arrange
        _projectServiceMock.Setup(s => s.LoadProjectAsync(It.IsAny<string>()))
            .ThrowsAsync(new FileNotFoundException("文件未找到"));

        var vm = CreateViewModel();

        // Act
        await vm.LoadProjectAsync("/test/project");

        // Assert
        vm.CurrentProject.Should().BeNull();
        vm.ErrorMessage.Should().Contain("项目文件未找到");
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadProjectAsync_WithJsonException_ShouldSetErrorMessage()
    {
        // Arrange
        _projectServiceMock.Setup(s => s.LoadProjectAsync(It.IsAny<string>()))
            .ThrowsAsync(new System.Text.Json.JsonException("JSON 格式错误"));

        var vm = CreateViewModel();

        // Act
        await vm.LoadProjectAsync("/test/project");

        // Assert
        vm.CurrentProject.Should().BeNull();
        vm.ErrorMessage.Should().Contain("项目文件格式错误");
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadProjectAsync_WithGenericException_ShouldSetErrorMessage()
    {
        // Arrange
        _projectServiceMock.Setup(s => s.LoadProjectAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("未知错误"));

        var vm = CreateViewModel();

        // Act
        await vm.LoadProjectAsync("/test/project");

        // Assert
        vm.CurrentProject.Should().BeNull();
        vm.ErrorMessage.Should().Contain("加载项目失败");
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadProjectAsync_ShouldSetIsLoadingDuringOperation()
    {
        // Arrange
        var project = CreateTestProject();
        var tcs = new TaskCompletionSource<ProjectConfig>();
        _projectServiceMock.Setup(s => s.LoadProjectAsync(It.IsAny<string>()))
            .Returns(tcs.Task);

        var vm = CreateViewModel();

        // Act
        var loadTask = vm.LoadProjectAsync("/test/project");
        
        // Assert - 加载中
        vm.IsLoading.Should().BeTrue();
        
        // 完成加载
        tcs.SetResult(project);
        await loadTask;

        // Assert - 加载完成
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadProjectAsync_WithMidiFile_ShouldSetHasMidiFile()
    {
        // Arrange
        var project = CreateTestProject();
        project = project with
        {
            Spec = project.Spec! with
            {
                MidiFilePath = "/test/song.mid"
            }
        };
        
        _projectServiceMock.Setup(s => s.LoadProjectAsync(It.IsAny<string>()))
            .ReturnsAsync(project);

        var vm = CreateViewModel();

        // Act
        await vm.LoadProjectAsync("/test/project");

        // Assert
        vm.ProjectSummary.Should().NotBeNull();
        vm.ProjectSummary!.HasMidiFile.Should().BeTrue();
    }

    #endregion

    #region 导航命令测试

    [Fact]
    public void NavigateToLyricsEditorCommand_ShouldChangeCurrentView()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.NavigateToLyricsEditorCommand.Execute(null);

        // Assert
        vm.CurrentView.Should().Be("LyricsEditor");
    }

    [Fact]
    public void NavigateToAIChatCommand_ShouldChangeCurrentView()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.NavigateToAIChatCommand.Execute(null);

        // Assert
        vm.CurrentView.Should().Be("AIChat");
    }

    [Fact]
    public void NavigateToMidiAnalysisCommand_ShouldChangeCurrentView()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.NavigateToMidiAnalysisCommand.Execute(null);

        // Assert
        vm.CurrentView.Should().Be("MidiAnalysis");
    }

    [Fact]
    public void NavigateToProjectSettingsCommand_ShouldChangeCurrentView()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.NavigateToProjectSettingsCommand.Execute(null);

        // Assert
        vm.CurrentView.Should().Be("ProjectSettings");
    }

    [Fact]
    public void NavigateToProjectOverviewCommand_ShouldChangeCurrentView()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.CurrentView = "LyricsEditor"; // 先切换到其他视图

        // Act
        vm.NavigateToProjectOverviewCommand.Execute(null);

        // Assert
        vm.CurrentView.Should().Be("ProjectOverview");
    }

    #endregion

    #region SaveProjectAsync 测试

    [Fact]
    public async Task SaveProjectAsync_WithNoProject_ShouldSetErrorMessage()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        await vm.SaveProjectAsync();

        // Assert
        vm.ErrorMessage.Should().Contain("没有可保存的项目");
    }

    [Fact]
    public async Task SaveProjectAsync_WithValidProject_ShouldSaveProject()
    {
        // Arrange
        var project = CreateTestProject();
        _projectServiceMock.Setup(s => s.SaveProjectAsync(It.IsAny<ProjectConfig>()))
            .Returns(Task.CompletedTask);

        var vm = CreateViewModel();
        vm.CurrentProject = project;
        vm.ProjectSummary = vm.CreateProjectSummary(project);

        // Act
        await vm.SaveProjectAsync();

        // Assert
        _projectServiceMock.Verify(s => s.SaveProjectAsync(project), Times.Once);
        vm.ErrorMessage.Should().BeNull();
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task SaveProjectAsync_WithException_ShouldSetErrorMessage()
    {
        // Arrange
        var project = CreateTestProject();
        _projectServiceMock.Setup(s => s.SaveProjectAsync(It.IsAny<ProjectConfig>()))
            .ThrowsAsync(new Exception("保存失败"));

        var vm = CreateViewModel();
        vm.CurrentProject = project;

        // Act
        await vm.SaveProjectAsync();

        // Assert
        vm.ErrorMessage.Should().Contain("保存项目失败");
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task SaveProjectAsync_ShouldUpdateProjectSummary()
    {
        // Arrange
        var project = CreateTestProject();
        _projectServiceMock.Setup(s => s.SaveProjectAsync(It.IsAny<ProjectConfig>()))
            .Returns(Task.CompletedTask);

        var vm = CreateViewModel();
        vm.CurrentProject = project;

        // Act
        await vm.SaveProjectAsync();

        // Assert
        vm.ProjectSummary.Should().NotBeNull();
        vm.ProjectSummary!.ProjectName.Should().Be(project.Name);
    }

    #endregion
}

