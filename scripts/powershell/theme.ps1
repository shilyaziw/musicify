# 主题构思

. "$PSScriptRoot\common.ps1"

$projectDir = Get-CurrentProject
$projectName = Get-ProjectName

$specFile = Test-SpecExists
$themeFile = Join-Path $projectDir "theme.md"

$specContent = Get-Content $specFile | ConvertFrom-Json

if (Test-Path $themeFile) {
    $existingTheme = Get-Content $themeFile -Raw
    $wordCount = Get-LyricsWordCount -FilePath $themeFile
    
    $result = @{
        status = "success"
        action = "review"
        project_name = $projectName
        theme_file = $themeFile
        spec = $specContent
        existing_theme = (ConvertTo-JsonString -InputString $existingTheme)
        word_count = $wordCount
        message = "发现已有主题，AI 可引导用户审查或修改"
    }
    Write-Output ($result | ConvertTo-Json -Depth 10 -Compress)
} else {
    $template = @"
# 歌曲主题

## 核心主题
（想表达什么？爱情/友情/成长/梦想/社会/治愈...）

## 情感表达
（想传达什么情绪和信息？）

## 故事性
（是否有具体故事线？如果有，简述故事）

## 独特视角
（这首歌与其他同主题歌曲有什么不同的切入点？）

## 一句话概括
（用一句话概括这首歌的核心）

---
创建时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
"@
    
    Set-Content -Path $themeFile -Value $template
    
    $result = @{
        status = "success"
        action = "create"
        project_name = $projectName
        theme_file = $themeFile
        spec = $specContent
        message = "已创建主题模板，AI 应引导用户思考并填写"
        guidance = "通过提问引导用户思考，而不是替用户想创意"
    }
    Write-Output ($result | ConvertTo-Json -Depth 10 -Compress)
}

