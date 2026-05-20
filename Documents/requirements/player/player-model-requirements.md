---
title: "PlayerModel 추가/변경 기능 요구사항"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

## 개요

플레이어 모델 기능은 접속 시 기본 모델을 자동으로 적용하고, 런타임 중 모델 식별자(ID)로 시각 모델을 교체하는 기능이다. 사용자 관점에서는 캐릭터 외형이 일관되게 보이고, 변경 즉시 모든 참가자에게 동일하게 반영되어야 한다.

## 상세

- 시스템은 플레이어별 모델 ID를 관리해야 하며, 서버 권한으로만 변경을 확정해야 한다.
- 모델 변경 시 기존 AttachPoint 하위 모델은 제거되고, 새 모델 프리팹이 동일 지점에 재배치되어야 한다.
- 모델 변경 결과는 네트워크 전체에 동기화되어 관전자/다른 클라이언트도 동일 모델을 봐야 한다.
- 서버 접속 시 기본 모델 ID가 설정되어 있으면 최초 한 번 자동 할당되어야 한다.
- 모델 등록 데이터는 ID-프리팹 매핑으로 관리되며, 잘못된 항목(빈 ID, null 프리팹, 인터페이스 미구현)은 검증/경고 대상이어야 한다.

## 기술적 세부 사항

- `PlayerController.Model` partial이 모델 상태 필드(`_playerModelIdentifier`, `_currentPlayerModelIdentifier`)와 변경 API를 제공한다.
- 클라이언트 요청 진입점은 `RequestSetPlayerModel`, 서버 적용 진입점은 `ApplyPlayerModelByIdentifierServer`다.
- 모델 오브젝트 장착은 `PlayerCharacterModelAttachPoint.ReplaceAttachedModel(...)`가 담당하며, 교체 전 자식 오브젝트를 모두 정리한다.
- 네트워크 동기화는 `SyncVar<string>`와 OnChange 구독으로 처리한다.
- 등록 저장소는 `RegistryType.PlayerModel`이며, 값은 `GameObject` 또는 `IPlayerCharacterModelObject`를 구현한 `Component`를 허용한다.

## 참조

- [api:MultiplayerInfrastructure.Player.PlayerModel](../../api-references/MultiplayerInfrastructure.Player.PlayerModel.md)
- [api:TriageTrainer.MultiplayerInfrastructureSupports.RegisteringMultiplayerInfrastructureSupport.PlayerModel](../../api-references/TriageTrainer.MultiplayerInfrastructureSupports.RegisteringMultiplayerInfrastructureSupport.PlayerModel.md)
- [api:MultiplayerInfrastructure.Registry](../../api-references/MultiplayerInfrastructure.Registry.md)
