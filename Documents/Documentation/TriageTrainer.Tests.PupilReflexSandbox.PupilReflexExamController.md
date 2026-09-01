# <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexExamController"></a> Class PupilReflexExamController

Namespace: [TriageTrainer.Tests.PupilReflexSandbox](TriageTrainer.Tests.PupilReflexSandbox.md)  
Assembly: Assembly\-CSharp.dll  

Tracks bilateral direct light exam completion and exposes UnityEvents
that can be connected to downstream scenario logic.

```csharp
[DisallowMultipleComponent]
public class PupilReflexExamController : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[PupilReflexExamController](TriageTrainer.Tests.PupilReflexSandbox.PupilReflexExamController.md)

## Properties

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexExamController_HasAssignedEyes"></a> HasAssignedEyes

```csharp
public bool HasAssignedEyes { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexExamController_IsCompleted"></a> IsCompleted

```csharp
public bool IsCompleted { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexExamController_LeftChecked"></a> LeftChecked

```csharp
public bool LeftChecked { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexExamController_RightChecked"></a> RightChecked

```csharp
public bool RightChecked { get; }
```

#### Property Value

 bool

## Methods

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexExamController_ConfigureEyes_TriageTrainer_Tests_PupilReflexSandbox_PupilReflexEye_TriageTrainer_Tests_PupilReflexSandbox_PupilReflexEye_"></a> ConfigureEyes\(PupilReflexEye, PupilReflexEye\)

```csharp
public void ConfigureEyes(PupilReflexEye left, PupilReflexEye right)
```

#### Parameters

`left` [PupilReflexEye](TriageTrainer.Tests.PupilReflexSandbox.PupilReflexEye.md)

`right` [PupilReflexEye](TriageTrainer.Tests.PupilReflexSandbox.PupilReflexEye.md)

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexExamController_ResetProgress"></a> ResetProgress\(\)

```csharp
public void ResetProgress()
```

