# 오버레이 UIDocument 작성 가이드 (표시/입력 가로채기 방지)

이 문서는 `MultiplayerInfrastructure`의 여러 UIDocument 기반 UI가 **하나의 PanelSettings를 공유**하는 구조에서
반복적으로 발생하는 두 부류의 버그를 방지하기 위한 필수 규약을 정리합니다.

- **문제 A**: 열려야 할 UI(예: 인벤토리)가 표시되지 않는다.
- **문제 B**: 닫혀 있는 상위 오버레이(예: 그래픽/키설정 UI)가 아래 UI의 클릭을 가로채서, 하위 UI(인벤토리 슬롯 등)가 무반응이다.

두 문제 모두 근본 원인이 같습니다: **UIDocument의 `rootVisualElement` 라이프사이클/히트테스트 이해 부족**.

---

## 0. 반드시 먼저 알아야 하는 3가지 사실

1. **모든 시스템 UI는 하나의 PanelSettings(`Assets/UI Toolkit/PanelSettings.asset`)를 공유한다.**
   따라서 모든 UIDocument는 **하나의 UI Toolkit 패널로 합성**되고, `sortingOrder`가 z-order와
   포인터 히트테스트 우선순위를 함께 결정합니다. (sortingOrder 상수는 `DefaultsUIDocument.cs` 참조)

2. **UIDocument는 자체 라이프사이클에서 `rootVisualElement.style.display`를 강제로 `Flex`로 되돌린다.**
   즉 `rootVisualElement.style.display = None`으로 숨겨도, UIDocument가 이후에 다시 `Flex`로 만들 수 있습니다.
   → **`display`만으로는 히트테스트를 신뢰성 있게 막을 수 없습니다.**

3. **UIDocument는 `rootVisualElement`를 (재)생성한다.**
   특히 이 UI가 **네트워크 스폰 프리팹의 자식**이거나 비활성→활성 전환을 겪으면, `OnEnable`에서
   rootVisualElement가 새로 만들어질 수 있습니다. `Awake`에서 캐시한 요소가 실제 렌더 트리와
   **다른 인스턴스(detached)** 가 되어, 초기 숨김/바인딩이 화면에 반영되지 않습니다.

---

## 1. 문제 A: UI가 표시되지 않음 (detached rootVisualElement)

### 증상
- 입력→토글→`SetVisible(true)`까지 로직은 모두 정상 실행되고 `IsVisible`도 true인데 화면에 안 보인다.
- UI Debugger로 보면 대상 요소가 `panel == null`이거나 `worldBound`가 `NaN`이다.

### 원인
`Awake`에서 `_document.rootVisualElement`를 한 번만 조회해 뷰/자식 요소를 캐시했는데,
이후 UIDocument가 rootVisualElement를 재생성하면서 캐시가 **패널에 붙지 않은 죽은 트리**를 가리킴.

### 규약 (해야 할 것)
- **뷰/자식 요소 바인딩을 `Awake` 한 번으로 끝내지 말 것.**
- `OnEnable`에서 현재 `rootVisualElement` 기준으로 (재)바인딩하고,
  실제 사용 시점(예: `OnOverlayPushed`)에 `element.panel == null`이면 다시 바인딩할 것.

참고 구현: `InventoryUIController.BindViewToCurrentDocumentRoot()` +
`OnOverlayPushed()`의 `if (_view == null || _view.panel == null) BindViewToCurrentDocumentRoot();`

---

## 2. 문제 B: 닫힌 오버레이가 하위 UI 클릭을 가로챔

### 증상
- 하위 UI(인벤토리 슬롯 등) 클릭이 무반응.
- 하위 UI의 `sortingOrder`를 가로채는 오버레이보다 **높게** 올리면(예: 8 → 8.1) 갑자기 동작한다.

### 원인
- 닫힌 오버레이가 내부 컨텐츠 요소(`_root`, backdrop 등)만 `display:None`으로 숨기고,
  **UIDocument의 `rootVisualElement`(전체 화면을 덮음)** 또는 **컨텐츠 root의 `pickingMode`** 를
  방치함.
- `display:None`은 위 0-2번 이유로 UIDocument가 되돌리며, `Awake` 캐시가 detached면(0-3) 컨텐츠 root
  숨김이 실제 렌더 트리에 적용되지 않아 `pickingMode=Position`인 전체화면 요소가 남는다.
- 그 결과 sortingOrder가 높은 닫힌 오버레이가 아래 UI의 포인터 이벤트를 흡수한다.

### 규약 (해야 할 것)
- 오버레이 표시/숨김은 **컨텐츠 요소의 `display`만 토글하지 말고,
  반드시 `UIControllerABC.SetDocumentRootInteractable(document, visible)`를 함께 호출**할 것.
  - 이 헬퍼는 `display`가 아니라 **`rootVisualElement` 서브트리 전체의 `pickingMode`** 를 토글합니다.
    (`pickingMode`는 UIDocument가 되돌리지 않으므로 신뢰 가능)
    - 숨김: 서브트리 전체 `PickingMode.Ignore` → 클릭을 흡수하지 않음
    - 표시: 서브트리 전체 `PickingMode.Position` → 내부 버튼/슬롯 인터랙션 활성화
- **초기 숨김 타이밍 보호**: `Awake`의 `SetVisible(false)` 시점에 rootVisualElement가 아직 없거나
  이후 재생성될 수 있으므로, `OnEnable`에서 다음을 호출해 rootVisualElement 준비 후 중립화할 것.
  ```csharp
  private void OnEnable()
  {
    if (!_isVisible) // 또는 !IsOpen
      StartCoroutine(NeutralizeDocumentRootWhenReady(_document));
  }
  ```

참고 구현: `UIControllerABC.SetDocumentRootInteractable`,
`UIControllerABC.NeutralizeDocumentRootWhenReady`, 그리고 이를 사용하는
`GraphicsSettingsUIController` / `KeyConfigUIController` / `GameEscapeMenuUIController` /
`ProblemSheetUIController`.

---

## 3. 신규 오버레이 UIDocument 작성 체크리스트

새 오버레이 컨트롤러(`UIControllerABC` + `IUIOverlay`)를 만들 때 아래를 반드시 확인:

- [ ] `DefaultsUIDocument`에 sortingOrder 상수를 추가하고 `_document.sortingOrder`에 대입했는가.
- [ ] 표시/숨김 메서드가 컨텐츠 `display` 토글에 더해 `SetDocumentRootInteractable(_document, visible)`를 호출하는가.
- [ ] `OnEnable`에서 `StartCoroutine(NeutralizeDocumentRootWhenReady(_document))`로 초기 중립화를 보장하는가.
- [ ] 캐시한 뷰/요소를 실제 사용 직전에 `panel == null` 검사로 재바인딩하는가(네트워크 프리팹 자식일 경우 특히 중요).
- [ ] UXML 최상위 요소에 `style="visibility: hidden;"` 같은 인라인 스타일을 하드코딩하지 않았는가.
      (인라인 스타일은 코드의 표시 제어와 충돌한다. 초기 숨김은 코드에서 일원화할 것.)

---

## 4. 디버깅 레시피 (같은 증상이 재발하면)

1. **표시 안 됨(문제 A) 판별**: 대상 요소의 `panel`, `worldBound`, `resolvedStyle.visibility`를 로그.
   `panel == null` 또는 `worldBound.width == NaN` → detached. 1번 규약 적용.
2. **클릭 가로채기(문제 B) 판별**: UI가 열린 상태에서 슬롯/버튼 좌표에 대해
   `element.panel.Pick(worldCoord)`를 호출하고, 반환된 요소의 **조상 체인(name/class/pickingMode/display)** 을
   root까지 로그. 히트된 요소가 대상이 아닌 다른 UIDocument 소속이면 그 오버레이가 범인.
   → 해당 오버레이에 2번 규약(pickingMode 중립화 + OnEnable 코루틴) 적용.
3. 모든 UIDocument 상태를 한 번에 보려면 `FindObjectsByType<UIDocument>`로 각 rootVisualElement의
   `sortingOrder / pickingMode / style.display / worldBound`를 덤프.

> 위 진단 로그는 임시로만 사용하고, 원인 확정 후 제거한다.

---

## 5. 관련 파일

- `Assets/Modules/MultiplayerInfrastructure/Scripts/UI/Controllers/UIControllerABC.cs`
  (`SetDocumentRootInteractable`, `NeutralizeDocumentRootWhenReady`)
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Definitions/DefaultsUIDocument.cs` (sortingOrder 상수)
- `Assets/Modules/MultiplayerInfrastructure/Scripts/UI/UIOverlayStack.cs` (오버레이 스택 계약)
- 참고 컨트롤러: `InventoryUIController`, `GraphicsSettingsUIController`, `KeyConfigUIController`,
  `GameEscapeMenuUIController`, `ProblemSheetUIController`
