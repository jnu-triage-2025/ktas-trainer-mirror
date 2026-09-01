# <a id="MultiplayerInfrastructure_Scenario_ScenarioValidatorWaitTimeoutBehavior"></a> Enum ScenarioValidatorWaitTimeoutBehavior

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

<xref href="MultiplayerInfrastructure.Scenario.ScenarioValidatorNode.WaitForCondition" data-throw-if-not-resolved="false"></xref> 게이트가 <xref href="MultiplayerInfrastructure.Scenario.ScenarioValidatorNode.WaitTimeoutSeconds" data-throw-if-not-resolved="false"></xref>
동안 조건을 충족하지 못했을 때의 행동 정책. 기본값(<xref href="MultiplayerInfrastructure.Scenario.ScenarioValidatorWaitTimeoutBehavior.KeepWaiting" data-throw-if-not-resolved="false"></xref>)은 기존 동작(무한 대기)과 동일하다.

```csharp
public enum ScenarioValidatorWaitTimeoutBehavior
```

## Fields

`FailBranch = 1` 

<xref href="MultiplayerInfrastructure.Scenario.ScenarioValidatorNode.FailureNextIdentifier" data-throw-if-not-resolved="false"></xref> 로 분기한다(미지정/미존재 시 KeepWaiting 으로 폴백).



`ForceAdvance = 2` 

<xref href="MultiplayerInfrastructure.Scenario.ScenarioValidatorNode.NextIdentifier" data-throw-if-not-resolved="false"></xref> 로 강제 진행한다(미수행 기록 이벤트와 함께).



`KeepWaiting = 0` 

기존 동작(무한 대기). 타임아웃을 무시하고 조건이 올라올 때까지 계속 대기한다. 기본값.



`WarnAndKeepWaiting = 3` 

운영자에게 경고(콘솔/인게임챗)한 뒤 계속 대기한다.



