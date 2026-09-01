# <a id="TriageTrainer_DebugTools_DebugSignalEmitter"></a> Class DebugSignalEmitter

Namespace: [TriageTrainer.DebugTools](TriageTrainer.DebugTools.md)  
Assembly: Assembly\-CSharp.dll  

IndevScene 등 검증용으로, 임의의 시나리오 인터랙션 신호(sig.*)를 손쉽게 발생/해제하는 디버그 컴포넌트.

<p>사용처</p>
<ul><li>평면 월드맵에 빈 GameObject 로 임시 배치하여 게이트(Validator/Parallel) 통과를 검증한다.</li><li>구역 진입을 흉내내는 트리거 박스로 쓰거나, 키 입력으로 특정 신호를 즉시 올린다.</li></ul>

<p>주의</p>

디버그 전용이다. 실제 게임플레이 배선(아이템 사용/사정/연결 등)을 대체하지 않으며,
빌드/프로덕션 씬에는 배치하지 않는 것을 전제로 한다. 신호는
<xref href="MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Raise(System.String)" data-throw-if-not-resolved="false"></xref>(서버 권한 라우팅)로 올린다.

```csharp
public sealed class DebugSignalEmitter : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[DebugSignalEmitter](TriageTrainer.DebugTools.DebugSignalEmitter.md)

## Methods

### <a id="TriageTrainer_DebugTools_DebugSignalEmitter_ClearAll"></a> ClearAll\(\)

```csharp
[ContextMenu("Clear Signals Now")]
public void ClearAll()
```

### <a id="TriageTrainer_DebugTools_DebugSignalEmitter_RaiseAll"></a> RaiseAll\(\)

```csharp
[ContextMenu("Raise Signals Now")]
public void RaiseAll()
```

