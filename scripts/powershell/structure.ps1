# 歌曲结构设计

. "$PSScriptRoot\common.ps1"

$projectDir = Get-CurrentProject
$projectName = Get-ProjectName

$specFile = Test-SpecExists
$themeFile = Test-ThemeExists
$structureFile = Join-Path $projectDir "structure.json"

$specContent = Get-Content $specFile | ConvertFrom-Json
$themeContent = Get-Content $themeFile -Raw

if (Test-Path $structureFile) {
    $existingStructure = Get-Content $structureFile | ConvertFrom-Json
    
    $result = @{
        status = "success"
        action = "update"
        project_name = $projectName
        structure_file = $structureFile
        spec = $specContent
        theme = (ConvertTo-JsonString -InputString $themeContent)
        existing_structure = $existingStructure
        message = "找到现有结构，AI 可引导用户更新"
    }
    Write-Output ($result | ConvertTo-Json -Depth 10 -Compress)
} else {
    $standardStructure = @{
        total_duration = ""
        sections = @(
            @{
                type = "intro"
                order = 1
                duration = ""
                purpose = "引入歌曲氛围，吸引听众注意"
            },
            @{
                type = "verse"
                order = 2
                duration = ""
                purpose = "铺陈故事或情境，建立情感基础"
                rhyme_scheme = ""
            },
            @{
                type = "pre-chorus"
                order = 3
                duration = ""
                purpose = "情绪过渡，为副歌蓄力"
            },
            @{
                type = "chorus"
                order = 4
                duration = ""
                purpose = "核心情感表达，最朗朗上口的部分"
                rhyme_scheme = ""
            },
            @{
                type = "verse"
                order = 5
                duration = ""
                purpose = "深化故事或情感层次"
                rhyme_scheme = ""
            },
            @{
                type = "chorus"
                order = 6
                duration = ""
                purpose = "重复核心情感，加深印象"
            },
            @{
                type = "bridge"
                order = 7
                duration = ""
                purpose = "情感转折或升华，提供新的视角"
            },
            @{
                type = "chorus"
                order = 8
                duration = ""
                purpose = "最后一次核心表达，达到情感高潮"
            },
            @{
                type = "outro"
                order = 9
                duration = ""
                purpose = "收尾，余韵"
            }
        )
        created_at = Get-CurrentTimestamp
        updated_at = Get-CurrentTimestamp
    }
    
    $standardStructure | ConvertTo-Json -Depth 10 | Set-Content $structureFile
    
    $result = @{
        status = "success"
        action = "create"
        project_name = $projectName
        structure_file = $structureFile
        spec = $specContent
        theme = (ConvertTo-JsonString -InputString $themeContent)
        message = "已创建标准歌曲结构模板，AI 可引导用户调整"
        note = "可根据歌曲类型调整结构（如说唱：Hook-Verse-Hook，民谣：Verse-Chorus循环）"
    }
    Write-Output ($result | ConvertTo-Json -Depth 10 -Compress)
}

