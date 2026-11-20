#!/usr/bin/env bash
# unity 명령이 60초 안에 끝나지 않았으면(종료 코드 124) 에디터 대화상자로 보고 멈추라고 Claude 맥락에 알린다 (unity-guidelines.md 8장).
# 명령이 실패로 끝나면 PostToolUseFailure, 종료 코드를 출력으로 찍고 성공으로 끝나면 PostToolUse에서 불린다
input=$(cat)

printf '%s' "$input" | grep -q 'unity' || exit 0
printf '%s' "$input" | grep -qiE 'exit code:? *124|exit=124' || exit 0

event=$(printf '%s' "$input" | grep -oE '"hook_event_name" *: *"[A-Za-z]+"' | sed -E 's/.*"([A-Za-z]+)"$/\1/')

printf '{"hookSpecificOutput":{"hookEventName":"%s","additionalContext":"[훅 알림] unity 명령이 60초 안에 끝나지 않았습니다(종료 코드 124). 에디터 대화상자로 보고 지금 멈추세요. 멈춘 명령을 사용자에게 알리고 처리를 요청한 뒤, 사용자가 처리했다고 직접 말할 때까지 unity 명령을 실행하지 마세요. 백그라운드 완료 알림이나 git 상태는 처리 확인이 아닙니다 (unity-guidelines.md 8장)."}}\n' "${event:-PostToolUse}"
