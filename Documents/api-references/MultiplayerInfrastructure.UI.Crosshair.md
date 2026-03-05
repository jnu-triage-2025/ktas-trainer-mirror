# API 레퍼런스: Crosshair & Raycast

> **네임스페이스:**  
> - `MultiplayerInfrastructure.Player` (PlayerController.Raycast, PlayerController.Crosshair)
> - `MultiplayerInfrastructure.UI` (CrosshairElement, CrosshairUIController)
>
> **파일 위치:**  
> - `Assets/Modules/MultiplayerInfrastructure/Scripts/Player/PlayerController.Raycast.cs`
> - `Assets/Modules/MultiplayerInfrastructure/Scripts/Player/PlayerController.Crosshair.cs`
> - `Assets/Modules/MultiplayerInfrastructure/Scripts/UI/VisualElements/CrosshairElement.cs`
> - `Assets/Modules/MultiplayerInfrastructure/Scripts/UI/Controllers/CrosshairUIController.cs`

---

Table of Contents
- [PlayerController.Raycast](#playercontrollerraycast)
- [PlayerController.Crosshair](#playercontrollercrosshair)
- [CrosshairElement](#crosshairelement)
- [CrosshairUIController](#crosshairuicontroller)

---

## PlayerController.Raycast

### 네임스페이스
```csharp
namespace MultiplayerInfrastructure.Player
```

### 상속
partial class (PlayerController의 확장)

### 개요

PlayerController의 레이캐스트 처리 부분입니다. 매 프레임 카메라 뷰포트 중앙에서 지정된 레이어 마스크를 향해 Physics.Raycast를 수행합니다.

---

### 필드 (Serialized)

#### `_raycastLayerMask`
```csharp
[SerializeField] private LayerMask _raycastLayerMask = ~0;
```
- **타입:** `LayerMask`
- **기본값:** `~0` (모든 레이어)
- **설명:** 크로스헤어 중심선 레이캐스트 대상 레이어 마스크

#### `_raycastMaxDistance`
```csharp
[SerializeField] private float _raycastMaxDistance = 10f;
```
- **타입:** `float`
- **기본값:** `10f`
- **단위:** 미터 (m)
- **설명:** 크로스헤어 중심선 레이캐스트의 최대 거리

---

### 프로퍼티

#### `RaycastHasHit`
```csharp
public bool RaycastHasHit { get; private set; }
```
- **타입:** `bool`
- **설명:** 이번 프레임 크로스헤어 중심선 레이캐스트가 어떤 콜라이더에 맞았으면 `true`
- **읽기 전용**

#### `RaycastHit`
```csharp
public RaycastHit RaycastHit { get; private set; }
```
- **타입:** `RaycastHit` (Unity.Physics)
- **설명:** 크로스헤어 중심선 레이캐스트의 원시 `RaycastHit` 결과
- **유효 조건:** `RaycastHasHit == true`일 때만 유효한 데이터 포함
- **읽기 전용**
- **RaycastHit 필드:**
  - `distance` (float): 광선 원점에서 충돌점까지의 거리
  - `point` (Vector3): 충돌점의 월드 좌표
  - `normal` (Vector3): 표면의 법선 벡터
  - `collider` (Collider): 충돌한 콜라이더
  - `transform` (Transform): 충돌한 오브젝트의 변환

#### `RaycastHitObject`
```csharp
public GameObject RaycastHitObject => RaycastHasHit ? RaycastHit.collider.gameObject : null;
```
- **타입:** `GameObject`
- **설명:** 레이캐스트에 맞은 GameObject
- **반환값:** 
  - `RaycastHasHit == true` 시: 맞은 오브젝트
  - `RaycastHasHit == false` 시: `null`
- **읽기 전용**

---

### 메서드 (내부)

#### `Awake_Raycast()`
```csharp
void Awake_Raycast()
```
- **설명:** 초기화 로직 (PlayerController.Awake에서 호출)
- **호출:** PlayerController.Awake → Awake_Raycast()
- **접근:** private

#### `Update_Raycast()`
```csharp
void Update_Raycast()
```
- **설명:** 매 프레임 크로스헤어 레이캐스트 수행 (PlayerController.Update에서 호출)
- **호출:** PlayerController.Update → Update_Raycast()
- **조건:** `IsOwner == true`일 때만 실행
- **접근:** private

#### `PerformCrosshairRaycast()`
```csharp
private void PerformCrosshairRaycast()
```
- **설명:** 카메라 뷰포트 중앙(0.5, 0.5)에서 `_raycastLayerMask` 레이어를 향해 레이캐스트를 수행하고, 결과를 `RaycastHasHit` / `RaycastHit`에 저장합니다.
- **구현:**
  1. `IsOwner` 및 `_camControl` 확인
  2. 뷰포트 중앙에서 레이 생성: `camera.ViewportPointToRay(0.5, 0.5)`
  3. Physics.Raycast 수행
  4. 결과 저장
- **접근:** private

---

### 사용 예제

```csharp
// PlayerController 참조 획득
var player = GetComponent<PlayerController>();

// Update에서 결과 체크 (매 프레임 자동 업데이트됨)
if (player.RaycastHasHit)
{
    GameObject hitObject = player.RaycastHitObject;
    float distance = player.RaycastHit.distance;
    Vector3 hitPoint = player.RaycastHit.point;
    
    Debug.Log($"Hit {hitObject.name} at distance {distance}");
}
```

---

## PlayerController.Crosshair

### 네임스페이스
```csharp
namespace MultiplayerInfrastructure.Player
```

### 상속
partial class (PlayerController의 확장)

### 개요

PlayerController의 크로스헤어 UI 처리 부분입니다. CrosshairUIController를 찾아 등록하고, 필요에 따라 크로스헤어 UI의 시각성을 제어합니다.

---

### 필드 (Serialized)

#### `_crosshairUI`
```csharp
[SerializeField] private CrosshairUIController _crosshairUI;
```
- **타입:** `CrosshairUIController`
- **설명:** 크로스헤어 UI 컨트롤러 참조 (선택사항, 미할당 시 Registry 자동 탐색)
- **초기화 시점:** `OnStartClient_Crosshair()`

---

### 메서드

#### `OnStartClient_Crosshair()`
```csharp
void OnStartClient_Crosshair()
```
- **설명:** 크로스헤어 UI 초기화 (PlayerController.OnStartClient에서 호출)
- **호출:** PlayerController.OnStartClient → OnStartClient_Crosshair()
- **조건:** `IsOwner == true`일 때만 실행
- **동작:**
  1. `_crosshairUI`가 미할당되었으면 Registry에서 탐색
  2. 찾지 못하면 경고 로그 출력
  3. CrosshairUIController의 `SetCrosshairVisible(true)` 호출
- **접근:** private

---

### 사용 예제

```csharp
// Inspector에서 CrosshairUI GameObject 할당, 또는
// 자동으로 Registry를 통해 탐색됨

// CrosshairUI 숨기기 (외부에서 필요시)
var crosshairUI = Registry.Registry.Get<CrosshairUIController>(
    RegistryType.UI, 
    Registry.Registry.TypeKey<CrosshairUIController>()
);
crosshairUI?.SetCrosshairVisible(false);
```

---

## CrosshairElement

### 네임스페이스
```csharp
namespace MultiplayerInfrastructure.UI
```

### 상속
`UnityEngine.UIElements.VisualElement`

### 특성
```csharp
[UxmlElement]
public partial class CrosshairElement : VisualElement
```

### 개요

화면 중앙에 표시되는 크로스헤어 VisualElement입니다. 단순 점(dot) 형태로 6×6px 흰 원을 렌더링합니다.

---

### 필드 (내부)

#### `_dot`
```csharp
private readonly VisualElement _dot;
```
- **타입:** `VisualElement`
- **설명:** 6×6px 원을 표시하는 자식 요소
- **클래스:** `crosshair__dot`

---

### 생성자

#### `CrosshairElement()`
```csharp
public CrosshairElement()
```
- **설명:** 크로스헤어 요소를 초기화합니다.
- **동작:**
  1. `pickingMode = PickingMode.Ignore` (마우스 상호작용 무시)
  2. 클래스 리스트에 `"crosshair"` 추가
  3. 자식 VisualElement `_dot` 생성
  4. `_dot`에 `"crosshair__dot"` 클래스 추가

---

### 스타일 클래스

#### `.crosshair`
```uss
.crosshair {
  position: absolute;
  width: 0;
  height: 0;
  align-items: center;
  justify-content: center;
}
```
- **설명:** 0×0 크기로 중앙 기준점 역할

#### `.crosshair__dot`
```uss
.crosshair__dot {
  width: 6px;
  height: 6px;
  border-radius: 3px;
  background-color: rgba(255, 255, 255, 0.90);
  border-left-width: 1px;
  border-right-width: 1px;
  border-top-width: 1px;
  border-bottom-width: 1px;
  border-left-color: rgba(0, 0, 0, 0.55);
  border-right-color: rgba(0, 0, 0, 0.55);
  border-top-color: rgba(0, 0, 0, 0.55);
  border-bottom-color: rgba(0, 0, 0, 0.55);
  translate: -3px -3px;
}
```
- **설명:** 6×6px 흰 원, 검은색 테두리, 중앙 정렬용 translate

---

## CrosshairUIController

### 네임스페이스
```csharp
namespace MultiplayerInfrastructure.UI
```

### 상속
`MultiplayerInfrastructure.UI.UIControllerABC`

### 특성
```csharp
[RequireComponent(typeof(UIDocument))]
public class CrosshairUIController : UIControllerABC
```

### 개요

크로스헤어 UI를 표시하고 관리합니다. UIDocument를 통해 CrosshairElement를 제어하며, Registry에 자동으로 등록됩니다.

---

### 필드 (Serialized)

#### `_uiDocument`
```csharp
[SerializeField] private UIDocument _uiDocument;
```
- **타입:** `UIDocument`
- **설명:** 크로스헤어 UXML을 로드한 UIDocument (자동 할당)

---

### 프로퍼티

없음 (UI 제어는 메서드를 통해 수행)

---

### 메서드

#### `Awake()` (오버라이드)
```csharp
protected override void Awake()
```
- **설명:** UIControllerABC의 Awake를 호출한 후 크로스헤어 UI 초기화
- **호출:** Unity 라이프사이클 (자동)
- **동작:**
  1. `base.Awake()` (Registry 등록)
  2. `SetupCrosshairUI()` 호출

#### `SetCrosshairVisible(bool visible)`
```csharp
public void SetCrosshairVisible(bool visible)
```
- **매개변수:**
  - `visible` (bool): `true` 시 표시, `false` 시 숨김
- **설명:** 크로스헤어 UI의 시각성을 제어합니다.
- **구현:** `_crosshairElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;`
- **사용 예:**
  ```csharp
  crosshairUI.SetCrosshairVisible(false); // 숨기기
  crosshairUI.SetCrosshairVisible(true);  // 보이기
  ```

#### `SetupCrosshairUI()` (내부)
```csharp
private void SetupCrosshairUI()
```
- **설명:** UIDocument에서 크로스헤어 요소를 찾아 `_crosshairElement`에 할당
- **호출:** Awake에서 자동 호출
- **동작:**
  1. `_uiDocument` null 체크
  2. `root.Q<CrosshairElement>("crosshair-root")` 쿼리
  3. 찾지 못하면 경고 로그 출력
- **접근:** private

---

### Registry 등록

UIControllerABC를 상속받으므로, Awake 시 자동으로 Registry에 등록됩니다.

```csharp
// 다른 시스템에서 접근
var crosshairUI = Registry.Registry.Get<CrosshairUIController>(
    RegistryType.UI,
    Registry.Registry.TypeKey<CrosshairUIController>()
);
```

---

### 사용 예제

```csharp
// 시작 시 내가 접근하는 방법
var crosshairUI = GetComponent<CrosshairUIController>();
crosshairUI.SetCrosshairVisible(true);

// 다른 곳에서 접근하는 방법
var crosshairUI = Registry.Registry.Get<CrosshairUIController>(
    RegistryType.UI,
    Registry.Registry.TypeKey<CrosshairUIController>()
);

if (crosshairUI != null)
{
    // 특정 상황에서 크로스헤어 숨기기 (예: 아이템 사용 중)
    crosshairUI.SetCrosshairVisible(false);
}
```

---

### UXML 정의

CrosshairUI.uxml:
```xml
<?xml version="1.0" encoding="utf-8"?>
<ui:UXML
  xmlns:ui="UnityEngine.UIElements"
  xmlns:mi="MultiplayerInfrastructure.UI"
  editor-extension-mode="False"
>
  <ui:Style src="CrosshairUI.uss" />
  <ui:VisualElement class="crosshair-container" picking-mode="Ignore">
    <mi:CrosshairElement name="crosshair-root" />
  </ui:VisualElement>
</ui:UXML>
```

---

## 라이프사이클 순서

```
Scene Start
  ↓
MainCameraController.Awake ()
  ↓ (Player 스폰 후, OnStartClient)
PlayerController.Awake()
  → Awake_Raycast()
  
PlayerController.OnStartClient()
  → OnStartClient_Crosshair()
    → CrosshairUIController.SetCrosshairVisible(true)

매 프레임:
  PlayerController.Update()
    → Update_Raycast()
      → PerformCrosshairRaycast()
        → RaycastHasHit, RaycastHit 업데이트
```

---

## 트러블슈팅 체크리스트

| 증상 | 점검 사항 |
|---|---|
| 레이캐스트가 감지하지 못함 | 1. 대상이 설정 레이어에 있는가큐<br>2. 대상에 Collider가 있는가<br>3. `Raycast Max Distance` < 실제 거리는 아닌가 |
| 크로스헤어가 보이지 않음 | 1. CrosshairUI GameObject가 활성화되어 있는가<br>2. UIDocument.Panel Settings가 올바른가<br>3. Sort Order가 충분히 앞인가 |
| 업데이트되지 않음 | 1. PlayerController가 IsOwner인가<br>2. Update_Raycast()가 호출되는가<br>3. 카메라 참조가 valid한가 |
