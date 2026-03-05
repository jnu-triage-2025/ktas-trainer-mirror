# TextToSpeechService.TTSService

## 0. 개요

`TTSService`는 TTS 음성 재생을 담당하는 Unity `MonoBehaviour` 서비스 컴포넌트입니다.

- Transcript JSON을 읽어 세그먼트로 파싱합니다.
- 사전 합성된(baked) WAV 파일을 우선 로드하고, 없으면 런타임에 ONNX 모델로 합성합니다.
- 변수 오버라이드 재생 및 동적 텍스트의 미리 합성(`PrepareVariable`)을 지원합니다.

**네임스페이스:** `TextToSpeechService`  
**파일:** `Assets/Modules/TextToSpeechService/src/Services/TTSService.cs`  
**의존:** [`TTSCore`](TextToSpeechService.TTSCore.md), `TranscriptParser`

---

## 1. Inspector 설정

씬에 배치된 컴포넌트에서 다음 필드를 설정합니다.

| 필드 | 기본값 | 설명 |
|---|---|---|
| `transcriptJsonRelPath` | `"transcripts.json"` | StreamingAssets 기준 상대 경로. Transcript JSON 파일 위치. |
| `voiceStyleName` | `"F1"` | 음성 스타일 파일명 (확장자 생략 가능). `TTS/Models/voice_styles/` 하위에 위치. |
| `language` | `"ko"` | TTS 언어 코드. `en`, `ko`, `es`, `pt`, `fr` 중 하나. |
| `totalStep` | `5` | Diffusion 스텝 수. 높을수록 음질 향상, 처리 시간 증가. |
| `speed` | `1.05` | 발화 속도 배율. `1.0` 이 기본 속도. |

---

## 2. 프로퍼티

### `IsReady`

```csharp
public bool IsReady { get; private set; }
```

`Awake` → 초기화 코루틴이 완료되면 `true`가 됩니다.  
`GetClips`, `PlayTranscript` 호출 전에 `IsReady`를 확인하거나, 초기화 완료 시점에 맞춰 호출 타이밍을 조율하세요.

---

## 3. 공개 메서드

### `GetClips`

```csharp
public List<AudioClip> GetClips(
    string identifier,
    Dictionary<string, string> overrideVariables = null)
```

Transcript `identifier`에 해당하는 `AudioClip` 목록을 반환합니다.  
세그먼트 순서대로 정렬되어 있으므로, 이 목록을 순서대로 재생하면 올바른 발화가 됩니다.

**Dynamic 세그먼트 텍스트 결정 순서:**
1. `overrideVariables`에 해당 키가 있으면 그 값 사용
2. Transcript JSON의 `variables` 기본값 사용
3. 둘 다 없으면 해당 세그먼트 스킵 (무음)

캐시에 없는 텍스트는 **메인 스레드에서 동기 합성**합니다. 처리 시간이 걸릴 수 있으므로 가능하면 `PrepareVariable`로 미리 합성해 두세요.

**예외:** `identifier`가 존재하지 않으면 `ArgumentException`을 던집니다.

---

### `PlayTranscript`

```csharp
public Coroutine PlayTranscript(
    string identifier,
    AudioSource audioSource,
    Dictionary<string, string> overrideVariables = null)
```

`GetClips`로 얻은 AudioClip 목록을 `audioSource`를 통해 **순서대로 재생**하는 코루틴을 시작합니다.  
각 클립의 재생이 끝나면 자동으로 다음 클립을 재생합니다.

**예시:**

```csharp
// 기본값으로 재생
_ttsService.PlayTranscript("triage-move-patient", audioSource);

// 변수 오버라이드 재생
_ttsService.PlayTranscript("triage-move-patient", audioSource, new Dictionary<string, string>
{
    { "patient-name", "김철수" },
    { "destination",  "수술실" }
});
```

---

### `PrepareVariable`

```csharp
public Coroutine PrepareVariable(string text, Action onDone = null)
```

지정한 텍스트를 백그라운드에서 TTS 합성하여 캐시에 저장합니다.  
이미 캐시에 있는 텍스트는 즉시 `onDone`을 호출하고 종료합니다.

게임 로딩 중 변수 값(예: 환자 이름, 목적지)이 확정되는 시점에 미리 호출해 두면, 이후 `PlayTranscript` 호출 시 지연 없이 재생됩니다.

**예시:**

```csharp
_ttsService.PrepareVariable("김철수", onDone: () =>
{
    Debug.Log("합성 완료, 재생 준비됨");
});
```

---

## 4. 초기화 흐름

`Awake` 시점에 다음 순서로 초기화됩니다.

```
Awake
  └─ ONNX 모델 존재 확인 (TTSCore.AreModelsPresent)
      └─ InitializeCoroutine (코루틴)
            1. Transcript JSON 파싱 + TranscriptParser로 세그먼트 분해
            2. Static 세그먼트의 baked WAV 로드 (UnityWebRequest, 메인 스레드)
            3. 백그라운드 Task:
               - TTSCore 생성 (ONNX 모델 로드)
               - Dynamic 세그먼트 기본값 합성
               - baked 없는 Static 세그먼트 합성
            4. 메인 스레드: AudioClip 생성 및 캐시 등록
            5. IsReady = true
```

ONNX 모델이 없으면 `Debug.LogError`를 출력하고 초기화가 중단됩니다.  
→ 에디터 메뉴 **Tools > Text to Speech Service > Download Models** 에서 다운로드하세요.

---

## 5. 관련 문서

- [TTSCore API](TextToSpeechService.TTSCore.md)
- [Transcript 작성 가이드](../tts-transcripts.md)
