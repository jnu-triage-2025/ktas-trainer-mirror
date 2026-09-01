# <a id="MultiplayerInfrastructure_Audio_AudioDeviceDescriptor"></a> Class AudioDeviceDescriptor

Namespace: [MultiplayerInfrastructure.Audio](MultiplayerInfrastructure.Audio.md)  
Assembly: Assembly\-CSharp.dll  

운영체제가 인식하고 있는 오디오 장치 한 개를 나타냅니다.

<xref href="MultiplayerInfrastructure.Audio.AudioDeviceDescriptor.Id" data-throw-if-not-resolved="false"></xref>는 설정 저장에 쓰는 식별자입니다. 장치를 뽑았다가 다시 꽂거나 이름을 바꿔도
되도록 같은 값이 유지되도록, 플랫폼별로 아래 값을 사용합니다.
  - Windows 출력: WASAPI 엔드포인트 ID 문자열
  - macOS 출력: CoreAudio 장치 UID
  - 입력(공통): <xref href="UnityEngine.Microphone" data-throw-if-not-resolved="false"></xref>이 쓰는 장치 이름
    (Unity에 이름 말고 다른 손잡이가 없어서 이름을 그대로 식별자로 씁니다.)

빈 문자열 ID는 "특정 장치를 고르지 않음", 즉 시스템 설정을 따른다는 뜻으로 예약되어 있습니다.
<xref href="MultiplayerInfrastructure.Audio.AudioDeviceSelectionResolver.SystemDefaultId" data-throw-if-not-resolved="false"></xref>를 참고하세요.

```csharp
public sealed class AudioDeviceDescriptor
```

#### Inheritance

object ← 
[AudioDeviceDescriptor](MultiplayerInfrastructure.Audio.AudioDeviceDescriptor.md)

## Constructors

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceDescriptor__ctor_System_String_System_String_System_Boolean_"></a> AudioDeviceDescriptor\(string, string, bool\)

```csharp
public AudioDeviceDescriptor(string id, string displayName, bool isSystemDefault = false)
```

#### Parameters

`id` string

`displayName` string

`isSystemDefault` bool

## Properties

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceDescriptor_DisplayName"></a> DisplayName

설정 화면에 보여줄 장치 이름입니다.

```csharp
public string DisplayName { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceDescriptor_Id"></a> Id

설정 저장에 쓰는 장치 식별자입니다.

```csharp
public string Id { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceDescriptor_IsSystemDefault"></a> IsSystemDefault

운영체제가 현재 기본 장치로 쓰고 있으면 true입니다.

```csharp
public bool IsSystemDefault { get; }
```

#### Property Value

 bool

## Methods

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceDescriptor_Equals_MultiplayerInfrastructure_Audio_AudioDeviceDescriptor_"></a> Equals\(AudioDeviceDescriptor\)

```csharp
public bool Equals(AudioDeviceDescriptor other)
```

#### Parameters

`other` [AudioDeviceDescriptor](MultiplayerInfrastructure.Audio.AudioDeviceDescriptor.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceDescriptor_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` object

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceDescriptor_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 int

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceDescriptor_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 string

