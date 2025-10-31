#!/usr/bin/env bash
# 歌曲结构设计

# 加载通用函数库
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/common.sh"

# 获取项目路径
PROJECT_DIR=$(get_current_project)
PROJECT_NAME=$(get_project_name)

# 检查配置文件
SPEC_FILE=$(check_spec_exists)
THEME_FILE=$(check_theme_exists)
STRUCTURE_FILE="$PROJECT_DIR/structure.json"

# 读取规格和主题
spec_content=$(cat "$SPEC_FILE")
theme_content=$(cat "$THEME_FILE")

# 检查是否已有结构文件
if [ -f "$STRUCTURE_FILE" ]; then
    existing_structure=$(cat "$STRUCTURE_FILE")
    output_json "{
      \"status\": \"success\",
      \"action\": \"update\",
      \"project_name\": \"$PROJECT_NAME\",
      \"structure_file\": \"$STRUCTURE_FILE\",
      \"spec\": $spec_content,
      \"theme\": \"$(escape_json "$theme_content")\",
      \"existing_structure\": $existing_structure,
      \"message\": \"找到现有结构，AI 可引导用户更新\"
    }"
else
    # 创建结构模板（标准流行歌曲结构）
    cat > "$STRUCTURE_FILE" <<EOF
{
  "total_duration": "",
  "sections": [
    {
      "type": "intro",
      "order": 1,
      "duration": "",
      "purpose": "引入歌曲氛围，吸引听众注意"
    },
    {
      "type": "verse",
      "order": 2,
      "duration": "",
      "purpose": "铺陈故事或情境，建立情感基础",
      "rhyme_scheme": ""
    },
    {
      "type": "pre-chorus",
      "order": 3,
      "duration": "",
      "purpose": "情绪过渡，为副歌蓄力"
    },
    {
      "type": "chorus",
      "order": 4,
      "duration": "",
      "purpose": "核心情感表达，最朗朗上口的部分",
      "rhyme_scheme": ""
    },
    {
      "type": "verse",
      "order": 5,
      "duration": "",
      "purpose": "深化故事或情感层次",
      "rhyme_scheme": ""
    },
    {
      "type": "chorus",
      "order": 6,
      "duration": "",
      "purpose": "重复核心情感，加深印象"
    },
    {
      "type": "bridge",
      "order": 7,
      "duration": "",
      "purpose": "情感转折或升华，提供新的视角"
    },
    {
      "type": "chorus",
      "order": 8,
      "duration": "",
      "purpose": "最后一次核心表达，达到情感高潮"
    },
    {
      "type": "outro",
      "order": 9,
      "duration": "",
      "purpose": "收尾，余韵"
    }
  ],
  "created_at": "$(date -u +"%Y-%m-%dT%H:%M:%SZ")",
  "updated_at": "$(date -u +"%Y-%m-%dT%H:%M:%SZ")"
}
EOF

    output_json "{
      \"status\": \"success\",
      \"action\": \"create\",
      \"project_name\": \"$PROJECT_NAME\",
      \"structure_file\": \"$STRUCTURE_FILE\",
      \"spec\": $spec_content,
      \"theme\": \"$(escape_json "$theme_content")\",
      \"message\": \"已创建标准歌曲结构模板，AI 可引导用户调整\",
      \"note\": \"可根据歌曲类型调整结构（如说唱：Hook-Verse-Hook，民谣：Verse-Chorus循环）\"
    }"
fi

