# 押韵检查

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

$lyricsContent = Get-Content $lyricsFile -Raw
$structureFile = Join-Path $projectDir "structure.json"

if (Test-Path $structureFile) {
    $structureContent = Get-Content $structureFile | ConvertFrom-Json
} else {
    $structureContent = @{}
}

$result = @{
    status = "success"
    project_name = $projectName
    lyrics_file = $lyricsFile
    lyrics_content = (ConvertTo-JsonString -InputString $lyricsContent)
    structure = $structureContent
    message = "AI 应分析押韵模式、韵母匹配度，并提供优化建议"
}
Write-Output ($result | ConvertTo-Json -Depth 10 -Compress)

