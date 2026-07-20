# API 레퍼런스: `MultiplayerInfrastructure.Entity.IScenarioIdentifiedEntity`

> **네임스페이스:** `MultiplayerInfrastructure.Entity`
>
> **파일 위치:** `Assets/Modules/MultiplayerInfrastructure/Scripts/Entity/IScenarioIdentifiedEntity.cs`

---

## 0. 개요

시나리오 식별자를 노출하는 엔티티가 구현하는 범용 인터페이스다. "진입한 엔티티가 누구인지" 를 알아야 하는 컴포넌트(예: `ScenarioTriggerZone` 의 대상별 신호)가 도메인 타입(`PatientController` 등)에 직접 의존하지 않고 엔티티 식별자를 얻기 위해 사용한다.

진입 콜라이더에서 `GetComponentInParent<IScenarioIdentifiedEntity>()` 로 조회한다.

---

## 1. 인터페이스 정의

```csharp
public interface IScenarioIdentifiedEntity
{
    string ScenarioEntityIdentifier { get; }
}
```

### `ScenarioEntityIdentifier`

이 엔티티의 시나리오 식별자(레지스트리 등록 식별자와 동일). 비어 있을 수 있다.

---

## 2. 구현체

| 클래스 | 위치 | 반환 |
|--------|------|------|
| `PatientController` | `Assets/Modules/TriageTrainer/Scripts/Patient/PatientController.cs` | `EffectiveIdentifier`(= `Identifier`) |

---

## 3. 호출 측

`ScenarioTriggerZone.TryRaisePerEntitySignal` (`ScenarioTriggerZone.cs`) 이 진입 오브젝트에서 이 인터페이스를 조회하여, 대상별 신호 템플릿(`_perEntitySignalTemplate`)의 `{id}` 를 식별자로 치환해 신호를 발신한다.

```csharp
var identified = other.GetComponentInParent<IScenarioIdentifiedEntity>();
string id = identified?.ScenarioEntityIdentifier;
// "enter_triage_zone_{id}" → "enter_triage_zone_patient_b"
```

이 신호들을 `SignalCounter` 노드(접두사 매칭 distinct 계측)와 결합하면 도착 인원 수 게이트를 구성할 수 있다.

---

## 참조

- [api:ScenarioGraphNodes — SignalCounter 노드](MultiplayerInfrastructure.Scenario.ScenarioGraphNodes.md)
- [guide:인터랙션 신호 통합 스펙 — 구역 진입](../working-guide/features/scenario/interaction-signal-integration-spec.md)
