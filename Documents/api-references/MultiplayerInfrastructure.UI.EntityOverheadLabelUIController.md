# API 레퍼런스: `MultiplayerInfrastructure.UI.EntityOverheadLabelUIController`

> **네임스페이스:** `MultiplayerInfrastructure.UI`
>
> **파일 위치:**
> - `Assets/Modules/MultiplayerInfrastructure/Scripts/UI/Controllers/EntityOverheadLabelUIController.cs`
> - `Assets/Modules/MultiplayerInfrastructure/Scripts/UI/VisualElements/EntityOverheadLabelElement.cs`

---

## 0. 개요

임의의 월드 엔티티(Transform) 위에 "색상 사각형(swatch) + 텍스트" 형태의 라벨을 스크린 스페이스(UI Toolkit)로 표시하는 범용 오버헤드 라벨 컨트롤러다.

플레이어 이름표를 머리 위에 표기하는 방식과 동일하게, 매 `LateUpdate`에서 월드 좌표를 `RuntimePanelUtils.CameraTransformWorldToPanel`로 패널 좌표로 변환해 라벨 위치를 갱신한다. 트리아지 평가 결과를 환자 위에 표기하는 것처럼 도메인 코드는 `SetLabel`/`RemoveLabel` API만 호출하면 된다.

---

## 1. 씬 배치

`EntityOverheadLabelUIController` 컴포넌트와 `UIDocument` 컴포넌트를 동일 GameObject에 배치한다. 카메라를 인스펙터에서 지정하지 않으면 `Camera.main`을 자동으로 사용한다(내부적으로 캐시해 매 프레임 탐색을 피한다).

---

## 2. `LabelContent` 구조체

```csharp
public readonly struct LabelContent
{
    public readonly Color SwatchColor;  // 색상 사각형 배경 색
    public readonly string Text;        // 표기할 텍스트
    public readonly Color TextColor;    // 텍스트 색

    public LabelContent(Color swatchColor, string text, Color textColor) { ... }
}
```

> **색상 관례:** 어두운 라벨 배경 위이므로 `TextColor`에는 `TriageLevelInfo.GetColor(level)` (swatch 색 자체)을 사용해 등급 색을 텍스트에 입힌다. `GetTextColor`(swatch 배경 위 가독성 색)와 혼동하지 않도록 주의한다.

---

## 3. 주요 API

### `SetLabel(Transform target, LabelContent content)`

대상 엔티티 위에 라벨을 설정한다. 라벨이 없으면 생성하고, 있으면 내용을 갱신한다.

```csharp
var ui = EntityOverheadLabelUIController.ActiveInstance;
ui?.SetLabel(
    transform,
    new EntityOverheadLabelUIController.LabelContent(
        TriageLevelInfo.GetColor(level),
        TriageLevelInfo.GetDisplayName(level),
        TriageLevelInfo.GetColor(level)));
```

### `SetLabel(Transform target, string channelId, int channelOrder, LabelContent content, float maxVisibleDistance = 0)`

같은 앵커에 채널별로 라벨을 여러 개 등록한다. 채널은 `channelOrder` 오름차순으로 앵커에서 위로 26px 간격으로 쌓인다(예: NPC 이름표 `npc-name`=0, 퀘스트 마크 `quest:...`=100 이상).

`maxVisibleDistance`는 카메라와 그 거리보다 멀어지면 해당 채널을 숨긴다. `0` 이하이면 거리 제한 없이 항상 표시한다. 한계 직전 `_distanceFadeBand` 구간에서는 선형으로 흐려지고, 거리로 숨겨진 채널은 스택에서 자리를 차지하지 않으므로 위 채널이 빈 칸 위에 뜨지 않는다.

```csharp
ui?.SetLabel(
    anchor,
    "npc-name",
    0,
    new EntityOverheadLabelUIController.LabelContent(displayName, Color.white),
    maxVisibleDistance: 12f);
```

### `RemoveLabel(Transform target)`

대상 엔티티의 라벨을 제거한다.

### `ActiveInstance`

씬에 배치된 단일 인스턴스의 정적 참조. `Awake`에서 등록되고 `OnDestroy`에서 해제된다.

---

## 4. 인스펙터 필드

| 필드 | 기본값 | 설명 |
|------|--------|------|
| `_sortingOrder` | `2.2` | UIDocument 정렬 순서 (`DefaultsUIDocument.EntityOverheadLabelSortOrder`) |
| `_worldHeightOffset` | `0.4` | 앵커 Transform으로부터 위로 띄울 추가 높이(월드 단위) |
| `_maxChannelsPerAnchor` | `4` | 한 앵커에 동시에 표시할 채널 수 상한 |
| `_distanceFadeBand` | `2` | 표시 한계 거리 직전에 라벨을 흐리게 만들 구간 길이(월드 단위). `0`이면 즉시 사라짐 |
| `_camera` | null | 투영에 사용할 카메라. 미지정 시 `Camera.main` 자동 캐시 |

---

## 5. `EntityOverheadLabelElement` 비주얼 엘리먼트

`EntityOverheadLabelElement`는 `VisualElement`를 상속한 내부 구성 요소로, 컨트롤러가 자동 생성한다.

- `SetContent(swatchColor, text, textColor)`: 색상 사각형과 텍스트를 갱신한다.
- `SetScreenPosition(Vector2 panelPosition)`: 패널 좌표로 라벨 위치를 갱신한다. `translate: (-50%, -100%)`로 라벨 하단 중앙이 앵커 좌표에 오도록 정렬된다.
- `pickingMode = Ignore`: 라벨은 포인터 이벤트를 수신하지 않는다.

---

## 6. 대상 파괴 처리

`LateUpdate`에서 `target == null`인 항목은 자동으로 제거된다. 엔티티 디스폰 시 `RemoveLabel`을 명시적으로 호출하는 것이 권장된다.

---

## 참조

- [req:환자 트리아지 분류 기능 요구사항](../requirements/patient/triage-classification-requirements.md)
