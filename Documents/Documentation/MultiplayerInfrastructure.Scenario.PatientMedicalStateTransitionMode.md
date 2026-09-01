# <a id="MultiplayerInfrastructure_Scenario_PatientMedicalStateTransitionMode"></a> Enum PatientMedicalStateTransitionMode

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

프리셋 값이 대상 환자에게 적용되는 방식.

```csharp
public enum PatientMedicalStateTransitionMode
```

## Fields

`Gradual = 1` 

지정한 소요 시간(<xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.TransitionDurationSeconds" data-throw-if-not-resolved="false"></xref>)
동안 현재 값에서 대상 값으로 점차 보간(lerp)한다.
수치 필드(GCS/호흡수/맥박수/혈압 등)만 보간되며, 열거형/불리언 등 비수치 필드는 보간 종료 시점에 적용된다.
측정 불가(-1) 값은 보간 대상이 아니므로 즉시 적용된다.



`Immediate = 0` 

즉시(한 프레임에) 대상 값으로 덮어쓴다.



