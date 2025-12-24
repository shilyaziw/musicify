namespace Musicify.Core.Abstractions;

/// <summary>
/// 文件系统具体实现
/// </summary>
public class FileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public Task<string> ReadAllTextAsync(string path) => File.ReadAllTextAsync(path);

    public Task WriteAllTextAsync(string path, string content) => File.WriteAllTextAsync(path, content);

    public string[] GetDirectories(string path) => Directory.GetDirectories(path);

    public string[] GetFiles(string path, string searchPattern, SearchOption searchOption) => Directory.GetFiles(path, searchPattern, searchOption);
}
