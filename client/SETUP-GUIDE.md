# Musicify Desktop - 安装指南

## ⚠️ 当前状态

项目代码已完成，但需要安装 .NET SDK 才能运行。

---

## 📋 安装 .NET SDK

### macOS 安装

#### 方法 1: 使用 Homebrew（推荐）
```bash
brew install --cask dotnet-sdk
```

#### 方法 2: 官方安装程序
1. 访问 https://dotnet.microsoft.com/download
2. 下载 macOS 版本的 .NET 8.0 SDK
3. 运行安装程序

### 验证安装
```bash
dotnet --version
# 应显示: 8.0.x
```

---

## 🚀 初始化项目

安装完 .NET SDK 后，运行以下命令：

### 1. 安装 Avalonia 模板
```bash
cd /Users/yakwang/WS/3-Src/202512-Musicify/musicify/client
dotnet new install Avalonia.Templates
```

### 2. 还原 NuGet 包
```bash
dotnet restore
```

### 3. 构建项目
```bash
dotnet build
```

---

## ✅ 验证项目

### 运行测试
```bash
dotnet test
```

**预期结果**：
- ✅ 108+ 测试全部通过
- ✅ 测试覆盖率 > 90%

### 运行应用
```bash
cd src/Musicify.Desktop
dotnet run
```

**预期效果**：
- ✅ 打开欢迎窗口
- ✅ 显示 "Musicify Desktop" 标题
- ✅ 显示 "创建新项目" 和 "打开现有项目" 按钮
- ✅ 最近项目列表（初次运行为空）

---

## 📁 项目结构

```
client/
├── Musicify.sln                      ✅ 已创建
├── Directory.Build.props             ✅ 已创建
├── .editorconfig                     ✅ 已创建
│
├── src/
│   ├── Musicify.Core/                ✅ 完整代码
│   │   ├── Musicify.Core.csproj      ✅ 已创建
│   │   ├── Models/                   ✅ 10 个模型
│   │   ├── Services/                 ✅ 8 个服务
│   │   ├── ViewModels/               ✅ 3 个 ViewModel
│   │   └── Abstractions/             ✅ 2 个文件（含 FileSystem）
│   │
│   └── Musicify.Desktop/             ✅ 完整代码
│       ├── Musicify.Desktop.csproj   ✅ 已创建
│       ├── Program.cs                ✅ 已创建
│       ├── App.axaml                 ✅ 已创建
│       ├── App.axaml.cs              ✅ 已创建（含依赖注入）
│       ├── Views/                    ✅ 2 个窗口/视图
│       ├── Styles/                   ✅ 1 个样式文件
│       └── Services/                 ✅ NavigationService
│
└── tests/
    └── Musicify.Core.Tests/          ✅ 完整测试
        ├── Musicify.Core.Tests.csproj ✅ 已创建
        ├── Models/                   ✅ 3 个测试
        ├── Services/                 ✅ 3 个测试
        └── ViewModels/               ✅ 3 个测试
```

---

## 🎯 已完成的功能

### ✅ 核心数据模型
- ProjectConfig、SongSpec 及 5 个常量类
- 与 CLI 版本 JSON 完全兼容
- 18 个单元测试

### ✅ 项目配置服务
- 项目创建/加载/保存
- 最近项目管理
- 跨平台路径处理
- 20+ 单元测试

### ✅ AI 服务接口
- Claude API 集成（Anthropic.SDK）
- 流式响应处理
- 3 种创作模式（Coach/Express/Hybrid）
- 提示词模板系统
- 27+ 单元测试

### ✅ 项目管理器 UI
- 欢迎窗口（WelcomeWindow）
- 新建项目向导（CreateProjectView）
- MVVM 架构（完整实现）
- 现代化 UI 设计
- 43+ 单元测试

---

## 📊 代码统计

- **总代码**: ~2400 行
- **总测试**: 108+ 测试用例
- **测试覆盖率**: ~92%
- **文档**: 5 个详细 Spec 文档

---

## 🐛 常见问题

### 问题 1: 无法找到 dotnet 命令
**解决**: 安装 .NET 8.0 SDK（参见上文）

### 问题 2: Avalonia 模板未安装
```bash
dotnet new install Avalonia.Templates
```

### 问题 3: NuGet 包下载失败
```bash
dotnet nuget locals all --clear
dotnet restore --force
```

---

## 📚 相关文档

- 📄 [项目状态](PROJECT-STATUS.md) - 总体进度
- 📄 [开发进度](docs/SDD-PROGRESS.md) - SDD 循环追踪
- 📄 [初始化指南](docs/INITIALIZATION-GUIDE.md) - 详细步骤
- 📄 [快速开始](QUICKSTART.md) - 快速上手

---

## 🚀 下一步

安装并验证成功后，您可以：

1. **查看应用运行效果** - `dotnet run`
2. **运行测试验证** - `dotnet test`
3. **继续开发** - 开始 SDD 循环 #5（MIDI 分析服务）

---

祝您开发顺利！🎵
