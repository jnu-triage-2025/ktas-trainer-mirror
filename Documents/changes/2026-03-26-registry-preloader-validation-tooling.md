# Registry Preloader Validation 도구 개편 (2026-03-26)

## 변경 목적
- Registry Preloader 리소스 무결성 검증을 에디터에서 빠르게 실행할 수 있도록 전용 검증 흐름을 정리합니다.
- 단일 SO 검증 중심에서, 별도 패널 기반의 선택형 검증 타겟 구조로 확장합니다.
- 아이템 등록 검증 타겟을 브랜치 기준 실제 클래스(`MultiplayerInfrastructureRegisterSupport`)에 맞춰 반영합니다.

## 핵심 변경 사항

### 1. Inspector 검증 구조 정리
- 대상: `Assets/Modules/MultiplayerInfrastructure/Editor/Registry/RegistryPreloaderControllerEditor.cs`
- 반영 내용:
  - 검증 로직을 재사용 가능한 형태로 유지 (패널에서도 동일 검증기 사용)
  - `Validate Open Scenes` 흐름 제거
  - Inspector에서는 `Validate This Preloader` 중심으로 단순화
  - 불필요한 `UnityEngine.SceneManagement` using 제거

### 2. 별도 검증 패널 도입
- 대상: `Assets/Modules/MultiplayerInfrastructure/Editor/Registry/RegistryPreloaderValidationPanel.cs`
- 메뉴:
  - `Tools/Multiplayer Infrastructure/Registry Preloader: Validate Registering Resources`
- 반영 내용:
  - `Validate Target` 선택형 UI 제공
  - SO 기반 타겟(ScenarioGraph/IconSprite/Npc/Waypoint/Entity/InteractableEntity/UIController) 검증 지원
  - `Validate Selected Target` / `Ping Selected SO` 동작 분리
  - `Ping Selected SO`는 실제 SO 선택 시에만 활성화

### 3. 아이템 등록 검증 타겟 교체
- 기존 논의 타겟: `TriageItemRegistrar`
- 최종 반영 타겟: `MultiplayerInfrastructureRegisterSupport`
- 반영 이유:
  - 브랜치 기준 중복 역할 클래스 정리로 실제 운영 대상이 `MultiplayerInfrastructureRegisterSupport`로 확인됨
- 검증 방식:
  - `RegisterAllItems()` 호출 후 `RegistryType.Item` 등록 상태 점검
  - 각 identifier에 대해 인스턴스 생성 가능 여부 확인
  - 아이콘 리소스 확인: `Resources/{DefaultsItemRegistry.ItemTexturesPath}/{identifier}`
  - 모델 리소스 확인: `Resources/Models/Items/{identifier}`

## 어셈블리 참조 이슈 대응
- 이슈:
  - 에디터 어셈블리에서 `MultiplayerInfrastructureRegisterSupport` 직접 참조 시 타입 해석/참조 오류 발생 가능
- 대응:
  - 패널에서 직접 타입 참조 대신 reflection 기반 타입 탐색으로 호출
  - 대상 타입명: `TriageTrainer.Items.MultiplayerInfrastructureRegisterSupport`
  - 정적 메서드 `RegisterAllItems()` 존재 여부 검사 후 호출
  - 타입/메서드 미발견 시 검증 리포트에 오류로 출력

## 사용 방법
1. Unity 메뉴에서 `Tools/Multiplayer Infrastructure/Registry Preloader: Validate Registering Resources` 실행
2. `Validate Target` 선택
3. SO 타겟은 해당 SO 할당 후 `Validate Selected Target` 실행
4. `MultiplayerInfrastructureRegisterSupport` 타겟은 SO 할당 없이 바로 검증 실행

## 검증 결과
- `Assets/Modules/MultiplayerInfrastructure/Editor/Registry/RegistryPreloaderValidationPanel.cs`: 컴파일 에러 없음
- `Assets/Modules/MultiplayerInfrastructure/Editor/Registry/RegistryPreloaderControllerEditor.cs`: 컴파일 에러 없음

## 영향 범위 요약
- Editor 툴링 레이어 변경이며, 런타임 플레이 로직 직접 변경은 없음
- 리소스 누락/등록 누락을 사전에 식별하는 검증 경로 강화