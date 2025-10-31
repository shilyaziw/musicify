#!/usr/bin/env bash
# 定义/更新歌曲规格

# 加载通用函数库
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/common.sh"

# 获取项目路径
PROJECT_DIR=$(get_current_project)
PROJECT_NAME=$(get_project_name)

SPEC_FILE="$PROJECT_DIR/spec.json"
CONFIG_FILE="$PROJECT_DIR/.musicify/config.json"

# 读取 config.json 中的 defaultType (如果存在)
DEFAULT_TYPE=""
if [ -f "$CONFIG_FILE" ]; then
    DEFAULT_TYPE=$(grep -o '"defaultType"[[:space:]]*:[[:space:]]*"[^"]*"' "$CONFIG_FILE" | \
                   sed 's/"defaultType"[[:space:]]*:[[:space:]]*"\([^"]*\)"/\1/')
fi

# 如果已有配置，读取现有配置
if [ -f "$SPEC_FILE" ]; then
    existing_config=$(cat "$SPEC_FILE")
    output_json "{
      \"status\": \"success\",
      \"action\": \"update\",
      \"project_name\": \"$PROJECT_NAME\",
      \"project_path\": \"$PROJECT_DIR\",
      \"spec_file\": \"$SPEC_FILE\",
      \"existing_config\": $existing_config,
      \"message\": \"找到现有配置，AI 可引导用户更新\"
    }"
else
    # 创建初始配置模板
    cat > "$SPEC_FILE" <<EOF
{
  "project_name": "$PROJECT_NAME",
  "song_type": "$DEFAULT_TYPE",
  "duration": "",
  "style": "",
  "language": "",
  "audience": {
    "age": "",
    "gender": ""
  },
  "target_platform": [],
  "tone": "",
  "created_at": "$(date -u +"%Y-%m-%dT%H:%M:%SZ")",
  "updated_at": "$(date -u +"%Y-%m-%dT%H:%M:%SZ")"
}
EOF

    output_json "{
      \"status\": \"success\",
      \"action\": \"create\",
      \"project_name\": \"$PROJECT_NAME\",
      \"project_path\": \"$PROJECT_DIR\",
      \"spec_file\": \"$SPEC_FILE\",
      \"default_type\": \"$DEFAULT_TYPE\",
      \"message\": \"已创建配置模板，AI 应引导用户填写\",
      \"required_fields\": [
        \"song_type (类型): 流行/摇滚/说唱/民谣/电子/古风等\",
        \"duration (时长): 如 '3分30秒' 或 '4分钟'\",
        \"style (风格): 抒情/激昂/轻快/忧郁/治愈等\",
        \"language (语言): 中文/英文/粤语/日语等\",
        \"audience (受众): 年龄段和性别\",
        \"target_platform (目标平台): QQ音乐/网易云/抖音/B站等\"
      ]
    }"
fi

