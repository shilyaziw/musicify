#!/usr/bin/env bash
# 押韵检查

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

# 读取歌词和结构
lyrics_content=$(cat "$LYRICS_FILE")
STRUCTURE_FILE="$PROJECT_DIR/structure.json"

if [ -f "$STRUCTURE_FILE" ]; then
    structure_content=$(cat "$STRUCTURE_FILE")
else
    structure_content="{}"
fi

output_json "{
  \"status\": \"success\",
  \"project_name\": \"$PROJECT_NAME\",
  \"lyrics_file\": \"$LYRICS_FILE\",
  \"lyrics_content\": \"$(escape_json "$lyrics_content")\",
  \"structure\": $structure_content,
  \"message\": \"AI 应分析押韵模式、韵母匹配度，并提供优化建议\"
}"

