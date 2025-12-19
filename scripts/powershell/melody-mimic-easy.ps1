# 旋律风格学习助手 (简易版) - 支持 MP3 输入
# 自动将 MP3 转换为 MIDI，然后进行旋律分析

. "$PSScriptRoot\common.ps1"

$projectDir = Get-CurrentProject
$projectName = Get-ProjectName

# 创建必要目录
Ensure-Directory -Path (Join-Path $projectDir "workspace")
Ensure-Directory -Path (Join-Path $projectDir "workspace\references")
Ensure-Directory -Path (Join-Path $projectDir "workspace\output")

# 获取 skills 脚本目录
$skillsScriptsDir = Join-Path $PSScriptRoot "..\..\skills\scripts"

# 解析参数
$songName = ""
$forceConvert = $false
$checkMode = $false

for ($i = 0; $i -lt $args.Length; $i++) {
    if ($args[$i] -eq "--song" -and $i+1 -lt $args.Length) {
        $songName = $args[$i+1]
    } elseif ($args[$i] -eq "--force") {
        $forceConvert = $true
    } elseif ($args[$i] -eq "--check") {
        $checkMode = $true
    } elseif ($args[$i] -eq "--project") {
        # 跳过项目参数
    } elseif (![string]::IsNullOrWhiteSpace($args[$i]) -and !$args[$i].StartsWith("--") -and [string]::IsNullOrWhiteSpace($songName)) {
        $songName = $args[$i]
    }
}

# 检查 Python 是否可用
function Get-PythonCommand {
    $pythonCmds = @("python3", "python")
    foreach ($cmd in $pythonCmds) {
        try {
            $version = & $cmd --version 2>&1
            if ($version -match "Python 3") {
                return $cmd
            }
        } catch {
            continue
        }
    }
    return $null
}

$pythonCmd = Get-PythonCommand

# 检查模式
if ($checkMode) {
    if ($null -eq $pythonCmd) {
        $result = @{
            status = "error"
            error = "Python3 未安装"
            message = "请先安装 Python3"
        }
        Write-Output ($result | ConvertTo-Json -Depth 10 -Compress)
        exit 1
    }

    $audioToMidiScript = Join-Path $skillsScriptsDir "audio_to_midi.py"
    & $pythonCmd $audioToMidiScript --check
    exit $LASTEXITCODE
}

# 扫描 references 目录，查找 MP3 和 MIDI 文件
function Scan-References {
    $referencesDir = Join-Path $projectDir "workspace\references"
    $songsWithMp3 = @()
    $songsWithMidi = @()
    $songsNeedConvert = @()

    if (Test-Path $referencesDir) {
        $songDirs = Get-ChildItem -Path $referencesDir -Directory -ErrorAction SilentlyContinue
        foreach ($songDir in $songDirs) {
            $songDirName = $songDir.Name
            $mp3File = Join-Path $songDir.FullName "$songDirName.mp3"
            $midiFile = Join-Path $songDir.FullName "$songDirName.mid"
            $lyricsFile = Join-Path $songDir.FullName "$songDirName.txt"

            $hasMp3 = Test-Path $mp3File
            $hasMidi = Test-Path $midiFile
            $hasLyrics = Test-Path $lyricsFile

            if ($hasMidi -and $hasLyrics) {
                $songsWithMidi += $songDirName
            } elseif ($hasMp3 -and $hasLyrics) {
                $songsWithMp3 += $songDirName
                $songsNeedConvert += $songDirName
            }
        }
    }

    return @{
        mp3 = $songsWithMp3
        midi = $songsWithMidi
        need_convert = $songsNeedConvert
    }
}

# 如果没有提供歌曲名，扫描目录
if ([string]::IsNullOrWhiteSpace($songName)) {
    $referencesDir = Join-Path $projectDir "workspace\references"
    $scanResult = Scan-References

    $mp3Count = $scanResult.mp3.Count
    $midiCount = $scanResult.midi.Count
    $totalCount = $mp3Count + $midiCount

    if ($totalCount -eq 0) {
        $result = @{
            status = "no_songs_found"
            project_name = $projectName
            references_dir = $referencesDir
            message = "未找到参考歌曲文件"
            supported_formats = @("MP3 + 歌词", "MIDI + 歌词")
            required_structure = "workspace/references/{song-name}/{song-name}.mp3 (或 .mid) + {song-name}.txt"
            action = "请按要求准备参考文件"
        }
        Write-Output ($result | ConvertTo-Json -Depth 10 -Compress)
        exit 1
    }

    # 如果只有一首歌，自动选择
    if ($totalCount -eq 1) {
        if ($midiCount -eq 1) {
            $songName = $scanResult.midi[0]
        } else {
            $songName = $scanResult.mp3[0]
        }
    } else {
        # 多首歌曲，让用户选择
        $result = @{
            status = "multiple_songs_found"
            project_name = $projectName
            scan_result = $scanResult
            songs_ready = $scanResult.midi
            songs_need_convert = $scanResult.mp3
            message = "找到多个参考歌曲"
            action = "请指定歌曲名: /melody-mimic-easy [歌曲名]"
        }
        Write-Output ($result | ConvertTo-Json -Depth 10 -Compress)
        exit 0
    }
}

# 设置文件路径
$songDir = Join-Path $projectDir "workspace\references\$songName"
$mp3File = Join-Path $songDir "$songName.mp3"
$midiFile = Join-Path $songDir "$songName.mid"
$lyricsFile = Join-Path $songDir "$songName.txt"
$outputDir = Join-Path $projectDir "workspace\output"

# 检查歌词文件
if (!(Test-Path $lyricsFile)) {
    $result = @{
        status = "missing_lyrics"
        project_name = $projectName
        song_name = $songName
        expected_file = $lyricsFile
        message = "缺少歌词文件"
        action = "请创建歌词文件: $songName.txt"
    }
    Write-Output ($result | ConvertTo-Json -Depth 10 -Compress)
    exit 1
}

# 检查是否需要转换 MP3
$needsConversion = $false
if (!(Test-Path $midiFile)) {
    if (Test-Path $mp3File) {
        $needsConversion = $true
    } else {
        $result = @{
            status = "missing_audio"
            project_name = $projectName
            song_name = $songName
            expected_files = @("$songName.mp3", "$songName.mid")
            message = "缺少音频文件 (MP3 或 MIDI)"
            action = "请提供 MP3 或 MIDI 文件"
        }
        Write-Output ($result | ConvertTo-Json -Depth 10 -Compress)
        exit 1
    }
}

# 如果需要转换 MP3
if ($needsConversion -or $forceConvert) {
    # 检查 Python
    if ($null -eq $pythonCmd) {
        $result = @{
            status = "python_not_found"
            project_name = $projectName
            song_name = $songName
            message = "需要 Python3 来转换 MP3"
            action = "请安装 Python3，或使用在线工具转换"
            online_tool = "https://basicpitch.spotify.com"
        }
        Write-Output ($result | ConvertTo-Json -Depth 10 -Compress)
        exit 1
    }

    # 检查转换依赖
    $audioToMidiScript = Join-Path $skillsScriptsDir "audio_to_midi.py"
    $depCheckOutput = & $pythonCmd $audioToMidiScript --check 2>&1

    try {
        $depCheck = $depCheckOutput | ConvertFrom-Json
        $depStatus = $depCheck.status
    } catch {
        $depStatus = "error"
        $depCheck = @{
            status = "error"
            error = "无法解析依赖检查结果"
        }
    }

    if ($depStatus -ne "ready") {
        # 获取硬件信息
        $hardwareDevice = if ($depCheck.hardware) { $depCheck.hardware.device } else { "cpu" }
        $hardwareDesc = if ($depCheck.hardware) { $depCheck.hardware.description } else { "CPU 模式" }
        $estimatedTime = if ($depCheck.hardware) { $depCheck.hardware.estimated_time } else { "5-15 分钟" }

        $result = @{
            status = "need_dependencies"
            project_name = $projectName
            song_name = $songName
            mp3_file = $mp3File
            dependency_check = $depCheck
            message = "需要安装依赖来转换 MP3 为 MIDI"
            install_command = "pip install demucs basic-pitch"
            hardware = @{
                device = $hardwareDevice
                description = $hardwareDesc
                estimated_time = $estimatedTime
            }
            alternatives = @(
                @{
                    name = "在线工具"
                    url = "https://basicpitch.spotify.com"
                    description = "上传 MP3，下载 MIDI，放入 $songDir"
                }
            )
            action = "安装依赖后重新运行，或使用在线工具"
        }
        Write-Output ($result | ConvertTo-Json -Depth 10 -Compress)
        exit 1
    }

    # 依赖已就绪，获取硬件信息
    $hardwareDevice = if ($depCheck.hardware) { $depCheck.hardware.device } else { "cpu" }
    $hardwareDesc = if ($depCheck.hardware) { $depCheck.hardware.description } else { "CPU 模式" }
    $estimatedTime = if ($depCheck.hardware) { $depCheck.hardware.estimated_time } else { "5-15 分钟" }

    # 输出转换准备状态
    $result = @{
        status = "ready_to_convert"
        action = "convert_mp3"
        project_name = $projectName
        song_name = $songName
        files = @{
            mp3_path = $mp3File
            lyrics_path = $lyricsFile
            output_midi = $midiFile
        }
        hardware = @{
            device = $hardwareDevice
            description = $hardwareDesc
            estimated_time = $estimatedTime
        }
        conversion_steps = @(
            "1. 使用 Demucs 分离人声音轨",
            "2. 使用 Basic Pitch 将人声转为 MIDI",
            "3. 进入旋律分析流程"
        )
        message = "准备将 MP3 转换为 MIDI"
        confirm_prompt = "预计耗时 $estimatedTime，是否开始转换?"
        python_command = "$pythonCmd `"$audioToMidiScript`" `"$mp3File`" `"$songDir`""
    }
    Write-Output ($result | ConvertTo-Json -Depth 10 -Compress)
    exit 0
}

# MIDI 文件已存在，直接进入分析流程
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

# 输出状态信息 (与 melody-mimic.ps1 兼容)
$result = @{
    status = "ready"
    action = "analyze"
    project_name = $projectName
    song_name = $songName
    source = "midi"
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
        "4. 生成双报告（旋律特征 + 歌词分析）",
        "5. 引导歌词创作流程",
        "6. 生成原创旋律"
    )
    copyright_reminder = "⚠️ 此工具仅供学习参考，请注意版权风险"
}

Write-Output ($result | ConvertTo-Json -Depth 10 -Compress)
