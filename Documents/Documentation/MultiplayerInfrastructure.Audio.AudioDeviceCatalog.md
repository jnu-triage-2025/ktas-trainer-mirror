# <a id="MultiplayerInfrastructure_Audio_AudioDeviceCatalog"></a> Class AudioDeviceCatalog

Namespace: [MultiplayerInfrastructure.Audio](MultiplayerInfrastructure.Audio.md)  
Assembly: Assembly\-CSharp.dll  

지금 이 기기가 인식하고 있는 오디오 장치 목록을 모아 두는 곳입니다.

플랫폼별 열거기를 골라 붙이고 결과를 캐시합니다. 장치를 훑는 일은 운영체제 호출이라
값이 싸지 않으므로, 설정 창을 열거나 새로 고침을 누를 때만 <xref href="MultiplayerInfrastructure.Audio.AudioDeviceCatalog.Refresh" data-throw-if-not-resolved="false"></xref>가 돕니다.

출력 방향은 Windows(WASAPI)와 macOS(CoreAudio)에서만 훑을 수 있습니다.
입력 방향은 <xref href="MultiplayerInfrastructure.Audio.MicrophoneAudioDeviceEnumerator" data-throw-if-not-resolved="false"></xref>가 모든 플랫폼에서 맡습니다.

```csharp
public static class AudioDeviceCatalog
```

#### Inheritance

object ← 
[AudioDeviceCatalog](MultiplayerInfrastructure.Audio.AudioDeviceCatalog.md)

## Methods

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceCatalog_GetDevices_MultiplayerInfrastructure_Audio_AudioDeviceKind_"></a> GetDevices\(AudioDeviceKind\)

마지막으로 읽은 장치 목록입니다. 아직 한 번도 읽지 않았다면 여기서 읽어 옵니다.

```csharp
public static IReadOnlyList<AudioDeviceDescriptor> GetDevices(AudioDeviceKind kind)
```

#### Parameters

`kind` [AudioDeviceKind](MultiplayerInfrastructure.Audio.AudioDeviceKind.md)

#### Returns

 IReadOnlyList<[AudioDeviceDescriptor](MultiplayerInfrastructure.Audio.AudioDeviceDescriptor.md)\>

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceCatalog_GetSystemDefault_MultiplayerInfrastructure_Audio_AudioDeviceKind_"></a> GetSystemDefault\(AudioDeviceKind\)

운영체제가 현재 쓰고 있는 기본 장치입니다. 알아내지 못했으면 null입니다.

```csharp
public static AudioDeviceDescriptor GetSystemDefault(AudioDeviceKind kind)
```

#### Parameters

`kind` [AudioDeviceKind](MultiplayerInfrastructure.Audio.AudioDeviceKind.md)

#### Returns

 [AudioDeviceDescriptor](MultiplayerInfrastructure.Audio.AudioDeviceDescriptor.md)

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceCatalog_IsSupported_MultiplayerInfrastructure_Audio_AudioDeviceKind_"></a> IsSupported\(AudioDeviceKind\)

해당 방향의 장치를 훑을 수 있는 플랫폼이면 true입니다.

```csharp
public static bool IsSupported(AudioDeviceKind kind)
```

#### Parameters

`kind` [AudioDeviceKind](MultiplayerInfrastructure.Audio.AudioDeviceKind.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceCatalog_Refresh"></a> Refresh\(\)

운영체제에 다시 물어 목록을 갱신합니다.

```csharp
public static void Refresh()
```

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceCatalog_Refreshed"></a> Refreshed

장치 목록이 새로 읽힐 때마다 발생합니다.

```csharp
public static event Action Refreshed
```

#### Event Type

 Action

