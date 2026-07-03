# API 레퍼런스: `MultiplayerInfrastructure.Entity.IScenarioTriageAssessTarget`

> **네임스페이스:** `MultiplayerInfrastructure.Entity`
>
> **파일 위치:** `Assets/Modules/MultiplayerInfrastructure/Scripts/Entity/IScenarioTriageAssessTarget.cs`

---

## 0. 개요

시나리오 그래프의 `TriageAssessControl` 노드가 트리아지 평가 인터랙션을 활성화하거나 비활성화할 수 있는 엔티티가 구현하는 인터페이스다.

`IScenarioEntityInitTarget`과 동일한 설계 원칙을 따른다. 시나리오 컨트롤러는 이 인터페이스를 통해 도메인 구현(`PatientController`)에 직접 의존하지 않고 평가 가능 여부를 제어한다.

---

## 1. 인터페이스 정의

```csharp
public interface IScenarioTriageAssessTarget
{
    void SetTriageAssessable(bool assessable);
}
```

### `SetTriageAssessable(bool assessable)`

트리아지 평가 인터랙션의 활성화 여부를 설정한다.

- `true`: 인터랙션을 활성화해 플레이어가 트리아지를 분류할 수 있게 한다.
- `false`: 인터랙션을 비활성화해 분류를 차단한다.

서버 컨텍스트에서 호출되면 내부적으로 `SyncVar<bool>`을 통해 모든 클라이언트에 상태가 복제된다.

---

## 2. 구현체

| 클래스 | 위치 |
|--------|------|
| `PatientController` | `Assets/Modules/TriageTrainer/Scripts/Patient/PatientController.cs` (부분 구현: `PatientController.Triage.cs`) |

---

## 3. 호출 측

`ScenarioController.ExecuteTriageAssessControlNode` (`ScenarioController.cs`) 에서 레지스트리로 대상 엔티티를 조회한 뒤 이 인터페이스를 통해 호출한다.

```csharp
var target = descriptor.GameObject
    .GetComponentInChildren<IScenarioTriageAssessTarget>(true);
target?.SetTriageAssessable(node.Assessable);
```

---

## 참조

- [req:TriageAssessControl 노드 요구사항](../requirements/scenario/triage-assess-control-node-requirements.md)
- [api:IScenarioEntityInitTarget](MultiplayerInfrastructure.Entity.IItemUseTarget.md)
