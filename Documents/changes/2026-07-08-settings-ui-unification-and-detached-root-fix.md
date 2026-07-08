# 설정 UI 통합(탭) + 오버레이 detached root 표시 버그 수정 + 카메라 POV 설정 (2026-07-08)

## 변경 목적
1. Esc 메뉴에서 오버레이(Esc 메뉴 자체, 키 설정, 그래픽 설정)가 열려도 화면에 표시되지 않던 버그를 수정합니다.
2. Esc 메뉴의 "키 설정" / "그래픽 설정" 두 버튼을 단일 "설정" 버튼으로 통합합니다.
3. 설정 창을 탭 형식(키 / 그래픽)으로 재구성합니다.
4. 그래픽 설정 탭에 3인칭 카메라 POV(거리) 조정 슬라이더를 추가하고, 값을 PlayerPrefs에 저장/로드합니다.

## 배경 / 근본 원인 (표시 안 됨)
모든 시스템 UI(`MI: ...`)는 하나의 PanelSettings(`Assets/UI Toolkit/PanelSettings.asset`)를 공유하며,
network-spawned 백엔드 프리팹의 자식으로 배치됩니다. UIDocument는 자체 라이프사이클(OnEnable)에서
`rootVisualElement`를 재생성해 패널에 부착합니다.

`GameEscapeMenuUIController` / `KeyConfigUIController` / `GraphicsSettingsUIController`는 `Awake`에서
`rootVisualElement`의 하위 요소(`_root`, 버튼 등)를 **단 한 번만** 캐시했습니다. 스폰/재활성 과정에서
`rootVisualElement`가 교체되면 캐시된 `_root`가 **패널에 부착되지 않은 detached 트리**를 가리키게 되어,
`_root.style.display = Flex`를 설정해도 실제 렌더링되는 요소에는 반영되지 않았습니다. (인벤토리에서
동일 원인의 버그를 2026-07-01에 수정한 바 있으나, 이 세 오버레이에는 재바인딩 규약이 누락되어 있었습니다.)

진단 로그로 실행 흐름(입력 → Push → OnOverlayPushed → SetVisible)이 모두 정상 수행됨을 확인했고,
`_root.panel`이 detached라 화면 미반영임을 특정했습니다.

## 핵심 변경 사항

### 1) detached root 재바인딩 적용 (표시 버그 수정)
- `GameEscapeMenuUIController.cs`: `BindViewToCurrentDocumentRoot()` 추가. Awake/OnEnable/표시 시점에
  현재 live `rootVisualElement` 기준으로 `_root`/버튼을 (재)바인딩. 버튼 구독 해제는 `DetachButtonHandlers()`로 분리.
- `KeyConfigUIController.cs`, `GraphicsSettingsUIController.cs`: `RebindToCurrentDocumentRoot()` 추가,
  `SetVisible(true)`에서 `_root == null || _root.panel == null`이면 재바인딩. (기존 컨트롤러는 통합 설정으로
  대체되지만, 에디터에서 프리팹 정리 전까지의 안전망 및 일관성을 위해 함께 수정.)

### 2) `PlayerController.EscapeMenu.cs` 폴백 보강
- `EnsureEscapeMenuController()`가 Registry 조회 실패 시 `FindFirstObjectByType`로 재시도하고, 실패 시
  경고 로그를 남기도록 보강(QuestUIController와 동일 규약). 표시 버그의 직접 원인은 아니었으나 일관성 확보.

### 3) 통합 설정 UI 신규 생성 (탭 형식)
- 신규 컨트롤러: `Assets/Modules/MultiplayerInfrastructure/Scripts/UI/Controllers/SettingsUIController.cs`
  (+ partial `SettingsUIController.Key.cs`, `SettingsUIController.Graphics.cs`)
  - `UIControllerABC` 상속, `IUIOverlay` 구현 → 기존 오버레이 스택 규약 동일.
  - UXML(뼈대: 헤더/탭바/컨텐츠/푸터)만 정적이고, 탭 버튼과 각 탭 컨텐츠(키 목록/키보드, 텍스처 품질/POV)는
    **코드로 동적 생성**합니다. (Unity 에디터 컴포넌트는 런타임 사용 불가하므로 UI Toolkit + 코드 생성이 정답.)
  - 키 탭: 기존 `KeyConfigEntryElement`, `KeyboardLayoutElement`, `KeyBindingRepository` 로직/요소를 이식/재사용.
  - 그래픽 탭: 기존 `TextureQualityOptionElement`, `TexturePerformanceService` 재사용 + POV 슬라이더 추가.
- 신규 UXML/USS: `Assets/Modules/MultiplayerInfrastructure/UIDocuments/SettingsUI.uxml`, `SettingsUI.uss`.
- `DefaultsUIDocument.SettingsUISortOrder = 8f` 추가.

### 4) Esc 메뉴 버튼 통합
- `GameEscapeMenuUI.uxml`: `key-config-button` + `graphics-settings-button` → 단일 `settings-button`("설정").
- `GameEscapeMenuUIController.cs`: `HandleKeyConfigClicked`/`HandleGraphicsSettingsClicked` →
  `HandleSettingsClicked`로 통합. `SettingsUIController`를 Registry/씬에서 조회해 오버레이로 push.

### 5) 카메라 POV(거리) 설정 저장/적용
- 신규 서비스: `Assets/Modules/MultiplayerInfrastructure/Scripts/Performance/CameraDistancePreferenceService.cs`
  - `PlayerPrefs`에 3인칭 카메라 거리를 저장/로드(`TexturePerformanceService`와 동일 패턴).
  - `RegistryType.Service`에 등록. `MainCameraController.SetThirdPersonDistance()`로 적용.
  - 저장값이 없으면 카메라 인스펙터 기본 거리를 사용.
- `CameraHolder.cs`: `MinThirdPersonDistance`/`MaxThirdPersonDistance` 프로퍼티 추가.
- `MainCameraController.cs`: 위 min/max를 노출.
- 그래픽 탭의 POV 슬라이더가 이 서비스와 연동(값 변경 시 즉시 적용 + 저장).

### 8) 라벨이 슬라이더/버튼 클릭을 가로채는 문제 수정 (후속)
- 증상: POV 슬라이더가 "텍스처 품질을 …로 변경했습니다" 상태 텍스트(및 설명 라벨)와 겹쳐서
  슬라이더 인터랙션이 되지 않음.
- 원인: `SetDocumentRootInteractable(true)`가 서브트리 전체를 `PickingMode.Position`으로 복구하면서
  텍스트 라벨(제목/설명/상태/값)까지 Position이 되어, 전체 화면 레이아웃에서 라벨이 차지하는(빈)
  영역이 하위 슬라이더/버튼의 포인터 이벤트를 흡수함.
- 수정: `SettingsUIController.NeutralizeNonInteractiveLabels()` 추가. 표시/탭전환 시 히트테스트 복구
  직후, 설정 창이 직접 생성한 비인터랙션 라벨(`settings__title/status/section-title/section-desc/
  panel-title/pov-value`)의 pickingMode를 다시 `Ignore`로 되돌림.
  - 키 설정의 키 레이블(KeyConfigEntryElement 내부 Label)은 클릭 대상이므로 대상에서 제외
    (설정 창이 직접 만든 라벨 클래스만 선별 처리).

### 7) 설정 창 클릭/드래그 미입력 수정 (후속)
- 증상: 설정 창(특히 POV 슬라이더)에서 마우스 클릭/드래그가 먹지 않음.
- 원인: `SetVisible`에서 히트테스트 복구(`SetDocumentRootInteractable(true)`)를 **탭 컨텐츠 생성 전에**
  호출했고, 탭 전환 시에는 아예 재호출하지 않아, 나중에 동적 생성된 슬라이더/타일/키 목록이
  `pickingMode` 복구 대상에서 누락됨(전체화면 오버레이 특성상 상위가 이벤트를 흡수).
- 수정:
  - `SetVisible`: display/opacity 반영 → `ShowTab`(컨텐츠 구성) → **그 다음** `SetDocumentRootInteractable`
    호출 순으로 재정렬.
  - `ShowTab`: 탭 컨텐츠 재구성 후 표시 중이면 `SetDocumentRootInteractable(true)`를 재호출하여
    새 컨텐츠까지 히트테스트를 복구.
  - `RefreshPovFromService`: 슬라이더 초기화 가드 플래그(`_povSliderInitializing`)를 `try/finally`로
    보호하여 예외 시 고착(입력 무시 지속)을 방지.

### 6) 설정 창 레이아웃/키보드 표시 수정 (후속)
- 초기 `SettingsUI.uss`에 키보드 컨테이너 크기 규칙과 `.keyboard-layout` / `.key-config-entry` 내부
  스타일이 누락되어, (a) 키보드가 0 크기로 접혀 표시되지 않고 (b) 패널 크기가 컨텐츠/탭에 따라
  가변적이며 화면을 벗어나는 문제가 있었습니다.
- 수정:
  - `.settings__panel`에 고정 크기(`width: 1000px; height: 640px`) + `max-width: 92% / max-height: 90%`
    부여 → 탭 전환/컨텐츠와 무관하게 크기 불변, 화면 이탈 방지.
  - `.settings__key-keyboard { width:100%; height:240px; }` 및 KeyConfigUI.uss의 `.keyboard-layout*` /
    `.key-config-entry*` 스타일을 SettingsUI.uss로 이식(키가 퍼센트 좌표라 컨테이너 크기 필수).
  - 중첩 flex 스크롤 정상화를 위해 헤더/탭바/푸터에 `flex-shrink:0`, 컨텐츠 영역에 `min-height:0`.

## 검증
- Play 모드에서 Esc → 설정 메뉴 표시 확인.
- 설정 창에서 키/그래픽 탭 전환, 키 리바인딩, 텍스처 품질 변경, POV 슬라이더 동작 확인.
- 키보드 시각화가 정상 표시되고, 탭 전환 시 패널 크기가 변하지 않으며 화면을 벗어나지 않는지 확인.
- POV 슬라이더 변경 후 재시작 시 값 유지(PlayerPrefs) 확인 → `CameraDistancePreferenceService`가 씬에
  배치되어 있어야 함(미배치 시 즉시 적용은 되나 저장/복원 안 됨).

## 후속 / 에디터 작업 필요 (아래 "에디터 셋업 가이드" 참조)
- `MI: SettingsUI` UIDocument 프리팹 신규 생성 및 백엔드 시스템 프리팹에 nested.
- `CameraDistancePreferenceService` 컴포넌트를 시스템 오브젝트에 배치.
- (선택) 기존 `MI_ KeyConfigUI`, `MI_ GraphicsSettingsUI` 프리팹 및 컨트롤러 스크립트 제거.
  스크립트 삭제 전 반드시 에디터에서 프리팹 nested 인스턴스를 먼저 제거해야 missing script 경고를 피할 수 있음.

---

## 에디터 셋업 가이드 (비전문 운영자용)

Unity 에디터에서 아래 순서대로 진행하세요. 스크립트/문서는 이미 생성되어 있으며, 아래는 에디터에서만 가능한
프리팹/씬 배치 작업입니다.

### A. 코드 컴파일 확인
1. Unity 에디터로 프로젝트를 엽니다(스크립트 자동 컴파일).
2. Console에 컴파일 오류가 없는지 확인합니다. 새 `.uxml`/`.uss`/`.cs`의 `.meta`는 이때 자동 생성됩니다.

### B. `MI_ SettingsUI` 프리팹 생성
1. Project 창에서 `Assets/Modules/MultiplayerInfrastructure/Prefabs/Systems/` 폴더로 이동.
2. 기존 `MI_ GraphicsSettingsUI.prefab`을 참고 삼아 새 프리팹을 만듭니다:
   - Hierarchy에서 빈 GameObject 생성 → 이름 `MI_ SettingsUI`.
   - `UIDocument` 컴포넌트 추가:
     - `Panel Settings`: 다른 MI_ UI들과 동일한 `Assets/UI Toolkit/PanelSettings.asset` 지정
       (guid `9a6e30b1a446845fc83ea6c441b81e4a`).
     - `Source Asset`: `Assets/Modules/MultiplayerInfrastructure/UIDocuments/SettingsUI.uxml` 지정.
   - `SettingsUIController` 컴포넌트 추가.
     - `_sortingOrder`는 기본값(8) 유지.
   - 이 GameObject를 `Prefabs/Systems/`에 `MI_ SettingsUI.prefab`으로 저장.

### C. 백엔드 시스템 프리팹에 nested
1. `Assets/Modules/MultiplayerInfrastructure/Prefabs/MultiplayerInfrastructureBackendSystem.prefab`을 엽니다.
2. 내부 `UI` GameObject(다른 `MI: ...` UI들이 자식으로 있는 부모) 아래에 `MI_ SettingsUI` 프리팹을
   드래그해 자식으로 추가합니다. 이름은 관례에 맞게 `MI: SettingsUI`로 변경.
3. 저장. (이렇게 하면 `SettingsUIController.Awake()`가 Registry에 자동 등록되어 Esc 메뉴에서 조회됩니다.)

### D. 카메라 거리 설정 서비스 배치
1. 같은 백엔드 시스템 프리팹 내에서 `TexturePerformanceService`가 붙어 있는 GameObject를 찾습니다
   (`MI_ TexturePerformanceService`).
2. 그 GameObject(또는 동일한 시스템 오브젝트)에 `CameraDistancePreferenceService` 컴포넌트를 추가합니다.
   - 별도 GameObject로 두어도 무방하나, 시스템 오브젝트에 두는 것을 권장.
3. 저장.

### E. 동작 확인
1. `IndevScene` 또는 `IngameScene`을 Play.
2. Esc → 설정 메뉴에 "설정" 버튼이 하나만 보이는지 확인 → 클릭 → 탭(키/그래픽) 전환 확인.
3. 그래픽 탭에서 POV 슬라이더를 움직여 카메라 거리가 바뀌는지, 재시작 후에도 유지되는지 확인.

### F. (선택) 기존 UI 제거
동작이 검증되면 아래를 제거해 중복을 정리할 수 있습니다. 반드시 이 순서를 지키세요.
1. 백엔드 시스템 프리팹에서 `MI: KeyConfigUI`, `MI: GraphicsSettingsUI` nested 인스턴스를 삭제.
2. `Prefabs/Systems/MI_ KeyConfigUI.prefab`, `MI_ GraphicsSettingsUI.prefab` 및 `.meta` 삭제.
3. 그 후 스크립트 `KeyConfigUIController.cs`, `GraphicsSettingsUIController.cs`와 대응 UXML/USS
   (`KeyConfigUI.uxml/.uss`, `GraphicsSettingsUI.uxml/.uss`) 삭제.
   (스크립트를 프리팹보다 먼저 지우면 missing script 경고가 발생하므로 순서 준수.)
