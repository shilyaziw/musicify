---
description: 旋律风格学习助手 (简易版) - 支持 MP3 输入，自动转换为 MIDI
scripts:
  sh: ../../scripts/bash/melody-mimic-easy.sh
  ps1: ../../scripts/powershell/melody-mimic-easy.ps1
mode: mimic-easy
---

# /melody-mimic-easy - 旋律风格学习助手 (简易版) 🎼

> **核心理念**: 普通用户只需提供 MP3 + 歌词，自动完成 MIDI 转换和旋律分析
> **目标**: 降低使用门槛，让没有 MIDI 文件的用户也能学习旋律风格

---

## 与 /melody-mimic 的区别

| 特性 | /melody-mimic | /melody-mimic-easy |
|------|---------------|-------------------|
| 输入格式 | MIDI + 歌词 | **MP3 + 歌词** (或 MIDI + 歌词) |
| 技术门槛 | 需要获取 MIDI 文件 | 只需 MP3 文件 |
| 额外依赖 | 无 | demucs, basic-pitch (可选) |
| 处理时间 | 即时 | 需要 2-15 分钟转换 |

---

## 第一步: 运行脚本获取状态 ⚠️ 必须执行

```bash
# AI 操作: 运行脚本
bash scripts/bash/melody-mimic-easy.sh
```

### 解析返回结果

脚本会返回不同状态:

**状态 1: `ready_to_convert`** - 检测到 MP3，需要转换
```json
{
  "status": "ready_to_convert",
  "action": "convert_mp3",
  "hardware": {
    "device": "mps",
    "description": "Apple Silicon 加速",
    "estimated_time": "2-3 分钟"
  },
  "python_command": "python3 .../audio_to_midi.py 'song.mp3' 'output_dir'"
}
```

**状态 2: `ready`** - MIDI 已存在，直接分析
```json
{
  "status": "ready",
  "action": "analyze",
  "source": "midi"
}
```

**状态 3: `need_dependencies`** - 需要安装依赖
```json
{
  "status": "need_dependencies",
  "install_command": "pip install demucs basic-pitch",
  "alternatives": [{"name": "在线工具", "url": "https://basicpitch.spotify.com"}]
}
```

---

## 📁 输入文件要求

### 文件目录结构

```
workspace/
└── references/
    └── {song-name}/
        ├── {song-name}.mp3      # MP3 文件 (或 .mid)
        └── {song-name}.txt      # 歌词文件（必需）
```

**示例**:
```
workspace/
└── references/
    └── 探故知/
        ├── 探故知.mp3           # MP3 音频文件
        └── 探故知.txt           # 歌词文件
```

### 歌词文件格式

歌词文件使用纯文本格式，需包含段落标记 (兼容 Suno/Tunee):

```
[Verse 1]
三两笔着墨迟迟
不为记事
随手便成诗

[Chorus]
多少往事随风去
化作云烟散
只留一曲探故知
```

---

## 🔄 MP3 转换流程

### 当检测到 MP3 文件时

**AI 应该**:

1. **展示硬件信息和预计时间**:
   ```
   检测到 MP3 文件: workspace/references/探故知/探故知.mp3

   您的设备: Apple Silicon 加速 (MPS)
   预计耗时: 约 2-3 分钟

   转换流程:
   1. 使用 Demucs 分离人声音轨
   2. 使用 Basic Pitch 将人声转为 MIDI
   3. 进入旋律分析流程

   是否开始转换? (Y/n)
   ```

2. **用户确认后执行转换**:
   ```bash
   # 执行脚本返回的 python_command
   python3 skills/scripts/audio_to_midi.py "workspace/references/探故知/探故知.mp3" "workspace/references/探故知"
   ```

3. **转换完成后继续分析流程**

### 转换失败的备选方案

如果本地转换失败或用户设备较慢，提供在线工具选项:

```
本地转换可能需要较长时间 (约 10-15 分钟)

您也可以使用在线工具:
1. 访问 https://basicpitch.spotify.com
2. 上传 MP3 文件
3. 下载生成的 MIDI 文件
4. 将 MIDI 文件重命名为 {song-name}.mid
5. 放入 workspace/references/{song-name}/ 目录
6. 重新运行 /melody-mimic-easy
```

---

## 🖥️ 硬件兼容性

| 设备 | Demucs 耗时 | Basic Pitch 耗时 | 总耗时 |
|------|-------------|------------------|--------|
| Mac M1/M2 | ~2 分钟 | ~20 秒 | **~3 分钟** |
| Windows + NVIDIA GPU | ~1 分钟 | ~10 秒 | **~2 分钟** |
| Windows + 集成显卡 | ~8 分钟 | ~40 秒 | **~10 分钟** |
| 老旧电脑 | ~15 分钟 | ~60 秒 | **~16 分钟** |

### 依赖安装

```bash
# 安装 MP3 转 MIDI 依赖 (可选，仅需要转换 MP3 时)
pip install demucs basic-pitch

# 检查依赖状态
bash scripts/bash/melody-mimic-easy.sh --check
```

---

## ⚠️ 版权风险提醒

**重要声明**：

1. **旋律受版权保护**: 参考歌曲的旋律属于原作者的知识产权
2. **仅供学习参考**: 本工具生成的内容仅供个人学习和创作参考使用
3. **商用风险评估**: 如需商业使用，请自行评估版权风险并获取必要授权
4. **原创性要求**: 建议在生成内容基础上进行充分的二次创作

> 使用本命令即表示您已了解并接受上述风险提示

---

## 🎵 后续流程

MP3 转换完成后，流程与 `/melody-mimic` 完全相同:

### 步骤 1: 人声音轨匹配
- 分析 MIDI 音轨
- 字数匹配验证
- 音域验证

### 步骤 2: 特征分析
- 🥁 节奏型分布
- 🎵 音程分布
- 🎼 调式推断
- 📈 旋律轮廓

### 步骤 3: 生成双报告
- 📝 歌词深度分析报告
- 🎵 旋律特征分析报告

### 步骤 4: 选择创作模式
- ⚡ 快速模式 (3-8分钟)
- 🎯 专业模式 (10-18分钟)
- 🎓 教练模式 (20-35分钟)
- 🔧 专家模式 (30-60分钟)

### 步骤 5: 歌词创作引导
- 主题输入
- 逐段创作
- 结构验证

### 步骤 6: 旋律生成
- 选择借鉴程度
- 基于特征生成
- 输出简谱和 MIDI

---

## 📄 输出格式

### 生成文件

```
workspace/
└── output/
    └── {new-song-name}/
        ├── {new-song-name}.txt           # 新歌词文件
        ├── {new-song-name}.jianpu        # 简谱文件
        ├── {new-song-name}.mid           # MIDI 文件
        └── {new-song-name}-创作报告.md   # 创作成果报告（可选）
```

---

## 💡 使用提示

### 最佳实践

1. **优先使用高质量 MP3** - 320kbps 或更高
2. **确保人声清晰** - 避免过多混响或伴奏干扰
3. **歌词格式规范** - 使用标准段落标记 `[Verse 1]`, `[Chorus]` 等
4. **耐心等待转换** - 首次转换需要下载模型，可能较慢

### 常见问题

**Q: 转换失败怎么办?**
A: 尝试使用在线工具 basicpitch.spotify.com

**Q: 转换后的 MIDI 质量不好?**
A: MP3 转 MIDI 是有损过程，建议使用原版 MIDI 文件获得最佳效果

**Q: 没有 GPU 可以用吗?**
A: 可以，但会较慢。建议使用在线工具或耐心等待。

---

**准备好开始了吗？** 🎶

只需准备 MP3 + 歌词文件，运行脚本即可开始！
