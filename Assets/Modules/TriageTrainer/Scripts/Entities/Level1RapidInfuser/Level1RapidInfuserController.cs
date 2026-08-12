using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.UI;
using TriageTrainer.Entity.IntravenousLine;
using TriageTrainer.ItemDefinitions;
using MultiplayerInfrastructure.Logging;
using UnityEngine;

namespace TriageTrainer.Entity
{
  [Serializable]
  public struct Level1RapidInfuserState
  {
    public bool HasNormalSaline;
    public bool HasPlasmaSolution;
    public bool HasBloodTransfusionSet;
    public string ConnectedPatientIdentifier;
  }

  /// <summary>
  /// 환자 저장 상태가 급속 주입기 상태를 보유할 경우 구현하는 선택적 복원 규약.
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
    IInteractable, IInteract, IInteractorConditional, ISpawnedEntityIdentifierReceiver,
    IItemizableWorldEntity
  {
    private const string FlowTag = "RapidInfuserFlow";
    private void LogFlow(string message, bool warning = false)
    {
      string role = IsServerStarted ? "server" : (IsClientStarted ? "client" : "offline");
      string id = string.IsNullOrWhiteSpace(_registeredIdentifier) ? "<unregistered>" : _registeredIdentifier;
      string full = $"[LV1RapidInfuser] role={role} object='{name}' id='{id}' {message}";
      GameLogService.Write(warning ? GameLogCategory.Misc : GameLogCategory.Interaction, full, FlowTag);
      if (warning) Debug.LogWarning(full, this); else Debug.Log(full, this);
    }
    private enum FluidKind : byte
    {
      NormalSaline,
      PlasmaSolution,
      BloodTransfusionSet
    }

    private sealed class AddFluidInteract : IInteract, IInteractorConditional, IInteractDisplayIcons
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
        ? "Normal Saline 추가"
        : _kind == FluidKind.PlasmaSolution
          ? "Plasma Solution 연결"
          : "Blood transfusion set 연결";
      public Sprite DisplayIcon => null;
      public IReadOnlyList<Sprite> DisplayIcons => new[] { Icon.ClearRightBottom, _heldItemIcon };
      public bool AllowDisplayIconFallback => true;
      public Color DisplayColor => Color.white;

      public bool CanInteract(Transform interactor)
      {
        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        string id = player?.HandlingItem?.CurrentIdentifier;
        _heldItemIcon = player?.HandlingItem?.CurrentItemIconTexture;
        return _owner.CanAddFluid(_kind) && IsFluidFamily(id, _kind);
      }

      public void Interact(Transform interactor)
      {
        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        string id = player?.HandlingItem?.CurrentIdentifier;
        if (player != null && _owner.CanAddFluid(_kind) && IsFluidFamily(id, _kind))
          _owner.RequestAddFluid(_kind, id, player);
      }
    }

    [Header("Displays (assign on prefab)")]
    [SerializeField] private GameObject _normalSalineDisplay;
    [SerializeField] private GameObject _plasmaSolutionDisplay;
    [SerializeField] private GameObject _bloodTransfusionSetDisplay;

    [Header("IV connection (assign on prefab)")]
    [SerializeField] private IntravenousLineConnectionPoint _ivConnectionPoint;

    [Header("Offline / initial state")]
    [SerializeField] private bool _initialHasNormalSaline;
    [SerializeField] private bool _initialHasPlasmaSolution;
    [SerializeField] private bool _initialHasBloodTransfusionSet;
    [SerializeField] private string _initialConnectedPatientIdentifier;

    [Header("Identity / interaction")]
    [SerializeField] private string _displayText = "Level 1 급속 주입기 조종";
    [SerializeField] private Sprite _displayIcon;

    private readonly SyncVar<bool> _hasNormalSaline = new(false);
    private readonly SyncVar<bool> _hasPlasmaSolution = new(false);
    private readonly SyncVar<bool> _hasBloodTransfusionSet = new(false);
    private readonly SyncVar<string> _connectedPatientIdentifier = new(string.Empty);
    private readonly SyncVar<string> _runtimeIdentifier = new(string.Empty);
    private IInteract[] _fluidInteracts;
    private string _registeredIdentifier;
    private int _pendingSalineClientId = -1;
    private int _pendingPlasmaClientId = -1;
    private int _pendingBloodClientId = -1;
    private float _pendingSalineExpiresAt;
    private float _pendingPlasmaExpiresAt;
    private float _pendingBloodExpiresAt;
    private bool _itemizationPending;
    private const float InteractionDistance = 3f;
    private const float FluidRequestTimeout = 3f;

    public IEnumerable<IInteract> AdditionalInteracts =>
      _fluidInteracts ??= new IInteract[]
      {
        new AddFluidInteract(this, FluidKind.NormalSaline),
        new AddFluidInteract(this, FluidKind.PlasmaSolution),
        new AddFluidInteract(this, FluidKind.BloodTransfusionSet)
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
    public bool HasBloodTransfusionSet => IsClientStarted || IsServerStarted
      ? _hasBloodTransfusionSet.Value
      : _initialHasBloodTransfusionSet;
    public string ConnectedPatientIdentifier => IsClientStarted || IsServerStarted
      ? _connectedPatientIdentifier.Value
      : _initialConnectedPatientIdentifier;
    public Transform IvConnectionPoint =>
      _ivConnectionPoint != null ? _ivConnectionPoint.transform : transform;

    private void Awake()
    {
      Awake_MinecraftBoadLikeControl();
      Configure(1); // Level 1 Rapid Infuser는 한 명만 조종한다.
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
      _hasBloodTransfusionSet.Value = _initialHasBloodTransfusionSet;
      _connectedPatientIdentifier.Value = _initialConnectedPatientIdentifier ?? string.Empty;
      ApplyDisplays();
    }

    public override void OnStartClient()
    {
      base.OnStartClient();
      _hasNormalSaline.OnChange += OnFluidChanged;
      _hasPlasmaSolution.OnChange += OnFluidChanged;
      _hasBloodTransfusionSet.OnChange += OnFluidChanged;
      _runtimeIdentifier.OnChange += OnRuntimeIdentifierChanged;
      RegisterEntity();
      ApplyDisplays();
    }

    public override void OnStopClient()
    {
      _hasNormalSaline.OnChange -= OnFluidChanged;
      _hasPlasmaSolution.OnChange -= OnFluidChanged;
      _hasBloodTransfusionSet.OnChange -= OnFluidChanged;
      _runtimeIdentifier.OnChange -= OnRuntimeIdentifierChanged;
      UnregisterEntity();
      base.OnStopClient();
    }

    public void Interact(Transform interactor) => Toggle(interactor);

    public bool CanInteract(Transform interactor) => CanToggle(interactor);

    public string ItemizationEntityIdentifier => _registeredIdentifier;

    // PlayerController의 공격 처리와 별개로, 설치체 자체의 Collider 클릭도 회수 요청으로 취급한다.
    // 카메라 레이캐스트 레이어/입력 소비 상태 때문에 Attack 경로가 건너뛰어져도 회수할 수 있다.
    private void OnMouseDown()
    {
      var player = FindLocalOwnerPlayer();
      LogFlow($"OnMouseDown player={(player == null ? "<null>" : player.name)}");
      if (player != null)
        RequestItemization(player);
    }

    /// <summary>
    /// 좌클릭한 설치형 주입기를 획득 가능한 월드 아이템으로 되돌린다.
    /// 네트워크에서는 서버가 거리 검증 후 아이템을 스폰하고 엔티티를 despawn한다.
    /// </summary>
    public bool RequestItemization(PlayerController player)
    {
      LogFlow($"RequestItemization player={(player == null ? "<null>" : player.name)} pending={_itemizationPending} distanceOk={IsWithinInteractionDistance(player)}");
      if (_itemizationPending)
        return true;
      if (player == null || !IsWithinInteractionDistance(player))
      {
        LogFlow("RequestItemization rejected: invalid player or interaction distance", true);
        return false;
      }

      bool accepted = player.RequestItemization(this);
      if (accepted && !InstanceFinder.IsServerStarted && player.IsSpawned)
        _itemizationPending = true;
      return accepted;
    }

    public bool TryItemizeOnServer(PlayerController player)
    {
      LogFlow($"TryItemizeOnServer player={(player == null ? "<null>" : player.name)} pending={_itemizationPending} distanceOk={IsWithinInteractionDistance(player)}");
      if (_itemizationPending)
        return false;
      if (player == null || !IsWithinInteractionDistance(player))
      {
        LogFlow("TryItemizeOnServer rejected: invalid player or interaction distance", true);
        return false;
      }

      _itemizationPending = true;
      bool itemized = SpawnItemAndDespawn(player);
      LogFlow($"TryItemizeOnServer result={itemized}");
      if (!itemized)
        _itemizationPending = false;
      return itemized;
    }

    private bool SpawnItemAndDespawn(PlayerController player)
    {
      LogFlow($"SpawnItemAndDespawn begin player={player.name}");
      var item = MultiplayerInfrastructure.Registry.Registry.CreateItemInstance(Level1RapidInfuser.Identifier);
      if (item == null)
      {
        LogFlow("SpawnItemAndDespawn failed: item instance creation", true);
        Debug.LogWarning("[Level1RapidInfuser] Failed to create item while itemizing.", this);
        return false;
      }

      Vector3 direction = player.transform.position - transform.position;
      direction.y = 0f;
      if (direction.sqrMagnitude < 0.001f)
        direction = transform.forward;
      direction.Normalize();

      Vector3 position = transform.position + Vector3.up * 0.35f;
      if (!player.TrySpawnWorldItem(item, position, direction * 1.25f))
      {
        LogFlow($"SpawnItemAndDespawn failed: TrySpawnWorldItem position={position}", true);
        return false;
      }

      var networkObject = GetComponent<NetworkObject>();
      if (InstanceFinder.IsServerStarted && networkObject != null && networkObject.IsSpawned)
        InstanceFinder.ServerManager.Despawn(networkObject);
      else
        Destroy(gameObject);
      LogFlow($"SpawnItemAndDespawn success despawnedNetworkObject={networkObject != null && networkObject.IsSpawned}");
      return true;
    }

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
      HasBloodTransfusionSet = HasBloodTransfusionSet,
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
      {
        ConnectPatient(patient);

        // 환자에게 IV 수액 공급원 연결을 알림(좌/우 팔은 환자 측 연결점 식별자로 판정).
        bool isLeftArm = IsLeftArmConnectionPoint(otherPoint);
        patient.SetIVFluidConnection(isLeftArm, this);
      }
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
      {
        // 환자에게 IV 수액 공급원 해제를 알림
        if (patient != null)
        {
          if (otherPoint != null)
          {
            // 연결점 식별자로 좌/우 팔을 판정해 해당 쪽만 해제
            bool isLeftArm = IsLeftArmConnectionPoint(otherPoint);
            patient.ClearIVFluidConnection(isLeftArm, this);
          }
          else
          {
            // 연결점이 파괴되어 팔을 판정할 수 없으므로 양쪽 모두 해제
            patient.ClearIVFluidConnection(isLeftArm: true, expectedSource: this);
            patient.ClearIVFluidConnection(isLeftArm: false, expectedSource: this);
          }
        }
        ConnectPatient(null);
      }
    }

    /// <summary>
    /// 환자 측 IV 연결 지점이 좌측 팔인지 판정한다.
    /// 지점 Identifier 에 "left" 가 포함되면 좌측, 아니면 우측으로 간주한다.
    /// </summary>
    private static bool IsLeftArmConnectionPoint(IntravenousLineConnectionPoint point)
    {
      if (point == null || string.IsNullOrWhiteSpace(point.Identifier))
        return true;

      return point.Identifier.Contains("left", StringComparison.OrdinalIgnoreCase);
    }

    private bool HasFluid(FluidKind kind) => kind switch
    {
      FluidKind.NormalSaline => HasNormalSaline,
      FluidKind.PlasmaSolution => HasPlasmaSolution,
      FluidKind.BloodTransfusionSet => HasBloodTransfusionSet,
      _ => false
    };

    private bool CanAddFluid(FluidKind kind)
    {
      if (HasFluid(kind))
        return false;

      // 그래프는 플라즈마 연결(V019_1) 뒤에 수혈세트 연결(V020)을 검증한다.
      // RuntimeState 신호가 sticky이므로 역순 연결을 허용하면 V020이 나중에 무행동 통과한다.
      return kind != FluidKind.BloodTransfusionSet || HasPlasmaSolution;
    }

    private static bool IsFluidFamily(string itemIdentifier, FluidKind kind)
    {
      if (string.IsNullOrWhiteSpace(itemIdentifier))
        return false;
      return kind switch
      {
        FluidKind.NormalSaline => itemIdentifier.StartsWith("normal_saline", StringComparison.Ordinal),
        FluidKind.PlasmaSolution => itemIdentifier.StartsWith("plasma_solution", StringComparison.Ordinal),
        FluidKind.BloodTransfusionSet =>
          string.Equals(itemIdentifier, BloodTransfusionSet.Identifier, StringComparison.Ordinal),
        _ => false
      };
    }

    private void RequestAddFluid(FluidKind kind, string itemIdentifier, PlayerController player)
    {
      if (!IsClientStarted && !IsServerStarted)
      {
        if (CanAddFluid(kind) && player.RemoveItemFromInventory(itemIdentifier, 1) == 1)
          SetFluidOffline(kind);
        return;
      }
      if (IsServerStarted)
      {
        // 호스트의 인벤토리는 이 인스턴스에 있으므로 즉시 소비/확정할 수 있다.
        if (CanAddFluid(kind) &&
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
      if (rawKind > (byte)FluidKind.BloodTransfusionSet || sender == null || !sender.IsValid)
        return;

      var kind = (FluidKind)rawKind;
      var player = FindPlayer(sender.ClientId);
      if (player == null || !CanAddFluid(kind) || !IsFluidFamily(itemIdentifier, kind) ||
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
      if (rawKind > (byte)FluidKind.BloodTransfusionSet)
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
      if (rawKind > (byte)FluidKind.BloodTransfusionSet || sender == null || !sender.IsValid)
        return;

      var kind = (FluidKind)rawKind;
      if (!MatchesPendingFluid(kind, sender.ClientId) || !CanAddFluid(kind))
        return;

      ClearPendingFluid(kind);
      SetFluidOnServer(kind);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdReportFluidConsumptionFailure(byte rawKind, NetworkConnection sender = null)
    {
      if (rawKind > (byte)FluidKind.BloodTransfusionSet || sender == null || !sender.IsValid)
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
      _hasBloodTransfusionSet.Value = state.HasBloodTransfusionSet;
      _connectedPatientIdentifier.Value = state.ConnectedPatientIdentifier ?? string.Empty;
      ApplyDisplays();
    }

    private void ApplyStateOffline(Level1RapidInfuserState state)
    {
      _initialHasNormalSaline = state.HasNormalSaline;
      _initialHasPlasmaSolution = state.HasPlasmaSolution;
      _initialHasBloodTransfusionSet = state.HasBloodTransfusionSet;
      _initialConnectedPatientIdentifier = state.ConnectedPatientIdentifier ?? string.Empty;
      ApplyDisplays();
    }

    private void SetFluidOffline(FluidKind kind)
    {
      if (kind == FluidKind.NormalSaline) _initialHasNormalSaline = true;
      else if (kind == FluidKind.PlasmaSolution) _initialHasPlasmaSolution = true;
      else _initialHasBloodTransfusionSet = true;
      ApplyDisplays();
      RaiseScenarioConnectionSignal(kind);
    }

    private void SetFluidOnServer(FluidKind kind)
    {
      if (kind == FluidKind.NormalSaline) _hasNormalSaline.Value = true;
      else if (kind == FluidKind.PlasmaSolution) _hasPlasmaSolution.Value = true;
      else _hasBloodTransfusionSet.Value = true;
      ApplyDisplays();
      RaiseScenarioConnectionSignal(kind);
    }

    private static void RaiseScenarioConnectionSignal(FluidKind kind)
    {
      if (kind == FluidKind.PlasmaSolution)
        ScenarioInteractionSignals.Raise("connect_ps1_to_lv1");
      else if (kind == FluidKind.BloodTransfusionSet)
        ScenarioInteractionSignals.Raise("connect_blood_to_lv1");
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
      float expiry = kind switch
      {
        FluidKind.NormalSaline => _pendingSalineExpiresAt,
        FluidKind.PlasmaSolution => _pendingPlasmaExpiresAt,
        _ => _pendingBloodExpiresAt
      };
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
        if (kind == FluidKind.PlasmaSolution)
        {
          _pendingPlasmaClientId = clientId;
          _pendingPlasmaExpiresAt = Time.unscaledTime + FluidRequestTimeout;
        }
        else
        {
          _pendingBloodClientId = clientId;
          _pendingBloodExpiresAt = Time.unscaledTime + FluidRequestTimeout;
        }
      }
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
        _pendingSalineExpiresAt = 0f;
      }
      else
      {
        if (kind == FluidKind.PlasmaSolution)
        {
          _pendingPlasmaClientId = -1;
          _pendingPlasmaExpiresAt = 0f;
        }
        else
        {
          _pendingBloodClientId = -1;
          _pendingBloodExpiresAt = 0f;
        }
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
      if (_bloodTransfusionSetDisplay != null) _bloodTransfusionSetDisplay.SetActive(HasBloodTransfusionSet);
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
