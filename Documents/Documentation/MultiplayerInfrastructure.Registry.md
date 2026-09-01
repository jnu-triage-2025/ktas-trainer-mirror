# <a id="MultiplayerInfrastructure_Registry"></a> Namespace MultiplayerInfrastructure.Registry

### Classes

 [EntityDescriptor](MultiplayerInfrastructure.Registry.EntityDescriptor.md)

Registry.Entity 저장소에 등록되는 단일 엔티티 설명자입니다.

- Identifier: 서버/모든 클라이언트에서 동일해야 하는 전역 고유 식별자
- EntityType: 엔티티 종류
- GameObject: 로컬 프로세스에서 이 엔티티를 나타내는 실제 게임 오브젝트
- OwnerUserIdentifier / ClientId: 플레이어 엔티티일 때 연결 정보

 [EntityId](MultiplayerInfrastructure.Registry.EntityId.md)

씬 고정 엔티티에 사용할 안정적인 식별자를 생성/보정하는 유틸리티입니다.

- 비어 있으면 현재 GameObject 이름 기반 접두어 + GUID를 생성
- 이미 값이 있으면 그대로 유지

 [EntityPresetDefinition](MultiplayerInfrastructure.Registry.EntityPresetDefinition.md)

엔티티 프리셋 정의: 하나의 프리팹에 사전 설정(EntityType 폴백/표시명/네트워크 여부)과 식별자를 부여해 저장한 단위.

하위 오브젝트는 <xref href="MultiplayerInfrastructure.Registry.EntityPresetDefinition.ChildReferences" data-throw-if-not-resolved="false"></xref> 로 표현하며, 각 항목은 <b>다른 EntityPreset 의 식별자</b>를 가리킨다.
(원본 프리팹의 Transform 자식을 경로로 가리키지 않는다.) 스폰 시 루트 프리셋과 각 하위 프리셋이 독립적으로
인스턴스화되며, unwrap 옵션에 따라 하위를 루트의 자식으로 둘지 또는 루트와 동일 계층(형제 루트)에 둘지 결정한다.

 [Registry](MultiplayerInfrastructure.Registry.Registry.md)

 [RegistryGlobalKeys](MultiplayerInfrastructure.Registry.RegistryGlobalKeys.md)

 [RegistryPreloadEntitySO](MultiplayerInfrastructure.Registry.RegistryPreloadEntitySO.md)

 [RegistryPreloadIconSpriteSO](MultiplayerInfrastructure.Registry.RegistryPreloadIconSpriteSO.md)

 [RegistryPreloadInteractableEntitySO](MultiplayerInfrastructure.Registry.RegistryPreloadInteractableEntitySO.md)

 [RegistryPreloadNpcSO](MultiplayerInfrastructure.Registry.RegistryPreloadNpcSO.md)

 [RegistryPreloadPlayerCharacterSO](MultiplayerInfrastructure.Registry.RegistryPreloadPlayerCharacterSO.md)

 [RegistryPreloadProblemSetSO](MultiplayerInfrastructure.Registry.RegistryPreloadProblemSetSO.md)

 [RegistryPreloadScenarioGraphSO](MultiplayerInfrastructure.Registry.RegistryPreloadScenarioGraphSO.md)

 [RegistryPreloadUIControllerSO](MultiplayerInfrastructure.Registry.RegistryPreloadUIControllerSO.md)

 [RegistryPreloadWaypointSO](MultiplayerInfrastructure.Registry.RegistryPreloadWaypointSO.md)

 [RegistryPreloaderController](MultiplayerInfrastructure.Registry.RegistryPreloaderController.md)

RegistryPreloader는 게임 시작 시점에 필요한 레지스트리 항목을 미리 로드할 수 있도록 합니다.

레지스트리에 등록할 개별 항목들에 Registry 로직이 있음에도 별도로 작성한 것은,
이들 로직은 Unity Life Cycle 과정 내에서 호출되도록 되어있어, 하이어라키에 존재하지 않으면
레지스터되지 않기 때문입니다.

실제로는 ScriptableObject를 로드하여 등록합니다:
특정한 씬에 의존하거나, 관리가 적절히 되지 않을 수 있을 우려를 제거하기 위함입니다.

 [WaypointAnchor](MultiplayerInfrastructure.Registry.WaypointAnchor.md)

 [WaypointSet](MultiplayerInfrastructure.Registry.WaypointSet.md)

순서가 있는 waypoint 묶음이다. <xref href="MultiplayerInfrastructure.Registry.WaypointSet._waypoints" data-throw-if-not-resolved="false"></xref> 목록의 인덱스가 이동
순서이며, 각 waypoint는 독립된 waypoint로도 계속 레지스트리에 등록된다.

### Structs

 [EntityPresetChildReference](MultiplayerInfrastructure.Registry.EntityPresetChildReference.md)

엔티티 프리셋이 스폰 시 함께 생성할 <b>하위 엔티티 프리셋</b> 참조.

핵심 설계: 하위 오브젝트는 (컨테이너 프리팹의 자식 Transform 경로가 아니라) <b>다른 EntityPreset 의 식별자</b>로
참조한다. 즉 하위가 프리팹이더라도 "원본 프리팹"이 아니라, 사전 설정이 끝난 <b>하위 EntityPreset</b>(이미 레지스트리에
등록된 프리셋)을 선택한다. 이렇게 하면 핵심 로직이 항상 EntityPreset 단위로 동작하며, 중첩 NetworkObject 가 한
프리팹에 nested 되는 구조(FishNet 직렬화 문제의 원인)를 피할 수 있다 — 각 프리셋은 런타임에 독립적으로 인스턴스화된다.

이 타입은 프리셋 정의(EntityPresetDefinition) 및 등록 SO(EntityPresetRegistryRequirement)에 직렬화되어,
"환자 + 환자침대를 결합 배치하고 스폰" 같은 구성을 <b>비런타임(에디터)에서 사전 설정·검증</b>할 수 있게 한다.

 [EntityRegistryRequirement](MultiplayerInfrastructure.Registry.EntityRegistryRequirement.md)

 [IconSpriteRegistryRequirement](MultiplayerInfrastructure.Registry.IconSpriteRegistryRequirement.md)

 [ItemRegistryRequirement](MultiplayerInfrastructure.Registry.ItemRegistryRequirement.md)

 [NPCRegistryRequirements](MultiplayerInfrastructure.Registry.NPCRegistryRequirements.md)

 [PlayerCharacterRegistryRequirement](MultiplayerInfrastructure.Registry.PlayerCharacterRegistryRequirement.md)

 [ProblemSetRegistryRequirement](MultiplayerInfrastructure.Registry.ProblemSetRegistryRequirement.md)

 [ScenarioGraphRegistryRequirement](MultiplayerInfrastructure.Registry.ScenarioGraphRegistryRequirement.md)

 [UIControllerRegistryRequirement](MultiplayerInfrastructure.Registry.UIControllerRegistryRequirement.md)

UI 컨트롤러의 등록 요구 사항입니다.
identifier를 비워두면 컨트롤러 타입의 FullName을 자동으로 키로 사용합니다.

 [WaypointRequirements](MultiplayerInfrastructure.Registry.WaypointRequirements.md)

### Interfaces

 [IEntityPresetParentLinkReceiver](MultiplayerInfrastructure.Registry.IEntityPresetParentLinkReceiver.md)

엔티티 프리셋 스폰 시, 하위 프리셋 인스턴스가 <b>부모(루트) 프리셋의 런타임 식별자</b>를 전달받기 위한 인터페이스.

하위 참조(EntityPresetChildReference)에서 부모 연결이 요청되면(linkChildToParent),
엔진은 하위 스폰 직후 이 메서드를 호출해 "이 하위는 어떤 부모와 결합되어야 하는가"를 식별자로 알려준다.
구체적인 결합 의미(예: 환자침대가 그 부모 환자를 자기 위에 누인다)는 프로젝트 측 구현이 결정한다.
엔진은 "부모 식별자를 전달" 하는 일반 메커니즘만 제공한다(특정 엔티티 종류에 의존하지 않음).

서버 컨텍스트에서 호출되는 것을 전제로 한다(프리셋 스폰은 서버에서 수행). 구현체는 보통 이 식별자를
SyncVar 등에 기록해 전 피어로 복제·결합한다.

 [ISpawnedEntityIdentifierReceiver](MultiplayerInfrastructure.Registry.ISpawnedEntityIdentifierReceiver.md)

엔티티 프리셋 스폰 시 인스턴스에 식별자를 주입받기 위한 인터페이스.

`Registry.TrySpawnEntityPreset(...)` 가 Instantiate 직후, 인스턴스의 자가 등록
(예: NetworkBehaviour.OnStartClient) 이 일어나기 전에 이 메서드를 호출하여,
하나의 프리셋(또는 하위 프리셋 참조)에서 인스턴스별 식별자(예: patient_a / bed_a)를
부여할 수 있게 한다.

### Enums

 [EntityType](MultiplayerInfrastructure.Registry.EntityType.md)

Registry.Entity 저장소에 등록되는 런타임 엔티티 종류입니다.
UI, 서비스, 전역 상태값은 포함하지 않습니다.

 [RegistryType](MultiplayerInfrastructure.Registry.RegistryType.md)

