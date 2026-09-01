# <a id="TriageTrainer_Entity_ILevel1RapidInfuserStateSource"></a> Interface ILevel1RapidInfuserStateSource

Namespace: [TriageTrainer.Entity](TriageTrainer.Entity.md)  
Assembly: Assembly\-CSharp.dll  

환자 저장 상태가 급속 주입기 상태를 보유할 경우 구현하는 선택적 복원 규약.
PatientController 자체에 저장 형식을 강제하지 않기 위해 인터페이스로 예비한다.

```csharp
public interface ILevel1RapidInfuserStateSource
```

## Methods

### <a id="TriageTrainer_Entity_ILevel1RapidInfuserStateSource_TryGetLevel1RapidInfuserState_TriageTrainer_Entity_Level1RapidInfuserState__"></a> TryGetLevel1RapidInfuserState\(out Level1RapidInfuserState\)

```csharp
bool TryGetLevel1RapidInfuserState(out Level1RapidInfuserState state)
```

#### Parameters

`state` [Level1RapidInfuserState](TriageTrainer.Entity.Level1RapidInfuserState.md)

#### Returns

 bool

