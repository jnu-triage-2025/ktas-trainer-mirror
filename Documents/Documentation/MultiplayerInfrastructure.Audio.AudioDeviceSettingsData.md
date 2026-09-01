# <a id="MultiplayerInfrastructure_Audio_AudioDeviceSettingsData"></a> Class AudioDeviceSettingsData

Namespace: [MultiplayerInfrastructure.Audio](MultiplayerInfrastructure.Audio.md)  
Assembly: Assembly\-CSharp.dll  

오디오 장치 선택을 담는 직렬화 대상 설정값입니다.

그래픽 설정(<code>GraphicsSettingsData</code>)과 같은 규약을 씁니다.
<xref href="UnityEngine.JsonUtility" data-throw-if-not-resolved="false"></xref>로 직렬화해 <xref href="UnityEngine.PlayerPrefs" data-throw-if-not-resolved="false"></xref>에 문자열로 보관합니다.

장치 이름 필드는 표시용 캐시입니다. 저장 당시의 이름을 기억해 두었다가,
다음 실행에서 그 장치가 사라졌을 때 어떤 장치가 없어졌는지 안내에 쓰기 위한 값입니다.
실제 대조는 언제나 식별자로 합니다.

```csharp
[Serializable]
public class AudioDeviceSettingsData
```

#### Inheritance

object ← 
[AudioDeviceSettingsData](MultiplayerInfrastructure.Audio.AudioDeviceSettingsData.md)

## Fields

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceSettingsData_InputDeviceId"></a> InputDeviceId

입력 장치 식별자입니다. 빈 문자열이면 시스템 설정을 따릅니다.

```csharp
public string InputDeviceId
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceSettingsData_InputDeviceName"></a> InputDeviceName

저장 당시의 입력 장치 이름(표시용 캐시)입니다.

```csharp
public string InputDeviceName
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceSettingsData_OutputDeviceId"></a> OutputDeviceId

출력 장치 식별자입니다. 빈 문자열이면 시스템 설정을 따릅니다.

```csharp
public string OutputDeviceId
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceSettingsData_OutputDeviceName"></a> OutputDeviceName

저장 당시의 출력 장치 이름(표시용 캐시)입니다.

```csharp
public string OutputDeviceName
```

#### Field Value

 string

## Methods

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceSettingsData_Clone"></a> Clone\(\)

```csharp
public AudioDeviceSettingsData Clone()
```

#### Returns

 [AudioDeviceSettingsData](MultiplayerInfrastructure.Audio.AudioDeviceSettingsData.md)

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceSettingsData_GetDeviceId_MultiplayerInfrastructure_Audio_AudioDeviceKind_"></a> GetDeviceId\(AudioDeviceKind\)

```csharp
public string GetDeviceId(AudioDeviceKind kind)
```

#### Parameters

`kind` [AudioDeviceKind](MultiplayerInfrastructure.Audio.AudioDeviceKind.md)

#### Returns

 string

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceSettingsData_GetDeviceName_MultiplayerInfrastructure_Audio_AudioDeviceKind_"></a> GetDeviceName\(AudioDeviceKind\)

```csharp
public string GetDeviceName(AudioDeviceKind kind)
```

#### Parameters

`kind` [AudioDeviceKind](MultiplayerInfrastructure.Audio.AudioDeviceKind.md)

#### Returns

 string

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceSettingsData_Sanitize"></a> Sanitize\(\)

null 필드를 빈 문자열로 메우고 앞뒤 공백을 걷어냅니다.

```csharp
public void Sanitize()
```

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceSettingsData_SetDevice_MultiplayerInfrastructure_Audio_AudioDeviceKind_System_String_System_String_"></a> SetDevice\(AudioDeviceKind, string, string\)

```csharp
public void SetDevice(AudioDeviceKind kind, string deviceId, string deviceName)
```

#### Parameters

`kind` [AudioDeviceKind](MultiplayerInfrastructure.Audio.AudioDeviceKind.md)

`deviceId` string

`deviceName` string

