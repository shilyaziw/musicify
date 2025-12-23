# Musicify Desktop - 项目初始化指南

**更新时间**: 2024-12-23  
**状态**: 代码已完成,待项目初始化

---

## 📋 前提条件

### 必需软件
1. **.NET SDK 8.0+**
   - 下载: https://dotnet.microsoft.com/download
   - 验证: `dotnet --version`

2. **IDE (任选其一)**
   - JetBrains Rider (推荐)
   - Visual Studio 2022 (Windows)
   - VS Code + C# Dev Kit

---

## 🚀 快速初始化

### 方法 1: 使用脚本 (推荐)

```bash
cd /Volumes/Doc/WS/9-Git/wordflowlab/musicify/client
chmod +x scripts/init-project.sh
./scripts/init-project.sh
```

**脚本会自动**:
- ✅ 创建 .NET 解决方案
- ✅ 创建 5 个项目 (Desktop, Core, Audio, AI, Tests)
- ✅ 安装所有 NuGet 包
- ✅ 配置项目引用
- ✅ 构建解决方案

---

### 方法 2: 手动初始化

#### Step 1: 创建解决方案
```bash
cd /Volumes/Doc/WS/9-Git/wordflowlab/musicify/client
dotnet new sln -n Musicify
```

#### Step 2: 创建项目

```bash
# 1. AvaloniaUI 主应用
dotnet new install Avalonia.Templates
dotnet new avalonia.mvvm -n Musicify.Desktop -o src/Musicify.Desktop

# 2. 核心类库
dotnet new classlib -n Musicify.Core -o src/Musicify.Core

# 3. 音频处理库
dotnet new classlib -n Musicify.Audio -o src/Musicify.Audio

# 4. AI 服务库
dotnet new classlib -n Musicify.AI -o src/Musicify.AI

# 5. 测试项目
dotnet new xunit -n Musicify.Core.Tests -o tests/Musicify.Core.Tests
```

#### Step 3: 添加到解决方案

```bash
dotnet sln add src/Musicify.Desktop/Musicify.Desktop.csproj
dotnet sln add src/Musicify.Core/Musicify.Core.csproj
dotnet sln add src/Musicify.Audio/Musicify.Audio.csproj
dotnet sln add src/Musicify.AI/Musicify.AI.csproj
dotnet sln add tests/Musicify.Core.Tests/Musicify.Core.Tests.csproj
```

#### Step 4: 配置项目引用

```bash
# Desktop 引用所有库
dotnet add src/Musicify.Desktop reference src/Musicify.Core
dotnet add src/Musicify.Desktop reference src/Musicify.Audio
dotnet add src/Musicify.Desktop reference src/Musicify.AI

# 测试引用 Core
dotnet add tests/Musicify.Core.Tests reference src/Musicify.Core
```

#### Step 5: 安装 NuGet 包

```bash
# Musicify.Desktop
cd src/Musicify.Desktop
dotnet add package CommunityToolkit.Mvvm --version 8.2.2
cd ../..

# Musicify.Core
cd src/Musicify.Core
dotnet add package Microsoft.Extensions.DependencyInjection --version 8.0.0
dotnet add package Microsoft.Extensions.Configuration.Json --version 8.0.0
dotnet add package Serilog --version 3.1.1
dotnet add package Serilog.Sinks.File --version 5.0.0
cd ../..

# Musicify.Audio
cd src/Musicify.Audio
dotnet add package Melanchall.DryWetMidi --version 7.2.0
dotnet add package NAudio --version 2.2.1
dotnet add package Python.Runtime --version 3.0.4
cd ../..

# Musicify.AI
cd src/Musicify.AI
dotnet add package Anthropic.SDK --version 0.4.0
cd ../..

# Tests
cd tests/Musicify.Core.Tests
dotnet add package FluentAssertions --version 6.12.0
dotnet add package Moq --version 4.20.70
cd ../..
```

#### Step 6: 构建

```bash
dotnet build
```

---

## 📁 复制已完成的代码

### Step 1: 复制核心模型

```bash
# 已完成的文件位于:
# src/Musicify.Core/Models/*.cs
# src/Musicify.Core/Services/*.cs
# src/Musicify.Core/ViewModels/*.cs
# tests/Musicify.Core.Tests/**/*.cs
# src/Musicify.Desktop/Views/*.axaml*
# src/Musicify.Desktop/Styles/*.axaml
```

**这些文件无需修改,可直接使用!**

### Step 2: 配置 App.axaml

在 `src/Musicify.Desktop/App.axaml.cs` 中配置依赖注入:

```csharp
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Musicify.Core.Services;
using Musicify.Core.ViewModels;
using Musicify.Desktop.Services;
using Musicify.Desktop.Views;

namespace Musicify.Desktop;

public partial class App : Application
{
    public IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        ConfigureServices();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var welcomeViewModel = Services.GetRequiredService<WelcomeViewModel>();
            desktop.MainWindow = new WelcomeWindow
            {
                DataContext = welcomeViewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices()
    {
        var services = new ServiceCollection();

        // 注册服务
        services.AddSingleton<IProjectService, ProjectService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IAIService, ClaudeService>();
        services.AddSingleton<IPromptTemplateService, PromptTemplateService>();
        services.AddTransient<IFileSystem, FileSystem>();

        // 注册 ViewModels
        services.AddTransient<WelcomeViewModel>();
        services.AddTransient<CreateProjectViewModel>();

        Services = services.BuildServiceProvider();
    }
}
```

### Step 3: 创建 NavigationService 实现

创建 `src/Musicify.Desktop/Services/NavigationService.cs`:

```csharp
using Avalonia.Controls;
using Musicify.Core.Services;
using Musicify.Desktop.Views;

namespace Musicify.Desktop.Services;

public class NavigationService : INavigationService
{
    private readonly Stack<Window> _navigationStack = new();

    public void NavigateTo(string viewName, object? parameter = null)
    {
        Window? window = viewName switch
        {
            "WelcomeWindow" => new WelcomeWindow(),
            "CreateProjectView" => new Window { Content = new CreateProjectView() },
            "MainWindow" => new Window(), // TODO: 实现主窗口
            _ => null
        };

        if (window != null)
        {
            window.DataContext = parameter;
            _navigationStack.Push(window);
            window.Show();
        }
    }

    public bool GoBack()
    {
        if (_navigationStack.Count > 1)
        {
            var current = _navigationStack.Pop();
            current.Close();
            return true;
        }
        return false;
    }

    public bool CanGoBack => _navigationStack.Count > 1;

    public void CloseCurrentWindow()
    {
        if (_navigationStack.TryPeek(out var window))
        {
            window.Close();
            _navigationStack.Pop();
        }
    }

    public Task<object?> ShowDialogAsync(string dialogName, object? parameter = null)
    {
        // TODO: 实现对话框
        return Task.FromResult<object?>(null);
    }
}
```

### Step 4: 创建 FileSystem 实现

创建 `src/Musicify.Core/Abstractions/FileSystem.cs`:

```csharp
namespace Musicify.Core.Abstractions;

public class FileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);
    public bool DirectoryExists(string path) => Directory.Exists(path);
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);
    public string ReadAllText(string path) => File.ReadAllText(path);
    public void WriteAllText(string path, string contents) => File.WriteAllText(path, contents);
    public string[] GetFiles(string path, string searchPattern) => Directory.GetFiles(path, searchPattern);
}
```

---

## ✅ 验证安装

### 运行测试

```bash
dotnet test
```

**预期输出**:
```
Test Run Successful.
Total tests: 108
     Passed: 108
```

### 运行应用

```bash
cd src/Musicify.Desktop
dotnet run
```

**预期效果**:
- ✅ 打开欢迎窗口
- ✅ 显示 "Musicify" Logo
- ✅ 显示 "创建新项目" 和 "打开现有项目" 按钮
- ✅ 右侧显示最近项目列表 (初次运行为空)

---

## 🐛 常见问题

### 问题 1: Avalonia 模板未安装
```bash
错误: dotnet new: 'avalonia.mvvm' is not found
```

**解决**:
```bash
dotnet new install Avalonia.Templates
```

---

### 问题 2: .NET SDK 版本过低
```bash
错误: The current .NET SDK does not support targeting .NET 8.0
```

**解决**:
- 升级到 .NET 8.0+ SDK
- 或修改 `Directory.Build.props` 中的 `<TargetFramework>net8.0</TargetFramework>` 为你的版本

---

### 问题 3: NuGet 包下载失败
```bash
错误: Unable to find package 'Anthropic.SDK'
```

**解决**:
```bash
dotnet nuget locals all --clear
dotnet restore --force
```

---

## 📊 初始化后的项目结构

```
client/
├── Musicify.sln                    ✅ 解决方案
├── Directory.Build.props           ✅ 全局配置
├── .editorconfig                   ✅ 编码规范
│
├── src/
│   ├── Musicify.Desktop/           ✅ UI 层
│   │   ├── Views/                  ✅ 2 个窗口/视图
│   │   ├── Styles/                 ✅ 1 个样式文件
│   │   ├── Services/               ⚠️ 需手动创建 NavigationService
│   │   └── App.axaml*              ⚠️ 需配置依赖注入
│   │
│   ├── Musicify.Core/              ✅ 核心业务
│   │   ├── Models/                 ✅ 10 个模型
│   │   ├── Services/               ✅ 7 个服务接口/实现
│   │   ├── ViewModels/             ✅ 3 个 ViewModel
│   │   └── Abstractions/           ✅ 1 个抽象 + ⚠️ 需实现 FileSystem
│   │
│   ├── Musicify.Audio/             ⚪ 未开始
│   └── Musicify.AI/                ⚪ 未开始
│
└── tests/
    └── Musicify.Core.Tests/        ✅ 9 个测试文件
        ├── Models/                 ✅ 3 个测试
        ├── Services/               ✅ 3 个测试
        └── ViewModels/             ✅ 3 个测试
```

**状态**:
- ✅ 完成 (28 个文件)
- ⚠️ 需补充 (3 个文件)
- ⚪ 未开始

---

## 🎯 下一步

初始化完成后,你可以:

1. **验证运行** - `dotnet run` 查看欢迎窗口
2. **运行测试** - `dotnet test` 验证所有 108+ 测试通过
3. **继续开发** - 开始 SDD 循环 #5: MIDI 分析服务

---

祝你初始化顺利! 🚀

如有问题,请查看:
- 📄 `docs/SDD-PROGRESS.md` - 开发进度
- 📄 `docs/SDD-CYCLE-04-SUMMARY.md` - 本轮总结
- 📄 `docs/specs/*.md` - 详细规范
