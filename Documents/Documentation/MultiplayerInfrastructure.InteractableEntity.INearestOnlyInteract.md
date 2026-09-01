# <a id="MultiplayerInfrastructure_InteractableEntity_INearestOnlyInteract"></a> Interface INearestOnlyInteract

Namespace: [MultiplayerInfrastructure.InteractableEntity](MultiplayerInfrastructure.InteractableEntity.md)  
Assembly: Assembly\-CSharp.dll  

같은 종류의 후보가 감지 범위에 여러 개 들어왔을 때 가장 가까운 상호작용 하나만 노출해야 하는 항목입니다.
빈 그룹 키를 반환하면 현재 상태에서는 거리 필터를 적용하지 않습니다.

```csharp
public interface INearestOnlyInteract
```

## Properties

### <a id="MultiplayerInfrastructure_InteractableEntity_INearestOnlyInteract_NearestOnlyCollider"></a> NearestOnlyCollider

```csharp
Collider NearestOnlyCollider { get; }
```

#### Property Value

 Collider

### <a id="MultiplayerInfrastructure_InteractableEntity_INearestOnlyInteract_NearestOnlyDistanceOrigin"></a> NearestOnlyDistanceOrigin

```csharp
Transform NearestOnlyDistanceOrigin { get; }
```

#### Property Value

 Transform

### <a id="MultiplayerInfrastructure_InteractableEntity_INearestOnlyInteract_NearestOnlyGroup"></a> NearestOnlyGroup

```csharp
string NearestOnlyGroup { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_InteractableEntity_INearestOnlyInteract_NearestOnlyTieBreaker"></a> NearestOnlyTieBreaker

```csharp
int NearestOnlyTieBreaker { get; }
```

#### Property Value

 int

