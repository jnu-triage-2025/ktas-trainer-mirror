---
title: "UI Controllers 공통 기능 요구사항"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

## 개요

UI Controllers는 채팅, 인벤토리, 퀘스트, ESC 메뉴 등 화면 단위 패널의 수명주기와 입력 연결을 담당하는 기능이다. 사용자에게는 패널 간 일관된 열기/닫기 경험을 제공해야 한다.

## 상세

- UI 컨트롤러는 공통 기반(UIControllerABC)에서 초기화 규칙을 공유해야 한다.
- 오버레이 UI는 입력 잠금/해제와 연동되어 플레이 조작 충돌이 없어야 한다.
- ProblemSheet UI는 오버레이 스택 규약을 준수하여 표시 중에는 월드 상호작용이 차단되어야 한다.
- 채팅/인벤토리/퀘스트/그래픽 설정 등 각 패널은 독립 제어 가능해야 한다.
- 시나리오 및 상호작용 힌트 UI는 플레이 상태에 따라 즉시 갱신되어야 한다.
- 닫혀 있는 패널과 포인터 입력이 필요 없는 HUD는 문서 전체의 포인터 히트테스트를 놓아야 한다. UIDocument는 화면 전체를 덮는 루트를 가지므로, 이를 지키지 않으면 sortingOrder 가 더 낮은 문서가 클릭과 휠을 전혀 받지 못한다.
- ProblemSheet는 단일 문제 모드와 전체 세트 모드를 모두 지원해야 하며, 전체 세트 모드에서는 정답 시 다음 문제로 진행할 수 있어야 한다.

## 기술적 세부 사항

- 주요 구현체: ChatUIController, InventoryUIController, QuestUIController, CrosshairUIController, DialoguePanelUIController, HotbarUIController, GameEscapeMenuUIController 등.
- `IUIOverlay` 인터페이스로 오버레이 패널 공통 동작을 정의한다.
- PlayerController의 UIOverlayStack과 결합해 중첩 오버레이를 관리한다.
- 닫힌 패널은 `SetDocumentVisible(document, false)`로, 표시는 유지하되 입력을 받지 않는 HUD는 `SetDocumentRootPickingEnabled(document, false)`로 문서 하위 트리 전체의 `PickingMode`를 해제한다. 패널 내부 요소만 `display:none` 으로 감추면 문서 루트와 래퍼가 남아 아래 문서의 입력을 가로챈다.
- ProblemSheetController는 문제셋 시작 인덱스 지정(`startIndex`)과 단일 문제 모드(`singleProblemMode`)를 지원한다.
- 마지막 문제가 정답 처리되면 닫기 버튼 레이블이 "완료"로 전환되어야 한다.

## 참조

- [api:MultiplayerInfrastructure.Player.PlayerController](../../api-references/MultiplayerInfrastructure.Player.PlayerController.md)
- [api:MultiplayerInfrastructure.UI.Crosshair](../../api-references/MultiplayerInfrastructure.UI.Crosshair.md)
- [api:requirements-ui](../ui/README.md)
