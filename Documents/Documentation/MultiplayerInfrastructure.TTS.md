# <a id="MultiplayerInfrastructure_TTS"></a> Namespace MultiplayerInfrastructure.TTS

### Classes

 [ScenarioTTSService](MultiplayerInfrastructure.TTS.ScenarioTTSService.md)

MultiplayerInfrastructure 측 TTS 재생 서비스.

TextToSpeechService 모듈(<xref href="TextToSpeechService.TTSService" data-throw-if-not-resolved="false"></xref>)에 의존하여
시나리오 흐름에서의 음성 재생을 담당한다. TextToSpeechService 모듈은 순수 합성/파일 처리만
책임지고, 게임 통합(컴포넌트 배치·AudioSource 관리·시나리오 연동)은 이 서비스가 담당한다.

배치:
  · 이 컴포넌트를 GameObject에 붙이면 <xref href="TextToSpeechService.TTSService" data-throw-if-not-resolved="false"></xref> 와
    <xref href="MultiplayerInfrastructure.TTS.ScenarioTTSService.AudioSource" data-throw-if-not-resolved="false"></xref> 가 자동으로 보장(없으면 추가)된다.
  · <xref href="MultiplayerInfrastructure.Scenario.ScenarioController" data-throw-if-not-resolved="false"></xref> 의
    _ttsService / _ttsAudioSource 필드에 각각 이 컴포넌트와 <xref href="MultiplayerInfrastructure.TTS.ScenarioTTSService.AudioSource" data-throw-if-not-resolved="false"></xref> 를 연결한다.

