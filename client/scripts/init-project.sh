#!/bin/bash

# Musicify Desktop - 项目初始化脚本
# 自动创建解决方案和所有项目

set -e  # 遇到错误立即退出

echo "🎵 Musicify Desktop - 项目初始化"
echo "=================================="
echo ""

# 检查 .NET SDK
if ! command -v dotnet &> /dev/null; then
    echo "❌ 错误: 未安装 .NET SDK"
    echo "请访问: https://dotnet.microsoft.com/download"
    exit 1
fi

echo "✅ .NET SDK 版本: $(dotnet --version)"
echo ""

# 检查 Avalonia 模板
if ! dotnet new list | grep -q "avalonia.mvvm"; then
    echo "📦 安装 Avalonia 模板..."
    dotnet new install Avalonia.Templates
fi

echo "✅ Avalonia 模板已安装"
echo ""

# 创建解决方案
echo "📂 创建解决方案..."
if [ ! -f "Musicify.sln" ]; then
    dotnet new sln -n Musicify
    echo "✅ 解决方案创建完成"
else
    echo "⚠️  解决方案已存在,跳过"
fi
echo ""

# 创建项目目录
mkdir -p src tests scripts docs/{specs,architecture,tasks}

# 创建主应用 (AvaloniaUI)
echo "🖥️  创建主应用 (Musicify.Desktop)..."
if [ ! -d "src/Musicify.Desktop" ]; then
    dotnet new avalonia.mvvm -n Musicify.Desktop -o src/Musicify.Desktop
    echo "✅ Musicify.Desktop 创建完成"
else
    echo "⚠️  Musicify.Desktop 已存在,跳过"
fi

# 创建核心类库
echo "📚 创建核心类库 (Musicify.Core)..."
if [ ! -d "src/Musicify.Core" ]; then
    dotnet new classlib -n Musicify.Core -o src/Musicify.Core
    echo "✅ Musicify.Core 创建完成"
else
    echo "⚠️  Musicify.Core 已存在,跳过"
fi

# 创建音频处理库
echo "🎵 创建音频处理库 (Musicify.Audio)..."
if [ ! -d "src/Musicify.Audio" ]; then
    dotnet new classlib -n Musicify.Audio -o src/Musicify.Audio
    echo "✅ Musicify.Audio 创建完成"
else
    echo "⚠️  Musicify.Audio 已存在,跳过"
fi

# 创建 AI 服务库
echo "🤖 创建 AI 服务库 (Musicify.AI)..."
if [ ! -d "src/Musicify.AI" ]; then
    dotnet new classlib -n Musicify.AI -o src/Musicify.AI
    echo "✅ Musicify.AI 创建完成"
else
    echo "⚠️  Musicify.AI 已存在,跳过"
fi

# 创建测试项目
echo "🧪 创建测试项目 (Musicify.Core.Tests)..."
if [ ! -d "tests/Musicify.Core.Tests" ]; then
    dotnet new xunit -n Musicify.Core.Tests -o tests/Musicify.Core.Tests
    echo "✅ Musicify.Core.Tests 创建完成"
else
    echo "⚠️  Musicify.Core.Tests 已存在,跳过"
fi

echo ""
echo "📦 添加项目到解决方案..."

# 添加所有项目到解决方案
dotnet sln add src/Musicify.Desktop/Musicify.Desktop.csproj 2>/dev/null || echo "  - Musicify.Desktop 已在解决方案中"
dotnet sln add src/Musicify.Core/Musicify.Core.csproj 2>/dev/null || echo "  - Musicify.Core 已在解决方案中"
dotnet sln add src/Musicify.Audio/Musicify.Audio.csproj 2>/dev/null || echo "  - Musicify.Audio 已在解决方案中"
dotnet sln add src/Musicify.AI/Musicify.AI.csproj 2>/dev/null || echo "  - Musicify.AI 已在解决方案中"
dotnet sln add tests/Musicify.Core.Tests/Musicify.Core.Tests.csproj 2>/dev/null || echo "  - Musicify.Core.Tests 已在解决方案中"

echo ""
echo "🔗 添加项目引用..."

# Desktop 引用其他所有库
dotnet add src/Musicify.Desktop reference src/Musicify.Core 2>/dev/null || true
dotnet add src/Musicify.Desktop reference src/Musicify.Audio 2>/dev/null || true
dotnet add src/Musicify.Desktop reference src/Musicify.AI 2>/dev/null || true

# 测试项目引用 Core
dotnet add tests/Musicify.Core.Tests reference src/Musicify.Core 2>/dev/null || true

echo ""
echo "📦 安装必需的 NuGet 包..."

# Musicify.Desktop 包
cd src/Musicify.Desktop
dotnet add package CommunityToolkit.Mvvm --version 8.2.2 2>/dev/null || true
cd ../..

# Musicify.Core 包
cd src/Musicify.Core
dotnet add package Microsoft.Extensions.DependencyInjection --version 8.0.0 2>/dev/null || true
dotnet add package Microsoft.Extensions.Configuration.Json --version 8.0.0 2>/dev/null || true
dotnet add package Serilog --version 3.1.1 2>/dev/null || true
dotnet add package Serilog.Sinks.File --version 5.0.0 2>/dev/null || true
cd ../..

# Musicify.Audio 包
cd src/Musicify.Audio
dotnet add package Melanchall.DryWetMidi --version 7.2.0 2>/dev/null || true
dotnet add package NAudio --version 2.2.1 2>/dev/null || true
dotnet add package Python.Runtime --version 3.0.4 2>/dev/null || true
cd ../..

# Musicify.AI 包
cd src/Musicify.AI
dotnet add package Anthropic.SDK --version 0.4.0 2>/dev/null || true
cd ../..

# Test 包
cd tests/Musicify.Core.Tests
dotnet add package FluentAssertions --version 6.12.0 2>/dev/null || true
dotnet add package Moq --version 4.20.70 2>/dev/null || true
cd ../..

echo ""
echo "🏗️  创建 Directory.Build.props..."

cat > Directory.Build.props << 'EOF'
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>12.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
EOF

echo "✅ Directory.Build.props 创建完成"
echo ""

echo "📝 创建 EditorConfig..."

cat > .editorconfig << 'EOF'
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true

[*.cs]
indent_style = space
indent_size = 4

# C# 命名规范
dotnet_naming_rule.interfaces_must_be_prefixed_with_i.severity = warning
dotnet_naming_rule.interfaces_must_be_prefixed_with_i.symbols = interface
dotnet_naming_rule.interfaces_must_be_prefixed_with_i.style = begins_with_i

dotnet_naming_symbols.interface.applicable_kinds = interface
dotnet_naming_style.begins_with_i.required_prefix = I
dotnet_naming_style.begins_with_i.capitalization = pascal_case

# 代码风格
csharp_prefer_braces = true:warning
dotnet_sort_system_directives_first = true
EOF

echo "✅ EditorConfig 创建完成"
echo ""

echo "🔨 构建解决方案..."
dotnet build

echo ""
echo "=================================="
echo "✅ 项目初始化完成!"
echo ""
echo "📁 项目结构:"
echo "  src/Musicify.Desktop/    - UI 层"
echo "  src/Musicify.Core/       - 核心业务"
echo "  src/Musicify.Audio/      - 音频/MIDI"
echo "  src/Musicify.AI/         - AI 服务"
echo "  tests/                   - 测试项目"
echo ""
echo "🚀 下一步:"
echo "  1. 查看开发路线图: cat docs/tasks/development-roadmap.md"
echo "  2. 阅读第一个 Spec: cat docs/specs/01-project-setup.md"
echo "  3. 运行应用: cd src/Musicify.Desktop && dotnet run"
echo ""
echo "💡 提示: 使用 Rider 或 Visual Studio 打开 Musicify.sln"
echo ""
