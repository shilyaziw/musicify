# 智能导出系统 - 支持多平台导出

. "$PSScriptRoot\common.ps1"

$projectDir = Get-CurrentProject
$projectName = Get-ProjectName

# 检查必需文件
$specFile = Test-SpecExists
$lyricsFile = Join-Path $projectDir "lyrics.md"

if (-not (Test-Path $lyricsFile)) {
    $error = @{
        status = "error"
        message = "未找到 lyrics.md。请先创作歌词。"
    }
    Write-Output ($error | ConvertTo-Json -Compress)
    exit 1
}

# 读取所有项目文件
$specContent = Get-Content $specFile | ConvertFrom-Json
$lyricsContent = Get-Content $lyricsFile -Raw

# 读取可选文件
$structureFile = Join-Path $projectDir "structure.json"
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

$compositionFile = Join-Path $projectDir "composition.yaml"
$hasComposition = $false
$compositionContent = ""
if (Test-Path $compositionFile) {
    $compositionContent = Get-Content $compositionFile -Raw
    $hasComposition = $true
}

$notationFile = Join-Path $projectDir "notation.abc"
$hasNotation = $false
$notationContent = ""
if (Test-Path $notationFile) {
    $notationContent = Get-Content $notationFile -Raw
    $hasNotation = $true
}

$themeFile = Join-Path $projectDir "theme.md"
$themeContent = ""
if (Test-Path $themeFile) {
    $themeContent = ConvertTo-JsonString -InputString (Get-Content $themeFile -Raw)
}

# 输出所有信息给 AI
$result = @{
    status = "success"
    project_name = $projectName
    project_dir = $projectDir
    spec = $specContent
    structure = $structureContent
    mood = $moodContent
    theme = $themeContent
    lyrics_content = (ConvertTo-JsonString -InputString $lyricsContent)
    composition_content = (ConvertTo-JsonString -InputString $compositionContent)
    notation_content = (ConvertTo-JsonString -InputString $notationContent)
    has_composition = $hasComposition
    has_notation = $hasNotation
    export_dir = (Join-Path $projectDir "exports")
    message = "AI 应询问用户选择导出平台,然后生成对应文件"
    export_options = @(
        "1. Suno AI",
        "2. Tunee AI",
        "3. 通用格式",
        "4. 纯歌词",
        "5. 全部导出"
    )
}

Write-Output ($result | ConvertTo-Json -Depth 10 -Compress)

