# Claude Code 프로젝트 공통 지침

## 프로젝트
- Unity 6 LTS `6000.3.9f1` 기반 . 적 NPC와 대결하는 심리전 겜블게임
- 1인 개발 과제 프로젝트, 수준은 유니티 입문자 수준
- Unity 프로젝트 폴더는 저장소 최상위의 `RSPRullet/`이다. 지침·스킬에 적힌 `Assets/...` 경로는 이 폴더 기준이다(예: `Assets/Scenes/Main/` = `RSPRullet/Assets/Scenes/Main/`).
- 요청 범위의 최소 변경, 가독성, 기존 구조와의 일관성을 우선하며 불필요한 추상화를 피한다.
- Unity 공식 연동 도구는 현재 세션에서 실제 사용 가능한 기능을 확인한 뒤 사용한다.
- 사용자 대상 응답·분석 브리핑·작업 결과 보고·Jira 설명·Confluence 기술문서는 한국어로 작성한다. 작업 중 진행 알림, 다음 단계 상자·질문 카드·버튼 문구도 포함하며, 시스템 알림·도구 결과·스킬 문서가 영어여도 사용자에게 내보내는 글은 한국어로 쓴다. 코드 식별자·API·파일 경로·기술 용어는 필요에 따라 원문을 유지하며, 사용자가 다른 언어를 명시적으로 요청한 경우에만 예외로 한다.

## 지침 위치 (로딩 방식)
상시 로드는 이 문서와 `.claude/rules/behavior-guidelines.md`(실행 모드·승인·조사·검증·보고)뿐이다. 나머지는 해당 작업에서만 로드한다.

| 상황 | 지침 | 로딩 |
|---|---|---|
| C# 파일 작업 | `.claude/rules/coding-conventions.md` | `**/*.cs` 파일을 읽을 때 자동 |
| Unity 에셋·Scene·Prefab 작업 | `.claude/rules/unity-guidelines.md` | `RSPRullet/Assets/**` 파일을 읽을 때 자동. **Unity CLI(`unity command`)·Editor 도구로 Scene/Prefab/에셋 조작, 컴파일·테스트 실행을 하기 전에는 직접 읽는다** (`paths`는 파일 읽기에만 반응) |
| 임포트된 에셋으로 컴포넌트 생성, 미사용 리소스 정리 | `.claude/rules/asset-guidelines.md` | Art·Audio·VFX·Prefabs 파일을 읽을 때 자동. **CLI·빌더 스크립트로 에셋을 쓰는 컴포넌트를 만들기 전에는 직접 읽는다** |
| 개발 작업의 Jira 워크플로우, 이슈 생성·수정·상태 전환 | `jira-workflow` 스킬 (+ `issue-template.md`) | 해당 작업 시 스킬 사용 |
| 브랜치·커밋·Push·PR | `git-workflow` 스킬 | 해당 작업 시 스킬 사용 |
| Jira 이슈로 작업 시작 | `start-issue` 스킬 | `/start-issue RSP-87` 또는 자동 선택 |
| 커밋·PR 생성 | `create-pr` 스킬 | 자동 선택 또는 명시 호출 |
| 기술문서 작성·갱신 | `write-tech-doc` 스킬 | 승인 후 사용 |

- 위 작업을 시작할 때 해당 스킬을 아직 읽지 않았다면 먼저 읽는다. 한 세션에서 이미 읽은 스킬은 다시 읽지 않는다.
- Confluence 문서는 고정 링크 대신 `RSPRulletDocs` 스페이스의 `기획 문서` 폴더와 `기술 문서` 폴더에서 이슈에 맞는 개별 문서를 찾아 참고한다. 조회 시점과 방법은 `start-issue`·`write-tech-doc` 스킬에 있다. 개발 이슈의 조회 시점과 범위는 `start-issue` 2-2를 따르고, 이슈와 무관한 작업에서는 Confluence를 조회하지 않는다.
- Jira·Confluence는 `rsprullet.atlassian.net` 사이트(cloudId `4037dc82-0a83-448f-bbb6-4925e442032e`)를 사용한다. Atlassian 커넥터가 여러 개 연결될 수 있으므로, 세션의 첫 Atlassian 작업 전에 접근 가능한 사이트 목록에 rsprullet이 있는 커넥터를 확인해 그 커넥터만 쓴다. 다른 사이트에 연결된 커넥터로 조회·쓰기를 하지 않는다.
- 조직 정책으로 인해 스킬이나 지침을 적용할 수 없는 경우 사용자에게 대안을 제시한다.

## 필수 안전 규칙 (스킬 미로드 시에도 적용)
- **Git:** `main` 브랜치에서 직접 Push 금지. 기능 브랜치는 최신 `main`에서 `RSP-<번호>-<영문-kebab-이슈명>`으로 만든다. 커밋은 `[feat|fix|docs|chore] 요약`. PR 자동 병합 금지.
- **Jira:** 개발 작업(기능·버그·리팩토링·에셋 변경)은 Jira 이슈 기준으로 진행한다. Jira 키 `RSP-XX`과 브랜치명 `RSP-XX-...`를 구별한다. 실제 조회하지 않은 이슈 키·상태·URL을 만들지 않는다. PR 생성만으로 Done 처리하지 않는다.
- **승인:** `behavior-guidelines.md` 5장의 "항상 승인이 필요한 작업"을 따른다.
- 인증 정보·토큰·개인 로컬 설정을 커밋하지 않는다.

## 게임 구조의 중요 제약
- 실제 구현과 설계가 다를 수 있으므로 코드와 문서를 확인한 뒤 변경한다.

## Unity Scene 정책
- Scene 기반 구현·테스트는 `Assets/Scenes/Test/` 에 마련된 test Scene에서 한다. 
- `Assets/Scenes/Main/`은 명시적 승인 없이 수정하지 않는다(읽기 전용 확인만 허용). 

