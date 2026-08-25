using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.UI;
using TriageTrainer.Entity.IntravenousLine;
using UnityEngine;

namespace TriageTrainer.Entity
{
  public partial class MovingPatientBedController
  {
    private const float IntravenousFluidInteractionDistance = 3f;
    private const float PendingIntravenousFluidLifetimeSeconds = 10f;

    private enum IntravenousFluidKind : byte
    {
      NormalSaline,
      PlasmaSolution
    }

    private sealed class PendingIntravenousFluid
    {
      public int ClientId;
      public string ItemIdentifier;
      public float CreatedAt;
    }

    private sealed class HangIntravenousFluidInteract : IInteract, IInteractorConditional, IInteractDisplayIcons
    {
      private readonly MovingPatientBedController _owner;
      private readonly IntravenousFluidKind _kind;
      private Sprite _heldItemIcon;

      public HangIntravenousFluidInteract(MovingPatientBedController owner, IntravenousFluidKind kind)
      {
        _owner = owner;
        _kind = kind;
      }

      public string DisplayText => _kind == IntravenousFluidKind.NormalSaline
        ? "N/S 수액 걸기"
        : "P/S 수액 걸기";
      public Sprite DisplayIcon => null;
      public IReadOnlyList<Sprite> DisplayIcons => new[] { Icon.ClearRightBottom, _heldItemIcon };
      public bool AllowDisplayIconFallback => true;
      public Color DisplayColor => Color.white;

      public bool CanInteract(Transform interactor)
      {
        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        _heldItemIcon = null;
        return !_owner.IsIntravenousFluidInstalled(_kind) &&
               _owner.FindIntravenousFluidInventoryItem(player, _kind) != null;
      }

      public void Interact(Transform interactor)
      {
        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        string itemIdentifier = _owner.FindIntravenousFluidInventoryItem(player, _kind);
        if (player != null && _owner.IsIntravenousFluidItem(itemIdentifier, _kind))
          _owner.RequestHangIntravenousFluid(_kind, itemIdentifier, player);
      }
    }

    [Header("Attachment Display")]
    [SerializeField] private GameObject _intravenousStandReference;
    [SerializeField] private GameObject _intravenousHangerHangedNormalSalineReference;
    [SerializeField] private GameObject _intravenousHangerHangedPlasmaSolutionReference;

    [Header("Intravenous line points")]
    [SerializeField] private IntravenousLineConnectionPoint _normalSalineConnectionPoint;
    [SerializeField] private IntravenousLineConnectionPoint _plasmaSolutionConnectionPoint;

    [Header("Intravenous fluid item identifiers")]
    [Tooltip("Normal Saline으로 인식해 수액걸이에 설치할 수 있는 모든 아이템 식별자입니다.")]
    [SerializeField]
    private List<string> _normalSalineItemIdentifiers = new()
    {
      "normal_saline_1000ml", "normal_saline_20ml", "normal_saline_intravenous_ready",
      "normal_saline_5cc_syringe", "normal_saline_20cc_syringe", "normal_saline_50cc_syringe",
      "normal_saline_16g_5cc_syringe", "normal_saline_16g_20cc_syringe", "normal_saline_16g_50cc_syringe",
      "normal_saline_18g_5cc_syringe", "normal_saline_18g_20cc_syringe", "normal_saline_18g_50cc_syringe",
      "normal_saline_20g_5cc_syringe", "normal_saline_20g_20cc_syringe", "normal_saline_20g_50cc_syringe",
      "normal_saline_22g_5cc_syringe", "normal_saline_22g_20cc_syringe", "normal_saline_22g_50cc_syringe",
      "normal_saline_24g_5cc_syringe", "normal_saline_24g_20cc_syringe", "normal_saline_24g_50cc_syringe",
    };
    [Tooltip("Plasma Solution으로 인식해 수액걸이에 설치할 수 있는 모든 아이템 식별자입니다.")]
    [SerializeField]
    private List<string> _plasmaSolutionItemIdentifiers = new()
    {
      "plasma_solution_1000ml", "plasma_solution_intravenous_ready",
    };

    [Header("Intravenous attachment initial state")]
    [SerializeField] private bool _initialIntravenousStandInstalled;
    [SerializeField] private bool _initialNormalSalineInstalled;
    [SerializeField] private bool _initialPlasmaSolutionInstalled;

    private readonly SyncVar<bool> _intravenousStandInstalled = new(false);
    private readonly SyncVar<bool> _normalSalineInstalled = new(false);
    private readonly SyncVar<bool> _plasmaSolutionInstalled = new(false);
    private readonly Dictionary<IntravenousFluidKind, PendingIntravenousFluid> _pendingIntravenousFluids = new();
    private IInteract[] _intravenousFluidInteracts;

    public bool IsIntravenousStandInstalled => IsClientStarted || IsServerStarted
      ? _intravenousStandInstalled.Value : _initialIntravenousStandInstalled;
    public bool IsNormalSalineInstalled => IsClientStarted || IsServerStarted
      ? _normalSalineInstalled.Value : _initialNormalSalineInstalled;
    public bool IsPlasmaSolutionInstalled => IsClientStarted || IsServerStarted
      ? _plasmaSolutionInstalled.Value : _initialPlasmaSolutionInstalled;

    public bool TryGetNormalSalineConnectionPoint(out IntravenousLineConnectionPoint point)
    {
      point = null;
      if (!IsIntravenousStandInstalled || !IsNormalSalineInstalled
          || _normalSalineConnectionPoint == null)
        return false;

      point = _normalSalineConnectionPoint;
      return true;
    }

    public bool TryGetPlasmaSolutionConnectionPoint(out IntravenousLineConnectionPoint point)
    {
      point = null;
      if (!IsIntravenousStandInstalled || !IsPlasmaSolutionInstalled
          || _plasmaSolutionConnectionPoint == null)
        return false;

      point = _plasmaSolutionConnectionPoint;
      return true;
    }

    public bool IsNormalSalineConnectionPoint(IntravenousLineConnectionPoint point) =>
      point != null && ReferenceEquals(point, _normalSalineConnectionPoint);

    /// <summary>시나리오 수동 진입용으로 N/S 수액걸이를 즉시 준비한다.</summary>
    public void EnsureNormalSalineInstalledForScenario()
    {
      if (IsNormalSalineInstalled)
        return;

      if (IsServerStarted)
        SetIntravenousFluidInstalledOnServer(IntravenousFluidKind.NormalSaline);
      else if (!IsClientStarted)
        SetIntravenousFluidInstalledOffline(IntravenousFluidKind.NormalSaline);
    }

    public bool IsPlasmaSolutionConnectionPoint(IntravenousLineConnectionPoint point) =>
      point != null && ReferenceEquals(point, _plasmaSolutionConnectionPoint);

    private void InitializeIntravenousAttachmentDisplay() => ApplyIntravenousAttachmentDisplays();

    private IEnumerable<IInteract> IntravenousFluidInteracts => _intravenousFluidInteracts ??= new IInteract[]
    {
      new HangIntravenousFluidInteract(this, IntravenousFluidKind.NormalSaline),
      new HangIntravenousFluidInteract(this, IntravenousFluidKind.PlasmaSolution),
    };

    private void AddIntravenousFluidInteracts(List<IInteract> target)
    {
      if (target == null)
        return;
      target.AddRange(IntravenousFluidInteracts);
    }

    private void OnIntravenousAttachmentStartServer()
    {
      _pendingIntravenousFluids.Clear();
      _intravenousStandInstalled.Value = _initialIntravenousStandInstalled;
      _normalSalineInstalled.Value = _initialNormalSalineInstalled;
      _plasmaSolutionInstalled.Value = _initialPlasmaSolutionInstalled;
      ApplyIntravenousAttachmentDisplays();
    }

    private void OnIntravenousAttachmentStartClient()
    {
      _intravenousStandInstalled.OnChange += OnIntravenousAttachmentChanged;
      _normalSalineInstalled.OnChange += OnIntravenousAttachmentChanged;
      _plasmaSolutionInstalled.OnChange += OnIntravenousAttachmentChanged;
      ApplyIntravenousAttachmentDisplays();
    }

    private void OnIntravenousAttachmentStopClient()
    {
      _intravenousStandInstalled.OnChange -= OnIntravenousAttachmentChanged;
      _normalSalineInstalled.OnChange -= OnIntravenousAttachmentChanged;
      _plasmaSolutionInstalled.OnChange -= OnIntravenousAttachmentChanged;
    }

    private bool IsIntravenousFluidInstalled(IntravenousFluidKind kind) =>
      kind == IntravenousFluidKind.NormalSaline ? IsNormalSalineInstalled : IsPlasmaSolutionInstalled;

    private bool IsIntravenousFluidItem(string itemIdentifier, IntravenousFluidKind kind)
    {
      if (string.IsNullOrWhiteSpace(itemIdentifier))
        return false;
      var identifiers = kind == IntravenousFluidKind.NormalSaline
        ? _normalSalineItemIdentifiers : _plasmaSolutionItemIdentifiers;
      return identifiers != null && identifiers.Exists(id => string.Equals(id, itemIdentifier, StringComparison.Ordinal));
    }

    private string FindIntravenousFluidInventoryItem(PlayerController player, IntravenousFluidKind kind)
    {
      if (player == null)
        return null;
      var identifiers = kind == IntravenousFluidKind.NormalSaline
        ? _normalSalineItemIdentifiers : _plasmaSolutionItemIdentifiers;
      if (identifiers == null)
        return null;
      for (int i = 0; i < identifiers.Count; i++)
        if (player.CountItemInInventory(identifiers[i]) > 0)
          return identifiers[i];
      return null;
    }

    private void RequestHangIntravenousFluid(IntravenousFluidKind kind, string itemIdentifier, PlayerController player)
    {
      if (IsIntravenousFluidInstalled(kind) || !IsIntravenousFluidItem(itemIdentifier, kind))
        return;
      if (!IsClientStarted && !IsServerStarted)
      {
        if (player.RemoveItemFromInventory(itemIdentifier, 1) == 1)
          SetIntravenousFluidInstalledOffline(kind);
        return;
      }
      if (IsServerStarted)
      {
        if (player.RemoveItemFromInventory(itemIdentifier, 1) == 1)
          SetIntravenousFluidInstalledOnServer(kind);
      }
      else
        CmdHangIntravenousFluid((byte)kind, itemIdentifier);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdHangIntravenousFluid(byte rawKind, string itemIdentifier, NetworkConnection sender = null)
    {
      if (rawKind > (byte)IntravenousFluidKind.PlasmaSolution || sender == null || !sender.IsValid)
        return;
      var player = FindIntravenousAttachmentPlayer(sender.ClientId);
      var kind = (IntravenousFluidKind)rawKind;
      PruneExpiredPendingIntravenousFluid(kind);
      if (player == null
          || !IsWithinIntravenousFluidInteractionDistance(player)
          || IsIntravenousFluidInstalled(kind)
          || !IsIntravenousFluidItem(itemIdentifier, kind)
          || _pendingIntravenousFluids.ContainsKey(kind))
        return;
      _pendingIntravenousFluids[kind] = new PendingIntravenousFluid
      {
        ClientId = sender.ClientId,
        ItemIdentifier = itemIdentifier,
        CreatedAt = Time.unscaledTime
      };
      TargetConfirmHangIntravenousFluid(sender, rawKind, itemIdentifier);
    }

    [TargetRpc]
    private void TargetConfirmHangIntravenousFluid(NetworkConnection connection, byte rawKind, string itemIdentifier)
    {
      if (rawKind > (byte)IntravenousFluidKind.PlasmaSolution)
        return;
      var player = FindLocalIntravenousAttachmentPlayer();
      var kind = (IntravenousFluidKind)rawKind;
      if (player == null || !IsIntravenousFluidItem(itemIdentifier, kind) ||
          player.CountItemInInventory(itemIdentifier) < 1 || player.RemoveItemFromInventory(itemIdentifier, 1) != 1)
      {
        CmdReportHangIntravenousFluidFailure(rawKind, itemIdentifier);
        return;
      }
      CmdConfirmHangIntravenousFluid(rawKind, itemIdentifier);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdConfirmHangIntravenousFluid(
      byte rawKind,
      string itemIdentifier,
      NetworkConnection sender = null)
    {
      if (rawKind > (byte)IntravenousFluidKind.PlasmaSolution || sender == null || !sender.IsValid)
        return;
      var kind = (IntravenousFluidKind)rawKind;
      PruneExpiredPendingIntravenousFluid(kind);
      if (!_pendingIntravenousFluids.TryGetValue(kind, out var pending)
          || pending == null
          || pending.ClientId != sender.ClientId
          || !string.Equals(pending.ItemIdentifier, itemIdentifier, StringComparison.Ordinal))
        return;
      _pendingIntravenousFluids.Remove(kind);
      if (!IsIntravenousFluidInstalled(kind)
          && IsIntravenousFluidItem(itemIdentifier, kind))
        SetIntravenousFluidInstalledOnServer(kind);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdReportHangIntravenousFluidFailure(
      byte rawKind,
      string itemIdentifier,
      NetworkConnection sender = null)
    {
      if (rawKind > (byte)IntravenousFluidKind.PlasmaSolution || sender == null || !sender.IsValid)
        return;
      var kind = (IntravenousFluidKind)rawKind;
      if (_pendingIntravenousFluids.TryGetValue(kind, out var pending)
          && pending != null
          && pending.ClientId == sender.ClientId
          && string.Equals(pending.ItemIdentifier, itemIdentifier, StringComparison.Ordinal))
        _pendingIntravenousFluids.Remove(kind);
    }

    private void PruneExpiredPendingIntravenousFluid(IntravenousFluidKind kind)
    {
      if (_pendingIntravenousFluids.TryGetValue(kind, out var pending)
          && (pending == null
              || pending.CreatedAt < Time.unscaledTime - PendingIntravenousFluidLifetimeSeconds))
        _pendingIntravenousFluids.Remove(kind);
    }

    private bool IsWithinIntravenousFluidInteractionDistance(PlayerController player) =>
      player != null
      && (player.transform.position - transform.position).sqrMagnitude
      <= IntravenousFluidInteractionDistance * IntravenousFluidInteractionDistance;

    private void SetIntravenousFluidInstalledOffline(IntravenousFluidKind kind)
    {
      _initialIntravenousStandInstalled = true;
      if (kind == IntravenousFluidKind.NormalSaline)
        _initialNormalSalineInstalled = true;
      else
        _initialPlasmaSolutionInstalled = true;
      ApplyIntravenousAttachmentDisplays();
    }

    private void SetIntravenousFluidInstalledOnServer(IntravenousFluidKind kind)
    {
      _intravenousStandInstalled.Value = true;
      if (kind == IntravenousFluidKind.NormalSaline)
        _normalSalineInstalled.Value = true;
      else
        _plasmaSolutionInstalled.Value = true;
      ApplyIntravenousAttachmentDisplays();
    }

    private void OnIntravenousAttachmentChanged(bool previous, bool next, bool asServer)
    {
      ApplyIntravenousAttachmentDisplays();
      FindLocalIntravenousAttachmentPlayer()?.RefreshInteractableHintsNow();
    }

    private void ApplyIntravenousAttachmentDisplays()
    {
      if (_intravenousStandReference != null)
        _intravenousStandReference.SetActive(IsIntravenousStandInstalled);
      if (_intravenousHangerHangedNormalSalineReference != null)
        _intravenousHangerHangedNormalSalineReference.SetActive(IsNormalSalineInstalled);
      if (_intravenousHangerHangedPlasmaSolutionReference != null)
        _intravenousHangerHangedPlasmaSolutionReference.SetActive(IsPlasmaSolutionInstalled);
    }

    private static PlayerController FindIntravenousAttachmentPlayer(int clientId)
    {
      foreach (var player in FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        if (player != null && player.Owner != null && player.Owner.ClientId == clientId)
          return player;
      return null;
    }

    private static PlayerController FindLocalIntravenousAttachmentPlayer()
    {
      foreach (var player in FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        if (player != null && player.IsOwner)
          return player;
      return null;
    }
  }
}
