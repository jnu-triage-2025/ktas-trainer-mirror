# <a id="TriageTrainer_Entity_RecognitionCheckMicrophoneInput"></a> Class RecognitionCheckMicrophoneInput

Namespace: [TriageTrainer.Entity](TriageTrainer.Entity.md)  
Assembly: Assembly\-CSharp.dll  

마이크 음량이 임계치를 1초 이상 넘으면 활성 의식 확인을 완료한다.

```csharp
public sealed class RecognitionCheckMicrophoneInput : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[RecognitionCheckMicrophoneInput](TriageTrainer.Entity.RecognitionCheckMicrophoneInput.md)

## Methods

### <a id="TriageTrainer_Entity_RecognitionCheckMicrophoneInput_GetUnavailableGuidance_TriageTrainer_Entity_RecognitionCheckMicrophoneInput_Availability_"></a> GetUnavailableGuidance\(Availability\)

```csharp
public static string GetUnavailableGuidance(RecognitionCheckMicrophoneInput.Availability availability)
```

#### Parameters

`availability` [RecognitionCheckMicrophoneInput](TriageTrainer.Entity.RecognitionCheckMicrophoneInput.md).[Availability](TriageTrainer.Entity.RecognitionCheckMicrophoneInput.Availability.md)

#### Returns

 string

### <a id="TriageTrainer_Entity_RecognitionCheckMicrophoneInput_IsUnavailableState_TriageTrainer_Entity_RecognitionCheckMicrophoneInput_Availability_"></a> IsUnavailableState\(Availability\)

```csharp
public static bool IsUnavailableState(RecognitionCheckMicrophoneInput.Availability availability)
```

#### Parameters

`availability` [RecognitionCheckMicrophoneInput](TriageTrainer.Entity.RecognitionCheckMicrophoneInput.md).[Availability](TriageTrainer.Entity.RecognitionCheckMicrophoneInput.Availability.md)

#### Returns

 bool

### <a id="TriageTrainer_Entity_RecognitionCheckMicrophoneInput_Retry_System_Action_TriageTrainer_Entity_RecognitionCheckMicrophoneInput_Availability__"></a> Retry\(Action<Availability\>\)

```csharp
public static void Retry(Action<RecognitionCheckMicrophoneInput.Availability> completed)
```

#### Parameters

`completed` Action<[RecognitionCheckMicrophoneInput](TriageTrainer.Entity.RecognitionCheckMicrophoneInput.md).[Availability](TriageTrainer.Entity.RecognitionCheckMicrophoneInput.Availability.md)\>

### <a id="TriageTrainer_Entity_RecognitionCheckMicrophoneInput_StartMonitoring_TriageTrainer_Entity_PatientController_"></a> StartMonitoring\(PatientController\)

```csharp
public static void StartMonitoring(PatientController target)
```

#### Parameters

`target` [PatientController](TriageTrainer.Entity.PatientController.md)

### <a id="TriageTrainer_Entity_RecognitionCheckMicrophoneInput_StopMonitoring_TriageTrainer_Entity_PatientController_"></a> StopMonitoring\(PatientController\)

```csharp
public static void StopMonitoring(PatientController target)
```

#### Parameters

`target` [PatientController](TriageTrainer.Entity.PatientController.md)

