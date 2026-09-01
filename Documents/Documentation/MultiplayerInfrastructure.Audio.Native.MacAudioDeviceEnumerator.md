# <a id="MultiplayerInfrastructure_Audio_Native_MacAudioDeviceEnumerator"></a> Class MacAudioDeviceEnumerator

Namespace: [MultiplayerInfrastructure.Audio.Native](MultiplayerInfrastructure.Audio.Native.md)  
Assembly: Assembly\-CSharp.dll  

CoreAudio(AudioObject) 속성 조회로 macOS의 오디오 장치를 훑습니다.

식별자는 장치 UID(<code>kAudioDevicePropertyDeviceUID</code>)입니다. AudioDeviceID는 재부팅이나
재연결마다 달라지지만 UID는 유지되므로 설정 저장에 알맞습니다.

macOS는 입출력을 한 장치가 겸하는 경우가 있어(예: USB 헤드셋),
요청한 방향에 채널이 실제로 있는 장치만 골라 돌려줍니다.

```csharp
public sealed class MacAudioDeviceEnumerator : IAudioDeviceEnumerator
```

#### Inheritance

object ← 
[MacAudioDeviceEnumerator](MultiplayerInfrastructure.Audio.Native.MacAudioDeviceEnumerator.md)

#### Implements

[IAudioDeviceEnumerator](MultiplayerInfrastructure.Audio.IAudioDeviceEnumerator.md)

## Methods

### <a id="MultiplayerInfrastructure_Audio_Native_MacAudioDeviceEnumerator_Enumerate_MultiplayerInfrastructure_Audio_AudioDeviceKind_"></a> Enumerate\(AudioDeviceKind\)

지금 쓸 수 있는 장치를 모두 돌려줍니다. 실패하면 빈 목록을 돌려주고 예외를 던지지 않습니다.
운영체제 기본 장치에는 <xref href="MultiplayerInfrastructure.Audio.AudioDeviceDescriptor.IsSystemDefault" data-throw-if-not-resolved="false"></xref>가 서 있습니다.

```csharp
public IReadOnlyList<AudioDeviceDescriptor> Enumerate(AudioDeviceKind kind)
```

#### Parameters

`kind` [AudioDeviceKind](MultiplayerInfrastructure.Audio.AudioDeviceKind.md)

#### Returns

 IReadOnlyList<[AudioDeviceDescriptor](MultiplayerInfrastructure.Audio.AudioDeviceDescriptor.md)\>

### <a id="MultiplayerInfrastructure_Audio_Native_MacAudioDeviceEnumerator_IsSupported_MultiplayerInfrastructure_Audio_AudioDeviceKind_"></a> IsSupported\(AudioDeviceKind\)

이 구현이 해당 방향의 장치를 훑을 수 있으면 true입니다.

```csharp
public bool IsSupported(AudioDeviceKind kind)
```

#### Parameters

`kind` [AudioDeviceKind](MultiplayerInfrastructure.Audio.AudioDeviceKind.md)

#### Returns

 bool

