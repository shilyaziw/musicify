#!/usr/bin/env bash
# 旋律提示

# 加载通用函数库
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/common.sh"

# 获取项目路径
PROJECT_DIR=$(get_current_project)
PROJECT_NAME=$(get_project_name)

# 检查必需文件
SPEC_FILE=$(check_spec_exists)
LYRICS_FILE="$PROJECT_DIR/lyrics.md"
STRUCTURE_FILE="$PROJECT_DIR/structure.json"

if [ ! -f "$LYRICS_FILE" ]; then
    output_json "{
      \"status\": \"error\",
      \"message\": \"未找到 lyrics.md。请先创作歌词。\"
    }"
    exit 1
fi

# 读取文件
spec_content=$(cat "$SPEC_FILE")
lyrics_content=$(cat "$LYRICS_FILE")

structure_content="{}"
if [ -f "$STRUCTURE_FILE" ]; then
    structure_content=$(cat "$STRUCTURE_FILE")
fi

mood_content="{}"
MOOD_FILE="$PROJECT_DIR/mood.json"
if [ -f "$MOOD_FILE" ]; then
    mood_content=$(cat "$MOOD_FILE")
fi

output_json "{
  \"status\": \"success\",
  \"project_name\": \"$PROJECT_NAME\",
  \"spec\": $spec_content,
  \"structure\": $structure_content,
  \"mood\": $mood_content,
  \"lyrics_content\": \"$(escape_json "$lyrics_content")\",
  \"message\": \"AI 应根据歌曲类型、风格和情绪生成旋律提示\",
  \"hint_categories\": [
    \"vocal_range (音域范围)\",
    \"tempo (建议BPM)\",
    \"rhythm_pattern (节奏型)\",
    \"melody_direction (旋律走向)\",
    \"emphasis_points (重音位置)\",
    \"reference_songs (参考歌曲)\"
  ]
}"

