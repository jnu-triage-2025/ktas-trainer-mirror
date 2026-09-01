# <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexMouseProbe"></a> Class PupilReflexMouseProbe

Namespace: [TriageTrainer.Tests.PupilReflexSandbox](TriageTrainer.Tests.PupilReflexSandbox.md)  
Assembly: Assembly\-CSharp.dll  

Uses the mouse cursor as a penlight source and samples direct light
impact for each target eye.

```csharp
[DisallowMultipleComponent]
public class PupilReflexMouseProbe : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[PupilReflexMouseProbe](TriageTrainer.Tests.PupilReflexSandbox.PupilReflexMouseProbe.md)

## Properties

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexMouseProbe_CurrentPrimaryEye"></a> CurrentPrimaryEye

```csharp
public PupilReflexEye CurrentPrimaryEye { get; }
```

#### Property Value

 [PupilReflexEye](TriageTrainer.Tests.PupilReflexSandbox.PupilReflexEye.md)

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexMouseProbe_HasFaceCollider"></a> HasFaceCollider

```csharp
public bool HasFaceCollider { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexMouseProbe_HasFaceSurface"></a> HasFaceSurface

```csharp
public bool HasFaceSurface { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexMouseProbe_HasTargetCamera"></a> HasTargetCamera

```csharp
public bool HasTargetCamera { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexMouseProbe_HasTargetEyes"></a> HasTargetEyes

```csharp
public bool HasTargetEyes { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexMouseProbe_LightIntensityPercent"></a> LightIntensityPercent

```csharp
public float LightIntensityPercent { get; }
```

#### Property Value

 float

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexMouseProbe_ProbeEnabled"></a> ProbeEnabled

```csharp
public bool ProbeEnabled { get; set; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexMouseProbe_ProbeRadiusMillimeters"></a> ProbeRadiusMillimeters

```csharp
public float ProbeRadiusMillimeters { get; }
```

#### Property Value

 float

## Methods

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexMouseProbe_ConfigureProbe_System_Single_System_Single_System_Single_"></a> ConfigureProbe\(float, float, float\)

```csharp
public void ConfigureProbe(float radiusMm, float fullIntensityRadiusMm, float intensityPercent)
```

#### Parameters

`radiusMm` float

`fullIntensityRadiusMm` float

`intensityPercent` float

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexMouseProbe_SetCamera_UnityEngine_Camera_"></a> SetCamera\(Camera\)

```csharp
public void SetCamera(Camera camera)
```

#### Parameters

`camera` Camera

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexMouseProbe_SetFaceSurface_UnityEngine_Transform_UnityEngine_Collider_"></a> SetFaceSurface\(Transform, Collider\)

```csharp
public void SetFaceSurface(Transform root, Collider faceCollider)
```

#### Parameters

`root` Transform

`faceCollider` Collider

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexMouseProbe_SetProbeEnabled_System_Boolean_"></a> SetProbeEnabled\(bool\)

```csharp
public void SetProbeEnabled(bool enabled)
```

#### Parameters

`enabled` bool

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexMouseProbe_SetRequirePrimaryButtonHold_System_Boolean_"></a> SetRequirePrimaryButtonHold\(bool\)

```csharp
public void SetRequirePrimaryButtonHold(bool requireHold)
```

#### Parameters

`requireHold` bool

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexMouseProbe_SetTargetEyes_TriageTrainer_Tests_PupilReflexSandbox_PupilReflexEye___"></a> SetTargetEyes\(PupilReflexEye\[\]\)

```csharp
public void SetTargetEyes(PupilReflexEye[] eyesToTrack)
```

#### Parameters

`eyesToTrack` [PupilReflexEye](TriageTrainer.Tests.PupilReflexSandbox.PupilReflexEye.md)\[\]

