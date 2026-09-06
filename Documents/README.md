# Documents

- [일반적인 사항들, General](./working-guide/general.md): 프로젝트 전반에 걸쳐 알아두어야 할 일반적인 사항들
- [작업 상황 동기화 및 공유](./working-guide/sync-working-progress.md): 프로젝트 다운받기, 작업물을 다른 사람과 공유하기
- [온보딩 가이드](./working-guide/onboarding.md): 프로젝트 시작 시 참고
- [AI 작업 흐름 가이드](./working-guide/ai-workflow.md): AI를 활용해 작업할 때의 흐름과 주의사항
- [Comment Documentation](./Documentation/toc.yml): DocFX가 C# XML 문서 주석에서 생성한 API 색인

### MultiplayerInfrastructure:

- [MultiplayerInfrastructure 사용자 가이드](./api-references/architecture/multiplayer-infrastructure-overview.md): 모듈 전체 시스템 구조 개요 및 주요 사용 패턴
- [Crosshair & Raycast 요구사항](./requirements/gameplay/interaction/crosshair-raycast-spec.md): 플레이어 크로스헤어, 중앙선 레이캐스트 기능 요구사항
- [API: Registry](./api-references/MultiplayerInfrastructure.Registry.md): 중앙 레지스트리 등록·조회
- [API: Crosshair & Raycast](./api-references/MultiplayerInfrastructure.UI.Crosshair.md): 크로스헤어 UI 및 레이캐스트 API
- [API: PlayerController](./api-references/MultiplayerInfrastructure.Player.PlayerController.md): 플레이어 시스템 (이동, 인벤토리, 게임모드 등)
- [API: ScenarioController](./api-references/MultiplayerInfrastructure.Scenario.ScenarioController.md): 시나리오 그래프 실행 엔진
- [API: Scenario Graph Editor](./api-references/MultiplayerInfrastructure.Editor.ScenarioGraphAuthoringWindow.md): 시나리오 그래프 작성/저장/재생 하이라이트 편집기
- [API: ScenarioEventIdentifierRegistry](./api-references/MultiplayerInfrastructure.Scenario.ScenarioEventIdentifierRegistry.md): 씬 이벤트 핸들러 등록
- [API: Scenario SerializeSupport](./api-references/MultiplayerInfrastructure.Scenario.SerializeSupport.md): 시나리오 JSON 검증/역직렬화/변환 파이프라인
- [API: InteractableEntity](./api-references/MultiplayerInfrastructure.InteractableEntity.md): 인터랙터블 인터페이스 및 컴포넌트
- [API: ChatService](./api-references/MultiplayerInfrastructure.Chat.ChatService.md): 채팅 전송·커맨드 실행
- [API: PlayerTagService](./api-references/MultiplayerInfrastructure.Tag.PlayerTagService.md): 플레이어 태그 저장/동기화 서비스

### for 개발자/AI:

- [상호작용 기능 요구사항](./requirements/gameplay/interaction/interaction-feature-spec.md)
- [모듈 문서화 커버리지 요구사항](./requirements/traceability/module-documentation-coverage-spec.md)
- [모듈 문서화 갭 인벤토리](./requirements/traceability/module-doc-gap-inventory.md)
- [코딩 스타일 컨벤션](./working-guide/coding-style-conventions.md)
- [아이템 정의](./item.md)
- [NPC 멀티 인터랙트 & 아이콘 참조 API](./api-references/entities/npc-multi-interact-and-icon-reference.md)

<br />

- [데디케이티드 서버 (헤드리스 서버)](./guide/DedicatedServer.md): 서버 전용 빌드 산출과 커맨드라인 실행 방법
- [시나리오 그래프 노드 전체 스펙](./guide/ScenarioGraph.md): 모든 ScenarioGraph 노드 타입의 필드/동작 레퍼런스
- [시나리오의 프로그래밍 표현](./requirements/content-definitions/scenario/scenario-graph-spec.md)
- [시나리오 작성 가이드](./requirements/content-definitions/scenario/scenario-authoring-guide.md)
- [시나리오 이벤트 레지스트리](./requirements/content-definitions/scenario/event-registry.md)
- [시나리오 이벤트-구현 매핑](./requirements/content-definitions/scenario/event-mapping.md)

### API 레퍼런스 — 기존:

- [API: Command.ChatCommandExtensions](./api-references/MultiplayerInfrastructure.Command.ChatCommandExtensions.md): /give, /clean 커맨드 확장 이력
- [API: Datapack.DatapackRuntimeService](./api-references/MultiplayerInfrastructure.Datapack.DatapackRuntimeService.md): JSON 기반 주기 커맨드 자동화
- [API: Item.Item](./api-references/MultiplayerInfrastructure.Item.Item.md): 아이템 컴포넌트 구조
- [API: Player.PlayerController.InventoryCommands](./api-references/MultiplayerInfrastructure.Player.PlayerController.InventoryCommands.md): 인벤토리 커맨드 API
- [API: Quest.QuestManager](./api-references/MultiplayerInfrastructure.Quest.QuestManager.md): 퀘스트 관리
- [API: Registry.WaypointAnchor](./api-references/MultiplayerInfrastructure.Registry.WaypointAnchor.md): 웨이포인트 앵커
- [API: TriageScenarioEventBootstrap](./api-references/TriageTrainer.Scenario.TriageScenarioEventBootstrap.md): TriageTrainer 이벤트 부트스트랩
- [API: PatientMonitor](./api-references/TriageTrainer.Entity.PatientMonitor.md): 환자 모니터(ECG) 렌더링/제어

### 변경 기록:

- [2026-02-16: Interactable/NPC/Icon 구조 개편](./changes/2026-02-16-interactable-npc-icon-refactor.md)
- [2026-03-26: Registry Preloader Validation 도구 개편](./changes/2026-03-26-registry-preloader-validation-tooling.md)
- [2026-06-29: Scenario Graph Editor runtime highlight](./changes/2026-06-29-scenario-graph-editor-runtime-highlight.md)
- [2026-09-06: 인터렉션 레지스트리와 가시성 체계 도입](./changes/2026-09-06-interaction-registry-visibility.md)
- [2026-08-03: MPPM 메모리 최적화](./changes/2026-08-03-mppm-memory-optimization.md)
- [2026-08-29: 데디케이티드 서버 빌드](./changes/2026-08-29-dedicated-server-build.md)
