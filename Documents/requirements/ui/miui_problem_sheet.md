---
title: "문제지/문제 풀이 UI"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

## 개요

문제지/문제 풀이 UI는 학습자가 문제 지문, 선택지, 단답 입력을 같은 화면에서 확인하고 답안을 제출할 수 있도록 제공하는 오버레이 UI다.

## 상세

- 문제 본문은 텍스트 지문과 선택 이미지(참고 사진)를 함께 표시할 수 있어야 한다.
- 객관식 문제는 4~5개 선택지를 버튼 형태로 제시하고 즉시 정오답을 판정해야 한다.
- 단답형 문제는 문자열 비교 조건(완전 일치, 포함, 특정 문자열 미포함)을 조합해 정답을 판정해야 한다.
- 문제 세트는 런타임에 식별자로 로드되어야 하며, 문제 UI는 오버레이 스택 규칙(`IUIOverlay`, `UIOverlayStack`)을 따라야 한다.
- UI를 사용하는 운영자는 UXML 배치와 컨트롤러 연결만으로 문제지를 노출할 수 있어야 한다.

## 운영 적용 체크리스트

1. `UIDocument`를 가진 오브젝트에 `ProblemSheetUIController`를 추가한다.
2. 같은 오브젝트의 `UIDocument.visualTreeAsset`에 `ProblemSheetUI.uxml`을 연결한다.
3. 문제 JSON 파일을 `Assets/Modules/TriageTrainer/Resources/Problems/`에 배치한다.
4. 문제 이미지 텍스처를 `Assets/Modules/TriageTrainer/Resources/ProblemFigures/`에 배치한다.
5. 코드에서 `OpenProblemSet("문제세트식별자", 문제인덱스)`를 호출해 문제를 연다.

## 기술적 세부 사항

- 주요 컨트롤러: `ProblemSheetUIController`
- 주요 VisualElement:
  - `ProblemSheetElement`
  - `ProblemPromptElement`
  - `ProblemChoiceElement`
  - `ProblemShortAnswerElement`
- UXML: `Assets/Modules/MultiplayerInfrastructure/UIDocuments/ProblemSheetUI.uxml`
- 정렬 순서 상수: `DefaultsUIDocument.ProblemSheetUISortOrder`

## 참조

- [api:MultiplayerInfrastructure.UI.ProblemSheet](../../api-references/MultiplayerInfrastructure.UI.ProblemSheet.md)
- [api:MultiplayerInfrastructure.Registry.Problem](../../api-references/MultiplayerInfrastructure.Registry.Problem.md)
