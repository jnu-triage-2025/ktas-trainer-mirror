---
title: "오버월드 웨이포인트/스폰포인트 초기화 기능 요구사항"
domain: "module-features.triage-trainer"
progress: "3-implemented"
flags: []
---

## 개요

오버월드 씬에서 자주 사용하는 진입 웨이포인트와 공용 스폰 포인트를 빠르게 생성/정리하는 기능이다. 비개발 운영자도 에디터 도구만으로 기준 위치를 재설정할 수 있어야 한다.

## 상세

- 도구는 건물 입구/치료실 입구 웨이포인트와 공용 스폰 포인트를 한 번에 생성해야 한다.
- 재실행 시 기존 생성물을 정리하고 최신 입력값으로 다시 생성해야 한다.
- 생성된 스폰 포인트는 플레이어 스폰 시스템에서 즉시 사용할 수 있도록 런타임 등록되어야 한다.
- 생성물은 운영자가 안전하게 되돌릴 수 있도록 에디터 Undo를 지원해야 한다.
- 식별자와 좌표는 도구 창에서 수정 가능해야 하며, 기본값 복원 버튼을 제공해야 한다.

## 기술적 세부 사항

- 생성 루트는 `GeneratedByOverworldGameObjectInitializerEditor` 마커 컴포넌트로 식별한다.
- 웨이포인트 생성은 `WaypointAnchor`를 부착하고 내부 `identifier`를 반영해 Registry 조회 키와 일치시킨다.
- 스폰 포인트 생성은 `OverworldSpawnPoint`를 부착하고 `RegistryType.SpawnPoint` 및 `PlayerSpawnPointRegistry` 양쪽에 등록한다.
- 생성/삭제는 에디터 모드에서 `Undo` API를 사용하고, 플레이 모드에서는 일반 `Destroy`를 사용한다.

## 참조

- [api:TriageTrainer.Utils.OverworldGameObjectInitializer](../../api-references/TriageTrainer.Utils.OverworldGameObjectInitializer.md)
- [api:MultiplayerInfrastructure.Registry.WaypointAnchor](../../api-references/MultiplayerInfrastructure.Registry.WaypointAnchor.md)
- [api:MultiplayerInfrastructure.Registry](../../api-references/MultiplayerInfrastructure.Registry.md)
