---
title: "PlayerTagService 기능 요구사항"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

## 개요

PlayerTagService는 플레이어 상태를 태그로 표현하고 분기 조건에 활용하는 기능이다. 사용자 관점에서는 역할 분기, 조건부 진행, 이벤트 대상 필터링의 기준 데이터다.

## 상세

- 태그 저장은 플레이어 UUID 기준으로 분리 관리되어야 한다.
- 태그 저장은 플레이어뿐 아니라 엔티티 식별자 기준으로도 관리 가능해야 한다.
- 태그 추가/제거/교체는 서버 권한에서만 반영되어야 한다.
- 태그 변경 시 관전자에게 상태가 동기화되어야 한다.
- 플레이어 이탈/정리 시 태그 데이터 누수가 없어야 한다.

## 기술적 세부 사항

- 저장소는 RegistryType.PlayerTag를 사용하며 값은 문자열 목록이다.
- 서버 변경 가드는 `IsServerMutationAllowed()` 기준으로 적용된다.
- 엔티티 공통 처리를 위해 `AddTagToIdentifier`, `RemoveTagFromIdentifier`, `ChangeTagForIdentifier`, `GetTagsByIdentifier`, `HasTagOnIdentifier` API를 제공한다.
- `ReplaceTags`는 스냅샷 동기화 시 중복/빈값 필터링을 수행한다.
- `SyncPlayerTagsToObservers()` 연동으로 네트워크 반영을 보장한다.

## 참조

- [api:MultiplayerInfrastructure.Tag.PlayerTagService](../../api-references/MultiplayerInfrastructure.Tag.PlayerTagService.md)
- [api:scenario-graph-spec](../content-definitions/scenario/scenario-graph-spec.md)
- [api:event-mapping](../content-definitions/scenario/event-mapping.md)
