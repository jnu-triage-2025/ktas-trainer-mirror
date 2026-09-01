# <a id="MultiplayerInfrastructure_Scenario_ScenarioTimeOperationType"></a> Enum ScenarioTimeOperationType

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

시간 표시(스톱워치/카운트다운)에 가할 연산.
생성/흐름/표시가 각각 분리되어 있어, 하나의 노드 타입으로 다중 타이머를 유연하게 제어한다.

```csharp
public enum ScenarioTimeOperationType
```

## Fields

`Create = 0` 

타이머를 생성(또는 재설정)한다. "정지" 상태로 만들며 화면에 표시하지 않는다.
사용 파라미터: <xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeControlNode.TimerId" data-throw-if-not-resolved="false"></xref>,
<xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeControlNode.Direction" data-throw-if-not-resolved="false"></xref>,
<xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeControlNode.DurationSeconds" data-throw-if-not-resolved="false"></xref>(카운트다운 목표),
<xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeControlNode.StartSeconds" data-throw-if-not-resolved="false"></xref>(시작 표시값).



`Hide = 7` 

화면 표시를 끈다(타이머 상태/흐름은 유지). 파라미터 없음.



`Pause = 2` 

타이머 흐름을 일시정지한다. 사용 파라미터: TimerId.



`Remove = 8` 

타이머를 삭제한다(표시 중이면 표시도 꺼짐). 사용 파라미터: TimerId.



`Resume = 3` 

일시정지된 타이머 흐름을 재개한다. 사용 파라미터: TimerId.



`Set = 5` 

타이머의 현재 표시값을 절대값으로 설정한다(흐름 상태 유지). 카운트다운 목표(총)도 조정 가능.
사용 파라미터: TimerId, <xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeControlNode.StartSeconds" data-throw-if-not-resolved="false"></xref>(설정할 표시값),
<xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeControlNode.DurationSeconds" data-throw-if-not-resolved="false"></xref>(카운트다운 목표 재설정, 0 이면 유지).



`Show = 6` 

지정한 타이머를 화면에 표시한다(표시는 항상 최대 1개, 기존 표시 교체). 사용 파라미터: TimerId.



`Start = 1` 

타이머 흐름을 시작(또는 재시작)한다. 사용 파라미터: TimerId.



`Stop = 4` 

타이머 흐름을 정지하고 값을 시작값으로 되돌린다. 사용 파라미터: TimerId.



