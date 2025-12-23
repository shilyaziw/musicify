using Xunit;
using FluentAssertions;
using Moq;
using Musicify.Core.ViewModels;
using Musicify.Core.Services;
using Musicify.Core.Models;

namespace Musicify.Core.Tests.ViewModels;

/// <summary>
/// CreateProjectViewModel 测试
/// 测试覆盖:
/// - 初始化
/// - 输入验证
/// - 项目创建
/// - 导航流程
/// - 错误处理
/// </summary>
public class CreateProjectViewModelTests
{
    private readonly Mock<IProjectService> _mockProjectService;
    private readonly Mock<INavigationService> _mockNavigationService;
    private readonly CreateProjectViewModel _viewModel;

    public CreateProjectViewModelTests()
    {
        _mockProjectService = new Mock<IProjectService>();
        _mockNavigationService = new Mock<INavigationService>();
        _viewModel = new CreateProjectViewModel(
            _mockProjectService.Object,
            _mockNavigationService.Object
        );
    }

    #region 初始化测试

    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Assert
        _viewModel.ProjectName.Should().BeEmpty();
        _viewModel.ProjectPath.Should().BeEmpty();
        _viewModel.CreationMode.Should().Be("coach"); // 默认教练模式
        _viewModel.CurrentStep.Should().Be(1);
        _viewModel.TotalSteps.Should().Be(4);
        _viewModel.CanGoNext().Should().BeFalse();
        _viewModel.CanGoBack().Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CreateProjectViewModel(null!, _mockNavigationService.Object));

        Assert.Throws<ArgumentNullException>(() =>
            new CreateProjectViewModel(_mockProjectService.Object, null!));
    }

    #endregion

    #region 步骤 1: 基本信息测试

    [Theory]
    [InlineData("", "", false)] // 都为空
    [InlineData("My Project", "", false)] // 只有项目名
    [InlineData("", "/path/to/project", false)] // 只有路径
    [InlineData("My Project", "/path/to/project", true)] // 都填写
    public void Step1_ProjectNameAndPath_ShouldValidateCorrectly(
        string projectName, string projectPath, bool expected)
    {
        // Arrange
        _viewModel.CurrentStep = 1;

        // Act
        _viewModel.ProjectName = projectName;
        _viewModel.ProjectPath = projectPath;

        // Assert
        _viewModel.CanGoNext().Should().Be(expected);
    }

    [Fact]
    public void ProjectName_WithInvalidCharacters_ShouldShowValidationError()
    {
        // Act
        _viewModel.ProjectName = "My/Invalid\\Project*";

        // Assert
        _viewModel.ValidationErrors.Should().ContainKey("ProjectName");
        _viewModel.ValidationErrors["ProjectName"].Should().Contain("包含非法字符");
    }

    [Fact]
    public void ProjectPath_WhenExists_ShouldShowWarning()
    {
        // Arrange
        var existingPath = "/path/to/existing/project";
        _mockProjectService
            .Setup(x => x.ValidateProjectPath(existingPath))
            .Returns(false); // 路径已存在

        // Act
        _viewModel.ProjectPath = existingPath;

        // Assert
        _viewModel.ValidationErrors.Should().ContainKey("ProjectPath");
        _viewModel.ValidationErrors["ProjectPath"].Should().Contain("已存在");
    }

    [Fact]
    public void BrowseProjectPathCommand_ShouldSetProjectPath()
    {
        // Arrange
        var selectedPath = "/users/test/musicify-projects";
        _viewModel.OnBrowsePathRequested = () => Task.FromResult(selectedPath);

        // Act
        _viewModel.BrowseProjectPathCommand.Execute(null);

        // Assert
        _viewModel.ProjectPath.Should().Be(selectedPath);
    }

    #endregion

    #region 步骤 2: 歌曲信息测试

    [Fact]
    public void Step2_SongSpec_ShouldInitializeWithDefaults()
    {
        // Assert
        _viewModel.SongSpec.Should().NotBeNull();
        _viewModel.SongSpec.SongType.Should().Be("原创");
        _viewModel.SongSpec.Style.Should().Be("流行");
        _viewModel.SongSpec.Language.Should().Be("中文");
    }

    [Theory]
    [InlineData("流行", true)]
    [InlineData("摇滚", true)]
    [InlineData("民谣", true)]
    [InlineData("", false)]
    public void Step2_StyleSelection_ShouldValidate(string style, bool isValid)
    {
        // Arrange
        _viewModel.CurrentStep = 2;

        // Act
        _viewModel.SongSpec.Style = style;

        // Assert
        _viewModel.CanGoNext.Should().Be(isValid);
    }

    [Fact]
    public void Step2_ThemeAndKeywords_ShouldBeOptional()
    {
        // Arrange
        _viewModel.CurrentStep = 2;
        _viewModel.SongSpec.Style = "流行";

        // Act
        _viewModel.SongSpec.Theme = ""; // 留空
        _viewModel.SongSpec.Keywords = new List<string>(); // 空列表

        // Assert
        _viewModel.CanGoNext.Should().BeTrue(); // 仍然可以继续
    }

    #endregion

    #region 步骤 3: 创作模式测试

    [Theory]
    [InlineData("coach", "教练模式")]
    [InlineData("express", "快速模式")]
    [InlineData("hybrid", "混合模式")]
    public void Step3_CreationModeSelection_ShouldUpdateDescription(
        string mode, string expectedDescription)
    {
        // Arrange
        _viewModel.CurrentStep = 3;

        // Act
        _viewModel.CreationMode = mode;

        // Assert
        _viewModel.CreationModeDescription.Should().Contain(expectedDescription);
    }

    [Fact]
    public void Step3_CoachMode_ShouldShowMidiOption()
    {
        // Act
        _viewModel.CreationMode = "coach";

        // Assert
        _viewModel.ShowMidiOption.Should().BeTrue();
    }

    [Fact]
    public void Step3_ExpressMode_ShouldHideMidiOption()
    {
        // Act
        _viewModel.CreationMode = "express";

        // Assert
        _viewModel.ShowMidiOption.Should().BeFalse();
    }

    [Fact]
    public void Step3_SelectMidiFile_ShouldSetMidiPath()
    {
        // Arrange
        var midiPath = "/path/to/reference.mid";
        _viewModel.OnBrowseMidiRequested = () => Task.FromResult(midiPath);

        // Act
        _viewModel.SelectMidiFileCommand.Execute(null);

        // Assert
        _viewModel.MidiFilePath.Should().Be(midiPath);
    }

    #endregion

    #region 步骤 4: 确认和创建测试

    [Fact]
    public void Step4_Summary_ShouldDisplayAllSettings()
    {
        // Arrange
        _viewModel.ProjectName = "My Song";
        _viewModel.ProjectPath = "/path/to/project";
        _viewModel.SongSpec.Style = "流行";
        _viewModel.CreationMode = "coach";

        // Act
        _viewModel.CurrentStep = 4;

        // Assert
        _viewModel.ProjectSummary.Should().Contain("My Song");
        _viewModel.ProjectSummary.Should().Contain("流行");
        _viewModel.ProjectSummary.Should().Contain("教练模式");
    }

    [Fact]
    public async Task CreateProjectCommand_WithValidInputs_ShouldCreateAndNavigate()
    {
        // Arrange
        _viewModel.ProjectName = "Test Project";
        _viewModel.ProjectPath = "/path/to/project";
        _viewModel.SongSpec.Style = "流行";

        var expectedConfig = new ProjectConfig
        {
            ProjectName = "Test Project",
            ProjectPath = "/path/to/project",
            SongSpec = _viewModel.SongSpec
        };

        _mockProjectService
            .Setup(x => x.CreateProjectAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<SongSpec>()))
            .ReturnsAsync(expectedConfig);

        // Act
        await _viewModel.CreateProjectCommand.ExecuteAsync(null);

        // Assert
        _mockProjectService.Verify(x => x.CreateProjectAsync(
            "Test Project",
            "/path/to/project",
            It.IsAny<SongSpec>()), Times.Once);

        _mockNavigationService.Verify(x => x.NavigateTo("MainWindow", expectedConfig), Times.Once);
    }

    [Fact]
    public async Task CreateProjectCommand_OnError_ShouldShowErrorMessage()
    {
        // Arrange
        _viewModel.ProjectName = "Test Project";
        _viewModel.ProjectPath = "/path/to/project";

        _mockProjectService
            .Setup(x => x.CreateProjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SongSpec>()))
            .ThrowsAsync(new IOException("Failed to create project directory"));

        // Act
        await _viewModel.CreateProjectCommand.ExecuteAsync(null);

        // Assert
        _viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
        _viewModel.ErrorMessage.Should().Contain("创建项目失败");
        _mockNavigationService.Verify(x => x.NavigateTo(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    #endregion

    #region 导航测试

    [Fact]
    public void NextStepCommand_ShouldIncrementStep()
    {
        // Arrange
        _viewModel.CurrentStep = 1;
        _viewModel.ProjectName = "Test";
        _viewModel.ProjectPath = "/path";

        // Act
        _viewModel.NextStepCommand.Execute(null);

        // Assert
        _viewModel.CurrentStep.Should().Be(2);
    }

    [Fact]
    public void PreviousStepCommand_ShouldDecrementStep()
    {
        // Arrange
        _viewModel.CurrentStep = 2;

        // Act
        _viewModel.PreviousStepCommand.Execute(null);

        // Assert
        _viewModel.CurrentStep.Should().Be(1);
    }

    [Fact]
    public void CanGoBack_OnFirstStep_ShouldBeFalse()
    {
        // Arrange
        _viewModel.CurrentStep = 1;

        // Assert
        _viewModel.CanGoBack.Should().BeFalse();
    }

    [Fact]
    public void CanGoNext_OnLastStep_ShouldBeFalse()
    {
        // Arrange
        _viewModel.CurrentStep = 4;

        // Assert
        _viewModel.CanGoNext.Should().BeFalse();
    }

    [Fact]
    public void CancelCommand_ShouldNavigateToWelcome()
    {
        // Act
        _viewModel.CancelCommand.Execute(null);

        // Assert
        _mockNavigationService.Verify(x => x.NavigateTo("WelcomeWindow", null), Times.Once);
    }

    #endregion

    #region 进度计算测试

    [Theory]
    [InlineData(1, 25)]
    [InlineData(2, 50)]
    [InlineData(3, 75)]
    [InlineData(4, 100)]
    public void ProgressPercentage_ShouldCalculateCorrectly(int step, int expectedPercentage)
    {
        // Act
        _viewModel.CurrentStep = step;

        // Assert
        _viewModel.ProgressPercentage.Should().Be(expectedPercentage);
    }

    #endregion
}
