# <a id="MultiplayerInfrastructure_InteractableEntity_Definitions"></a> Namespace MultiplayerInfrastructure.InteractableEntity.Definitions

### Classes

 [LootableItemInteractHandler](MultiplayerInfrastructure.InteractableEntity.Definitions.LootableItemInteractHandler.md)

월드에 드롭된 아이템(<xref href="MultiplayerInfrastructure.ItemSystem.ItemObject" data-throw-if-not-resolved="false"></xref>)을 플레이어가 획득할 수 있도록 합니다.

필요할 때 별도로 부착할 수 있는 레거시 상호작용 핸들러입니다.
NearbyInteractablesDetector 가 같은 GameObject의 Collider를 통해 감지하며,
플레이어가 상호작용(E 키)하면 Interact()가 호출됩니다.

