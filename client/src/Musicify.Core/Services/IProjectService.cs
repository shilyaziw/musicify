using Musicify.Core.Models;

namespace Musicify.Core.Services;

/// <summary>
/// 项目配置服务接口
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// 创建新项目
    /// </summary>
    /// <param name="name">项目名称</param>
    /// <param name="basePath">基础路径 (可选,默认 ~/Documents/musicify)</param>
    /// <returns>创建的项目配置</returns>
    Task<ProjectConfig> CreateProjectAsync(string name, string? basePath = null);

    /// <summary>
    /// 加载现有项目
    /// </summary>
    /// <param name="projectPath">项目路径</param>
    /// <returns>项目配置,如果不存在返回 null</returns>
    Task<ProjectConfig?> LoadProjectAsync(string projectPath);

    /// <summary>
    /// 保存项目配置
    /// </summary>
    Task SaveProjectAsync(ProjectConfig config);

    /// <summary>
    /// 更新项目状态
    /// </summary>
    Task UpdateProjectStatusAsync(string projectPath, string status);

    /// <summary>
    /// 获取最近打开的项目列表
    /// </summary>
    /// <param name="limit">返回数量限制</param>
    Task<List<ProjectConfig>> GetRecentProjectsAsync(int limit = 10);

    /// <summary>
    /// 添加项目到最近列表
    /// </summary>
    Task AddToRecentProjectsAsync(string projectPath);

    /// <summary>
    /// 验证项目名称是否有效
    /// </summary>
    bool ValidateProjectName(string name);

    /// <summary>
    /// 验证项目路径是否有效（不存在且可创建）
    /// </summary>
    bool ValidateProjectPath(string projectPath);

    /// <summary>
    /// 获取项目配置文件路径
    /// </summary>
    string GetConfigFilePath(string projectPath);
    
    /// <summary>
    /// 获取项目规格文件路径
    /// </summary>
    string GetSpecFilePath(string projectPath);
}
