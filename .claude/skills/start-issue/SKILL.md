---
name: start-issue
description: 개발 작업을 시작할 때 Jira 이슈(RSP-번호)를 조회하거나 관련 이슈 검색·신규 생성을 확인하고, 필요한 Confluence 문서 확인, 작업 브리핑, Jira 업데이트, 최신 main 기반 기능 브랜치 준비까지 수행할 때 사용한다.
disable-model-invocation: false
---

# Jira 이슈 기반 개발 시작

> `jira-workflow` 스킬(`.claude/skills/jira-workflow/SKILL.md`)의 Phase 1~3을 실행하고 Phase 4로 넘기는 절차다. 이 세션에서 아직 읽지 않았다면 먼저 읽는다. 승인 시점과 정책은 해당 스킬과 `behavior-guidelines.md`를 따르며 여기서 반복하지 않는다.

## 입력
- Jira 이슈 키 `RSP-<번호>` 또는 작업 요청 내용을 받는다.
- `$ARGUMENTS`가 있으면 이슈 키를 추출한다. 실제 Jira 키는 브랜치 slug가 아니다.

## 1. Jira 이슈 확인 (Phase 1)
1. 이슈 키가 있으면 Atlassian MCP로 제목, 유형, 설명, 완료조건, 상태, 담당자, 관련 링크를 한 번 조회한다. 실패하면 중단하고 알린다.
2. 이슈 키가 없으면 요청 내용으로 관련 기존 이슈를 JQL 검색하고, 후보를 제시해 대상 이슈를 확인받는다.
3. 관련 이슈가 없으면 요청 내용을 기준으로 신규 이슈를 생성할지 묻는다.
4. 생성하기로 하면 `.claude/skills/jira-workflow/issue-template.md`로 초안을 작성한다. 배경/목적이 확인되지 않으면 질문하고, 초안 승인 후에만 생성한다. 생성 후 실제 이슈 키를 보고한다.

## 2. 조사 (읽기 전용)
1. 실행 모드(`behavior-guidelines.md` 4장)를 정하고 작업을 **설계 / 기능 개발 / 혼합**으로 분류한다.
2. Confluence는 필요한 경우에만 관련 문서를 조회한다 (QUICK: 조회하지 않음 / STANDARD: 이슈·코드만으로 판단할 수 없을 때 / DEEP: 관련 문서 조회).
   - 설계 시: Confluence `기획 문서` 폴더의 하위 페이지 목록을 조회하고, 이슈 내용에 맞는 개별 기획 문서만 읽는다.
     - Jira 이슈의 근거가 컨셉 기획서여도 같은 주제의 세부 기획서가 있으면 세부 기획서를 기준으로 한다. 제목 검색으로 대신하지 않고 폴더의 하위 페이지 목록을 확인한다.
   - 개발 시: Confluence `기술 문서` 폴더의 하위 페이지 목록을 조회하고, 이슈 내용에 맞는 개별 기술 문서만 읽는다.
   - 맞는 문서를 특정할 수 없으면 후보 문서 제목을 제시하고 사용자에게 확인한다.
   - 링크를 안다는 이유만으로 읽었다고 주장하지 않는다. 문서 접근 실패, 문서 간 충돌, 코드와의 불일치는 알리고 추측으로 확정하지 않는다. 문서가 없는 기능은 Jira 요구사항과 실제 코드를 확인한다.
3. 수정 대상 코드와 직접 호출 관계, 관련 Scene/Prefab을 **읽기 전용으로** 확인하고 범위·영향 파일·검증 계획을 정리한다.
   - Scene 기반 작업이면 `unity-guidelines.md`에따라 Scene 폴더를 결정한다. 확정할 수 없으면 사용자에게 확인한다.
4. `git status`와 현재 브랜치를 확인한다. 미커밋 변경이 있으면 보존하고 사용자에게 처리 방향을 묻는다.
5. 원격 최신 `main`을 확인하고 `git-workflow` 스킬 3장으로 브랜치명을 정한다. 기존 브랜치 여부를 확인한다.

## 3. 작업 브리핑 (Phase 2)
1. 모드별 형식으로 작성한다 (QUICK 생략 / STANDARD 3~5줄 계획 / DEEP `jira-workflow` Phase 2 항목 포함 상세 브리핑). 브랜치명, **Jira에 반영할 내용**을 포함한다.
2. 범위가 하나의 이슈로 크면 Sub-task 생성 또는 이슈 분리를 제안한다. 승인되면 1단계로 돌아가 이슈를 정리한다.
3. 계획 수립만 요청받았다면 같은 내용을 계획으로 제시하고 멈춘다.
4. DEEP이거나 `behavior-guidelines.md` 5장의 사전 승인 조건에 해당하면 명시적 승인을 기다린다. **Jira 쓰기는 모드와 무관하게 승인 전 하지 않는다.**

## 4. Jira 업데이트 및 검수 (Phase 3)
1. 승인받은 항목만 Jira에 반영한다 (설명 보완은 `issue-template.md`, 상태 전환은 가능한 전환을 조회한 뒤 수행).
2. 이슈를 다시 조회해 실제 반영 내용을 보고하고 사용자 검수를 받는다. 수정 요청이 있으면 반영 후 다시 검수받는다.

## 5. 브랜치 준비
1. 안전한 경우에만 **최신 `main` 기준**으로 브랜치를 생성한다. 예:
   ```bash
   git fetch origin
   git switch main
   git pull --ff-only origin main
   git switch -c RSP-xx-rhythm-system-design
   ```
   명령은 예시다. 현재 Git 상태에 맞게 조정한다.
2. `main`과 차이 및 충돌 위험을 보고한다. 승인 없이 merge/rebase하지 않는다.

## 6. 업무 수행으로 전환 (Phase 4~)
- 확정된 범위에서만 구현하고 `behavior-guidelines.md` 7장 기준으로 검증한다. 요청이 계획 수립뿐이면 구현하지 않는다.
- 범위가 바뀌면 `jira-workflow` Phase 5를 따른다.
- 이후 절차는 `jira-workflow` Phase 6~11과 `create-pr`, `write-tech-doc` 스킬을 따른다.

## 마무리
- `behavior-guidelines.md` 8장 형식으로 Jira 키/제목, Jira 반영 내용, 조회한 문서, 브랜치명, 계획, 남은 질문을 보고한다.
