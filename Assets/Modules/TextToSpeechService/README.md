# TextToSpeechService

Supertonic TTS 엔진을 기반으로 한 온디바이스 음성 합성 모듈입니다.  
ONNX Runtime(CPU)으로 동작하며 네트워크 연결 없이 로컬에서 합성합니다.

지원 언어: `ko`, `en`, `es`, `pt`, `fr` / 샘플레이트: 44,100 Hz mono

## 음성 세그먼트 구조

`basetext` 의 `{variable-key}` 자리표시자를 기준으로 **Static**(고정 텍스트)과 **Dynamic**(변수 텍스트)으로 분해됩니다.

- **Static** — 에디터에서 사전 합성(bake) 가능. bake 파일 없으면 런타임 합성.
- **Dynamic** — 항상 런타임 합성. 기본값은 `variables` 맵, 재생 시 `overrideVariables`로 교체 가능.

```json
[
  {
    "identifier": "triage-move-patient",
    "basetext": "{patient-name} 환자를 {destination}으로 이동해야 합니다.",
    "variables": {
      "patient-name": "환자",
      "destination": "처치실"
    }
  }
]
```

## 다중 목소리 프로파일 (Multi-Voice Profile)

`TTSService` Inspector의 **"다중 목소리 프로파일"** 배열에 캐릭터별 프로파일을 등록하면  
시나리오 노드에서 `ttsVoiceIdentifier` 필드로 목소리를 교체할 수 있습니다.

```
Voice Profiles 예시
  [0]  VoiceIdentifier: "narrator"   VoiceStyleName: "F1"
  [1]  VoiceIdentifier: "doctor"     VoiceStyleName: "M2"
  [2]  VoiceIdentifier: "patient"    VoiceStyleName: "F3"
```

사용 가능한 Voice Style: `F1`~`F5` (여성), `M1`~`M5` (남성) — 10종.  
`ttsVoiceIdentifier`를 지정하지 않으면 기본 `voiceStyleName`으로 재생됩니다.

자세한 내용은 [TTS 다중 목소리 프로파일 요구사항](../../Documents/requirements/content-definitions/audio/tts-multi-voice-spec.md)을 참고하세요.

## 주요 경로

| 경로 | 역할 |
|------|------|
| `Assets/StreamingAssets/TTS/Models/onnx/` | ONNX 모델 파일 4종 |
| `Assets/StreamingAssets/TTS/Models/voice_styles/` | Voice Style JSON 10종 |
| `Assets/StreamingAssets/TTS/Baked/` | transcript 기반 Static 세그먼트 사전 합성 WAV |
| `Assets/StreamingAssets/TTS/BakedInline/` | 시나리오 인라인 텍스트 사전 합성 WAV |
| `Assets/StreamingAssets/transcripts.json` | PlayTTS 노드용 스크립트 데이터 |

## 관련 문서

- [TTS 스크립트 가이드](../../Documents/requirements/content-definitions/audio/tts-transcripts-spec.md)
- [TTS 다중 목소리 프로파일](../../Documents/requirements/content-definitions/audio/tts-multi-voice-spec.md)
- [TTS 다중 목소리 설정 가이드 (운영자용)](../../Documents/working-guide/features/scenario/tts-voice-profile-setup-guide.md)
