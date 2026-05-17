---
title: "TriageTrainer 수액 라인 연결 상호작용 기능 요구사항"
domain: "module-features.triage-trainer"
progress: "3-implemented"
flags: []
---

## 개요

이 문서는 TriageTrainer의 수액 라인(IV line) 연결 상호작용 요구사항을 정리한다. 목표는 플레이어가 필요한 물품을 사용해 두 연결 포인트 사이 수액 라인을 생성/해제하고, 그 상태가 모든 참여자에게 일관되게 반영되도록 하는 것이다.

## 상세

- 수액 라인 연결 포인트는 `IInteractable` 기반 다중 상호작용을 제공해야 한다.
- 각 포인트는 연결 시작, 연결 완료, 연결 해제 3종 액션을 제공해야 한다.
- 연결 시작은 플레이어가 필수 아이템을 인벤토리에 보유한 경우에만 가능해야 한다.
- 연결 완료는 시작 포인트와 다른 포인트에서만 허용되어야 하며, 이미 연결된 포인트에는 중복 연결을 허용하지 않아야 한다.
- 연결이 성공하면 라인 오브젝트가 생성되고, 양쪽 포인트는 연결 상태를 즉시 반영해야 한다.
- 연결 해제 시 관련 라인 오브젝트를 제거하고 상호작용 힌트를 즉시 갱신해야 한다.
- 포인트 식별자는 씬 내에서 중복되지 않아야 하며, 자동 생성 정책을 제공해야 한다.
- 라인 시각화는 기본 곡선 렌더링을 제공하고, 필요 시 물리 시뮬레이션 기반 처짐/충돌 보정을 지원해야 한다.

## 기술적 세부 사항

- 포인트 컴포넌트는 `IntravenousLineConnectionPoint`를 사용한다.
  - 상호작용 식별자
    - `intravenous_line_connect_mode_start`
    - `intravenous_line_connect_here`
    - `intravenous_line_disconnect`
  - `SphereCollider`(Trigger) 기반 감지 반경을 보장한다.
- 서비스 컴포넌트는 `IntravenousLineConnectionService`를 사용한다.
  - 플레이어별 pending 상태(`PendingConnectionContext`)를 보관한다.
  - 필수 아이템 목록(`_requiredItems`, `_requiredItemIdentifiers`)을 기반으로 소모를 수행한다.
  - 기본 필수 식별자는 `IntravenousSet.Identifier`를 포함한다.
- 라인 런타임은 `IntravenousLineConnectionRuntime`으로 구성한다.
  - 비물리 모드: 분석적 곡선 렌더링
  - 물리 모드: 세그먼트 시뮬레이션 + 거리 제약 + 월드 충돌 보정
- 네트워크 오브젝트가 있는 경우 동일 `NetworkObject` 내부 포인트 간 연결은 금지한다.
- 플레이어의 연결 모드 상태는 `PlayerController`의 IV 연결 모드 플래그와 상호작용 힌트 갱신 경로를 따른다.

## 참조

- [api:TriageTrainer.Entity.IntravenousLine](../../api-references/entities/intravenous-line-reference.md)
- [api:MultiplayerInfrastructure.Player.PlayerController](../../api-references/MultiplayerInfrastructure.Player.PlayerController.md)
- [api:MultiplayerInfrastructure.InteractableEntity](../../api-references/MultiplayerInfrastructure.InteractableEntity.md)
