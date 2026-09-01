# <a id="MultiplayerInfrastructure_Audio_MicrophoneAudioDeviceEnumerator"></a> Class MicrophoneAudioDeviceEnumerator

Namespace: [MultiplayerInfrastructure.Audio](MultiplayerInfrastructure.Audio.md)  
Assembly: Assembly\-CSharp.dll  

입력 장치를 <xref href="UnityEngine.Microphone.devices" data-throw-if-not-resolved="false"></xref>로 훑는 구현입니다.

입력만큼은 네이티브가 아니라 Unity 목록을 기준으로 삼습니다. 녹음을 시작할 때
<xref href="UnityEngine.Microphone.Start(System.String%2cSystem.Boolean%2cSystem.Int32%2cSystem.Int32)" data-throw-if-not-resolved="false"></xref>에 넘길 수 있는 값이 이 이름뿐이라, 여기서 얻은 이름을
그대로 식별자로 써야 설정값과 실제 녹음 장치가 어긋나지 않습니다.

기본 장치 판별은 네이티브 열거기가 알려준 기본 입력 장치 이름과 맞춰봅니다.
맞는 항목이 없으면 Unity가 첫 번째로 돌려준 장치를 기본으로 봅니다.
(<xref href="UnityEngine.Microphone.Start(System.String%2cSystem.Boolean%2cSystem.Int32%2cSystem.Int32)" data-throw-if-not-resolved="false"></xref>에 null을 넘겼을 때 잡히는 장치와 같습니다.)

```csharp
public sealed class MicrophoneAudioDeviceEnumerator : IAudioDeviceEnumerator
```

#### Inheritance

object ← 
[MicrophoneAudioDeviceEnumerator](MultiplayerInfrastructure.Audio.MicrophoneAudioDeviceEnumerator.md)

#### Implements

[IAudioDeviceEnumerator](MultiplayerInfrastructure.Audio.IAudioDeviceEnumerator.md)

## Constructors

### <a id="MultiplayerInfrastructure_Audio_MicrophoneAudioDeviceEnumerator__ctor_System_Func_System_String__"></a> MicrophoneAudioDeviceEnumerator\(Func<string\>\)

```csharp
public MicrophoneAudioDeviceEnumerator(Func<string> systemDefaultNameProvider = null)
```

#### Parameters

`systemDefaultNameProvider` Func<string\>

## Methods

### <a id="MultiplayerInfrastructure_Audio_MicrophoneAudioDeviceEnumerator_Enumerate_MultiplayerInfrastructure_Audio_AudioDeviceKind_"></a> Enumerate\(AudioDeviceKind\)

지금 쓸 수 있는 장치를 모두 돌려줍니다. 실패하면 빈 목록을 돌려주고 예외를 던지지 않습니다.
운영체제 기본 장치에는 <xref href="MultiplayerInfrastructure.Audio.AudioDeviceDescriptor.IsSystemDefault" data-throw-if-not-resolved="false"></xref>가 서 있습니다.

```csharp
public IReadOnlyList<AudioDeviceDescriptor> Enumerate(AudioDeviceKind kind)
```

#### Parameters

`kind` [AudioDeviceKind](MultiplayerInfrastructure.Audio.AudioDeviceKind.md)

#### Returns

 IReadOnlyList<[AudioDeviceDescriptor](MultiplayerInfrastructure.Audio.AudioDeviceDescriptor.md)\>

### <a id="MultiplayerInfrastructure_Audio_MicrophoneAudioDeviceEnumerator_IsSupported_MultiplayerInfrastructure_Audio_AudioDeviceKind_"></a> IsSupported\(AudioDeviceKind\)

이 구현이 해당 방향의 장치를 훑을 수 있으면 true입니다.

```csharp
public bool IsSupported(AudioDeviceKind kind)
```

#### Parameters

`kind` [AudioDeviceKind](MultiplayerInfrastructure.Audio.AudioDeviceKind.md)

#### Returns

 bool

