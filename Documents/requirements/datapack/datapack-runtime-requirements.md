---
title: "DatapackRuntimeService 기능 요구사항"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

## 개요

DatapackRuntimeService는 JSON 기반 설정으로 주기 명령과 이벤트 핸들러를 런타임에 주입하는 기능이다. 사용자에게는 반복 운영 작업을 데이터로 자동화하는 기능이다.

## 상세

- 데이터팩은 `packId` 기준으로 등록/중복검사/해제되어야 한다.
- 런타임 파일은 `GameLogs`와 같은 위치의 `DataPacks` 폴더 아래에서 `*.datapack.json`을 재귀적으로 식별해야 한다.
- 역직렬화에 실패한 파일도 UI 식별 목록에는 표시해야 한다.
- IntroScene에서 활성 팩 순서를 편집할 수 있어야 하며 위 항목이 높은 우선순위를 가진다.
- 세션 시작 게임 규칙과 명령어 별칭을 데이터로 정의할 수 있어야 한다.
- 주기 명령은 interval 기반으로 반복 실행되어야 하며 즉시 실행 옵션을 지원해야 한다.
- 이벤트 핸들러 주입은 기존 핸들러 백업/복원 정책을 가져야 한다.
- 부트스트랩 목록(TextAsset) 기반 자동 로딩을 지원해야 한다.
- 세션 기본 설정 파일에서 호스트 주소, 포트, 세션 이름, 기본 데이터팩 목록을 읽어야 한다.

## 기술적 세부 사항

- `RegisterDatapackFromJson`, `UnregisterDatapack`가 핵심 제어 API다.
- 커맨드 실행은 `ChatService.TryExecuteSystemCommand`를 통일 경로로 사용한다.
- `ScenarioEventIdentifierRegistry`와 직접 연동해 이벤트 식별자를 덮어쓰거나 복원한다.
- 구조 검증 기준은 `Resources/Schema/datapack.scheme.json`이다.

## 참조

- [api:MultiplayerInfrastructure.Datapack.DatapackRuntimeService](../../api-references/MultiplayerInfrastructure.Datapack.DatapackRuntimeService.md)
- [api:MultiplayerInfrastructure.Chat.ChatService](../../api-references/MultiplayerInfrastructure.Chat.ChatService.md)
- [api:MultiplayerInfrastructure.Scenario.ScenarioEventIdentifierRegistry](../../api-references/MultiplayerInfrastructure.Scenario.ScenarioEventIdentifierRegistry.md)
