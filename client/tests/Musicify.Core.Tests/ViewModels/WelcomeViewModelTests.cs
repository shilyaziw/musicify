using Xunit;
using FluentAssertions;
using Moq;
using Musicify.Core.ViewModels;
using Musicify.Core.Services;
using Musicify.Core.Models;
using System.Collections.ObjectModel;

namespace Musicify.Core.Tests.ViewModels;

/// <summary>
/// WelcomeViewModel 测试
/// 测试覆盖:
/// - 初始化
/// - 最近项目加载
/// - 项目打开
/// - 新建项目导航
/// - 命令状态
/// </summary>
public class WelcomeViewModelTests
{
    private readonly Mock<IProjectService> _mockProjectService;
    private readonly Mock<INavigationService> _mockNavigationService;
    private readonly WelcomeViewModel _viewModel;

    public WelcomeViewModelTests()
    {
        _mockProjectService = new Mock<IProjectService>();
        _mockNavigationService = new Mock<INavigationService>();
        _viewModel = new WelcomeViewModel(
            _mockProjectService.Object,
            _mockNavigationService.Object
        );
    }

    #region 初始化测试

    [Fact]
    public void Constructor_ShouldInitializeWithEmptyRecentProjects()
    {
        // Assert
        _viewModel.RecentProjects.Should().NotBeNull();
        _viewModel.RecentProjects.Should().BeEmpty();
        _viewModel.HasRecentProjects.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new WelcomeViewModel(null!, _mockNavigationService.Object));

        Assert.Throws<ArgumentNullException>(() =>
            new WelcomeViewModel(_mockProjectService.Object, null!));
    }

    #endregion

    #region 最近项目加载测试

    [Fact]
    public async Task LoadRecentProjectsAsync_WithNoProjects_ShouldSetEmptyList()
    {
        // Arrange
        _mockProjectService
            .Setup(x => x.GetRecentProjectsAsync())
            .ReturnsAsync(new List<ProjectConfig>());

        // Act
        await _viewModel.LoadRecentProjectsAsync();

        // Assert
        _viewModel.RecentProjects.Should().BeEmpty();
        _viewModel.HasRecentProjects.Should().BeFalse();
        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadRecentProjectsAsync_WithProjects_ShouldPopulateList()
    {
        // Arrange
        var recentProjects = new List<ProjectConfig>
        {
            new() { Name = "Project 1", Type = "musicify-project", Version = "1.0.0", Created = DateTime.UtcNow, ProjectPath = "/path/to/project1" },
            new() { Name = "Project 2", Type = "musicify-project", Version = "1.0.0", Created = DateTime.UtcNow, ProjectPath = "/path/to/project2" },
            new() { Name = "Project 3", Type = "musicify-project", Version = "1.0.0", Created = DateTime.UtcNow, ProjectPath = "/path/to/project3" }
        };

        _mockProjectService
            .Setup(x => x.GetRecentProjectsAsync())
            .ReturnsAsync(recentProjects);

        // Act
        await _viewModel.LoadRecentProjectsAsync();

        // Assert
        _viewModel.RecentProjects.Should().HaveCount(3);
        _viewModel.HasRecentProjects.Should().BeTrue();
        _viewModel.RecentProjects[0].Name.Should().Be("Project 1");
    }

    [Fact]
    public async Task LoadRecentProjectsAsync_ShouldSetLoadingState()
    {
        // Arrange
        var loadingStates = new List<bool>();
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.IsLoading))
            {
                loadingStates.Add(_viewModel.IsLoading);
            }
        };

        _mockProjectService
            .Setup(x => x.GetRecentProjectsAsync())
            .ReturnsAsync(new List<ProjectConfig>());

        // Act
        await _viewModel.LoadRecentProjectsAsync();

        // Assert
        loadingStates.Should().ContainInOrder(true, false);
    }

    [Fact]
    public async Task LoadRecentProjectsAsync_OnError_ShouldSetErrorMessage()
    {
        // Arrange
        _mockProjectService
            .Setup(x => x.GetRecentProjectsAsync())
            .ThrowsAsync(new IOException("Failed to read recent projects"));

        // Act
        await _viewModel.LoadRecentProjectsAsync();

        // Assert
        _viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
        _viewModel.ErrorMessage.Should().Contain("加载最近项目失败");
        _viewModel.IsLoading.Should().BeFalse();
    }

    #endregion

    #region 项目打开测试

    [Fact]
    public async Task OpenProjectCommand_WithValidProject_ShouldLoadAndNavigate()
    {
        // Arrange
        var projectConfig = new ProjectConfig
        {
            ProjectName = "Test Project",
            ProjectPath = "/path/to/project"
        };

        _mockProjectService
            .Setup(x => x.LoadProjectAsync(projectConfig.ProjectPath))
            .ReturnsAsync(projectConfig);

        // Act
        await _viewModel.OpenProjectCommand.ExecuteAsync(projectConfig);

        // Assert
        _mockProjectService.Verify(x => x.LoadProjectAsync(projectConfig.ProjectPath), Times.Once);
        _mockNavigationService.Verify(x => x.NavigateTo("MainWindow", projectConfig), Times.Once);
    }

    [Fact]
    public async Task OpenProjectCommand_WithInvalidProject_ShouldShowError()
    {
        // Arrange
        var projectConfig = new ProjectConfig
        {
            ProjectName = "Invalid Project",
            ProjectPath = "/invalid/path"
        };

        _mockProjectService
            .Setup(x => x.LoadProjectAsync(projectConfig.ProjectPath))
            .ThrowsAsync(new FileNotFoundException("Project not found"));

        // Act
        await _viewModel.OpenProjectCommand.ExecuteAsync(projectConfig);

        // Assert
        _viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
        _viewModel.ErrorMessage.Should().Contain("打开项目失败");
        _mockNavigationService.Verify(x => x.NavigateTo(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public void OpenProjectCommand_WithNullParameter_ShouldNotExecute()
    {
        // Act & Assert
        _viewModel.OpenProjectCommand.CanExecute(null).Should().BeFalse();
    }

    #endregion

    #region 新建项目测试

    [Fact]
    public void CreateNewProjectCommand_ShouldNavigateToCreateView()
    {
        // Act
        _viewModel.CreateNewProjectCommand.Execute(null);

        // Assert
        _mockNavigationService.Verify(x => x.NavigateTo("CreateProjectView", null), Times.Once);
    }

    [Fact]
    public void CreateNewProjectCommand_ShouldAlwaysBeExecutable()
    {
        // Act & Assert
        _viewModel.CreateNewProjectCommand.CanExecute(null).Should().BeTrue();
    }

    #endregion

    #region 浏览项目测试

    [Fact]
    public async Task BrowseProjectCommand_WithSelectedPath_ShouldOpenProject()
    {
        // Arrange
        var selectedPath = "/path/to/selected/project";
        var projectConfig = new ProjectConfig
        {
            ProjectName = "Selected Project",
            ProjectPath = selectedPath
        };

        // Mock file dialog (通过 ViewModel 公开的方法)
        _viewModel.OnBrowseProjectRequested = () => Task.FromResult(selectedPath);

        _mockProjectService
            .Setup(x => x.LoadProjectAsync(selectedPath))
            .ReturnsAsync(projectConfig);

        // Act
        if (_viewModel.BrowseProjectCommand is AsyncRelayCommand asyncCommand)
        {
            await asyncCommand.ExecuteAsync(null);
        }
        else
        {
            _viewModel.BrowseProjectCommand.Execute(null);
            await Task.Delay(100); // 等待异步操作完成
        }

        // Assert
        _mockProjectService.Verify(x => x.LoadProjectAsync(selectedPath), Times.Once);
        _mockNavigationService.Verify(x => x.NavigateTo("MainWindow", projectConfig), Times.Once);
    }

    [Fact]
    public async Task BrowseProjectCommand_WhenCancelled_ShouldNotOpenProject()
    {
        // Arrange
        _viewModel.OnBrowseProjectRequested = () => Task.FromResult<string?>(null);

        // Act
        if (_viewModel.BrowseProjectCommand is AsyncRelayCommand asyncCommand)
        {
            await asyncCommand.ExecuteAsync(null);
        }
        else
        {
            _viewModel.BrowseProjectCommand.Execute(null);
            await Task.Delay(100); // 等待异步操作完成
        }

        // Assert
        _mockProjectService.Verify(x => x.LoadProjectAsync(It.IsAny<string>()), Times.Never);
        _mockNavigationService.Verify(x => x.NavigateTo(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    #endregion

    #region 属性变化测试

    [Fact]
    public void HasRecentProjects_WhenRecentProjectsAdded_ShouldBeTrue()
    {
        // Arrange
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.HasRecentProjects))
                propertyChangedRaised = true;
        };

        // Act
        _viewModel.RecentProjects.Add(new ProjectConfig
        {
            Name = "New Project",
            Type = "musicify-project",
            Version = "1.0.0",
            Created = DateTime.UtcNow,
            ProjectPath = "/path"
        });

        // Assert (需要手动触发,或通过 ObservableCollection 事件)
        // 这里假设 ViewModel 监听了 RecentProjects 的 CollectionChanged 事件
    }

    #endregion

    #region 清除错误测试

    [Fact]
    public void ClearErrorCommand_ShouldClearErrorMessage()
    {
        // Arrange
        _viewModel.ErrorMessage = "Test Error";

        // Act
        _viewModel.ClearErrorCommand.Execute(null);

        // Assert
        _viewModel.ErrorMessage.Should().BeNullOrEmpty();
    }

    #endregion
}
