---
title: "TTS 다중 목소리 프로파일 설정 가이드"
doc_type: working-guide
status: active
updated: 2026-07-03
---

# TTS 다중 목소리 프로파일 설정 가이드 (운영자용)

이 가이드는 시나리오 등장인물마다 **다른 목소리(Voice Profile)**를 지정하는 전체 작업 절차를 설명합니다.  
Unity 에디터에서 프로파일을 설정하고, 시나리오 JSON에 식별자를 기입하고, bake 도구로 음성을 미리 생성하는 순서로 진행합니다.

---

## 준비 확인

- ONNX 모델이 다운로드되어 있어야 합니다.  
  확인: `StreamingAssets/TTS/Models/onnx/` 폴더에 `.onnx` 파일 4개가 존재하는지 확인하세요.  
  없으면 **Tools > Text to Speech Service > Download Models** 를 실행합니다.
- Voice Style 파일이 있어야 합니다.  
  확인: `StreamingAssets/TTS/Models/voice_styles/` 폴더에 `F1.json` ~ `M5.json` (10개)가 있는지 확인하세요.

---

## 1단계 — 캐릭터·목소리 대응표 정리

작업 전에 시나리오의 등장인물과 사용할 목소리를 미리 결정합니다.

| 캐릭터 | VoiceIdentifier (직접 결정) | VoiceStyleName (10개 중 선택) |
|--------|----------------------------|-------------------------------|
| 나레이터 | `narrator` | `F1` |
| 상황실 교신원 | `dispatcher` | `M1` |
| 담당 의사 | `doctor` | `M2` |
| 환자 | `patient` | `F3` |

> **VoiceIdentifier** — 아무 영문자·숫자·하이픈 조합으로 자유롭게 결정하세요. 한 프로젝트 안에서 겹치지 않으면 됩니다.  
> **VoiceStyleName** — 반드시 `F1`~`F5`, `M1`~`M5` 중 하나를 정확히 입력해야 합니다.

---

## 2단계 — TTSService Inspector에 프로파일 등록

1. **씬(Scene) 열기**: TTS가 사용되는 씬을 엽니다.

2. **TTSService 컴포넌트 찾기**  
   - Hierarchy 또는 Project 창에서 `TTSService`(또는 `ScenarioTTSService`)가 붙어 있는 GameObject를 선택합니다.

3. **인스펙터 편집**  
   Inspector에서 **"다중 목소리 프로파일"** 섹션을 찾습니다.

   ```
   [Header] 다중 목소리 프로파일
   Voice Profiles  Size: 0
   ```

   - **Size 숫자**를 등록할 캐릭터 수만큼 늘립니다. (예: 4)
   - 생성된 각 항목에 아래와 같이 입력합니다:

   | 항목 | VoiceIdentifier | VoiceStyleName | Language | Speed | TotalStep |
   |------|-----------------|----------------|----------|-------|-----------|
   | 0 | `narrator` | `F1` | (비움) | `0` | `0` |
   | 1 | `dispatcher` | `M1` | (비움) | `0` | `0` |
   | 2 | `doctor` | `M2` | (비움) | `0` | `0` |
   | 3 | `patient` | `F3` | (비움) | `0` | `0` |

   > `Language`, `Speed`, `TotalStep`을 비워두거나 `0`으로 설정하면 TTSService의 기본값을 사용합니다.

4. **저장**: Ctrl+S 로 씬을 저장합니다.

---

## 3단계 — 시나리오 JSON에 식별자 입력

각 대사 노드(`Dialogue`, `Choice`, `Quiz`)에 `"ttsVoiceIdentifier"` 필드를 추가합니다.

### Dialogue 노드 예시

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

### Choice 노드 예시

```json
{
  "identifier": "C001",
  "nodeType": "Choice",
  "speakerName": "나레이터",
  "dialogueContent": "이 환자의 트리아지 등급을 결정하세요.",
  "playTTS": true,
  "ttsVoiceIdentifier": "narrator",
  "options": [ ... ],
  "nextIdentifier": null
}
```

### Quiz 노드 예시

```json
{
  "identifier": "Q001",
  "nodeType": "Quiz",
  "question": "쇼크 환자의 1순위 처치는?",
  "options": ["기도 확보", "지혈", "보온"],
  "correctIndex": 0,
  "feedbackCorrect": "맞습니다.",
  "feedbackIncorrect": "틀렸습니다.",
  "playTTS": true,
  "ttsVoiceIdentifier": "narrator",
  "nextIdentifier": "D002"
}
```

### PlayTTS 노드 (transcript 기반) 예시

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

> `"playTTS": false`이거나 필드가 없는 노드는 `ttsVoiceIdentifier`를 입력해도 재생되지 않습니다.

---

## 4단계 — Bake (사전 합성)

런타임에 대사가 끊김 없이 바로 재생되려면 미리 합성(bake)해야 합니다.

1. 메뉴에서 **Tools > Text to Speech Service > Bake Scenario Inline Audio** 를 엽니다.

2. **ONNX 디렉터리** 경로를 확인합니다.

3. **기본 목소리 설정** 섹션:
   - 음성 스타일: `StreamingAssets/TTS/Models/voice_styles/F1.json` (기본 목소리)
   - 언어 코드: `ko`

4. **추가 목소리 프로파일** 섹션에서 **"+ 프로파일 추가"** 버튼을 눌러 2단계에서 등록한 것과 동일하게 입력합니다.

   | VoiceIdentifier | 음성 스타일명 |
   |-----------------|--------------|
   | `narrator` | `F1` |
   | `dispatcher` | `M1` |
   | `doctor` | `M2` |
   | `patient` | `F3` |

   > 이 창의 프로파일 목록은 씬과 별개입니다. bake 창을 열 때마다 다시 입력해야 합니다.

5. **"변경/미사용 파일 정리"** 체크박스는 기본적으로 꺼져 있습니다.  
   처음 bake 시에는 켜지 마세요. 기존 파일이 삭제될 수 있습니다.

6. **"다시 스캔"** 버튼을 눌러 아직 bake되지 않은 항목을 확인합니다.

7. **"미bake/변경분만 굽기"** 버튼을 클릭합니다.  
   완료까지 수 분이 걸릴 수 있습니다.

8. bake가 완료되면 로그 창에 `[OK]` 항목이 나타나고 에디터 하단 상태바에 `완료`가 표시됩니다.

---

## 5단계 — 플레이 모드 진입 확인

Unity 에디터에서 플레이 모드를 시작할 때 미bake 항목이 있으면 팝업이 뜹니다.

- **"지금 bake"**: 자동으로 bake 후 재생합니다.
- **"그대로 재생"**: bake 없이 런타임 즉석 합성으로 재생합니다. (첫 재생 시 지연 발생 가능)
- **"취소"**: 플레이 모드 진입을 취소합니다.

---

## 문제 해결

| 증상 | 원인 | 해결 |
|------|------|------|
| 특정 캐릭터의 TTS가 기본 목소리로 재생됨 | JSON의 `ttsVoiceIdentifier`와 Inspector의 `VoiceIdentifier`가 다름 | 둘을 일치시킴 |
| TTS가 전혀 재생되지 않음 | 노드에 `"playTTS": true` 누락 | JSON에 `"playTTS": true` 추가 |
| bake 후에도 런타임 합성이 발생함 | Bake 창에 프로파일을 입력하지 않았거나 식별자 불일치 | Bake 창의 프로파일과 JSON 식별자 일치 확인 후 재bake |
| ONNX 모델 없음 오류 | 모델 미다운로드 | Tools > TTS > Download Models 실행 |

---

## 관련 문서

- [TTS 다중 목소리 프로파일 요구사항](../../../requirements/content-definitions/audio/tts-multi-voice-spec.md) — 기능 사양 및 JSON 필드 상세
- [TTS 스크립트 (Transcripts) 가이드](../../../requirements/content-definitions/audio/tts-transcripts-spec.md) — transcript 기반 PlayTTS 노드 작성
- [시나리오 작성 가이드](./scenario-authoring-guide.md) — 시나리오 JSON 전반 작성 규칙
