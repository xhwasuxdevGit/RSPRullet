#!/usr/bin/env bash
# git pull·merge·switch 뒤 지침 파일(.claude/, CLAUDE.md)이 바뀌었으면 다시 읽으라고 Claude 맥락에 알린다.
input=$(cat)

# settings.json의 if 조건은 복합 명령(히어독 등)에서 걸러지지 않을 수 있어, 명령 문자열을 직접 확인한다
if ! printf '%s' "$input" | grep -qE '"command" *: *"([^"\\]|\\.)*git +(pull|merge|switch|checkout)'; then
  exit 0
fi

cd "${CLAUDE_PROJECT_DIR:-.}" 2>/dev/null || exit 0

# 직전 HEAD 이동이 pull·merge·checkout(switch)일 때만 본다. 커밋으로 HEAD가 움직인 경우는 제외한다
last_move=$(git reflog -1 --format=%gs 2>/dev/null)
case "$last_move" in
  pull*|merge*|checkout*) ;;
  *) exit 0 ;;
esac

# HEAD@{1}은 직전 HEAD 위치. 바뀐 게 없으면 아무것도 하지 않는다
changed=$(git diff --name-only 'HEAD@{1}' HEAD -- .claude CLAUDE.md 2>/dev/null | tr '\n' ' ' | sed -E 's/ +$//')
[ -z "$changed" ] && exit 0

printf '{"hookSpecificOutput":{"hookEventName":"PostToolUse","additionalContext":"[훅 알림] 지침 파일이 바뀌었습니다: %s. 세션 시작 때 읽은 내용과 다를 수 있으니 진행 전에 다시 읽으세요."}}\n' "$changed"
