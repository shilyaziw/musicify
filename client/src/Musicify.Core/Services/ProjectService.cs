using System.Text.Json;
using System.Text.RegularExpressions;
using Musicify.Core.Abstractions;
using Musicify.Core.Models;

namespace Musicify.Core.Services;

/// <summary>
/// 项目配置服务实现
/// </summary>
public class ProjectService : IProjectService
{
    private const string ConfigFileName = "project-config.json";
    private const string SpecFileName = "spec.json";
    private const string RecentProjectsFile = "recent-projects.json";
    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

    private readonly IFileSystem _fileSystem;
    private readonly string _defaultBasePath;
    private readonly string _recentProjectsPath;

    public ProjectService(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;

        // 默认项目基础路径: ~/Documents/musicify
        _defaultBasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "musicify"
        );

        // 最近项目列表路径: ~/.config/Musicify/recent-projects.json (macOS/Linux)
        // 或 %APPDATA%/Musicify/recent-projects.json (Windows)
        _recentProjectsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Musicify",
            RecentProjectsFile
        );
    }

    public async Task<ProjectConfig> CreateProjectAsync(string name, string? basePath = null)
    {
        // 验证项目名称
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("项目名称不能为空", nameof(name));
        }

        if (!ValidateProjectName(name))
        {
            throw new ArgumentException("项目名称包含非法字符", nameof(name));
        }

        // 确定项目路径
        var projectPath = basePath != null ? basePath : Path.Combine(_defaultBasePath, name);

        // 检查项目是否已存在
        if (_fileSystem.DirectoryExists(projectPath))
        {
            throw new InvalidOperationException($"项目已存在: {projectPath}");
        }

        // 创建项目目录结构
        _fileSystem.CreateDirectory(projectPath);
        _fileSystem.CreateDirectory(Path.Combine(projectPath, "lyrics"));
        _fileSystem.CreateDirectory(Path.Combine(projectPath, "melody"));
        _fileSystem.CreateDirectory(Path.Combine(projectPath, "export"));

        // 创建项目配置
        var now = DateTime.UtcNow;
        var config = new ProjectConfig
        {
            Name = name,
            Type = "musicify-project",
            Version = "1.0.0",
            Created = now,
            ProjectPath = projectPath,
            UpdatedAt = now,
            Status = "draft",
            Spec = null
        };

        // 保存配置文件
        await SaveProjectAsync(config);

        // 添加到最近项目
        await AddToRecentProjectsAsync(projectPath);

        return config;
    }

    public async Task<ProjectConfig?> LoadProjectAsync(string projectPath)
    {
        // 检查项目目录是否存在
        if (!_fileSystem.DirectoryExists(projectPath))
        {
            return null;
        }

        var configPath = GetConfigFilePath(projectPath);

        // 检查配置文件是否存在
        if (!_fileSystem.FileExists(configPath))
        {
            return null;
        }

        try
        {
            var json = await _fileSystem.ReadAllTextAsync(configPath);

            var config = JsonSerializer.Deserialize<ProjectConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // 手动设置项目路径，因为它被标记为[JsonIgnore]
            if (config != null)
            {
                config = config with { ProjectPath = projectPath };
                
                // 尝试加载 spec.json 文件
                var specPath = GetSpecFilePath(projectPath);
                if (_fileSystem.FileExists(specPath))
                {
                    var specJson = await _fileSystem.ReadAllTextAsync(specPath);
                    var spec = JsonSerializer.Deserialize<SongSpec>(specJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (spec != null)
                    {
                        config = config with { Spec = spec };
                    }
                }
            }

            return config;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task SaveProjectAsync(ProjectConfig config)
    {
        // 更新时间戳
        var updatedConfig = config with { UpdatedAt = DateTime.UtcNow };

        var configPath = GetConfigFilePath(config.ProjectPath ?? throw new ArgumentException("ProjectPath 不能为空", nameof(config)));
        var json = JsonSerializer.Serialize(updatedConfig, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await _fileSystem.WriteAllTextAsync(configPath, json);
        
        // 单独保存 SongSpec 到 spec.json 文件
        if (config.Spec != null)
        {
            var specPath = GetSpecFilePath(config.ProjectPath);
            var specJson = JsonSerializer.Serialize(config.Spec, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await _fileSystem.WriteAllTextAsync(specPath, specJson);
        }
    }

    public async Task UpdateProjectStatusAsync(string projectPath, string status)
    {
        var config = await LoadProjectAsync(projectPath);
        if (config == null)
        {
            throw new InvalidOperationException($"项目不存在: {projectPath}");
        }

        var updatedConfig = config with { Status = status };
        await SaveProjectAsync(updatedConfig);
    }

    public async Task<List<ProjectConfig>> GetRecentProjectsAsync(int limit = 10)
    {
        if (!_fileSystem.FileExists(_recentProjectsPath))
        {
            return new List<ProjectConfig>();
        }

        try
        {
            var json = await _fileSystem.ReadAllTextAsync(_recentProjectsPath);
            var data = JsonSerializer.Deserialize<RecentProjectsData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (data?.Projects == null)
            {
                return new List<ProjectConfig>();
            }

            // 按最后打开时间倒序排序并限制数量
            return data.Projects
                .OrderByDescending(p => p.LastOpened)
                .Take(limit)
                .Select(p => new ProjectConfig
                {
                    Name = p.ProjectName,
                    Type = "musicify-project",
                    Version = "1.0.0",
                    Created = default,
                    ProjectPath = p.ProjectPath,
                    UpdatedAt = p.LastOpened,
                    Status = p.Status,
                    Spec = null
                })
                .ToList();
        }
        catch (JsonException)
        {
            return new List<ProjectConfig>();
        }
    }

    public async Task AddToRecentProjectsAsync(string projectPath)
    {
        var config = await LoadProjectAsync(projectPath);
        if (config == null)
        {
            throw new InvalidOperationException($"项目不存在: {projectPath}");
        }

        // 读取现有最近项目列表
        var recentProjects = new List<RecentProjectItem>();

        if (_fileSystem.FileExists(_recentProjectsPath))
        {
            try
            {
                var json = await _fileSystem.ReadAllTextAsync(_recentProjectsPath);
                var data = JsonSerializer.Deserialize<RecentProjectsData>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (data?.Projects != null)
                {
                    recentProjects = data.Projects.ToList();
                }
            }
            catch (JsonException)
            {
                // 忽略解析错误,重新创建列表
            }
        }

        // 移除已存在的相同项目
        recentProjects.RemoveAll(p => p.ProjectPath == projectPath);

        // 添加到列表开头
        recentProjects.Insert(0, new RecentProjectItem(
            ProjectName: config.Name,
            ProjectPath: config.ProjectPath ?? projectPath,
            LastOpened: DateTime.UtcNow,
            Status: config.Status ?? "draft"
        ));

        // 保留最多 20 个项目
        if (recentProjects.Count > 20)
        {
            recentProjects = recentProjects.Take(20).ToList();
        }

        // 保存
        var recentData = new RecentProjectsData(recentProjects);
        var outputJson = JsonSerializer.Serialize(recentData, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // 确保目录存在
        var directory = Path.GetDirectoryName(_recentProjectsPath);
        if (!string.IsNullOrEmpty(directory) && !_fileSystem.DirectoryExists(directory))
        {
            _fileSystem.CreateDirectory(directory);
        }

        await _fileSystem.WriteAllTextAsync(_recentProjectsPath, outputJson);
    }

    public bool ValidateProjectName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        // 检查是否包含非法字符
        return !name.Any(c => InvalidFileNameChars.Contains(c));
    }

    public bool ValidateProjectPath(string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            return false;
        }

        // 检查路径是否已存在
        if (_fileSystem.DirectoryExists(projectPath))
        {
            return false;
        }

        // 检查父目录是否存在
        var parentDir = Path.GetDirectoryName(projectPath);
        if (!string.IsNullOrEmpty(parentDir) && !_fileSystem.DirectoryExists(parentDir))
        {
            return false;
        }

        return true;
    }

    public string GetConfigFilePath(string projectPath)
    {
        return Path.Combine(projectPath, ConfigFileName);
    }
    
    /// <summary>
    /// 获取项目规格文件路径
    /// </summary>
    /// <param name="projectPath">项目路径</param>
    /// <returns>规格文件路径</returns>
    public string GetSpecFilePath(string projectPath)
    {
        return Path.Combine(projectPath, SpecFileName);
    }
}

/// <summary>
/// 最近项目数据模型
/// </summary>
internal record RecentProjectsData(List<RecentProjectItem> Projects);

/// <summary>
/// 最近项目项
/// </summary>
internal record RecentProjectItem(
    string ProjectName,
    string ProjectPath,
    DateTime LastOpened,
    string Status
);
