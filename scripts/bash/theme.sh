#!/usr/bin/env bash
# 主题构思

# 加载通用函数库
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/common.sh"

# 获取项目路径
PROJECT_DIR=$(get_current_project)
PROJECT_NAME=$(get_project_name)

# 检查配置文件
SPEC_FILE=$(check_spec_exists)
THEME_FILE="$PROJECT_DIR/theme.md"

# 读取歌曲规格
spec_content=$(cat "$SPEC_FILE")

# 检查是否已有主题文件
if [ -f "$THEME_FILE" ]; then
    existing_theme=$(cat "$THEME_FILE")
    word_count=$(count_lyrics_words "$THEME_FILE")

    output_json "{
      \"status\": \"success\",
      \"action\": \"review\",
      \"project_name\": \"$PROJECT_NAME\",
      \"theme_file\": \"$THEME_FILE\",
      \"spec\": $spec_content,
      \"existing_theme\": \"$(escape_json "$existing_theme")\",
      \"word_count\": $word_count,
      \"message\": \"发现已有主题，AI 可引导用户审查或修改\"
    }"
else
    # 创建主题模板
    cat > "$THEME_FILE" <<EOF
# 歌曲主题

## 核心主题
（想表达什么？爱情/友情/成长/梦想/社会/治愈...）

## 情感表达
（想传达什么情绪和信息？）

## 故事性
（是否有具体故事线？如果有，简述故事）

## 独特视角
（这首歌与其他同主题歌曲有什么不同的切入点？）

## 一句话概括
（用一句话概括这首歌的核心）

---
创建时间: $(date '+%Y-%m-%d %H:%M:%S')
EOF

    output_json "{
      \"status\": \"success\",
      \"action\": \"create\",
      \"project_name\": \"$PROJECT_NAME\",
      \"theme_file\": \"$THEME_FILE\",
      \"spec\": $spec_content,
      \"message\": \"已创建主题模板，AI 应引导用户思考并填写\",
      \"guidance\": \"通过提问引导用户思考，而不是替用户想创意\"
    }"
fi

