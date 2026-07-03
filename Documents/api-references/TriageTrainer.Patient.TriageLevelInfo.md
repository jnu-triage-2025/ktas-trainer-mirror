# API 레퍼런스: `TriageTrainer.Entity.Patient.TriageLevel` / `TriageLevelInfo`

> **네임스페이스:** `TriageTrainer.Entity.Patient`
>
> **파일 위치:**
> - `Assets/Modules/TriageTrainer/Scripts/Patient/Models/TriageLevel.cs`
> - `Assets/Modules/TriageTrainer/Scripts/Patient/Models/TriageLevelInfo.cs`

---

## 0. 개요

KTAS(Korean Triage and Acuity Scale) 5단계 트리아지 등급 체계를 정의하는 열거형과, 등급별 표준 색상·명칭을 단일 진실 공급원으로 제공하는 정적 유틸리티 클래스다.

UI, 인게임 오버헤드 라벨, 시나리오 조건 판정이 동일한 색상·명칭 규칙을 공유하도록 모든 시각적 매핑은 `TriageLevelInfo`를 통해 조회한다.

---

## 1. `TriageLevel` 열거형

```csharp
public enum TriageLevel
{
    Unassessed = 0,  // 미분류 (아직 평가되지 않음)
    Level1     = 1,  // 1단계 소생 — 파랑
    Level2     = 2,  // 2단계 긴급 — 빨강
    Level3     = 3,  // 3단계 응급 — 노랑
    Level4     = 4,  // 4단계 준응급 — 초록
    Level5     = 5,  // 5단계 비응급 — 흰색
}
```

`Unassessed`는 실제 KTAS 등급이 아니며 "아직 평가하지 않음"을 나타내는 기본값이다.

---

## 2. `TriageLevelInfo` 정적 클래스

### 2-1. 선택 가능한 등급 목록

```csharp
public static readonly TriageLevel[] SelectableLevels;
// Level1 ~ Level5 순서. Unassessed 제외.
```

### 2-2. 색상/명칭 조회 메서드

| 메서드 | 반환 | 설명 |
|--------|------|------|
| `GetColor(TriageLevel)` | `Color` | 등급 배경 색상 (swatch 색) |
| `GetTextColor(TriageLevel)` | `Color` | swatch 배경 위에 올릴 텍스트 색 (가독성 고려) |
| `GetDisplayName(TriageLevel)` | `string` | 한국어 명칭. 예: `"2단계 긴급"` |
| `GetShortLabel(TriageLevel)` | `string` | 짧은 라벨. 예: `"KTAS 2"` |

> **주의:** `GetTextColor`는 swatch **배경 위** 가독성을 위한 색이다(Level3·5에서 어두운 색). 어두운 배경 위 라벨 텍스트에는 `GetColor`를 사용한다.

### 2-3. 등급별 색상 표

| 등급 | 배경 색 | 텍스트 색(swatch 위) |
|------|---------|----------------------|
| Unassessed | 회색 | 흰색 |
| Level1 소생 | 파랑 `(0.15, 0.35, 0.85)` | 흰색 |
| Level2 긴급 | 빨강 `(0.85, 0.15, 0.15)` | 흰색 |
| Level3 응급 | 노랑 `(0.95, 0.80, 0.10)` | 어두운 색 |
| Level4 준응급 | 초록 `(0.20, 0.70, 0.25)` | 흰색 |
| Level5 비응급 | 흰색 `(0.96, 0.96, 0.96)` | 어두운 색 |

---

## 3. `PatientDescriptor` 확장 필드

트리아지 관련 데이터는 `PatientDescriptor` (`Assets/.../Patient/Models/PatientDescriptor.cs`)에 추가되었다.

```csharp
public class PatientDescriptor
{
    // ...기존 필드...
    public TriageLevel intendedTriage;  // 시나리오 설계자가 지정한 정답 등급
    public TriageLevel assessedTriage;  // 플레이어가 실제로 평가한 등급 (런타임)
}
```

`assessedTriage`는 런타임 상태값이며 `PatientController`의 `SyncVar<TriageLevel>`과 동기화된다. `intendedTriage`는 인스펙터에서 시나리오 설계 시 설정하는 정적 값이다.

---

## 참조

- [req:환자 트리아지 분류 기능 요구사항](../requirements/patient/triage-classification-requirements.md)
- [api:IScenarioTriageAssessTarget](MultiplayerInfrastructure.Entity.IScenarioTriageAssessTarget.md)
