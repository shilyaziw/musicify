# Musicify - AI 驱动的歌词创作工具

> **版本**: v0.1.0  
> **状态**: ✅ 核心功能已实现  
> **定位**: 专注歌词创作阶段的AI辅助工具

**核心价值**: 帮助创作者从零写好歌词,支持全类型歌曲、三种创作模式、完整创作流程

---

## ⚠️ 产品边界

**Musicify 专注于歌词创作**,不涉及编曲、录音、制作等环节。

✅ **做什么**:
- 全类型歌词创作 (流行/摇滚/说唱/民谣/电子/古风等)
- 三种创作模式 (教练/快速/混合)
- 歌词质量评估与优化
- 押韵检查和优化
- 旋律提示生成

❌ **不做什么**:
- 作曲编曲
- 音频制作
- 演唱录音
- MV制作

---

## 🎯 核心功能

### 1. 全类型歌曲支持

支持主流歌曲类型:
- 🎵 **流行** - 主流流行音乐
- 🎸 **摇滚** - 摇滚/朋克风格
- 🎤 **说唱** - Hip-Hop/Rap
- 🎻 **民谣** - 民谣/独立音乐
- 🎹 **电子** - EDM/电子音乐
- 🏮 **古风** - 中国风/古风
- 🎺 **R&B** - 节奏布鲁斯
- 🎷 **爵士** - 爵士乐
- 🤠 **乡村** - 乡村音乐
- 🔥 **金属** - 重金属/金属核

### 2. 三种创作模式

**教练模式 (Coach)** - 100%原创
- AI 引导你思考,逐段创作
- 提问式激发创意
- 质量实时检查
- 适合: 追求原创的创作者

**快速模式 (Express)** - 快速迭代
- AI 直接生成完整歌词
- 基于规格快速输出
- 适合: 快速原型,灵感激发

**混合模式 (Hybrid)** - 平衡效率与原创
- AI 生成框架和关键句
- 用户填充细节
- 适合: 需要结构指引的创作者

### 3. 完整创作流程

从主题到成品的完整工作流:
1. `/spec` - 定义歌曲规格
2. `/theme` - 主题构思
3. `/mood` - 情绪定位
4. `/structure` - 结构设计
5. `/lyrics` - 歌词创作
6. `/rhyme` - 押韵检查
7. `/polish` - 润色优化
8. `/melody-hint` - 旋律提示

### 4. 专业功能

- ✅ 押韵检查与优化
- ✅ 可唱性检测
- ✅ 意象丰富度分析
- ✅ 情感递进检查
- ✅ 旋律提示生成

---

## 📦 安装

```bash
npm install -g ai-musicify
```

或本地开发:

```bash
git clone https://github.com/wordflowlab/musicify.git
cd musicify
npm install
npm run build
```

---

## 🚀 快速开始

### 1. 初始化歌曲项目

```bash
# 交互式选择 AI 助手、歌曲类型、脚本类型
musicify init "我的第一首歌"

cd "我的第一首歌"
```

**支持13个AI编程助手**:
- Claude Code, Cursor, Gemini CLI
- Windsurf, Roo Code, GitHub Copilot
- Qwen Code, OpenCode, Codex CLI
- Kilo Code, Auggie CLI, CodeBuddy, Amazon Q Developer

### 2. 定义歌曲规格

```bash
/spec
```

AI 引导你填写:
- 类型: 流行/摇滚/说唱/民谣/电子/古风等
- 时长: 3-5分钟或自定义
- 风格: 抒情/激昂/轻快/忧郁/治愈等
- 语言: 中文/英文/粤语/混合等
- 受众: 年龄段和性别
- 平台: QQ音乐/网易云/抖音/B站等

### 3. 开始创作

**使用 Slash Commands 完成创作流程**:
```bash
/theme        # 1. 构思核心主题
/mood         # 2. 定位情绪氛围
/structure    # 3. 设计歌曲结构
/lyrics       # 4. 创作歌词（三种模式）
/rhyme        # 5. 押韵检查
/polish       # 6. 润色优化
/melody-hint  # 7. 旋律提示
```

**三种创作模式**:

```bash
# 教练模式 (追求原创)
musicify /lyrics --mode coach

# 快速模式 (追求效率)
musicify /lyrics --mode express

# 混合模式 (平衡两者)
musicify /lyrics --mode hybrid
```

### 4. 导出歌词

```bash
musicify /export --format txt   # 导出纯文本
musicify /export --format md    # 导出Markdown
musicify /export --format pdf   # 导出PDF
```

---

## 📚 完整命令列表

### 项目管理 (2个)
- `/init <项目名>` - 创建新项目
- `/export --format <格式>` - 导出歌词

### 歌词创作流程 (8个)
- `/spec` - 定义歌曲规格
- `/theme` - 主题构思
- `/mood` - 情绪定位
- `/structure` - 结构设计
- `/lyrics --mode <模式>` - 歌词创作
- `/fill` - 填充混合模式框架
- `/rhyme` - 押韵检查
- `/polish --focus <维度>` - 润色优化

### 辅助功能 (1个)
- `/melody-hint` - 旋律提示

---

## 🏗 架构设计

Musicify 基于三层架构:

```
Markdown指令层 (templates/commands/*.md)
  → 定义AI提示词和工作流程
  → 引导AI如何与用户交互

TypeScript CLI层 (src/cli.ts)
  → 命令行界面
  → 调用Bash脚本

Bash脚本层 (scripts/bash/*.sh)
  → 文件操作和项目管理
  → 输出JSON供AI使用
```

### 为什么这样设计?

1. **灵活性**: Markdown模板可以随时调整,无需重新编译
2. **可维护性**: 三层分离,职责清晰
3. **AI友好**: Markdown格式易于AI理解和执行
4. **跨平台**: 支持Bash和PowerShell

---

## 🎨 创作模式详解

### 教练模式 (Coach)

**理念**: AI是你的创作教练,不是代笔人

**流程**:
1. 逐段引导创作
2. 每段提出3-5个引导问题
3. 逐句检查质量
4. 发现问题立即指出
5. 绝不提供具体词句

**适合**:
- 追求100%原创
- 有时间慢慢打磨
- 想提升创作能力

### 快速模式 (Express)

**理念**: AI快速生成,你快速迭代

**流程**:
1. 分析规格、主题、结构
2. 直接生成完整歌词
3. 保证押韵和可唱性
4. 用户可修改调整

**适合**:
- 需要快速原型
- 寻找灵感
- 学习歌词结构

### 混合模式 (Hybrid)

**理念**: AI搭框架,你填内容

**流程**:
1. AI生成框架和关键句
2. 标注[待填充]部分
3. 提供押韵提示
4. 用户逐项填充

**适合**:
- 需要结构指引
- 平衡效率与原创
- 初学者学习

---

## 📖 使用示例

### 示例 1: 创作一首流行情歌

```bash
# 1. 初始化项目
musicify init "告白气球"
cd "告白气球"

# 2. 定义规格
/spec
# AI引导: 选择"流行"、"3分30秒"、"抒情"、"中文"

# 3. 主题构思
/theme
# AI引导: 表达暗恋的心情,想传达勇敢表白的信息

# 4. 情绪定位
/mood
# AI引导: 温暖、中等能量、中速、浪漫+甜蜜

# 5. 结构设计
/structure
# AI引导: 使用标准流行结构

# 6. 歌词创作 (教练模式)
/lyrics --mode coach
# AI逐段引导创作...

# 7. 押韵检查
/rhyme

# 8. 润色
/polish --focus all

# 9. 导出
/export --format txt
```

### 示例 2: 快速生成说唱歌词

```bash
musicify init "街头故事"
cd "街头故事"

/spec  # 选择"说唱"、"4分钟"、"激昂"
/theme # 表达奋斗和追梦
/mood  # 高能、快节奏
/structure  # 调整为说唱结构 (Hook-Verse-Hook)
/lyrics --mode express  # 快速生成
/rhyme  # 检查多押
/export --format txt
```

---

## 🛣 开发路线图

**Phase 1: MVP** (已完成 ✅)
- 核心命令实现
- 三种创作模式
- 基础质量评估

**Phase 2: 增强功能** (规划中 📋)
- 更智能的押韵建议
- 方言支持 (粤语/四川话等)
- 音节统计和节奏对齐
- 可唱性深度分析

**Phase 3: 生态集成** (未来 🔮)
- 与作曲软件集成
- 旋律参考库
- 风格化示例库

---

## 🤝 贡献

欢迎提交 Issue 和 Pull Request!

---

## 📄 License

MIT License

---

## 🙏 致谢

本项目架构参考了 [Scriptify](https://github.com/wordflowlab/scriptify) 项目。

---

**版本**: v0.1.0  
**发布日期**: 2025-10-31  
**状态**: ✅ 核心功能完成,专注歌词创作

