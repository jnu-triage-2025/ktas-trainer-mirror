# <a id="MultiplayerInfrastructure_Player_PlayerController"></a> Class PlayerController

Namespace: [MultiplayerInfrastructure.Player](MultiplayerInfrastructure.Player.md)  
Assembly: Assembly\-CSharp.dll  

PlayerController의 크로스헤어 UI 처리 부분 구현.

CrosshairUIController를 찾아 등록하고,
필요에 따라 크로스헤어 UI의 시각성을 제어합니다.

```csharp
[RequireComponent(typeof(InteractableEntityResolver))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour, IScenarioIdentifiedEntity
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
NetworkBehaviour ← 
[PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

#### Implements

[IScenarioIdentifiedEntity](MultiplayerInfrastructure.Entity.IScenarioIdentifiedEntity.md)

## Fields

### <a id="MultiplayerInfrastructure_Player_PlayerController_HandlingItem"></a> HandlingItem

```csharp
public Item HandlingItem
```

#### Field Value

 [Item](MultiplayerInfrastructure.ItemSystem.Item.md)

### <a id="MultiplayerInfrastructure_Player_PlayerController_canMove"></a> canMove

```csharp
public bool canMove
```

#### Field Value

 bool

## Properties

### <a id="MultiplayerInfrastructure_Player_PlayerController_CameraAttachPoint"></a> CameraAttachPoint

```csharp
public CameraAttachPoint CameraAttachPoint { get; }
```

#### Property Value

 [CameraAttachPoint](MultiplayerInfrastructure.Camera.CameraAttachPoint.md)

### <a id="MultiplayerInfrastructure_Player_PlayerController_CarriedReposable"></a> CarriedReposable

```csharp
public IReposable CarriedReposable { get; }
```

#### Property Value

 [IReposable](MultiplayerInfrastructure.Entity.IReposable.md)

### <a id="MultiplayerInfrastructure_Player_PlayerController_CurrentGamemode"></a> CurrentGamemode

```csharp
public PlayerGamemode CurrentGamemode { get; }
```

#### Property Value

 [PlayerGamemode](MultiplayerInfrastructure.Player.PlayerGamemode.md)

### <a id="MultiplayerInfrastructure_Player_PlayerController_CurrentMoveInputVector"></a> CurrentMoveInputVector

```csharp
public Vector3 CurrentMoveInputVector { get; }
```

#### Property Value

 Vector3

### <a id="MultiplayerInfrastructure_Player_PlayerController_CurrentPlayerModelIdentifier"></a> CurrentPlayerModelIdentifier

```csharp
public string CurrentPlayerModelIdentifier { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Player_PlayerController_DefaultWalkingSpeed"></a> DefaultWalkingSpeed

```csharp
public float DefaultWalkingSpeed { get; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Player_PlayerController_EntityIdentifier"></a> EntityIdentifier

이 PlayerController가 나타내는 엔티티의 전역 식별자.

```csharp
public string EntityIdentifier { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Player_PlayerController_EquipmentSlots"></a> EquipmentSlots

장비 슬롯 데이터에 대한 읽기 전용 접근자 (UI 바인딩용).

```csharp
public IReadOnlyList<EquipmentSlotModelDTO> EquipmentSlots { get; }
```

#### Property Value

 IReadOnlyList<[EquipmentSlotModelDTO](MultiplayerInfrastructure.Player.EquipmentSlotModelDTO.md)\>

### <a id="MultiplayerInfrastructure_Player_PlayerController_InteractionResolver"></a> InteractionResolver

```csharp
public InteractableEntityResolver InteractionResolver { get; }
```

#### Property Value

 [InteractableEntityResolver](MultiplayerInfrastructure.InteractableEntity.InteractableEntityResolver.md)

### <a id="MultiplayerInfrastructure_Player_PlayerController_InteractiveDetector"></a> InteractiveDetector

```csharp
public NearbyInteractablesDetector InteractiveDetector { get; }
```

#### Property Value

 [NearbyInteractablesDetector](MultiplayerInfrastructure.Camera.NearbyInteractablesDetector.md)

### <a id="MultiplayerInfrastructure_Player_PlayerController_InventorySlots"></a> InventorySlots

현재 인벤토리 슬롯을 읽기 전용으로 제공합니다.

```csharp
public IReadOnlyList<InventorySlotModelDTO> InventorySlots { get; }
```

#### Property Value

 IReadOnlyList<InventorySlotModelDTO\>

### <a id="MultiplayerInfrastructure_Player_PlayerController_IsCarryingReposable"></a> IsCarryingReposable

```csharp
public bool IsCarryingReposable { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_IsCursorLocked"></a> IsCursorLocked

```csharp
public bool IsCursorLocked { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_IsIntravenousLineConnectionMode"></a> IsIntravenousLineConnectionMode

```csharp
public bool IsIntravenousLineConnectionMode { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_IsLineConnectionMode"></a> IsLineConnectionMode

```csharp
public bool IsLineConnectionMode { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_IsMovementPositionOverridden"></a> IsMovementPositionOverridden

```csharp
public bool IsMovementPositionOverridden { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_IsMovementSuppressed"></a> IsMovementSuppressed

```csharp
public bool IsMovementSuppressed { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_IsPatientSelectionMode"></a> IsPatientSelectionMode

```csharp
public bool IsPatientSelectionMode { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_IsRidableControlActive"></a> IsRidableControlActive

```csharp
public bool IsRidableControlActive { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_IsRidableExitSuppressed"></a> IsRidableExitSuppressed

```csharp
public bool IsRidableExitSuppressed { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_IsRunning"></a> IsRunning

```csharp
public bool IsRunning { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_IsSpectatingTarget"></a> IsSpectatingTarget

```csharp
public bool IsSpectatingTarget { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_IsSpectator"></a> IsSpectator

```csharp
public bool IsSpectator { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_IsWallSuctionAvailable"></a> IsWallSuctionAvailable

흡인기(WallSuction)와 양커 라인으로 연결되어 있어, 환자에게 흡인기 사용 상호작용을
수행할 수 있는지 여부입니다. 라인 연결/해제 시 <xref href="MultiplayerInfrastructure.Player.PlayerController.SetWallSuctionAvailable(System.Boolean)" data-throw-if-not-resolved="false"></xref> 로 갱신됩니다.

```csharp
public bool IsWallSuctionAvailable { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_PlayerEntity"></a> PlayerEntity

```csharp
public Entity PlayerEntity { get; }
```

#### Property Value

 [Entity](MultiplayerInfrastructure.Entity.Entity.md)

### <a id="MultiplayerInfrastructure_Player_PlayerController_RaycastHasHit"></a> RaycastHasHit

이번 프레임 크로스헤어 중심선 레이캐스트가 어떤 콜라이더에 맞았으면 true

```csharp
public bool RaycastHasHit { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_RaycastHit"></a> RaycastHit

크로스헤어 중심선 레이캐스트의 원시 RaycastHit 결과 (<xref href="MultiplayerInfrastructure.Player.PlayerController.RaycastHasHit" data-throw-if-not-resolved="false"></xref>가 true일 때만 유효)

```csharp
public RaycastHit RaycastHit { get; }
```

#### Property Value

 RaycastHit

### <a id="MultiplayerInfrastructure_Player_PlayerController_RaycastHitObject"></a> RaycastHitObject

레이캐스트에 맞은 GameObject, 맞지 않았으면 null

```csharp
public GameObject RaycastHitObject { get; }
```

#### Property Value

 GameObject

### <a id="MultiplayerInfrastructure_Player_PlayerController_RunningSpeedMultiplier"></a> RunningSpeedMultiplier

```csharp
public float RunningSpeedMultiplier { get; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Player_PlayerController_ScenarioEntityIdentifier"></a> ScenarioEntityIdentifier

이 엔티티의 시나리오 식별자(레지스트리 등록 식별자와 동일). 비어 있을 수 있다.

```csharp
public string ScenarioEntityIdentifier { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Player_PlayerController_ServerRunningSpeedMultiplier"></a> ServerRunningSpeedMultiplier

```csharp
public static float ServerRunningSpeedMultiplier { get; }
```

#### Property Value

 float

### <a id="MultiplayerInfrastructure_Player_PlayerController_UserDisplayName"></a> UserDisplayName

이 PlayerController가 나타내는 플레이어의 DisplayName.

```csharp
public string UserDisplayName { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Player_PlayerController_UserIdentifier"></a> UserIdentifier

이 PlayerController가 나타내는 플레이어의 Identifier(UUID).

```csharp
public string UserIdentifier { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Player_PlayerController_WalkingSpeed"></a> WalkingSpeed

```csharp
public float WalkingSpeed { get; }
```

#### Property Value

 float

## Methods

### <a id="MultiplayerInfrastructure_Player_PlayerController_AlignYawTo_UnityEngine_Vector3_"></a> AlignYawTo\(Vector3\)

```csharp
public void AlignYawTo(Vector3 worldForward)
```

#### Parameters

`worldForward` Vector3

### <a id="MultiplayerInfrastructure_Player_PlayerController_ApplyRunningSpeedMultiplierServer_System_Single_"></a> ApplyRunningSpeedMultiplierServer\(float\)

서버 전체의 달리기 배율을 변경하고, 현재 및 이후 플레이어에게 동기화한다.

```csharp
public static void ApplyRunningSpeedMultiplierServer(float value)
```

#### Parameters

`value` float

### <a id="MultiplayerInfrastructure_Player_PlayerController_ApplyScriptedMove_UnityEngine_Vector3_System_Boolean_"></a> ApplyScriptedMove\(Vector3, bool\)

스크립트 이동 1프레임 분의 변위를 적용한다.
<code class="paramref">horizontalDelta</code> 는 이번 프레임에 이동할 수평 변위(월드 기준, y 무시).
<code class="paramref">applyGravity</code> 가 true면 접지 전까지 중력을 누적 적용한다.
CharacterController.Move 를 사용하므로 velocity 기반 walk 애니메이션이 자동으로 재생된다.

```csharp
public void ApplyScriptedMove(Vector3 horizontalDelta, bool applyGravity)
```

#### Parameters

`horizontalDelta` Vector3

`applyGravity` bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_ApplyStaticObjectDisplaymentForScenario_System_String_"></a> ApplyStaticObjectDisplaymentForScenario\(string\)

시나리오 준비 단계에서 정적 장비를 서버 권위로 설치하고 모든 관찰자에게 표시한다.
아이템 소비·소유자 검증이 필요한 플레이어 상호작용 경로와 달리, 이미 시나리오가 보장한
상태를 복원하는 용도로만 사용한다.

```csharp
public bool ApplyStaticObjectDisplaymentForScenario(string entityIdentifier)
```

#### Parameters

`entityIdentifier` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_Attack"></a> Attack\(\)

```csharp
public void Attack()
```

### <a id="MultiplayerInfrastructure_Player_PlayerController_BeginScriptedMovement"></a> BeginScriptedMovement\(\)

스크립트(시나리오 등)에 의한 이동 제어를 시작한다.
이 동안 입력 기반 이동은 억제되며, 실제 이동은 <xref href="MultiplayerInfrastructure.Player.PlayerController.ApplyScriptedMove(UnityEngine.Vector3%2cSystem.Boolean)" data-throw-if-not-resolved="false"></xref> 로 구동한다.

```csharp
public void BeginScriptedMovement()
```

### <a id="MultiplayerInfrastructure_Player_PlayerController_CanAcceptItem_MultiplayerInfrastructure_ItemSystem_Item_"></a> CanAcceptItem\(Item\)

```csharp
public bool CanAcceptItem(Item item)
```

#### Parameters

`item` [Item](MultiplayerInfrastructure.ItemSystem.Item.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_CancelAttack"></a> CancelAttack\(\)

```csharp
public ActionResult CancelAttack()
```

#### Returns

 [ActionResult](MultiplayerInfrastructure.ItemSystem.ActionResult.md)

### <a id="MultiplayerInfrastructure_Player_PlayerController_CancelUseItem"></a> CancelUseItem\(\)

```csharp
public ActionResult CancelUseItem()
```

#### Returns

 [ActionResult](MultiplayerInfrastructure.ItemSystem.ActionResult.md)

### <a id="MultiplayerInfrastructure_Player_PlayerController_ClearForcedFollowAnchor_UnityEngine_Transform_"></a> ClearForcedFollowAnchor\(Transform\)

```csharp
public void ClearForcedFollowAnchor(Transform anchor = null)
```

#### Parameters

`anchor` Transform

### <a id="MultiplayerInfrastructure_Player_PlayerController_ClearInventory"></a> ClearInventory\(\)

```csharp
public int ClearInventory()
```

#### Returns

 int

### <a id="MultiplayerInfrastructure_Player_PlayerController_CompleteConsumedItemUse_MultiplayerInfrastructure_Player_PlayerController_ItemUseConsumptionReceipt_System_Boolean_"></a> CompleteConsumedItemUse\(ItemUseConsumptionReceipt, bool\)

```csharp
public void CompleteConsumedItemUse(PlayerController.ItemUseConsumptionReceipt receipt, bool accepted)
```

#### Parameters

`receipt` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md).[ItemUseConsumptionReceipt](MultiplayerInfrastructure.Player.PlayerController.ItemUseConsumptionReceipt.md)

`accepted` bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_CountItemInInventory_System_String_"></a> CountItemInInventory\(string\)

```csharp
public int CountItemInInventory(string itemIdentifier)
```

#### Parameters

`itemIdentifier` string

#### Returns

 int

### <a id="MultiplayerInfrastructure_Player_PlayerController_DropHeldItem"></a> DropHeldItem\(\)

```csharp
public void DropHeldItem()
```

### <a id="MultiplayerInfrastructure_Player_PlayerController_EndScriptedMovement"></a> EndScriptedMovement\(\)

스크립트 이동 제어를 종료하고 입력 기반 이동으로 복귀한다.

```csharp
public void EndScriptedMovement()
```

### <a id="MultiplayerInfrastructure_Player_PlayerController_EnterUIOverlayMode"></a> EnterUIOverlayMode\(\)

```csharp
public void EnterUIOverlayMode()
```

### <a id="MultiplayerInfrastructure_Player_PlayerController_ExitUIOverlayMode"></a> ExitUIOverlayMode\(\)

```csharp
public void ExitUIOverlayMode()
```

### <a id="MultiplayerInfrastructure_Player_PlayerController_FindFirstInventoryItem_System_Func_System_String_System_Boolean__"></a> FindFirstInventoryItem\(Func<string, bool\>\)

조건을 만족하는 보유 아이템 식별자를 하나 찾는다. 손에 들고 있는 상태는 요구하지 않는다.

```csharp
public string FindFirstInventoryItem(Func<string, bool> predicate)
```

#### Parameters

`predicate` Func<string, bool\>

#### Returns

 string

### <a id="MultiplayerInfrastructure_Player_PlayerController_GetCraftableRecipes"></a> GetCraftableRecipes\(\)

현재 인벤토리 보유량 기준으로 조합 가능한 레시피 목록을 조합 패널용 DTO로 반환한다.
(등록된 모든 레시피 중, 재료가 충분한 레시피만 포함)

```csharp
public List<InventoryUIView.CraftableRecipeDisplay> GetCraftableRecipes()
```

#### Returns

 List<[InventoryUIView](MultiplayerInfrastructure.UI.InventoryUIView.md).[CraftableRecipeDisplay](MultiplayerInfrastructure.UI.InventoryUIView.CraftableRecipeDisplay.md)\>

### <a id="MultiplayerInfrastructure_Player_PlayerController_LockCursor"></a> LockCursor\(\)

```csharp
public void LockCursor()
```

### <a id="MultiplayerInfrastructure_Player_PlayerController_MoveToPositionPreservingForcedFollowAnchor_UnityEngine_Vector3_"></a> MoveToPositionPreservingForcedFollowAnchor\(Vector3\)

현재 forced-follow 소유자를 변경하지 않고 로컬 플레이어 위치를 안전하게 갱신한다.
다른 시스템이 플레이어를 고정하고 있을 수 있는 표현 복귀 경로에서 사용한다.

```csharp
public void MoveToPositionPreservingForcedFollowAnchor(Vector3 position)
```

#### Parameters

`position` Vector3

### <a id="MultiplayerInfrastructure_Player_PlayerController_OnSpawnServer_FishNet_Connection_NetworkConnection_"></a> OnSpawnServer\(NetworkConnection\)

Called on the server after a spawn message for this object has been sent to clients.
Useful for sending remote calls or data to clients.

```csharp
public override void OnSpawnServer(NetworkConnection connection)
```

#### Parameters

`connection` NetworkConnection

Connection the object is being spawned for.

### <a id="MultiplayerInfrastructure_Player_PlayerController_OnStartClient"></a> OnStartClient\(\)

Called on the client after initializing this object.

```csharp
public override void OnStartClient()
```

### <a id="MultiplayerInfrastructure_Player_PlayerController_OnStartServer"></a> OnStartServer\(\)

Called on the server after initializing this object.
SyncTypes modified before or during this method will be sent to clients in the spawn message.

```csharp
public override void OnStartServer()
```

### <a id="MultiplayerInfrastructure_Player_PlayerController_OnStopClient"></a> OnStopClient\(\)

Called on the client before deinitializing this object.

```csharp
public override void OnStopClient()
```

### <a id="MultiplayerInfrastructure_Player_PlayerController_OnStopServer"></a> OnStopServer\(\)

Called on the server before deinitializing this object.

```csharp
public override void OnStopServer()
```

### <a id="MultiplayerInfrastructure_Player_PlayerController_OnValidate"></a> OnValidate\(\)

```csharp
protected override void OnValidate()
```

### <a id="MultiplayerInfrastructure_Player_PlayerController_OrderInteractsByDisplayPriority_System_Collections_Generic_List_MultiplayerInfrastructure_InteractableEntity_IInteract__"></a> OrderInteractsByDisplayPriority\(List<IInteract\>\)

우선순위를 명시한 항목만 앞으로 이동한다. 같은 우선순위의 기존 감지 순서를 보존하여
주기적인 Physics 조회 결과가 선택 항목을 불필요하게 흔들지 않게 한다.

```csharp
public static void OrderInteractsByDisplayPriority(List<IInteract> interacts)
```

#### Parameters

`interacts` List<[IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)\>

### <a id="MultiplayerInfrastructure_Player_PlayerController_RefreshInteractableHintsNow"></a> RefreshInteractableHintsNow\(\)

```csharp
public void RefreshInteractableHintsNow()
```

### <a id="MultiplayerInfrastructure_Player_PlayerController_RemoveAllOfItemFromInventory_System_String_"></a> RemoveAllOfItemFromInventory\(string\)

```csharp
public int RemoveAllOfItemFromInventory(string itemIdentifier)
```

#### Parameters

`itemIdentifier` string

#### Returns

 int

### <a id="MultiplayerInfrastructure_Player_PlayerController_RemoveItemFromInventory_System_String_System_Int32_"></a> RemoveItemFromInventory\(string, int\)

```csharp
public int RemoveItemFromInventory(string itemIdentifier, int count)
```

#### Parameters

`itemIdentifier` string

`count` int

#### Returns

 int

### <a id="MultiplayerInfrastructure_Player_PlayerController_RequestItemization_MultiplayerInfrastructure_Entity_IItemizableWorldEntity_"></a> RequestItemization\(IItemizableWorldEntity\)

설치형 엔티티 회수를 PlayerController의 네트워크 경로로 중계한다.
설치체 프리팹의 NetworkBehaviour 직렬화 상태와 무관하게, 소유 플레이어의 ServerRpc가
서버에서 대상 식별자를 검증한 후 회수를 수행한다.

```csharp
public bool RequestItemization(IItemizableWorldEntity target)
```

#### Parameters

`target` [IItemizableWorldEntity](MultiplayerInfrastructure.Entity.IItemizableWorldEntity.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_RequestPlaceHeldEntityPrefab_System_String_"></a> RequestPlaceHeldEntityPrefab\(string\)

소유 클라이언트의 로컬 인벤토리와 서버의 월드 스폰을 예약/확정 프로토콜로 연결한다.
인벤토리는 서버 PlayerController 복제본에 존재하지 않으므로 서버에서 직접 소비하지 않는다.

```csharp
public void RequestPlaceHeldEntityPrefab(string itemIdentifier)
```

#### Parameters

`itemIdentifier` string

### <a id="MultiplayerInfrastructure_Player_PlayerController_RequestPlaceHeldEntityPreset_System_String_System_String_"></a> RequestPlaceHeldEntityPreset\(string, string\)

```csharp
public void RequestPlaceHeldEntityPreset(string itemIdentifier, string entityPresetIdentifier)
```

#### Parameters

`itemIdentifier` string

`entityPresetIdentifier` string

### <a id="MultiplayerInfrastructure_Player_PlayerController_RequestSetPlayerModel_System_String_"></a> RequestSetPlayerModel\(string\)

```csharp
public void RequestSetPlayerModel(string modelIdentifier)
```

#### Parameters

`modelIdentifier` string

### <a id="MultiplayerInfrastructure_Player_PlayerController_Reset"></a> Reset\(\)

```csharp
protected override void Reset()
```

### <a id="MultiplayerInfrastructure_Player_PlayerController_SetForcedFollowAnchor_UnityEngine_Transform_"></a> SetForcedFollowAnchor\(Transform\)

```csharp
public void SetForcedFollowAnchor(Transform anchor)
```

#### Parameters

`anchor` Transform

### <a id="MultiplayerInfrastructure_Player_PlayerController_SetIntravenousLineConnectionMode_System_Boolean_System_Boolean_"></a> SetIntravenousLineConnectionMode\(bool, bool\)

```csharp
public void SetIntravenousLineConnectionMode(bool enabled, bool showActionbarHint = true)
```

#### Parameters

`enabled` bool

`showActionbarHint` bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_SetLineConnectionMode_System_Boolean_System_Boolean_"></a> SetLineConnectionMode\(bool, bool\)

Generic line-connection API. The serialized IV-named backing field is
retained to preserve existing scene and prefab data.

```csharp
public void SetLineConnectionMode(bool enabled, bool showActionbarHint = true)
```

#### Parameters

`enabled` bool

`showActionbarHint` bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_SetMovementSuppressed_UnityEngine_Object_System_Boolean_"></a> SetMovementSuppressed\(Object, bool\)

```csharp
public void SetMovementSuppressed(Object owner, bool suppressed)
```

#### Parameters

`owner` Object

`suppressed` bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_SetPatientSelectionMode_System_Boolean_"></a> SetPatientSelectionMode\(bool\)

```csharp
public void SetPatientSelectionMode(bool enabled)
```

#### Parameters

`enabled` bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_SetRidableExitSuppressed_UnityEngine_Object_System_Boolean_"></a> SetRidableExitSuppressed\(Object, bool\)

특정 시스템이 플레이어 입력을 점유하는 동안 같은 키를 사용하는 탑승 해제를 막는다.
소유자별로 관리하므로 한 시스템의 해제가 다른 시스템의 억제 상태를 제거하지 않는다.

```csharp
public void SetRidableExitSuppressed(Object owner, bool suppressed)
```

#### Parameters

`owner` Object

`suppressed` bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_SetWallSuctionAvailable_System_Boolean_"></a> SetWallSuctionAvailable\(bool\)

```csharp
public void SetWallSuctionAvailable(bool available)
```

#### Parameters

`available` bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_SyncPlayerTagsToObservers"></a> SyncPlayerTagsToObservers\(\)

서버에서 현재 플레이어의 태그 목록을 모든 옵저버에게 동기화합니다.

```csharp
public void SyncPlayerTagsToObservers()
```

### <a id="MultiplayerInfrastructure_Player_PlayerController_SyncQuestStateFlagsToObservers"></a> SyncQuestStateFlagsToObservers\(\)

서버에서 현재 플레이어의 퀘스트 상태 플래그 풀을 모든 옵저버에게 동기화합니다.
상호작용 노출 판정이 각 피어에서 로컬로 이뤄지므로, 서버 기록만으로는 화면에 반영되지 않습니다.

```csharp
public void SyncQuestStateFlagsToObservers()
```

### <a id="MultiplayerInfrastructure_Player_PlayerController_TeleportToServer_UnityEngine_Vector3_"></a> TeleportToServer\(Vector3\)

서버에서 이 플레이어를 지정 위치로 즉시 텔레포트합니다.
CharacterController 를 일시 비활성화한 뒤 위치를 설정하여 콜라이더 충돌을 우회합니다.
변경 사항은 ObserversRpc 를 통해 모든 클라이언트에 전파됩니다.

```csharp
public void TeleportToServer(Vector3 position)
```

#### Parameters

`position` Vector3

### <a id="MultiplayerInfrastructure_Player_PlayerController_TriggerAttack"></a> TriggerAttack\(\)

```csharp
public void TriggerAttack()
```

### <a id="MultiplayerInfrastructure_Player_PlayerController_TriggerUseItem"></a> TriggerUseItem\(\)

```csharp
public void TriggerUseItem()
```

### <a id="MultiplayerInfrastructure_Player_PlayerController_TryAddItemToInventory_MultiplayerInfrastructure_ItemSystem_Item_"></a> TryAddItemToInventory\(Item\)

```csharp
public bool TryAddItemToInventory(Item item)
```

#### Parameters

`item` [Item](MultiplayerInfrastructure.ItemSystem.Item.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_TryAddItemToInventory_MultiplayerInfrastructure_ItemSystem_Item_MultiplayerInfrastructure_ItemSystem_Item__"></a> TryAddItemToInventory\(Item, out Item\)

```csharp
public bool TryAddItemToInventory(Item item, out Item leftover)
```

#### Parameters

`item` [Item](MultiplayerInfrastructure.ItemSystem.Item.md)

`leftover` [Item](MultiplayerInfrastructure.ItemSystem.Item.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_TryApplyStaticObjectDisplayment_System_String_System_String_System_Int32_"></a> TryApplyStaticObjectDisplayment\(string, string, int\)

정적 오브젝트의 표시(설치/적용)를 시도합니다. 요구 아이템 식별자/수량을 함께 전달하며,
서버 승인 후 요청자 클라이언트에서 해당 아이템을 인벤토리에서 소비합니다.
아이템 소비가 필요 없으면 <code class="paramref">requiredItemIdentifier</code> 를 비우면 됩니다.

```csharp
public void TryApplyStaticObjectDisplayment(string entityIdentifier, string requiredItemIdentifier = null, int consumeCount = 0)
```

#### Parameters

`entityIdentifier` string

`requiredItemIdentifier` string

`consumeCount` int

### <a id="MultiplayerInfrastructure_Player_PlayerController_TryClearStaticObjectDisplaymentAndGrantItem_System_String_System_String_"></a> TryClearStaticObjectDisplaymentAndGrantItem\(string, string\)

서버 전역 표시 상태를 해제하고 요청자에게 아이템 하나를 지급합니다.
맵에 사전 배치된 설치물을 회수할 때 사용합니다.

```csharp
public void TryClearStaticObjectDisplaymentAndGrantItem(string entityIdentifier, string itemIdentifier)
```

#### Parameters

`entityIdentifier` string

`itemIdentifier` string

### <a id="MultiplayerInfrastructure_Player_PlayerController_TryConsumeItemUse_System_String_"></a> TryConsumeItemUse\(string\)

아이템 사용 1회를 소비합니다. 내구도 변화가 활성화된 아이템은 수량 대신 내구도를
변경하며, 내구도가 0에 도달한 경우에만 슬롯에서 제거합니다.

```csharp
public bool TryConsumeItemUse(string itemIdentifier)
```

#### Parameters

`itemIdentifier` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_TryConsumeItemUse_System_String_MultiplayerInfrastructure_Player_PlayerController_ItemUseConsumptionReceipt__"></a> TryConsumeItemUse\(string, out ItemUseConsumptionReceipt\)

```csharp
public bool TryConsumeItemUse(string itemIdentifier, out PlayerController.ItemUseConsumptionReceipt receipt)
```

#### Parameters

`itemIdentifier` string

`receipt` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md).[ItemUseConsumptionReceipt](MultiplayerInfrastructure.Player.PlayerController.ItemUseConsumptionReceipt.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_TryCraftRecipe_System_String_"></a> TryCraftRecipe\(string\)

지정한 결과 식별자의 레시피를 1회 조합한다.
재료가 충분하면 인벤토리에서 재료를 소비하고, 생성된 결과 아이템 인스턴스를 반환한다.
(결과 아이템은 인벤토리에 추가하지 않는다 — 호출자(조합 패널)가 커서로 pickup 처리)
재료가 부족하거나 결과 생성에 실패하면 null 을 반환한다.

조합 결과물은 커서(held item)로 지급된 뒤 인벤토리 슬롯에 배치되는데, 이 배치 경로
(InventoryUIView 의 slot.SetItem/Push)는 <xref href="MultiplayerInfrastructure.Player.PlayerController.TryAddItemToInventory(MultiplayerInfrastructure.ItemSystem.Item)" data-throw-if-not-resolved="false"></xref> 를 우회하므로
획득 훅(<xref href="MultiplayerInfrastructure.ItemSystem.Item.OnGet(MultiplayerInfrastructure.Player.PlayerController)" data-throw-if-not-resolved="false"></xref>)이 생략된다. 이를 보완하려고 조합 "시점"에 OnGet 을
호출하면 아이템이 아직 인벤토리에 없는 상태에서 획득 신호가 먼저 발행되는 문제가 있다.
따라서 여기서는 결과 아이템에 <xref href="MultiplayerInfrastructure.ItemSystem.Item.DeferredOnGet" data-throw-if-not-resolved="false"></xref> 플래그만 설정하고,
실제 인벤토리 진입 시점(InventoryUIController 가 슬롯 배치 감지)에 OnGet 이 발행되도록 한다.
MedicalItem 은 OnGet 에서 시나리오 게이팅/퀘스트 신호(sig.&lt;id&gt;, sig.click_&lt;id&gt;)를 발행한다.

```csharp
public Item TryCraftRecipe(string outputItemIdentifier)
```

#### Parameters

`outputItemIdentifier` string

#### Returns

 [Item](MultiplayerInfrastructure.ItemSystem.Item.md)

### <a id="MultiplayerInfrastructure_Player_PlayerController_TryDropCarriedReposable_MultiplayerInfrastructure_Entity_IReposable__"></a> TryDropCarriedReposable\(out IReposable\)

```csharp
public bool TryDropCarriedReposable(out IReposable dropped)
```

#### Parameters

`dropped` [IReposable](MultiplayerInfrastructure.Entity.IReposable.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_TryDropItemInFront_MultiplayerInfrastructure_ItemSystem_Item_"></a> TryDropItemInFront\(Item\)

```csharp
public bool TryDropItemInFront(Item itemData)
```

#### Parameters

`itemData` [Item](MultiplayerInfrastructure.ItemSystem.Item.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_TryPickUpReposable_MultiplayerInfrastructure_Entity_IReposable_"></a> TryPickUpReposable\(IReposable\)

```csharp
public bool TryPickUpReposable(IReposable reposable)
```

#### Parameters

`reposable` [IReposable](MultiplayerInfrastructure.Entity.IReposable.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_TryPickupStaticPlacedItem_System_String_"></a> TryPickupStaticPlacedItem\(string\)

정적 아이템 획득을 시도합니다. <xref href="MultiplayerInfrastructure.ItemSystem.StaticPlacedItem.Interact(UnityEngine.Transform)" data-throw-if-not-resolved="false"></xref> 에서 호출됩니다.

```csharp
public void TryPickupStaticPlacedItem(string entityIdentifier)
```

#### Parameters

`entityIdentifier` string

### <a id="MultiplayerInfrastructure_Player_PlayerController_TryPickupWorldItem_MultiplayerInfrastructure_ItemSystem_ItemObject_"></a> TryPickupWorldItem\(ItemObject\)

```csharp
public bool TryPickupWorldItem(ItemObject itemObject)
```

#### Parameters

`itemObject` [ItemObject](MultiplayerInfrastructure.ItemSystem.ItemObject.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_TryPickupWorldItem_System_String_"></a> TryPickupWorldItem\(string\)

```csharp
public bool TryPickupWorldItem(string entityIdentifier)
```

#### Parameters

`entityIdentifier` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_TrySpawnWorldItem_MultiplayerInfrastructure_ItemSystem_Item_UnityEngine_Vector3_UnityEngine_Vector3_"></a> TrySpawnWorldItem\(Item, Vector3, Vector3\)

설치형 엔티티 등을 아이템으로 되돌릴 때, 지정 위치에 획득 가능한 월드 아이템을 생성한다.

```csharp
public bool TrySpawnWorldItem(Item itemData, Vector3 position, Vector3 throwForce)
```

#### Parameters

`itemData` [Item](MultiplayerInfrastructure.ItemSystem.Item.md)

`position` Vector3

`throwForce` Vector3

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Player_PlayerController_UnlockCursor"></a> UnlockCursor\(\)

```csharp
public void UnlockCursor()
```

### <a id="MultiplayerInfrastructure_Player_PlayerController_Update_Input"></a> Update\_Input\(\)

```csharp
public void Update_Input()
```

### <a id="MultiplayerInfrastructure_Player_PlayerController_UseItem"></a> UseItem\(\)

```csharp
public void UseItem()
```

