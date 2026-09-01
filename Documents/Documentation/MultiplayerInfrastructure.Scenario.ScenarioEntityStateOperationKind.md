# <a id="MultiplayerInfrastructure_Scenario_ScenarioEntityStateOperationKind"></a> Enum ScenarioEntityStateOperationKind

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

<xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityInitNode" data-throw-if-not-resolved="false"></xref> 가 엔티티에 적용하는 초기 상태 항목의 종류.

```csharp
public enum ScenarioEntityStateOperationKind
```

## Fields

`DisplayState = 1` 

엔티티 컴포넌트(<xref href="MultiplayerInfrastructure.Entity.IScenarioEntityInitTarget" data-throw-if-not-resolved="false"></xref> 구현체)의
명명된 표시/부착 상태를 설정한다. 환자 엔티티의 처치 부착물(주사기/거즈/경부보호대 등)
초기 표시 여부 설정이 1차 목표다. <xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityStateOperation.DisplayActive" data-throw-if-not-resolved="false"></xref> 로 표시/비표시를 정한다.



`StateStore = 0` 

시나리오 인메모리 상태 저장소(<code>_stateStore</code>)에 <code>"{entityIdentifier}.{key}" = value</code> 를 기록한다.
시나리오 그래프의 다른 노드가 참조하는 순수 데이터 상태.



