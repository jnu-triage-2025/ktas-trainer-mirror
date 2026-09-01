# <a id="MultiplayerInfrastructure_Audio_AudioEndpointPath"></a> Class AudioEndpointPath

Namespace: [MultiplayerInfrastructure.Audio](MultiplayerInfrastructure.Audio.md)  
Assembly: Assembly\-CSharp.dll  

Windows 오디오 엔드포인트 ID를 장치 인터페이스 경로로 바꿉니다.

앱별 기본 장치 지정 API는 <code>IMMDevice::GetId</code>가 주는 날것의 엔드포인트 ID가 아니라
아래 형태의 장치 인터페이스 경로를 받습니다.

  \\?\SWD#MMDEVAPI#{엔드포인트 ID}#{장치 인터페이스 GUID}

플랫폼 가드 밖에 두어 어느 플랫폼에서든 테스트할 수 있게 했습니다.

```csharp
public static class AudioEndpointPath
```

#### Inheritance

object ← 
[AudioEndpointPath](MultiplayerInfrastructure.Audio.AudioEndpointPath.md)

## Methods

### <a id="MultiplayerInfrastructure_Audio_AudioEndpointPath_ForWindowsEndpoint_System_String_MultiplayerInfrastructure_Audio_AudioDeviceKind_"></a> ForWindowsEndpoint\(string, AudioDeviceKind\)

엔드포인트 ID를 장치 인터페이스 경로로 감쌉니다.
빈 식별자(시스템 설정)면 빈 문자열을 돌려주므로, 부르는 쪽에서 "지정 해제"로 다루면 됩니다.

```csharp
public static string ForWindowsEndpoint(string endpointId, AudioDeviceKind kind)
```

#### Parameters

`endpointId` string

`kind` [AudioDeviceKind](MultiplayerInfrastructure.Audio.AudioDeviceKind.md)

#### Returns

 string

### <a id="MultiplayerInfrastructure_Audio_AudioEndpointPath_UnwrapWindowsEndpoint_System_String_"></a> UnwrapWindowsEndpoint\(string\)

장치 인터페이스 경로에서 다시 엔드포인트 ID만 벗겨냅니다.

```csharp
public static string UnwrapWindowsEndpoint(string devicePath)
```

#### Parameters

`devicePath` string

#### Returns

 string

