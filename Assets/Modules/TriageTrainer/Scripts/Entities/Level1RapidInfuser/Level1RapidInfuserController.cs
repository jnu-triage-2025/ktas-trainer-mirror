using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using TriageTrainer.Entity.IntravenousLine;
using UnityEngine;

namespace TriageTrainer.Entity
{
  [Serializable]
  public struct Level1RapidInfuserState
  {
    public bool HasNormalSaline;
    public bool HasPlasmaSolution;
    public string ConnectedPatientIdentifier;
  }

  /// <summary>
  /// 환자 저장 상태가 급속 주입기 상태를 보유할 경우 구현하는 선택적 복원 계약.
  /// PatientController 자체에 저장 형식을 강제하지 않기 위해 인터페이스로 예비한다.
  /// </summary>
  public interface ILevel1RapidInfuserStateSource
  {
    bool TryGetLevel1RapidInfuserState(out Level1RapidInfuserState state);
  }

  /// <summary>
  /// 급속 주입기의 상호작용과 상태를 소유한다. 이동은 도메인 독립 공통 모듈
  /// <see cref="MinecraftBoadLikeControl"/> 에 위임한다.
  /// </summary>
  public sealed class Level1RapidInfuserController : MinecraftBoadLikeControl,
    IInteractable, IInteract, IInteractorConditional, ISpawnedEntityIdentifierReceiver
  {
    private enum FluidKind : byte
    {
      NormalSaline,
      PlasmaSolution
    }

    private sealed class AddFluidInteract : IInteract, IInteractorConditional
    {
      private readonly Level1RapidInfuserController _owner;
      private readonly FluidKind _kind;

      public AddFluidInteract(Level1RapidInfuserController owner, FluidKind kind)
      {
        _owner = owner;
        _kind = kind;
      }

      public string DisplayText => _kind == FluidKind.NormalSaline
        ? "Normal Saline 추가"
        : "Plasma Solution 추가";
      public Sprite DisplayIcon => null;
      public bool AllowDisplayIconFallback => true;
      public Color DisplayColor => Color.white;

      public bool CanInteract(Transform interactor)
      {
        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        string id = player?.HandlingItem?.CurrentIdentifier;
        return !_owner.HasFluid(_kind) && IsFluidFamily(id, _kind);
      }

      public void Interact(Transform interactor)
      {
        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        string id = player?.HandlingItem?.CurrentIdentifier;
        if (player != null && IsFluidFamily(id, _kind))
          _owner.RequestAddFluid(_kind, id, player);
      }
    }

    [Header("Displays (assign on prefab)")]
    [SerializeField] private GameObject _normalSalineDisplay;
    [SerializeField] private GameObject _plasmaSolutionDisplay;

    [Header("IV connection (assign on prefab)")]
    [SerializeField] private IntravenousLineConnectionPoint _ivConnectionPoint;

    [Header("Offline / initial state")]
    [SerializeField] private bool _initialHasNormalSaline;
    [SerializeField] private bool _initialHasPlasmaSolution;
    [SerializeField] private string _initialConnectedPatientIdentifier;

    [Header("Identity / interaction")]
    [SerializeField] private string _displayText = "Level 1 급속 주입기 조종";
    [SerializeField] private Sprite _displayIcon;

    private readonly SyncVar<bool> _hasNormalSaline = new(false);
    private readonly SyncVar<bool> _hasPlasmaSolution = new(false);
    private readonly SyncVar<string> _connectedPatientIdentifier = new(string.Empty);
    private readonly SyncVar<string> _runtimeIdentifier = new(string.Empty);
    private IInteract[] _fluidInteracts;
    private string _registeredIdentifier;
    private int _pendingSalineClientId = -1;
    private int _pendingPlasmaClientId = -1;
    private float _pendingSalineExpiresAt;
    private float _pendingPlasmaExpiresAt;
    private const float InteractionDistance = 3f;
    private const float FluidRequestTimeout = 3f;

    public IEnumerable<IInteract> AdditionalInteracts =>
      _fluidInteracts ??= new IInteract[]
      {
        new AddFluidInteract(this, FluidKind.NormalSaline),
        new AddFluidInteract(this, FluidKind.PlasmaSolution)
      };

    public IInteract[] Interacts
    {
      get
      {
        var result = new List<IInteract> { this };
        result.AddRange(AdditionalInteracts);
        return result.ToArray();
      }
    }
    public string DisplayText => _displayText;
    public Sprite DisplayIcon => _displayIcon;
    public bool AllowDisplayIconFallback => true;
    public Color DisplayColor => Color.white;

    public bool HasNormalSaline => IsClientStarted || IsServerStarted
      ? _hasNormalSaline.Value
      : _initialHasNormalSaline;
    public bool HasPlasmaSolution => IsClientStarted || IsServerStarted
      ? _hasPlasmaSolution.Value
      : _initialHasPlasmaSolution;
    public string ConnectedPatientIdentifier => IsClientStarted || IsServerStarted
      ? _connectedPatientIdentifier.Value
      : _initialConnectedPatientIdentifier;
    public Transform IvConnectionPoint =>
      _ivConnectionPoint != null ? _ivConnectionPoint.transform : transform;

    private void Awake()
    {
      Awake_MinecraftBoadLikeControl();
      Configure(1);
      if (_ivConnectionPoint != null)
      {
        _ivConnectionPoint.OnConnected += OnIntravenousLineConnected;
        _ivConnectionPoint.OnDisconnected += OnIntravenousLineDisconnected;
      }
      ApplyDisplays();
    }

    private void OnDestroy()
    {
      UnregisterEntity();
      if (_ivConnectionPoint == null)
        return;
      _ivConnectionPoint.OnConnected -= OnIntravenousLineConnected;
      _ivConnectionPoint.OnDisconnected -= OnIntravenousLineDisconnected;
    }

    private void Update() => Update_MinecraftBoadLikeControl();

    public override void OnStartServer()
    {
      base.OnStartServer();
      _hasNormalSaline.Value = _initialHasNormalSaline;
      _hasPlasmaSolution.Value = _initialHasPlasmaSolution;
      _connectedPatientIdentifier.Value = _initialConnectedPatientIdentifier ?? string.Empty;
      ApplyDisplays();
    }

    public override void OnStartClient()
    {
      base.OnStartClient();
      _hasNormalSaline.OnChange += OnFluidChanged;
      _hasPlasmaSolution.OnChange += OnFluidChanged;
      _runtimeIdentifier.OnChange += OnRuntimeIdentifierChanged;
      RegisterEntity();
      ApplyDisplays();
    }

    public override void OnStopClient()
    {
      _hasNormalSaline.OnChange -= OnFluidChanged;
      _hasPlasmaSolution.OnChange -= OnFluidChanged;
      _runtimeIdentifier.OnChange -= OnRuntimeIdentifierChanged;
      UnregisterEntity();
      base.OnStopClient();
    }

    public void Interact(Transform interactor) => Toggle(interactor);

    public bool CanInteract(Transform interactor) => CanToggle(interactor);

    public void ApplySpawnedEntityIdentifier(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return;
      string value = identifier.Trim();
      if (IsServerStarted)
        _runtimeIdentifier.Value = value;
      _registeredIdentifier = value;
      RegisterEntity();
    }

    private void OnRuntimeIdentifierChanged(string previous, string next, bool asServer)
    {
      UnregisterEntity();
      _registeredIdentifier = next;
      RegisterEntity();
    }

    private void RegisterEntity()
    {
      string identifier = !string.IsNullOrWhiteSpace(_runtimeIdentifier.Value)
        ? _runtimeIdentifier.Value
        : _registeredIdentifier;
      if (string.IsNullOrWhiteSpace(identifier))
        return;
      _registeredIdentifier = identifier;
      Registry.RegisterEntity(
        identifier,
        EntityType.Level1RapidInfuser,
        gameObject,
        _displayText,
        isNetworked: IsClientStarted || IsServerStarted);
    }

    private void UnregisterEntity()
    {
      if (!string.IsNullOrWhiteSpace(_registeredIdentifier))
        Registry.UnregisterEntity(_registeredIdentifier);
    }

    public Level1RapidInfuserState CaptureState() => new()
    {
      HasNormalSaline = HasNormalSaline,
      HasPlasmaSolution = HasPlasmaSolution,
      ConnectedPatientIdentifier = ConnectedPatientIdentifier
    };

    public void ApplyState(Level1RapidInfuserState state)
    {
      if (!IsClientStarted && !IsServerStarted)
      {
        ApplyStateOffline(state);
        return;
      }
      if (IsServerStarted)
        ApplyStateOnServer(state);
      else
        Debug.LogWarning("[Level1RapidInfuser] Patient state restoration must be requested by the server.", this);
    }

    public bool TryLoadStateFromPatient(PatientController patient)
    {
      if (patient == null)
        return false;

      var components = patient.GetComponents<MonoBehaviour>();
      for (int i = 0; i < components.Length; i++)
      {
        if (components[i] is not ILevel1RapidInfuserStateSource source ||
            !source.TryGetLevel1RapidInfuserState(out var state))
          continue;
        // 상태를 제공한 현재 환자가 연결 관계의 진실 원천이다.
        // 저장 데이터의 오래된/빈 연결 식별자로 방금 연결한 환자가 덮이지 않게 한다.
        state.ConnectedPatientIdentifier = patient.Identifier;
        ApplyState(state);
        return true;
      }
      return false;
    }

    public void ConnectPatient(PatientController patient)
    {
      string identifier = patient != null ? patient.Identifier : string.Empty;
      if (!IsClientStarted && !IsServerStarted)
      {
        _initialConnectedPatientIdentifier = identifier;
        if (patient != null)
          TryLoadStateFromPatient(patient);
      }
      else if (IsServerStarted)
      {
        _connectedPatientIdentifier.Value = identifier;
        if (patient != null)
          TryLoadStateFromPatient(patient);
      }
      else
        CmdConnectPatient(identifier);
    }

    private void OnIntravenousLineConnected(
      IntravenousLineConnectionPoint ownPoint,
      IntravenousLineConnectionPoint otherPoint)
    {
      if (!ReferenceEquals(ownPoint, _ivConnectionPoint) || otherPoint == null)
        return;

      var patient = otherPoint.GetComponentInParent<PatientController>();
      if (patient != null)
        ConnectPatient(patient);
    }

    private void OnIntravenousLineDisconnected(
      IntravenousLineConnectionPoint ownPoint,
      IntravenousLineConnectionPoint otherPoint)
    {
      if (!ReferenceEquals(ownPoint, _ivConnectionPoint))
        return;

      var patient = otherPoint != null ? otherPoint.GetComponentInParent<PatientController>() : null;
      if (patient == null ||
          string.Equals(ConnectedPatientIdentifier, patient.Identifier, StringComparison.Ordinal))
        ConnectPatient(null);
    }

    private bool HasFluid(FluidKind kind) =>
      kind == FluidKind.NormalSaline ? HasNormalSaline : HasPlasmaSolution;

    private static bool IsFluidFamily(string itemIdentifier, FluidKind kind)
    {
      if (string.IsNullOrWhiteSpace(itemIdentifier))
        return false;
      string prefix = kind == FluidKind.NormalSaline ? "normal_saline" : "plasma_solution";
      return itemIdentifier.StartsWith(prefix, StringComparison.Ordinal);
    }

    private void RequestAddFluid(FluidKind kind, string itemIdentifier, PlayerController player)
    {
      if (!IsClientStarted && !IsServerStarted)
      {
        if (!HasFluid(kind) && player.RemoveItemFromInventory(itemIdentifier, 1) == 1)
          SetFluidOffline(kind);
        return;
      }
      if (IsServerStarted)
      {
        // 호스트의 인벤토리는 이 인스턴스에 있으므로 즉시 소비/확정할 수 있다.
        if (!HasFluid(kind) &&
            IsWithinInteractionDistance(player) &&
            player.RemoveItemFromInventory(itemIdentifier, 1) == 1)
          SetFluidOnServer(kind);
      }
      else
        CmdRequestAddFluid((byte)kind, itemIdentifier);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdRequestAddFluid(byte rawKind, string itemIdentifier, NetworkConnection sender = null)
    {
      if (rawKind > (byte)FluidKind.PlasmaSolution || sender == null || !sender.IsValid)
        return;

      var kind = (FluidKind)rawKind;
      var player = FindPlayer(sender.ClientId);
      if (player == null || HasFluid(kind) || !IsFluidFamily(itemIdentifier, kind) ||
          !IsWithinInteractionDistance(player) || HasActivePendingFluid(kind))
        return;

      SetPendingFluid(kind, sender.ClientId);
      TargetConfirmFluidConsumption(sender, rawKind, itemIdentifier);
    }

    [TargetRpc]
    private void TargetConfirmFluidConsumption(
      NetworkConnection connection,
      byte rawKind,
      string itemIdentifier)
    {
      if (rawKind > (byte)FluidKind.PlasmaSolution)
        return;

      var kind = (FluidKind)rawKind;
      var player = FindLocalOwnerPlayer();
      if (player == null ||
          !IsFluidFamily(itemIdentifier, kind) ||
          !string.Equals(player.HandlingItem?.CurrentIdentifier, itemIdentifier, StringComparison.Ordinal) ||
          player.CountItemInInventory(itemIdentifier) < 1 ||
          player.RemoveItemFromInventory(itemIdentifier, 1) != 1)
      {
        CmdReportFluidConsumptionFailure(rawKind);
        return;
      }

      CmdAcknowledgeFluidConsumption(rawKind);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdAcknowledgeFluidConsumption(byte rawKind, NetworkConnection sender = null)
    {
      if (rawKind > (byte)FluidKind.PlasmaSolution || sender == null || !sender.IsValid)
        return;

      var kind = (FluidKind)rawKind;
      if (!MatchesPendingFluid(kind, sender.ClientId) || HasFluid(kind))
        return;

      ClearPendingFluid(kind);
      SetFluidOnServer(kind);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdReportFluidConsumptionFailure(byte rawKind, NetworkConnection sender = null)
    {
      if (rawKind > (byte)FluidKind.PlasmaSolution || sender == null || !sender.IsValid)
        return;

      var kind = (FluidKind)rawKind;
      if (MatchesPendingFluid(kind, sender.ClientId))
        ClearPendingFluid(kind);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdConnectPatient(string identifier, NetworkConnection sender = null)
    {
      var player = sender != null && sender.IsValid ? FindPlayer(sender.ClientId) : null;
      if (player == null || Vector3.Distance(player.transform.position, IvConnectionPoint.position) > 3f)
        return;

      if (string.IsNullOrWhiteSpace(identifier))
      {
        _connectedPatientIdentifier.Value = string.Empty;
        return;
      }

      if (!Registry.TryGetEntity(identifier, out var descriptor) ||
          descriptor?.GameObject == null ||
          !descriptor.GameObject.TryGetComponent<PatientController>(out var patient))
        return;
      if (Vector3.Distance(patient.transform.position, IvConnectionPoint.position) > 5f)
        return;

      _connectedPatientIdentifier.Value = patient.Identifier;
      TryLoadStateFromPatient(patient);
    }

    private void ApplyStateOnServer(Level1RapidInfuserState state)
    {
      _hasNormalSaline.Value = state.HasNormalSaline;
      _hasPlasmaSolution.Value = state.HasPlasmaSolution;
      _connectedPatientIdentifier.Value = state.ConnectedPatientIdentifier ?? string.Empty;
      ApplyDisplays();
    }

    private void ApplyStateOffline(Level1RapidInfuserState state)
    {
      _initialHasNormalSaline = state.HasNormalSaline;
      _initialHasPlasmaSolution = state.HasPlasmaSolution;
      _initialConnectedPatientIdentifier = state.ConnectedPatientIdentifier ?? string.Empty;
      ApplyDisplays();
    }

    private void SetFluidOffline(FluidKind kind)
    {
      if (kind == FluidKind.NormalSaline) _initialHasNormalSaline = true;
      else _initialHasPlasmaSolution = true;
      ApplyDisplays();
    }

    private void SetFluidOnServer(FluidKind kind)
    {
      if (kind == FluidKind.NormalSaline) _hasNormalSaline.Value = true;
      else _hasPlasmaSolution.Value = true;
      ApplyDisplays();
    }

    private bool IsWithinInteractionDistance(PlayerController player)
    {
      return player != null &&
             (player.transform.position - transform.position).sqrMagnitude <=
             InteractionDistance * InteractionDistance;
    }

    private bool HasActivePendingFluid(FluidKind kind)
    {
      int clientId = kind == FluidKind.NormalSaline ? _pendingSalineClientId : _pendingPlasmaClientId;
      float expiry = kind == FluidKind.NormalSaline ? _pendingSalineExpiresAt : _pendingPlasmaExpiresAt;
      if (clientId < 0)
        return false;
      if (Time.unscaledTime <= expiry)
        return true;
      ClearPendingFluid(kind);
      return false;
    }

    private void SetPendingFluid(FluidKind kind, int clientId)
    {
      if (kind == FluidKind.NormalSaline)
      {
        _pendingSalineClientId = clientId;
        _pendingSalineExpiresAt = Time.unscaledTime + FluidRequestTimeout;
      }
      else
      {
        _pendingPlasmaClientId = clientId;
        _pendingPlasmaExpiresAt = Time.unscaledTime + FluidRequestTimeout;
      }
    }

    private bool MatchesPendingFluid(FluidKind kind, int clientId)
    {
      if (!HasActivePendingFluid(kind))
        return false;
      return (kind == FluidKind.NormalSaline ? _pendingSalineClientId : _pendingPlasmaClientId) == clientId;
    }

    private void ClearPendingFluid(FluidKind kind)
    {
      if (kind == FluidKind.NormalSaline)
      {
        _pendingSalineClientId = -1;
        _pendingSalineExpiresAt = 0f;
      }
      else
      {
        _pendingPlasmaClientId = -1;
        _pendingPlasmaExpiresAt = 0f;
      }
    }

    private void OnFluidChanged(bool previous, bool next, bool asServer)
    {
      ApplyDisplays();
      FindLocalOwnerPlayer()?.RefreshInteractableHintsNow();
    }

    private void ApplyDisplays()
    {
      if (_normalSalineDisplay != null) _normalSalineDisplay.SetActive(HasNormalSaline);
      if (_plasmaSolutionDisplay != null) _plasmaSolutionDisplay.SetActive(HasPlasmaSolution);
    }

    private static PlayerController FindPlayer(int clientId)
    {
      foreach (var player in FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        if (player != null && player.Owner != null && player.Owner.ClientId == clientId)
          return player;
      return null;
    }

    private static PlayerController FindLocalOwnerPlayer()
    {
      foreach (var player in FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        if (player != null && player.IsOwner)
          return player;
      return null;
    }
  }
}
