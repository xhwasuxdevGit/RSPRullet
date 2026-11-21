---
name: git-workflow
description: Git 브랜치·커밋·Push·PR 정책. 기능 브랜치 생성, 커밋 메시지 작성, Push, PR 제목·본문 작성, 실행 절차는 start-issue·create-pr 스킬.
disable-model-invocation: false
---

# Git 브랜치 및 커밋 정책

> 이 문서는 **정책**만 정의하며 실제 실행 순서는 `start-issue`와 `create-pr` 스킬에서 관리한다.

## 1. 브랜치 역할
| 브랜치 | 역할 |
| --- | --- |
| `main` | 기능 통합·충돌 및 버그 검증. 기능 브랜치에서 PR로 병합 |
| `RSP-<번호>-<jira-issue-name>` | Jira 이슈 단위 기능 개발 브랜치. **최신 `main`에서 분기** |

## 2. 필수 흐름
- 기능 브랜치는 **항상 최신 `main`** 기준으로 생성한다.
- 기능 완료 및 로컬 검증 후 `main`으로 PR을 생성한다.
- `main` 직접 Push 금지. PR 병합은 사용자/리뷰어가 결정한다.
- 기능 브랜치는 PR 병합 후 삭제를 원칙으로 하되 Claude가 삭제하려면 승인을 받는다.

## 3. 기능 브랜치명
- 형식: `RSP-<실제 이슈 번호>-<영문 소문자 kebab-case 이슈명>`
- Jira 이슈 키는 **`RSP-00`**이고, 브랜치명은 **`RSP-00-core-rule-design`**처럼 서로 다르다.
- 이슈 제목을 실제 Jira에서 조회하여 의미를 보존하는 짧은 영문 slug로 만든다. 공백·특수문자는 `-`로 정리한다.
- 제목을 조회할 수 없다면 임의로 만들지 말고 확인을 요청한다.
- 기존 브랜치를 임의로 이름 변경하거나 중복 생성하지 않는다.

## 4. 커밋 가이드
- `[feat]`: 기능 추가, 변경, 삭제 등 개발 전반
- `[fix]`: 개발 진행 중 **Bug Issue 티켓** 관련 수정
- `[docs]`: 문서 작업
- `[chore]`: 코드 컨벤션 맞춤 및 기타 작업

**형식:** `[type] 변경 내용 요약`

예:
```text
[feat] 아이템 커피 효과 구현
[fix] 카드 조합 효과 미적용 오류 수정
[docs] Claude 공통 문서 정리
[chore] C# 네이밍 정리
```
- 유형은 실제 Jira 이슈 유형과 변경 목적에 따라 선택한다.
- 관련 파일만 커밋한다. Unity 에셋과 `.meta`의 GUID를 보존한다.
- 캐시·빌드 산출물·비밀 정보는 커밋하지 않는다.

## 5. PR 정책
- 기능 PR: `RSP-번호-이슈명` → `main`
- PR 본문에 Jira 이슈, 변경사항, 주요 파일, 검증 결과, 미검증 항목, 위험/리뷰 포인트, 문서 변경 메모(`create-pr`)를 기재한다.
- Claude는 승인 없이 PR을 병합하지 않는다.

### 5.1 PR 제목
PR 제목은 Merge commit 제목으로 그대로 사용된다(5.2). 개별 커밋 메시지(4장)와 역할을 구분한다.

| PR | 형식 | 예 |
| --- | --- | --- |
| 기능 → `main` | `Merge: RSP-번호 작업명` | `Merge: RSP-53 딜러 AI 1규칙 구현` |

- 기능 PR은 Jira 이슈 번호를 포함하고 작업명을 간결하게 쓴다.
- PR 번호는 제목에 쓰지 않는다. GitHub가 Merge commit 제목에 자동으로 붙인다.

### 5.2 Merge commit
- 병합 전략은 **Create a merge commit**을 유지한다. Squash merge, Rebase merge로 바꾸지 않는다.
- GitHub 저장소 설정 `Allow merge commits → Default to pull request title`을 전제로, PR 제목이 Merge commit 제목이 된다.

## 6. 안전
- 작업 트리의 미커밋 변경을 임의로 삭제·덮어쓰기·stash하지 않는다.
- 승인 없는 강제 Push, `git reset --hard`, `git clean`, 브랜치 삭제 및 임의 merge/rebase 금지.
- `main`과의 차이가 있으면 충돌 위험을 확인하고 사용자에게 보고한다.
- PR 생성만으로 Jira 이슈를 Done으로 전환하지 않는다.
