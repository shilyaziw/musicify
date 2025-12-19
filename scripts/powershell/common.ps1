# Musicify 通用函数库 (PowerShell)

# 查找 musicify 项目根目录
function Get-CurrentProject {
    $currentDir = Get-Location
    while ($currentDir.Path -ne $currentDir.Root.Path) {
        $configPath = Join-Path $currentDir.Path ".musicify\config.json"
        if (Test-Path $configPath) {
            return $currentDir.Path
        }
        $currentDir = $currentDir.Parent
    }
    return (Get-Location).Path
}

# 获取项目名称
function Get-ProjectName {
    $projectDir = Get-CurrentProject
    return Split-Path -Leaf $projectDir
}

# 检查 spec.json 是否存在
function Test-SpecExists {
    $projectDir = Get-CurrentProject
    $specFile = Join-Path $projectDir "spec.json"
    
    if (-not (Test-Path $specFile)) {
        $error = @{
            status = "error"
            message = "未找到 spec.json。请先运行 /spec 命令定义歌曲规格。"
        }
        Write-Output ($error | ConvertTo-Json -Compress)
        exit 1
    }
    
    return $specFile
}

# 检查 theme.md 是否存在
function Test-ThemeExists {
    $projectDir = Get-CurrentProject
    $themeFile = Join-Path $projectDir "theme.md"
    
    if (-not (Test-Path $themeFile)) {
        $error = @{
            status = "error"
            message = "未找到 theme.md。请先运行 /theme 命令构思主题。"
        }
        Write-Output ($error | ConvertTo-Json -Compress)
        exit 1
    }
    
    return $themeFile
}

# 检查 structure.json 是否存在
function Test-StructureExists {
    $projectDir = Get-CurrentProject
    $structureFile = Join-Path $projectDir "structure.json"
    
    if (-not (Test-Path $structureFile)) {
        $error = @{
            status = "error"
            message = "未找到 structure.json。请先运行 /structure 命令设计歌曲结构。"
        }
        Write-Output ($error | ConvertTo-Json -Compress)
        exit 1
    }
    
    return $structureFile
}

# 输出 JSON 格式结果
function Write-JsonOutput {
    param([string]$JsonString)
    Write-Output $JsonString
}

# 转义 JSON 字符串
function ConvertTo-JsonString {
    param([string]$InputString)
    
    $escaped = $InputString -replace '\\', '\\' `
                           -replace '"', '\"' `
                           -replace "`n", '\n' `
                           -replace "`r", '\r' `
                           -replace "`t", '\t'
    return $escaped
}

# 读取文件内容并转义为 JSON 字符串
function Get-FileAsJson {
    param([string]$FilePath)
    
    if (Test-Path $FilePath) {
        $content = Get-Content -Path $FilePath -Raw
        return ConvertTo-JsonString -InputString $content
    }
    return ""
}

# 统计歌词字数（中文字符数）
function Get-LyricsWordCount {
    param([string]$FilePath)
    
    if (Test-Path $FilePath) {
        $content = Get-Content -Path $FilePath -Raw
        # 移除注释和空行
        $lines = $content -split "`n" | Where-Object { $_ -notmatch '^\s*#' -and $_ -notmatch '^\s*$' }
        $text = $lines -join ''
        # 统计中文字符
        $chineseChars = [regex]::Matches($text, '[\u4e00-\u9fa5]')
        return $chineseChars.Count
    }
    return 0
}

# 获取当前时间戳 (ISO 8601)
function Get-CurrentTimestamp {
    return (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
}

# 确保目录存在
function Ensure-Directory {
    param([string]$Path)
    if (-not (Test-Path $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

