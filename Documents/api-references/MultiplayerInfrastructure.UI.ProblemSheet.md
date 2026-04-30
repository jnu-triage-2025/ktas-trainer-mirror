# API 레퍼런스: `MultiplayerInfrastructure.UI.ProblemSheet`

> **네임스페이스:** `MultiplayerInfrastructure.UI`  
> **관련 파일:**
> - `Assets/Modules/MultiplayerInfrastructure/Scripts/UI/Controllers/ProblemSheetUIController.cs`
> - `Assets/Modules/MultiplayerInfrastructure/Scripts/UI/VisualElements/ProblemSheetElement.cs`
> - `Assets/Modules/MultiplayerInfrastructure/UIDocuments/ProblemSheetUI.uxml`

---

## 0. 문서 목적

문제지 오버레이 UI를 게임에 배치하고, 문제 세트를 열고, 답안을 판정하는 최소 API와 연결 절차를 설명한다.

---

## 1. 구성 요소

- 컨트롤러: `ProblemSheetUIController` (`UIControllerABC`, `IUIOverlay` 구현)
- 루트 요소: `ProblemSheetElement`
- 하위 요소:
  - `ProblemPromptElement` (지문 + 이미지)
  - `ProblemChoiceElement` (객관식 버튼)
  - `ProblemShortAnswerElement` (단답 입력/제출)

---

## 2. 주요 메서드

### `OpenProblemSet`

```csharp
public bool OpenProblemSet(string problemSetIdentifier, int index = 0)
```

- Registry에서 문제 세트를 로드해 지정 인덱스 문제를 연다.
- 성공 시 `true`, 실패 시 `false`.

### `OpenProblem`

```csharp
public void OpenProblem(ProblemDefinition problem)
```

- 단일 문제 데이터를 직접 바인딩해 연다.

### `Close`

```csharp
public void Close()
```

- 오버레이 스택에서 해당 UI를 닫는다.

---

## 3. 오버레이 동작

- `Open...` 호출 시 `UIOverlayStack.Push(this)` 경로를 사용한다.
- `OnOverlayPushed`에서 패널 표시 + 플레이어 오버레이 모드 진입.
- `OnOverlayPopped`에서 패널 숨김 + 플레이어 오버레이 모드 해제.

---

## 4. 답안 판정 규칙

- 객관식: `ProblemAnswerEvaluator.EvaluateChoice`
  - `selectedIndex == correctIndex`면 정답
- 단답형: `ProblemAnswerEvaluator.EvaluateShortAnswer`
  - 조건 타입: `exact`, `contains`, `not_contains`
  - 결합 연산자: `operator = "and" | "or"`

---

## 5. JSON 스키마(운영 규약)

```json
{
  "identifier": "sample_problem_set",
  "title": "Sample Problem Set",
  "problems": [
    {
      "id": "p1",
      "prompt": "문제 지문",
      "figure": "probfig:(ktas_reference)",
      "choice": {
        "options": ["A", "B", "C", "D"],
        "correctIndex": 0
      }
    },
    {
      "id": "p2",
      "prompt": "단답형 문제",
      "shortAnswer": {
        "operator": "and",
        "conditions": [
          { "type": "contains", "value": "airway", "ignoreCase": true },
          { "type": "not_contains", "value": "delay", "ignoreCase": true }
        ]
      }
    }
  ]
}
```

---

## 6. 관련 문서

- `Documents/requirements/ui/miui_problem_sheet.md`
- `Documents/api-references/MultiplayerInfrastructure.Registry.Problem.md`
