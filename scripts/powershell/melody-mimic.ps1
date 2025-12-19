# 旋律风格学习助手 - MIDI 分析和旋律创作

. "$PSScriptRoot\common.ps1"

$projectDir = Get-CurrentProject
$projectName = Get-ProjectName

# 创建必要目录
Ensure-Directory -Path (Join-Path $projectDir "workspace")
Ensure-Directory -Path (Join-Path $projectDir "workspace\references")
Ensure-Directory -Path (Join-Path $projectDir "workspace\output")

# 解析参数
$songName = ""
for ($i = 0; $i -lt $args.Length; $i++) {
    if ($args[$i] -eq "--song" -and $i+1 -lt $args.Length) {
        $songName = $args[$i+1]
    } elseif ($args[$i] -eq "--project") {
        # 跳过项目参数
    } elseif (![string]::IsNullOrWhiteSpace($args[$i]) -and !$args[$i].StartsWith("--") -and [string]::IsNullOrWhiteSpace($songName)) {
        $songName = $args[$i]
    }
}

# 如果没有提供歌曲名,扫描 references 目录
if ([string]::IsNullOrWhiteSpace($songName)) {
    $referencesDir = Join-Path $projectDir "workspace\references"
    $availableSongs = @()

    if (Test-Path $referencesDir) {
        $songDirs = Get-ChildItem -Path $referencesDir -Directory
        foreach ($songDir in $songDirs) {
            $songDirName = $songDir.Name
            $midiFile = Join-Path $songDir.FullName "$songDirName.mid"
            $lyricsFile = Join-Path $songDir.FullName "$songDirName.txt"

            # 检查是否同时存在 MIDI 和歌词文件
            if ((Test-Path $midiFile) -and (Test-Path $lyricsFile)) {
                $availableSongs += $songDirName
            }
        }
    }

    if ($availableSongs.Count -eq 0) {
        $result = @{
            status = "no_songs_found"
            project_name = $projectName
            references_dir = $referencesDir
            message = "未找到参考歌曲文件"
            required_structure = "workspace/references/{song-name}/{song-name}.mid + {song-name}.txt"
            action = "请按要求准备参考文件"
        }
        Write-Output ($result | ConvertTo-Json -Depth 10 -Compress)
        exit 1
    } elseif ($availableSongs.Count -eq 1) {
        $songName = $availableSongs[0]
    } else {
        # 多个歌曲,让用户选择
        $result = @{
            status = "multiple_songs_found"
            project_name = $projectName
            available_songs = $availableSongs
            message = "找到多个参考歌曲，请选择一个"
            action = "请指定歌曲名: pwsh scripts/powershell/melody-mimic.ps1 [歌曲名]"
        }
        Write-Output ($result | ConvertTo-Json -Depth 10 -Compress)
        exit 0
    }
}

# 设置文件路径
$songDir = Join-Path $projectDir "workspace\references\$songName"
$midiFile = Join-Path $songDir "$songName.mid"
$lyricsFile = Join-Path $songDir "$songName.txt"
$outputDir = Join-Path $projectDir "workspace\output"

# 检查文件存在性
$missingFiles = @()
if (!(Test-Path $midiFile)) {
    $missingFiles += "MIDI 文件: $midiFile"
}
if (!(Test-Path $lyricsFile)) {
    $missingFiles += "歌词文件: $lyricsFile"
}

if ($missingFiles.Count -gt 0) {
    $result = @{
        status = "missing_files"
        project_name = $projectName
        song_name = $songName
        missing_files = $missingFiles
        expected_structure = @{
            directory = $songDir
            midi_file = "$songName.mid"
            lyrics_file = "$songName.txt"
        }
        message = "缺少必需文件"
        action = "请按要求准备参考文件"
    }
    Write-Output ($result | ConvertTo-Json -Depth 10 -Compress)
    exit 1
}

# 读取歌词文件内容并分析结构
$lyricsContent = Get-Content $lyricsFile -Raw
$cleanedContent = $lyricsContent -replace '[\n\r\t ]', '' -replace '[，。！？、；：""''（）【】《》…—]', ''
$lyricsWordCount = $cleanedContent.Length

# 检查歌词文件格式
$hasSections = $lyricsContent -match '\[.*\]'

# 获取 MIDI 文件信息
$midiFileInfo = Get-Item $midiFile
$midiSize = $midiFileInfo.Length

# 检查输出目录中是否已有分析结果
$analysisFile = Join-Path $outputDir "$songName-analysis.json"
$hasExistingAnalysis = Test-Path $analysisFile

# 输出状态信息
$result = @{
    status = "ready"
    action = "analyze"
    project_name = $projectName
    song_name = $songName
    files = @{
        midi_path = $midiFile
        lyrics_path = $lyricsFile
        midi_size = "$midiSize bytes"
        lyrics_word_count = $lyricsWordCount
        has_section_markers = $hasSections
    }
    directories = @{
        song_dir = $songDir
        output_dir = $outputDir
        analysis_file = $analysisFile
    }
    analysis = @{
        has_existing = $hasExistingAnalysis
        status = if ($hasExistingAnalysis) { "found_previous" } else { "new_analysis" }
    }
    message = "参考文件检查完成，准备开始分析"
    next_steps = @(
        "1. 解析 MIDI 文件，识别音轨结构",
        "2. 匹配人声音轨与歌词字数",
        "3. 分析旋律特征（节奏型、音程分布、调式）",
        "4. 引导歌词创作流程",
        "5. 生成原创旋律"
    )
    copyright_reminder = "⚠️ 此工具仅供学习参考，请注意版权风险"
}

Write-Output ($result | ConvertTo-Json -Depth 10 -Compress)