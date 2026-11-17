#!/usr/bin/env bash
# git pull·merge·switch 뒤 지침 파일(.claude/, CLAUDE.md)이 바뀌었으면 다시 읽으라고 Claude 맥락에 알린다.
cat > /dev/null
cd "${CLAUDE_PROJECT_DIR:-.}" 2>/dev/null || exit 0

# HEAD@{1}은 직전 HEAD 위치. 바뀐 게 없거나 reflog가 없으면 아무것도 하지 않는다
changed=$(git diff --name-only 'HEAD@{1}' HEAD -- .claude CLAUDE.md 2>/dev/null | tr '\n' ' ' | sed -E 's/ +$//')
[ -z "$changed" ] && exit 0

printf '{"hookSpecificOutput":{"hookEventName":"PostToolUse","additionalContext":"[훅 알림] 지침 파일이 바뀌었습니다: %s. 세션 시작 때 읽은 내용과 다를 수 있으니 진행 전에 다시 읽으세요."}}\n' "$changed"
