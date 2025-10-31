# 情绪氛围定位

. "$PSScriptRoot\common.ps1"

$projectDir = Get-CurrentProject
$projectName = Get-ProjectName

$specFile = Test-SpecExists
$themeFile = Test-ThemeExists
$moodFile = Join-Path $projectDir "mood.json"

$specContent = Get-Content $specFile | ConvertFrom-Json
$themeContent = Get-Content $themeFile -Raw

if (Test-Path $moodFile) {
    $existingMood = Get-Content $moodFile | ConvertFrom-Json
    
    $result = @{
        status = "success"
        action = "update"
        project_name = $projectName
        mood_file = $moodFile
        spec = $specContent
        theme = (ConvertTo-JsonString -InputString $themeContent)
        existing_mood = $existingMood
        message = "找到现有情绪定位，AI 可引导用户更新"
    }
    Write-Output ($result | ConvertTo-Json -Depth 10 -Compress)
} else {
    $initialMood = @{
        emotion_color = ""
        energy_level = ""
        tempo = ""
        atmosphere_tags = @()
        created_at = Get-CurrentTimestamp
        updated_at = Get-CurrentTimestamp
    }
    
    $initialMood | ConvertTo-Json -Depth 10 | Set-Content $moodFile
    
    $result = @{
        status = "success"
        action = "create"
        project_name = $projectName
        mood_file = $moodFile
        spec = $specContent
        theme = (ConvertTo-JsonString -InputString $themeContent)
        message = "已创建情绪模板，AI 应引导用户填写"
        required_fields = @(
            "emotion_color (情绪色彩): 温暖/冷峻/明亮/灰暗等",
            "energy_level (能量级别): 高能/中等/低能",
            "tempo (节奏): 快节奏/中速/慢板",
            "atmosphere_tags (氛围标签): 浪漫/孤独/自由/压抑等"
        )
    }
    Write-Output ($result | ConvertTo-Json -Depth 10 -Compress)
}

