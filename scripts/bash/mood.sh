#!/usr/bin/env bash
# 情绪氛围定位

# 加载通用函数库
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/common.sh"

# 获取项目路径
PROJECT_DIR=$(get_current_project)
PROJECT_NAME=$(get_project_name)

# 检查配置文件
SPEC_FILE=$(check_spec_exists)
THEME_FILE=$(check_theme_exists)
MOOD_FILE="$PROJECT_DIR/mood.json"

# 读取规格和主题
spec_content=$(cat "$SPEC_FILE")
theme_content=$(cat "$THEME_FILE")

# 检查是否已有情绪文件
if [ -f "$MOOD_FILE" ]; then
    existing_mood=$(cat "$MOOD_FILE")
    output_json "{
      \"status\": \"success\",
      \"action\": \"update\",
      \"project_name\": \"$PROJECT_NAME\",
      \"mood_file\": \"$MOOD_FILE\",
      \"spec\": $spec_content,
      \"theme\": \"$(escape_json "$theme_content")\",
      \"existing_mood\": $existing_mood,
      \"message\": \"找到现有情绪定位，AI 可引导用户更新\"
    }"
else
    # 创建情绪模板
    cat > "$MOOD_FILE" <<EOF
{
  "emotion_color": "",
  "energy_level": "",
  "tempo": "",
  "atmosphere_tags": [],
  "created_at": "$(date -u +"%Y-%m-%dT%H:%M:%SZ")",
  "updated_at": "$(date -u +"%Y-%m-%dT%H:%M:%SZ")"
}
EOF

    output_json "{
      \"status\": \"success\",
      \"action\": \"create\",
      \"project_name\": \"$PROJECT_NAME\",
      \"mood_file\": \"$MOOD_FILE\",
      \"spec\": $spec_content,
      \"theme\": \"$(escape_json "$theme_content")\",
      \"message\": \"已创建情绪模板，AI 应引导用户填写\",
      \"required_fields\": [
        \"emotion_color (情绪色彩): 温暖/冷峻/明亮/灰暗等\",
        \"energy_level (能量级别): 高能/中等/低能\",
        \"tempo (节奏): 快节奏/中速/慢板\",
        \"atmosphere_tags (氛围标签): 浪漫/孤独/自由/压抑等\"
      ]
    }"
fi

