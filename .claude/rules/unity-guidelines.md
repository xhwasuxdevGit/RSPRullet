---
paths:
  - "RSPRullet/Assets/**"
---

# Unity 프로젝트 개발 지침

## 1. 개발 환경
- Unity 6 LTS `6000.3.9f1`을 기준으로 한다. 승인 없이 Editor나 패키지를 업그레이드하지 않는다.
- Scene이나 에셋을 수정하기 전에 공식 Unity 연동 기능에서 확인할 수 있는 실제 프로젝트 상태를 조사한다.

## 2. 프로젝트 폴더
새 게임 콘텐츠는 `Assets/` 아래에 배치한다.

```text
Assets/
├── Art/{Characters,Backgrounds,UI,Items}
├── VFX/
├── Audio/
├── Animations/{Characters,UI,Items}
├── Prefabs/{Characters,UI,Effects,Items}
├── Scenes/{Main,Test}  
├── Scripts/{Battle,Character,Enemy,UI,Core,Enums,Interfaces,Items,Utilities}
├── SO/
├── Tests/EditMode/
└── Settings/
```

- 게임 코드는 `Scripts/`의 `RSPRullet.asmdef` 어셈블리에, EditMode 테스트는 `Tests/EditMode/`의 `RSPRullet.Tests.EditMode.asmdef` 어셈블리에 둔다. 게임 코드에서 다른 패키지를 쓰면 `RSPRullet.asmdef`의 참조 목록에 해당 어셈블리가 있는지 확인한다.
- ScriptableObject 에셋(`.asset`)은 `Assets/SO/`에 둔다. SO 클래스 스크립트(`.cs`)는 `Scripts/` 하위에 둔다.

- 기존 Editor 빌드 스크립트는 `Assets/Editor/`에 있을 수 있다.
- 기존 폴더 구조를 따르며 중복 폴더를 만들지 않는다.

## 3. Scene 및 에셋 보호
- `.meta` 파일과 GUID 참조를 보존한다. 에셋을 임의로 재생성하지 않는다.
- Scene 또는 Prefab 전체를 불필요하게 재작성하지 않는다. Component 또는 필드 변경 전에 직렬화 참조를 확인한다.
- 명시적 승인 없이 Assembly Definition 파일이나 신규 패키지를 추가하지 않는다.
- Input System 및 렌더 파이프라인은 프로젝트의 현재 설정을 확인하고 따른다.

## 4. 검증
- 가능한 경우 Unity Console 및 컴파일 상태, 관련 Scene/Prefab, Play Mode 동작을 확인한다.
- 공식 플러그인이 특정 Editor 작업을 지원하지 않으면 존재하지 않는 명령어를 만들어 내지 말고 한계를 보고한다.

### Play 검증 줄이기
- 로직은 EditMode 테스트로 먼저 검증한다. Play 검증은 화면 배치·연결·흐름처럼 실행해야만 보이는 항목에만 쓴다.
- Play에 들어가기 전에 확인할 항목을 목록으로 정하고, Play Mode 진입 1회에 모두 확인한다.
- 확인하려는 상태까지 처음부터 플레이하지 않는다. 가능하면 `script eval`로 해당 메서드를 직접 호출해 그 상태로 바로 간다(코드 수정 없이 가능할 때만).
- 값과 상태는 `script eval`이나 Console 로그로 읽는다. 스크린샷은 화면 배치를 볼 때만, 확인 지점마다 1장 이하로 `--max-resolution 512` 이하로 찍는다.

### 사용자에게 맡기는 Play 검증
- 검증할 상황이 무작위(난수로 정해지는 결과, AI·NPC의 선택, 확률 판정, 타이밍에 따라 달라지는 상황 등)로 나와야만 만들어지고, 위 방법으로 바로 만들 수 없으면 Claude가 반복 플레이하지 않고 사용자에게 검증을 요청한다.
- 요청에는 테스트 Scene 경로, 그 상황을 만드는 방법, 확인할 항목과 기대 결과를 체크리스트로 적는다.
- 사용자가 결과를 알려 주기 전까지 해당 항목은 중간 보고에 "사용자 검증 대기"로 적고, 통과로 처리하지 않는다.

## 5. 씬 관리 규칙

### Main Scene 보호
**`Assets/Scenes/Main/`의 Scene은 명시적인 사용자 승인 없이 절대 수정하지 않는다.**
- 개인 기능 개발 및 테스트에 Main Scene을 사용하지 않는다.
- Main Scene의 GameObject, Component, Inspector 설정을 임의로 변경하지 않는다.
- 테스트 후 원상 복구하는 방식도 금지한다.
- 읽기 전용 구조 확인은 허용한다. 이때 Scene을 저장하지 않는다.
- Main Scene 변경이 필요하면 변경 이유, 영향 범위, 수정 파일을 먼저 브리핑하고 승인받는다.
- Jira 이슈에 Main Scene 작업이 포함되어 있더라도 명시적 승인 여부가 불분명하면 먼저 확인한다.

### Scene 수정이 필요 없는 작업
순수 C# 클래스 작성, Scene과 무관한 로직 수정, 유틸리티 구현, 코드 분석, 정적 검증에서는 불필요하게 Scene을 생성하거나 수정하지 않는다.

## 6. Unity Pipeline CLI 사용
- 에디터 조작은 `unity` CLI(`unity command <group> <name>`)로 한다. `unity-pipeline` 스킬의 절차를 따르되, 명령·플래그는 아래 표와 실제 목록(`unity command <group>`)을 우선한다.
- 스킬 문서는 CLI 1.0.0 / `com.unity.pipeline` 1.0.0과 다른 곳이 있다(2026-11-16 대조). 플래그는 하이픈(kebab-case)만 허용되고 밑줄 표기는 거부된다.

| 스킬 문서 | 실제 |
|---|---|
| `unity pipeline list` | `unity pipeline editors` |
| `editor set_auto_tick --enable` | `--enabled` |
| `--dry_run`, `--async_tests`, `--filter_type`, `--assemblyDir` | `--dry-run`, `--async-tests`, `--filter-type`, `--assembly-dir` |
| `script eval_file <경로>` | `script eval --file <경로>` |
| `test run --mode playmode` | `--async-tests` 함께 지정 |
| `capture screenshot --view`, `--output` | `capture game_view` / `scene_view` / `editor_window` |
| `--save_path`, `--max_resolution`, `--include_inline_image` | `--output-path`, `--max-resolution`, `--include-inline-image` |

- 표에 없는 명령은 실행 전에 `unity command --query <단어>`로 확인한다. 패키지나 CLI를 업그레이드한 뒤에는 이 표를 다시 대조한다.

## 7. CLI 출력 줄이기
- 명령을 찾을 때는 그룹 전체 목록(`unity command <group>`) 대신 `unity command --query <단어>`를 먼저 쓴다.
- 상태 확인은 짧은 명령부터 한다: `editor status`, `console status`, `script recompile_status`. 로그는 문제가 있을 때만 `console get --level error --tail 20`처럼 수준과 개수를 제한해 읽는다.
- 테스트는 `test list`로 전체 목록을 뽑지 않고, `test run --filter <클래스명>` 또는 `--filter-type Assembly`로 범위를 좁혀 실행한다. 비동기 실행 결과는 `test status --detail failures`(실패만) 또는 `summary`(개수만)로 읽는다.
- 화면 캡처는 `--output-path`로 파일에 저장하고, 이미지를 직접 봐야 할 때만 `--max-resolution 512` 이하로 인라인 이미지를 받는다.

## 8. 에디터 대화상자로 작업이 멈출 때
- 모든 `unity` 명령은 시간 제한 60초(`timeout 60`)로 실행한다. 60초 안에 응답이 없으면(종료 코드 124) 대화상자로 보고 멈춘 뒤, 실행하던 명령을 알리고 사용자에게 처리를 요청한다. 사용자가 처리했다고 알리면 멈췄던 단계를 다시 진행한다.
