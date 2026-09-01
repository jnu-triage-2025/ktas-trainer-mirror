# <a id="TriageTrainer_MultiplayerInfrastructureSupports_PlayerCharacterModelBase"></a> Class PlayerCharacterModelBase

Namespace: [TriageTrainer.MultiplayerInfrastructureSupports](TriageTrainer.MultiplayerInfrastructureSupports.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public abstract class PlayerCharacterModelBase : MonoBehaviour, IPlayerCharacterModelObject
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[PlayerCharacterModelBase](TriageTrainer.MultiplayerInfrastructureSupports.PlayerCharacterModelBase.md)

#### Derived

[PlayerCharacterModelAiden](TriageTrainer.MultiplayerInfrastructureSupports.PlayerCharacterModelAiden.md), 
[PlayerCharacterModelBrian](TriageTrainer.MultiplayerInfrastructureSupports.PlayerCharacterModelBrian.md), 
[PlayerCharacterModelDominic](TriageTrainer.MultiplayerInfrastructureSupports.PlayerCharacterModelDominic.md), 
[PlayerCharacterModelEmma](TriageTrainer.MultiplayerInfrastructureSupports.PlayerCharacterModelEmma.md), 
[PlayerCharacterModelEthan](TriageTrainer.MultiplayerInfrastructureSupports.PlayerCharacterModelEthan.md), 
[PlayerCharacterModelJeb](TriageTrainer.MultiplayerInfrastructureSupports.PlayerCharacterModelJeb.md), 
[PlayerCharacterModelLiam](TriageTrainer.MultiplayerInfrastructureSupports.PlayerCharacterModelLiam.md), 
[PlayerCharacterModelLisa](TriageTrainer.MultiplayerInfrastructureSupports.PlayerCharacterModelLisa.md), 
[PlayerCharacterModelMaya](TriageTrainer.MultiplayerInfrastructureSupports.PlayerCharacterModelMaya.md), 
[PlayerCharacterModelOlivia](TriageTrainer.MultiplayerInfrastructureSupports.PlayerCharacterModelOlivia.md), 
[PlayerCharacterModelSerah](TriageTrainer.MultiplayerInfrastructureSupports.PlayerCharacterModelSerah.md), 
[PlayerCharacterModelSofia](TriageTrainer.MultiplayerInfrastructureSupports.PlayerCharacterModelSofia.md)

#### Implements

[IPlayerCharacterModelObject](MultiplayerInfrastructure.Player.IPlayerCharacterModelObject.md)

## Properties

### <a id="TriageTrainer_MultiplayerInfrastructureSupports_PlayerCharacterModelBase_Animator"></a> Animator

```csharp
public Animator Animator { get; }
```

#### Property Value

 Animator

### <a id="TriageTrainer_MultiplayerInfrastructureSupports_PlayerCharacterModelBase_AnimatorControllerObject"></a> AnimatorControllerObject

```csharp
public PlayerCharacterModelAnimatorControllerObject AnimatorControllerObject { get; }
```

#### Property Value

 [PlayerCharacterModelAnimatorControllerObject](MultiplayerInfrastructure.Player.PlayerCharacterModelAnimatorControllerObject.md)

### <a id="TriageTrainer_MultiplayerInfrastructureSupports_PlayerCharacterModelBase_CharacterControllerCenter"></a> CharacterControllerCenter

```csharp
public virtual Vector3 CharacterControllerCenter { get; }
```

#### Property Value

 Vector3

### <a id="TriageTrainer_MultiplayerInfrastructureSupports_PlayerCharacterModelBase_HeldItemAttachPoint"></a> HeldItemAttachPoint

```csharp
public Transform HeldItemAttachPoint { get; }
```

#### Property Value

 Transform

### <a id="TriageTrainer_MultiplayerInfrastructureSupports_PlayerCharacterModelBase_HeldItemAttachPointObject"></a> HeldItemAttachPointObject

```csharp
public PlayerCharacterModelHoldingItemAttachPoint HeldItemAttachPointObject { get; }
```

#### Property Value

 [PlayerCharacterModelHoldingItemAttachPoint](MultiplayerInfrastructure.Player.PlayerCharacterModelHoldingItemAttachPoint.md)

## Methods

### <a id="TriageTrainer_MultiplayerInfrastructureSupports_PlayerCharacterModelBase_Awake"></a> Awake\(\)

```csharp
protected virtual void Awake()
```

### <a id="TriageTrainer_MultiplayerInfrastructureSupports_PlayerCharacterModelBase_DisableRootMotionIfAvailable"></a> DisableRootMotionIfAvailable\(\)

```csharp
protected void DisableRootMotionIfAvailable()
```

### <a id="TriageTrainer_MultiplayerInfrastructureSupports_PlayerCharacterModelBase_OnValidate"></a> OnValidate\(\)

```csharp
protected virtual void OnValidate()
```

### <a id="TriageTrainer_MultiplayerInfrastructureSupports_PlayerCharacterModelBase_Reset"></a> Reset\(\)

```csharp
protected virtual void Reset()
```

