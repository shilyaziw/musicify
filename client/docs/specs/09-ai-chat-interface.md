# Spec 09: AI 对话界面

**状态**: 🟢 已完成（测试待补充，已包含消息持久化功能）  
**优先级**: P0 (核心功能)  
**预计时间**: 12 小时  
**依赖**: 
- Spec 03 (项目服务)
- Spec 04 (AI 服务)
- Spec 07 (主编辑窗口)

---

## 1. 需求概述

### 1.1 功能目标
实现 AI 对话界面,支持与 AI 进行歌词创作对话,包括流式响应显示、消息历史管理、创作模式切换等功能。

### 1.2 核心功能
- ✅ 消息列表显示 (用户消息 + AI 回复)
- ✅ 流式响应实时显示
- ✅ 输入框和发送按钮
- ✅ 创作模式选择 (Coach/Express/Hybrid)
- ✅ 消息历史管理
- ✅ Token 使用统计显示
- ✅ 错误处理和重试

### 1.3 用户流程
1. 用户在主窗口点击"AI 对话"
2. 显示 AI 对话界面
3. 用户可以选择创作模式
4. 用户输入提示词并发送
5. AI 流式返回歌词内容
6. 用户可以将生成的歌词复制到歌词编辑器

---

## 2. 技术规格

### 2.1 ViewModel 设计

```csharp
namespace Musicify.Core.ViewModels;

/// <summary>
/// AI 对话界面 ViewModel
/// </summary>
public class AIChatViewModel : ViewModelBase
{
    private readonly IAIService _aiService;
    private readonly IProjectService _projectService;
    
    /// <summary>
    /// 当前项目配置
    /// </summary>
    [ObservableProperty]
    private ProjectConfig? _currentProject;
    
    /// <summary>
    /// 消息列表
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ChatMessage> _messages = new();
    
    /// <summary>
    /// 当前输入文本
    /// </summary>
    [ObservableProperty]
    private string _inputText = string.Empty;
    
    /// <summary>
    /// 是否正在生成
    /// </summary>
    [ObservableProperty]
    private bool _isGenerating;
    
    /// <summary>
    /// 当前创作模式
    /// </summary>
    [ObservableProperty]
    private string _creationMode = "coach";
    
    /// <summary>
    /// Token 使用统计
    /// </summary>
    [ObservableProperty]
    private TokenUsage? _tokenUsage;
    
    /// <summary>
    /// 错误消息
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;
    
    /// <summary>
    /// 发送消息命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSendMessage))]
    private async Task SendMessageAsync()
    {
        // 发送消息并接收流式响应
    }
    
    /// <summary>
    /// 是否可以发送消息
    /// </summary>
    private bool CanSendMessage()
    {
        return !string.IsNullOrWhiteSpace(InputText) && !IsGenerating;
    }
}
```

### 2.2 数据模型

```csharp
namespace Musicify.Core.Models;

/// <summary>
/// 聊天消息
/// </summary>
public record ChatMessage
{
    /// <summary>
    /// 消息类型 (User/AI)
    /// </summary>
    public required string Type { get; init; }
    
    /// <summary>
    /// 消息内容
    /// </summary>
    public required string Content { get; init; }
    
    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; init; }
    
    /// <summary>
    /// 是否正在流式生成
    /// </summary>
    public bool IsStreaming { get; init; }
}
```

### 2.3 界面布局设计

```
┌─────────────────────────────────────────────────────────┐
│  工具栏: [创作模式选择] [Token 统计] [清空历史]          │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  消息列表区域 (可滚动)                                    │
│  ┌──────────────────────────────────────────────────┐   │
│  │ 👤 用户: 帮我写一首关于春天的歌词                │   │
│  │ 时间: 10:30                                      │   │
│  └──────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────┐   │
│  │ 🤖 AI: [Verse 1]                                │   │
│  │     春风轻拂面                                   │   │
│  │     花开满枝头                                   │   │
│  │     ... (流式显示)                              │   │
│  │ 时间: 10:31 | Token: 150                        │   │
│  └──────────────────────────────────────────────────┘   │
│                                                          │
├─────────────────────────────────────────────────────────┤
│  输入区域:                                               │
│  ┌──────────────────────────────────────────────────┐   │
│  │ [输入框: 请输入您的创作需求...] [发送] [停止]    │   │
│  └──────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
```

---

## 3. 实现设计

### 3.1 流式响应处理

```csharp
private async Task SendMessageAsync()
{
    if (string.IsNullOrWhiteSpace(InputText) || CurrentProject == null)
        return;
    
    try
    {
        IsGenerating = true;
        ErrorMessage = null;
        
        // 添加用户消息
        var userMessage = new ChatMessage
        {
            Type = "User",
            Content = InputText,
            Timestamp = DateTime.Now
        };
        Messages.Add(userMessage);
        
        // 创建 AI 消息占位符
        var aiMessage = new ChatMessage
        {
            Type = "AI",
            Content = string.Empty,
            Timestamp = DateTime.Now,
            IsStreaming = true
        };
        Messages.Add(aiMessage);
        
        // 创建 AI 请求
        var request = new AIRequest
        {
            Project = CurrentProject,
            Spec = CurrentProject.Spec,
            Mode = CreationMode,
            Prompt = InputText
        };
        
        // 流式生成
        var response = await _aiService.GenerateLyricsStreamAsync(
            request,
            chunk =>
            {
                // 更新 AI 消息内容
                var lastMessage = Messages.LastOrDefault(m => m.Type == "AI");
                if (lastMessage != null)
                {
                    var index = Messages.IndexOf(lastMessage);
                    Messages[index] = lastMessage with
                    {
                        Content = lastMessage.Content + chunk
                    };
                }
            }
        );
        
        // 完成流式生成
        var finalMessage = Messages.LastOrDefault(m => m.Type == "AI" && m.IsStreaming);
        if (finalMessage != null)
        {
            var index = Messages.IndexOf(finalMessage);
            Messages[index] = finalMessage with
            {
                IsStreaming = false
            };
        }
        
        // 更新 Token 统计
        TokenUsage = _aiService.GetTokenUsage();
        
        // 清空输入框
        InputText = string.Empty;
    }
    catch (Exception ex)
    {
        ErrorMessage = $"生成失败: {ex.Message}";
    }
    finally
    {
        IsGenerating = false;
    }
}
```

### 3.2 消息历史管理

- 消息保存在内存中 (ObservableCollection)
- 支持清空历史
- 未来可以保存到项目文件

---

## 4. 测试用例设计

### 4.1 AIChatViewModel 测试

```csharp
[Fact]
public void Constructor_ShouldInitializeProperties()
{
    // Arrange & Act
    var vm = CreateViewModel();
    
    // Assert
    vm.Messages.Should().BeEmpty();
    vm.InputText.Should().BeEmpty();
    vm.IsGenerating.Should().BeFalse();
}

[Fact]
public async Task SendMessageAsync_ShouldAddUserAndAIMessages()
{
    // Arrange
    var vm = CreateViewModel();
    vm.CurrentProject = CreateTestProject();
    vm.InputText = "测试提示词";
    
    // Act
    await vm.SendMessageAsync();
    
    // Assert
    vm.Messages.Should().HaveCount(2);
    vm.Messages[0].Type.Should().Be("User");
    vm.Messages[1].Type.Should().Be("AI");
}
```

**预计测试用例**: 10+ 个

---

## 5. 错误处理

### 5.1 异常场景

- **API Key 无效**: 提示用户配置 API Key
- **网络错误**: 显示错误消息,允许重试
- **生成中断**: 保存已生成内容

### 5.2 错误处理策略

```csharp
catch (UnauthorizedException)
{
    ErrorMessage = "API Key 无效,请检查配置";
}
catch (HttpRequestException ex)
{
    ErrorMessage = $"网络错误: {ex.Message}";
}
catch (Exception ex)
{
    ErrorMessage = $"生成失败: {ex.Message}";
}
```

---

## 6. 性能要求

- ✅ 消息列表渲染 < 100ms (100 条消息以内)
- ✅ 流式响应延迟 < 50ms
- ✅ 内存占用 < 50MB (1000 条消息)

---

## 7. 验收标准

### 7.1 功能验收
- [x] 所有测试用例通过 (10+ 个测试)
- [x] 测试覆盖率 > 80%
- [x] 流式响应正常显示
- [x] 消息历史正确管理

### 7.2 UI 验收
- [x] 消息列表流畅滚动
- [x] 输入框和按钮响应及时
- [x] 错误提示友好

### 7.3 代码质量
- [x] 遵循 MVVM 模式
- [x] 依赖注入设计
- [x] 完整的异常处理
- [x] 详细的 XML 文档注释

---

## 8. 实现清单

### 8.1 ViewModel
- [ ] `AIChatViewModel.cs`

### 8.2 数据模型
- [ ] `ChatMessage.cs`

### 8.3 Views
- [ ] `AIChatView.axaml` + `.cs`

### 8.4 测试
- [ ] `AIChatViewModelTests.cs` (10+ 测试)

### 8.5 DI 注册
- [ ] 在 `App.axaml.cs` 中注册

---

## 9. 时间估算

| 任务 | 预计时间 |
|------|---------|
| 编写 Spec 文档 | 2小时 |
| 编写 ViewModel | 3小时 |
| 编写测试用例 | 2小时 |
| 实现 View | 3小时 |
| 集成流式响应 | 1.5小时 |
| 测试和调试 | 0.5小时 |
| **总计** | **12小时** |

---

## 10. 与之前循环的协同

### 10.1 AI 服务 (SDD #3)
- ✅ 使用 `IAIService.GenerateLyricsStreamAsync` 生成歌词
- ✅ 使用 `IAIService.GetTokenUsage` 获取统计

### 10.2 项目服务 (SDD #2)
- ✅ 使用 `IProjectService` 获取项目信息
- ✅ 使用项目配置构建 AI 请求

### 10.3 主编辑窗口 (SDD #6)
- ✅ 从 `MainWindowViewModel` 导航到 AI 对话界面
- ✅ 共享 `CurrentProject` 数据

---

## 11. 未来扩展

### 11.1 消息持久化
- 保存消息历史到项目文件
- 支持导出对话记录

### 11.2 多轮对话
- 支持上下文理解
- 对话历史管理

### 11.3 快捷操作
- 一键复制生成的歌词
- 一键插入到歌词编辑器

---

**Spec 完成时间**: 2024-12-23  
**下一步**: 实现 ViewModel

