#!/usr/bin/env bash
# 导出歌词

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
FORMAT="txt"  # 默认纯文本

while [[ $# -gt 0 ]]; do
    case $1 in
        --format)
            FORMAT="$2"
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

# 读取歌词
lyrics_content=$(cat "$LYRICS_FILE")

# 创建导出目录
EXPORT_DIR="$PROJECT_DIR/export"
ensure_dir "$EXPORT_DIR"

# 根据格式导出
OUTPUT_FILE="$EXPORT_DIR/${PROJECT_NAME}-lyrics.$FORMAT"

case $FORMAT in
    txt)
        # 纯文本：去除markdown标记
        echo "$lyrics_content" | sed 's/^#.*$//' | sed 's/^\*\*.*\*\*$//' > "$OUTPUT_FILE"
        ;;
    md)
        # Markdown：直接复制
        cp "$LYRICS_FILE" "$OUTPUT_FILE"
        ;;
    pdf)
        # PDF需要额外工具，这里先创建标记
        echo "$lyrics_content" > "$OUTPUT_FILE.md"
        OUTPUT_FILE="$OUTPUT_FILE.md"
        ;;
    *)
        error_exit "不支持的格式: $FORMAT"
        ;;
esac

output_json "{
  \"status\": \"success\",
  \"project_name\": \"$PROJECT_NAME\",
  \"output_file\": \"$OUTPUT_FILE\",
  \"format\": \"$FORMAT\",
  \"message\": \"歌词已导出到: $OUTPUT_FILE\"
}"

