#!/usr/bin/env bash
# 填充混合模式框架

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
      \"message\": \"未找到 lyrics.md。请先运行 /lyrics --mode hybrid 创建歌词框架。\"
    }"
    exit 1
fi

# 读取歌词内容
lyrics_content=$(cat "$LYRICS_FILE")

# 检查是否有待填充标记
fill_count=$(echo "$lyrics_content" | grep -c "\[待填充")

if [ "$fill_count" -eq 0 ]; then
    output_json "{
      \"status\": \"info\",
      \"action\": \"completed\",
      \"project_name\": \"$PROJECT_NAME\",
      \"lyrics_file\": \"$LYRICS_FILE\",
      \"message\": \"歌词中没有待填充项，可能已经全部完成\",
      \"suggestion\": \"可以运行 /rhyme 检查押韵，或 /polish 进行润色\"
    }"
else
    output_json "{
      \"status\": \"success\",
      \"action\": \"fill\",
      \"project_name\": \"$PROJECT_NAME\",
      \"lyrics_file\": \"$LYRICS_FILE\",
      \"fill_count\": $fill_count,
      \"lyrics_content\": \"$(escape_json "$lyrics_content")\",
      \"message\": \"找到 $fill_count 个待填充项，AI 应引导用户逐一填充\"
    }"
fi

