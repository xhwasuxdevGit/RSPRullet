#!/usr/bin/env bash
# 앱 작업 목록에서 다음 단계로 넘길 때(update_plan advance) 직전 단계의 중간 보고를 썼는지 Claude 맥락에 알린다.
input=$(cat)

if ! printf '%s' "$input" | grep -qE '"advance" *: *true'; then
  exit 0
fi

printf '{"hookSpecificOutput":{"hookEventName":"PreToolUse","additionalContext":"[훅 알림] 다음 단계로 넘기기 전에 직전 단계의 [작업 결과] N단계 블록 전체를 턴의 마지막 메시지로 보내고 사용자 확인을 받았는지 확인하세요 (behavior-guidelines.md 8장). 받지 않았다면 이 턴에서 다음 단계로 넘어가지 말고 보고 후 멈추세요."}}\n'
