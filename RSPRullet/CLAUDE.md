# Fourbeat — Claude Code 프로젝트 공통 지침

## 프로젝트
- Unity 6 LTS `6000.3.9f1` 기반 . 적 NPC와 대결하는 심리전 겜블게임
- 1인 개발 과제 프로젝트, 수준은 유니티 입문자 수준
- 요청 범위의 최소 변경, 가독성, 기존 구조와의 일관성을 우선하며 불필요한 추상화를 피한다.
- Unity 공식 연동 도구는 현재 세션에서 실제 사용 가능한 기능을 확인한 뒤 사용한다.
- 사용자 대상 응답·분석 브리핑·작업 결과 보고·Jira 설명·Confluence 기술문서는 한국어로 작성한다. 코드 식별자·API·파일 경로·기술 용어는 필요에 따라 원문을 유지하며, 사용자가 다른 언어를 명시적으로 요청한 경우에만 예외로 한다.

## 지침 위치 (로딩 방식)
상시 로드는 이 문서와 `.claude/rules/behavior-guidelines.md`(실행 모드·승인·조사·검증·보고)뿐이다. 나머지는 해당 작업에서만 로드한다.

| 상황 | 지침 | 로딩 |
|---|---|---|
| C# 파일 작업 | `.claude/rules/coding-conventions.md` | `**/*.cs` 파일을 읽을 때 자동 |
| Unity 에셋·Scene·Prefab 작업 | `.claude/rules/unity-guidelines.md` | `Assets/**` 등 파일을 읽을 때 자동. **Unity MCP·Editor 도구로 Scene/Prefab/에셋을 조작하기 전에는 직접 읽는다** (`paths`는 파일 읽기에만 반응) |
| 개발 작업의 Jira 워크플로우, 이슈 생성·수정·상태 전환 | `jira-workflow` 스킬 (+ `issue-template.md`) | 해당 작업 시 스킬 사용 |
| 브랜치·커밋·Push·PR | `git-workflow` 스킬 | 해당 작업 시 스킬 사용 |
| Jira 이슈로 작업 시작 | `start-issue` 스킬 | `/start-issue FB-87` 또는 자동 선택 |
| 커밋·PR 생성 | `create-pr` 스킬 | 자동 선택 또는 명시 호출 |
| 기술문서 작성·갱신 | `write-tech-doc` 스킬 | 승인 후 사용 |

- 위 작업을 시작할 때 해당 스킬을 아직 읽지 않았다면 먼저 읽는다. 한 세션에서 이미 읽은 스킬은 다시 읽지 않는다.
- Confluence 기획서·기술문서 링크와 조회 정책은 `start-issue`·`write-tech-doc` 스킬에 있다. 문서 작업이 아니면 Confluence를 조회하지 않는다.

## 필수 안전 규칙 (스킬 미로드 시에도 적용)
- **Git:** `main` 브랜치에서 직접 Push 금지. 기능 브랜치는 최신 `main`에서 `RSP-<번호>-<영문-kebab-이슈명>`으로 만든다. 커밋은 `[feat|fix|docs|chore] 요약`. PR 자동 병합 금지.
- **Jira:** 개발 작업(기능·버그·리팩토링·에셋 변경)은 Jira 이슈 기준으로 진행한다. Jira 키 `RSP-XX`과 브랜치명 `RSP-XX-...`를 구별한다. 실제 조회하지 않은 이슈 키·상태·URL을 만들지 않는다. PR 생성만으로 Done 처리하지 않는다.
- **승인:** `behavior-guidelines.md` 5장의 "항상 승인이 필요한 작업"을 따른다.
- 인증 정보·토큰·개인 로컬 설정을 커밋하지 않는다.

## 게임 구조의 중요 제약
- 실제 구현과 설계가 다를 수 있으므로 코드와 문서를 확인한 뒤 변경한다.

## Unity Scene 정책
- Scene 기반 구현·테스트는 `Assets/Scenes/test/` 에 마련된 test Scene에서 한다. 
- `Assets/Scenes/Main/`은 명시적 승인 없이 수정하지 않는다(읽기 전용 확인만 허용). 

