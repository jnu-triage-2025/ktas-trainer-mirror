# API 레퍼런스: `MultiplayerInfrastructure.UI.ProblemSheet`

> **네임스페이스:** `MultiplayerInfrastructure.UI`  
> **관련 파일:**
> - `Assets/Modules/MultiplayerInfrastructure/Scripts/UI/Controllers/ProblemSheetUIController.cs`
> - `Assets/Modules/MultiplayerInfrastructure/Scripts/UI/VisualElements/ProblemSheetElement.cs`
> - `Assets/Modules/MultiplayerInfrastructure/UIDocuments/ProblemSheetUI.uxml`

---

## 0. 문서 목적

문제지 오버레이 UI를 게임에 배치하고, 문제 세트를 열고, 답안을 판정/진행/보상 처리하는 API와 연결 절차를 설명한다.

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
public bool OpenProblemSet(string problemSetIdentifier, int index = 0, bool singleProblemMode = false)
```

- Registry에서 문제 세트를 로드해 지정 인덱스 문제를 연다.
- `singleProblemMode=false`이면 정답 후 다음 문제로 진행 가능한 전체 세트 모드로 동작한다.
- `singleProblemMode=true`이면 해당 문제만 표시하는 단일 문제 모드로 동작한다.
- 성공 시 `true`, 실패 시 `false`.

### `OpenProblem`

```csharp
public void OpenProblem(ProblemDefinition problem)
```

- 단일 문제 데이터를 직접 바인딩해 연다.

### 진행/채점 상태

- `ProblemSheetElement`는 문제 진행도(`문제 x/y`)를 표시한다.
- 전체 세트 모드에서 정답일 때만 `다음 문제` 버튼이 노출된다.
- `닫기` 버튼은 마지막 문제를 정답 처리하면 `완료`로 라벨이 전환된다.

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
- `HideImmediately`에서도 오버레이 모드 해제를 수행하여 월드 상호작용 잠금 상태가 일관되게 유지된다.

---

## 4. 답안 판정 규칙

- 객관식: `ProblemAnswerEvaluator.EvaluateChoice`
  - `selectedIndex == correctIndex`면 정답
- 단답형: `ProblemAnswerEvaluator.EvaluateShortAnswer`
  - 조건 타입: `exact`, `contains`, `not_contains`
  - 결합 연산자: `operator = "and" | "or"`

### 재도전 정책

- 문제 JSON의 `grading.retryOnWrong`으로 오답 처리 정책을 제어한다.
  - `true`(기본): 오답 후 재도전 가능
  - `false`: 오답 즉시 최종 판정(입력 비활성화)

### 정답 보상 정책

- 문제 JSON의 `onCorrect.scoreboard[]`로 정답 시 scoreboard 조작을 선언할 수 있다.
- 각 항목 스키마:
  - `objective`: 변수 저장소 식별자(사용자 정의)
  - `criteria`: `dummy` 또는 `trigger` (기본 `dummy`)
  - `operation`: `add` | `set` | `remove` (기본 `add`)
  - `value`: 정수 (기본 `1`)
- objective가 존재하지 않으면 서버가 자동 생성 후 값을 적용한다.

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
      "grading": {
        "retryOnWrong": false
      },
      "choice": {
        "options": ["A", "B", "C", "D"],
        "correctIndex": 0
      },
      "onCorrect": {
        "scoreboard": [
          {
            "objective": "triage_problem_score",
            "criteria": "dummy",
            "operation": "add",
            "value": 1
          }
        ]
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
      },
      "grading": {
        "retryOnWrong": true
      }
    }
  ]
}
```

---

## 6. 관련 문서

- `Documents/requirements/ui/miui_problem_sheet.md`
- `Documents/api-references/MultiplayerInfrastructure.Registry.Problem.md`
- `Documents/api-references/MultiplayerInfrastructure.Chat.ChatService.md`
