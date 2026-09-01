# <a id="MultiplayerInfrastructure_Camera_MainCameraController"></a> Class MainCameraController

Namespace: [MultiplayerInfrastructure.Camera](MultiplayerInfrastructure.Camera.md)  
Assembly: Assembly\-CSharp.dll  

메인 카메라 컨트롤러는 메인 카메라 제어를 위해 작성되었습니다.
이 컨트롤러는 게임 시작 시 자동으로 인스턴스화하여 싱글톤 오브젝트로 동작합니다.

카메라를 실제로 들고 다니며 부착점을 추종하는 로직은 <xref href="MultiplayerInfrastructure.Camera.CameraHolder" data-throw-if-not-resolved="false"></xref>가 담당합니다.
이 컨트롤러는 네트워크/싱글톤 수명주기와 부착 대상(플레이어) 바인딩을 담당하고,
카메라 제어 API는 내부 <xref href="MultiplayerInfrastructure.Camera.MainCameraController._holder" data-throw-if-not-resolved="false"></xref>에 위임합니다.

```csharp
[RequireComponent(typeof(NearbyInteractablesDetector))]
public class MainCameraController : NetworkBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
NetworkBehaviour ← 
[MainCameraController](MultiplayerInfrastructure.Camera.MainCameraController.md)

## Properties

### <a id="MultiplayerInfrastructure_Camera_MainCameraController_Camera"></a> Camera

```csharp
public Camera Camera { get; }
```

#### Property Value

 Camera

### <a id="MultiplayerInfrastructure_Camera_MainCameraController_CurrentViewMode"></a> CurrentViewMode

```csharp
public CameraViewMode CurrentViewMode { get; set; }
```

#### Property Value

 [CameraViewMode](MultiplayerInfrastructure.Camera.CameraViewMode.md)

### <a id="MultiplayerInfrastructure_Camera_MainCameraController_DesiredThirdPersonDistance"></a> DesiredThirdPersonDistance

```csharp
public float DesiredThirdPersonDistance { get; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Camera_MainCameraController_FollowingCameraHolder"></a> FollowingCameraHolder

현재 카메라가 추종 중인 부착점의 Transform입니다.
관전 추종 등에서 다른 플레이어의 부착점으로 직접 지정할 수 있습니다.

```csharp
public Transform FollowingCameraHolder { get; set; }
```

#### Property Value

 Transform

### <a id="MultiplayerInfrastructure_Camera_MainCameraController_Holder"></a> Holder

카메라를 들고 다니는 래퍼입니다.

```csharp
public CameraHolder Holder { get; }
```

#### Property Value

 [CameraHolder](MultiplayerInfrastructure.Camera.CameraHolder.md)

### <a id="MultiplayerInfrastructure_Camera_MainCameraController_Instance"></a> Instance

```csharp
public static MainCameraController Instance { get; }
```

#### Property Value

 [MainCameraController](MultiplayerInfrastructure.Camera.MainCameraController.md)

### <a id="MultiplayerInfrastructure_Camera_MainCameraController_MaxThirdPersonDistance"></a> MaxThirdPersonDistance

3인칭 POV 거리 조정의 허용 최대값입니다.

```csharp
public float MaxThirdPersonDistance { get; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Camera_MainCameraController_MinThirdPersonDistance"></a> MinThirdPersonDistance

3인칭 POV 거리 조정의 허용 최소값입니다.

```csharp
public float MinThirdPersonDistance { get; }
```

#### Property Value

 float

## Methods

### <a id="MultiplayerInfrastructure_Camera_MainCameraController_AdjustThirdPersonDistance_System_Single_"></a> AdjustThirdPersonDistance\(float\)

```csharp
public void AdjustThirdPersonDistance(float steps)
```

#### Parameters

`steps` float

### <a id="MultiplayerInfrastructure_Camera_MainCameraController_OnStartClient"></a> OnStartClient\(\)

Called on the client after initializing this object.

```csharp
public override void OnStartClient()
```

### <a id="MultiplayerInfrastructure_Camera_MainCameraController_SetSpectatorLayerCulling_System_Boolean_"></a> SetSpectatorLayerCulling\(bool\)

```csharp
public void SetSpectatorLayerCulling(bool enableSpectator)
```

#### Parameters

`enableSpectator` bool

### <a id="MultiplayerInfrastructure_Camera_MainCameraController_SetTarget_MultiplayerInfrastructure_Player_PlayerController_"></a> SetTarget\(PlayerController\)

로컬 소유자 플레이어의 카메라 부착점에 카메라를 부착합니다.

```csharp
public void SetTarget(PlayerController playerController)
```

#### Parameters

`playerController` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

### <a id="MultiplayerInfrastructure_Camera_MainCameraController_SetThirdPersonDistance_System_Single_"></a> SetThirdPersonDistance\(float\)

```csharp
public void SetThirdPersonDistance(float distance)
```

#### Parameters

`distance` float

