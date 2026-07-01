# 인벤토리 UI 표시/상호작용 버그 수정 + 오버레이 클릭 가로채기 방지 + 아이템 툴팁 (2026-07-01)

## 변경 목적
- E 키로 인벤토리 UI가 열려도 화면에 표시되지 않던 문제를 해결합니다.
- 인벤토리가 표시된 뒤에도 슬롯 클릭이 전혀 먹지 않던(상위 오버레이가 클릭을 가로채던) 문제를 해결합니다.
- 동일한 원인을 가진 다른 오버레이 UI들도 일괄 보정합니다.
- 인벤토리 아이템에 마우스 hover 시 툴팁을 표시하는 기능을 추가합니다.

## 배경 / 근본 원인
모든 시스템 UI(`MI: ...`)는 **하나의 PanelSettings(`Assets/UI Toolkit/PanelSettings.asset`)를 공유**하며,
하나의 UI Toolkit 패널로 합성됩니다. 이 구조에서 두 부류의 버그가 있었습니다.

1. **표시 안 됨 (detached rootVisualElement)**
   - `InventoryUIController`가 `Awake`에서 `rootVisualElement`를 한 번만 조회해 뷰를 캐시.
   - 이 UI는 NetworkBehaviour 프리팹의 자식이라, 스폰/재활성 과정에서 UIDocument가 rootVisualElement를
     재생성 → 캐시된 뷰가 패널에 붙지 않은 죽은 트리(`panel == null`, `worldBound = NaN`)를 가리킴.
   - 그래서 `SetVisible(true)`가 화면에 반영되지 않음.

2. **클릭 가로채기**
   - 닫힌 상위 오버레이(sortingOrder 8인 KeyConfig 등)가 내부 컨텐츠만 `display:None`으로 숨기고
     `rootVisualElement`/컨텐츠 root의 `pickingMode`를 방치.
   - **UIDocument가 `rootVisualElement.style.display`를 강제로 `Flex`로 되돌리는 특성** + 위 detached 문제로,
     `pickingMode=Position`인 전체화면 요소가 남아 인벤토리(sortingOrder 6) 슬롯 클릭을 흡수.
   - 진단(`panel.Pick`)으로 슬롯 좌표에서 `key-config__panel` / `key-config-root`가 히트됨을 확인.
   - sortingOrder를 8.1로 올리면 동작하던 현상과 일치.

## 핵심 변경 사항

### 1) 인벤토리 표시 문제 수정
- 변경 파일: `Assets/Modules/MultiplayerInfrastructure/Scripts/UI/Controllers/InventoryUIController.cs`
- 뷰 바인딩을 `Awake` 1회 → `OnEnable`마다 현재 `rootVisualElement` 기준으로 (재)바인딩(`BindViewToCurrentDocumentRoot`).
- `OnOverlayPushed`에서 `_view == null || _view.panel == null`이면 재바인딩 후 표시(안전망).
- 뷰 교체 시 이벤트 구독 누수 방지(`DetachViewEvents`).

### 2) 오버레이 클릭 가로채기 방지 (공통 헬퍼)
- 변경 파일: `Assets/Modules/MultiplayerInfrastructure/Scripts/UI/Controllers/UIControllerABC.cs`
- `SetDocumentRootInteractable(UIDocument, bool visible)` 추가:
  - `display`가 아니라 **`rootVisualElement` 서브트리 전체의 `pickingMode`** 를 토글.
    (`pickingMode`는 UIDocument가 되돌리지 않아 신뢰 가능)
  - 숨김 → `PickingMode.Ignore`, 표시 → `PickingMode.Position`.
- `NeutralizeDocumentRootWhenReady(UIDocument)` 코루틴 추가:
  - rootVisualElement가 준비될 때까지 대기 후 숨김 중립화. `Awake` 조기 실행/재생성 타이밍 문제 대응.

### 3) 동일 결함 오버레이 일괄 적용
- 변경 파일:
  - `GraphicsSettingsUIController.cs`
  - `KeyConfigUIController.cs`
  - `GameEscapeMenuUIController.cs`
  - `ProblemSheetUIController.cs`
- 표시/숨김 경로에서 `SetDocumentRootInteractable(_document, visible)` 호출(컨텐츠 `_root == null` 가드보다 앞).
- 각 컨트롤러 `OnEnable`에서 `StartCoroutine(NeutralizeDocumentRootWhenReady(...))`로 초기 중립화 보장.

### 4) 인벤토리 아이템 hover 툴팁 추가
- 변경 파일:
  - `Assets/Modules/MultiplayerInfrastructure/Scripts/UI/VisualElements/InventoryUIViewElement.cs`
  - `Assets/Modules/MultiplayerInfrastructure/UIDocuments/InventoryUI.uss`
- 슬롯에 `PointerEnter`/`PointerLeave` 콜백 등록, held-item-ghost와 동일한 마우스 추적 방식으로 툴팁 표시.
- 툴팁 내용: 이름(`CurrentDisplayName`/`CurrentIdentifier`), 타입(`GetType().Name`), 설명(`CurrentDescription`),
  상세(`CurrentDetailComment`), 스택(`CurrentStackCount / CurrentMaxStackCount`, stackable일 때). 이름 색은 `CurrentColor`.
  (아이템에 희소도/카테고리 필드가 없어 색상으로 구분)
- 아이템을 들고 있을 때/인벤토리 닫힘 시 툴팁 숨김, 인벤토리 루트 경계에서 위치 보정.
- USS는 BEM 컨벤션(`.item-tooltip`, `.item-tooltip__name` 등).

## 재발 방지 문서
- 신규 가이드: `Documents/working-guide/features/ui/overlay-uidocument-picking-guide.md`
  - 공유 PanelSettings 구조, `rootVisualElement` display 되돌림/재생성 특성, 필수 규약, 신규 오버레이 체크리스트,
    디버깅 레시피(`panel.Pick` 조상 체인 덤프) 정리.

## 검증
- Play 모드에서 E로 인벤토리 표시 확인.
- 인벤토리 슬롯 클릭/드래그 정상 동작(상위 오버레이가 클릭을 가로채지 않음).
- 그래픽 설정/ESC 메뉴/키 설정 등 오버레이는 여전히 정상적으로 열리고 내부 버튼이 눌림.
- 아이템 hover 시 툴팁 표시 확인.

## 주의 / 후속
- `SetDocumentRootInteractable`는 표시 시 서브트리 전체를 `Position`으로 복구한다. 라벨/아이콘 등
  의도적으로 `Ignore`였던 비인터랙션 요소도 `Position`이 되지만 클릭 동작에는 영향이 없다.
  특정 요소를 반드시 비인터랙션으로 유지해야 하면 표시 이후 개별 재설정이 필요하다.
- `NeutralizeDocumentRootWhenReady`는 최대 10프레임 대기 후 1회 중립화한다. UIDocument가 그보다 늦게
  rootVisualElement를 재생성하는 사례가 발견되면 재적용 트리거를 보강해야 한다.
