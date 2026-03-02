using FishNet.Object;
using MultiplayerInfrastructure.Item;
using MultiplayerInfrastructure.ItemSystem;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;
using MultiplayerInfrastructure.Entity;
using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
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
    /// Usage Sequence:
    /// called TriggerAttack/TriggerUseItem from other part
    /// -> triggered by Trigger~
    /// -> trigger resolved when Update_Item called in Update loop
    /// </summary>

    void Update_Item()
    {
      ResolveHandledItemTriggered();
    }

    private void ResolveHandledItemTriggered()
    {
      if (attackTriggered)
      {
        attackTriggered = false;
        if (InvokeAttack() != ActionResult.Cancelled)
          return;
        Attack();
      }

      if (useItemTriggered)
      {
        useItemTriggered = false;
        if (InvokeUseItem() != ActionResult.Cancelled)
          return;
        UseItem();
      }
    }

    private void ResolveHandledItem()
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
      if (HandlingItem == null)
        return ActionResult.Passed;
      _attackTarget = RaycastTargetEntity();
      return HandlingItem.OnAttack(this, _attackTarget);
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
      if (IsOwner)
        PlayViewmodelUse();
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

      if (CameraHolderTransform == null)
        return;

      var go = new GameObject("HeldItemViewmodelRoot");
      go.transform.SetParent(CameraHolderTransform, false);
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

      var prefab = Registry.Registry.Get<GameObject>(RegistryType.Item, HandlingItem.CurrentIdentifier);
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
