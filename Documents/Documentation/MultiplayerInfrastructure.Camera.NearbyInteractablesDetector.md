# <a id="MultiplayerInfrastructure_Camera_NearbyInteractablesDetector"></a> Class NearbyInteractablesDetector

Namespace: [MultiplayerInfrastructure.Camera](MultiplayerInfrastructure.Camera.md)  
Assembly: Assembly\-CSharp.dll  

PlayerInteractiveDetector는 플레이어가 상호작용할 수 있는 물체를 감지해 사용자의 로컬 UI에 표시하도록 지시합니다.

상호작용 가능 물체에 대한 툴팁은 플레이어 위치가 아닌 카메라의 위치를 기준으로 표시되므로,
이 컴포넌트는 카메라에 추가되도록 의도되었습니다.

카메라의 위치를 기준으로 하는 이유는 관전자 모드에서도 상호작용 툴팁을 표시하기 위함입니다.
(카메라 홀더를 기준으로 하면, 관전자 모드 상황에서는 카메라만 다른 카메라 홀더에 붙으므로 적절히 표시되지 않음)
(+ 이와 관련한 개선 구현 방안이 있으나 후순위로 변경: TODO.md 참고)

```csharp
public class NearbyInteractablesDetector : NetworkBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
NetworkBehaviour ← 
[NearbyInteractablesDetector](MultiplayerInfrastructure.Camera.NearbyInteractablesDetector.md)

## Properties

### <a id="MultiplayerInfrastructure_Camera_NearbyInteractablesDetector_DetectionPosition"></a> DetectionPosition

```csharp
public Vector3 DetectionPosition { get; }
```

#### Property Value

 Vector3

### <a id="MultiplayerInfrastructure_Camera_NearbyInteractablesDetector_InteractableNearbyExists"></a> InteractableNearbyExists

```csharp
public bool InteractableNearbyExists { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Camera_NearbyInteractablesDetector_Nearby"></a> Nearby

```csharp
public IReadOnlyList<IInteractable> Nearby { get; }
```

#### Property Value

 IReadOnlyList<[IInteractable](MultiplayerInfrastructure.InteractableEntity.IInteractable.md)\>

## Methods

### <a id="MultiplayerInfrastructure_Camera_NearbyInteractablesDetector_QueryCurrentInteractables_System_Collections_Generic_List_MultiplayerInfrastructure_InteractableEntity_IInteractable__"></a> QueryCurrentInteractables\(List<IInteractable\>\)

```csharp
public void QueryCurrentInteractables(List<IInteractable> results)
```

#### Parameters

`results` List<[IInteractable](MultiplayerInfrastructure.InteractableEntity.IInteractable.md)\>

### <a id="MultiplayerInfrastructure_Camera_NearbyInteractablesDetector_RegisterDetectBased_UnityEngine_Transform_"></a> RegisterDetectBased\(Transform\)

```csharp
public void RegisterDetectBased(Transform _transform)
```

#### Parameters

`_transform` Transform

### <a id="MultiplayerInfrastructure_Camera_NearbyInteractablesDetector_NearbyUpdated"></a> NearbyUpdated

```csharp
public event Action<IReadOnlyList<IInteractable>> NearbyUpdated
```

#### Event Type

 Action<IReadOnlyList<[IInteractable](MultiplayerInfrastructure.InteractableEntity.IInteractable.md)\>\>

