# <a id="MultiplayerInfrastructure_Audio_AudioOutputRouting"></a> Class AudioOutputRouting

Namespace: [MultiplayerInfrastructure.Audio](MultiplayerInfrastructure.Audio.md)  
Assembly: Assembly\-CSharp.dll  

출력 장치 지정을 플랫폼 구현에 넘기고, 오디오 엔진을 다시 여는 수단을 제공합니다.

두 가지 일을 일부러 갈라 두었습니다.
  - <xref href="MultiplayerInfrastructure.Audio.AudioOutputRouting.Apply(System.String%2cSystem.String%40)" data-throw-if-not-resolved="false"></xref>는 운영체제에 "앞으로는 이 장치로" 라고 알리기만 합니다. 부작용이 없습니다.
  - <xref href="MultiplayerInfrastructure.Audio.AudioOutputRouting.RestartAudioEngine" data-throw-if-not-resolved="false"></xref>은 이미 열려 있는 스트림을 새 장치로 옮깁니다. 이쪽이 파괴적입니다.

<xref href="UnityEngine.AudioSettings.Reset(UnityEngine.AudioConfiguration)" data-throw-if-not-resolved="false"></xref>은 재생 중인 소리를 모두 끊고, <code>Microphone.Start</code>로
만든 <xref href="UnityEngine.AudioClip" data-throw-if-not-resolved="false"></xref>까지 무효로 만듭니다. 그래서 언제 부를지는 부르는 쪽이 정합니다.
게임을 켤 때는 부르지 않습니다. Windows는 앱별 지정을 레지스트리에 남겨 두었다가 프로세스가
시작될 때 이미 반영해 주므로, 시작 시점에 엔진을 흔들 이유가 없습니다.

```csharp
public static class AudioOutputRouting
```

#### Inheritance

object ← 
[AudioOutputRouting](MultiplayerInfrastructure.Audio.AudioOutputRouting.md)

## Properties

### <a id="MultiplayerInfrastructure_Audio_AudioOutputRouting_IsSupported"></a> IsSupported

이 플랫폼에서 출력 경로를 실제로 바꿀 수 있으면 true입니다.

```csharp
public static bool IsSupported { get; }
```

#### Property Value

 bool

## Methods

### <a id="MultiplayerInfrastructure_Audio_AudioOutputRouting_Apply_System_String_System_String__"></a> Apply\(string, out string\)

앞으로 열릴 출력 스트림이 나갈 장치를 지정합니다. 이미 흐르고 있는 소리는 건드리지 않으므로,
지금 재생 중인 것까지 옮기려면 <xref href="MultiplayerInfrastructure.Audio.AudioOutputRouting.RestartAudioEngine" data-throw-if-not-resolved="false"></xref>을 따로 부르세요.

```csharp
public static bool Apply(string deviceId, out string failureReason)
```

#### Parameters

`deviceId` string

출력 장치 식별자. 빈 문자열이면 지정을 풀고 시스템 설정을 따릅니다.

`failureReason` string

실패 사유. 성공하거나 지원하지 않는 플랫폼이면 null입니다.

#### Returns

 bool

실제로 경로를 바꿨으면 true.

### <a id="MultiplayerInfrastructure_Audio_AudioOutputRouting_IsAnyAudioPlaying"></a> IsAnyAudioPlaying\(\)

지금 소리를 내고 있는 <xref href="UnityEngine.AudioSource" data-throw-if-not-resolved="false"></xref>가 하나라도 있으면 true입니다.
엔진을 다시 열어도 되는 순간인지 가늠하는 데 씁니다.

```csharp
public static bool IsAnyAudioPlaying()
```

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Audio_AudioOutputRouting_RestartAudioEngine"></a> RestartAudioEngine\(\)

Unity 오디오 엔진이 출력 장치를 다시 열게 합니다.
설정을 바꾸지 않고 현재 구성 그대로 다시 여는 것이 목적입니다.

<b>재생 중인 소리가 모두 끊기고 마이크 클립도 무효가 됩니다.</b>
소리가 잦아든 뒤에 부르세요.

```csharp
public static bool RestartAudioEngine()
```

#### Returns

 bool

