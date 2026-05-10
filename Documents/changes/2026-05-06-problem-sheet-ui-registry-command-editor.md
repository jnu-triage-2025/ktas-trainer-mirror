# 문제지 UI/Registry/명령어/에디터 통합 변경 (2026-05-06)

## 변경 목적
- 인게임 문제 풀이 UI를 MultiplayerInfrastructure의 Overlay UI/Registry 체계에 정식 편입합니다.
- 문제 데이터(JSON)와 참고 이미지 로딩 규약(`probfig:(identifier)`)을 표준화합니다.
- 운영자가 채팅 명령어로 특정 대상에게 문제지를 즉시 표시할 수 있도록 합니다.
- 비개발자도 문제 세트를 제작/배포할 수 있도록 정적 웹 에디터(WebComponent)와 ZIP 패키징 규약을 제공합니다.

## 핵심 변경 사항

### 1) 문제 도메인 모델/채점기 추가
- 경로: `Assets/Modules/MultiplayerInfrastructure/Scripts/Problem/`
- 추가 파일:
  - `ProblemSetDefinition.cs`
  - `ProblemSetLoader.cs`
  - `ProblemAnswerEvaluator.cs`
- 지원 기능:
  - 객관식: 선택지 + 정답 인덱스
  - 단답형: `exact`, `contains`, `not_contains`
  - 조건 결합: `and` / `or`

### 2) Registry 확장
- 변경 파일:
  - `Assets/Modules/MultiplayerInfrastructure/Scripts/Registry/Models/RegistryType.cs`
  - `Assets/Modules/MultiplayerInfrastructure/Scripts/Registry/Registry.cs`
  - `Assets/Modules/MultiplayerInfrastructure/Scripts/Registry/Registry.Problem.cs`
- 반영 내용:
  - `RegistryType.ProblemSet`, `RegistryType.ProblemFigure` 추가
  - `PreloadProblemSet`, `TryGetProblemSet`, `RegisterProblemFigure`, `TryResolveProblemFigureReference` 추가
  - 이미지 로드 폴백 경로 확장:
    - `Resources/ProblemFigures/{id}`
    - `Resources/Problems/figures/{id}`
    - `Resources/Problems/{id}`

### 3) Manifest 기반 패키지 식별 지원
- 변경 파일: `Assets/Modules/MultiplayerInfrastructure/Scripts/Registry/Registry.Problem.cs`
- 추가 API:
  - `TryGetProblemIdentifierFromManifest(string manifestResourceName, out string problemIdentifier)`
  - `PreloadProblemSetFromManifest(string manifestResourceName = "problem-pack.manifest")`
- 목적:
  - `Resources/Problems`에 ZIP을 바로 해제한 뒤, 루트 manifest로 문제세트 식별자를 파악해 즉시 로드 가능

### 4) 문제지 Overlay UI 추가
- 경로: `Assets/Modules/MultiplayerInfrastructure/Scripts/UI/`
- 추가 파일:
  - 컨트롤러: `Controllers/ProblemSheetUIController.cs`
  - 요소: `VisualElements/ProblemSheetElement.cs`, `ProblemPromptElement.cs`, `ProblemChoiceElement.cs`, `ProblemShortAnswerElement.cs`
  - UXML: `Assets/Modules/MultiplayerInfrastructure/UIDocuments/ProblemSheetUI.uxml`
- 연동:
  - `IUIOverlay`, `UIOverlayStack` 규약 준수
  - `DefaultsUIDocument.ProblemSheetUISortOrder` 추가

### 5) 채팅 명령어로 문제지 열기
- 변경 파일:
  - `Assets/Modules/MultiplayerInfrastructure/Scripts/Command/CommandService.cs`
  - `Assets/Modules/MultiplayerInfrastructure/Scripts/Command/CommandDefinitions/CommandDefinition.Scenario.cs`
  - `Assets/Modules/MultiplayerInfrastructure/Scripts/Chat/ChatService.cs`
- 신규 명령어:
  - `/problemsheet <target> <problem-identifier>`
- target 지원:
  - `@s`, `@a`, `@n`, `fish:<clientId>`
- 서버가 대상 클라이언트로 TargetRpc를 보내고, 클라이언트가 `ProblemSheetUIController.OpenProblemSet(...)` 실행

### 6) 정적 웹 에디터(WebComponent) 추가 및 패키징 규약 변경
- 경로: `Tools/ingame-problem-solving-ui-editor/index.html`
- 기능:
  - WebComponent 기반 문제 세트 생성/편집
  - 객관식/단답형/이미지 설정
  - ZIP 다운로드(JSZip)
- ZIP 출력 구조(최신):
  - 루트: `problem-pack.manifest`
  - 루트: `<problem-identifier>.json`
  - 하위: `figures/*`
- 운영 방식:
  - ZIP을 `Assets/Modules/TriageTrainer/Resources/Problems`에 바로 압축 해제
  - manifest 기준 식별자로 문제세트 로드

## 샘플 데이터
- 추가 파일: `Assets/Modules/TriageTrainer/Resources/Problems/sample_problem_set.json`
- 목적: 객관식 + 단답형 조건식 + `probfig:(...)` 사용 예시 제공

## 문서화 반영
- requirements:
  - `Documents/requirements/ui/miui_problem_sheet.md`
- api-references:
  - `Documents/api-references/MultiplayerInfrastructure.Registry.Problem.md`
  - `Documents/api-references/MultiplayerInfrastructure.UI.ProblemSheet.md`

## 검증 요약
- `dotnet build Assembly-CSharp.csproj` 기준 컴파일 오류 0 확인
- 기존 코드베이스 경고는 다수 유지(본 변경과 무관)
