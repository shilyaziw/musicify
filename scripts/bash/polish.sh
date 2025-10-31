#!/usr/bin/env bash
# 歌词润色

# 加载通用函数库
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/common.sh"

# 获取项目路径
PROJECT_DIR=$(get_current_project)
PROJECT_NAME=$(get_project_name)

# 检查歌词文件
LYRICS_FILE="$PROJECT_DIR/lyrics.md"

if [ ! -f "$LYRICS_FILE" ]; then
    output_json "{
      \"status\": \"error\",
      \"message\": \"未找到 lyrics.md。请先创作歌词。\"
    }"
    exit 1
fi

# 解析参数
FOCUS="all"  # 默认全面润色

while [[ $# -gt 0 ]]; do
    case $1 in
        --focus)
            FOCUS="$2"
            shift 2
            ;;
        --project)
            shift 2
            ;;
        *)
            shift
            ;;
    esac
done

# 读取文件
lyrics_content=$(cat "$LYRICS_FILE")
SPEC_FILE="$PROJECT_DIR/spec.json"
THEME_FILE="$PROJECT_DIR/theme.md"

spec_content="{}"
theme_content=""

if [ -f "$SPEC_FILE" ]; then
    spec_content=$(cat "$SPEC_FILE")
fi

if [ -f "$THEME_FILE" ]; then
    theme_content=$(cat "$THEME_FILE")
fi

output_json "{
  \"status\": \"success\",
  \"project_name\": \"$PROJECT_NAME\",
  \"lyrics_file\": \"$LYRICS_FILE\",
  \"focus\": \"$FOCUS\",
  \"lyrics_content\": \"$(escape_json "$lyrics_content")\",
  \"spec\": $spec_content,
  \"theme\": \"$(escape_json "$theme_content")\",
  \"message\": \"AI 应根据重点进行润色\",
  \"focus_areas\": {
    \"wording\": \"用词精准度优化\",
    \"imagery\": \"意象丰富度提升\",
    \"emotion\": \"情感表达优化\",
    \"singability\": \"可唱性优化（避免拗口）\",
    \"all\": \"全面润色\"
  }
}"

