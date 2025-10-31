# 歌词润色

. "$PSScriptRoot\common.ps1"

$projectDir = Get-CurrentProject
$projectName = Get-ProjectName

$lyricsFile = Join-Path $projectDir "lyrics.md"

if (-not (Test-Path $lyricsFile)) {
    $error = @{
        status = "error"
        message = "未找到 lyrics.md。请先创作歌词。"
    }
    Write-Output ($error | ConvertTo-Json -Compress)
    exit 1
}

# 解析参数
$focus = "all"
for ($i = 0; $i -lt $args.Length; $i++) {
    if ($args[$i] -eq "--focus" -and $i+1 -lt $args.Length) {
        $focus = $args[$i+1]
    }
}

$lyricsContent = Get-Content $lyricsFile -Raw
$specFile = Join-Path $projectDir "spec.json"
$themeFile = Join-Path $projectDir "theme.md"

if (Test-Path $specFile) {
    $specContent = Get-Content $specFile | ConvertFrom-Json
} else {
    $specContent = @{}
}

if (Test-Path $themeFile) {
    $themeContent = Get-Content $themeFile -Raw
} else {
    $themeContent = ""
}

$focusAreas = @{
    wording = "用词精准度优化"
    imagery = "意象丰富度提升"
    emotion = "情感表达优化"
    singability = "可唱性优化（避免拗口）"
    all = "全面润色"
}

$result = @{
    status = "success"
    project_name = $projectName
    lyrics_file = $lyricsFile
    focus = $focus
    lyrics_content = (ConvertTo-JsonString -InputString $lyricsContent)
    spec = $specContent
    theme = (ConvertTo-JsonString -InputString $themeContent)
    message = "AI 应根据重点进行润色"
    focus_areas = $focusAreas
}
Write-Output ($result | ConvertTo-Json -Depth 10 -Compress)

