#!/usr/bin/env bash
# 旋律风格学习助手 (简易版) - 支持 MP3 输入
# 自动将 MP3 转换为 MIDI，然后进行旋律分析

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

# 获取 skills 脚本目录
SKILLS_SCRIPTS_DIR="$SCRIPT_DIR/../../skills/scripts"

# 解析参数
SONG_NAME=""
FORCE_CONVERT=false
while [[ $# -gt 0 ]]; do
    case $1 in
        --song)
            SONG_NAME="$2"
            shift 2
            ;;
        --force)
            FORCE_CONVERT=true
            shift
            ;;
        --check)
            # 检查依赖模式
            if command -v python3 &> /dev/null; then
                python3 "$SKILLS_SCRIPTS_DIR/audio_to_midi.py" --check
            else
                output_json "{
                  \"status\": \"error\",
                  \"error\": \"Python3 未安装\",
                  \"message\": \"请先安装 Python3\"
                }"
                exit 1
            fi
            exit $?
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

# 检查 Python3 是否可用
check_python() {
    if command -v python3 &> /dev/null; then
        echo "python3"
    elif command -v python &> /dev/null; then
        # 检查是否是 Python 3
        if python --version 2>&1 | grep -q "Python 3"; then
            echo "python"
        else
            echo ""
        fi
    else
        echo ""
    fi
}

PYTHON_CMD=$(check_python)

# 扫描 references 目录，查找 MP3 和 MIDI 文件
scan_references() {
    local references_dir="$PROJECT_DIR/workspace/references"
    local songs_with_mp3=()
    local songs_with_midi=()
    local songs_need_convert=()

    if [ -d "$references_dir" ]; then
        for song_dir in "$references_dir"/*/; do
            if [ -d "$song_dir" ]; then
                local song_name=$(basename "$song_dir")
                local mp3_file="$song_dir$song_name.mp3"
                local midi_file="$song_dir$song_name.mid"
                local lyrics_file="$song_dir$song_name.txt"

                # 检查文件存在性
                local has_mp3=false
                local has_midi=false
                local has_lyrics=false

                [ -f "$mp3_file" ] && has_mp3=true
                [ -f "$midi_file" ] && has_midi=true
                [ -f "$lyrics_file" ] && has_lyrics=true

                if [ "$has_midi" = true ] && [ "$has_lyrics" = true ]; then
                    songs_with_midi+=("$song_name")
                elif [ "$has_mp3" = true ] && [ "$has_lyrics" = true ]; then
                    songs_with_mp3+=("$song_name")
                    songs_need_convert+=("$song_name")
                fi
            fi
        done
    fi

    # 输出扫描结果
    local mp3_json=$(printf '%s\n' "${songs_with_mp3[@]}" | jq -R . | jq -s . 2>/dev/null || echo "[]")
    local midi_json=$(printf '%s\n' "${songs_with_midi[@]}" | jq -R . | jq -s . 2>/dev/null || echo "[]")
    local convert_json=$(printf '%s\n' "${songs_need_convert[@]}" | jq -R . | jq -s . 2>/dev/null || echo "[]")

    echo "{\"mp3\": $mp3_json, \"midi\": $midi_json, \"need_convert\": $convert_json}"
}

# 如果没有提供歌曲名，扫描目录
if [ -z "$SONG_NAME" ]; then
    REFERENCES_DIR="$PROJECT_DIR/workspace/references"
    scan_result=$(scan_references)

    # 解析扫描结果
    songs_mp3=$(echo "$scan_result" | jq -r '.mp3[]' 2>/dev/null)
    songs_midi=$(echo "$scan_result" | jq -r '.midi[]' 2>/dev/null)
    songs_need_convert=$(echo "$scan_result" | jq -r '.need_convert[]' 2>/dev/null)

    mp3_count=$(echo "$scan_result" | jq '.mp3 | length' 2>/dev/null || echo "0")
    midi_count=$(echo "$scan_result" | jq '.midi | length' 2>/dev/null || echo "0")
    total_count=$((mp3_count + midi_count))

    if [ "$total_count" -eq 0 ]; then
        output_json "{
          \"status\": \"no_songs_found\",
          \"project_name\": \"$PROJECT_NAME\",
          \"references_dir\": \"$REFERENCES_DIR\",
          \"message\": \"未找到参考歌曲文件\",
          \"supported_formats\": [\"MP3 + 歌词\", \"MIDI + 歌词\"],
          \"required_structure\": \"workspace/references/{song-name}/{song-name}.mp3 (或 .mid) + {song-name}.txt\",
          \"action\": \"请按要求准备参考文件\"
        }"
        exit 1
    fi

    # 如果只有一首歌，自动选择
    if [ "$total_count" -eq 1 ]; then
        if [ "$midi_count" -eq 1 ]; then
            SONG_NAME=$(echo "$scan_result" | jq -r '.midi[0]')
        else
            SONG_NAME=$(echo "$scan_result" | jq -r '.mp3[0]')
        fi
    else
        # 多首歌曲，让用户选择
        output_json "{
          \"status\": \"multiple_songs_found\",
          \"project_name\": \"$PROJECT_NAME\",
          \"scan_result\": $scan_result,
          \"songs_ready\": $(echo "$scan_result" | jq '.midi'),
          \"songs_need_convert\": $(echo "$scan_result" | jq '.mp3'),
          \"message\": \"找到多个参考歌曲\",
          \"action\": \"请指定歌曲名: /melody-mimic-easy [歌曲名]\"
        }"
        exit 0
    fi
fi

# 设置文件路径
SONG_DIR="$PROJECT_DIR/workspace/references/$SONG_NAME"
MP3_FILE="$SONG_DIR/$SONG_NAME.mp3"
MIDI_FILE="$SONG_DIR/$SONG_NAME.mid"
LYRICS_FILE="$SONG_DIR/$SONG_NAME.txt"
OUTPUT_DIR="$PROJECT_DIR/workspace/output"

# 检查歌词文件
if [ ! -f "$LYRICS_FILE" ]; then
    output_json "{
      \"status\": \"missing_lyrics\",
      \"project_name\": \"$PROJECT_NAME\",
      \"song_name\": \"$SONG_NAME\",
      \"expected_file\": \"$LYRICS_FILE\",
      \"message\": \"缺少歌词文件\",
      \"action\": \"请创建歌词文件: $SONG_NAME.txt\"
    }"
    exit 1
fi

# 检查是否需要转换 MP3
needs_conversion=false
if [ ! -f "$MIDI_FILE" ]; then
    if [ -f "$MP3_FILE" ]; then
        needs_conversion=true
    else
        output_json "{
          \"status\": \"missing_audio\",
          \"project_name\": \"$PROJECT_NAME\",
          \"song_name\": \"$SONG_NAME\",
          \"expected_files\": [\"$SONG_NAME.mp3\", \"$SONG_NAME.mid\"],
          \"message\": \"缺少音频文件 (MP3 或 MIDI)\",
          \"action\": \"请提供 MP3 或 MIDI 文件\"
        }"
        exit 1
    fi
fi

# 如果需要转换 MP3
if [ "$needs_conversion" = true ] || [ "$FORCE_CONVERT" = true ]; then
    # 检查 Python
    if [ -z "$PYTHON_CMD" ]; then
        output_json "{
          \"status\": \"python_not_found\",
          \"project_name\": \"$PROJECT_NAME\",
          \"song_name\": \"$SONG_NAME\",
          \"message\": \"需要 Python3 来转换 MP3\",
          \"action\": \"请安装 Python3，或使用在线工具转换\",
          \"online_tool\": \"https://basicpitch.spotify.com\"
        }"
        exit 1
    fi

    # 检查转换依赖
    dep_check=$($PYTHON_CMD "$SKILLS_SCRIPTS_DIR/audio_to_midi.py" --check 2>/dev/null)
    dep_status=$(echo "$dep_check" | jq -r '.status' 2>/dev/null)

    if [ "$dep_status" != "ready" ]; then
        # 获取硬件信息
        hardware_device=$(echo "$dep_check" | jq -r '.hardware.device' 2>/dev/null || echo "cpu")
        hardware_desc=$(echo "$dep_check" | jq -r '.hardware.description' 2>/dev/null || echo "CPU 模式")
        estimated_time=$(echo "$dep_check" | jq -r '.hardware.estimated_time' 2>/dev/null || echo "5-15 分钟")

        output_json "{
          \"status\": \"need_dependencies\",
          \"project_name\": \"$PROJECT_NAME\",
          \"song_name\": \"$SONG_NAME\",
          \"mp3_file\": \"$MP3_FILE\",
          \"dependency_check\": $dep_check,
          \"message\": \"需要安装依赖来转换 MP3 为 MIDI\",
          \"install_command\": \"pip install demucs basic-pitch\",
          \"hardware\": {
            \"device\": \"$hardware_device\",
            \"description\": \"$hardware_desc\",
            \"estimated_time\": \"$estimated_time\"
          },
          \"alternatives\": [
            {
              \"name\": \"在线工具\",
              \"url\": \"https://basicpitch.spotify.com\",
              \"description\": \"上传 MP3，下载 MIDI，放入 $SONG_DIR\"
            }
          ],
          \"action\": \"安装依赖后重新运行，或使用在线工具\"
        }"
        exit 1
    fi

    # 依赖已就绪，获取硬件信息
    hardware_device=$(echo "$dep_check" | jq -r '.hardware.device' 2>/dev/null || echo "cpu")
    hardware_desc=$(echo "$dep_check" | jq -r '.hardware.description' 2>/dev/null || echo "CPU 模式")
    estimated_time=$(echo "$dep_check" | jq -r '.hardware.estimated_time' 2>/dev/null || echo "5-15 分钟")

    # 输出转换准备状态
    output_json "{
      \"status\": \"ready_to_convert\",
      \"action\": \"convert_mp3\",
      \"project_name\": \"$PROJECT_NAME\",
      \"song_name\": \"$SONG_NAME\",
      \"files\": {
        \"mp3_path\": \"$MP3_FILE\",
        \"lyrics_path\": \"$LYRICS_FILE\",
        \"output_midi\": \"$MIDI_FILE\"
      },
      \"hardware\": {
        \"device\": \"$hardware_device\",
        \"description\": \"$hardware_desc\",
        \"estimated_time\": \"$estimated_time\"
      },
      \"conversion_steps\": [
        \"1. 使用 Demucs 分离人声音轨\",
        \"2. 使用 Basic Pitch 将人声转为 MIDI\",
        \"3. 进入旋律分析流程\"
      ],
      \"message\": \"准备将 MP3 转换为 MIDI\",
      \"confirm_prompt\": \"预计耗时 $estimated_time，是否开始转换?\",
      \"python_command\": \"$PYTHON_CMD $SKILLS_SCRIPTS_DIR/audio_to_midi.py '$MP3_FILE' '$SONG_DIR'\"
    }"
    exit 0
fi

# MIDI 文件已存在，直接进入分析流程
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

# 输出状态信息 (与 melody-mimic.sh 兼容)
output_json "{
  \"status\": \"ready\",
  \"action\": \"analyze\",
  \"project_name\": \"$PROJECT_NAME\",
  \"song_name\": \"$SONG_NAME\",
  \"source\": \"midi\",
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
    \"4. 生成双报告（旋律特征 + 歌词分析）\",
    \"5. 引导歌词创作流程\",
    \"6. 生成原创旋律\"
  ],
  \"copyright_reminder\": \"⚠️ 此工具仅供学习参考，请注意版权风险\"
}"
