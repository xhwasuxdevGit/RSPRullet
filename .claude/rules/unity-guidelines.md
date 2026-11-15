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
