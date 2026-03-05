# TextToSpeechService.TTSCore

## 0. 개요

`TTSCore`는 Supertonic ONNX TTS 엔진을 감싸는 순수 C# 래퍼 클래스입니다. `UnityEngine`에 의존하지 않아 에디터 스크립트와 런타임 코드 양쪽에서 사용할 수 있습니다.

일반적으로 직접 사용하지 않고 [`TTSService`](TextToSpeechService.TTSService.md)를 통해 간접 사용합니다. 에디터 도구(`StaticAudioBakerWindow`)나 독립 스크립트에서 직접 사용할 때 이 API를 참조하세요.

**네임스페이스:** `TextToSpeechService`  
**파일:** `Assets/Modules/TextToSpeechService/src/Services/TTSCore.cs`  
**구현:** `IDisposable`

---

## 1. 경로 상수

### StreamingAssets 하위 경로

| 상수 | 값 | 설명 |
|---|---|---|
| `DefaultOnnxSubdir` | `"TTS/Models/onnx"` | ONNX 모델 파일 폴더 |
| `DefaultStyleSubdir` | `"TTS/Models/voice_styles"` | 음성 스타일 JSON 폴더 |
| `BakedAudioSubdir` | `"TTS/Baked"` | 사전 합성 WAV 폴더 |
| `HFBaseUrl` | `"https://huggingface.co/Supertone/supertonic-2/resolve/main/"` | 모델 다운로드 베이스 URL |

### 파일 목록

```csharp
// 필수 ONNX 모델 파일 (HF 상대 경로)
public static readonly string[] RequiredOnnxFiles;

// 다운로드 가능한 음성 스타일 파일 (HF 상대 경로)
public static readonly string[] VoiceStyleFiles;
```

사용 가능한 스타일: `F1`, `F2`, `F3`, `F4`, `F5`, `M1`, `M2`, `M3`, `M4`, `M5`

---

## 2. 경로 헬퍼 (정적 메서드)

### `GetOnnxDir`

```csharp
public static string GetOnnxDir(string streamingAssetsPath)
```

`Application.streamingAssetsPath`를 받아 ONNX 모델 디렉터리의 절대 경로를 반환합니다.

```csharp
string onnxDir = TTSCore.GetOnnxDir(Application.streamingAssetsPath);
// → "{projectRoot}/Assets/StreamingAssets/TTS/Models/onnx"
```

---

### `GetVoiceStylePath`

```csharp
public static string GetVoiceStylePath(string streamingAssetsPath, string styleName)
```

음성 스타일 JSON 파일의 절대 경로를 반환합니다. `styleName`은 확장자 포함/생략 모두 허용합니다.

```csharp
string path = TTSCore.GetVoiceStylePath(sa, "F1");   // "F1.json" 도 동일
```

---

### `GetVoiceStyleDir`

```csharp
public static string GetVoiceStyleDir(string streamingAssetsPath)
```

음성 스타일 폴더의 절대 경로를 반환합니다.

---

### `GetBakedClipPath`

```csharp
public static string GetBakedClipPath(
    string streamingAssetsPath,
    string identifier,
    int segmentIndex)
```

Static 세그먼트에 대응하는 사전 합성 WAV 파일의 절대 경로를 반환합니다.

저장 경로 규칙: `StreamingAssets/TTS/Baked/{identifier}/{segmentIndex}.wav`

```csharp
string path = TTSCore.GetBakedClipPath(sa, "triage-move-patient", 0);
// → ".../StreamingAssets/TTS/Baked/triage-move-patient/0.wav"
```

---

## 3. 모델 검사 (정적 메서드)

### `GetMissingModelFiles`

```csharp
public static List<string> GetMissingModelFiles(string onnxDir)
```

ONNX 모델 디렉터리에서 누락된 필수 파일 목록을 반환합니다. 모두 존재하면 빈 리스트를 반환합니다.

---

### `AreModelsPresent`

```csharp
public static bool AreModelsPresent(string onnxDir)
```

필수 파일이 하나라도 없으면 `false`를 반환합니다.

```csharp
if (!TTSCore.AreModelsPresent(onnxDir))
{
    Debug.LogError("모델 파일을 다운로드하세요.");
}
```

---

## 4. 생성자

```csharp
public TTSCore(string onnxDir, string voiceStylePath)
```

ONNX 세션을 열고 음성 스타일을 로드합니다. **블로킹 작업**이므로 백그라운드 스레드에서 호출하세요.

| 파라미터 | 설명 |
|---|---|
| `onnxDir` | ONNX 모델 디렉터리 **절대** 경로 |
| `voiceStylePath` | 음성 스타일 JSON 파일 **절대** 경로 |

---

## 5. 프로퍼티

### `SampleRate`

```csharp
public int SampleRate { get; }
```

합성 오디오의 샘플레이트(Hz). `AudioClip.Create` 시 사용합니다.

---

## 6. 합성 메서드

### `Synthesize`

```csharp
public float[] Synthesize(
    string text,
    string lang,
    int totalStep = 5,
    float speed = 1.05f)
```

텍스트를 TTS 합성하여 원시 PCM `float[]` 샘플을 반환합니다. **블로킹 메서드**이므로 백그라운드 스레드에서 호출하세요.

| 파라미터 | 설명 |
|---|---|
| `text` | 합성할 텍스트 |
| `lang` | 언어 코드 (`en`, `ko`, `es`, `pt`, `fr`) |
| `totalStep` | Diffusion 스텝 수. 높을수록 품질 향상, 속도 저하 |
| `speed` | 발화 속도 배율. `1.0` = 기본 속도 |

**반환값:** mono, `SampleRate` Hz, float32 정규화된 PCM 샘플 배열

Unity `AudioClip`으로 변환 예시:

```csharp
float[] samples = core.Synthesize("안녕하세요", "ko");
var clip = AudioClip.Create("hello", samples.Length, 1, core.SampleRate, false);
clip.SetData(samples, 0);
```

---

## 7. 직접 사용 예시 (에디터 스크립트)

```csharp
string sa    = Application.streamingAssetsPath;
string onnx  = TTSCore.GetOnnxDir(sa);
string style = TTSCore.GetVoiceStylePath(sa, "F1");

if (!TTSCore.AreModelsPresent(onnx))
{
    Debug.LogError("모델 없음");
    return;
}

// 백그라운드 스레드에서 실행
await Task.Run(() =>
{
    using var core = new TTSCore(onnx, style);
    float[] wav = core.Synthesize("테스트 문장입니다.", "ko");
    File.WriteAllBytes("/tmp/test.wav", WavEncoder.Encode(wav, core.SampleRate));
});
```

---

## 8. 관련 문서

- [TTSService API](TextToSpeechService.TTSService.md)
- [Transcript 작성 가이드](../tts-transcripts.md)
