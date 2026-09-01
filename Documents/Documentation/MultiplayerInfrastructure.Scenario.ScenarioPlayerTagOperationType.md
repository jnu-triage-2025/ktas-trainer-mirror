# <a id="MultiplayerInfrastructure_Scenario_ScenarioPlayerTagOperationType"></a> Enum ScenarioPlayerTagOperationType

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

PlayerTag 노드의 태그 조작 타입.

```csharp
public enum ScenarioPlayerTagOperationType
```

## Fields

`Add = 0` 

지정한 태그를 대상 플레이어에게 추가합니다.



`Change = 2` 

대상 플레이어의 FromTag를 ToTag로 교체합니다.



`Remove = 1` 

지정한 태그를 대상 플레이어에게서 제거합니다.



`Swap = 3` 

SwapTagA 보유 플레이어와 SwapTagB 보유 플레이어의 해당 태그를 서로 교환합니다(역할 교대).



