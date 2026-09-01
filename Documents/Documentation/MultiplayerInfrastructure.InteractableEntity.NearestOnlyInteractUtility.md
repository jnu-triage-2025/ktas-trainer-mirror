# <a id="MultiplayerInfrastructure_InteractableEntity_NearestOnlyInteractUtility"></a> Class NearestOnlyInteractUtility

Namespace: [MultiplayerInfrastructure.InteractableEntity](MultiplayerInfrastructure.InteractableEntity.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public static class NearestOnlyInteractUtility
```

#### Inheritance

object ← 
[NearestOnlyInteractUtility](MultiplayerInfrastructure.InteractableEntity.NearestOnlyInteractUtility.md)

## Methods

### <a id="MultiplayerInfrastructure_InteractableEntity_NearestOnlyInteractUtility_DistanceTo_MultiplayerInfrastructure_InteractableEntity_INearestOnlyInteract_UnityEngine_Vector3_"></a> DistanceTo\(INearestOnlyInteract, Vector3\)

```csharp
public static float DistanceTo(INearestOnlyInteract candidate, Vector3 referencePosition)
```

#### Parameters

`candidate` [INearestOnlyInteract](MultiplayerInfrastructure.InteractableEntity.INearestOnlyInteract.md)

`referencePosition` Vector3

#### Returns

 float

### <a id="MultiplayerInfrastructure_InteractableEntity_NearestOnlyInteractUtility_IsPreferred_System_Single_System_Int32_System_Single_System_Int32_"></a> IsPreferred\(float, int, float, int\)

```csharp
public static bool IsPreferred(float distance, int tieBreaker, float nearestDistance, int nearestTieBreaker)
```

#### Parameters

`distance` float

`tieBreaker` int

`nearestDistance` float

`nearestTieBreaker` int

#### Returns

 bool

