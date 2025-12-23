namespace Musicify.Core.Abstractions;

/// <summary>
/// 文件系统抽象接口 (用于依赖注入和单元测试)
/// </summary>
public interface IFileSystem
{
    /// <summary>
    /// 检查文件是否存在
    /// </summary>
    bool FileExists(string path);
    
    /// <summary>
    /// 检查目录是否存在
    /// </summary>
    bool DirectoryExists(string path);
    
    /// <summary>
    /// 创建目录
    /// </summary>
    void CreateDirectory(string path);
    
    /// <summary>
    /// 异步读取文件内容
    /// </summary>
    Task<string> ReadAllTextAsync(string path);
    
    /// <summary>
    /// 异步写入文件内容
    /// </summary>
    Task WriteAllTextAsync(string path, string content);
    
    /// <summary>
    /// 获取指定目录下的所有子目录
    /// </summary>
    string[] GetDirectories(string path);
}

/// <summary>
/// 默认文件系统实现 (System.IO 包装器)
/// </summary>
public class DefaultFileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);
    
    public bool DirectoryExists(string path) => Directory.Exists(path);
    
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);
    
    public Task<string> ReadAllTextAsync(string path) => File.ReadAllTextAsync(path);
    
    public Task WriteAllTextAsync(string path, string content) => File.WriteAllTextAsync(path, content);
    
    public string[] GetDirectories(string path) => Directory.GetDirectories(path);
}
