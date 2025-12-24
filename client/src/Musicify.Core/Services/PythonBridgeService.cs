using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Musicify.Core.Abstractions;

namespace Musicify.Core.Services;

/// <summary>
/// Python 脚本桥接服务实现
/// </summary>
public class PythonBridgeService : IPythonBridgeService
{
    private readonly IFileSystem _fileSystem;
    private PythonEnvironmentInfo? _cachedEnvironmentInfo;
    private readonly object _lockObject = new();

    // 必需的 Python 包
    private static readonly string[] RequiredPackages = { "mido", "music21", "numpy" };

    public PythonBridgeService(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public async Task<PythonEnvironmentInfo?> CheckPythonEnvironmentAsync(CancellationToken cancellationToken = default)
    {
        // 使用缓存（避免频繁检查）
        if (_cachedEnvironmentInfo != null)
        {
            return _cachedEnvironmentInfo;
        }

        // 使用异步锁避免阻塞主线程
        return await Task.Run(() =>
        {
            lock (_lockObject)
            {
                if (_cachedEnvironmentInfo != null)
                {
                    return _cachedEnvironmentInfo;
                }

                return _cachedEnvironmentInfo = CheckPythonEnvironmentSync();
            }
        }, cancellationToken);
    }

    private PythonEnvironmentInfo? CheckPythonEnvironmentSync()
    {
        // 尝试查找 Python 可执行文件
        var pythonPaths = new[]
        {
            "python3",
            "python",
            "/usr/bin/python3",
            "/usr/local/bin/python3",
            "C:\\Python3\\python.exe",
            "C:\\Python\\python.exe"
        };

        string? pythonPath = null;
        string? version = null;

        foreach (var path in pythonPaths)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processStartInfo);
                if (process != null)
                {
                    process.WaitForExit(5000); // 5 秒超时
                    if (process.ExitCode == 0)
                    {
                        var output = process.StandardOutput.ReadToEnd().Trim();
                        if (output.StartsWith("Python", StringComparison.OrdinalIgnoreCase))
                        {
                            pythonPath = path;
                            version = output;
                            break;
                        }
                    }
                }
            }
            catch
            {
                // 继续尝试下一个路径
            }
        }

        if (pythonPath == null)
        {
            return new PythonEnvironmentInfo
            {
                PythonPath = string.Empty,
                Version = "未找到",
                IsAvailable = false,
                MissingPackages = RequiredPackages.ToList()
            };
        }

        // 检查已安装的包
        var installedPackages = new List<string>();
        var missingPackages = new List<string>();

        foreach (var package in RequiredPackages)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = pythonPath,
                    Arguments = $"-c \"import {package}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processStartInfo);
                if (process != null)
                {
                    process.WaitForExit(5000);
                    if (process.ExitCode == 0)
                    {
                        installedPackages.Add(package);
                    }
                    else
                    {
                        missingPackages.Add(package);
                    }
                }
            }
            catch
            {
                missingPackages.Add(package);
            }
        }

        return new PythonEnvironmentInfo
        {
            PythonPath = pythonPath,
            Version = version ?? "未知",
            IsAvailable = missingPackages.Count == 0,
            InstalledPackages = installedPackages,
            MissingPackages = missingPackages
        };
    }

    public async Task<string> AnalyzeMidiAsync(string midiFilePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(midiFilePath))
        {
            throw new ArgumentException("MIDI 文件路径不能为空", nameof(midiFilePath));
        }

        if (!_fileSystem.FileExists(midiFilePath))
        {
            throw new FileNotFoundException($"MIDI 文件不存在: {midiFilePath}");
        }

        var scriptPath = GetPythonScriptPath("midi_analyzer.py");
        if (scriptPath == null || !_fileSystem.FileExists(scriptPath))
        {
            throw new FileNotFoundException("未找到 midi_analyzer.py 脚本");
        }

        return await ExecutePythonScriptAsync(scriptPath, new[] { midiFilePath }, cancellationToken);
    }

    public async Task<string> ConvertAudioToMidiAsync(string audioFilePath, string? outputDirectory = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(audioFilePath))
        {
            throw new ArgumentException("音频文件路径不能为空", nameof(audioFilePath));
        }

        if (!_fileSystem.FileExists(audioFilePath))
        {
            throw new FileNotFoundException($"音频文件不存在: {audioFilePath}");
        }

        var scriptPath = GetPythonScriptPath("audio_to_midi.py");
        if (scriptPath == null || !_fileSystem.FileExists(scriptPath))
        {
            throw new FileNotFoundException("未找到 audio_to_midi.py 脚本");
        }

        var arguments = new List<string> { audioFilePath };
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            arguments.Add(outputDirectory);
        }

        return await ExecutePythonScriptAsync(scriptPath, arguments, cancellationToken);
    }

    public async Task<string> ExecutePythonScriptAsync(string scriptPath, IEnumerable<string>? arguments = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scriptPath))
        {
            throw new ArgumentException("脚本路径不能为空", nameof(scriptPath));
        }

        if (!_fileSystem.FileExists(scriptPath))
        {
            throw new FileNotFoundException($"Python 脚本不存在: {scriptPath}");
        }

        // 检查 Python 环境
        var envInfo = await CheckPythonEnvironmentAsync(cancellationToken);
        if (envInfo == null || !envInfo.IsAvailable)
        {
            var missingPackages = envInfo?.MissingPackages ?? new List<string>();
            throw new InvalidOperationException(
                $"Python 环境不可用。缺失的包: {string.Join(", ", missingPackages)}");
        }

        // 构建参数
        var args = new StringBuilder();
        args.Append($"\"{scriptPath}\"");

        if (arguments != null)
        {
            foreach (var arg in arguments)
            {
                args.Append(" ");
                args.Append($"\"{arg}\"");
            }
        }

        // 执行 Python 脚本
        var processStartInfo = new ProcessStartInfo
        {
            FileName = envInfo.PythonPath,
            Arguments = args.ToString(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = processStartInfo };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
            }
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // 等待完成（带取消支持）
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                var errorOutput = errorBuilder.ToString();
                throw new InvalidOperationException(
                    $"Python 脚本执行失败 (退出码: {process.ExitCode}): {errorOutput}");
            }

            var output = outputBuilder.ToString().Trim();

            // 尝试解析 JSON 输出（如果脚本输出 JSON）
            if (output.StartsWith("{") || output.StartsWith("["))
            {
                try
                {
                    JsonDocument.Parse(output); // 验证 JSON 格式
                }
                catch
                {
                    // 不是有效的 JSON，返回原始输出
                }
            }

            return output;
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill();
            }
            catch
            {
                // 忽略
            }
            throw;
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            throw new InvalidOperationException($"执行 Python 脚本时发生错误: {ex.Message}", ex);
        }
    }

    public string? GetPythonScriptPath(string scriptName)
    {
        if (string.IsNullOrWhiteSpace(scriptName))
        {
            return null;
        }

        // 尝试多个可能的路径
        var possiblePaths = new List<string>();

        // 1. 相对于项目根目录的 skills/scripts 目录
        var projectRoot = FindProjectRoot();
        if (projectRoot != null)
        {
            possiblePaths.Add(Path.Combine(projectRoot, "skills", "scripts", scriptName));
        }

        // 2. 相对于当前工作目录
        var currentDir = Environment.CurrentDirectory;
        possiblePaths.Add(Path.Combine(currentDir, "skills", "scripts", scriptName));
        possiblePaths.Add(Path.Combine(currentDir, "..", "skills", "scripts", scriptName));
        possiblePaths.Add(Path.Combine(currentDir, "..", "..", "skills", "scripts", scriptName));

        // 3. 环境变量指定的路径
        var scriptsPath = Environment.GetEnvironmentVariable("MUSICIFY_PYTHON_SCRIPTS");
        if (!string.IsNullOrWhiteSpace(scriptsPath))
        {
            possiblePaths.Add(Path.Combine(scriptsPath, scriptName));
        }

        // 查找第一个存在的文件
        foreach (var path in possiblePaths)
        {
            if (_fileSystem.FileExists(path))
            {
                return Path.GetFullPath(path);
            }
        }

        return null;
    }

    private string? FindProjectRoot()
    {
        var currentDir = new DirectoryInfo(Environment.CurrentDirectory);
        var maxDepth = 10;
        var depth = 0;

        while (currentDir != null && depth < maxDepth)
        {
            // 查找包含 skills/scripts 目录的目录
            var skillsPath = Path.Combine(currentDir.FullName, "skills", "scripts");
            if (_fileSystem.DirectoryExists(skillsPath))
            {
                return currentDir.FullName;
            }

            currentDir = currentDir.Parent;
            depth++;
        }

        return null;
    }
}

