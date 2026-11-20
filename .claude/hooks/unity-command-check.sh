#!/usr/bin/env bash
# unity CLI 명령을 실행하기 전에 unity-guidelines.md 8장 형식인지 검사하고, 어기면 실행을 막는다.
# 검사: unity 호출마다 바로 앞에 "timeout 60", 백그라운드 실행 금지, unity 출력을 같은 구간에서 파이프로 넘기지 않음
input=$(cat)

# unity라는 글자가 없는 명령은 바로 통과시킨다
printf '%s' "$input" | grep -q 'unity' || exit 0

printf '%s' "$input" | node -e '
let raw = "";
process.stdin.on("data", chunk => raw += chunk);
process.stdin.on("end", () => {
  const toolInput = JSON.parse(raw).tool_input || {};
  const command = toolInput.command || "";

  // 명령 위치(줄 처음, ; & | ( $( 뒤, do/then/else 뒤)에 오는 unity 호출만 본다.
  // grep "unity command"처럼 문자열로 언급만 하는 경우는 명령 위치가 아니라 검사하지 않는다
  const pattern = /(?:^|[\n;&|(]|\$\()\s*(?:(?:do|then|else)\s+)?(?:timeout\s+(\S+)\s+)?unity(?=\s|$)/g;
  const problems = [];
  let found = false;
  let match;
  while ((match = pattern.exec(command)) !== null) {
    found = true;
    if (match[1] === undefined) {
      problems.push("timeout 60 없이 unity를 실행함");
    } else if (match[1] !== "60") {
      problems.push("timeout " + match[1] + " 사용 (60만 허용)");
    }

    // 같은 구간(다음 ; && || 줄바꿈 전)에서 단일 파이프로 출력을 넘기면 종료 코드가 가려진다
    const rest = command.slice(pattern.lastIndex).split(/\n|;|&&|\|\|/)[0];
    if (/(^|[^|])\|(?!\|)/.test(rest)) {
      problems.push("unity 출력을 파이프로 넘김 (종료 코드 124를 볼 수 없음, 출력은 파일로 저장)");
    }
  }

  if (found && toolInput.run_in_background) {
    problems.push("unity 명령을 백그라운드로 실행함");
  }

  if (problems.length === 0) {
    return;
  }

  const reason = "[훅 차단] " + problems.join(" / ") +
    ". unity 명령은 timeout 60 unity … 형태로, 출력은 파일로 저장해 실행하세요. 60초를 넘길 테스트는 test run --async-tests true 뒤 test status로 읽으세요 (unity-guidelines.md 8장).";
  process.stdout.write(JSON.stringify({
    hookSpecificOutput: {
      hookEventName: "PreToolUse",
      permissionDecision: "deny",
      permissionDecisionReason: reason
    }
  }));
});
'
