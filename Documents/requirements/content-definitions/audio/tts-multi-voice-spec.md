---
title: "TTS 다중 목소리 프로파일 (Multi-Voice Profile)"
domain: content-definitions
progress: "3-implemented"
status: active
updated: 2026-07-03
---

# TTS 다중 목소리 프로파일 (Multi-Voice Profile)

인게임 등장인물마다 다른 목소리를 사용하기 위한 기능입니다.  
`TTSService`에 목소리 프로파일을 등록하고, 시나리오 노드에서 프로파일 식별자를 지정하면  
해당 캐릭터의 대사가 지정된 목소리로 재생됩니다.

---

## 1. 개념

### 1.1 Voice Style 이란

Supertonic TTS 엔진은 10가지 사전 학습된 목소리 스타일을 제공합니다.  
각 스타일은 `StreamingAssets/TTS/Models/voice_styles/` 폴더에 JSON 파일로 저장됩니다.

| 파일명 | 성별 |
|--------|------|
| `F1` ~ `F5` | 여성 |
| `M1` ~ `M5` | 남성 |

### 1.2 Voice Profile 이란

Voice Profile은 **게임 내 캐릭터와 Voice Style을 연결하는 설정**입니다.  
개발자가 직접 정한 **식별자(Identifier)**와 사용할 Voice Style 파일명을 쌍으로 등록합니다.

```
Voice Profile 예시
  identifier: "doctor"       → VoiceStyleName: "M2"
  identifier: "patient"      → VoiceStyleName: "F3"
  identifier: "narrator"     → VoiceStyleName: "F1"
```

### 1.3 흐름 요약

```
시나리오 JSON                    TTSService Inspector          파일
────────────────────────────     ─────────────────────────     ──────────────
"ttsVoiceIdentifier": "doctor"  →  identifier: "doctor"    →   M2.json
                                    voiceStyleName: "M2"
```

지정하지 않으면(`null` 또는 필드 생략) TTSService의 기본 `voiceStyleName`(기본값 `F1`)으로 재생됩니다.

---

## 2. TTSService Inspector 설정

`TTSService` 컴포넌트의 인스펙터에서 **다중 목소리 프로파일** 배열을 편집합니다.

| 필드 | 타입 | 설명 |
|------|------|------|
| `VoiceIdentifier` | string | 시나리오 노드에서 참조할 고유 이름. 예: `"doctor"`, `"narrator"` |
| `VoiceStyleName` | string | 사용할 Voice Style 파일명. `F1`~`F5`, `M1`~`M5` 중 하나 |
| `Language` | string | 언어 코드. 비워 두면 TTSService 기본값(`ko`) 사용 |
| `Speed` | float | 발화 속도 배율. `0` 이하이면 TTSService 기본값(`1.05`) 사용 |
| `TotalStep` | int | Diffusion 스텝 수. `0` 이하이면 TTSService 기본값(`5`) 사용 |

> 인스펙터 세부 설정 방법은 [TTS 다중 목소리 설정 가이드](../../../working-guide/features/scenario/tts-voice-profile-setup-guide.md)를 참고하세요.

---

## 3. 시나리오 JSON 작성법

### 3.1 Dialogue / Choice 노드

```json
{
  "identifier": "D001",
  "nodeType": "Dialogue",
  "speakerName": "김의사",
  "dialogueContent": "기도 확보 후 정맥로를 확보하세요.",
  "playTTS": true,
  "ttsVoiceIdentifier": "doctor",
  "nextIdentifier": "D002"
}
```

### 3.2 Quiz 노드

```json
{
  "identifier": "Q001",
  "nodeType": "Quiz",
  "question": "쇼크 환자의 1순위 처치는?",
  "options": ["기도 확보", "지혈", "보온"],
  "correctIndex": 0,
  "feedbackCorrect": "맞습니다. 기도 확보가 최우선입니다.",
  "feedbackIncorrect": "틀렸습니다. 기도 확보가 먼저입니다.",
  "playTTS": true,
  "ttsVoiceIdentifier": "narrator",
  "nextIdentifier": "D002"
}
```

> Quiz 노드에서 `ttsVoiceIdentifier`를 지정하면 문제 텍스트(`question`)와  
> 정답/오답 피드백(`feedbackCorrect`, `feedbackIncorrect`) 모두 동일한 목소리로 재생됩니다.

### 3.3 PlayTTS 노드 (transcript 기반)

```json
{
  "identifier": "TTS001",
  "nodeType": "PlayTTS",
  "transcriptIdentifier": "tts-role-instruction",
  "variables": { "role": "응급의학과" },
  "waitUntilFinished": true,
  "ttsVoiceIdentifier": "dispatcher",
  "nextIdentifier": "D003"
}
```

### 3.4 필드 규칙

| 필드 | 값 | 동작 |
|------|----|------|
| `"ttsVoiceIdentifier": "doctor"` | 등록된 식별자 | 해당 프로파일의 목소리 사용 |
| `"ttsVoiceIdentifier": null` | null | 기본 목소리(fallback) 사용 |
| 필드 자체 생략 | — | 기본 목소리(fallback) 사용 |
| `"ttsVoiceIdentifier": "unknown"` | 미등록 식별자 | 기본 목소리(fallback)로 자동 대체 |
| `"playTTS": false` 또는 생략 | — | `ttsVoiceIdentifier` 무시, TTS 재생 안 함 |

---

## 4. Baked WAV 경로 구조

Voice Profile을 사용하면 사전 합성(bake) 파일의 저장 경로가 달라집니다.

| 목소리 설정 | baked 파일 경로 |
|-------------|----------------|
| 기본 목소리 (미지정) | `TTS/BakedInline/{scenario}/{nodeId}_{hash}.wav` |
| 프로파일 지정 | `TTS/BakedInline/{scenario}/{voiceIdentifier}/{nodeId}_{hash}.wav` |

bake는 **Tools > Text to Speech Service > Bake Scenario Inline Audio** 창에서 실행합니다.  
에디터 창에서 각 프로파일의 VoiceStyleName을 입력해야 올바른 목소리로 bake됩니다.

---

## 5. 주의사항

- 시나리오 JSON의 `ttsVoiceIdentifier`와 TTSService Inspector의 `VoiceIdentifier`는 **대소문자 구분** 없이 일치해야 합니다.
- Inspector에 없는 식별자를 사용하면 오류 없이 기본 목소리로 재생됩니다. 빈 목소리나 침묵이 아닌 폴백입니다.
- `playTTS: false`이거나 `playTTS` 필드가 없는 노드는 `ttsVoiceIdentifier`가 있어도 재생되지 않습니다.
- Voice Profile은 런타임에 동적으로 추가하거나 변경할 수 없습니다. Inspector에서만 설정 가능합니다.

---

## 6. 관련 파일

| 경로 | 역할 |
|------|------|
| `Assets/Modules/TextToSpeechService/src/Models/TTSVoiceProfile.cs` | 프로파일 데이터 모델 |
| `Assets/Modules/TextToSpeechService/src/Services/TTSService.cs` | 다중 프로파일 로딩·합성 서비스 |
| `Assets/Modules/MultiplayerInfrastructure/Scripts/TTS/ScenarioTTSService.cs` | 시나리오 통합 파사드 |
| `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/ScenarioController.cs` | 노드 실행 시 voiceIdentifier 전달 |
| `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/SerializeSupport/ScenarioTTSBakeScanner.cs` | voice별 bake 경로 관리 |
| `Assets/Modules/MultiplayerInfrastructure/Editor/Scenario/ScenarioInlineAudioBakerWindow.cs` | 에디터 bake 도구 |

---

## 7. 관련 문서

- [TTS 스크립트 (Transcripts) 가이드](./tts-transcripts-spec.md) — transcript 기반 PlayTTS 노드 작성
- [TTS 다중 목소리 설정 가이드](../../../working-guide/features/scenario/tts-voice-profile-setup-guide.md) — Inspector 설정 및 bake 절차 (운영자용)
