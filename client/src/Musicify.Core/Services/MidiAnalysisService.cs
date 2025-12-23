using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Musicify.Core.Models;
using Note = Melanchall.DryWetMidi.Interaction.Note;

namespace Musicify.Core.Services;

/// <summary>
/// MIDI 分析服务实现
/// </summary>
public class MidiAnalysisService : IMidiAnalysisService
{
    /// <summary>
    /// 人声音域范围 (MIDI note numbers)
    /// C3 (48) - C6 (84)
    /// </summary>
    private static readonly (int Min, int Max) VocalRange = (48, 84);

    /// <summary>
    /// 人声音轨名称关键词
    /// </summary>
    private static readonly string[] VocalKeywords = 
    {
        "vocal", "voice", "sing", "melody", "lead",
        "人声", "主旋律", "主唱", "vocalist"
    };

    public async Task<MidiAnalysisResult> AnalyzeAsync(
        string midiFilePath, 
        CancellationToken cancellationToken = default)
    {
        if (!ValidateMidiFile(midiFilePath))
        {
            throw new FileNotFoundException($"MIDI 文件不存在或无效: {midiFilePath}");
        }

        return await Task.Run(() =>
        {
            // 加载 MIDI 文件
            var midiFile = MidiFile.Read(midiFilePath);

            // 识别人声音轨
            var vocalTrack = IdentifyVocalTrack(midiFile);
            if (vocalTrack == null)
            {
                throw new InvalidOperationException("未找到合适的人声音轨");
            }

            // 提取音符
            var notes = ExtractNotes(midiFile, vocalTrack.TrackIndex);

            if (notes.Count == 0)
            {
                throw new InvalidOperationException("人声音轨中未找到音符数据");
            }

            // 分析特征
            var noteRange = CalculateNoteRange(notes);
            var rhythmPatterns = AnalyzeRhythmPatterns(notes, midiFile);
            var intervalDistribution = AnalyzeIntervalDistribution(notes);
            var modeInfo = DetectMode(notes);

            return new MidiAnalysisResult(
                FilePath: midiFilePath,
                TotalNotes: notes.Count,
                NoteRange: noteRange,
                RhythmPatterns: rhythmPatterns,
                IntervalDistribution: intervalDistribution,
                ModeInfo: modeInfo
            );
        }, cancellationToken);
    }

    public bool ValidateMidiFile(string midiFilePath)
    {
        if (string.IsNullOrWhiteSpace(midiFilePath))
            return false;

        if (!File.Exists(midiFilePath))
            return false;

        try
        {
            // 尝试读取文件头验证格式
            var midiFile = MidiFile.Read(midiFilePath);
            return midiFile != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<MidiFileInfo> GetFileInfoAsync(string midiFilePath)
    {
        if (!ValidateMidiFile(midiFilePath))
        {
            throw new FileNotFoundException($"MIDI 文件不存在或无效: {midiFilePath}");
        }

        return await Task.Run(() =>
        {
            var midiFile = MidiFile.Read(midiFilePath);
            var tempoMap = midiFile.GetTempoMap();
            
            // 获取最后一个事件的时间
            var lastEvent = midiFile.GetTimedEvents().LastOrDefault();
            var totalTime = lastEvent != null 
                ? lastEvent.TimeAs<MetricTimeSpan>(tempoMap) 
                : new MetricTimeSpan(0);
            
            // 获取速度 (BPM)
            var tempoChange = tempoMap.GetTempoAtTime(new MidiTimeSpan(0));
            var tempo = tempoChange.BeatsPerMinute;

            // 将 MetricTimeSpan 转换为 TimeSpan
            var duration = TimeSpan.FromSeconds(totalTime.TotalSeconds);

            return new MidiFileInfo(
                FilePath: midiFilePath,
                TrackCount: midiFile.Chunks.Count,
                Duration: duration,
                TicksPerQuarterNote: midiFile.TimeDivision is TicksPerQuarterNoteTimeDivision tpqn 
                    ? tpqn.TicksPerQuarterNote 
                    : 480,
                Tempo: (int)tempo
            );
        });
    }

    /// <summary>
    /// 识别人声音轨
    /// </summary>
    private VocalTrackCandidate? IdentifyVocalTrack(MidiFile midiFile)
    {
        var candidates = new List<VocalTrackCandidate>();

        for (int i = 0; i < midiFile.Chunks.Count; i++)
        {
            if (midiFile.Chunks[i] is not TrackChunk trackChunk)
                continue;

            var notes = ExtractNotes(midiFile, i);
            if (notes.Count == 0)
                continue;

            var score = CalculateVocalScore(trackChunk, notes);
            var trackName = GetTrackName(trackChunk, i);
            var noteRange = CalculateNoteRange(notes);

            candidates.Add(new VocalTrackCandidate(
                TrackIndex: i,
                TrackName: trackName,
                NoteCount: notes.Count,
                NoteRange: noteRange,
                Score: score
            ));
        }

        return candidates.OrderByDescending(c => c.Score).FirstOrDefault();
    }

    /// <summary>
    /// 计算人声音轨评分
    /// </summary>
    private float CalculateVocalScore(TrackChunk track, List<Note> notes)
    {
        float score = 0f;
        var trackName = GetTrackName(track, -1).ToLowerInvariant();
        var noteRange = CalculateNoteRange(notes);

        // 1. 音轨名称匹配 (30分)
        if (VocalKeywords.Any(keyword => trackName.Contains(keyword)))
        {
            score += 30f;
        }

        // 2. 音域匹配 (25分)
        var rangeOverlap = CalculateRangeOverlap(noteRange, VocalRange);
        if (rangeOverlap > 0.7f)
        {
            score += 25f;
        }
        else if (rangeOverlap > 0.5f)
        {
            score += 15f;
        }

        // 3. 音符数量合理性 (20分)
        if (notes.Count >= 20 && notes.Count <= 200)
        {
            score += 20f;
        }
        else if (notes.Count > 10)
        {
            score += 10f;
        }

        // 4. 音符密度 (15分)
        var density = CalculateNoteDensity(notes);
        if (density > 0.3f && density < 0.8f)
        {
            score += 15f;
        }

        // 5. 音程特征 (10分)
        var intervalVariety = CalculateIntervalVariety(notes);
        if (intervalVariety > 0.3f)
        {
            score += 10f;
        }

        return score;
    }

    /// <summary>
    /// 提取音轨中的音符
    /// </summary>
    private List<Note> ExtractNotes(MidiFile midiFile, int trackIndex)
    {
        if (trackIndex < 0 || trackIndex >= midiFile.Chunks.Count)
            return new List<Note>();

        if (midiFile.Chunks[trackIndex] is not TrackChunk trackChunk)
            return new List<Note>();

        return trackChunk.GetNotes().ToList();
    }

    /// <summary>
    /// 计算音符范围
    /// </summary>
    private (int Min, int Max) CalculateNoteRange(List<Note> notes)
    {
        if (notes.Count == 0)
            return (0, 0);

        var noteNumbers = notes.Select(n => (int)n.NoteNumber).ToList();
        return (noteNumbers.Min(), noteNumbers.Max());
    }

    /// <summary>
    /// 分析节奏型分布
    /// </summary>
    private Dictionary<string, float> AnalyzeRhythmPatterns(
        List<Note> notes, 
        MidiFile midiFile)
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

        if (notes.Count == 0)
            return patterns;

        var tempoMap = midiFile.GetTempoMap();
        var totalDuration = 0.0;

        foreach (var note in notes)
        {
            var length = note.LengthAs<MusicalTimeSpan>(tempoMap);
            var ticks = note.Length;
            var pattern = ClassifyRhythmPattern(ticks, midiFile.TimeDivision);
            
            if (patterns.ContainsKey(pattern))
            {
                patterns[pattern] += (float)ticks;
            }
            totalDuration += ticks;
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

    /// <summary>
    /// 分类节奏型
    /// </summary>
    private string ClassifyRhythmPattern(long ticks, TimeDivision timeDivision)
    {
        var ticksPerQuarter = timeDivision is TicksPerQuarterNoteTimeDivision tpqn 
            ? tpqn.TicksPerQuarterNote 
            : 480;

        var quarterRatio = (double)ticks / ticksPerQuarter;

        return quarterRatio switch
        {
            >= 4.0 => "whole",
            >= 2.0 => "half",
            >= 1.0 => "quarter",
            >= 0.5 => "eighth",
            >= 0.25 => "sixteenth",
            _ => "triplet"
        };
    }

    /// <summary>
    /// 分析音程分布
    /// </summary>
    private Dictionary<string, float> AnalyzeIntervalDistribution(List<Note> notes)
    {
        var distribution = new Dictionary<string, float>
        {
            ["unison"] = 0f,
            ["step"] = 0f,
            ["small_leap"] = 0f,
            ["large_leap"] = 0f
        };

        if (notes.Count < 2)
            return distribution;

        var intervals = new List<int>();
        var sortedNotes = notes.OrderBy(n => n.Time).ToList();

        for (int i = 1; i < sortedNotes.Count; i++)
        {
            var interval = Math.Abs((int)sortedNotes[i].NoteNumber - 
                                   (int)sortedNotes[i - 1].NoteNumber);
            intervals.Add(interval);
        }

        if (intervals.Count == 0)
            return distribution;

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

        return distribution;
    }

    /// <summary>
    /// 检测调式
    /// </summary>
    private ModeAnalysis DetectMode(List<Note> notes)
    {
        if (notes.Count == 0)
        {
            return new ModeAnalysis(
                DetectedMode: "Unknown",
                Confidence: 0f,
                ScaleNotes: new List<string>()
            );
        }

        // 统计音级出现频率 (0-11, C=0, C#=1, ..., B=11)
        var pitchClasses = notes
            .Select(n => (int)n.NoteNumber % 12)
            .GroupBy(pc => pc)
            .ToDictionary(g => g.Key, g => g.Count());

        // 找到主音 (出现频率最高的音级)
        var tonic = pitchClasses
            .OrderByDescending(kvp => kvp.Value)
            .First().Key;

        // 分析音阶模式
        var scaleNotes = AnalyzeScale(pitchClasses, tonic);
        var mode = IdentifyMode(scaleNotes, tonic);
        var confidence = CalculateModeConfidence(pitchClasses, scaleNotes);

        return new ModeAnalysis(
            DetectedMode: mode,
            Confidence: confidence,
            ScaleNotes: scaleNotes
        );
    }

    /// <summary>
    /// 分析音阶
    /// </summary>
    private List<string> AnalyzeScale(
        Dictionary<int, int> pitchClasses, 
        int tonic)
    {
        var scaleNotes = new List<string>();
        var noteNames = new[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        // 大调音阶模式: 0, 2, 4, 5, 7, 9, 11
        var majorScale = new[] { 0, 2, 4, 5, 7, 9, 11 };
        var majorMatches = majorScale.Count(step => 
            pitchClasses.ContainsKey((tonic + step) % 12));

        // 小调音阶模式: 0, 2, 3, 5, 7, 8, 10
        var minorScale = new[] { 0, 2, 3, 5, 7, 8, 10 };
        var minorMatches = minorScale.Count(step => 
            pitchClasses.ContainsKey((tonic + step) % 12));

        // 五声音阶模式: 0, 2, 4, 7, 9
        var pentatonicScale = new[] { 0, 2, 4, 7, 9 };
        var pentatonicMatches = pentatonicScale.Count(step => 
            pitchClasses.ContainsKey((tonic + step) % 12));

        // 选择匹配度最高的音阶
        if (majorMatches >= 5)
        {
            scaleNotes = majorScale.Select(step => 
                noteNames[(tonic + step) % 12]).ToList();
        }
        else if (minorMatches >= 5)
        {
            scaleNotes = minorScale.Select(step => 
                noteNames[(tonic + step) % 12]).ToList();
        }
        else if (pentatonicMatches >= 4)
        {
            scaleNotes = pentatonicScale.Select(step => 
                noteNames[(tonic + step) % 12]).ToList();
        }
        else
        {
            // 使用实际出现的音级
            scaleNotes = pitchClasses.Keys
                .OrderBy(pc => pc)
                .Select(pc => noteNames[pc])
                .ToList();
        }

        return scaleNotes;
    }

    /// <summary>
    /// 识别调式名称
    /// </summary>
    private string IdentifyMode(List<string> scaleNotes, int tonic)
    {
        var noteNames = new[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        var tonicName = noteNames[tonic];

        if (scaleNotes.Count == 7)
        {
            // 检查是否为大调
            var majorPattern = new[] { 0, 2, 4, 5, 7, 9, 11 };
            var isMajor = majorPattern.All(step => 
                scaleNotes.Contains(noteNames[(tonic + step) % 12]));
            
            if (isMajor)
                return $"{tonicName} Major";
        }

        if (scaleNotes.Count >= 5)
        {
            // 检查是否为小调
            var minorPattern = new[] { 0, 2, 3, 5, 7, 8, 10 };
            var isMinor = minorPattern.Take(scaleNotes.Count).All(step => 
                scaleNotes.Contains(noteNames[(tonic + step) % 12]));
            
            if (isMinor)
                return $"{tonicName} Minor";
        }

        if (scaleNotes.Count == 5)
        {
            // 五声音阶
            return $"{tonicName} Pentatonic";
        }

        return $"{tonicName} (Unknown Mode)";
    }

    /// <summary>
    /// 计算调式检测置信度
    /// </summary>
    private float CalculateModeConfidence(
        Dictionary<int, int> pitchClasses, 
        List<string> scaleNotes)
    {
        if (scaleNotes.Count == 0 || pitchClasses.Count == 0)
            return 0f;

        // 计算音阶音符在总音符中的占比
        var totalNotes = pitchClasses.Values.Sum();
        var scaleNoteCount = pitchClasses
            .Where(kvp => scaleNotes.Contains(GetNoteName(kvp.Key)))
            .Sum(kvp => kvp.Value);

        return Math.Min(1.0f, (float)scaleNoteCount / totalNotes);
    }

    /// <summary>
    /// 获取音级名称
    /// </summary>
    private string GetNoteName(int pitchClass)
    {
        var noteNames = new[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        return noteNames[pitchClass % 12];
    }

    /// <summary>
    /// 计算音域重叠度
    /// </summary>
    private float CalculateRangeOverlap(
        (int Min, int Max) range1, 
        (int Min, int Max) range2)
    {
        var overlapMin = Math.Max(range1.Min, range2.Min);
        var overlapMax = Math.Min(range1.Max, range2.Max);

        if (overlapMin > overlapMax)
            return 0f;

        var overlap = overlapMax - overlapMin + 1;
        var range1Size = range1.Max - range1.Min + 1;

        return (float)overlap / range1Size;
    }

    /// <summary>
    /// 计算音符密度
    /// </summary>
    private float CalculateNoteDensity(List<Note> notes)
    {
        if (notes.Count == 0)
            return 0f;

        var sortedNotes = notes.OrderBy(n => n.Time).ToList();
        var totalTime = sortedNotes.Last().Time + sortedNotes.Last().Length - sortedNotes.First().Time;
        
        if (totalTime == 0)
            return 0f;

        return (float)notes.Count / totalTime;
    }

    /// <summary>
    /// 计算音程变化丰富度
    /// </summary>
    private float CalculateIntervalVariety(List<Note> notes)
    {
        if (notes.Count < 2)
            return 0f;

        var intervals = new HashSet<int>();
        var sortedNotes = notes.OrderBy(n => n.Time).ToList();

        for (int i = 1; i < sortedNotes.Count; i++)
        {
            var interval = Math.Abs((int)sortedNotes[i].NoteNumber - 
                                   (int)sortedNotes[i - 1].NoteNumber);
            intervals.Add(interval);
        }

        return (float)intervals.Count / Math.Min(12, notes.Count - 1);
    }

    /// <summary>
    /// 获取音轨名称
    /// </summary>
    private string GetTrackName(TrackChunk track, int index)
    {
        // 查找 SequenceTrackNameEvent
        var nameEvent = track.Events
            .OfType<Melanchall.DryWetMidi.Core.SequenceTrackNameEvent>()
            .FirstOrDefault();

        return nameEvent?.Text ?? $"Track {index + 1}";
    }
}

/// <summary>
/// 人声音轨候选
/// </summary>
internal record VocalTrackCandidate(
    int TrackIndex,
    string TrackName,
    int NoteCount,
    (int Min, int Max) NoteRange,
    float Score
);

