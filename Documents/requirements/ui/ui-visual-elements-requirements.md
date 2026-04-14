---
title: "UI VisualElements 공통 기능 요구사항"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

## 개요

UI VisualElements는 UI Toolkit 기반 재사용 컴포넌트 계층이다. 사용자에게는 화면별로 동일한 표현 규칙과 상호작용 피드백을 제공하는 것이 목표다.

## 상세

- 채팅, 크로스헤어, 다이얼로그, 핫바, 인벤토리, 퀘스트 요소는 재사용 가능한 요소 단위로 구성되어야 한다.
- 요소는 입력 상태와 데이터 변경에 따라 즉시 시각 상태를 갱신해야 한다.
- 힌트/퀘스트/키설정 등 반복 항목은 리스트 요소 컴포넌트로 분리되어야 한다.

## 기술적 세부 사항

- 주요 구현체: ChatPanelElement, CrosshairElement, DialogueElement, HotbarControl, InventoryUIViewElement, QuestPanelElement, QuestPreviewHudElement, InteractableObjectHintList 계열.
- 컨트롤러 계층이 데이터 바인딩을, VisualElement 계층이 렌더링을 담당하는 분리 구조를 유지한다.

## 참조

- [api:MultiplayerInfrastructure.UI.Crosshair](../../api-references/MultiplayerInfrastructure.UI.Crosshair.md)
- [api:MultiplayerInfrastructure.Quest.QuestManager](../../api-references/MultiplayerInfrastructure.Quest.QuestManager.md)
- [api:requirements-ui](../ui/README.md)
