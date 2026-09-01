# <a id="MultiplayerInfrastructure_Performance_CameraDistancePreferenceService"></a> Class CameraDistancePreferenceService

Namespace: [MultiplayerInfrastructure.Performance](MultiplayerInfrastructure.Performance.md)  
Assembly: Assembly\-CSharp.dll  

3인칭 카메라 POV(거리) 사용자 설정을 관리하는 서비스입니다.

역할:
  - 사용자가 설정한 3인칭 카메라 거리를 <xref href="UnityEngine.PlayerPrefs" data-throw-if-not-resolved="false"></xref>에 저장/로드합니다.
  - 활성 <xref href="MultiplayerInfrastructure.Camera.MainCameraController" data-throw-if-not-resolved="false"></xref>에 값을 적용합니다.
  - 설정 변경 시 <xref href="MultiplayerInfrastructure.Performance.CameraDistancePreferenceService.OnDistanceChanged" data-throw-if-not-resolved="false"></xref> 이벤트를 발행합니다.
  - <xref href="MultiplayerInfrastructure.Registry.RegistryType.Service" data-throw-if-not-resolved="false"></xref>에 등록되어 설정 UI 등 외부에서 조회 가능합니다.

저장값이 없으면(HasStoredValue == false) 카메라의 인스펙터 기본 거리를 사용합니다.
씬에 하나만 배치하세요(TexturePerformanceService와 동일한 시스템 오브젝트에 두는 것을 권장).

```csharp
public class CameraDistancePreferenceService : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[CameraDistancePreferenceService](MultiplayerInfrastructure.Performance.CameraDistancePreferenceService.md)

## Properties

### <a id="MultiplayerInfrastructure_Performance_CameraDistancePreferenceService_CurrentDistance"></a> CurrentDistance

현재 서비스가 보유한 3인칭 카메라 거리 값입니다.

```csharp
public float CurrentDistance { get; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Performance_CameraDistancePreferenceService_HasStoredValue"></a> HasStoredValue

PlayerPrefs에 저장된 값이 존재하는지 여부입니다.

```csharp
public bool HasStoredValue { get; }
```

#### Property Value

 bool

## Methods

### <a id="MultiplayerInfrastructure_Performance_CameraDistancePreferenceService_GetDistanceRange_System_Single__System_Single__"></a> GetDistanceRange\(out float, out float\)

현재 카메라의 POV 거리 허용 범위를 반환합니다. 카메라가 없으면 기본 범위를 반환합니다.

```csharp
public void GetDistanceRange(out float min, out float max)
```

#### Parameters

`min` float

`max` float

### <a id="MultiplayerInfrastructure_Performance_CameraDistancePreferenceService_PersistDistance_System_Single_"></a> PersistDistance\(float\)

POV 거리만 저장합니다. 현재 카메라에는 적용하지 않으므로, 휠 조정 중 자동으로 전환된
1/3인칭 시점은 유지하면서 다음 실행에 사용할 거리만 별도로 보관할 수 있습니다.

```csharp
public void PersistDistance(float distance)
```

#### Parameters

`distance` float

### <a id="MultiplayerInfrastructure_Performance_CameraDistancePreferenceService_ReapplyToCamera"></a> ReapplyToCamera\(\)

저장된 값을 다시 불러와 카메라에 적용합니다. 카메라가 늦게 초기화된 경우 재적용에 사용합니다.

```csharp
public void ReapplyToCamera()
```

### <a id="MultiplayerInfrastructure_Performance_CameraDistancePreferenceService_ResolveEffectiveDistance"></a> ResolveEffectiveDistance\(\)

현재 활성 카메라의 거리를 기준으로 서비스 값을 동기화합니다.
저장값이 없을 때 설정 UI가 슬라이더 초기값을 카메라 현재값으로 맞추기 위해 사용합니다.

```csharp
public float ResolveEffectiveDistance()
```

#### Returns

 float

### <a id="MultiplayerInfrastructure_Performance_CameraDistancePreferenceService_SetDistance_System_Single_"></a> SetDistance\(float\)

3인칭 카메라 거리를 설정하고 즉시 카메라에 적용한 뒤 PlayerPrefs에 저장합니다.

```csharp
public void SetDistance(float distance)
```

#### Parameters

`distance` float

### <a id="MultiplayerInfrastructure_Performance_CameraDistancePreferenceService_OnDistanceChanged"></a> OnDistanceChanged

POV 거리가 변경되었을 때 새 거리 값이 전달됩니다.

```csharp
public event Action<float> OnDistanceChanged
```

#### Event Type

 Action<float\>

