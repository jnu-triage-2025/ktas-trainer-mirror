# API 레퍼런스: `MultiplayerInfrastructure.Entity.IScenarioEntityStateEventSource`

> **네임스페이스:** `MultiplayerInfrastructure.Entity`
>
> **파일 위치:** `Assets/Modules/MultiplayerInfrastructure/Scripts/Entity/IScenarioEntityStateEventSource.cs`

---

## 0. 개요

엔티티의 상태(state) 변경을 **명명된 이벤트**로 노출하여, 시나리오 그래프가 그 이벤트를 시나리오 신호로 변환할 수 있게 하는 범용 인터페이스다.

시나리오의 `EntityStateSignalBinding` 노드가 식별자로 엔티티를 레지스트리에서 찾은 뒤, 그 GameObject 에서 이 인터페이스를 찾아 (eventName, key) 조합에 대한 리스너를 등록/해제한다. 엔티티가 해당 이벤트를 발생시키면 등록된 콜백이 호출되고, 노드(정확히는 `ScenarioEntityStateSignalBindings`)가 콜백에서 `ScenarioInteractionSignals.Raise` 를 수행한다.

`IScenarioEntityInitTarget` / `IScenarioTriageAssessTarget` 과 동일한 설계 원칙을 따른다: 이 인터페이스 자체는 어떤 도메인(환자/처치 등)에도 의존하지 않으며, 어떤 이벤트가 있고 key 가 무엇을 의미하는지는 구현체가 정의한다.

---

## 1. 인터페이스 정의

```csharp
public interface IScenarioEntityStateEventSource
{
    IReadOnlyList<string> GetStateEventNames();
    bool RegisterStateEventListener(string eventName, string key, Action<string> onFired);
    void UnregisterStateEventListeners(string eventName);
}
```

### `GetStateEventNames()`

이 엔티티가 지원하는 상태 이벤트 이름 목록을 반환한다(검토/검증/문서화용).

### `RegisterStateEventListener(string eventName, string key, Action<string> onFired)`

명명된 상태 이벤트에 리스너를 등록한다.

- `eventName`: 이벤트 이름(구현체 정의; `GetStateEventNames()` 중 하나).
- `key`: 이벤트 세부 대상 필터(구현체 해석). `null`/빈 문자열이면 해당 이벤트의 모든 발생에 매칭된다. 콜백에는 실제 발생 key 가 전달된다.
- `onFired`: 이벤트 발생 시 호출되는 콜백. 인자는 실제 발생 key(없으면 `null`).
- 반환: 이벤트 이름을 인식하여 등록했으면 `true`.

### `UnregisterStateEventListeners(string eventName)`

등록된 리스너를 해제한다. `eventName` 이 비어 있으면 이 엔티티의 모든 리스너를 해제한다.

> 소스는 **이벤트 이름 단위 해제**만 제공한다. 특정 바인딩만 제거해야 하는 경우 `ScenarioEntityStateSignalBindings` 가 해당 (소스, 이벤트) 리스너를 모두 지운 뒤 남은 바인딩을 재등록한다.

---

## 2. 구현체

| 클래스 | 위치 | 노출 이벤트 |
|--------|------|------|
| `PatientController` | `Assets/Modules/TriageTrainer/Scripts/Patient/PatientController.cs` (부분 구현: `PatientController.StateEvents.cs`) | `TreatmentApplied`, `TreatmentRemoved`, `VitalChanged`, `TriageSubmitted` |

**`PatientController` 이벤트 사양:**

| eventName | key(대상) | C# 이벤트 | 발생 지점 |
|---|---|---|---|
| `TreatmentApplied` | 처치 표시 항목명(`TreatmentDisplay`) | `OnTreatmentApplied(TreatmentDisplay)` | `SetTreatmentDisplay` (false→true 전이) |
| `TreatmentRemoved` | 처치 표시 항목명 | `OnTreatmentRemoved(TreatmentDisplay)` | `SetTreatmentDisplay` (true→false 전이) |
| `VitalChanged` | (없음) | `OnVitalChanged(PatientMedicalState)` | `NotifyMedicalStateChanged` |
| `TriageSubmitted` | 트리아지 등급명(`TriageLevel`) | `OnTriageSubmitted(TriageLevel)` | `ApplyAssessedTriage` / `ApplyAssessedTriageLocalOnly` |

이벤트는 서버(호스트) 권위 상태 적용 지점에서 발생하므로, 콜백에서의 신호 발신이 서버 권위로 전 피어에 일관되게 전파된다.

---

## 3. 호출 측

`ScenarioController.ExecuteEntityStateSignalBindingNode` (`ScenarioController.cs`) 에서 레지스트리로 대상 엔티티를 조회한 뒤 이 인터페이스를 통해 바인딩을 등록/해제한다.

```csharp
var source = descriptor.GameObject
    .GetComponentInChildren<IScenarioEntityStateEventSource>(true);
ScenarioEntityStateSignalBindings.Register(
    node.BindingIdentifier, source, node.EventName, node.EventKey,
    node.OutputSignalIdentifier, node.ConsumeOnce);
```

---

## 참조

- [api:ScenarioGraphNodes — EntityStateSignalBinding 노드](MultiplayerInfrastructure.Scenario.ScenarioGraphNodes.md)
- [api:IScenarioTriageAssessTarget](MultiplayerInfrastructure.Entity.IScenarioTriageAssessTarget.md)
