# <a id="MultiplayerInfrastructure_Audio_AudioDevicePreferenceService"></a> Class AudioDevicePreferenceService

Namespace: [MultiplayerInfrastructure.Audio](MultiplayerInfrastructure.Audio.md)  
Assembly: Assembly\-CSharp.dll  

오디오 출력/입력 장치 선택을 저장하고 되살리는 서비스입니다.

저장 방식은 다른 설정과 같습니다. <xref href="MultiplayerInfrastructure.Audio.AudioDeviceSettingsData" data-throw-if-not-resolved="false"></xref>를
<xref href="UnityEngine.JsonUtility" data-throw-if-not-resolved="false"></xref>로 직렬화해 <xref href="UnityEngine.PlayerPrefs" data-throw-if-not-resolved="false"></xref>에 넣습니다.

저장된 장치가 지금은 없을 때:
  - 실제로 쓰이는 값(<xref href="MultiplayerInfrastructure.Audio.AudioDevicePreferenceService.ResolveEffectiveDeviceId(MultiplayerInfrastructure.Audio.AudioDeviceKind)" data-throw-if-not-resolved="false"></xref>)은 곧바로 시스템 설정으로 돌아갑니다.
  - 저장값 자체를 지우는 것은 장치 목록을 제대로 읽었을 때만입니다. 목록 읽기가 통째로
    실패한 상황(권한 문제 등)에서 멀쩡한 사용자 선택을 날려버리지 않기 위해서입니다.

출력 장치는 <xref href="MultiplayerInfrastructure.Audio.AudioOutputRouting" data-throw-if-not-resolved="false"></xref>이 운영체제 쪽에서 경로를 돌려 줍니다.
Unity에 출력 장치를 고르는 API가 없어서 플랫폼별 우회에 기대므로, 지원하지 않는
플랫폼이나 실패한 경우에는 저장만 남고 재생은 시스템 설정을 따릅니다.
입력 장치는 <code>Microphone.Start</code>에 넘길 이름을 정하므로 그대로 반영됩니다.

씬에는 하나만 두세요(TexturePerformanceService와 같은 시스템 오브젝트를 권장).

```csharp
public class AudioDevicePreferenceService : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[AudioDevicePreferenceService](MultiplayerInfrastructure.Audio.AudioDevicePreferenceService.md)

## Properties

### <a id="MultiplayerInfrastructure_Audio_AudioDevicePreferenceService_CurrentSettings"></a> CurrentSettings

현재 설정의 사본입니다.

```csharp
public AudioDeviceSettingsData CurrentSettings { get; }
```

#### Property Value

 [AudioDeviceSettingsData](MultiplayerInfrastructure.Audio.AudioDeviceSettingsData.md)

### <a id="MultiplayerInfrastructure_Audio_AudioDevicePreferenceService_Instance"></a> Instance

등록되어 있으면 서비스 인스턴스를, 아니면 null을 돌려줍니다.

```csharp
public static AudioDevicePreferenceService Instance { get; }
```

#### Property Value

 [AudioDevicePreferenceService](MultiplayerInfrastructure.Audio.AudioDevicePreferenceService.md)

### <a id="MultiplayerInfrastructure_Audio_AudioDevicePreferenceService_IsOutputRestartPending"></a> IsOutputRestartPending

새 장치로 옮기는 일을 재생이 끝날 때까지 미뤄 둔 상태면 true입니다.
설정 화면이 "곧 옮긴다"고 안내하는 데 씁니다.

```csharp
public bool IsOutputRestartPending { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Audio_AudioDevicePreferenceService_IsOutputRoutingActive"></a> IsOutputRoutingActive

출력 경로를 실제로 옮기는 데 성공했는지입니다.
지원하지 않는 플랫폼이거나 비공개 API 호출이 실패하면 false로 남고,
설정 화면은 이 값을 보고 "저장만 되었다"는 안내를 띄웁니다.

```csharp
public bool IsOutputRoutingActive { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Audio_AudioDevicePreferenceService_IsOutputRoutingSupported"></a> IsOutputRoutingSupported

이 플랫폼에서 출력 경로를 바꿀 수 있으면 true입니다.

```csharp
public static bool IsOutputRoutingSupported { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Audio_AudioDevicePreferenceService_OutputRoutingFailureReason"></a> OutputRoutingFailureReason

출력 경로 변경이 실패했을 때의 사유입니다. 성공했거나 시도하지 않았으면 null입니다.

```csharp
public string OutputRoutingFailureReason { get; }
```

#### Property Value

 string

## Methods

### <a id="MultiplayerInfrastructure_Audio_AudioDevicePreferenceService_GetOrCreateInstance"></a> GetOrCreateInstance\(\)

장면에 배치된 서비스가 아직 로드되지 않았어도 설정 화면에서 바로 쓸 수 있게 합니다.

시작 화면에서는 SystemOverlayScene이 아직 추가 로드되지 않을 수 있습니다. 이때도
입력 장치 설정을 저장할 수 있도록, 필요한 경우 지속되는 런타임 서비스를 만듭니다.

```csharp
public static AudioDevicePreferenceService GetOrCreateInstance()
```

#### Returns

 [AudioDevicePreferenceService](MultiplayerInfrastructure.Audio.AudioDevicePreferenceService.md)

### <a id="MultiplayerInfrastructure_Audio_AudioDevicePreferenceService_LoadAndReconcile"></a> LoadAndReconcile\(\)

저장값을 읽고 지금 장치 목록과 대조합니다. Awake에서 자동으로 불립니다.

```csharp
public void LoadAndReconcile()
```

### <a id="MultiplayerInfrastructure_Audio_AudioDevicePreferenceService_RefreshDevices"></a> RefreshDevices\(\)

운영체제에 장치 목록을 다시 묻고, 사라진 장치를 가리키던 설정을 정리합니다.

```csharp
public void RefreshDevices()
```

### <a id="MultiplayerInfrastructure_Audio_AudioDevicePreferenceService_ResetToSystemDefault"></a> ResetToSystemDefault\(\)

시스템 설정으로 되돌립니다.

```csharp
public void ResetToSystemDefault()
```

### <a id="MultiplayerInfrastructure_Audio_AudioDevicePreferenceService_ResolveEffectiveDeviceId_MultiplayerInfrastructure_Audio_AudioDeviceKind_"></a> ResolveEffectiveDeviceId\(AudioDeviceKind\)

실제로 적용할 식별자입니다. 저장된 장치가 지금 목록에 없으면 시스템 설정을 뜻하는
빈 문자열이 나옵니다.

```csharp
public string ResolveEffectiveDeviceId(AudioDeviceKind kind)
```

#### Parameters

`kind` [AudioDeviceKind](MultiplayerInfrastructure.Audio.AudioDeviceKind.md)

#### Returns

 string

### <a id="MultiplayerInfrastructure_Audio_AudioDevicePreferenceService_ResolveInputDeviceName"></a> ResolveInputDeviceName\(\)

<code>Microphone.Start</code>에 넘길 장치 이름입니다.
시스템 설정을 따르는 경우 null이 나오며, 이는 운영체제 기본 마이크를 뜻합니다.

서비스가 씬에 없어도 동작하도록 저장값을 직접 읽는 경로를 함께 둡니다.
(마이크를 쓰는 쪽이 서비스보다 먼저 깨어나는 경우가 있습니다.)

```csharp
public static string ResolveInputDeviceName()
```

#### Returns

 string

### <a id="MultiplayerInfrastructure_Audio_AudioDevicePreferenceService_SetDevice_MultiplayerInfrastructure_Audio_AudioDeviceKind_System_String_"></a> SetDevice\(AudioDeviceKind, string\)

한쪽 방향의 장치를 고릅니다. 빈 식별자를 넘기면 시스템 설정을 따릅니다.

```csharp
public void SetDevice(AudioDeviceKind kind, string deviceId)
```

#### Parameters

`kind` [AudioDeviceKind](MultiplayerInfrastructure.Audio.AudioDeviceKind.md)

`deviceId` string

### <a id="MultiplayerInfrastructure_Audio_AudioDevicePreferenceService_SetSettings_MultiplayerInfrastructure_Audio_AudioDeviceSettingsData_"></a> SetSettings\(AudioDeviceSettingsData\)

설정 전체를 갈아끼우고 저장합니다.

```csharp
public void SetSettings(AudioDeviceSettingsData settings)
```

#### Parameters

`settings` [AudioDeviceSettingsData](MultiplayerInfrastructure.Audio.AudioDeviceSettingsData.md)

### <a id="MultiplayerInfrastructure_Audio_AudioDevicePreferenceService_InputDeviceChanged"></a> InputDeviceChanged

입력 장치 선택이 바뀌었을 때 발생합니다.

마이크를 쓰는 쪽은 서비스 인스턴스보다 먼저 만들어지기도 하고 씬을 넘나들기도 해서,
인스턴스를 붙잡지 않고도 구독할 수 있도록 정적 이벤트로 둡니다.
구독한 쪽은 반드시 해제까지 챙기세요.

```csharp
public static event Action InputDeviceChanged
```

#### Event Type

 Action

### <a id="MultiplayerInfrastructure_Audio_AudioDevicePreferenceService_OnSettingsChanged"></a> OnSettingsChanged

설정이 바뀔 때마다 최신 사본이 전달됩니다.

```csharp
public event Action<AudioDeviceSettingsData> OnSettingsChanged
```

#### Event Type

 Action<[AudioDeviceSettingsData](MultiplayerInfrastructure.Audio.AudioDeviceSettingsData.md)\>

