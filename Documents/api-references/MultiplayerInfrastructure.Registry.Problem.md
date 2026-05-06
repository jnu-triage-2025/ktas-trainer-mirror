# API 레퍼런스: `MultiplayerInfrastructure.Registry` (Problem 확장)

> **네임스페이스:** `MultiplayerInfrastructure.Registry`  
> **관련 파일:** `Assets/Modules/MultiplayerInfrastructure/Scripts/Registry/Registry.Problem.cs`

---

## 0. 문서 목적

문제지 데이터(ProblemSet)와 문제 이미지(ProblemFigure)를 Registry에서 등록/조회/지연 로드하는 방법을 정리한다.
최신 문제팩 구조(`Problems/<set>/<set>.json`, `Problems/<set>/problem-pack.manifest`, `Problems/<set>/figures/*`)를 포함한다.

---

## 1. RegistryType 확장

문제 시스템은 다음 분류를 추가로 사용한다.

- `RegistryType.ProblemSet`: 문제 세트 데이터 저장소
- `RegistryType.ProblemFigure`: 문제 참고 이미지(Texture2D) 저장소

---

## 2. 공개 API

### `PreloadProblemSet`

```csharp
public static bool PreloadProblemSet(string identifier)
```

- `ProblemSet` 식별자를 기준으로 등록 항목을 확인한다.
- 없으면 아래 순서로 `TextAsset`을 로드한다.
  - `Resources/Problems/{identifier}`
  - `Resources/Problems/{identifier}/{identifier}`
- JSON 파싱 성공 시 `ProblemSetDefinition`으로 캐시 교체한다.

### `TryGetProblemSet`

```csharp
public static bool TryGetProblemSet(string identifier, out ProblemSetDefinition problemSet, out string error)
```

- 식별자로 문제 세트를 안전 조회한다.
- 내부적으로 `PreloadProblemSet`을 호출해 지연 로드를 수행한다.

### `PreloadProblemSetFromManifest`

```csharp
public static bool PreloadProblemSetFromManifest(string manifestResourceName = "problem-pack.manifest")
```

- 문제팩 manifest에서 `problemJson`을 읽어 문제세트를 로드한다.
- `problemJson`은 경로/확장자(`.json`) 포함 여부와 무관하게 정규화되어 해석된다.

### `RegisterProblemFigure`

```csharp
public static void RegisterProblemFigure(string identifier, Texture2D texture)
```

- 문제 이미지를 직접 Registry에 등록한다.

### `TryGetProblemFigure`

```csharp
public static bool TryGetProblemFigure(string identifier, out Texture2D texture)
```

- 이미지 식별자로 `Texture2D`를 조회한다.

### `TryResolveProblemFigureReference`

```csharp
public static bool TryResolveProblemFigureReference(string figureReference, out Texture2D texture)
```

- `probfig:(identifier)` 형식 문자열을 파싱해 문제 이미지를 해석한다.
- Registry에 없으면 아래 경로를 순차 시도한다.
  - `Resources/ProblemFigures/{identifier}`
  - `Resources/Problems/figures/{identifier}`
  - `Resources/Problems/{identifier}`
  - `Resources/Problems` 하위 전체 Texture2D에서 이름 일치 항목

### `ParseProblemFigureIdentifier`

```csharp
public static string ParseProblemFigureIdentifier(string figureReference)
```

- `probfig:(identifier)` 또는 `probfig:identifier`를 `identifier`로 변환한다.

---

## 3. 이미지 참조 규칙

- 권장 포맷: `probfig:(ktas_reference)`
- 허용 포맷: `probfig:ktas_reference`
- 실제 로드 경로: `Resources/ProblemFigures/ktas_reference` 또는 `Resources/Problems/.../figures/ktas_reference`

---

## 4. 운영 예시

```csharp
if (Registry.TryGetProblemSet("sample_problem_set", out var set, out var error))
{
    // set.Problems 사용
}

if (Registry.TryResolveProblemFigureReference("probfig:(ktas_reference)", out var tex))
{
    // UI에 tex 표시
}
```

---

## 5. 관련 문서

- `Documents/requirements/ui/miui_problem_sheet.md`
- `Documents/api-references/MultiplayerInfrastructure.UI.ProblemSheet.md`
