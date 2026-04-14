---
title: "Scenario 런타임 실행/검증 요구사항"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

## 개요

Scenario 런타임 실행/검증 기능은 운영자가 채팅 명령으로 시나리오를 대상 플레이어에게 실행하고, 실행 실패 원인을 즉시 확인할 수 있도록 하는 기능이다. 이 기능은 훈련 진행 중 시나리오 전환을 안정적으로 수행하기 위한 운영 핵심 경로다.

## 상세

- 시스템은 시나리오 실행 명령을 서버 권한에서만 처리해야 한다.
- 대상 플레이어 선택자(`@s`, `@a`, `@n`, `fish:<id>`)를 지원해 운영자가 실행 대상을 명확히 지정할 수 있어야 한다.
- 시나리오 식별자 미입력, 대상 미해결, 미등록 시나리오 등 주요 실패 사유를 사용자 메시지로 반환해야 한다.
- 대상 플레이어별로 시나리오 실행 RPC를 전달해 클라이언트에서 실제 그래프 시작이 이루어져야 한다.
- 실행 진입점은 현재 구현된 커맨드 체인을 기준으로 문서화되어야 하며, 코드에 없는 런너 개념을 전제로 하면 안 된다.

## 기술적 세부 사항

- 현재 코드베이스 기준 실행 체인:
  - `CommandDefinition_Scenario.Execute(...)`
  - `ChatService.TryDispatchScenario(...)`
  - `ChatService.TargetRunScenario(...)`
  - `ScenarioController.Instance.StartScenario(...)`
- `TryDispatchScenario`는 `IsServer` 여부와 `RegistryType.ScenarioGraph` 등록 여부를 먼저 검증한다.
- 대상 해석은 `CommandDefinition_Scenario.TryResolveTargets`에서 수행하며, 중복 연결은 `ClientId` 기준으로 제거된다.
- 클라이언트 실행 단계(`TargetRunScenario`)에서 시나리오 그래프 조회 실패 또는 `ScenarioController.Instance == null`인 경우 경고 로그를 남기고 중단한다.
- 구현 상 별도 `ScenarioCommandRunner` 클래스는 존재하지 않으므로, 운영 검증 기준은 위 커맨드+채팅 서비스 경로를 표준으로 본다.

## 참조

- [api:MultiplayerInfrastructure.Chat.ChatService](../../api-references/MultiplayerInfrastructure.Chat.ChatService.md)
- [api:MultiplayerInfrastructure.Scenario.ScenarioController](../../api-references/MultiplayerInfrastructure.Scenario.ScenarioController.md)
- [api:MultiplayerInfrastructure.Scenario.ScenarioEventIdentifierRegistry](../../api-references/MultiplayerInfrastructure.Scenario.ScenarioEventIdentifierRegistry.md)
- [api:scenario-graph-spec](../content-definitions/scenario/scenario-graph-spec.md)
