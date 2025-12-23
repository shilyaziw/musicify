# Musicify Desktop - 快速启动指南

## 🚀 5分钟开始开发

### Step 1: 环境检查

```bash
# 检查 .NET SDK (需要 8.0+)
dotnet --version

# 检查 Python (可选,用于 MIDI 分析)
python3 --version
```

### Step 2: 初始化项目

```bash
cd client

# 安装 Avalonia 模板
dotnet new install Avalonia.Templates

# 运行初始化脚本
chmod +x scripts/init-project.sh
./scripts/init-project.sh
```

### Step 3: 打开项目

```bash
# 使用 Rider (推荐)
rider Musicify.sln

# 或使用 VS Code
code .

# 或使用 Visual Studio
open Musicify.sln
```

### Step 4: 运行应用

```bash
cd src/Musicify.Desktop
dotnet run
```

---

## 📋 SDD 开发模式工作流

### 1. 选择一个任务

查看 `docs/tasks/development-roadmap.md` 选择当前周期的任务

### 2. 阅读 Spec 文档

```bash
# 例如: Task 1.3 - 项目配置
cat docs/specs/01-project-setup.md
```

### 3. 编写测试 (TDD)

```bash
# 创建测试文件
cd tests/Musicify.Core.Tests
# 编写测试用例 (参考 Spec 中的测试章节)
```

### 4. 实现功能

```bash
cd src/Musicify.Core
# 实现接口和服务
```

### 5. 运行测试

```bash
dotnet test
```

### 6. 更新文档

在 Spec 文档中标记完成状态

---

## 📚 本周任务 (Week 1)

### ✅ 已完成
- [x] 创建项目结构
- [x] 编写 SDD 文档

### 🟡 进行中
- [ ] Task 1.3: 配置项目设置
- [ ] Task 1.4: 设计核心数据模型
- [ ] Task 1.5: 实现项目配置服务

### ⏱️ 预计完成时间
- **本周剩余**: 15 小时
- **预计完成日期**: 本周末

---

## 🔧 常用命令

```bash
# 构建解决方案
dotnet build

# 运行测试
dotnet test

# 运行应用 (Debug)
dotnet run --project src/Musicify.Desktop

# 清理构建产物
dotnet clean

# 添加 NuGet 包
dotnet add package PackageName

# 创建新类库
dotnet new classlib -n ProjectName
```

---

## 📖 推荐阅读顺序

1. `client/README.md` - 项目总览
2. `docs/tasks/development-roadmap.md` - 开发路线图
3. `docs/specs/01-project-setup.md` - 第一个 Spec 文档
4. `docs/architecture/system-overview.md` - 架构设计 (待创建)

---

## 🆘 遇到问题?

1. 查看 `docs/specs/` 中的相关 Spec 文档
2. 查看已有的单元测试示例
3. 参考 CLI 版本的实现 (`../src/`, `../scripts/`)

---

## 🎯 下一步

运行以下命令开始第一个任务:

```bash
# 查看详细任务
cat docs/tasks/development-roadmap.md

# 阅读第一个 Spec
cat docs/specs/01-project-setup.md
```

Good luck! 🚀
