# Musicify Desktop Client (C#)

> 基于 AvaloniaUI 的跨平台桌面客户端,提供现代化的音乐创作体验

## 🎯 项目目标

将 Musicify CLI 工具转化为功能完整的桌面应用,提供:
- ✅ 可视化的歌词编辑器
- ✅ MIDI 分析与可视化
- ✅ AI 辅助创作界面
- ✅ 项目管理系统
- ✅ 跨平台支持 (Windows/macOS/Linux)

## 🏗 技术栈

| 组件 | 技术选型 | 版本 | 用途 |
|------|----------|------|------|
| **框架** | .NET | 8.0 | 应用框架 |
| **UI** | AvaloniaUI | 11.x | 跨平台界面 |
| **MIDI** | DryWetMIDI | 7.x | MIDI 解析与生成 |
| **音频** | NAudio | 2.x | 音频处理 |
| **Python互操作** | Python.NET | 3.x | 调用现有Python脚本 |
| **AI服务** | Anthropic.SDK | Latest | Claude API |
| **JSON** | System.Text.Json | Built-in | 配置文件处理 |
| **架构** | MVVM | - | UI 架构模式 |

## 📁 项目结构

```
client/
├── docs/                          # 📋 SDD 文档目录
│   ├── specs/                     # 功能规格说明书
│   │   ├── 01-project-setup.md
│   │   ├── 02-core-services.md
│   │   ├── 03-project-manager.md
│   │   ├── 04-spec-editor.md
│   │   ├── 05-lyrics-editor.md
│   │   ├── 06-ai-integration.md
│   │   ├── 07-midi-analysis.md
│   │   └── 08-export-system.md
│   ├── architecture/              # 架构设计文档
│   │   ├── system-overview.md
│   │   ├── data-flow.md
│   │   └── api-design.md
│   └── tasks/                     # 任务分解
│       └── development-roadmap.md
├── src/                           # 📦 源代码目录
│   ├── Musicify.Desktop/          # 主应用程序
│   │   ├── App.axaml
│   │   ├── ViewModels/
│   │   └── Views/
│   ├── Musicify.Core/             # 核心业务逻辑
│   │   ├── Models/
│   │   ├── Services/
│   │   └── Interfaces/
│   ├── Musicify.Audio/            # 音频/MIDI 处理
│   │   ├── MidiAnalyzer.cs
│   │   ├── AudioConverter.cs
│   │   └── PythonBridge.cs
│   └── Musicify.AI/               # AI 服务集成
│       ├── ClaudeService.cs
│       ├── PromptBuilder.cs
│       └── StreamingHandler.cs
├── tests/                         # 🧪 测试项目
│   ├── Musicify.Core.Tests/
│   └── Musicify.Audio.Tests/
├── scripts/                       # 🔧 构建脚本
│   ├── setup-python-env.sh
│   └── build-release.sh
└── Musicify.sln                   # 解决方案文件
```

## 🚀 快速开始

### 环境要求

```bash
# 1. 安装 .NET 8 SDK
dotnet --version  # 应显示 8.x.x

# 2. 安装 AvaloniaUI 模板
dotnet new install Avalonia.Templates

# 3. 安装 Python 3.10+ (用于 MIDI 分析)
python3 --version

# 4. 安装 Python 依赖
pip install mido music21 numpy
```

### 创建项目

```bash
cd client

# 1. 创建解决方案
dotnet new sln -n Musicify

# 2. 创建主应用 (AvaloniaUI)
dotnet new avalonia.mvvm -n Musicify.Desktop -o src/Musicify.Desktop

# 3. 创建核心类库
dotnet new classlib -n Musicify.Core -o src/Musicify.Core
dotnet new classlib -n Musicify.Audio -o src/Musicify.Audio
dotnet new classlib -n Musicify.AI -o src/Musicify.AI

# 4. 创建测试项目
dotnet new xunit -n Musicify.Core.Tests -o tests/Musicify.Core.Tests

# 5. 添加项目到解决方案
dotnet sln add src/Musicify.Desktop/Musicify.Desktop.csproj
dotnet sln add src/Musicify.Core/Musicify.Core.csproj
dotnet sln add src/Musicify.Audio/Musicify.Audio.csproj
dotnet sln add src/Musicify.AI/Musicify.AI.csproj
dotnet sln add tests/Musicify.Core.Tests/Musicify.Core.Tests.csproj

# 6. 添加项目引用
dotnet add src/Musicify.Desktop reference src/Musicify.Core
dotnet add src/Musicify.Desktop reference src/Musicify.Audio
dotnet add src/Musicify.Desktop reference src/Musicify.AI
```

## 📋 开发模式: SDD (Spec-Driven Development)

### 工作流程

```
1️⃣ 编写规格说明书 (Spec)
   ↓
2️⃣ 设计 API 接口
   ↓
3️⃣ 编写单元测试 (TDD)
   ↓
4️⃣ 实现功能代码
   ↓
5️⃣ 集成测试
   ↓
6️⃣ 文档更新
```

### Spec 文档模板

每个功能模块的 Spec 应包含:

```markdown
# 功能名称

## 概述
简要描述功能目标和用户价值

## 用户故事
- 作为 [角色], 我想要 [功能], 以便 [价值]

## 功能需求
### 必须实现 (Must Have)
- [ ] 需求1
- [ ] 需求2

### 应该实现 (Should Have)
- [ ] 需求3

### 可以实现 (Could Have)
- [ ] 需求4

## 技术规格
### API 设计
\`\`\`csharp
public interface IXxxService
{
    Task<Result> DoSomething(Request req);
}
\`\`\`

### 数据模型
\`\`\`csharp
public class Model { }
\`\`\`

### 依赖关系
- 依赖模块 A
- 依赖服务 B

## UI 设计
- 线框图/原型链接
- 交互流程说明

## 测试用例
1. 场景1: 预期行为
2. 场景2: 边界情况
3. 场景3: 异常处理

## 验收标准
- [ ] 标准1
- [ ] 标准2

## 开发时间估算
- 设计: X 小时
- 开发: Y 小时
- 测试: Z 小时
```

## 🎯 开发里程碑

### Phase 1: 项目基础 (Week 1-2)
- [x] 项目结构搭建
- [ ] 核心服务框架
- [ ] 项目配置系统
- [ ] 基础 UI 框架

### Phase 2: 核心功能 (Week 3-4)
- [ ] 项目管理器
- [ ] 规格编辑器
- [ ] 歌词编辑器
- [ ] 文件系统抽象

### Phase 3: AI 集成 (Week 5-6)
- [ ] Claude API 封装
- [ ] 提示词系统
- [ ] 流式响应处理
- [ ] 三种创作模式

### Phase 4: 音乐分析 (Week 7-9)
- [ ] MIDI 解析器
- [ ] 人声音轨识别
- [ ] 特征提取算法
- [ ] Python 脚本集成

### Phase 5: 高级功能 (Week 10-11)
- [ ] 导出系统
- [ ] 押韵检查
- [ ] 和弦生成
- [ ] 五线谱渲染

### Phase 6: 优化发布 (Week 12)
- [ ] 性能优化
- [ ] UI/UX 优化
- [ ] 打包发布
- [ ] 用户文档

## 📚 参考资源

### 官方文档
- [AvaloniaUI Docs](https://docs.avaloniaui.net/)
- [DryWetMIDI Docs](https://melanchall.github.io/drywetmidi/)
- [NAudio Docs](https://github.com/naudio/NAudio)

### CLI 项目资源复用
- `../templates/` - AI 提示词模板
- `../skills/` - Claude Skill 定义
- `../skills/scripts/` - Python 分析脚本

## 🔧 开发规范

### 代码风格
- 遵循 C# 官方编码规范
- 使用 EditorConfig 统一格式
- 所有公开 API 必须有 XML 文档注释

### Git 工作流
- 主分支: `main` (稳定版本)
- 开发分支: `develop` (集成分支)
- 功能分支: `feature/xxx`
- 修复分支: `fix/xxx`

### 提交规范
```
feat: 新功能
fix: 修复bug
docs: 文档更新
refactor: 代码重构
test: 测试相关
chore: 构建/工具变更
```

## 📞 联系方式

如有问题请提交 Issue 或联系开发团队。
