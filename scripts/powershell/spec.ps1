# 定义/更新歌曲规格

# 加载通用函数库
. "$PSScriptRoot\common.ps1"

$projectDir = Get-CurrentProject
$projectName = Get-ProjectName

$specFile = Join-Path $projectDir "spec.json"
$configFile = Join-Path $projectDir ".musicify\config.json"

# 读取 config.json 中的 defaultType
$defaultType = ""
if (Test-Path $configFile) {
    $config = Get-Content $configFile | ConvertFrom-Json
    $defaultType = $config.defaultType
}

# 如果已有配置，读取现有配置
if (Test-Path $specFile) {
    $existingConfig = Get-Content $specFile -Raw
    $result = @{
        status = "success"
        action = "update"
        project_name = $projectName
        project_path = $projectDir
        spec_file = $specFile
        existing_config = ($existingConfig | ConvertFrom-Json)
        message = "找到现有配置，AI 可引导用户更新"
    }
    Write-Output ($result | ConvertTo-Json -Depth 10 -Compress)
} else {
    # 创建初始配置模板
    $initialConfig = @{
        project_name = $projectName
        song_type = $defaultType
        duration = ""
        style = ""
        language = ""
        audience = @{
            age = ""
            gender = ""
        }
        target_platform = @()
        tone = ""
        created_at = Get-CurrentTimestamp
        updated_at = Get-CurrentTimestamp
    }
    
    $initialConfig | ConvertTo-Json -Depth 10 | Set-Content $specFile
    
    $result = @{
        status = "success"
        action = "create"
        project_name = $projectName
        project_path = $projectDir
        spec_file = $specFile
        default_type = $defaultType
        message = "已创建配置模板，AI 应引导用户填写"
        required_fields = @(
            "song_type (类型): 流行/摇滚/说唱/民谣/电子/古风等",
            "duration (时长): 如 '3分30秒' 或 '4分钟'",
            "style (风格): 抒情/激昂/轻快/忧郁/治愈等",
            "language (语言): 中文/英文/粤语/日语等",
            "audience (受众): 年龄段和性别",
            "target_platform (目标平台): QQ音乐/网易云/抖音/B站等"
        )
    }
    Write-Output ($result | ConvertTo-Json -Depth 10 -Compress)
}

