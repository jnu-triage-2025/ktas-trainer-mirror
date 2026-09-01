# <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexEye"></a> Class PupilReflexEye

Namespace: [TriageTrainer.Tests.PupilReflexSandbox](TriageTrainer.Tests.PupilReflexSandbox.md)  
Assembly: Assembly\-CSharp.dll  

Models a single eye with sclera, iris, pupil and cornea layers.
The iris and pupil are rendered as spherical patches that share
the same curvature as the eyeball.

```csharp
[DisallowMultipleComponent]
public class PupilReflexEye : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[PupilReflexEye](TriageTrainer.Tests.PupilReflexSandbox.PupilReflexEye.md)

## Properties

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexEye_ConstrictionStrength"></a> ConstrictionStrength

```csharp
public float ConstrictionStrength { get; }
```

#### Property Value

 float

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexEye_CurrentPupilDiameterMillimeters"></a> CurrentPupilDiameterMillimeters

```csharp
public float CurrentPupilDiameterMillimeters { get; }
```

#### Property Value

 float

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexEye_CurrentPupilRadiusMeters"></a> CurrentPupilRadiusMeters

```csharp
public float CurrentPupilRadiusMeters { get; }
```

#### Property Value

 float

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexEye_EyeballDiameterMeters"></a> EyeballDiameterMeters

```csharp
public float EyeballDiameterMeters { get; }
```

#### Property Value

 float

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexEye_HasReceivedDirectLight"></a> HasReceivedDirectLight

```csharp
public bool HasReceivedDirectLight { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexEye_PupilCenterWorld"></a> PupilCenterWorld

```csharp
public Vector3 PupilCenterWorld { get; }
```

#### Property Value

 Vector3

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexEye_ReactsToDirectLight"></a> ReactsToDirectLight

```csharp
public bool ReactsToDirectLight { get; }
```

#### Property Value

 bool

## Methods

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexEye_ConfigureAnatomy_System_Single_System_Single_System_Single_System_Single_"></a> ConfigureAnatomy\(float, float, float, float\)

```csharp
public void ConfigureAnatomy(float eyeballDiameterCm, float irisDiameterMm, float restPupilMm, float minPupilMm)
```

#### Parameters

`eyeballDiameterCm` float

`irisDiameterMm` float

`restPupilMm` float

`minPupilMm` float

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexEye_ConfigureReaction_System_Boolean_System_Single_System_Single_System_Single_System_Single_"></a> ConfigureReaction\(bool, float, float, float, float\)

```csharp
public void ConfigureReaction(bool reacts, float strength, float constrictionSeconds, float dilationSeconds, float decayB)
```

#### Parameters

`reacts` bool

`strength` float

`constrictionSeconds` float

`dilationSeconds` float

`decayB` float

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexEye_ResetExamState"></a> ResetExamState\(\)

```csharp
public void ResetExamState()
```

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexEye_SetDirectLightLevel_System_Single_"></a> SetDirectLightLevel\(float\)

```csharp
public void SetDirectLightLevel(float stimulus01)
```

#### Parameters

`stimulus01` float

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexEye_TryRaycastEyeball_UnityEngine_Ray_UnityEngine_Vector3__UnityEngine_Vector3__"></a> TryRaycastEyeball\(Ray, out Vector3, out Vector3\)

```csharp
public bool TryRaycastEyeball(Ray ray, out Vector3 hitPoint, out Vector3 hitNormal)
```

#### Parameters

`ray` Ray

`hitPoint` Vector3

`hitNormal` Vector3

#### Returns

 bool

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexEye_TrySampleLight_UnityEngine_Ray_System_Single_System_Single_System_Single_System_Single__System_Single__"></a> TrySampleLight\(Ray, float, float, float, out float, out float\)

Samples how much direct light reaches the pupil.
Returns true when the probe ray can be projected onto the pupil plane.

```csharp
public bool TrySampleLight(Ray probeRay, float probeOuterRadiusMeters, float probeInnerRadiusMeters, float lightIntensity01, out float stimulus01, out float radialDistanceMeters)
```

#### Parameters

`probeRay` Ray

`probeOuterRadiusMeters` float

`probeInnerRadiusMeters` float

`lightIntensity01` float

`stimulus01` float

`radialDistanceMeters` float

#### Returns

 bool

### <a id="TriageTrainer_Tests_PupilReflexSandbox_PupilReflexEye_TrySampleLightFromAxis_UnityEngine_Vector3_UnityEngine_Vector3_System_Single_System_Single_System_Single_System_Single__System_Single__"></a> TrySampleLightFromAxis\(Vector3, Vector3, float, float, float, out float, out float\)

```csharp
public bool TrySampleLightFromAxis(Vector3 axisPoint, Vector3 axisDirection, float probeOuterRadiusMeters, float probeInnerRadiusMeters, float lightIntensity01, out float stimulus01, out float radialDistanceMeters)
```

#### Parameters

`axisPoint` Vector3

`axisDirection` Vector3

`probeOuterRadiusMeters` float

`probeInnerRadiusMeters` float

`lightIntensity01` float

`stimulus01` float

`radialDistanceMeters` float

#### Returns

 bool

