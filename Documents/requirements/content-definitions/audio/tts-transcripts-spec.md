---
title: "TTS 스크립트 (Transcripts) 가이드"
doc_type: requirement
status: active
updated: 2026-04-14
---

# TTS 스크립트 (Transcripts) 가이드

TTS 음성을 재생하려면 **Transcript JSON** 파일에 발화 내용을 정의해야 합니다.  
이 문서는 JSON 포맷, 세그먼트 파싱 규칙, 사용 절차를 설명합니다.

---

## 파일 위치

| 파일 | 역할 |
|---|---|
| `Assets/StreamingAssets/transcripts.json` | 런타임에 TTSService가 읽는 스크립트 데이터 |
| `Assets/Modules/TextToSpeechService/Resources/Transcripts.schema.json` | JSON Schema (VS Code 등에서 자동완성·검증용) |

`.vscode/settings.json`의 `json.schemas` 설정을 통해 아래 경로의 JSON 파일에 스키마가 자동 연결됩니다.

- `Assets/StreamingAssets/transcripts.json`
- `Assets/**/Resources/SpeechTranscripts/*.json`

별도 설정 없이 VS Code에서 해당 파일을 열면 자동완성과 유효성 검사가 동작합니다.

---

## JSON 포맷

파일 전체는 `SpeechTranscript` 객체의 **배열** 입니다.

```json
[
  {
    "identifier": "intro-welcome",
    "basetext": "KTAS 중증도 분류 훈련에 오신 것을 환영합니다.",
    "variables": {}
  },
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

### 필드 설명

| 필드 | 타입 | 필수 | 설명 |
|---|---|---|---|
| `identifier` | `string` | ✅ | C# 코드에서 참조하는 고유 ID. `소문자-하이픈` 형식 권장. 전체 파일에서 유일해야 함. |
| `basetext` | `string` | ✅ | 발화할 텍스트. `{variable-key}` 형식의 자리표시자를 포함할 수 있음. |
| `variables` | `object` | ❌ | 각 `{key}`의 기본 발화 텍스트 맵. 런타임에 오버라이드 가능. |

---

## 세그먼트 파싱 규칙

`basetext`는 `TranscriptParser`에 의해 **Static**과 **Dynamic** 세그먼트 목록으로 분해됩니다.

| 세그먼트 종류 | 내용 | TTS 처리 방식 |
|---|---|---|
| `Static` | `{...}` 이 아닌 텍스트 부분 | 에디터에서 사전 합성(bake) 가능. bake 파일이 없으면 런타임 합성. |
| `Dynamic` | `{variable-key}` 자리표시자 | 항상 런타임 합성. 값은 `variables` 기본값 또는 `overrideVariables`로 결정. |

### 파싱 예시

```
basetext = "{patient-name} 환자를 {destination}으로 이동해야 합니다."

결과:
  [0] Dynamic  → key: "patient-name"
  [1] Static   → text: "환자를"
  [2] Dynamic  → key: "destination"
  [3] Static   → text: "으로 이동해야 합니다."
```

```
basetext = "KTAS 중증도 분류 훈련에 오신 것을 환영합니다."

결과:
  [0] Static   → text: "KTAS 중증도 분류 훈련에 오신 것을 환영합니다."
```

> **Static Bake**: `{...}` 가 전혀 없는 발화 또는 Static 세그먼트는 *Tools > Text to Speech Service > Bake Static Audio* 메뉴로 사전 합성할 수 있습니다. Baked WAV는 `StreamingAssets/TTS/Baked/{identifier}/{segmentIndex}.wav` 에 저장됩니다.

---

## C# 코드에서 사용하기

### 기본 재생

```csharp
// identifier에 해당하는 AudioClip 목록을 가져와 AudioSource로 순서대로 재생
_ttsService.PlayTranscript("triage-move-patient", audioSource);
```

### 변수 오버라이드

```csharp
var vars = new Dictionary<string, string>
{
  { "patient-name", "김철수" },
  { "destination",  "수술실" }
};

_ttsService.PlayTranscript("triage-move-patient", audioSource, overrideVariables: vars);
```

### 변수 미리 합성 (로딩 중)

Dynamic 세그먼트는 기본값으로 초기화 시 합성되지만, 게임 도중 확정되는 실제 값은 미리 준비(`PrepareVariable`)해 두면 지연 없이 재생됩니다.

```csharp
_ttsService.PrepareVariable("김철수", onDone: () =>
{
  Debug.Log("합성 완료");
});
```

---

## 작성 규칙

- **identifier**: `소문자-영숫자-하이픈` 형식. 예) `intro-welcome`, `step3-cpr-start`
- **basetext**: 한 identifier에 너무 많은 문장을 넣지 마세요. 문장 단위로 분리해 관리하면 재사용·수정이 쉽습니다.
- **variables**: basetext에 등장하는 모든 `{key}`는 variables에 기본값을 명시하세요. 기본값이 없는 변수는 런타임 오버라이드가 없을 경우 해당 세그먼트가 **무음으로 스킵**됩니다.
- identifier는 전체 파일 내에서 중복되면 안 됩니다. `TTSService`는 처음 발견된 값을 사용합니다.

---

## 관련 파일

| 경로 | 역할 |
|---|---|
| `Assets/Modules/TextToSpeechService/src/Models/SpeechTranscript.cs` | JSON 역직렬화 모델 |
| `Assets/Modules/TextToSpeechService/src/Models/SpeechSegment.cs` | 세그먼트 타입 정의 |
| `Assets/Modules/TextToSpeechService/src/Services/TranscriptParser.cs` | basetext → 세그먼트 파싱 로직 |
| `Assets/Modules/TextToSpeechService/src/Services/TTSService.cs` | 런타임 재생·캐싱 서비스 |
| `Assets/Modules/TextToSpeechService/src/UnitySupport/StaticAudioBakerWindow.cs` | 에디터 bake 도구 |
