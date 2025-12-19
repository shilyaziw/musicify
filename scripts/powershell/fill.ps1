# 填充混合模式框架

. "$PSScriptRoot\common.ps1"

$projectDir = Get-CurrentProject
$projectName = Get-ProjectName

$lyricsFile = Join-Path $projectDir "lyrics.md"

if (-not (Test-Path $lyricsFile)) {
    $error = @{
        status = "error"
        message = "未找到 lyrics.md。请先运行 /lyrics --mode hybrid 创建歌词框架。"
    }
    Write-Output ($error | ConvertTo-Json -Compress)
    exit 1
}

$lyricsContent = Get-Content $lyricsFile -Raw
$fillCount = ([regex]::Matches($lyricsContent, '\[待填充')).Count

if ($fillCount -eq 0) {
    $result = @{
        status = "info"
        action = "completed"
        project_name = $projectName
        lyrics_file = $lyricsFile
        message = "歌词中没有待填充项，可能已经全部完成"
        suggestion = "可以运行 /rhyme 检查押韵，或 /polish 进行润色"
    }
    Write-Output ($result | ConvertTo-Json -Depth 10 -Compress)
} else {
    $result = @{
        status = "success"
        action = "fill"
        project_name = $projectName
        lyrics_file = $lyricsFile
        fill_count = $fillCount
        lyrics_content = (ConvertTo-JsonString -InputString $lyricsContent)
        message = "找到 $fillCount 个待填充项，AI 应引导用户逐一填充"
    }
    Write-Output ($result | ConvertTo-Json -Depth 10 -Compress)
}

