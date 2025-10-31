# 旋律提示

. "$PSScriptRoot\common.ps1"

$projectDir = Get-CurrentProject
$projectName = Get-ProjectName

$specFile = Test-SpecExists
$lyricsFile = Join-Path $projectDir "lyrics.md"
$structureFile = Join-Path $projectDir "structure.json"

if (-not (Test-Path $lyricsFile)) {
    $error = @{
        status = "error"
        message = "未找到 lyrics.md。请先创作歌词。"
    }
    Write-Output ($error | ConvertTo-Json -Compress)
    exit 1
}

$specContent = Get-Content $specFile | ConvertFrom-Json
$lyricsContent = Get-Content $lyricsFile -Raw

if (Test-Path $structureFile) {
    $structureContent = Get-Content $structureFile | ConvertFrom-Json
} else {
    $structureContent = @{}
}

$moodFile = Join-Path $projectDir "mood.json"
if (Test-Path $moodFile) {
    $moodContent = Get-Content $moodFile | ConvertFrom-Json
} else {
    $moodContent = @{}
}

$result = @{
    status = "success"
    project_name = $projectName
    spec = $specContent
    structure = $structureContent
    mood = $moodContent
    lyrics_content = (ConvertTo-JsonString -InputString $lyricsContent)
    message = "AI 应根据歌曲类型、风格和情绪生成旋律提示"
    hint_categories = @(
        "vocal_range (音域范围)",
        "tempo (建议BPM)",
        "rhythm_pattern (节奏型)",
        "melody_direction (旋律走向)",
        "emphasis_points (重音位置)",
        "reference_songs (参考歌曲)"
    )
}
Write-Output ($result | ConvertTo-Json -Depth 10 -Compress)

