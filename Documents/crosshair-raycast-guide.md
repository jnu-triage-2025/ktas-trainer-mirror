# Crosshair & Raycast 구현 가이드

> **등록일:** 2026-03-05  
> **관련 파일:** 
> - `Assets/Modules/MultiplayerInfrastructure/Scripts/Player/PlayerController.Raycast.cs`
> - `Assets/Modules/MultiplayerInfrastructure/Scripts/Player/PlayerController.Crosshair.cs`
> - `Assets/Modules/MultiplayerInfrastructure/Scripts/UI/VisualElements/CrosshairElement.cs`
> - `Assets/Modules/MultiplayerInfrastructure/Scripts/UI/Controllers/CrosshairUIController.cs`

---

Table of Contents
- [개요](#개요)
- [아키텍처](#아키텍처)
- [씬 배치](#씬-배치)
- [기본 사용법](#기본-사용법)
- [レイ캐스트 대상 설정](#레이캐스트-대상-설정)
- [레이캐스트 결과 활용](#레이캐스트-결과-활용)
- [크로스헤어 UI 제어](#크로스헤어-ui-제어)

---

## 개요

플레이어의 화면 중앙에 크로스헤어(점)를 표시하고, 해당 위치의 앞 방향으로 **Physics.Raycast**를 매 프레임 수행하여 대상 감지를 구현합니다.

**주요 특징:**
- 레이캐스트 로직과 UI 표시 분리
- `PlayerController`의 partial class로 관리
- Inspector에서 레이캐스트 대상 레이어 및 최대 거리 설정 가능
- 다음 구현을 위한 확장 가능한 구조

---

## 아키텍처

### 역할 분리

```
PlayerController (주관)
├── PlayerController.Raycast.cs
│   └─ 레이캐스트 수행 → RaycastHasHit, RaycastHit, RaycastHitObject 프로퍼티
│
└── PlayerController.Crosshair.cs
    └─ CrosshairUIController 연결 및 UI 관리
```

### 구성 요소

| 컴포넌트 | 역할 |
|---|---|
| **CrosshairElement** | VisualElement 서브클래스, 6×6px 흰 점 표시 |
| **CrosshairUIController** | UIDocument 관리, 크로스헤어 시각성 제어 |
| **PlayerController.Raycast.cs** | 매 프레임 레이캐스트, 결과 저장 |
| **PlayerController.Crosshair.cs** | UIController 초기화, UI 표시 관리 |

---

## 씬 배치

### 1단계: CrosshairUI GameObject 생성

1. Hierarchy에서 빈 GameObject 생성 → 이름: `CrosshairUI`
2. `UIDocument` 컴포넌트 추가
   - **Source Asset** → `Assets/Modules/MultiplayerInfrastructure/UIDocuments/CrosshairUI.uxml`
   - **Sort Order** → `1` (또는 `DefaultsUIDocument.CrosshairUISortOrder` 사용)
   - **Panel Settings** → 기존 팬널 사용 (또는 새로 할당)

3. `CrosshairUIController` 컴포넌트 추가
   - **UI Document** 필드는 자동 할당되거나 위 GameObject에서 가져옴

### 2단계: PlayerController 설정

PlayerController Inspector에서:
- **Raycast Layer Mask** → 레이캐스트가 감지할 레이어 선택 (예: "NPCs", "Interactables")
- **Raycast Max Distance** → 최대 거리 입력 (기본값: 10m)
- **_crosshairUI** (선택) → 위에서 생성한 CrosshairUI GameObject 할당
  - 미할당 시 `OnStartClient`에서 Registry 자동 탐색

---

## 기본 사용법

### 레이캐스트 결과 확인

```csharp
// PlayerController 참조 획득
var player = Registry.Registry.Get<PlayerController>(
    RegistryType.Entity, 
    Registry.Registry.TypeKey<PlayerController>()
);

// 매 프레임 업데이트되는 결과 확인
if (player.RaycastHasHit)
{
    GameObject hitObject = player.RaycastHitObject;
    RaycastHit hitInfo = player.RaycastHit;
    
    // 맞은 오브젝트의 트래그, 레이어 등 조사
    Debug.Log($"Hit: {hitObject.name}, Distance: {hitInfo.distance}");
}
```

### 예제: NPC 상호작용

```csharp
// 플레이어의 레이캐스트 결과를 활용한 NPC 감지
if (player.RaycastHasHit)
{
    var npc = player.RaycastHitObject.GetComponent<NPC>();
    if (npc != null)
    {
        // NPC에 대한 처리
        npc.ShowInteractionPrompt();
    }
}
```

---

## 레이캐스트 대상 설정

### 레이어 마스크 설정 (권장)

1. **Project Settings → Tags and Layers**에서 레이어 정의
   - 예: "NPC", "Interactable", "Ragdoll" 등

2. **PlayerController Inspector**에서:
   - **Raycast Layer Mask** → 감지할 레이어들 체크
   
   ```
   ☑ Default
   ☐ TransparentFX
   ☐ Ignore Raycast
   ☑ NPC          ← 감지 대상
   ☑ Interactable ← 감지 대상
   ☐ Water
   ...
   ```

3. **모든 히트 가능한 요소 할당**
   - NPC의 GameObject/Collider를 "NPC" 레이어에 배치
   - Interactable 오브젝트를 "Interactable" 레이어에 배치

### 레이어 마스크 실시간 변경 (코드)

```csharp
// "NPC" 레이어만 감지
player._raycastLayerMask = LayerMask.GetMask("NPC");

// 다중 레이어
player._raycastLayerMask = LayerMask.GetMask("NPC", "Interactable", "Patient");

// 특정 레이어 제외
player._raycastLayerMask = ~LayerMask.GetMask("Ignore Raycast");
```

---

## 레이캐스트 결과 활용

### 프로퍼티 설명

| 프로퍼티 | 타입 | 설명 |
|---|---|---|
| `RaycastHasHit` | `bool` | 이번 프레임 레이캐스트 성공 여부 |
| `RaycastHit` | `RaycastHit` | Unity RaycastHit (distance, point, normal 등) |
| `RaycastHitObject` | `GameObject` | 맞은 오브젝트 (HasHit=false 시 null) |

### RaycastHit 활용 예제

```csharp
if (player.RaycastHasHit)
{
    RaycastHit hit = player.RaycastHit;
    
    // 충돌 지점 월드 좌표
    Vector3 hitPoint = hit.point;
    
    // 표면의 법선 벡터
    Vector3 hitNormal = hit.normal;
    
    // 카메라에서 충돌점까지의 거리
    float distance = hit.distance;
    
    // 충돌한 Collider
    Collider collider = hit.collider;
    
    // 변환 행렬
    Transform hitTransform = hit.transform;
}
```

---

## 크로스헤어 UI 제어

### 시각성 토글

```csharp
// CrosshairUIController 참조
var crosshairUI = Registry.Registry.Get<CrosshairUIController>(
    RegistryType.UI, 
    Registry.Registry.TypeKey<CrosshairUIController>()
);

// 보이기
crosshairUI.SetCrosshairVisible(true);

// 숨기기
crosshairUI.SetCrosshairVisible(false);
```

### 스타일 커스터마이징

[CrosshairUI.uss](../../Assets/Modules/MultiplayerInfrastructure/UIDocuments/CrosshairUI.uss)를 편집:

```uss
/* 점의 크기 변경 */
.crosshair__dot {
  width: 8px;      /* 기본값: 6px */
  height: 8px;
  border-radius: 4px;
}

/* 색상 변경 */
.crosshair__dot {
  background-color: rgba(255, 100, 0, 0.90);  /* 주황색으로 변경 */
}
```

---

## 트러블슈팅

### Q: 레이캐스트가 아무것도 감지하지 못합니다

**해결:**
1. 감지 대상 오브젝트가 설정된 레이어에 있는지 확인
2. 감지 대상 오브젝트에 **Collider가 있는지** 확인 (Renderer만으로는 부족)
3. PlayerController Inspector의 **Raycast Max Distance**가 충분히 큰지 확인
4. 특정 레이어만 감지 중인지 확인: `Raycast Layer Mask` 점검

### Q: 크로스헤어가 화면에 보이지 않습니다

**해결:**
1. CrosshairUI GameObject가 활성화되어 있는지 확인
2. UIDocument의 **Panel Settings**이 올바른 Canvas에 연결되어 있는지 확인
3. **Sort Order (`CrosshairUISortOrder`)** 값이 다른 UI보다 앞인지 확인
4. CrosshairUIController의 `SetCrosshairVisible(true)` 호출 여부 확인

### Q: 크로스헤어가 움직이지 않습니다 / 레이캐스트가 업데이트되지 않습니다

**해결:**
1. PlayerController가 `IsOwner`인지 확인 (NetworkBehaviour)
2. `Update_Raycast()`가 PlayerController.cs에서 호출되고 있는지 확인
3. 카메라 참조가 올바른지 확인: `_camControl.Camera != null`

---

## 다음 구현

- 레이캐스트 결과에 따른 NPC/상호작용 오브젝트 하이라이트
- 범위 내 다중 대상 감지 (여러 결과)
- 대상별 특별된 처리 로직 (공격, 상호작용 등)
