---
name: create-pr
description: 기능 브랜치의 변경사항을 검증하고 커밋한 뒤 main 대상으로 GitHub Pull Request를 작성할 때 사용한다.
disable-model-invocation: false
---

# 기능 PR 생성

> 브랜치명·커밋 유형·PR 제목 형식은 `git-workflow` 스킬(`.claude/skills/git-workflow/SKILL.md`)이 원본이다. 이 세션에서 아직 읽지 않았다면 먼저 읽는다.

## 사전 확인
1. 현재 브랜치가 `RSP-번호-이슈명` 형식인지 확인한다. Jira 이슈 키는 `RSP-번호`만 추출한다.
2. `git status`, 변경 파일, diff, 원격 및 `main` 대상 상태를 확인한다.
3. 미커밋 사용자 변경과 작업 범위 밖의 파일을 임의로 포함하지 않는다.
4. 해당 Jira 이슈의 완료 조건을 확인한다. 이 세션에서 이미 조회했다면 다시 조회하지 않는다. 기술문서는 문서와 코드 대조가 필요할 때만 확인한다.
5. 검증은 `behavior-guidelines.md` 7장의 작업 유형별 기준으로 수행한다. 이미 수행한 검증은 결과를 재사용하고, 못 한 검증은 미실행으로 명시한다.
6. 작업 범위 위반 여부를 확인한다.
   - 승인받지 않은 파일 변경 (STANDARD 계획 범위 포함)
   - `Assets/RSPRullet/Scenes/Main/` 무단 변경
   - Scene 폴더 변경(`unity-guidelines.md`)
   - Jira 작업 범위와 실제 변경사항의 불일치
   - 위반이 있으면 변경사항을 폐기·되돌리지 않고 해당 파일과 내용을 사용자에게 보고한 뒤 처리 방향을 확인한다. 확인 전에는 해당 파일을 stage하지 않는다.

## 커밋
- `git-workflow` 스킬 4장의 유형과 형식(`[type] 변경 내용 요약`)을 적용한다.
- 관련 파일만 명시적으로 stage하고, 변경사항을 확인한 뒤 커밋한다.
- 사용자 요청 범위에 커밋·Push·PR 생성이 포함되는지 확인한다. 포함되지 않으면 계획만 제안한다.

## PR
1. GitHub CLI(`gh`) 인증 및 저장소를 확인한다.
2. 기능 PR은 반드시 `RSP-번호-이슈명` → `main`으로 생성한다. 제목·본문은 `git-workflow` 스킬 5장을 따른다.
3. 생성된 PR URL과 실제 상태를 보고한다. PR 자동 병합 금지.

## 개발 완료 후
- 사용자에게 기술문서를 신규 작성하거나 기존 문서에 반영할지, Jira 문서화 이슈가 필요한지 질문한다 (`jira-workflow` Phase 10).
- 승인받으면 `write-tech-doc` 스킬을 사용한다.
- PR 생성만으로 Jira Done 처리하지 않는다.
