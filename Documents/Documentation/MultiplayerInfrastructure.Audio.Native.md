# <a id="MultiplayerInfrastructure_Audio_Native"></a> Namespace MultiplayerInfrastructure.Audio.Native

### Classes

 [MacAudioDeviceEnumerator](MultiplayerInfrastructure.Audio.Native.MacAudioDeviceEnumerator.md)

CoreAudio(AudioObject) 속성 조회로 macOS의 오디오 장치를 훑습니다.

식별자는 장치 UID(<code>kAudioDevicePropertyDeviceUID</code>)입니다. AudioDeviceID는 재부팅이나
재연결마다 달라지지만 UID는 유지되므로 설정 저장에 알맞습니다.

macOS는 입출력을 한 장치가 겸하는 경우가 있어(예: USB 헤드셋),
요청한 방향에 채널이 실제로 있는 장치만 골라 돌려줍니다.

