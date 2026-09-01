# <a id="MultiplayerInfrastructure_InteractableEntity"></a> Namespace MultiplayerInfrastructure.InteractableEntity

### Namespaces

 [MultiplayerInfrastructure.InteractableEntity.Definitions](MultiplayerInfrastructure.InteractableEntity.Definitions.md)

### Classes

 [Interactable](MultiplayerInfrastructure.InteractableEntity.Interactable.md)

 [InteractableEntityResolver](MultiplayerInfrastructure.InteractableEntity.InteractableEntityResolver.md)

 [ItemSubmissionDefinition](MultiplayerInfrastructure.InteractableEntity.ItemSubmissionDefinition.md)

아이템 제출 상호작용의 요구 사항/표시/완료 신호를 담는 직렬화 가능한 설정.

이 정의는 두 경로에서 값을 얻을 수 있다:
 1) 프리셋 기본값: <xref href="MultiplayerInfrastructure.InteractableEntity.ItemSubmissionInteractable" data-throw-if-not-resolved="false"></xref> 컴포넌트(또는 스폰되는 프리팹)에 인스펙터로 사전 설정된다.
 2) 그래프 노드 오버라이드: 시나리오 그래프 노드가 런타임에 요구 아이템/완료 신호를 덮어쓴다.

완료 처리는 서버 세션 전역 신호(<xref href="MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals" data-throw-if-not-resolved="false"></xref>)로 이루어진다.

 [ItemSubmissionInteractable](MultiplayerInfrastructure.InteractableEntity.ItemSubmissionInteractable.md)

"요구 아이템을 들고 있으면 상호작용하여 제출할 수 있는" 독립 Interactable 컴포넌트.

사용 시나리오(예): 의사 NPC 또는 접수대에 부착 → 플레이어가 상호작용 → 제출 UI 가 열리고
요구 아이템을 넣고 제출 → 인벤토리에서 소모 → 서버 세션 전역 신호(sig.*)를 올린다.

설정 우선순위: 인스펙터의 프리셋 기본값(<xref href="MultiplayerInfrastructure.InteractableEntity.ItemSubmissionInteractable._definition" data-throw-if-not-resolved="false"></xref>)을 기본으로 하되,
시나리오 그래프 노드가 런타임에 요구 아이템/완료 신호/활성 상태를 덮어쓸 수 있다
(<xref href="MultiplayerInfrastructure.InteractableEntity.ItemSubmissionInteractable.ApplyDefinitionOverride(MultiplayerInfrastructure.InteractableEntity.ItemSubmissionDefinition)" data-throw-if-not-resolved="false"></xref>, <xref href="MultiplayerInfrastructure.InteractableEntity.ItemSubmissionInteractable.SetEnabled(System.Boolean)" data-throw-if-not-resolved="false"></xref>).

이 컴포넌트는 <xref href="MultiplayerInfrastructure.Registry.RegistryType.InteractableEntity" data-throw-if-not-resolved="false"></xref> 와 <xref href="MultiplayerInfrastructure.Registry.RegistryType.Entity" data-throw-if-not-resolved="false"></xref> 에
식별자로 등록되어, 그래프 노드가 식별자로 이 인스턴스를 찾아 사전 설정할 수 있게 한다.
프리팹으로 만들어 <xref href="MultiplayerInfrastructure.Registry.EntityPresetDefinition" data-throw-if-not-resolved="false"></xref> 으로 등록하면, EntityPresetSpawn 노드나
전용 ItemSubmissionConfig 노드로 스폰/사전설정할 수 있다.

 [NearestOnlyInteractUtility](MultiplayerInfrastructure.InteractableEntity.NearestOnlyInteractUtility.md)

 [PlayerInteractableModel](MultiplayerInfrastructure.InteractableEntity.PlayerInteractableModel.md)

### Structs

 [ItemRequirement](MultiplayerInfrastructure.InteractableEntity.ItemRequirement.md)

아이템 제출에서 요구되는 단일 아이템을 식별자와 수량으로 표현한다.
인벤토리 아이템은 <code>CurrentIdentifier</code> 와 <code>CurrentStackCount</code> 로 식별되므로,
이 구조체는 그와 동일한 (identifier, count) 쌍으로 요구 사항을 정의한다.

### Interfaces

 [IAdditionalInteractProvider](MultiplayerInfrastructure.InteractableEntity.IAdditionalInteractProvider.md)

기존 <xref href="MultiplayerInfrastructure.InteractableEntity.IInteractable" data-throw-if-not-resolved="false"></xref> 컨트롤러에 같은 GameObject의 기능 컴포넌트가
조건부 상호작용 항목을 보탤 때 사용한다.

 [IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)

IInteractable 모든 상호작용 가능 객체의 컨트롤러에서 구현해야합니다. 이후에 InteractableEntityResolver에 의해 활용됩니다.

 [IInteractDisplayIcons](MultiplayerInfrastructure.InteractableEntity.IInteractDisplayIcons.md)

하나의 상호작용 항목에 여러 표시 아이콘을 제공할 때 구현합니다.
각 아이콘은 힌트 UI에서 독립된 정사각형 슬롯에 원본 비율을 유지해 표시됩니다.

 [IInteractDisplayPriority](MultiplayerInfrastructure.InteractableEntity.IInteractDisplayPriority.md)

주변 상호작용 목록에서 기본 감지 순서보다 먼저 표시되어야 하는 항목이 선택적으로 구현합니다.
값이 클수록 먼저 표시되며, 같은 값인 항목의 기존 감지 순서는 유지됩니다.

 [IInteractToggleable](MultiplayerInfrastructure.InteractableEntity.IInteractToggleable.md)

상호작용을 런타임에 활성/비활성 전환할 수 있는 Interactable 이 구현한다.
시나리오 그래프 노드(예: NPCControl / ItemSubmissionConfig)가 이 인터페이스로
개별 Interactable 의 활성 상태를 제어한다.

 [IInteractable](MultiplayerInfrastructure.InteractableEntity.IInteractable.md)

IInteractable 모든 상호작용 가능 객체의 컨트롤러에서 구현해야합니다. 이후에 InteractableEntityResolver에 의해 활용됩니다.

 [IInteractorConditional](MultiplayerInfrastructure.InteractableEntity.IInteractorConditional.md)

 [ILocalInteractionFocus](MultiplayerInfrastructure.InteractableEntity.ILocalInteractionFocus.md)

로컬 선택 상태에 따른 표시 효과를 위한 선택적 규약입니다.

 [INearestOnlyInteract](MultiplayerInfrastructure.InteractableEntity.INearestOnlyInteract.md)

같은 종류의 후보가 감지 범위에 여러 개 들어왔을 때 가장 가까운 상호작용 하나만 노출해야 하는 항목입니다.
빈 그룹 키를 반환하면 현재 상태에서는 거리 필터를 적용하지 않습니다.

 [IQuestPresentationTarget](MultiplayerInfrastructure.InteractableEntity.IQuestPresentationTarget.md)

