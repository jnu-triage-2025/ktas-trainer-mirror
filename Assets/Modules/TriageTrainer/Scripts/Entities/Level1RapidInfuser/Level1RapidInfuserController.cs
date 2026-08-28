using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Logging;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.UI;
using TriageTrainer.Entity.CentralLine;
using TriageTrainer.Entity.LineConnection;
using TriageTrainer.ItemDefinitions;
using UnityEngine;

namespace TriageTrainer.Entity
{
  [Serializable]
  public struct Level1RapidInfuserState
  {
    public bool HasNormalSaline;
    public bool HasPlasmaSolution;
    public bool HasBloodBag;
    public string ConnectedPatientIdentifier;
  }

  /// <summary>
  /// 환자 저장 상태가 급속 주입기 상태를 보유할 경우 구현하는 선택적 복원 규약.
  /// PatientController 자체에 저장 형식을 강제하지 않기 위해 인터페이스로 예비한다.
  /// </summary>
  public interface ILevel1RapidInfuserStateSource
  {
    public bool TryGetLevel1RapidInfuserState(out Level1RapidInfuserState state);
  }

  public enum BloodBagWithoutPlasmaPolicy : byte
  {
    CancelTry,
    Pass
  }

  public enum RapidInfuserFluidCancellationReason : byte
  {
    None,
    AlreadyApplied,
    PlasmaAppliedRequired,
    UnsupportedItem,
    PlayerUnavailable,
    OutOfInteractionRange,
    ItemUnavailable,
    PendingConfirmation,
    ConsumptionFailed
  }

  [Flags]
  public enum BloodBagCancellationBehaviour : byte
  {
    None = 0,
    ShowPlasmaRequiredDialogue = 1 << 0
  }

  /// <summary>급속 주입기 용액 적용 수명주기 이벤트의 상세 정보다.</summary>
  [Serializable]
  public struct RapidInfuserFluidLifecycleEvent
  {
    public string FluidKind;
    public string ItemIdentifier;
    public string PlayerIdentifier;
    public int PlayerClientId;
    public string RapidInfuserIdentifier;
    public RapidInfuserFluidCancellationReason CancellationReason;
    public BloodBagWithoutPlasmaPolicy BloodWithoutPlasmaPolicy;
    public bool IsServer;
  }

  /// <summary>
  /// 급속 주입기의 상호작용과 상태를 소유한다. 이동은 도메인 독립 공통 모듈
  /// <see cref="MinecraftBoatLikeControl"/> 에 위임한다.
  /// </summary>
  public sealed class Level1RapidInfuserController : MinecraftBoatLikeControl,
    IInteractable, IInteract, IInteractorConditional, ISpawnedEntityIdentifierReceiver
  {
    private const string FlowTag = "RapidInfuserFlow";
    private void LogFlow(string message, bool warning = false)
    {
      string role = IsServerStarted ? "server" : (IsClientStarted ? "client" : "offline");
      string id = string.IsNullOrWhiteSpace(_registeredIdentifier) ? "<unregistered>" : _registeredIdentifier;
      string full = $"[LV1RapidInfuser] role={role} object='{name}' id='{id}' {message}";
      GameLogService.Write(warning ? GameLogCategory.Misc : GameLogCategory.Interaction, full, FlowTag);
      if (warning)
        Debug.LogWarning(full, this);
      else
        Debug.Log(full, this);
    }
    private enum FluidKind : byte
    {
      NormalSaline,
      PlasmaSolution,
      BloodBag
    }

    private const string BloodPlasmaRequiredSignal = "blood_to_lv1_requires_plasma";

    private sealed class AddFluidInteract : IInteract, IInteractorConditional, IInteractDisplayIcons,
      IQuestPresentationTarget
    {
      private readonly Level1RapidInfuserController _owner;
      private readonly FluidKind _kind;
      private Sprite _heldItemIcon;

      public AddFluidInteract(Level1RapidInfuserController owner, FluidKind kind)
      {
        _owner = owner;
        _kind = kind;
      }

      public string DisplayText => _kind == FluidKind.NormalSaline
        ? "생리식염수 추가"
        : _kind == FluidKind.PlasmaSolution
          ? "급속주입기에 플라즈마 솔루션 연결"
          : "급속주입기에 혈액백 연결";
      public string PresentationEntityIdentifier => _owner.Identifier;
      public string InteractionIdentifier => _kind switch
      {
        FluidKind.NormalSaline => InteractIdAddNormalSaline,
        FluidKind.PlasmaSolution => InteractIdAddPlasmaSolution,
        _ => InteractIdAddBloodBag
      };
      public Sprite DisplayIcon => null;
      public IReadOnlyList<Sprite> DisplayIcons => new[] { Icon.ClearRightBottom, _heldItemIcon };
      public bool AllowDisplayIconFallback => true;
      public Color DisplayColor => Color.white;

      public bool CanInteract(Transform interactor)
      {
        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        string id = _owner.FindFluidInventoryItem(player, _kind);
        _heldItemIcon = null;
        return _owner.CanAttemptFluid(_kind) && IsFluidFamily(id, _kind);
      }

      public void Interact(Transform interactor)
      {
        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        string id = _owner.FindFluidInventoryItem(player, _kind);
        if (player != null && _owner.CanAttemptFluid(_kind) && IsFluidFamily(id, _kind))
          _owner.RequestAddFluid(_kind, id, player);
      }
    }

    private sealed class ConnectCLineInteract : IInteract, IInteractorConditional,
      IQuestPresentationTarget
    {
      private readonly Level1RapidInfuserController _owner;

      public ConnectCLineInteract(Level1RapidInfuserController owner) { _owner = owner; }

      public string DisplayText => "환자에게 C라인 연결";
      public string PresentationEntityIdentifier => _owner.Identifier;
      public string InteractionIdentifier => InteractIdConnectCLine;
      public Sprite DisplayIcon => null;
      public bool AllowDisplayIconFallback => true;
      public Color DisplayColor => Color.white;

      public bool CanInteract(Transform interactor)
        => _owner.CanAttemptCLineConnection();

      public void Interact(Transform interactor)
      {
        if (_owner.CanAttemptCLineConnection())
          _owner.RequestCLineConnection();
      }
    }

    [Header("Displays (assign on prefab)")]
    [SerializeField] private GameObject _normalSalineDisplay;
    [SerializeField] private GameObject _plasmaSolutionDisplay;
    [SerializeField] private GameObject _bloodBagDisplay;

    [Header("Central line connection (assign on prefab)")]
    [SerializeField] private CentralLineConnectionPoint _centralLineConnectionPoint;

    [Header("Offline / initial state")]
    [SerializeField] private bool _initialHasNormalSaline;
    [SerializeField] private bool _initialHasPlasmaSolution;
    [SerializeField] private bool _initialHasBloodBag;
    [SerializeField] private string _initialConnectedPatientIdentifier;

    [Header("Blood bag policy")]
    [SerializeField]
    private BloodBagWithoutPlasmaPolicy _bloodBagWithoutPlasmaPolicy =
      BloodBagWithoutPlasmaPolicy.CancelTry;
    [SerializeField]
    private BloodBagCancellationBehaviour _bloodBagCancellationBehaviour =
      BloodBagCancellationBehaviour.ShowPlasmaRequiredDialogue;

    [Header("Identity / interaction")]
    [Tooltip("씬에 사전 배치된 이 주입기 인스턴스의 고유 식별자입니다. EntityPreset으로 스폰되면 스폰 요청의 식별자로 대체됩니다.")]
    [SerializeField] private string _entityIdentifier = "level1_rapid_infuser_a";
    [SerializeField] private string _displayText = "Level 1 급속 주입기 조종";
    [SerializeField] private Sprite _displayIcon;

    private readonly SyncVar<bool> _hasNormalSaline = new(false);
    private readonly SyncVar<bool> _hasPlasmaSolution = new(false);
    private readonly SyncVar<bool> _hasBloodBag = new(false);
    private readonly SyncVar<string> _connectedPatientIdentifier = new(string.Empty);
    private readonly SyncVar<string> _runtimeIdentifier = new(string.Empty);
    private IInteract[] _fluidInteracts;
    private string _registeredIdentifier;
    private int _pendingSalineClientId = -1;
    private int _pendingPlasmaClientId = -1;
    private int _pendingBloodClientId = -1;
    private string _pendingSalineItemIdentifier;
    private string _pendingPlasmaItemIdentifier;
    private string _pendingBloodItemIdentifier;
    private float _pendingSalineLastConfirmationSentAt;
    private float _pendingPlasmaLastConfirmationSentAt;
    private float _pendingBloodLastConfirmationSentAt;
    private bool _clientConfirmedSalineConsumption;
    private bool _clientConfirmedPlasmaConsumption;
    private bool _clientConfirmedBloodConsumption;
    private const float InteractionDistance = 3f;
    private const float FluidConfirmationRetrySeconds = 1f;

    /// <summary>C라인을 이을 수 있는 두 연결 지점 사이의 최대 거리.</summary>
    private const float CLineConnectionDistance = 5f;

    /// <summary>C라인 연결 상호작용 노출 조건을 다시 확인하는 간격(초).</summary>
    private const float CLineAvailabilityCheckInterval = 0.25f;

    private bool _cLineConnectionAvailable;
    private float _nextCLineAvailabilityCheckAt;

    public const string InteractIdAddNormalSaline = "level1_add_normal_saline";
    public const string InteractIdAddPlasmaSolution = "level1_add_plasma_solution";
    public const string InteractIdAddBloodBag = "level1_add_blood_bag";
    public const string InteractIdConnectCLine = "level1_connect_cline";

    public event Action<RapidInfuserFluidLifecycleEvent> OnPlasmaTry;
    public event Action<RapidInfuserFluidLifecycleEvent> OnBloodTry;
    public event Action<RapidInfuserFluidLifecycleEvent> OnPlasmaCancelled;
    public event Action<RapidInfuserFluidLifecycleEvent> OnBloodCancelled;
    public event Action<RapidInfuserFluidLifecycleEvent> OnPlasmaApplied;
    public event Action<RapidInfuserFluidLifecycleEvent> OnBloodApplied;

    public IEnumerable<IInteract> AdditionalInteracts =>
      _fluidInteracts ??= new IInteract[]
      {
        new AddFluidInteract(this, FluidKind.NormalSaline),
        new AddFluidInteract(this, FluidKind.PlasmaSolution),
        new AddFluidInteract(this, FluidKind.BloodBag),
        new ConnectCLineInteract(this)
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
    public Sprite DisplayIcon => _displayIcon != null ? _displayIcon : ResolvedDefaultControlIcon;
    public bool AllowDisplayIconFallback => true;
    public Color DisplayColor => Color.white;

    public bool HasNormalSaline => IsClientStarted || IsServerStarted
      ? _hasNormalSaline.Value
      : _initialHasNormalSaline;
    public bool HasPlasmaSolution => IsClientStarted || IsServerStarted
      ? _hasPlasmaSolution.Value
      : _initialHasPlasmaSolution;
    public bool HasBloodBag => IsClientStarted || IsServerStarted
      ? _hasBloodBag.Value
      : _initialHasBloodBag;
    public string ConnectedPatientIdentifier => IsClientStarted || IsServerStarted
      ? _connectedPatientIdentifier.Value
      : _initialConnectedPatientIdentifier;
    public Transform IvConnectionPoint =>
      _centralLineConnectionPoint != null ? _centralLineConnectionPoint.transform : transform;
    public string Identifier => EffectiveIdentifier;

    private string EffectiveIdentifier =>
      !string.IsNullOrWhiteSpace(_runtimeIdentifier.Value) ? _runtimeIdentifier.Value
      : !string.IsNullOrWhiteSpace(_entityIdentifier) ? _entityIdentifier
      : _registeredIdentifier;

    private void Awake()
    {
      Awake_MinecraftBoatLikeControl();
      Configure(1); // Level 1 Rapid Infuser는 한 명만 조종한다.
      EnsureDisplayReferences();
      OnBloodCancelled += RaiseBloodPlasmaRequiredSignal;
      ApplyDisplays();
    }

    private void OnDestroy()
    {
      UnregisterEntity();
      OnBloodCancelled -= RaiseBloodPlasmaRequiredSignal;
    }

    private void Update()
    {
      Update_MinecraftBoatLikeControl();
      if (IsServerStarted)
        RetryPendingFluidConfirmations();
      RefreshCLineHintOnAvailabilityChange();
    }

    public override void OnStartServer()
    {
      base.OnStartServer();
      _hasNormalSaline.Value = _initialHasNormalSaline;
      _hasPlasmaSolution.Value = _initialHasPlasmaSolution;
      _hasBloodBag.Value = _initialHasBloodBag;
      _connectedPatientIdentifier.Value = _initialConnectedPatientIdentifier ?? string.Empty;
      RegisterEntity();
      ApplyDisplays();
    }

    public override void OnStopServer()
    {
      // 풀링된 NetworkObject는 OnDestroy 없이 재사용될 수 있으므로 서버 수명주기에서 해제한다.
      UnregisterEntity();
      base.OnStopServer();
    }

    public override void OnStartClient()
    {
      base.OnStartClient();
      _hasNormalSaline.OnChange += OnFluidChanged;
      _hasPlasmaSolution.OnChange += OnFluidChanged;
      _hasBloodBag.OnChange += OnFluidChanged;
      _runtimeIdentifier.OnChange += OnRuntimeIdentifierChanged;
      RegisterEntity();
      ApplyDisplays();
    }

    public override void OnStopClient()
    {
      _hasNormalSaline.OnChange -= OnFluidChanged;
      _hasPlasmaSolution.OnChange -= OnFluidChanged;
      _hasBloodBag.OnChange -= OnFluidChanged;
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
      if (!string.IsNullOrWhiteSpace(_registeredIdentifier) &&
          !string.Equals(_registeredIdentifier, value, StringComparison.Ordinal))
        UnregisterEntity();

      _entityIdentifier = value;
      if (IsServerStarted)
        _runtimeIdentifier.Value = value;
      RegisterEntity();
    }

    private void OnRuntimeIdentifierChanged(string previous, string next, bool asServer)
    {
      UnregisterEntity();
      if (!string.IsNullOrWhiteSpace(next))
        _entityIdentifier = next;
      RegisterEntity();
    }

    private void RegisterEntity()
    {
      string identifier = EffectiveIdentifier;
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
      _registeredIdentifier = null;
    }

    public Level1RapidInfuserState CaptureState() => new()
    {
      HasNormalSaline = HasNormalSaline,
      HasPlasmaSolution = HasPlasmaSolution,
      HasBloodBag = HasBloodBag,
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

    private bool HasFluid(FluidKind kind) => kind switch
    {
      FluidKind.NormalSaline => HasNormalSaline,
      FluidKind.PlasmaSolution => HasPlasmaSolution,
      FluidKind.BloodBag => HasBloodBag,
      _ => false
    };

    private bool CanAddFluid(FluidKind kind)
      => GetFluidCancellationReason(kind) == RapidInfuserFluidCancellationReason.None;

    // 취소 가능한 시도도 수명주기 이벤트와 사용자 피드백을 위해 상호작용으로 노출한다.
    private bool CanAttemptFluid(FluidKind kind) => !HasFluid(kind);

    private RapidInfuserFluidCancellationReason GetFluidCancellationReason(FluidKind kind)
    {
      if (HasFluid(kind))
        return RapidInfuserFluidCancellationReason.AlreadyApplied;

      if (kind == FluidKind.BloodBag && !HasPlasmaSolution &&
          _bloodBagWithoutPlasmaPolicy == BloodBagWithoutPlasmaPolicy.CancelTry)
        return RapidInfuserFluidCancellationReason.PlasmaAppliedRequired;

      return RapidInfuserFluidCancellationReason.None;
    }

    private static bool IsFluidFamily(string itemIdentifier, FluidKind kind)
    {
      if (string.IsNullOrWhiteSpace(itemIdentifier))
        return false;
      return kind switch
      {
        FluidKind.NormalSaline => itemIdentifier.StartsWith("normal_saline", StringComparison.Ordinal),
        FluidKind.PlasmaSolution => itemIdentifier.StartsWith("plasma_solution", StringComparison.Ordinal),
        FluidKind.BloodBag =>
          string.Equals(itemIdentifier, BloodBag.Identifier, StringComparison.Ordinal),
        _ => false
      };
    }

    private string FindFluidInventoryItem(PlayerController player, FluidKind kind)
    {
      return player?.FindFirstInventoryItem(identifier => IsFluidFamily(identifier, kind));
    }

    private void RequestAddFluid(FluidKind kind, string itemIdentifier, PlayerController player)
    {
      if (!IsClientStarted && !IsServerStarted)
      {
        TryApplyFluidOffline(kind, itemIdentifier, player);
        return;
      }
      if (IsServerStarted)
      {
        TryApplyFluidOnServer(kind, itemIdentifier, player);
      }
      else
        CmdRequestAddFluid((byte)kind, itemIdentifier);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdRequestAddFluid(byte rawKind, string itemIdentifier, NetworkConnection sender = null)
    {
      if (rawKind > (byte)FluidKind.BloodBag || sender == null || !sender.IsValid)
        return;

      var kind = (FluidKind)rawKind;
      var player = FindPlayer(sender.ClientId);
      RaiseFluidTry(kind, itemIdentifier, player);
      var cancellationReason = GetRequestCancellationReason(kind, itemIdentifier, player);
      if (cancellationReason != RapidInfuserFluidCancellationReason.None)
      {
        RaiseFluidCancelled(kind, itemIdentifier, player, cancellationReason);
        return;
      }

      SetPendingFluid(kind, sender.ClientId, itemIdentifier);
      SendFluidConsumptionConfirmation(kind, sender);
    }

    [TargetRpc]
    private void TargetConfirmFluidConsumption(
      NetworkConnection connection,
      byte rawKind,
      string itemIdentifier)
    {
      if (rawKind > (byte)FluidKind.BloodBag)
        return;

      var kind = (FluidKind)rawKind;
      var player = FindLocalOwnerPlayer();
      if (HasClientConfirmedConsumption(kind))
      {
        CmdAcknowledgeFluidConsumption(rawKind);
        return;
      }
      if (player == null ||
          !IsFluidFamily(itemIdentifier, kind) ||
          player.CountItemInInventory(itemIdentifier) < 1 ||
          player.RemoveItemFromInventory(itemIdentifier, 1) != 1)
      {
        CmdReportFluidConsumptionFailure(rawKind);
        return;
      }

      SetClientConfirmedConsumption(kind);
      CmdAcknowledgeFluidConsumption(rawKind);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdAcknowledgeFluidConsumption(byte rawKind, NetworkConnection sender = null)
    {
      if (rawKind > (byte)FluidKind.BloodBag || sender == null || !sender.IsValid)
        return;

      var kind = (FluidKind)rawKind;
      if (!MatchesPendingFluid(kind, sender.ClientId))
        return;

      var player = FindPlayer(sender.ClientId);
      var itemIdentifier = GetPendingFluidItemIdentifier(kind);
      var cancellationReason = GetFluidCancellationReason(kind);
      ClearPendingFluid(kind);
      if (cancellationReason != RapidInfuserFluidCancellationReason.None)
      {
        RaiseFluidCancelled(kind, itemIdentifier, player, cancellationReason);
        if (player?.Owner != null && !string.IsNullOrWhiteSpace(itemIdentifier))
          TargetRefundFluidConsumption(player.Owner, itemIdentifier);
        return;
      }
      SetFluidOnServer(kind, itemIdentifier, player);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdReportFluidConsumptionFailure(byte rawKind, NetworkConnection sender = null)
    {
      if (rawKind > (byte)FluidKind.BloodBag || sender == null || !sender.IsValid)
        return;

      var kind = (FluidKind)rawKind;
      if (MatchesPendingFluid(kind, sender.ClientId))
      {
        var player = FindPlayer(sender.ClientId);
        var itemIdentifier = GetPendingFluidItemIdentifier(kind);
        ClearPendingFluid(kind);
        RaiseFluidCancelled(kind, itemIdentifier, player, RapidInfuserFluidCancellationReason.ConsumptionFailed);
      }
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
      if (!IsWithinCLineConnectionDistance(patient))
        return;

      _connectedPatientIdentifier.Value = patient.Identifier;
      TryLoadStateFromPatient(patient);
    }

    private void ApplyStateOnServer(Level1RapidInfuserState state)
    {
      _hasNormalSaline.Value = state.HasNormalSaline;
      _hasPlasmaSolution.Value = state.HasPlasmaSolution;
      _hasBloodBag.Value = state.HasBloodBag;
      _connectedPatientIdentifier.Value = state.ConnectedPatientIdentifier ?? string.Empty;
      ApplyDisplays();
    }

    private void ApplyStateOffline(Level1RapidInfuserState state)
    {
      _initialHasNormalSaline = state.HasNormalSaline;
      _initialHasPlasmaSolution = state.HasPlasmaSolution;
      _initialHasBloodBag = state.HasBloodBag;
      _initialConnectedPatientIdentifier = state.ConnectedPatientIdentifier ?? string.Empty;
      ApplyDisplays();
    }

    private void TryApplyFluidOffline(FluidKind kind, string itemIdentifier, PlayerController player)
    {
      RaiseFluidTry(kind, itemIdentifier, player);
      var cancellationReason = GetRequestCancellationReason(kind, itemIdentifier, player);
      if (cancellationReason != RapidInfuserFluidCancellationReason.None)
      {
        RaiseFluidCancelled(kind, itemIdentifier, player, cancellationReason);
        return;
      }

      if (player.RemoveItemFromInventory(itemIdentifier, 1) != 1)
      {
        RaiseFluidCancelled(kind, itemIdentifier, player, RapidInfuserFluidCancellationReason.ConsumptionFailed);
        return;
      }
      SetFluidOffline(kind, itemIdentifier, player);
    }

    private void TryApplyFluidOnServer(FluidKind kind, string itemIdentifier, PlayerController player)
    {
      RaiseFluidTry(kind, itemIdentifier, player);
      var cancellationReason = GetRequestCancellationReason(kind, itemIdentifier, player);
      if (cancellationReason != RapidInfuserFluidCancellationReason.None)
      {
        RaiseFluidCancelled(kind, itemIdentifier, player, cancellationReason);
        return;
      }

      if (player.RemoveItemFromInventory(itemIdentifier, 1) != 1)
      {
        RaiseFluidCancelled(kind, itemIdentifier, player, RapidInfuserFluidCancellationReason.ConsumptionFailed);
        return;
      }
      SetFluidOnServer(kind, itemIdentifier, player);
    }

    private RapidInfuserFluidCancellationReason GetRequestCancellationReason(
      FluidKind kind, string itemIdentifier, PlayerController player)
    {
      if (player == null)
        return RapidInfuserFluidCancellationReason.PlayerUnavailable;
      if (!IsFluidFamily(itemIdentifier, kind))
        return RapidInfuserFluidCancellationReason.UnsupportedItem;
      var stateReason = GetFluidCancellationReason(kind);
      if (stateReason != RapidInfuserFluidCancellationReason.None)
        return stateReason;
      if (!IsWithinInteractionDistance(player))
        return RapidInfuserFluidCancellationReason.OutOfInteractionRange;
      if (HasActivePendingFluid(kind))
        return RapidInfuserFluidCancellationReason.PendingConfirmation;
      return player.CountItemInInventory(itemIdentifier) < 1
        ? RapidInfuserFluidCancellationReason.ItemUnavailable
        : RapidInfuserFluidCancellationReason.None;
    }

    private void SetFluidOffline(FluidKind kind, string itemIdentifier, PlayerController player)
    {
      if (kind == FluidKind.NormalSaline)
        _initialHasNormalSaline = true;
      else if (kind == FluidKind.PlasmaSolution)
        _initialHasPlasmaSolution = true;
      else
        _initialHasBloodBag = true;
      ApplyDisplays();
      RaiseFluidApplied(kind, itemIdentifier, player);
      RaiseScenarioConnectionSignal(kind);
    }

    private void SetFluidOnServer(FluidKind kind, string itemIdentifier, PlayerController player)
    {
      if (kind == FluidKind.NormalSaline)
        _hasNormalSaline.Value = true;
      else if (kind == FluidKind.PlasmaSolution)
        _hasPlasmaSolution.Value = true;
      else
        _hasBloodBag.Value = true;
      ApplyDisplays();
      RaiseFluidApplied(kind, itemIdentifier, player);
      RaiseScenarioConnectionSignal(kind);
    }

    private void RaiseFluidTry(FluidKind kind, string itemIdentifier, PlayerController player)
    {
      if (kind == FluidKind.PlasmaSolution)
        OnPlasmaTry?.Invoke(CreateFluidEvent(kind, itemIdentifier, player, RapidInfuserFluidCancellationReason.None));
      else if (kind == FluidKind.BloodBag)
        OnBloodTry?.Invoke(CreateFluidEvent(kind, itemIdentifier, player, RapidInfuserFluidCancellationReason.None));
    }

    private void RaiseFluidCancelled(FluidKind kind, string itemIdentifier, PlayerController player,
      RapidInfuserFluidCancellationReason reason)
    {
      if (kind == FluidKind.PlasmaSolution)
        OnPlasmaCancelled?.Invoke(CreateFluidEvent(kind, itemIdentifier, player, reason));
      else if (kind == FluidKind.BloodBag)
        OnBloodCancelled?.Invoke(CreateFluidEvent(kind, itemIdentifier, player, reason));
    }

    private void RaiseFluidApplied(FluidKind kind, string itemIdentifier, PlayerController player)
    {
      if (kind == FluidKind.PlasmaSolution)
        OnPlasmaApplied?.Invoke(CreateFluidEvent(kind, itemIdentifier, player, RapidInfuserFluidCancellationReason.None));
      else if (kind == FluidKind.BloodBag)
        OnBloodApplied?.Invoke(CreateFluidEvent(kind, itemIdentifier, player, RapidInfuserFluidCancellationReason.None));
    }

    private RapidInfuserFluidLifecycleEvent CreateFluidEvent(FluidKind kind, string itemIdentifier,
      PlayerController player, RapidInfuserFluidCancellationReason cancellationReason) => new()
      {
        FluidKind = kind.ToString(),
        ItemIdentifier = itemIdentifier ?? string.Empty,
        PlayerIdentifier = player != null ? player.name : string.Empty,
        PlayerClientId = player?.Owner != null ? player.Owner.ClientId : -1,
        RapidInfuserIdentifier = _registeredIdentifier ?? string.Empty,
        CancellationReason = cancellationReason,
        BloodWithoutPlasmaPolicy = _bloodBagWithoutPlasmaPolicy,
        IsServer = IsServerStarted
      };

    private void RaiseBloodPlasmaRequiredSignal(RapidInfuserFluidLifecycleEvent lifecycleEvent)
    {
      if (lifecycleEvent.CancellationReason != RapidInfuserFluidCancellationReason.PlasmaAppliedRequired ||
          (!IsServerStarted && IsClientStarted))
        return;
      ScenarioInteractionSignals.Raise(BloodPlasmaRequiredSignal, JsonUtility.ToJson(lifecycleEvent));
      if ((_bloodBagCancellationBehaviour &
           BloodBagCancellationBehaviour.ShowPlasmaRequiredDialogue) == 0)
        return;

      var player = FindPlayer(lifecycleEvent.PlayerClientId);
      if (IsClientStarted && player != null && player.IsOwner)
      {
        PresentPlasmaRequiredDialogue();
        return;
      }
      if (player?.Owner != null)
        TargetPresentPlasmaRequiredDialogue(player.Owner);
    }

    [TargetRpc]
    private void TargetPresentPlasmaRequiredDialogue(NetworkConnection connection)
      => PresentPlasmaRequiredDialogue();

    [TargetRpc]
    private void TargetRefundFluidConsumption(NetworkConnection connection, string itemIdentifier)
    {
      var player = FindLocalOwnerPlayer();
      var item = MultiplayerInfrastructure.Registry.Registry.CreateItemInstance(itemIdentifier);
      if (player == null || item == null)
      {
        Debug.LogWarning($"[Level1RapidInfuser] Failed to refund '{itemIdentifier}' after cancelled fluid application.");
        return;
      }
      if (player.TryAddItemToInventory(item, out var leftover))
        return;
      if (leftover != null && player.TryDropItemInFront(leftover))
        return;
      Debug.LogWarning($"[Level1RapidInfuser] Failed to return or drop refund '{itemIdentifier}'.");
    }

    private static void PresentPlasmaRequiredDialogue()
    {
      var dialogue = MultiplayerInfrastructure.Registry.Registry.Get<DialoguePanelUIController>(
        RegistryType.UI, MultiplayerInfrastructure.Registry.Registry.TypeKey<DialoguePanelUIController>());
      if (dialogue == null)
        dialogue = FindFirstObjectByType<DialoguePanelUIController>(FindObjectsInactive.Exclude);
      if (dialogue == null)
      {
        Debug.LogWarning("[Level1RapidInfuser] Dialogue UI is unavailable for plasma prerequisite feedback.");
        return;
      }
      dialogue.TryPresentTransientDialogue(
        "{PLAYER_NAME}",
        "플라즈마 솔루션을 먼저 넣어야 할 것 같다");
    }

    private static void RaiseScenarioConnectionSignal(FluidKind kind)
    {
      if (kind == FluidKind.PlasmaSolution)
        ScenarioInteractionSignals.Raise("connect_ps1_to_lv1");
      else if (kind == FluidKind.BloodBag)
        ScenarioInteractionSignals.Raise("connect_blood_to_lv1");
    }

    // ── C-line 연결 ────────────────────────────────────────────────────

    private const string CLineConnectSignal = "connect_cline_to_lv1";
    private const string PatientAIdentifier = "patient_a";

    /// <summary>
    /// C라인 연결 상호작용 가능 여부를 판정한다.
    /// 급속 주입기에 PlasmaSolution과 BloodBag이 모두 장착되어 있고,
    /// patient_a가 반경 내에 있으며, C라인 연결 지점이 열려 있어야 한다.
    /// </summary>
    private bool CanAttemptCLineConnection()
    {
      if (!HasPlasmaSolution || !HasBloodBag)
        return false;
      if (_centralLineConnectionPoint == null)
        return false;

      var patient = FindPatientAInRange();
      if (patient == null)
        return false;

      return patient.IsClineConnectionAvailable();
    }

    private PatientController FindPatientAInRange()
    {
      if (!Registry.TryGetEntity(PatientAIdentifier, out var descriptor)
          || descriptor?.GameObject == null)
        return null;

      if (!descriptor.GameObject.TryGetComponent<PatientController>(out var patient))
        return null;

      return IsWithinCLineConnectionDistance(patient) ? patient : null;
    }

    /// <summary>
    /// C라인을 이을 수 있는 거리인지 판정한다. 거리는 주입기와 환자의 원점이 아니라 실제로
    /// 이어질 두 연결 지점 사이에서 잰다. 주입기를 침대 발치나 머리맡에 세우면 원점 사이
    /// 거리가 아이템 상호작용 반경(<see cref="InteractionDistance"/>)을 쉽게 넘겨서, 다른
    /// 조건이 모두 갖춰졌는데도 연결 상호작용이 조용히 사라졌다. 기준값은 서버가 환자 결합을
    /// 검증할 때 쓰던 값과 같은 <see cref="CLineConnectionDistance"/> 이다.
    /// </summary>
    private bool IsWithinCLineConnectionDistance(PatientController patient)
    {
      if (patient == null)
        return false;

      var patientPoint = patient.CentralLineAttachmentPoint;
      Vector3 patientPosition = patientPoint != null
        ? patientPoint.transform.position
        : patient.transform.position;

      return (patientPosition - IvConnectionPoint.position).sqrMagnitude
             <= CLineConnectionDistance * CLineConnectionDistance;
    }

    /// <summary>
    /// C라인 연결 상호작용의 노출 여부를 감시한다. 힌트 목록은 감지된 상호작용 대상 집합이
    /// 달라질 때만 다시 만들어지므로, 플레이어가 주입기 옆에 선 채로 의사가 중심정맥관을
    /// 삽입하거나 침대가 자리를 잡으면 조건이 충족되어도 목록이 갱신되지 않는다.
    /// 노출 여부가 바뀐 순간에 로컬 플레이어의 힌트를 다시 그리게 한다.
    /// </summary>
    private void RefreshCLineHintOnAvailabilityChange()
    {
      if (Time.unscaledTime < _nextCLineAvailabilityCheckAt)
        return;
      _nextCLineAvailabilityCheckAt = Time.unscaledTime + CLineAvailabilityCheckInterval;

      bool available = CanAttemptCLineConnection();
      if (available == _cLineConnectionAvailable)
        return;

      _cLineConnectionAvailable = available;
      LogFlow($"C-line connection availability changed to {available}");
      FindLocalOwnerPlayer()?.RefreshInteractableHintsNow();
    }

    private void RequestCLineConnection()
    {
      LogFlow("RequestCLineConnection begin");
      if (!IsClientStarted && !IsServerStarted)
      {
        TryCreateCLineConnection();
        return;
      }
      if (IsServerStarted)
        TryCreateCLineConnection();
      else
        CmdRequestCLineConnection();
    }

    private void TryCreateCLineConnection()
    {
      var patient = FindPatientAInRange();
      if (patient == null)
      {
        LogFlow("TryCreateCLineConnection rejected: patient_a not in range", true);
        return;
      }

      var centralLinePoint = patient.CentralLineAttachmentPoint;
      if (centralLinePoint == null || _centralLineConnectionPoint == null)
      {
        LogFlow("TryCreateCLineConnection rejected: connection point missing", true);
        return;
      }

      var service = LineConnectionService.TopologyService
                    ?? FindFirstObjectByType<LineConnectionService>(FindObjectsInactive.Include);
      if (service == null)
      {
        LogFlow("TryCreateCLineConnection rejected: LineConnectionService unavailable", true);
        return;
      }

      if (service.TryCreateAutomaticConnection(_centralLineConnectionPoint, centralLinePoint))
      {
        ConnectPatient(patient);
        LogFlow("TryCreateCLineConnection success");
        ScenarioInteractionSignals.Raise(CLineConnectSignal);
      }
      else
      {
        LogFlow("TryCreateCLineConnection failed: service rejected connection", true);
      }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdRequestCLineConnection(NetworkConnection sender = null)
    {
      if (!HasPlasmaSolution || !HasBloodBag)
        return;
      if (_centralLineConnectionPoint == null)
        return;

      var patient = FindPatientAInRange();
      if (patient == null || !patient.IsClineConnectionAvailable())
        return;

      TryCreateCLineConnection();
    }

    private bool IsWithinInteractionDistance(PlayerController player)
    {
      return player != null &&
             (player.transform.position - transform.position).sqrMagnitude <=
             InteractionDistance * InteractionDistance;
    }

    private bool HasActivePendingFluid(FluidKind kind)
    {
      int clientId = kind switch
      {
        FluidKind.NormalSaline => _pendingSalineClientId,
        FluidKind.PlasmaSolution => _pendingPlasmaClientId,
        _ => _pendingBloodClientId
      };
      if (clientId < 0)
        return false;
      // 소비 확인은 클라이언트의 실제 인벤토리 변경 뒤에 도착한다. 시간 만료로 pending을
      // 먼저 버리면 지연된 확인이 무시되어 아이템이 유실되므로, 연결된 플레이어가 있는 동안
      // 보류 상태를 유지한다. 연결 해제 시에는 다음 조회에서 안전하게 정리한다.
      if (FindPlayer(clientId) != null)
        return true;
      ClearPendingFluid(kind);
      return false;
    }

    private void SetPendingFluid(FluidKind kind, int clientId, string itemIdentifier)
    {
      if (kind == FluidKind.NormalSaline)
      {
        _pendingSalineClientId = clientId;
        _pendingSalineItemIdentifier = itemIdentifier;
      }
      else
      {
        if (kind == FluidKind.PlasmaSolution)
        {
          _pendingPlasmaClientId = clientId;
          _pendingPlasmaItemIdentifier = itemIdentifier;
        }
        else
        {
          _pendingBloodClientId = clientId;
          _pendingBloodItemIdentifier = itemIdentifier;
        }
      }
    }

    private void RetryPendingFluidConfirmations()
    {
      RetryPendingFluidConfirmation(FluidKind.NormalSaline);
      RetryPendingFluidConfirmation(FluidKind.PlasmaSolution);
      RetryPendingFluidConfirmation(FluidKind.BloodBag);
    }

    private void RetryPendingFluidConfirmation(FluidKind kind)
    {
      if (!HasActivePendingFluid(kind))
        return;
      float lastSentAt = kind switch
      {
        FluidKind.NormalSaline => _pendingSalineLastConfirmationSentAt,
        FluidKind.PlasmaSolution => _pendingPlasmaLastConfirmationSentAt,
        _ => _pendingBloodLastConfirmationSentAt
      };
      if (Time.unscaledTime - lastSentAt < FluidConfirmationRetrySeconds)
        return;

      int clientId = kind switch
      {
        FluidKind.NormalSaline => _pendingSalineClientId,
        FluidKind.PlasmaSolution => _pendingPlasmaClientId,
        _ => _pendingBloodClientId
      };
      var player = FindPlayer(clientId);
      if (player?.Owner != null)
        SendFluidConsumptionConfirmation(kind, player.Owner);
    }

    private void SendFluidConsumptionConfirmation(FluidKind kind, NetworkConnection connection)
    {
      SetPendingFluidConfirmationSentAt(kind, Time.unscaledTime);
      TargetConfirmFluidConsumption(connection, (byte)kind, GetPendingFluidItemIdentifier(kind));
    }

    private void SetPendingFluidConfirmationSentAt(FluidKind kind, float value)
    {
      if (kind == FluidKind.NormalSaline)
        _pendingSalineLastConfirmationSentAt = value;
      else if (kind == FluidKind.PlasmaSolution)
        _pendingPlasmaLastConfirmationSentAt = value;
      else
        _pendingBloodLastConfirmationSentAt = value;
    }

    private bool HasClientConfirmedConsumption(FluidKind kind) => kind switch
    {
      FluidKind.NormalSaline => _clientConfirmedSalineConsumption,
      FluidKind.PlasmaSolution => _clientConfirmedPlasmaConsumption,
      _ => _clientConfirmedBloodConsumption
    };

    private void SetClientConfirmedConsumption(FluidKind kind)
    {
      if (kind == FluidKind.NormalSaline)
        _clientConfirmedSalineConsumption = true;
      else if (kind == FluidKind.PlasmaSolution)
        _clientConfirmedPlasmaConsumption = true;
      else
        _clientConfirmedBloodConsumption = true;
    }

    private bool MatchesPendingFluid(FluidKind kind, int clientId)
    {
      if (!HasActivePendingFluid(kind))
        return false;
      return (kind switch
      {
        FluidKind.NormalSaline => _pendingSalineClientId,
        FluidKind.PlasmaSolution => _pendingPlasmaClientId,
        _ => _pendingBloodClientId
      }) == clientId;
    }

    private void ClearPendingFluid(FluidKind kind)
    {
      if (kind == FluidKind.NormalSaline)
      {
        _pendingSalineClientId = -1;
        _pendingSalineItemIdentifier = null;
      }
      else
      {
        if (kind == FluidKind.PlasmaSolution)
        {
          _pendingPlasmaClientId = -1;
          _pendingPlasmaItemIdentifier = null;
        }
        else
        {
          _pendingBloodClientId = -1;
          _pendingBloodItemIdentifier = null;
        }
      }
    }

    private string GetPendingFluidItemIdentifier(FluidKind kind) => kind switch
    {
      FluidKind.NormalSaline => _pendingSalineItemIdentifier,
      FluidKind.PlasmaSolution => _pendingPlasmaItemIdentifier,
      _ => _pendingBloodItemIdentifier
    };

    private void OnFluidChanged(bool previous, bool next, bool asServer)
    {
      ApplyDisplays();
      FindLocalOwnerPlayer()?.RefreshInteractableHintsNow();
    }

    private void ApplyDisplays()
    {
      EnsureDisplayReferences();
      if (_normalSalineDisplay != null)
        _normalSalineDisplay.SetActive(HasNormalSaline);
      if (_plasmaSolutionDisplay != null)
        _plasmaSolutionDisplay.SetActive(HasPlasmaSolution);
      if (_bloodBagDisplay != null)
        _bloodBagDisplay.SetActive(HasBloodBag);
    }

    private void EnsureDisplayReferences()
    {
      _normalSalineDisplay ??= FindChildGameObject("NormalSalineDisplay")
                               ?? FindClosestDirectChild(new Vector3(-0.15f, 1.3f, 0f));
      _plasmaSolutionDisplay ??= FindChildGameObject("PlasmaSolutionDisplay")
                                 ?? FindClosestDirectChild(new Vector3(0.15f, 1.3f, 0f));
      _bloodBagDisplay ??= FindChildGameObject("BloodBagDisplay")
                           ?? FindClosestDirectChild(new Vector3(0f, 1.3f, 0.15f));
    }

    private GameObject FindChildGameObject(string childName)
    {
      var children = GetComponentsInChildren<Transform>(true);
      for (int i = 0; i < children.Length; i++)
        if (children[i] != null && string.Equals(children[i].name, childName, StringComparison.Ordinal))
          return children[i].gameObject;
      return null;
    }

    private GameObject FindClosestDirectChild(Vector3 expectedLocalPosition)
    {
      Transform closest = null;
      float closestDistance = 0.01f * 0.01f;
      for (int i = 0; i < transform.childCount; i++)
      {
        var child = transform.GetChild(i);
        float distance = (child.localPosition - expectedLocalPosition).sqrMagnitude;
        if (distance > closestDistance)
          continue;
        closest = child;
        closestDistance = distance;
      }
      return closest != null ? closest.gameObject : null;
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
