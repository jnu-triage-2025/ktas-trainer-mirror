# <a id="MultiplayerInfrastructure_Camera_CameraHolder"></a> Class CameraHolder

Namespace: [MultiplayerInfrastructure.Camera](MultiplayerInfrastructure.Camera.md)  
Assembly: Assembly\-CSharp.dll  

실제 <xref href="UnityEngine.Camera" data-throw-if-not-resolved="false"></xref>를 감싸고 특정 <xref href="MultiplayerInfrastructure.Camera.CameraAttachPoint" data-throw-if-not-resolved="false"></xref>에 부착되어
그 지점을 추종하는 "카메라 래퍼"입니다.

이전 구조에서는 <code>MainCameraController</code>가 플레이어 자식으로 생성된 "CameraHolder" Transform을
직접 <code>_followingCameraHolder</code>로 참조하여, "카메라가 붙는 대상"과 "카메라를 들고 다니는 주체"의
책임이 분리되어 있지 않았습니다.

이 클래스는 "카메라를 들고 다니는 주체" 책임만을 담당합니다.
- <xref href="MultiplayerInfrastructure.Camera.CameraHolder.AttachTo(MultiplayerInfrastructure.Camera.CameraAttachPoint)" data-throw-if-not-resolved="false"></xref> / <xref href="MultiplayerInfrastructure.Camera.CameraHolder.Detach" data-throw-if-not-resolved="false"></xref>로 어느 부착점을 따라갈지 결정합니다.
- 매 프레임 <xref href="MultiplayerInfrastructure.Camera.CameraHolder.Follow" data-throw-if-not-resolved="false"></xref>에서 부착점의 위치/회전을 카메라에 복사하고,
  시점 모드에 따른 거리 오프셋과 벽 충돌 보정을 적용합니다.

<xref href="UnityEngine.MonoBehaviour" data-throw-if-not-resolved="false"></xref>가 아닌 직렬화 가능한 순수 클래스로 두어,
소유자(<code>MainCameraController</code>)의 인스펙터에서 설정값을 그대로 편집할 수 있게 합니다.

```csharp
[Serializable]
public class CameraHolder
```

#### Inheritance

object ← 
[CameraHolder](MultiplayerInfrastructure.Camera.CameraHolder.md)

## Properties

### <a id="MultiplayerInfrastructure_Camera_CameraHolder_Camera"></a> Camera

```csharp
public Camera Camera { get; }
```

#### Property Value

 Camera

### <a id="MultiplayerInfrastructure_Camera_CameraHolder_CurrentViewMode"></a> CurrentViewMode

```csharp
public CameraViewMode CurrentViewMode { get; set; }
```

#### Property Value

 [CameraViewMode](MultiplayerInfrastructure.Camera.CameraViewMode.md)

### <a id="MultiplayerInfrastructure_Camera_CameraHolder_DesiredThirdPersonDistance"></a> DesiredThirdPersonDistance

현재 사용자가 설정한 3인칭 POV 거리(벽 충돌 반영 전)입니다.

```csharp
public float DesiredThirdPersonDistance { get; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Camera_CameraHolder_FollowingPivot"></a> FollowingPivot

현재 부착되어 추종 중인 부착점의 피벗 Transform입니다. 부착되지 않았다면 null입니다.
관전 추종 등에서 다른 플레이어의 부착점을 직접 지정할 때 사용합니다.
이 setter로 대상을 바꾸면 이전 대상의 충돌 무시 콜라이더 목록은 초기화됩니다.
(관전 대상 등 자기 자신이 아닌 대상을 따라갈 때는 자기 콜라이더 무시가 불필요/부정확하므로)

```csharp
public Transform FollowingPivot { get; set; }
```

#### Property Value

 Transform

### <a id="MultiplayerInfrastructure_Camera_CameraHolder_MaxThirdPersonDistance"></a> MaxThirdPersonDistance

3인칭 POV 거리 조정의 허용 최대값입니다.

```csharp
public float MaxThirdPersonDistance { get; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Camera_CameraHolder_MinThirdPersonDistance"></a> MinThirdPersonDistance

3인칭 POV 거리 조정의 허용 최소값입니다.

```csharp
public float MinThirdPersonDistance { get; }
```

#### Property Value

 float

## Methods

### <a id="MultiplayerInfrastructure_Camera_CameraHolder_AdjustThirdPersonDistance_System_Single_"></a> AdjustThirdPersonDistance\(float\)

3인칭 POV 거리를 스텝 단위로 조정합니다.
양수(steps &gt; 0)는 카메라를 플레이어에 가깝게, 음수는 멀게 합니다.

```csharp
public void AdjustThirdPersonDistance(float steps)
```

#### Parameters

`steps` float

스크롤 스텝 수. 각 스텝은 _distanceZoomStep 만큼 변화합니다.

### <a id="MultiplayerInfrastructure_Camera_CameraHolder_AttachTo_MultiplayerInfrastructure_Camera_CameraAttachPoint_"></a> AttachTo\(CameraAttachPoint\)

지정한 부착점에 카메라를 부착합니다. 이후 <xref href="MultiplayerInfrastructure.Camera.CameraHolder.Follow" data-throw-if-not-resolved="false"></xref>가 이 지점을 추종합니다.
해당 부착점을 소유한 플레이어의 콜라이더는 카메라 충돌 검사에서 무시됩니다.

```csharp
public void AttachTo(CameraAttachPoint attachPoint)
```

#### Parameters

`attachPoint` [CameraAttachPoint](MultiplayerInfrastructure.Camera.CameraAttachPoint.md)

### <a id="MultiplayerInfrastructure_Camera_CameraHolder_Detach"></a> Detach\(\)

부착을 해제합니다. 이후 카메라는 추종을 멈춥니다.

```csharp
public void Detach()
```

### <a id="MultiplayerInfrastructure_Camera_CameraHolder_Follow"></a> Follow\(\)

매 프레임(LateUpdate) 호출하여 카메라를 부착점에 맞춰 이동/회전시키고
거리 오프셋 및 충돌 보정을 적용합니다.

```csharp
public void Follow()
```

### <a id="MultiplayerInfrastructure_Camera_CameraHolder_Initialize"></a> Initialize\(\)

소유자의 초기화 시점에 호출하여 거리 상태를 현재 모드에 맞춰 즉시 확정합니다.

```csharp
public void Initialize()
```

### <a id="MultiplayerInfrastructure_Camera_CameraHolder_RefreshFromInspector"></a> RefreshFromInspector\(\)

에디터에서 인스펙터 값이 변경되었을 때(재생 중) 즉시 반영하기 위한 갱신입니다.

```csharp
public void RefreshFromInspector()
```

### <a id="MultiplayerInfrastructure_Camera_CameraHolder_SetSpectatorLayerCulling_System_Boolean_"></a> SetSpectatorLayerCulling\(bool\)

로컬 카메라에서 관전자 레이어 가시성을 토글합니다.
플레이어는 관전자를 볼 수 없고, 관전자는 볼 수 있어야 합니다.

```csharp
public void SetSpectatorLayerCulling(bool enableSpectator)
```

#### Parameters

`enableSpectator` bool

### <a id="MultiplayerInfrastructure_Camera_CameraHolder_SetThirdPersonDistance_System_Single_"></a> SetThirdPersonDistance\(float\)

3인칭 POV 거리를 절대값으로 설정합니다. 허용 범위로 clamp됩니다.

```csharp
public void SetThirdPersonDistance(float distance)
```

#### Parameters

`distance` float

