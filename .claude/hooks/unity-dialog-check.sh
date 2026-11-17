#!/usr/bin/env bash
# unity 명령이 Editor를 찾지 못하거나 Pipeline 연결에 실패하면,
# unity-guidelines.md 8장(에디터 대화상자) 절차를 Claude 맥락에 알린다. 도구 실행은 막지 않는다.
input=$(cat)

# 명령 문자열 자체에 같은 문구가 있어도 오탐하지 않도록 tool_input.command 값을 지우고 검사한다
output=$(printf '%s' "$input" | sed -E 's/"command" *: *"([^"\\]|\\.)*"//')

if ! printf '%s' "$output" | grep -qE '인스턴스를 찾을 수 없습니다|서버에 연결할 수 없습니다|timed out|시간 초과'; then
  exit 0
fi

event=$(printf '%s' "$input" | grep -oE '"hook_event_name" *: *"[A-Za-z]+"' | head -1 | sed -E 's/.*"([A-Za-z]+)"$/\1/')
[ -z "$event" ] && event="PostToolUse"

printf '{"hookSpecificOutput":{"hookEventName":"%s","additionalContext":"[훅 알림] unity-guidelines.md 8장 에디터 대화상자 신호입니다. 원인을 추측하지 말고 editor status를 1회만 확인한 뒤 멈추세요. 실행하던 명령, 예상 대화상자 종류(Scene 저장, Script 업데이트, Safe Mode, 임포트 확인 등), 확인할 문구와 버튼을 사용자에게 묻고, 중간 보고의 주의/미검증에 기록하세요."}}\n' "$event"
