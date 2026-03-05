# Documents

- [일반적인 사항들, General](./general.md): 프로젝트 전반에 걸쳐 알아두어야 할 일반적인 사항들
- [작업 상황 동기화 및 공유](./sync-progress.md): 프로젝트 다운받기, 작업물을 다른 사람과 공유하기
- [온보딩 가이드](./onboarding.md): 프로젝트 시작 시 참고
- [AI 작업 흐름 가이드](./ai-workflow.md): AI를 활용해 작업할 때의 흐름과 주의사항

### MultiplayerInfrastructure:

- [MultiplayerInfrastructure 사용자 가이드](./multiplayer-infrastructure-guide.md): 모듈 전체 시스템 구조 개요 및 주요 사용 패턴
- [Crosshair & Raycast 구현 가이드](./crosshair-raycast-guide.md): 플레이어 크로스헤어, 중앙선 레이캐스트
- [API: Registry](./api-references/MultiplayerInfrastructure.Registry.md): 중앙 레지스트리 등록·조회
- [API: Crosshair & Raycast](./api-references/MultiplayerInfrastructure.UI.Crosshair.md): 크로스헤어 UI 및 레이캐스트 API
- [API: PlayerController](./api-references/MultiplayerInfrastructure.Player.PlayerController.md): 플레이어 시스템 (이동, 인벤토리, 게임모드 등)
- [API: ScenarioController](./api-references/MultiplayerInfrastructure.Scenario.ScenarioController.md): 시나리오 그래프 실행 엔진
- [API: ScenarioEventIdentifierRegistry](./api-references/MultiplayerInfrastructure.Scenario.ScenarioEventIdentifierRegistry.md): 씬 이벤트 핸들러 등록
- [API: InteractableEntity](./api-references/MultiplayerInfrastructure.InteractableEntity.md): 인터랙터블 인터페이스 및 컴포넌트
- [API: ChatService](./api-references/MultiplayerInfrastructure.Chat.ChatService.md): 채팅 전송·커맨드 실행

### for 개발자/AI:

- [상호작용 기능 구현 가이드](./interaction-implementation-guide.md)
- [코딩 스타일 컨벤션](./coding-style-conventions.md)
- [아이템 정의](./item.md)
- [NPC 멀티 인터랙트 & 아이콘 참조 API](./api-references/entities/npc-multi-interact-and-icon-reference.md)

<br />

- [시나리오의 프로그래밍 표현](./scenario-graph.md)
- [시나리오 작성 가이드](./scenario-authoring.md)
- [시나리오 이벤트 레지스트리](./scenario/event-registry.md)
- [시나리오 이벤트-구현 매핑](./scenario/event-mapping.md)

### API 레퍼런스 — 기존:

- [API: Command.ChatCommandExtensions](./api-references/MultiplayerInfrastructure.Command.ChatCommandExtensions.md): /give, /clean 커맨드 확장 이력
- [API: Datapack.DatapackRuntimeService](./api-references/MultiplayerInfrastructure.Datapack.DatapackRuntimeService.md): JSON 기반 주기 커맨드 자동화
- [API: Item.Item](./api-references/MultiplayerInfrastructure.Item.Item.md): 아이템 컴포넌트 구조
- [API: Player.PlayerController.InventoryCommands](./api-references/MultiplayerInfrastructure.Player.PlayerController.InventoryCommands.md): 인벤토리 커맨드 API
- [API: Quest.QuestManager](./api-references/MultiplayerInfrastructure.Quest.QuestManager.md): 퀘스트 관리
- [API: Registry.WaypointAnchor](./api-references/MultiplayerInfrastructure.Registry.WaypointAnchor.md): 웨이포인트 앵커

### 변경 기록:

- [2026-02-16: Interactable/NPC/Icon 구조 개편](./update-notes/2026-02-16-interactable-npc-icon-refactor.md)
