# <a id="MultiplayerInfrastructure_Audio_AudioVolumePreferenceService"></a> Class AudioVolumePreferenceService

Namespace: [MultiplayerInfrastructure.Audio](MultiplayerInfrastructure.Audio.md)  
Assembly: Assembly\-CSharp.dll  

전체 사운드 볼륨을 적용하고 저장하는 서비스입니다.

```csharp
public class AudioVolumePreferenceService : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[AudioVolumePreferenceService](MultiplayerInfrastructure.Audio.AudioVolumePreferenceService.md)

## Properties

### <a id="MultiplayerInfrastructure_Audio_AudioVolumePreferenceService_CurrentVolume"></a> CurrentVolume

현재 전체 사운드 볼륨입니다. 0은 음소거이고 1은 최대 볼륨입니다.

```csharp
public float CurrentVolume { get; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Audio_AudioVolumePreferenceService_Instance"></a> Instance

```csharp
public static AudioVolumePreferenceService Instance { get; }
```

#### Property Value

 [AudioVolumePreferenceService](MultiplayerInfrastructure.Audio.AudioVolumePreferenceService.md)

## Methods

### <a id="MultiplayerInfrastructure_Audio_AudioVolumePreferenceService_GetOrCreateInstance"></a> GetOrCreateInstance\(\)

서비스가 아직 로드되지 않은 시작 화면에서도 볼륨을 적용합니다.

```csharp
public static AudioVolumePreferenceService GetOrCreateInstance()
```

#### Returns

 [AudioVolumePreferenceService](MultiplayerInfrastructure.Audio.AudioVolumePreferenceService.md)

### <a id="MultiplayerInfrastructure_Audio_AudioVolumePreferenceService_LoadAndApply"></a> LoadAndApply\(\)

저장된 전체 사운드 볼륨을 읽어 즉시 적용합니다.

```csharp
public void LoadAndApply()
```

### <a id="MultiplayerInfrastructure_Audio_AudioVolumePreferenceService_SetVolume_System_Single_"></a> SetVolume\(float\)

전체 사운드 볼륨을 즉시 적용하고 저장합니다.

```csharp
public void SetVolume(float volume)
```

#### Parameters

`volume` float

### <a id="MultiplayerInfrastructure_Audio_AudioVolumePreferenceService_OnVolumeChanged"></a> OnVolumeChanged

볼륨이 변경되었을 때 새 값(0~1)을 알립니다.

```csharp
public event Action<float> OnVolumeChanged
```

#### Event Type

 Action<float\>

