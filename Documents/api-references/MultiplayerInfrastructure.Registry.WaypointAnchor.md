# MultiplayerInfrastructure.Registry.WaypointAnchor

## 0. 문서 목적

이 문서는 `WaypointAnchor`의 현재 동작과 하이라이트 기능 추가에 따른 실행 흐름을 설명합니다. 핵심적으로 개별 웨이포인트가 어떻게 Registry에 등록되는지, `Highlight()`가 어떤 조건과 방식으로 동작하며, HUD나 시나리오 그래프에서 어떻게 호출되어야 하는지를 단계별로 정리합니다.

---

## 1. 주요 역할 요약

`WaypointAnchor`는 월드상의 특정 위치를 대표하는 컴포넌트입니다. 주요 책임은 다음과 같습니다.

1. `Registry`에 웨이포인트/상호작용 가능한 엔티티로 스스로를 등록하여 다른 시스템이 위치 정보를 조회할 수 있도록 만듭니다.
2. `identifier`를 기준으로 정렬된 정적 사전(`_anchorsByIdentifier`)을 관리하여 `TryGet` 등 정적 호출로 대상 앵커를 바로 찾을 수 있게 합니다.
3. 새로 추가된 `Highlight` 메서드로 시각적 강조를 제공하여 HUD/시나리오 노드/플레이어 입력으로부터 호출될 수 있습니다.

---

## 2. 실행 흐름

### 2.1 초기 등록

- `Awake`/`OnEnable`에서 `RegisterToRegistry()`를 호출하며 `identifier`가 비어 있으면 게임 오브젝트 이름(또는 `OnValidate`에서 설정됨)을 채웁니다.
- `Registry.Register`를 통해 `RegistryType.Waypoint`와 `RegistryType.InteractableEntity`에 각각 동일한 위치를 등록하고, `_anchorsByIdentifier`에 현재 인스턴스를 맵핑합니다.
- `OnDisable`과 `OnDestroy`에서는 해당 키를 삭제하고, `Highlight` 관련 GameObject/Material도 해제합니다.

### 2.2 하이라이트 흐름

1. `Highlight()` 호출 시 조건을 체크합니다: 비활성화 상태거나 `_highlightSprite` 미지정이면 아무 동작도 하지 않습니다.
2. `_highlightObject`가 없으면 `EnsureHighlightVisual()`이 새로운 `GameObject`(숨김 상태)를 생성하고 `SpriteRenderer`를 구성하여 Z 테스트를 `Always`로 설정, 다중 오브젝트 앞에서도 보이게 합니다.
3. 하이라이트를 활성화하고 색상/스케일을 초기화한 뒤 `PulseHighlight` 코루틴을 시작하여 `baseScale` → `pulseScale` → `baseScale`로 부드럽게 애니메이트합니다.
4. HUD의 `QuestPreviewHudUIController` Y 키 입력 또는 `ScenarioQuestWaypointHighlightNode` 실행 시 `WaypointAnchor.TryGet(identifier, out var anchor)`으로 참조한 후 `anchor.Highlight()`를 호출하면 시각적으로 강조됩니다.

### 2.3 정적 조회 구조

- `TryGet(string identifier, out WaypointAnchor anchor)`는 `_anchorsByIdentifier`를 조회하여, HUD/시나리오/디버거가 웨이포인트를 식별자로 퀵 루킹할 수 있게 합니다.
- `identifier`가 비어 있거나 등록되지 않은 경우 `false`를 반환하며, 호출자는 null 검사 또는 로그 출력을 통해 유효하지 않은 식별자를 추적할 수 있습니다.

---

## 3. 주요 메서드 설명

### `void Highlight()`
- 목적: 현재 웨이포인트에 대한 시각적 강조를 즉시 발생시킵니다.
- 동작: highlight GameObject를 생성/활성화하고 색상 및 스케일을 설정한 뒤, 펄스 코루틴을 실행합니다.
- 주의: `_highlightSprite`가 할당되어 있어야 하며 GameObject가 활성화 상태여야 합니다.

### `static bool TryGet(string identifier, out WaypointAnchor anchor)`
- 목적: 식별자로 `WaypointAnchor` 인스턴스를 회수합니다.
- 반환: 성공 시 `true`와 해당 앵커, 실패 시 `false`.
- 사용처: HUD 키 입력 처리, 시나리오 노드 실행, 테스트 코드.

### `RegisterToRegistry()`, `UnregisterFromRegistry()`
- 목적: `Registry`와 `_anchorsByIdentifier` 간의 동기화
- 특징: `identifier`가 비어 있거나 이미 등록된 경우를 방지함.

### `EnsureHighlightVisual()` / `PulseHighlight(float baseScale)`
- `EnsureHighlightVisual`: `SpriteRenderer` 기반의 하이라이트 객체를 생성하며 `CompareFunction.Always`로 다른 오브젝트에 가리지 않게 합니다.
- `PulseHighlight`: `_highlightPulseDuration`을 기준으로 스케일을 확대했다가 축소하며 `baseScale` 값을 유지합니다.

---

## 4. 의도된 사용 시나리오

1. **QuestPreviewHudUIController**
   - HUD에 표시된 트래킹 퀘스트의 `WaypointIdentifier`를 모아 `HighlightTrackedWaypoints()`를 호출.
   - `Y` 키 입력을 감지하면 `WaypointAnchor.TryGet`으로 찾아 `Highlight()`를 실행하여 플레이어가 찾고 싶은 웨이포인트를 눈에 띄게 표시합니다.

2. **ScenarioQuestWaypointHighlightNode**
   - 시나리오 그래프 노드가 실행되면 해당 노드에 지정된 `WaypointIdentifier`가 존재하는지 검사하고, 존재할 경우 `Highlight()`를 호출해 플레이어에게 웨이포인트 위치를 알립니다.

3. **디버깅/테스트**
   - 플레이 중 디버거가 필요할 때 콘솔 명령 또는 에디터 스크립트에서 `WaypointAnchor.TryGet("SomeWaypoint").Highlight()`로 즉시 시각 피드백.

---

## 5. 예시
```csharp
// HUD에서 Y키를 누르면 등록된 웨이포인트를 하이라이트
foreach (var waypointId in trackedWaypointIds)
{
    if (WaypointAnchor.TryGet(waypointId, out var anchor))
    {
        anchor.Highlight();
    }
}

// ScenarioController가 노드 실행 시 호출
if (WaypointAnchor.TryGet(node.WaypointIdentifier, out var waypoint))
{
    waypoint.Highlight();
}
else
{
    Debug.LogWarning($"Waypoint '{node.WaypointIdentifier}'를 찾을 수 없습니다.");
}
```

---

## 6. 참고: Inspector 설정 팁

- `_highlightSprite`: `WaypointAnchor`에 사용할 원형 또는 싱글톤 스프라이트를 지정하세요.
- `_highlightColor`: 강조 색상을 지정하고 알파를 조절하면 HUD 색상과 통일됩니다.
- `_highlightPulseDuration`/`_highlightPulseScale`: 너무 빠르면 눈 피로도가 증가하니 0.2~0.5s, 1.2~1.4 범위를 권장합니다.
- `_highlightSortingOrder`: HUD/시네틱 요소를 덮어썬느 기준을 잡을 때 조절하세요.

---

## 7. 주요 이슈 체크리스트

1. `_highlightSprite` 미할당 상태에서 `Highlight()`를 호출하면 아무런 효과가 없습니다.
2. 웨이포인트가 비활성화 또는 파괴 상태일 때는 `Highlight()`가 호출되어도 시각적 효과가 없음.
3. `identifier`가 중복되면 마지막 등록된 앵커가 덮어쓰므로, `identifier`를 전역적으로 유니크하게 관리해야 합니다.
4. `TryGet` 실패는 HUD와 시나리오에서 `Debug.LogWarning`으로 드러나며, 이 로그가 반복될 경우 `identifier`의 일관성을 점검해야 합니다.

---

## 8. 마무리

`WaypointAnchor`는 단순한 위치 표시를 넘어서, HUD/시나리오가 연동 가능한 Registry 기반의 `Highlight` 인터페이스를 제공합니다. 핵심은 “식별자 기반 정적 조회 → 하이라이트 생성 → 펄스 애니메이션”이며, UI·시나리오·디버깅 전 영역에서 재사용 가능하도록 설계되어 있습니다.
