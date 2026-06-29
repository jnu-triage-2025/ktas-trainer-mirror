# API 레퍼런스: `MultiplayerInfrastructure.Editor.ScenarioGraphAuthoringWindow`

> **네임스페이스:** `MultiplayerInfrastructure.Editor`  
> **기반 클래스:** `UnityEditor.EditorWindow`  
> **파일 위치:** `Assets/Modules/MultiplayerInfrastructure/Editor/Scenario/ScenarioGraphEditor/ScenarioGraphEditor.cs`

---

## 0. 문서 목적

`ScenarioGraphAuthoringWindow`는 ScenarioGraph를 시각적으로 편집하고 JSON으로 저장하는 에디터 창입니다.  
노드 생성, 연결 편집, 이름 변경, 타입 교체, 레이아웃 저장/복원뿐 아니라 플레이 모드에서 현재 실행 노드를 하이라이트하는 기능을 제공합니다.

---

## 1. 창 열기 및 수명주기

```csharp
[MenuItem("Tools/Multiplayer Infrastructure/Scenario Graph Editor")]
public static void Open()
```

에디터 메뉴에서 창을 엽니다. 창 제목은 `Scenario Graph Editor`입니다.

### 수명주기 동작

- `OnEnable`
  - UI 구성
  - `ScenarioGraphView`, `ScenarioInspectorView`, `ScenarioNodeSearchWindow` 생성
  - 플레이 모드 상태 변경 이벤트 구독
  - 런타임 하이라이트 상태 동기화
- `OnDisable`
  - 플레이 모드 이벤트 해제
  - 실행 하이라이트 해제
  - 루트 UI 제거

---

## 2. 편집 기능

### `CreateNode`

```csharp
public ScenarioNodeView CreateNode(ScenarioNodeType type, Vector2 screenMousePosition)
```

- 새 `IScenarioNode`를 생성해 그래프에 추가합니다.
- 화면 좌표를 그래프 좌표로 변환해 노드 위치를 배치합니다.
- 새 노드를 인스펙터 타깃으로 설정합니다.

### `ChangeNodeType`

```csharp
public ScenarioNodeView ChangeNodeType(ScenarioNodeView nodeView, ScenarioNodeType newType)
```

- 기존 노드의 식별자와 `NextIdentifier`를 유지한 채 타입을 교체합니다.
- 교체 후 연결선을 다시 구성합니다.

### `TryRenameNode`

```csharp
public bool TryRenameNode(ScenarioNodeView nodeView, string newId)
```

- 중복 식별자를 검사하고 성공 시 그래프 키와 참조를 갱신합니다.
- `NextIdentifier`, `ChoiceOption.NextNodeIdentifier`, `ParallelBranch.Identifier`, `Quiz` 분기 식별자까지 함께 갱신합니다.

### `RemoveNode`

```csharp
public void RemoveNode(ScenarioNodeView nodeView)
```

- 그래프에서 노드를 제거하고, 다른 노드가 참조하던 식별자도 정리합니다.

---

## 3. 저장 포맷

### 시나리오 파일

- 본문 그래프는 `.scenario.json`으로 저장합니다.
- 저장 시 `ScenarioGraphLoader.SaveToJson(graphData, true)`를 사용합니다.

### 에디터 사이드카

- 노드 위치는 별도 사이드카 파일 `.scenario.editor.json`에 저장합니다.
- `ScenarioGraphEditorData`에 노드 위치를 기록하고 `System.Text.Json`으로 직렬화합니다.

---

## 4. 런타임 하이라이트

이 창의 핵심 추가 기능은 플레이 모드에서 현재 실행 중인 시나리오 노드를 에디터에서 강조 표시하는 것입니다.

### 동기화 조건

- `Application.isPlaying == true`일 때만 동작합니다.
- `ScenarioController.Instance`가 존재해야 합니다.
- 현재 그래프의 식별자와 에디터에서 열어둔 그래프의 식별자가 일치해야 합니다.

### 구독 이벤트

창은 다음 이벤트를 구독합니다.

- `ScenarioController.OnScenarioStarted`
- `ScenarioController.OnScenarioEnded`
- `ScenarioController.OnNodeChanged`

### 하이라이트 동작

- 현재 노드 식별자와 같은 `ScenarioNodeView`를 찾아 실행 강조를 적용합니다.
- 강조는 `ScenarioNodeView.SetExecutionHighlighted(true)`로 처리됩니다.
- 시나리오 종료, 플레이 모드 종료, 그래프 불일치 시 강조를 해제합니다.

---

## 5. 관련 보조 클래스

- `ScenarioGraphView`
  - 노드/엣지 생성, 그래프 좌표 변환, 선택 처리 담당
- `ScenarioNodeView`
  - 개별 노드 UI 표현 및 실행 하이라이트 처리 담당
- `ScenarioInspectorView`
  - 선택된 노드의 상세 편집 UI 담당
- `ScenarioNodeSearchWindow`
  - 새 노드 추가용 검색 UI 담당
- `ScenarioGraphEditorData`
  - 노드 배치 위치를 저장하는 사이드카 데이터

---

## 6. 주의사항

- 런타임 하이라이트는 그래프 식별자를 기준으로 매칭하므로, 서로 다른 시나리오를 동시에 열어 두면 현재 실행 중인 그래프와 일치하는 창만 강조됩니다.
- 하이라이트는 편집 상태를 바꾸지 않습니다. 노드 선택과 실행 강조는 독립적으로 유지됩니다.

---

## 관련 문서

- [scenario-graph-spec.md](../requirements/content-definitions/scenario/scenario-graph-spec.md) — 시나리오 노드 구조
- [scenario-authoring-guide.md](../requirements/content-definitions/scenario/scenario-authoring-guide.md) — 시나리오 작성 기준
- [MultiplayerInfrastructure.Scenario.ScenarioController.md](MultiplayerInfrastructure.Scenario.ScenarioController.md) — 런타임 실행 엔진
- [2026-06-29-scenario-graph-editor-runtime-highlight.md](../changes/2026-06-29-scenario-graph-editor-runtime-highlight.md) — 이번 변경 기록