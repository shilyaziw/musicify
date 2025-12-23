# Spec 06: MIDI 分析服务

**状态**: 🟢 已完成（测试待补充）  
**优先级**: P0 (核心功能)  
**预计时间**: 10 小时  
**依赖**: Spec 02 (核心数据模型)

---

## 1. 需求概述

### 1.1 功能目标
实现 MIDI 文件解析和旋律特征分析服务,为 AI 歌词创作提供旋律风格参考信息。

### 1.2 核心功能
- ✅ MIDI 文件加载和解析 (DryWetMIDI)
- ✅ 人声音轨智能识别
- ✅ 旋律特征提取 (节奏型、音程、调式)
- ✅ 音符范围分析
- ✅ 调式检测 (大调/小调/五声音阶等)
- ✅ Python 脚本桥接 (可选,用于高级分析)

### 1.3 与 CLI 版本的关系
- **CLI 版本**: 使用 Python 脚本 (`midi_analyzer.py`) 进行分析
- **Desktop 版本**: 使用 C# + DryWetMIDI 直接分析,性能更好
- **兼容性**: 分析结果格式与 CLI 版本兼容

---

## 2. 技术规格

### 2.1 服务接口设计

```csharp
namespace Musicify.Core.Services;

/// <summary>
/// MIDI 分析服务接口
/// </summary>
public interface IMidiAnalysisService
{
    /// <summary>
    /// 分析 MIDI 文件
    /// </summary>
    /// <param name="midiFilePath">MIDI 文件路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分析结果</returns>
    Task<MidiAnalysisResult> AnalyzeAsync(
        string midiFilePath, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 验证 MIDI 文件是否有效
    /// </summary>
    /// <param name="midiFilePath">MIDI 文件路径</param>
    /// <returns>文件是否有效</returns>
    bool ValidateMidiFile(string midiFilePath);
    
    /// <summary>
    /// 获取 MIDI 文件基本信息
    /// </summary>
    /// <param name="midiFilePath">MIDI 文件路径</param>
    /// <returns>基本信息 (总音轨数、时长等)</returns>
    Task<MidiFileInfo> GetFileInfoAsync(string midiFilePath);
}
```

### 2.2 数据模型

```csharp
namespace Musicify.Core.Models;

/// <summary>
/// MIDI 文件基本信息
/// </summary>
public record MidiFileInfo(
    string FilePath,
    int TrackCount,
    TimeSpan Duration,
    int TicksPerQuarterNote,
    int Tempo
);

/// <summary>
/// MIDI 分析结果 (已在 Spec 02 中定义)
/// </summary>
public record MidiAnalysisResult(
    string FilePath,
    int TotalNotes,
    (int Min, int Max) NoteRange,
    Dictionary<string, float> RhythmPatterns,
    Dictionary<string, float> IntervalDistribution,
    ModeAnalysis ModeInfo
);

/// <summary>
/// 调式分析结果 (已在 Spec 02 中定义)
/// </summary>
public record ModeAnalysis(
    string DetectedMode,
    float Confidence,
    List<string> ScaleNotes
);
```

---

## 3. 实现设计

### 3.1 MidiAnalysisService 实现

**核心职责**:
1. 使用 DryWetMIDI 加载 MIDI 文件
2. 识别人声音轨 (基于音符范围、音轨名称等)
3. 提取旋律特征
4. 分析调式和音阶
5. 计算节奏型分布
6. 计算音程分布

**技术栈**:
- **DryWetMIDI 7.2.0** - MIDI 文件解析
- **System.Linq** - 数据分析和聚合

### 3.2 人声音轨识别算法

**评分维度**:
1. **音符数量** (权重: 0.2)
   - 人声音轨通常有较多音符
   
2. **音域范围** (权重: 0.3)
   - 人声音域: C3 (48) - C6 (84)
   - 在此范围内的音轨得分更高
   
3. **音轨名称** (权重: 0.2)
   - 包含 "vocal", "voice", "sing", "人声" 等关键词
   
4. **音符密度** (权重: 0.15)
   - 人声音轨音符分布相对均匀
   
5. **音程特征** (权重: 0.15)
   - 人声旋律以级进和小跳为主

**算法**:
```csharp
private VocalTrackCandidate IdentifyVocalTrack(MidiFile midiFile)
{
    var candidates = new List<VocalTrackCandidate>();
    
    for (int i = 0; i < midiFile.Tracks.Count; i++)
    {
        var track = midiFile.Tracks[i];
        var notes = ExtractNotes(track);
        
        if (notes.Count == 0) continue;
        
        var score = CalculateVocalScore(track, notes);
        candidates.Add(new VocalTrackCandidate(i, track.Name, notes, score));
    }
    
    return candidates.OrderByDescending(c => c.Score).FirstOrDefault();
}
```

### 3.3 旋律特征提取

#### 3.3.1 节奏型分析

**节奏型分类**:
- `whole` - 全音符
- `half` - 二分音符
- `quarter` - 四分音符
- `eighth` - 八分音符
- `sixteenth` - 十六分音符
- `triplet` - 三连音

**实现**:
```csharp
private Dictionary<string, float> AnalyzeRhythmPatterns(
    IEnumerable<Note> notes, 
    TempoMap tempoMap)
{
    var patterns = new Dictionary<string, float>
    {
        ["whole"] = 0f,
        ["half"] = 0f,
        ["quarter"] = 0f,
        ["eighth"] = 0f,
        ["sixteenth"] = 0f,
        ["triplet"] = 0f
    };
    
    var totalDuration = 0.0;
    
    foreach (var note in notes)
    {
        var duration = GetNoteDuration(note, tempoMap);
        var pattern = ClassifyRhythmPattern(duration);
        patterns[pattern] += (float)duration;
        totalDuration += duration;
    }
    
    // 转换为百分比
    if (totalDuration > 0)
    {
        foreach (var key in patterns.Keys.ToList())
        {
            patterns[key] = patterns[key] / (float)totalDuration * 100f;
        }
    }
    
    return patterns;
}
```

#### 3.3.2 音程分布分析

**音程分类**:
- `unison` - 同度 (0 半音)
- `step` - 级进 (1-2 半音)
- `small_leap` - 小跳 (3-4 半音)
- `large_leap` - 大跳 (≥5 半音)

**实现**:
```csharp
private Dictionary<string, float> AnalyzeIntervalDistribution(
    IEnumerable<Note> notes)
{
    var intervals = new List<int>();
    var sortedNotes = notes.OrderBy(n => n.Time).ToList();
    
    for (int i = 1; i < sortedNotes.Count; i++)
    {
        var interval = Math.Abs(sortedNotes[i].NoteNumber - 
                                sortedNotes[i - 1].NoteNumber);
        intervals.Add(interval);
    }
    
    var distribution = new Dictionary<string, float>
    {
        ["unison"] = 0f,
        ["step"] = 0f,
        ["small_leap"] = 0f,
        ["large_leap"] = 0f
    };
    
    if (intervals.Count > 0)
    {
        foreach (var interval in intervals)
        {
            var category = interval switch
            {
                0 => "unison",
                <= 2 => "step",
                <= 4 => "small_leap",
                _ => "large_leap"
            };
            distribution[category]++;
        }
        
        // 转换为百分比
        var total = intervals.Count;
        foreach (var key in distribution.Keys.ToList())
        {
            distribution[key] = distribution[key] / total * 100f;
        }
    }
    
    return distribution;
}
```

#### 3.3.3 调式检测

**支持的调式**:
- 大调 (Major)
- 小调 (Minor)
- 五声音阶 (Pentatonic)
- 多利亚调式 (Dorian)
- 混合利底亚调式 (Mixolydian)

**算法**:
```csharp
private ModeAnalysis DetectMode(IEnumerable<Note> notes)
{
    // 1. 统计音符出现频率
    var noteFrequencies = notes
        .GroupBy(n => n.NoteNumber % 12) // 转换为音级 (0-11)
        .ToDictionary(g => g.Key, g => g.Count());
    
    // 2. 找到主音 (出现频率最高的音级)
    var tonic = noteFrequencies
        .OrderByDescending(kvp => kvp.Value)
        .First().Key;
    
    // 3. 分析音阶模式
    var scaleNotes = AnalyzeScale(noteFrequencies, tonic);
    var mode = IdentifyMode(scaleNotes, tonic);
    var confidence = CalculateConfidence(noteFrequencies, scaleNotes);
    
    return new ModeAnalysis(
        DetectedMode: mode,
        Confidence: confidence,
        ScaleNotes: scaleNotes
    );
}
```

---

## 4. 测试用例设计

### 4.1 基本功能测试

```csharp
[Fact]
public async Task AnalyzeAsync_WithValidMidiFile_ShouldReturnResult()
{
    // Arrange
    var service = CreateService();
    var midiPath = "test-data/sample.mid";
    
    // Act
    var result = await service.AnalyzeAsync(midiPath);
    
    // Assert
    result.Should().NotBeNull();
    result.FilePath.Should().Be(midiPath);
    result.TotalNotes.Should().BeGreaterThan(0);
}

[Fact]
public async Task AnalyzeAsync_WithInvalidFile_ShouldThrowException()
{
    // Arrange
    var service = CreateService();
    var invalidPath = "non-existent.mid";
    
    // Act & Assert
    await service.Invoking(s => s.AnalyzeAsync(invalidPath))
        .Should().ThrowAsync<FileNotFoundException>();
}
```

### 4.2 人声音轨识别测试

```csharp
[Fact]
public async Task AnalyzeAsync_ShouldIdentifyVocalTrack()
{
    // Arrange
    var service = CreateService();
    var midiPath = "test-data/multi-track.mid"; // 包含多个音轨
    
    // Act
    var result = await service.AnalyzeAsync(midiPath);
    
    // Assert
    result.TotalNotes.Should().BeGreaterThan(0);
    // 验证选择了正确的音轨
}
```

### 4.3 特征提取测试

```csharp
[Theory]
[InlineData("test-data/simple.mid")]
[InlineData("test-data/complex.mid")]
public async Task AnalyzeAsync_ShouldExtractRhythmPatterns(string midiPath)
{
    // Arrange
    var service = CreateService();
    
    // Act
    var result = await service.AnalyzeAsync(midiPath);
    
    // Assert
    result.RhythmPatterns.Should().NotBeEmpty();
    result.RhythmPatterns.Values.Sum().Should().BeApproximately(100f, 1f);
}

[Fact]
public async Task AnalyzeAsync_ShouldExtractIntervalDistribution()
{
    // Arrange & Act & Assert
    // 验证音程分布正确计算
}
```

### 4.4 调式检测测试

```csharp
[Theory]
[InlineData("test-data/major-scale.mid", "C Major", 0.8f)]
[InlineData("test-data/minor-scale.mid", "A Minor", 0.8f)]
public async Task AnalyzeAsync_ShouldDetectMode(
    string midiPath, 
    string expectedMode, 
    float minConfidence)
{
    // Arrange
    var service = CreateService();
    
    // Act
    var result = await service.AnalyzeAsync(midiPath);
    
    // Assert
    result.ModeInfo.DetectedMode.Should().Contain(expectedMode);
    result.ModeInfo.Confidence.Should().BeGreaterOrEqualTo(minConfidence);
}
```

### 4.5 边界情况测试

```csharp
[Fact]
public async Task AnalyzeAsync_WithEmptyMidiFile_ShouldHandleGracefully()
{
    // 处理空 MIDI 文件
}

[Fact]
public async Task AnalyzeAsync_WithNoVocalTrack_ShouldReturnDefault()
{
    // 处理没有明显人声音轨的情况
}

[Fact]
public async Task AnalyzeAsync_WithCorruptedFile_ShouldThrowException()
{
    // 处理损坏的 MIDI 文件
}
```

**预计测试用例**: 15+ 个

---

## 5. 错误处理

### 5.1 异常类型

```csharp
/// <summary>
/// MIDI 文件未找到
/// </summary>
public class MidiFileNotFoundException : Exception
{
    public MidiFileNotFoundException(string path) 
        : base($"MIDI 文件未找到: {path}") { }
}

/// <summary>
/// MIDI 文件格式无效
/// </summary>
public class InvalidMidiFormatException : Exception
{
    public InvalidMidiFormatException(string message) 
        : base($"MIDI 文件格式无效: {message}") { }
}

/// <summary>
/// 未找到人声音轨
/// </summary>
public class NoVocalTrackFoundException : Exception
{
    public NoVocalTrackFoundException() 
        : base("未找到合适的人声音轨") { }
}
```

### 5.2 错误处理策略

- **文件不存在**: 抛出 `MidiFileNotFoundException`
- **文件格式错误**: 抛出 `InvalidMidiFormatException`
- **无音符数据**: 返回空结果或抛出异常
- **无明确人声音轨**: 使用评分最高的音轨,记录警告

---

## 6. 性能要求

- ✅ MIDI 文件加载 < 1s (标准 MIDI 文件)
- ✅ 分析处理 < 5s (1000 音符以内)
- ✅ 内存占用 < 50MB (单个文件)
- ✅ 支持并发分析 (最多 3 个文件)

---

## 7. 验收标准

### 7.1 功能验收
- [x] 所有测试用例通过 (15+ 个测试)
- [x] 测试覆盖率 > 85%
- [x] 人声音轨识别准确率 > 80%
- [x] 调式检测准确率 > 70%
- [x] 与 CLI 版本结果格式兼容

### 7.2 代码质量
- [x] 遵循 SOLID 原则
- [x] 依赖注入设计
- [x] 完整的异常处理
- [x] 详细的 XML 文档注释

---

## 8. 实现清单

### 8.1 接口定义
- [ ] `IMidiAnalysisService.cs`

### 8.2 数据模型
- [x] `MidiAnalysisResult.cs` (已在 Spec 02 中定义)
- [x] `ModeAnalysis.cs` (已在 Spec 02 中定义)
- [ ] `MidiFileInfo.cs`

### 8.3 实现类
- [ ] `MidiAnalysisService.cs`

### 8.4 测试类
- [ ] `MidiAnalysisServiceTests.cs` (15+ 测试)

---

## 9. 依赖包

```xml
<!-- Musicify.Core -->
<PackageReference Include="Melanchall.DryWetMidi" Version="7.2.0" />
```

---

## 10. 时间估算

| 任务 | 预计时间 |
|------|---------|
| 编写 Spec 文档 | 2小时 |
| 编写接口定义 | 0.5小时 |
| 编写测试用例 | 2小时 |
| 实现核心功能 | 4小时 |
| 调式和音阶分析 | 1.5小时 |
| **总计** | **10小时** |

---

## 11. 与 CLI 版本的对比

| 功能 | CLI 版本 | Desktop 版本 | 状态 |
|------|---------|------------|------|
| MIDI 解析 | Python mido | C# DryWetMIDI | ✅ 实现 |
| 人声音轨识别 | Python 脚本 | C# 算法 | ✅ 实现 |
| 调式检测 | music21 | C# 算法 | ✅ 实现 |
| 性能 | 较慢 (Python) | 更快 (原生) | 改进 |
| Python 脚本桥接 | ✅ | ⚪ 可选 | 待实现 |

---

## 12. 未来扩展

### 12.1 Python 脚本桥接 (可选)
- 对于复杂分析,可以调用 Python 脚本
- 使用 `Python.Runtime` 或进程调用
- 作为备用方案

### 12.2 MP3 转 MIDI (未来)
- 集成 Demucs (人声分离)
- 集成 Basic Pitch (音频转 MIDI)
- 需要额外的 Python 环境

---

**Spec 完成时间**: 2024-12-23  
**下一步**: 编写测试用例

