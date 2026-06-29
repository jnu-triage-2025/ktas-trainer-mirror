# 2026-06-29 Scenario Graph Editor Runtime Highlight

## 변경 개요
- 인게임 서버에서 실행 중인 시나리오의 현재 노드를 Scenario Graph Editor에서 즉시 강조하도록 연결했습니다.
- `ScenarioController.OnScenarioStarted`, `OnScenarioEnded`, `OnNodeChanged`를 구독하여 현재 그래프와 실행 노드가 일치할 때만 하이라이트를 표시합니다.
- `ScenarioNodeView`에 실행 상태용 시각 스타일을 추가하여 선택 상태와 구분되는 테두리/배경 강조를 제공합니다.

## 문서화 대상
- Assets/Modules/MultiplayerInfrastructure/Editor/Scenario/ScenarioGraphEditor/ScenarioGraphEditor.cs
- Assets/Modules/MultiplayerInfrastructure/Editor/Scenario/ScenarioGraphEditor/ScenarioNodeView.cs
- Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/ScenarioController.cs

## 문서화 메모
- Scenario Graph Editor의 런타임 연동과 노드 하이라이트는 기존 요구사항/레퍼런스에 별도로 정리되어 있지 않던 기능입니다.
- 이번 반영으로 편집기에서 재생 중인 시나리오의 현재 노드를 즉시 추적할 수 있게 되었습니다.