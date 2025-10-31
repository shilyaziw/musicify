# 歌词创作

. "$PSScriptRoot\common.ps1"

$projectDir = Get-CurrentProject
$projectName = Get-ProjectName

$specFile = Test-SpecExists
$themeFile = Test-ThemeExists
$structureFile = Test-StructureExists
$lyricsFile = Join-Path $projectDir "lyrics.md"

# 解析参数
$mode = "coach"
for ($i = 0; $i -lt $args.Length; $i++) {
    if ($args[$i] -eq "--mode" -and $i+1 -lt $args.Length) {
        $mode = $args[$i+1]
    }
}

$specContent = Get-Content $specFile | ConvertFrom-Json
$themeContent = Get-Content $themeFile -Raw
$structureContent = Get-Content $structureFile | ConvertFrom-Json

if (Test-Path $lyricsFile) {
    $existingLyrics = Get-Content $lyricsFile -Raw
    $wordCount = Get-LyricsWordCount -FilePath $lyricsFile
    
    $result = @{
        status = "success"
        action = "review"
        project_name = $projectName
        lyrics_file = $lyricsFile
        mode = $mode
        spec = $specContent
        theme = (ConvertTo-JsonString -InputString $themeContent)
        structure = $structureContent
        existing_lyrics = (ConvertTo-JsonString -InputString $existingLyrics)
        word_count = $wordCount
        message = "发现已有歌词，AI 可引导用户审查或修改"
    }
    Write-Output ($result | ConvertTo-Json -Depth 10 -Compress)
} else {
    $template = @"
# 歌词

## 歌曲信息
- 创作模式: $mode
- 创建时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')

---

## [Intro]


## [Verse 1]


## [Pre-Chorus]


## [Chorus]


## [Verse 2]


## [Chorus]


## [Bridge]


## [Chorus]


## [Outro]


---
## 创作笔记
"@
    
    Set-Content -Path $lyricsFile -Value $template
    
    $modeGuide = @{
        coach = "逐段引导用户思考和创作，提问式激发，检查韵脚和意象"
        express = "AI 直接生成完整歌词，基于规格、主题、结构快速输出"
        hybrid = "AI 生成框架和关键句，标注 [待填充] 部分让用户填写"
    }
    
    $result = @{
        status = "success"
        action = "create"
        project_name = $projectName
        lyrics_file = $lyricsFile
        mode = $mode
        spec = $specContent
        theme = (ConvertTo-JsonString -InputString $themeContent)
        structure = $structureContent
        message = "已创建歌词模板，AI 应根据模式引导创作"
        mode_guide = $modeGuide
    }
    Write-Output ($result | ConvertTo-Json -Depth 10 -Compress)
}

