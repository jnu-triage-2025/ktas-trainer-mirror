# API 레퍼런스: `MultiplayerInfrastructure.Scenario.ScenarioEventIdentifierRegistry`

> **네임스페이스:** `MultiplayerInfrastructure.Scenario`  
> **파일 위치:** `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/ScenarioEventIdentifierRegistry.cs`

---

## 0. 문서 목적

`ScenarioEventIdentifierRegistry`는 시나리오 `InvokeEvent` 노드와 씬의 구체적인 로직을 연결하는 정적 레지스트리입니다.  
`TriageTrainer` 모듈에서 씬 별로 필요한 이벤트 핸들러를 등록하고 해제합니다.

---

## 1. 델리게이트

```csharp
public delegate IEnumerator ScenarioEventHandler();
```

핸들러는 `IEnumerator`를 반환합니다:

- `IEnumerator`를 반환하면 → ScenarioController가 코루틴으로 실행하고, **완료까지 대기** 후 다음 노드 이동
- `null`을 반환하면 → **즉시 다음 노드로** 이동

---

## 2. 공개 메서드

### `Register`

```csharp
public static void Register(string identifier, ScenarioEventHandler handler)
```

식별자를 핸들러에 연결합니다.  
같은 식별자로 중복 등록 시 기존 핸들러를 덮어씁니다.

**예외:**
- `identifier`가 `null` 또는 공백이면 → `ArgumentException`
- `handler`가 `null`이면 → `ArgumentNullException`

---

### `Unregister`

```csharp
public static bool Unregister(string identifier)
```

등록된 핸들러를 제거합니다. 성공 여부를 반환합니다.

---

### `TryGetHandler`

```csharp
public static bool TryGetHandler(string identifier, out ScenarioEventHandler handler)
```

식별자로 핸들러를 조회합니다. `ScenarioController` 내부에서 사용합니다.

---

### `Clear`

```csharp
public static void Clear()
```

모든 등록된 핸들러를 제거합니다. 씬 언로드 시 사용할 수 있습니다.

---

## 3. 사용 예시

### 기본 패턴

```csharp
// MonoBehaviour (TriageTrainer 측 이벤트 핸들러)
public class PatientBedEvents : MonoBehaviour
{
    private void OnEnable()
    {
        ScenarioEventIdentifierRegistry.Register("move_bed_to_treatment", HandleMoveBed);
        ScenarioEventIdentifierRegistry.Register("apply_oxygen_mask", HandleApplyOxygen);
    }

    private void OnDisable()
    {
        ScenarioEventIdentifierRegistry.Unregister("move_bed_to_treatment");
        ScenarioEventIdentifierRegistry.Unregister("apply_oxygen_mask");
    }

    private IEnumerator HandleMoveBed()
    {
        yield return _bedController.MoveTo(_treatmentRoom.position);
        // 완료 후 ScenarioController가 다음 노드로 진행
    }

    private IEnumerator HandleApplyOxygen()
    {
        _patient.SetAnimation("ApplyMask");
        yield return new WaitForSeconds(2f);
        yield return null; // 명시적 완료
    }
}
```

### 즉시 완료 (코루틴 없음)

```csharp
ScenarioEventIdentifierRegistry.Register("toggle_alarm", () =>
{
    _alarmSystem.Toggle();
    return null;   // 즉시 다음 노드로
});
```

### 씬 JSON에서의 연결

```json
{
    "type": "InvokeEvent",
    "identifier": "E010",
    "eventIdentifier": "move_bed_to_treatment",
    "moveNextBehavior": "WaitUntilDone",
    "next": "E020"
}
```

| `moveNextBehavior` 값 | 동작 |
|---|---|
| `"WaitUntilDone"` | 핸들러 코루틴이 완료될 때까지 대기 |
| `"Immediate"` | 핸들러 시작 직후 바로 다음 노드 이동 |

---

## 4. 주의사항

- 핸들러는 **씬 오브젝트의 수명에 맞춰** 등록/해제해야 합니다. `OnEnable`/`OnDisable` 또는 `Awake`/`OnDestroy` 쌍을 사용하세요.
- 같은 `identifier`로 여러 MonoBehaviour가 등록하면 마지막 등록만 유효합니다.
- `Clear()`는 모든 핸들러를 일괄 제거하므로, 다른 오브젝트의 핸들러도 삭제됩니다. 씬 리셋 용도 외에는 `Unregister`를 사용하세요.

---

## 관련 문서

- [multiplayer-infrastructure-overview.md](architecture/multiplayer-infrastructure-overview.md) — 시나리오 시스템 개요
- [api-references/MultiplayerInfrastructure.Scenario.ScenarioController.md](MultiplayerInfrastructure.Scenario.ScenarioController.md) — ScenarioController API
- [scenario-graph-spec.md](../requirements/content-definitions/scenario/scenario-graph-spec.md) — 시나리오 노드 JSON 스펙
