# <a id="MultiplayerInfrastructure_Entity_Ridable"></a> Class Ridable

Namespace: [MultiplayerInfrastructure.Entity](MultiplayerInfrastructure.Entity.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public abstract class Ridable : NetworkBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
NetworkBehaviour ← 
[Ridable](MultiplayerInfrastructure.Entity.Ridable.md)

## Properties

### <a id="MultiplayerInfrastructure_Entity_Ridable_AttachPoints"></a> AttachPoints

```csharp
protected IReadOnlyList<RidableAttachPointObject> AttachPoints { get; }
```

#### Property Value

 IReadOnlyList<[RidableAttachPointObject](MultiplayerInfrastructure.Entity.RidableAttachPointObject.md)\>

## Methods

### <a id="MultiplayerInfrastructure_Entity_Ridable_Awake_Ridable"></a> Awake\_Ridable\(\)

```csharp
protected void Awake_Ridable()
```

### <a id="MultiplayerInfrastructure_Entity_Ridable_ReleaseAttachPoint_MultiplayerInfrastructure_Player_PlayerController_System_Int32__"></a> ReleaseAttachPoint\(PlayerController, out int\)

```csharp
protected bool ReleaseAttachPoint(PlayerController player, out int attachPointIndex)
```

#### Parameters

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

`attachPointIndex` int

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Entity_Ridable_TryOccupyNextAttachPoint_MultiplayerInfrastructure_Player_PlayerController_System_Int32__UnityEngine_Transform__"></a> TryOccupyNextAttachPoint\(PlayerController, out int, out Transform\)

```csharp
protected bool TryOccupyNextAttachPoint(PlayerController player, out int attachPointIndex, out Transform attachTransform)
```

#### Parameters

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

`attachPointIndex` int

`attachTransform` Transform

#### Returns

 bool

