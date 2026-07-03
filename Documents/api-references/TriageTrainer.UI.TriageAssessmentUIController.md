# API 레퍼런스: `TriageTrainer.UI.TriageAssessmentUIController`

> **네임스페이스:** `TriageTrainer.UI`
>
> **파일 위치:**
> - `Assets/Modules/TriageTrainer/Scripts/UI/Controllers/TriageAssessmentUIController.cs`
> - `Assets/Modules/TriageTrainer/Scripts/UI/VisualElements/TriageAssessmentPanelElement.cs`

---

## 0. 개요

트리아지 평가 전체화면 오버레이 UI 컨트롤러다. 환자 트리아지 인터랙션 수행 시 열리며, KTAS 1~5 등급을 각 색상의 사각형으로 가로 나열해 표시한다. 플레이어가 사각형을 클릭하면 선택 결과를 콜백으로 통보하고 패널을 닫는다.

`ProblemSheetUIController`와 동일한 `IUIOverlay` / `UIOverlayStack` 관례를 따른다.

---

## 1. 씬 배치

`TriageAssessmentUIController` 컴포넌트와 `UIDocument` 컴포넌트를 동일 GameObject에 배치한다.

---

## 2. 주요 API

### `Open(TriageLevel current, Action<TriageLevel> onSelected)`

트리아지 평가 패널을 연다.

| 파라미터 | 설명 |
|----------|------|
| `current` | 현재 환자에 부여된 등급. 해당 사각형이 진한 테두리로 강조 표시된다. `Unassessed`이면 강조 없음. |
| `onSelected` | 등급 선택 완료 시 선택된 `TriageLevel`을 인자로 호출되는 콜백. 취소 시에는 호출되지 않는다. |

`UIOverlayStack`이 비어 있지 않으면 스택에 push되어 기존 오버레이가 뒤로 밀린다.

### `Close()`

패널을 닫는다. `UIOverlayStack.Pop()`을 통해 이전 오버레이를 복원한다.

### `ActiveInstance`

씬에 배치된 단일 인스턴스의 정적 참조. 환자 컨트롤러에서 UI를 열 때 사용한다.

---

## 3. 인스펙터 필드

| 필드 | 기본값 | 설명 |
|------|--------|------|
| `_sortingOrder` | `9.5` | UIDocument 정렬 순서 (`DefaultsUIDocument.TriageAssessmentUISortOrder`) |

---

## 4. `TriageAssessmentPanelElement` 비주얼 엘리먼트

`TriageAssessmentPanelElement`는 `VisualElement`를 상속하며, 컨트롤러가 자동 생성한다.

- **구성:** 타이틀, 안내 문구, 등급 사각형 행(row), 취소 버튼.
- **등급 사각형:** `TriageLevelInfo.SelectableLevels`를 순서대로 배치. 각 사각형은 등급 색, `GetShortLabel`, `GetDisplayName`을 표시한다. 마우스 호버 시 `scale(1.06)` 확대 효과.
- `SetCurrentSelection(TriageLevel current)`: 현재 등급 사각형의 테두리를 강조(두껍게)한다.
- `LevelSelected` 이벤트: 사각형 클릭 시 선택된 `TriageLevel`을 통보.
- `CancelRequested` 이벤트: 취소 버튼 클릭 시 통보.

---

## 5. 흐름 요약

```
PatientController.BeginTriageAssessment()
  → TriageAssessmentUIController.Open(current, callback)
      → TriageAssessmentPanelElement 표시 + SetCurrentSelection
  → 플레이어가 사각형 클릭
      → LevelSelected → HandleLevelSelected → Close() → callback(level)
  → PatientController.SubmitTriageAssessment(level)
      → SetAssessedTriageNetworked(level)
```

---

## 참조

- [req:트리아지 평가 UI 요구사항](../requirements/ui/miui_triage_assessment.md)
- [req:환자 트리아지 분류 기능 요구사항](../requirements/patient/triage-classification-requirements.md)
- [api:TriageLevel / TriageLevelInfo](TriageTrainer.Patient.TriageLevelInfo.md)
