---
description: 歌曲结构设计 - 设计完整的歌曲段落结构
scripts:
  sh: ../../scripts/bash/structure.sh
  ps1: ../../scripts/powershell/structure.ps1
---

# /structure - 歌曲结构设计系统 🏗️

> **核心理念**: 根据歌曲类型和时长,设计合理的段落结构
> **目标**: 完成 structure.json,明确每个段落的顺序、时长和功能

---

## 运行脚本 ⚠️ 必须执行

```bash
bash scripts/bash/structure.sh
```

脚本会创建标准流行歌曲结构模板,AI引导用户根据歌曲类型调整。

---

## 标准结构说明

### 流行歌曲标准结构

```
Intro → Verse 1 → Pre-Chorus → Chorus → 
Verse 2 → Chorus → Bridge → Chorus → Outro
```

### 段落功能说明

- **Intro**: 引入,营造氛围 (可选,10-20秒)
- **Verse**: 铺陈叙事,建立情感 (30-45秒)
- **Pre-Chorus**: 情绪过渡,为chorus蓄力 (可选,15-20秒)
- **Chorus**: 核心情感,最朗朗上口 (30-40秒)
- **Bridge**: 新视角,情感转折 (可选,20-30秒)
- **Outro**: 收尾,余韵 (可选,10-20秒)

---

## 根据类型调整

### 说唱结构

```
Hook → Verse 1 → Hook → Verse 2 → Bridge → Hook
```

- **Hook**: 相当于流行歌的Chorus
- **Verse**: 说唱部分,可以较长

### 民谣结构

```
Intro → Verse 1 → Chorus → Verse 2 → Chorus → 
Verse 3 → Chorus → Outro
```

- 可能没有Bridge
- Verse和Chorus循环

### 电子音乐结构

```
Intro → Build → Drop → Break → Build → Drop → Outro
```

- 重点在Drop部分

---

## 引导调整

AI根据用户的歌曲类型和时长,引导调整结构:

```
📋 你的歌曲信息:
  • 类型: [song_type]
  • 时长: [duration]
  • 风格: [style]

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

我已经为你创建了标准结构,是否需要调整?

常见调整:
  1. 去掉 Intro (直接开始)
  2. 去掉 Pre-Chorus (简化结构)
  3. 增加 Verse (更多叙事空间)
  4. 去掉 Bridge (保持简洁)

是否要调整?
  1. 使用标准结构
  2. 我要调整

请选择:
```

---

**开始执行** ⬇️

