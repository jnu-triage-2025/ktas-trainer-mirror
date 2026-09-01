# <a id="MultiplayerInfrastructure_Audio"></a> Namespace MultiplayerInfrastructure.Audio

### Namespaces

 [MultiplayerInfrastructure.Audio.Native](MultiplayerInfrastructure.Audio.Native.md)

### Classes

 [AudioDeviceCatalog](MultiplayerInfrastructure.Audio.AudioDeviceCatalog.md)

지금 이 기기가 인식하고 있는 오디오 장치 목록을 모아 두는 곳입니다.

플랫폼별 열거기를 골라 붙이고 결과를 캐시합니다. 장치를 훑는 일은 운영체제 호출이라
값이 싸지 않으므로, 설정 창을 열거나 새로 고침을 누를 때만 <xref href="MultiplayerInfrastructure.Audio.AudioDeviceCatalog.Refresh" data-throw-if-not-resolved="false"></xref>가 돕니다.

출력 방향은 Windows(WASAPI)와 macOS(CoreAudio)에서만 훑을 수 있습니다.
입력 방향은 <xref href="MultiplayerInfrastructure.Audio.MicrophoneAudioDeviceEnumerator" data-throw-if-not-resolved="false"></xref>가 모든 플랫폼에서 맡습니다.

 [AudioDeviceDescriptor](MultiplayerInfrastructure.Audio.AudioDeviceDescriptor.md)

운영체제가 인식하고 있는 오디오 장치 한 개를 나타냅니다.

<xref href="MultiplayerInfrastructure.Audio.AudioDeviceDescriptor.Id" data-throw-if-not-resolved="false"></xref>는 설정 저장에 쓰는 식별자입니다. 장치를 뽑았다가 다시 꽂거나 이름을 바꿔도
되도록 같은 값이 유지되도록, 플랫폼별로 아래 값을 사용합니다.
  - Windows 출력: WASAPI 엔드포인트 ID 문자열
  - macOS 출력: CoreAudio 장치 UID
  - 입력(공통): <xref href="UnityEngine.Microphone" data-throw-if-not-resolved="false"></xref>이 쓰는 장치 이름
    (Unity에 이름 말고 다른 손잡이가 없어서 이름을 그대로 식별자로 씁니다.)

빈 문자열 ID는 "특정 장치를 고르지 않음", 즉 시스템 설정을 따른다는 뜻으로 예약되어 있습니다.
<xref href="MultiplayerInfrastructure.Audio.AudioDeviceSelectionResolver.SystemDefaultId" data-throw-if-not-resolved="false"></xref>를 참고하세요.

 [AudioDevicePreferenceService](MultiplayerInfrastructure.Audio.AudioDevicePreferenceService.md)

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

 [AudioDeviceSelectionResolver](MultiplayerInfrastructure.Audio.AudioDeviceSelectionResolver.md)

저장된 장치 선택값을 현재 장치 목록과 대조하는 순수 함수 모음입니다.

Unity에 기대지 않으므로 에디터 테스트에서 그대로 검증할 수 있습니다.

 [AudioDeviceSettingsData](MultiplayerInfrastructure.Audio.AudioDeviceSettingsData.md)

오디오 장치 선택을 담는 직렬화 대상 설정값입니다.

그래픽 설정(<code>GraphicsSettingsData</code>)과 같은 규약을 씁니다.
<xref href="UnityEngine.JsonUtility" data-throw-if-not-resolved="false"></xref>로 직렬화해 <xref href="UnityEngine.PlayerPrefs" data-throw-if-not-resolved="false"></xref>에 문자열로 보관합니다.

장치 이름 필드는 표시용 캐시입니다. 저장 당시의 이름을 기억해 두었다가,
다음 실행에서 그 장치가 사라졌을 때 어떤 장치가 없어졌는지 안내에 쓰기 위한 값입니다.
실제 대조는 언제나 식별자로 합니다.

 [AudioEndpointPath](MultiplayerInfrastructure.Audio.AudioEndpointPath.md)

Windows 오디오 엔드포인트 ID를 장치 인터페이스 경로로 바꿉니다.

앱별 기본 장치 지정 API는 <code>IMMDevice::GetId</code>가 주는 날것의 엔드포인트 ID가 아니라
아래 형태의 장치 인터페이스 경로를 받습니다.

  \\?\SWD#MMDEVAPI#{엔드포인트 ID}#{장치 인터페이스 GUID}

플랫폼 가드 밖에 두어 어느 플랫폼에서든 테스트할 수 있게 했습니다.

 [AudioOutputRouting](MultiplayerInfrastructure.Audio.AudioOutputRouting.md)

출력 장치 지정을 플랫폼 구현에 넘기고, 오디오 엔진을 다시 여는 수단을 제공합니다.

두 가지 일을 일부러 갈라 두었습니다.
  - <xref href="MultiplayerInfrastructure.Audio.AudioOutputRouting.Apply(System.String%2cSystem.String%40)" data-throw-if-not-resolved="false"></xref>는 운영체제에 "앞으로는 이 장치로" 라고 알리기만 합니다. 부작용이 없습니다.
  - <xref href="MultiplayerInfrastructure.Audio.AudioOutputRouting.RestartAudioEngine" data-throw-if-not-resolved="false"></xref>은 이미 열려 있는 스트림을 새 장치로 옮깁니다. 이쪽이 파괴적입니다.

<xref href="UnityEngine.AudioSettings.Reset(UnityEngine.AudioConfiguration)" data-throw-if-not-resolved="false"></xref>은 재생 중인 소리를 모두 끊고, <code>Microphone.Start</code>로
만든 <xref href="UnityEngine.AudioClip" data-throw-if-not-resolved="false"></xref>까지 무효로 만듭니다. 그래서 언제 부를지는 부르는 쪽이 정합니다.
게임을 켤 때는 부르지 않습니다. Windows는 앱별 지정을 레지스트리에 남겨 두었다가 프로세스가
시작될 때 이미 반영해 주므로, 시작 시점에 엔진을 흔들 이유가 없습니다.

 [AudioVolumePreferenceService](MultiplayerInfrastructure.Audio.AudioVolumePreferenceService.md)

전체 사운드 볼륨을 적용하고 저장하는 서비스입니다.

 [MicrophoneAudioDeviceEnumerator](MultiplayerInfrastructure.Audio.MicrophoneAudioDeviceEnumerator.md)

입력 장치를 <xref href="UnityEngine.Microphone.devices" data-throw-if-not-resolved="false"></xref>로 훑는 구현입니다.

입력만큼은 네이티브가 아니라 Unity 목록을 기준으로 삼습니다. 녹음을 시작할 때
<xref href="UnityEngine.Microphone.Start(System.String%2cSystem.Boolean%2cSystem.Int32%2cSystem.Int32)" data-throw-if-not-resolved="false"></xref>에 넘길 수 있는 값이 이 이름뿐이라, 여기서 얻은 이름을
그대로 식별자로 써야 설정값과 실제 녹음 장치가 어긋나지 않습니다.

기본 장치 판별은 네이티브 열거기가 알려준 기본 입력 장치 이름과 맞춰봅니다.
맞는 항목이 없으면 Unity가 첫 번째로 돌려준 장치를 기본으로 봅니다.
(<xref href="UnityEngine.Microphone.Start(System.String%2cSystem.Boolean%2cSystem.Int32%2cSystem.Int32)" data-throw-if-not-resolved="false"></xref>에 null을 넘겼을 때 잡히는 장치와 같습니다.)

 [NetworkSoundEmitter](MultiplayerInfrastructure.Audio.NetworkSoundEmitter.md)

월드 오브젝트에 붙여 효과음을 자신의 현재 위치에서 서버 전역 재생하도록 하는 진입점입니다.
애니메이션 이벤트, 상호작용 코드, UnityEvent에서 <xref href="MultiplayerInfrastructure.Audio.NetworkSoundEmitter.Play" data-throw-if-not-resolved="false"></xref>를 호출할 수 있습니다.

 [SoundService](MultiplayerInfrastructure.Audio.SoundService.md)

지정한 위치에서 효과음을 재생합니다. 네트워크 동기화 없이 호출한 클라이언트의 로컬에서만 재생됩니다.

### Interfaces

 [IAudioDeviceEnumerator](MultiplayerInfrastructure.Audio.IAudioDeviceEnumerator.md)

운영체제에 물어 오디오 장치 목록을 받아오는 계층입니다.

Unity에는 출력 장치를 훑는 API가 없어서 플랫폼마다 네이티브 호출로 구현합니다.
(Windows: WASAPI / macOS: CoreAudio) 지원하지 않는 플랫폼에서는
<xref href="MultiplayerInfrastructure.Audio.IAudioDeviceEnumerator.IsSupported(MultiplayerInfrastructure.Audio.AudioDeviceKind)" data-throw-if-not-resolved="false"></xref>가 false를 돌려주고, 설정 화면은 시스템 설정 항목만 보여줍니다.

 [IAudioOutputRouter](MultiplayerInfrastructure.Audio.IAudioOutputRouter.md)

게임 소리가 나갈 출력 장치를 실제로 바꾸는 계층입니다.

Unity는 출력 경로를 고르는 API를 열어두지 않았고, FMOD도 UnityPlayer에 정적으로 묶여 있어
손댈 수 없습니다. 그래서 운영체제 쪽에서 우회합니다. 지금은 Windows만 구현되어 있습니다.

### Enums

 [AudioDeviceKind](MultiplayerInfrastructure.Audio.AudioDeviceKind.md)

오디오 장치의 방향 구분입니다.

