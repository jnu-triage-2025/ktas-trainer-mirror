---
title: "MultiplayerInfrastructure/TriageTrainer 구현체 문서화 TODO"
domain: "module-features"
progress: "3-implemented"
flags: []
---

## 개요

MultiplayerInfrastructure, TriageTrainer 구현 코드 전반을 기능 단위로 요구사항 문서화하기 위한 작업 목록이다. 이 목록은 API 레퍼런스와 실제 구현 코드를 함께 확인하여 작성하며, 비개발직군 독자가 이해할 수 있는 설명을 우선한다.

## 상세

### 공통 작업 규칙

- [x] 각 항목은 requirements 문서 1개 이상으로 작성한다.
- [x] 문서 front matter는 requirements 템플릿 규칙(title/domain/progress/flags)을 따른다.
- [x] 각 문서는 개요, 상세, 기술적 세부 사항, 참조 섹션을 포함한다.
- [x] 참조에 대응 API 문서를 반드시 연결한다.

### 기능별 문서화 대상

- [x] Registry 기능군 (등록/조회/프리로드/웨이포인트)
- [x] Player 기능군 (코어 루프/인벤토리 커맨드/태그)
- [x] Interaction 기능군 (IInteractable/IInteract/Handler)
- [x] Item 기능군 (공통 아이템 + Triage 아이템 정의)
- [x] Scenario 기능군 (공통 실행 엔진 + Triage 확장)
- [x] Quest 기능군
- [x] Chat/Command 기능군
- [x] Datapack 기능군
- [x] Session(LAN) 기능군
- [x] UI 기능군 (Controllers/VisualElements)
- [x] Patient 기능군 (모니터/환자 모델)

### 현재 턴(1차) 수행 계획

- [x] module-features 루트 README 작성
- [x] MultiplayerInfrastructure 핵심 7개 문서 작성
- [x] TriageTrainer 핵심 2개 문서 작성
- [ ] requirements 색인 링크 보강

### 완료 문서(기능 중심 경로)

- [x] registry/registry-requirements.md
- [x] registry/registry-preloader-authoring-requirements.md
- [x] registry/waypoints-requirements.md
- [x] player/player-controller-requirements.md
- [x] player/inventory-commands-requirements.md
- [x] player/player-tag-service-requirements.md
- [x] interaction/interactable-entity-requirements.md
- [x] item/item-system-requirements.md
- [x] item/triage-item-definitions-requirements.md
- [x] scenario/scenario-controller-requirements.md
- [x] scenario/scenario-event-registry-requirements.md
- [x] scenario/scenario-serialize-support-requirements.md
- [x] scenario/scenario-runtime-validation-requirements.md
- [x] scenario/triage-scenario-event-bootstrap-requirements.md
- [x] scenario/triage-scenario-module-requirements.md
- [x] quest/quest-manager-requirements.md
- [x] chat-command/chat-service-requirements.md
- [x] chat-command/command-extensions-requirements.md
- [x] datapack/datapack-runtime-requirements.md
- [x] session/session-lan-requirements.md
- [x] ui/ui-controllers-requirements.md
- [x] ui/ui-visual-elements-requirements.md
- [x] patient/patient-monitor-requirements.md
- [x] patient/triage-patient-models-requirements.md

## 기술적 세부 사항

작업 분할 단위는 기능 단위이며, 구현 클래스가 다수인 경우 하나의 기능 문서에서 묶어 설명한다. 모듈 차이는 디렉토리 대신 front matter의 `domain` 필드(`module-features.multiplayer-infrastructure`, `module-features.triage-trainer`)로 구분한다.

## 참조

- [api:multiplayer-infrastructure-overview](../../api-references/architecture/multiplayer-infrastructure-overview.md)
- [change:2026-03-26-registry-preloader-validation-tooling](../../changes/2026-03-26-registry-preloader-validation-tooling.md)
