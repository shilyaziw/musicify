#!/usr/bin/env bash
# 旋律风格学习助手 - MIDI 分析和旋律创作

# 加载通用函数库
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/common.sh"

# 获取项目路径
PROJECT_DIR=$(get_current_project)
PROJECT_NAME=$(get_project_name)

# 创建必要目录
ensure_dir "$PROJECT_DIR/workspace"
ensure_dir "$PROJECT_DIR/workspace/references"
ensure_dir "$PROJECT_DIR/workspace/output"

# 解析参数
SONG_NAME=""
while [[ $# -gt 0 ]]; do
    case $1 in
        --song)
            SONG_NAME="$2"
            shift 2
            ;;
        --project)
            shift 2
            ;;
        *)
            if [ -z "$SONG_NAME" ] && [ -n "$1" ] && [[ ! "$1" =~ ^-- ]]; then
                SONG_NAME="$1"
            fi
            shift
            ;;
    esac
done

# 如果没有提供歌曲名,扫描 references 目录
if [ -z "$SONG_NAME" ]; then
    REFERENCES_DIR="$PROJECT_DIR/workspace/references"
    available_songs=()

    if [ -d "$REFERENCES_DIR" ]; then
        for song_dir in "$REFERENCES_DIR"/*/; do
            if [ -d "$song_dir" ]; then
                song_name=$(basename "$song_dir")
                midi_file="$song_dir$song_name.mid"
                lyrics_file="$song_dir$song_name.txt"

                # 检查是否同时存在 MIDI 和歌词文件
                if [ -f "$midi_file" ] && [ -f "$lyrics_file" ]; then
                    available_songs+=("$song_name")
                fi
            fi
        done
    fi

    if [ ${#available_songs[@]} -eq 0 ]; then
        output_json "{
          \"status\": \"no_songs_found\",
          \"project_name\": \"$PROJECT_NAME\",
          \"references_dir\": \"$REFERENCES_DIR\",
          \"message\": \"未找到参考歌曲文件\",
          \"required_structure\": \"workspace/references/{song-name}/{song-name}.mid + {song-name}.txt\",
          \"action\": \"请按要求准备参考文件\"
        }"
        exit 1
    elif [ ${#available_songs[@]} -eq 1 ]; then
        SONG_NAME="${available_songs[0]}"
    else
        # 多个歌曲,让用户选择
        songs_json=$(printf '%s\n' "${available_songs[@]}" | jq -R . | jq -s .)
        output_json "{
          \"status\": \"multiple_songs_found\",
          \"project_name\": \"$PROJECT_NAME\",
          \"available_songs\": $songs_json,
          \"message\": \"找到多个参考歌曲，请选择一个\",
          \"action\": \"请指定歌曲名: bash scripts/bash/melody-mimic.sh [歌曲名]\"
        }"
        exit 0
    fi
fi

# 设置文件路径
SONG_DIR="$PROJECT_DIR/workspace/references/$SONG_NAME"
MIDI_FILE="$SONG_DIR/$SONG_NAME.mid"
LYRICS_FILE="$SONG_DIR/$SONG_NAME.txt"
OUTPUT_DIR="$PROJECT_DIR/workspace/output"

# 检查文件存在性
missing_files=()
if [ ! -f "$MIDI_FILE" ]; then
    missing_files+=("MIDI 文件: $MIDI_FILE")
fi
if [ ! -f "$LYRICS_FILE" ]; then
    missing_files+=("歌词文件: $LYRICS_FILE")
fi

if [ ${#missing_files[@]} -gt 0 ]; then
    missing_json=$(printf '%s\n' "${missing_files[@]}" | jq -R . | jq -s .)
    output_json "{
      \"status\": \"missing_files\",
      \"project_name\": \"$PROJECT_NAME\",
      \"song_name\": \"$SONG_NAME\",
      \"missing_files\": $missing_json,
      \"expected_structure\": {
        \"directory\": \"$SONG_DIR\",
        \"midi_file\": \"$SONG_NAME.mid\",
        \"lyrics_file\": \"$SONG_NAME.txt\"
      },
      \"message\": \"缺少必需文件\",
      \"action\": \"请按要求准备参考文件\"
    }"
    exit 1
fi

# 读取歌词文件内容并分析结构
lyrics_content=$(cat "$LYRICS_FILE")
lyrics_word_count=$(echo "$lyrics_content" | tr -d '[\n\r\t ]，。！？、；：""''（）【】《》…—' | wc -c | tr -d ' ')

# 检查歌词文件格式
has_sections=false
if echo "$lyrics_content" | grep -q '\[.*\]'; then
    has_sections=true
fi

# 获取 MIDI 文件信息
midi_size=$(stat -f%z "$MIDI_FILE" 2>/dev/null || stat -c%s "$MIDI_FILE" 2>/dev/null || echo "unknown")

# 检查输出目录中是否已有分析结果
ANALYSIS_FILE="$OUTPUT_DIR/$SONG_NAME-analysis.json"
has_existing_analysis=false
if [ -f "$ANALYSIS_FILE" ]; then
    has_existing_analysis=true
fi

# 输出状态信息
output_json "{
  \"status\": \"ready\",
  \"action\": \"analyze\",
  \"project_name\": \"$PROJECT_NAME\",
  \"song_name\": \"$SONG_NAME\",
  \"files\": {
    \"midi_path\": \"$MIDI_FILE\",
    \"lyrics_path\": \"$LYRICS_FILE\",
    \"midi_size\": \"$midi_size bytes\",
    \"lyrics_word_count\": $lyrics_word_count,
    \"has_section_markers\": $has_sections
  },
  \"directories\": {
    \"song_dir\": \"$SONG_DIR\",
    \"output_dir\": \"$OUTPUT_DIR\",
    \"analysis_file\": \"$ANALYSIS_FILE\"
  },
  \"analysis\": {
    \"has_existing\": $has_existing_analysis,
    \"status\": \"$([ $has_existing_analysis = true ] && echo 'found_previous' || echo 'new_analysis')\"
  },
  \"message\": \"参考文件检查完成，准备开始分析\",
  \"next_steps\": [
    \"1. 解析 MIDI 文件，识别音轨结构\",
    \"2. 匹配人声音轨与歌词字数\",
    \"3. 分析旋律特征（节奏型、音程分布、调式）\",
    \"4. 引导歌词创作流程\",
    \"5. 生成原创旋律\"
  ],
  \"copyright_reminder\": \"⚠️ 此工具仅供学习参考，请注意版权风险\"
}"