using FishNet.Object;
using FishNet.Connection;
using MultiplayerInfrastructure.ItemSystem;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;
using MultiplayerInfrastructure.Entity;
using UnityEngine;
using MultiplayerInfrastructure.Logging;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    private const string RapidInfuserFlowTag = "RapidInfuserFlow";
    private void LogRapidInfuser(string message, bool warning = false)
    {
      string full = $"[PlayerRapidInfuser] player='{name}' owner={IsOwner} server={IsServerStarted} client={IsClientStarted} {message}";
      GameLogService.Write(warning ? GameLogCategory.Misc : GameLogCategory.Interaction, full, RapidInfuserFlowTag);
      if (warning) Debug.LogWarning(full, this); else Debug.Log(full, this);
    }
    [SerializeField] private bool attackTriggered = false;
    [SerializeField] private bool useItemTriggered = false;
    private Entity.Entity _attackTarget;
    private Entity.Entity _useItemTarget;

    [Header("Held Item Viewmodel")]
    [SerializeField] private bool _enableViewmodel = true;
    [SerializeField] private Vector3 _viewmodelLocalPosition = new Vector3(0.35f, -0.35f, 0.65f);
    [SerializeField] private Vector3 _viewmodelLocalEuler = new Vector3(10f, -25f, 5f);
    [SerializeField] private Vector3 _viewmodelLocalScale = Vector3.one;
    [SerializeField] private string _viewmodelLayerName = "Viewmodel";

    private Transform _viewmodelRoot;
    private GameObject _viewmodelInstance;
    private ItemSystem.Item _viewmodelItem;
    private Coroutine _viewmodelAnim;

    void Start_Item()
    {
      if (_hotbarUI == null)
        _hotbarUI = Registry.Registry.Get<HotbarUIController>(RegistryType.UI, Registry.Registry.TypeKey<HotbarUIController>());

      if (_inventoryUI == null)
        _inventoryUI = Registry.Registry.Get<InventoryUIController>(RegistryType.UI, Registry.Registry.TypeKey<InventoryUIController>());

      if (_hotbarUI != null)
        _hotbarUI.OnSelectedSlotChanged += ResolveHandledItem;

      if (_inventoryUI != null)
        _inventoryUI.OnItemAtSelectedSlotChanged += ResolveHandledItem;
      ResolveHandledItem();

      if (IsOwner)
        EnsureViewmodelRoot();
    }

    /// <summary>
    /// 파괴 시 UI 이벤트 구독을 해제한다.
    /// 구독 해제를 누락하면 파괴된 플레이어 오브젝트로의 dangling 핸들러가 남아
    /// 이후 슬롯 변경 이벤트에서 예외가 발생한다.
    /// </summary>
    void OnDestroy_Item()
    {
      if (_hotbarUI != null)
        _hotbarUI.OnSelectedSlotChanged -= ResolveHandledItem;

      if (_inventoryUI != null)
        _inventoryUI.OnItemAtSelectedSlotChanged -= ResolveHandledItem;
    }

    /// <summary>
    /// Usage Sequence:
    /// called TriggerAttack/TriggerUseItem from other part
    /// -> triggered by Trigger~
    /// -> trigger resolved when Update_Item called in Update loop
    /// </summary>

    void Update_Item()
    {
      // 설치체 회수는 일반 공격 입력 경로(UI/상호작용 처리에서 소비될 수 있음)에 의존하지 않는다.
      // Update_Raycast 직후 실행되므로 이번 프레임의 카메라 조준 대상을 즉시 처리한다.
      if (Input.GetMouseButtonDown(0) && TryItemizeRaycastTarget())
      {
        attackTriggered = false;
        return;
      }

      ResolveHandledItemTriggered();
    }

    private void ResolveHandledItemTriggered()
    {
      if (attackTriggered)
      {
        attackTriggered = false;
        // 아이템 핸들러가 Cancelled 를 반환한 경우에만 기본 공격을 생략한다.
        // (Success/Passed 는 기본 동작을 계속 수행)
        if (InvokeAttack() != ActionResult.Cancelled)
          Attack();
      }

      if (useItemTriggered)
      {
        useItemTriggered = false;
        if (InvokeUseItem() != ActionResult.Cancelled)
          UseItem();
      }
    }

    private void ResolveHandledItem()
    {
      // 손에 든 아이템이 바뀌면 아이템 소지 조건부 상호작용(예: 특정 아이템을 든 경우에만 노출되는
      // IInteract)의 힌트가 즉시 재평가되어야 하므로, 변경 여부를 추적해 갱신을 트리거한다.
      var previousHandlingItem = HandlingItem;

      ResolveHandledItemCore();

      if (!ReferenceEquals(previousHandlingItem, HandlingItem))
        RefreshInteractableHintsNow();
    }

    private void ResolveHandledItemCore()
    {
      if (_slots == null || _slots.Count == 0)
      {
        HandlingItem = null;
        return;
      }

      if (_hotbarUI == null)
        _hotbarUI = Registry.Registry.Get<HotbarUIController>(RegistryType.UI, Registry.Registry.TypeKey<HotbarUIController>());

      int selectedIndex = _hotbarUI != null ? _hotbarUI.SelectedSlot : 0;
      if (selectedIndex < 0 || selectedIndex >= _slots.Count)
      {
        HandlingItem = null;
        return;
      }

      var slot = _slots[selectedIndex];
      var instance = slot?.ItemInstance;
      if (slot == null || slot.IsEmpty || instance == null)
      {
        HandlingItem = null;
        ClearViewmodel();
        return;
      }

      HandlingItem = instance;
      RefreshViewmodel();
    }

    public void TriggerAttack()
    {
      attackTriggered = true;
    }

    public void TriggerUseItem()
    {
      useItemTriggered = true;
    }

    private ActionResult InvokeAttack()
    {
      if (TryItemizeRaycastTarget())
        return ActionResult.Cancelled;

      if (HandlingItem == null)
        return ActionResult.Passed;
      _attackTarget = RaycastTargetEntity();
      return HandlingItem.OnAttack(this, _attackTarget);
    }

    private bool TryItemizeRaycastTarget()
    {
      var hit = RaycastHitObject;
      var itemizable = hit?.GetComponentInParent<IItemizableWorldEntity>();
      LogRapidInfuser($"raycast hit={(hit == null ? "<null>" : hit.name)} itemizable={(itemizable == null ? "<null>" : itemizable.ItemizationEntityIdentifier)}");
      bool result = itemizable != null && itemizable.RequestItemization(this);
      if (itemizable != null) LogRapidInfuser($"raycast itemization result={result}");
      return result;
    }

    /// <summary>
    /// 설치형 엔티티 회수를 PlayerController의 네트워크 경로로 중계한다.
    /// 설치체 프리팹의 NetworkBehaviour 직렬화 상태와 무관하게, 소유 플레이어의 ServerRpc가
    /// 서버에서 대상 식별자를 검증한 후 회수를 수행한다.
    /// </summary>
    public bool RequestItemization(IItemizableWorldEntity target)
    {
      LogRapidInfuser($"RequestItemization target={(target == null ? "<null>" : target.ItemizationEntityIdentifier)}");
      if (target is not Component targetComponent)
      { LogRapidInfuser("RequestItemization rejected: target is not Component", true); return false; }

      string entityIdentifier = target.ItemizationEntityIdentifier ?? string.Empty;
      Vector3 targetPosition = targetComponent.transform.position;

      if (!IsSpawned || IsServerStarted)
      {
        bool result = target.TryItemizeOnServer(this);
        LogRapidInfuser($"RequestItemization direct-server result={result}");
        return result;
      }

      CmdRequestItemization(entityIdentifier, targetPosition);
      LogRapidInfuser($"RequestItemization RPC sent entity='{entityIdentifier}' position={targetPosition}");
      return true;
    }

    [ServerRpc]
    private void CmdRequestItemization(
      string entityIdentifier,
      Vector3 targetPosition,
      NetworkConnection sender = null)
    {
      LogRapidInfuser($"CmdRequestItemization received sender={(sender == null ? "<null>" : sender.ClientId.ToString())} entity='{entityIdentifier}' position={targetPosition}");
      if (sender == null || !sender.IsValid || Owner == null || !Owner.IsValid ||
          sender.ClientId != Owner.ClientId)
      { LogRapidInfuser("CmdRequestItemization rejected: sender/owner validation", true); return; }

      IItemizableWorldEntity target = null;
      if (!string.IsNullOrWhiteSpace(entityIdentifier) &&
          Registry.Registry.TryGetEntity(entityIdentifier.Trim(), out var descriptor) &&
          descriptor?.GameObject != null)
        target = descriptor.GameObject.GetComponentInChildren<IItemizableWorldEntity>(true);

      // 프리팹의 NetworkBehaviour 직렬화가 갱신되기 전에도 클라이언트에는 런타임 식별자가
      // 아직 전달되지 않을 수 있다. 이 경우 클릭 위치와 가장 가까운 등록 엔티티만 허용한다.
      if (target == null)
        target = FindItemizableNear(targetPosition);
      if (target == null)
      {
        LogRapidInfuser("CmdRequestItemization rejected: target not found", true);
        return;
      }

      bool result = target.TryItemizeOnServer(this);
      LogRapidInfuser($"CmdRequestItemization target resolved='{target.ItemizationEntityIdentifier}' result={result}");
    }

    private static IItemizableWorldEntity FindItemizableNear(Vector3 position)
    {
      const float maximumDistanceSquared = 0.75f * 0.75f;
      IItemizableWorldEntity nearest = null;
      float nearestDistanceSquared = maximumDistanceSquared;
      foreach (var pair in Registry.Registry.GetAllEntities())
      {
        var candidate = pair.Value?.GameObject?.GetComponentInChildren<IItemizableWorldEntity>(true);
        if (candidate is not Component component)
          continue;

        float distanceSquared = (component.transform.position - position).sqrMagnitude;
        if (distanceSquared > nearestDistanceSquared)
          continue;
        nearest = candidate;
        nearestDistanceSquared = distanceSquared;
      }

      // 씬에 미리 배치된 엔티티는 EntityPreset을 거치지 않아 레지스트리 식별자가 없을 수 있다.
      // 서버 물리 월드에서 직접 조회해도 최종적으로 대상의 거리 검증이 수행되므로 안전하다.
      foreach (var collider in Physics.OverlapSphere(position, 0.75f, ~0, QueryTriggerInteraction.Collide))
      {
        var candidate = collider.GetComponentInParent<IItemizableWorldEntity>();
        if (candidate is not Component component)
          continue;

        float distanceSquared = (component.transform.position - position).sqrMagnitude;
        if (distanceSquared > nearestDistanceSquared)
          continue;
        nearest = candidate;
        nearestDistanceSquared = distanceSquared;
      }
      return nearest;
    }

    private ActionResult InvokeUseItem()
    {
      if (HandlingItem == null)
        return ActionResult.Passed;
      _useItemTarget = RaycastTargetEntity();
      return HandlingItem.OnUse(this, _useItemTarget);
    }

    private Entity.Entity RaycastTargetEntity()
    {
      // TODO
      return null;
    }

    public void Attack()
    {
      if (PlayerEntity == null)
        return;

      PlayerEntity.Attack(_attackTarget, 0);

      if (IsOwner)
        PlayViewmodelAttack();
    }

    public void UseItem()
    {
      // 들고 있는 아이템을 조준 대상(크로스헤어 레이캐스트 히트)에 "사용"한다.
      // 대상이 IItemUseTarget 을 구현하면 OnItemUsed 로 위임한다(아이템 적용/신호 배선은 대상 책임).
      TryUseHandlingItemOnTarget();

      if (IsOwner)
        PlayViewmodelUse();
    }

    /// <summary>
    /// 현재 조준 중인 대상(크로스헤어 레이캐스트 히트)에서 <see cref="Entity.IItemUseTarget"/> 를 찾아
    /// 들고 있는 아이템을 사용한다. 히트가 없거나 대상이 아니면 아무 것도 하지 않는다(기존 동작).
    /// </summary>
    private bool TryUseHandlingItemOnTarget()
    {
      var itemId = HandlingItem?.CurrentIdentifier;
      if (string.IsNullOrWhiteSpace(itemId))
        return false;

      var hitObject = RaycastHitObject;
      if (hitObject == null)
        return false;

      var useTarget = hitObject.GetComponentInParent<Entity.IItemUseTarget>();
      if (useTarget == null)
        return false;

      return useTarget.OnItemUsed(PlayerEntity, itemId);
    }

    public void DropHeldItem()
    {
      if (_hotbarUI == null) return;
      int selectedIndex = _hotbarUI.SelectedSlot;
      if (selectedIndex < 0 || selectedIndex >= _slots.Count) return;

      var slot = _slots[selectedIndex];
      if (slot == null || slot.IsEmpty) return;

      var item = slot.TakeAll();
      if (item == null) return;

      TryDropItemInFront(item);
      OnInventoryChangedAndReturn(true);
    }

    public ActionResult CancelAttack()
    {
      attackTriggered = false;
      return ActionResult.Success;
    }

    public ActionResult CancelUseItem()
    {
      useItemTriggered = false;
      return ActionResult.Success;
    }

    private void EnsureViewmodelRoot()
    {
      if (!_enableViewmodel || !IsOwner)
        return;

      if (_viewmodelRoot != null)
        return;

      if (CameraAttachPoint?.PivotTransform == null)
        return;

      var go = new GameObject("HeldItemViewmodelRoot");
      go.transform.SetParent(CameraAttachPoint.PivotTransform, false);
      go.transform.localPosition = _viewmodelLocalPosition;
      go.transform.localRotation = Quaternion.Euler(_viewmodelLocalEuler);
      go.transform.localScale = _viewmodelLocalScale;
      _viewmodelRoot = go.transform;
    }

    private void RefreshViewmodel()
    {
      if (!_enableViewmodel || !IsOwner)
        return;

      EnsureViewmodelRoot();
      if (_viewmodelRoot == null)
        return;

      if (HandlingItem == null || string.IsNullOrWhiteSpace(HandlingItem.CurrentIdentifier))
      {
        ClearViewmodel();
        return;
      }

      if (_viewmodelItem != null && _viewmodelItem.CurrentIdentifier == HandlingItem.CurrentIdentifier)
      {
        return;
      }

      ClearViewmodel();
      _viewmodelItem = HandlingItem;

      var prefab = Resources.Load<GameObject>($"Models/Items/{HandlingItem.CurrentIdentifier}");
      if (prefab == null)
        return;

      _viewmodelInstance = Instantiate(prefab, _viewmodelRoot, false);
      StripNetworkComponents(_viewmodelInstance);
      DisableViewmodelColliders(_viewmodelInstance);
      ApplyViewmodelLayer(_viewmodelInstance);

      _viewmodelInstance.transform.localPosition = Vector3.zero;
      _viewmodelInstance.transform.localRotation = Quaternion.identity;
      _viewmodelInstance.transform.localScale = Vector3.one;
    }

    private void ClearViewmodel()
    {
      if (_viewmodelInstance != null)
      {
        Destroy(_viewmodelInstance);
        _viewmodelInstance = null;
      }
      _viewmodelItem = null;
    }

    private void StripNetworkComponents(GameObject target)
    {
      if (target == null)
        return;

      var networkBehaviours = target.GetComponentsInChildren<NetworkBehaviour>(true);
      foreach (var nb in networkBehaviours)
      {
        if (nb != null)
          Destroy(nb);
      }

      var networkObjects = target.GetComponentsInChildren<NetworkObject>(true);
      foreach (var no in networkObjects)
      {
        if (no != null)
          Destroy(no);
      }
    }

    private void DisableViewmodelColliders(GameObject target)
    {
      var colliders = target.GetComponentsInChildren<Collider>(true);
      foreach (var col in colliders)
      {
        if (col != null)
          col.enabled = false;
      }

      var bodies = target.GetComponentsInChildren<Rigidbody>(true);
      foreach (var body in bodies)
      {
        if (body != null)
          body.isKinematic = true;
      }
    }

    private void ApplyViewmodelLayer(GameObject target)
    {
      int layer = LayerMask.NameToLayer(_viewmodelLayerName);
      if (layer < 0)
        return;

      SetLayerRecursively(target, layer);
    }

    private void SetLayerRecursively(GameObject target, int layer)
    {
      if (target == null)
        return;

      target.layer = layer;
      foreach (Transform child in target.transform)
        SetLayerRecursively(child.gameObject, layer);
    }

    private void PlayViewmodelAttack()
    {
      if (_viewmodelRoot == null)
        return;

      StartViewmodelKick(new Vector3(0.02f, -0.02f, -0.06f), new Vector3(-6f, 8f, -6f), 0.12f);
    }

    private void PlayViewmodelUse()
    {
      if (_viewmodelRoot == null)
        return;

      StartViewmodelKick(new Vector3(0.0f, -0.04f, -0.04f), new Vector3(4f, -6f, 4f), 0.12f);
    }

    private void StartViewmodelKick(Vector3 posOffset, Vector3 rotOffsetEuler, float duration)
    {
      if (_viewmodelAnim != null)
        StopCoroutine(_viewmodelAnim);

      _viewmodelAnim = StartCoroutine(ViewmodelKickRoutine(posOffset, rotOffsetEuler, duration));
    }

    private System.Collections.IEnumerator ViewmodelKickRoutine(Vector3 posOffset, Vector3 rotOffsetEuler, float duration)
    {
      Vector3 basePos = _viewmodelRoot.localPosition;
      Quaternion baseRot = _viewmodelRoot.localRotation;
      Quaternion kickRot = Quaternion.Euler(_viewmodelLocalEuler + rotOffsetEuler);
      Vector3 kickPos = _viewmodelLocalPosition + posOffset;

      float half = duration * 0.5f;
      float t = 0f;
      while (t < duration)
      {
        t += Time.deltaTime;
        float lerp = t < half ? t / half : 1f - ((t - half) / half);
        _viewmodelRoot.localPosition = Vector3.Lerp(basePos, kickPos, lerp);
        _viewmodelRoot.localRotation = Quaternion.Slerp(baseRot, kickRot, lerp);
        yield return null;
      }

      _viewmodelRoot.localPosition = _viewmodelLocalPosition;
      _viewmodelRoot.localRotation = Quaternion.Euler(_viewmodelLocalEuler);
      _viewmodelAnim = null;
    }
  }
}
