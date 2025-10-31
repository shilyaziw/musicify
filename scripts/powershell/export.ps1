# 导出歌词

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
$format = "txt"
for ($i = 0; $i -lt $args.Length; $i++) {
    if ($args[$i] -eq "--format" -and $i+1 -lt $args.Length) {
        $format = $args[$i+1]
    }
}

$lyricsContent = Get-Content $lyricsFile -Raw

# 创建导出目录
$exportDir = Join-Path $projectDir "export"
Ensure-Directory -Path $exportDir

# 根据格式导出
$outputFile = Join-Path $exportDir "$projectName-lyrics.$format"

switch ($format) {
    "txt" {
        # 纯文本：去除markdown标记
        $cleanContent = $lyricsContent -replace '^\s*#.*$', '' -replace '^\*\*.*\*\*$', ''
        Set-Content -Path $outputFile -Value $cleanContent
    }
    "md" {
        # Markdown：直接复制
        Copy-Item -Path $lyricsFile -Destination $outputFile -Force
    }
    "pdf" {
        # PDF需要额外工具，这里先创建标记
        Set-Content -Path "$outputFile.md" -Value $lyricsContent
        $outputFile = "$outputFile.md"
    }
    default {
        $error = @{
            status = "error"
            message = "不支持的格式: $format"
        }
        Write-Output ($error | ConvertTo-Json -Compress)
        exit 1
    }
}

$result = @{
    status = "success"
    project_name = $projectName
    output_file = $outputFile
    format = $format
    message = "歌词已导出到: $outputFile"
}
Write-Output ($result | ConvertTo-Json -Depth 10 -Compress)

