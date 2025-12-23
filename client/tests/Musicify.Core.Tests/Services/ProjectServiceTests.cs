using FluentAssertions;
using Moq;
using Musicify.Core.Abstractions;
using Musicify.Core.Models;
using Musicify.Core.Services;
using System.Text.Json;
using Xunit;

namespace Musicify.Core.Tests.Services;

/// <summary>
/// ProjectService 单元测试
/// </summary>
public class ProjectServiceTests
{
    private readonly Mock<IFileSystem> _fileSystemMock;
    private readonly string _testBasePath = "/test/musicify";

    public ProjectServiceTests()
    {
        _fileSystemMock = new Mock<IFileSystem>();
    }

    private ProjectService CreateService()
    {
        return new ProjectService(_fileSystemMock.Object);
    }

    #region CreateProject Tests

    [Fact]
    public async Task CreateProject_ShouldCreateValidProject()
    {
        // Arrange
        var service = CreateService();
        var projectName = "test-song";
        
        _fileSystemMock.Setup(f => f.DirectoryExists(It.IsAny<string>())).Returns(false);
        _fileSystemMock.Setup(f => f.CreateDirectory(It.IsAny<string>()));
        _fileSystemMock.Setup(f => f.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var config = await service.CreateProjectAsync(projectName, _testBasePath);

        // Assert
        config.Name.Should().Be(projectName);
        config.ProjectPath.Should().Contain(projectName);
        config.Status.Should().Be("draft");
        config.Created.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        
        // Verify directory creation
        _fileSystemMock.Verify(f => f.CreateDirectory(It.IsAny<string>()), Times.AtLeast(4));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateProject_WithEmptyName_ShouldThrowException(string invalidName)
    {
        var service = CreateService();

        await service.Invoking(s => s.CreateProjectAsync(invalidName!, _testBasePath))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*项目名称不能为空*");
    }

    [Theory]
    [InlineData("invalid/name")]
    [InlineData("invalid\\name")]
    [InlineData("invalid:name")]
    [InlineData("invalid*name")]
    [InlineData("invalid?name")]
    [InlineData("invalid\"name")]
    [InlineData("invalid<name")]
    [InlineData("invalid>name")]
    [InlineData("invalid|name")]
    public async Task CreateProject_WithInvalidCharacters_ShouldThrowException(string invalidName)
    {
        var service = CreateService();

        await service.Invoking(s => s.CreateProjectAsync(invalidName, _testBasePath))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*非法字符*");
    }

    [Fact]
    public async Task CreateProject_WhenProjectExists_ShouldThrowException()
    {
        var service = CreateService();
        var projectName = "existing-project";
        var projectPath = Path.Combine(_testBasePath, projectName);

        _fileSystemMock.Setup(f => f.DirectoryExists(projectPath)).Returns(true);

        await service.Invoking(s => s.CreateProjectAsync(projectName, _testBasePath))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*已存在*");
    }

    [Fact]
    public async Task CreateProject_ShouldCreateRequiredDirectories()
    {
        var service = CreateService();
        var projectName = "test-song";

        _fileSystemMock.Setup(f => f.DirectoryExists(It.IsAny<string>())).Returns(false);
        _fileSystemMock.Setup(f => f.CreateDirectory(It.IsAny<string>()));
        _fileSystemMock.Setup(f => f.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await service.CreateProjectAsync(projectName, _testBasePath);

        // Verify main project directory
        _fileSystemMock.Verify(f => f.CreateDirectory(It.Is<string>(p => p.Contains(projectName))), Times.AtLeastOnce());
        
        // Verify subdirectories (lyrics, melody, export)
        _fileSystemMock.Verify(f => f.CreateDirectory(It.Is<string>(p => p.Contains("lyrics"))), Times.Once());
        _fileSystemMock.Verify(f => f.CreateDirectory(It.Is<string>(p => p.Contains("melody"))), Times.Once());
        _fileSystemMock.Verify(f => f.CreateDirectory(It.Is<string>(p => p.Contains("export"))), Times.Once());
    }

    #endregion

    #region LoadProject Tests

    [Fact]
    public async Task LoadProject_WithValidPath_ShouldReturnConfig()
    {
        // Arrange
        var service = CreateService();
        var projectPath = "/test/musicify/test-song";
        var configPath = Path.Combine(projectPath, "project-config.json");
        
        var expectedConfig = new ProjectConfig
        {
            Name = "test-song",
            Type = "musicify-project",
            Version = "1.0.0",
            Created = DateTime.UtcNow,
            ProjectPath = projectPath,
            UpdatedAt = DateTime.UtcNow,
            Status = "draft",
            Spec = null
        };

        var json = JsonSerializer.Serialize(expectedConfig);

        _fileSystemMock.Setup(f => f.DirectoryExists(projectPath)).Returns(true);
        _fileSystemMock.Setup(f => f.FileExists(configPath)).Returns(true);
        _fileSystemMock.Setup(f => f.ReadAllTextAsync(configPath)).ReturnsAsync(json);

        // Act
        var loaded = await service.LoadProjectAsync(projectPath);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be("test-song");
        loaded.ProjectPath.Should().Be(projectPath);
    }

    [Fact]
    public async Task LoadProject_WithNonExistentDirectory_ShouldReturnNull()
    {
        var service = CreateService();
        var projectPath = "/non/existent/path";

        _fileSystemMock.Setup(f => f.DirectoryExists(projectPath)).Returns(false);

        var result = await service.LoadProjectAsync(projectPath);

        result.Should().BeNull();
    }

    [Fact]
    public async Task LoadProject_WithMissingConfigFile_ShouldReturnNull()
    {
        var service = CreateService();
        var projectPath = "/test/musicify/incomplete";
        var configPath = Path.Combine(projectPath, "project-config.json");

        _fileSystemMock.Setup(f => f.DirectoryExists(projectPath)).Returns(true);
        _fileSystemMock.Setup(f => f.FileExists(configPath)).Returns(false);

        var result = await service.LoadProjectAsync(projectPath);

        result.Should().BeNull();
    }

    [Fact]
    public async Task LoadProject_WithInvalidJson_ShouldThrowException()
    {
        var service = CreateService();
        var projectPath = "/test/musicify/corrupted";
        var configPath = Path.Combine(projectPath, "project-config.json");

        _fileSystemMock.Setup(f => f.DirectoryExists(projectPath)).Returns(true);
        _fileSystemMock.Setup(f => f.FileExists(configPath)).Returns(true);
        _fileSystemMock.Setup(f => f.ReadAllTextAsync(configPath)).ReturnsAsync("{ invalid json }");

        await service.Invoking(s => s.LoadProjectAsync(projectPath))
            .Should().ThrowAsync<JsonException>();
    }

    #endregion

    #region SaveProject Tests

    [Fact]
    public async Task SaveProject_ShouldWriteConfigFile()
    {
        var service = CreateService();
        var config = new ProjectConfig
        {
            Name = "test-song",
            Type = "musicify-project",
            Version = "1.0.0",
            Created = DateTime.UtcNow,
            ProjectPath = "/test/musicify/test-song",
            UpdatedAt = DateTime.UtcNow,
            Status = "draft",
            Spec = null
        };

        _fileSystemMock.Setup(f => f.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await service.SaveProjectAsync(config);

        _fileSystemMock.Verify(
            f => f.WriteAllTextAsync(
                It.Is<string>(p => p.EndsWith("project-config.json")),
                It.Is<string>(json => json.Contains("test-song"))
            ),
            Times.Once()
        );
    }

    [Fact]
    public async Task SaveProject_ShouldUpdateTimestamp()
    {
        var service = CreateService();
        var oldTime = DateTime.UtcNow.AddMinutes(-10);
        var config = new ProjectConfig(
            ProjectName: "test-song",
            ProjectPath: "/test/musicify/test-song",
            CreatedAt: oldTime,
            UpdatedAt: oldTime,
            Status: "draft",
            Spec: null
        );

        string? savedJson = null;
        _fileSystemMock.Setup(f => f.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((_, json) => savedJson = json)
            .Returns(Task.CompletedTask);

        await service.SaveProjectAsync(config);

        savedJson.Should().NotBeNull();
        var savedConfig = JsonSerializer.Deserialize<ProjectConfig>(savedJson!);
        savedConfig!.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region UpdateProjectStatus Tests

    [Fact]
    public async Task UpdateProjectStatus_ShouldUpdateConfigAndSave()
    {
        var service = CreateService();
        var projectPath = "/test/musicify/test-song";
        var configPath = Path.Combine(projectPath, "project-config.json");
        
        var originalConfig = new ProjectConfig(
            ProjectName: "test-song",
            ProjectPath: projectPath,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow,
            Status: "draft",
            Spec: null
        );

        _fileSystemMock.Setup(f => f.DirectoryExists(projectPath)).Returns(true);
        _fileSystemMock.Setup(f => f.FileExists(configPath)).Returns(true);
        _fileSystemMock.Setup(f => f.ReadAllTextAsync(configPath))
            .ReturnsAsync(JsonSerializer.Serialize(originalConfig));
        
        string? savedJson = null;
        _fileSystemMock.Setup(f => f.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((_, json) => savedJson = json)
            .Returns(Task.CompletedTask);

        await service.UpdateProjectStatusAsync(projectPath, "in_progress");

        savedJson.Should().NotBeNull();
        var updatedConfig = JsonSerializer.Deserialize<ProjectConfig>(savedJson!);
        updatedConfig!.Status.Should().Be("in_progress");
    }

    #endregion

    #region RecentProjects Tests

    [Fact]
    public async Task GetRecentProjects_WithNoProjects_ShouldReturnEmptyList()
    {
        var service = CreateService();
        
        _fileSystemMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        var result = await service.GetRecentProjectsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRecentProjects_ShouldReturnOrderedList()
    {
        var service = CreateService();
        var recentData = new
        {
            projects = new[]
            {
                new { projectName = "project2", projectPath = "/path/2", lastOpened = DateTime.UtcNow, status = "draft" },
                new { projectName = "project1", projectPath = "/path/1", lastOpened = DateTime.UtcNow.AddMinutes(-10), status = "draft" }
            }
        };

        _fileSystemMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _fileSystemMock.Setup(f => f.ReadAllTextAsync(It.IsAny<string>()))
            .ReturnsAsync(JsonSerializer.Serialize(recentData));

        var result = await service.GetRecentProjectsAsync();

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("project2"); // Most recent first
    }

    [Fact]
    public async Task GetRecentProjects_ShouldRespectLimit()
    {
        var service = CreateService();
        var projects = Enumerable.Range(1, 15)
            .Select(i => new 
            { 
                projectName = $"project{i}", 
                projectPath = $"/path/{i}", 
                lastOpened = DateTime.UtcNow.AddMinutes(-i),
                status = "draft"
            })
            .ToArray();

        var recentData = new { projects };

        _fileSystemMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
        _fileSystemMock.Setup(f => f.ReadAllTextAsync(It.IsAny<string>()))
            .ReturnsAsync(JsonSerializer.Serialize(recentData));

        var result = await service.GetRecentProjectsAsync(limit: 5);

        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task AddToRecentProjects_ShouldNotCreateDuplicates()
    {
        var service = CreateService();
        var projectPath = "/test/musicify/test-song";
        var configPath = Path.Combine(projectPath, "project-config.json");
        
        var config = new ProjectConfig
        {
            Name = "test-song",
            Type = "musicify-project",
            Version = "1.0.0",
            Created = DateTime.UtcNow,
            ProjectPath = projectPath,
            UpdatedAt = DateTime.UtcNow,
            Status = "draft",
            Spec = null
        };

        _fileSystemMock.Setup(f => f.DirectoryExists(projectPath)).Returns(true);
        _fileSystemMock.Setup(f => f.FileExists(configPath)).Returns(true);
        _fileSystemMock.Setup(f => f.ReadAllTextAsync(configPath))
            .ReturnsAsync(JsonSerializer.Serialize(config));

        var existingData = new
        {
            projects = new[]
            {
                new { projectName = "test-song", projectPath, lastOpened = DateTime.UtcNow.AddHours(-1), status = "draft" }
            }
        };

        _fileSystemMock.Setup(f => f.FileExists(It.Is<string>(p => p.Contains("recent-projects"))))
            .Returns(true);
        _fileSystemMock.Setup(f => f.ReadAllTextAsync(It.Is<string>(p => p.Contains("recent-projects"))))
            .ReturnsAsync(JsonSerializer.Serialize(existingData));

        string? savedJson = null;
        _fileSystemMock.Setup(f => f.WriteAllTextAsync(
            It.Is<string>(p => p.Contains("recent-projects")), 
            It.IsAny<string>()))
            .Callback<string, string>((_, json) => savedJson = json)
            .Returns(Task.CompletedTask);

        await service.AddToRecentProjectsAsync(projectPath);

        // Verify only one entry exists
        savedJson.Should().NotBeNull();
        var saved = JsonSerializer.Deserialize<JsonElement>(savedJson!);
        saved.GetProperty("projects").GetArrayLength().Should().Be(1);
    }

    #endregion

    #region Validation Tests

    [Theory]
    [InlineData("valid-project", true)]
    [InlineData("another_project", true)]
    [InlineData("project with spaces", true)]
    [InlineData("项目中文名", true)]
    [InlineData("invalid/project", false)]
    [InlineData("invalid\\project", false)]
    [InlineData("invalid:project", false)]
    [InlineData("invalid*project", false)]
    [InlineData("invalid?project", false)]
    [InlineData("invalid\"project", false)]
    [InlineData("invalid<project", false)]
    [InlineData("invalid>project", false)]
    [InlineData("invalid|project", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void ValidateProjectName_ShouldReturnExpectedResult(string name, bool expected)
    {
        var service = CreateService();
        
        var isValid = service.ValidateProjectName(name);
        
        isValid.Should().Be(expected);
    }

    [Fact]
    public void GetConfigFilePath_ShouldReturnCorrectPath()
    {
        var service = CreateService();
        var projectPath = "/test/musicify/my-project";

        var configPath = service.GetConfigFilePath(projectPath);

        configPath.Should().Be(Path.Combine(projectPath, "project-config.json"));
    }

    #endregion
}
