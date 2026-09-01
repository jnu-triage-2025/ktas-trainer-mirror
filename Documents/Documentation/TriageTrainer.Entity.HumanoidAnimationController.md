# <a id="TriageTrainer_Entity_HumanoidAnimationController"></a> Class HumanoidAnimationController

Namespace: [TriageTrainer.Entity](TriageTrainer.Entity.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public sealed class HumanoidAnimationController : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[HumanoidAnimationController](TriageTrainer.Entity.HumanoidAnimationController.md)

## Properties

### <a id="TriageTrainer_Entity_HumanoidAnimationController_Animator"></a> Animator

```csharp
public Animator Animator { get; }
```

#### Property Value

 Animator

### <a id="TriageTrainer_Entity_HumanoidAnimationController_HasAnimator"></a> HasAnimator

```csharp
public bool HasAnimator { get; }
```

#### Property Value

 bool

## Methods

### <a id="TriageTrainer_Entity_HumanoidAnimationController_CrossFade_System_String_System_Single_System_Int32_System_Single_"></a> CrossFade\(string, float, int, float\)

```csharp
public void CrossFade(string stateName, float transitionDuration, int layer = 0, float normalizedTime = 0)
```

#### Parameters

`stateName` string

`transitionDuration` float

`layer` int

`normalizedTime` float

### <a id="TriageTrainer_Entity_HumanoidAnimationController_Play_System_String_System_Int32_System_Single_"></a> Play\(string, int, float\)

```csharp
public void Play(string stateName, int layer = 0, float normalizedTime = 0)
```

#### Parameters

`stateName` string

`layer` int

`normalizedTime` float

### <a id="TriageTrainer_Entity_HumanoidAnimationController_ResetTrigger_System_String_"></a> ResetTrigger\(string\)

```csharp
public void ResetTrigger(string parameter)
```

#### Parameters

`parameter` string

### <a id="TriageTrainer_Entity_HumanoidAnimationController_SetBool_System_String_System_Boolean_"></a> SetBool\(string, bool\)

```csharp
public void SetBool(string parameter, bool value)
```

#### Parameters

`parameter` string

`value` bool

### <a id="TriageTrainer_Entity_HumanoidAnimationController_SetController_UnityEngine_RuntimeAnimatorController_"></a> SetController\(RuntimeAnimatorController\)

```csharp
public void SetController(RuntimeAnimatorController controller)
```

#### Parameters

`controller` RuntimeAnimatorController

### <a id="TriageTrainer_Entity_HumanoidAnimationController_SetFloat_System_String_System_Single_"></a> SetFloat\(string, float\)

```csharp
public void SetFloat(string parameter, float value)
```

#### Parameters

`parameter` string

`value` float

### <a id="TriageTrainer_Entity_HumanoidAnimationController_SetInt_System_String_System_Int32_"></a> SetInt\(string, int\)

```csharp
public void SetInt(string parameter, int value)
```

#### Parameters

`parameter` string

`value` int

### <a id="TriageTrainer_Entity_HumanoidAnimationController_SetTrigger_System_String_"></a> SetTrigger\(string\)

```csharp
public void SetTrigger(string parameter)
```

#### Parameters

`parameter` string

