using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;
using Musicify.Core.Abstractions;
using Musicify.Core.Models;
using Musicify.Core.Services;

namespace Musicify.Core.ViewModels;

/// <summary>
/// AI 对话界面 ViewModel
/// </summary>
public class AIChatViewModel : ViewModelBase
{
    private readonly IAIService _aiService;
    private readonly IProjectService _projectService;
    private readonly IFileSystem _fileSystem;

    // 缓存JsonSerializerOptions实例，避免重复创建
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private ProjectConfig? _currentProject;
    private ObservableCollection<ChatMessage> _messages = new();
    private string _inputText = string.Empty;
    private bool _isGenerating;
    private string _creationMode = "coach";
    private TokenUsage? _tokenUsage;
    private string? _errorMessage;
    private ChatMessage? _currentStreamingMessage;
    private int _currentStreamingIndex = -1;
    private System.Action? _onContentUpdated;

    public AIChatViewModel(
        IAIService aiService,
        IProjectService projectService,
        IFileSystem fileSystem)
    {
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

        // 初始化命令
        SendMessageCommand = new AsyncRelayCommand(SendMessageAsync, CanSendMessage);
        ClearHistoryCommand = new RelayCommand(ClearHistory);
        StopGenerationCommand = new RelayCommand(StopGeneration, () => IsGenerating);

        // 监听属性变化
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(InputText) || e.PropertyName == nameof(IsGenerating))
            {
                (SendMessageCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }

            if (e.PropertyName == nameof(IsGenerating))
            {
                (StopGenerationCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        };
    }

    #region 属性

    /// <summary>
    /// 当前项目配置
    /// </summary>
    public ProjectConfig? CurrentProject
    {
        get => _currentProject;
        set => SetProperty(ref _currentProject, value);
    }

    /// <summary>
    /// 消息列表
    /// </summary>
    public ObservableCollection<ChatMessage> Messages
    {
        get => _messages;
        private set => SetProperty(ref _messages, value);
    }

    /// <summary>
    /// 当前输入文本
    /// </summary>
    public string InputText
    {
        get => _inputText;
        set => SetProperty(ref _inputText, value);
    }

    /// <summary>
    /// 是否正在生成
    /// </summary>
    public bool IsGenerating
    {
        get => _isGenerating;
        private set => SetProperty(ref _isGenerating, value);
    }

    /// <summary>
    /// 当前创作模式
    /// </summary>
    public string CreationMode
    {
        get => _creationMode;
        set => SetProperty(ref _creationMode, value);
    }

    /// <summary>
    /// 可用的创作模式列表
    /// </summary>
    public List<string> CreationModes { get; } = new() { "coach", "express", "hybrid" };

    /// <summary>
    /// Token 使用统计
    /// </summary>
    public TokenUsage? TokenUsage
    {
        get => _tokenUsage;
        private set => SetProperty(ref _tokenUsage, value);
    }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    #endregion

    #region 命令

    /// <summary>
    /// 发送消息命令
    /// </summary>
    public ICommand SendMessageCommand { get; }

    /// <summary>
    /// 清空历史命令
    /// </summary>
    public ICommand ClearHistoryCommand { get; }

    /// <summary>
    /// 停止生成命令
    /// </summary>
    public ICommand StopGenerationCommand { get; }

    #endregion

    #region 公共方法

    /// <summary>
    /// 设置当前项目
    /// </summary>
    public async Task SetProjectAsync(ProjectConfig project)
    {
        CurrentProject = project;
        await LoadChatHistoryAsync();
    }

    /// <summary>
    /// 设置内容更新回调（用于触发 UI 滚动）
    /// </summary>
    public void SetContentUpdateCallback(Action? callback)
    {
        _onContentUpdated = callback;
    }

    #endregion

    #region 命令实现

    /// <summary>
    /// 发送消息
    /// </summary>
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText) || CurrentProject == null)
        {
            return;
        }

        try
        {
            IsGenerating = true;
            ErrorMessage = null;

            // 添加用户消息
            var userMessage = new ChatMessage
            {
                Type = "User",
                Content = InputText,
                Timestamp = DateTime.Now,
                IsStreaming = false
            };
            Messages.Add(userMessage);

            // 保存消息历史（用户消息）
            await SaveChatHistoryAsync();

            // 保存输入文本
            var prompt = InputText;
            InputText = string.Empty;

            // 创建 AI 消息占位符
            var aiMessage = new ChatMessage
            {
                Type = "AI",
                Content = string.Empty,
                Timestamp = DateTime.Now,
                IsStreaming = true
            };
            Messages.Add(aiMessage);
            _currentStreamingMessage = aiMessage;
            _currentStreamingIndex = Messages.Count - 1;

            // 创建 AI 请求
            if (CurrentProject.Spec == null)
            {
                ErrorMessage = "项目规格未配置,请先完成项目设置";
                return;
            }

            var request = new AIRequest
            {
                Project = CurrentProject,
                Spec = CurrentProject.Spec,
                Mode = CreationMode,
                UserInput = prompt
            };

            // 流式生成
            var accumulatedContent = new System.Text.StringBuilder();
            var lastUpdateTime = DateTime.Now;
            var updateInterval = TimeSpan.FromMilliseconds(50); // 每 50ms 更新一次 UI

            var response = await _aiService.GenerateLyricsStreamAsync(
                request,
                chunk =>
                {
                    // 累积内容
                    accumulatedContent.Append(chunk);

                    // 节流更新：避免过于频繁的 UI 更新
                    var now = DateTime.Now;
                    if (now - lastUpdateTime >= updateInterval)
                    {
                        // 更新 AI 消息内容
                        if (_currentStreamingIndex >= 0 && _currentStreamingIndex < Messages.Count)
                        {
                            var currentMessage = Messages[_currentStreamingIndex];
                            Messages[_currentStreamingIndex] = currentMessage with
                            {
                                Content = accumulatedContent.ToString()
                            };

                            // 触发滚动回调（节流）
                            _onContentUpdated?.Invoke();
                        }

                        lastUpdateTime = now;
                    }
                },
                default
            );

            // 确保最后一次更新
            if (_currentStreamingIndex >= 0 && _currentStreamingIndex < Messages.Count)
            {
                var currentMessage = Messages[_currentStreamingIndex];
                Messages[_currentStreamingIndex] = currentMessage with
                {
                    Content = accumulatedContent.ToString()
                };
            }

            // 完成流式生成
            if (_currentStreamingIndex >= 0 && _currentStreamingIndex < Messages.Count)
            {
                var finalMessage = Messages[_currentStreamingIndex];
                Messages[_currentStreamingIndex] = finalMessage with
                {
                    IsStreaming = false,
                    TokenCount = response.Usage?.TotalTokens
                };
            }

            // 更新 Token 统计
            TokenUsage = _aiService.GetTokenUsage();

            // 触发最终滚动
            _onContentUpdated?.Invoke();

            // 保存消息历史（AI 消息完成）
            await SaveChatHistoryAsync();

            _currentStreamingMessage = null;
            _currentStreamingIndex = -1;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"生成失败: {ex.Message}";

            // 移除失败的 AI 消息
            if (_currentStreamingIndex >= 0 && _currentStreamingIndex < Messages.Count)
            {
                Messages.RemoveAt(_currentStreamingIndex);
            }

            _currentStreamingMessage = null;
            _currentStreamingIndex = -1;
        }
        finally
        {
            IsGenerating = false;
        }
    }

    /// <summary>
    /// 是否可以发送消息
    /// </summary>
    private bool CanSendMessage()
    {
        return !string.IsNullOrWhiteSpace(InputText) && !IsGenerating && CurrentProject != null;
    }

    /// <summary>
    /// 清空历史
    /// </summary>
    private async void ClearHistory()
    {
        Messages.Clear();
        ErrorMessage = null;
        await DeleteChatHistoryAsync();
    }

    /// <summary>
    /// 停止生成
    /// </summary>
    private void StopGeneration()
    {
        // TODO: 实现取消令牌机制
        IsGenerating = false;
    }

    #endregion

    #region 消息持久化

    /// <summary>
    /// 获取聊天历史文件路径
    /// </summary>
    private string GetChatHistoryFilePath()
    {
        if (CurrentProject == null || string.IsNullOrWhiteSpace(CurrentProject.ProjectPath))
        {
            throw new InvalidOperationException("项目未加载");
        }

        return Path.Combine(CurrentProject.ProjectPath, ".musicify", "chat_history.json");
    }

    /// <summary>
    /// 加载聊天历史
    /// </summary>
    private async Task LoadChatHistoryAsync()
    {
        if (CurrentProject == null || string.IsNullOrWhiteSpace(CurrentProject.ProjectPath))
        {
            return;
        }

        try
        {
            var historyPath = GetChatHistoryFilePath();

            if (!_fileSystem.FileExists(historyPath))
            {
                Messages.Clear();
                return;
            }

            var json = await _fileSystem.ReadAllTextAsync(historyPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                Messages.Clear();
                return;
            }

            var messages = JsonSerializer.Deserialize<List<ChatMessage>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (messages != null)
            {
                Messages.Clear();
                foreach (var message in messages)
                {
                    Messages.Add(message);
                }
            }
        }
        catch (Exception ex)
        {
            // 加载失败时不影响使用，只记录错误
            ErrorMessage = $"加载聊天历史失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 保存聊天历史
    /// </summary>
    private async Task SaveChatHistoryAsync()
    {
        if (CurrentProject == null || string.IsNullOrWhiteSpace(CurrentProject.ProjectPath))
        {
            return;
        }

        try
        {
            var historyPath = GetChatHistoryFilePath();
            var directory = Path.GetDirectoryName(historyPath);

            if (!string.IsNullOrEmpty(directory) && !_fileSystem.DirectoryExists(directory))
            {
                _fileSystem.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(Messages.ToList(), new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await _fileSystem.WriteAllTextAsync(historyPath, json);
        }
        catch (Exception ex)
        {
            // 保存失败时不影响使用，只记录错误
            ErrorMessage = $"保存聊天历史失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 删除聊天历史文件
    /// </summary>
    private async Task DeleteChatHistoryAsync()
    {
        if (CurrentProject == null || string.IsNullOrWhiteSpace(CurrentProject.ProjectPath))
        {
            return;
        }

        try
        {
            var historyPath = GetChatHistoryFilePath();

            if (_fileSystem.FileExists(historyPath))
            {
                // 注意：IFileSystem 可能没有 DeleteFile 方法，需要检查
                // 如果不存在，可以写入空文件或留空
                await _fileSystem.WriteAllTextAsync(historyPath, "[]");
            }
        }
        catch (Exception ex)
        {
            // 删除失败时不影响使用
            ErrorMessage = $"删除聊天历史失败: {ex.Message}";
        }
    }

    #endregion
}

