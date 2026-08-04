#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.Collections.Generic;
using FishNet.Object;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.UI;
using TriageTrainer.Entity.LineConnection;
using TriageTrainer.ItemDefinitions;
using UnityEngine;

namespace TriageTrainer.Entity.IntravenousLine
{
  [RequireComponent(typeof(SphereCollider))]
  public class IntravenousLineConnectionPoint : LineConnectionPoint, IInteractable
  {
    [Serializable]
    public class InteractConfig
    {
      [SerializeField] private string _identifier;
      [SerializeField] private bool _enabled = true;

      public InteractConfig(string identifier, bool enabled = true)
      {
        _identifier = identifier;
        _enabled = enabled;
      }

      public string Identifier => _identifier;
      public bool Enabled
      {
        get => _enabled;
        set => _enabled = value;
      }
    }

    private sealed class StartConnectionInteract : IInteract, IInteractorConditional, IInteractDisplayIcons
    {
      private readonly IntravenousLineConnectionPoint _owner;
      public StartConnectionInteract(IntravenousLineConnectionPoint owner) { _owner = owner; }

      public string DisplayText => "수액 줄 연결 시작";
      public Sprite DisplayIcon => _owner._displayIcon;
      public IReadOnlyList<Sprite> DisplayIcons => new[] { Icon.ClearRightBottom, IntravenousSetIcon };
      public bool AllowDisplayIconFallback => true;
      public Color DisplayColor => Color.white;

      public bool CanInteract(Transform interactor)
      {
        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        if (player == null)
          return false;

        if (!_owner.IsInteractEnabled(InteractIdStartConnectionMode))
          return false;

        if (!_owner.CanAcceptAdditionalConnection)
          return false;

        if (player.IsIntravenousLineConnectionMode)
          return false;

        var controller = _owner.ResolveController();
        return controller != null && _owner.HasRequiredItem(player);
      }

      public void Interact(Transform interactor)
      {
        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        if (player == null)
          return;

        var controller = _owner.ResolveController();
        if (controller == null)
          return;

        controller.BeginConnectionMode(player, _owner);
      }
    }

    private const string IntravenousSetIconPath = "Textures/Items/intravenous_set";
    private static Sprite _intravenousSetIcon;
    private static Sprite IntravenousSetIcon => _intravenousSetIcon != null
      ? _intravenousSetIcon
      : _intravenousSetIcon = Resources.Load<Sprite>(IntravenousSetIconPath);

    private sealed class ConnectHereInteract : IInteract, IInteractorConditional
    {
      private readonly IntravenousLineConnectionPoint _owner;
      public ConnectHereInteract(IntravenousLineConnectionPoint owner) { _owner = owner; }

      public string DisplayText => "여기에 수액 줄 연결";
      public Sprite DisplayIcon => _owner._displayIcon;
      public bool AllowDisplayIconFallback => true;
      public Color DisplayColor => Color.white;

      public bool CanInteract(Transform interactor)
      {
        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        if (player == null)
          return false;

        if (!_owner.IsInteractEnabled(InteractIdConnectHere))
          return false;

        if (!_owner.CanAcceptAdditionalConnection)
          return false;

        if (!player.IsIntravenousLineConnectionMode)
          return false;

        var controller = _owner.ResolveController();
        if (controller == null)
          return false;

        if (!_owner.HasRequiredItem(player))
          return false;

        return controller.HasPendingStartPoint(player, out var startPoint)
          && startPoint != null
          && !ReferenceEquals(startPoint, _owner)
          && startPoint.CanConnectTo(_owner)
          && _owner.CanConnectTo(startPoint);
      }

      public void Interact(Transform interactor)
      {
        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        if (player == null)
          return;

        var controller = _owner.ResolveController();
        if (controller == null)
          return;

        controller.TryCompleteConnection(player, _owner);

        // Nearby interactables list may not change (same collider set),
        // so force hint refresh after attempting completion.
        player.RefreshInteractableHintsNow();
      }
    }

    private sealed class DisconnectInteract : IInteract, IInteractorConditional
    {
      private readonly IntravenousLineConnectionPoint _owner;
      public DisconnectInteract(IntravenousLineConnectionPoint owner) { _owner = owner; }

      public string DisplayText => "수액 줄 해제";

      // 이미 연결된 수액 줄에 대한 해제 상호작용은 투명 아이콘으로 표시한다.
      // (아이콘 없음 + fallback 아이콘도 표시하지 않음 → 배경색 Color.clear 로 렌더)
      public Sprite DisplayIcon => null;
      public bool AllowDisplayIconFallback => false;
      public Color DisplayColor => Color.clear;

      public bool CanInteract(Transform interactor)
      {
        if (!_owner.IsInteractEnabled(InteractIdDisconnect))
          return false;

        return _owner.HasAnyConnection;
      }

      public void Interact(Transform interactor)
      {
        var controller = _owner.ResolveController();
        if (controller == null)
          return;

        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        controller.DisconnectFromPoint(_owner, player);
        player?.RefreshInteractableHintsNow();
      }
    }

    public const string InteractIdStartConnectionMode = "intravenous_line_connect_mode_start";
    public const string InteractIdConnectHere = "intravenous_line_connect_here";
    public const string InteractIdDisconnect = "intravenous_line_disconnect";

    /// <summary>연결 작업 시작(한 점 연결) 시 인게임 서버로 올리는 신호 접두사. 뒤에 지점 Identifier 가 붙는다.</summary>
    public const string ConnectStartSignalPrefix = "iv_connect_start_";

    /// <summary>연결 완료 시 인게임 서버로 올리는 신호 접두사. 뒤에 지점 Identifier 가 붙는다.</summary>
    public const string ConnectedSignalPrefix = "iv_connected_";

    /// <summary>연결 끊김 시 인게임 서버로 올리는 신호 접두사. 뒤에 지점 Identifier 가 붙는다.</summary>
    public const string DisconnectedSignalPrefix = "iv_disconnected_";

    [Header("Service")]
    [SerializeField] private LineConnectionService connectionService;

    [Header("Identifier")]
    [SerializeField] private string _identifier;
    [SerializeField] private bool _autoGenerateIdentifier = true;

    [Header("Interact")]
    [SerializeField] private Sprite _displayIcon;
    [SerializeField] private List<InteractConfig> _interactConfigs = new();

    private List<IInteract> _interacts = new();
    private Dictionary<string, InteractConfig> _interactConfigMap = new(StringComparer.Ordinal);

    /// <summary>
    /// 이 지점에서 수액 줄 연결 작업이 시작될 때(한 점만 연결된 상태) 발생한다.
    /// 인자: 연결 시작 지점(this).
    /// </summary>
    public event Action<IntravenousLineConnectionPoint> OnConnectStart;

    /// <summary>
    /// 이 지점을 포함하는 수액 줄 연결이 완료되었을 때 발생한다.
    /// 인자: (this 지점, 상대 지점). 양 끝점 모두에서 각각 발생한다.
    /// </summary>
    public event Action<IntravenousLineConnectionPoint, IntravenousLineConnectionPoint> OnConnected;

    /// <summary>
    /// 이 지점을 포함하던 수액 줄 하나가 끊겼을 때 발생한다.
    /// 계약: "줄 단위" 이벤트다. 끊긴 줄마다 그 줄의 양 끝점에서 각각 발생하며,
    /// 지점에 다른 연결이 더 남아 있는지 여부와 무관하게 매번 발생한다
    /// (OnConnected 와 대칭). 지점이 완전히 비연결 상태가 되는 시점만 알고 싶다면
    /// 수신 측에서 <see cref="HasAnyConnection"/> 로 확인한다.
    /// 인자: (this 지점, 끊긴 줄의 상대 지점 또는 알 수 없으면 null).
    /// </summary>
    public event Action<IntravenousLineConnectionPoint, IntravenousLineConnectionPoint> OnDisconnected;

    public string Identifier => _identifier;
    public IInteract[] Interacts => _interacts.ToArray();

    private void Awake()
    {
      EnsureDefaults();
      EnsureDetectionCollider();
      BuildInteracts();
    }

    private void OnValidate()
    {
      EnsureDefaults();
      EnsureDetectionCollider();
    }

    private void BuildInteracts()
    {
      RebuildInteractConfigMap();

      _interacts.Clear();
      _interacts.Add(new StartConnectionInteract(this));
      _interacts.Add(new ConnectHereInteract(this));
      _interacts.Add(new DisconnectInteract(this));
    }

    private void EnsureDefaults()
    {
      EnsureRuntimeCollections();
      EnsureIdentifier();

      EnsureInteractConfig(InteractIdStartConnectionMode, true);
      EnsureInteractConfig(InteractIdConnectHere, true);
      EnsureInteractConfig(InteractIdDisconnect, true);
      RebuildInteractConfigMap();
    }

    private void EnsureIdentifier()
    {
      if (!string.IsNullOrWhiteSpace(_identifier))
      {
        _identifier = _identifier.Trim();
        if (!_autoGenerateIdentifier || !HasDuplicateIdentifier(_identifier, this))
          return;
      }

      _identifier = GenerateUniqueIdentifier();
    }

    private string GenerateUniqueIdentifier()
    {
      string baseName = SanitizeIdentifierBase(gameObject != null ? gameObject.name : null);
      if (string.IsNullOrWhiteSpace(baseName))
        baseName = "iv_point";

      string candidate = baseName;
      int suffix = 1;
      while (HasDuplicateIdentifier(candidate, this))
      {
        candidate = $"{baseName}_{suffix:000}";
        suffix++;

        if (suffix > 999)
        {
          candidate = $"{baseName}_{Guid.NewGuid():N}";
          break;
        }
      }

      return candidate;
    }

    private static string SanitizeIdentifierBase(string source)
    {
      if (string.IsNullOrWhiteSpace(source))
        return string.Empty;

      var chars = source.Trim().ToLowerInvariant().ToCharArray();
      for (int i = 0; i < chars.Length; i++)
      {
        if (!char.IsLetterOrDigit(chars[i]))
          chars[i] = '_';
      }

      var sanitized = new string(chars);
      while (sanitized.Contains("__", StringComparison.Ordinal))
        sanitized = sanitized.Replace("__", "_", StringComparison.Ordinal);

      return sanitized.Trim('_');
    }

    private static bool HasDuplicateIdentifier(string identifier, IntravenousLineConnectionPoint self)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return false;

      var points = FindObjectsByType<IntravenousLineConnectionPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
      for (int i = 0; i < points.Length; i++)
      {
        var each = points[i];
        if (each == null || ReferenceEquals(each, self))
          continue;

        if (string.Equals(each._identifier, identifier, StringComparison.Ordinal))
          return true;
      }

      return false;
    }

    public void SetIdentifier(string identifier)
    {
      _identifier = string.IsNullOrWhiteSpace(identifier) ? string.Empty : identifier.Trim();
      EnsureIdentifier();
    }

    private void EnsureRuntimeCollections()
    {
      _interactConfigs ??= new List<InteractConfig>();
      _interacts ??= new List<IInteract>();
      _interactConfigMap ??= new Dictionary<string, InteractConfig>(StringComparer.Ordinal);
    }

    private void EnsureDetectionCollider()
    {
      var sphere = GetComponent<SphereCollider>();
      if (sphere == null)
        return;

      sphere.isTrigger = true;
      if (sphere.radius <= 0.0001f)
        sphere.radius = 0.35f;
    }

    private void EnsureInteractConfig(string identifier, bool enabled)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return;

      for (int i = 0; i < _interactConfigs.Count; i++)
      {
        var each = _interactConfigs[i];
        if (each == null || !string.Equals(each.Identifier, identifier, StringComparison.Ordinal))
          continue;

        return;
      }

      _interactConfigs.Add(new InteractConfig(identifier, enabled));
    }

    private void RebuildInteractConfigMap()
    {
      EnsureRuntimeCollections();

      _interactConfigMap.Clear();
      for (int i = 0; i < _interactConfigs.Count; i++)
      {
        var each = _interactConfigs[i];
        if (each == null || string.IsNullOrWhiteSpace(each.Identifier))
          continue;

        _interactConfigMap[each.Identifier] = each;
      }
    }

    public bool IsInteractEnabled(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return false;

      if (_interactConfigMap.TryGetValue(identifier, out var cfg))
        return cfg.Enabled;

      return false;
    }

    /// <summary>
    /// 연결 작업 시작(한 점 연결)을 알린다. C# 이벤트를 발화하고, 인게임 서버에
    /// 이 지점 Identifier 와 함께 "연결 시도" 시그널을 올린다.
    /// </summary>
    public void NotifyConnectStart()
    {
      OnConnectStart?.Invoke(this);
      RaiseSignalWithIdentifier(ConnectStartSignalPrefix);
    }

    public override void NotifyConnectionStarted() => NotifyConnectStart();

    /// <summary>
    /// 연결 완료를 알린다. C# 이벤트를 발화하고, 인게임 서버에 이 지점 Identifier 와
    /// 함께 "연결 완료" 시그널을 올린다.
    /// </summary>
    public void NotifyConnected(IntravenousLineConnectionPoint other)
    {
      OnConnected?.Invoke(this, other);
      RaiseSignalWithIdentifier(ConnectedSignalPrefix);
    }

    public override void NotifyLineConnected(LineConnectionPoint other) =>
      NotifyConnected(other as IntravenousLineConnectionPoint);

    public override void NotifyReplicatedLineConnected(LineConnectionPoint other) =>
      OnConnected?.Invoke(this, other as IntravenousLineConnectionPoint);

    /// <summary>
    /// 줄 하나의 연결 끊김을 알린다. C# 이벤트를 발화하고, 인게임 서버에 이 지점
    /// Identifier 와 함께 "연결 끊김" 시그널을 올린다. 끊긴 줄마다 호출되며,
    /// 이 지점에 다른 연결이 남아 있는지 여부와 무관하게 매번 알린다.
    /// </summary>
    /// <param name="other">끊긴 줄의 상대 지점(알 수 없으면 null).</param>
    public void NotifyDisconnected(IntravenousLineConnectionPoint other = null)
    {
      OnDisconnected?.Invoke(this, other);
      RaiseSignalWithIdentifier(DisconnectedSignalPrefix);
      if (IsNormalSalineConnectionPair(other))
      {
        var patient = ResolveConnectionPatient(other);
        var salinePoint = string.Equals(_identifier, "connect_cannula_and_ns1", StringComparison.Ordinal)
          ? this
          : other;
        patient?.NotifyPatientBCNormalSalineDisconnected(salinePoint);
      }
    }

    public override void NotifyLineDisconnected(LineConnectionPoint other) =>
      NotifyDisconnected(other as IntravenousLineConnectionPoint);

    public override void NotifyReplicatedLineDisconnected(LineConnectionPoint other) =>
      OnDisconnected?.Invoke(this, other as IntravenousLineConnectionPoint);

    public override void NotifyConnectionCompleted(LineConnectionPoint other)
    {
      if (other is not IntravenousLineConnectionPoint otherPoint)
        return;

      RaiseConnectionSignals(otherPoint);
    }

    public override bool CanPlayerCompleteConnection(PlayerController player, LineConnectionPoint other)
    {
      if (other is not IntravenousLineConnectionPoint otherPoint)
        return false;
      if (!IsNormalSalineConnectionPair(otherPoint))
        return true;

      var patient = ResolveConnectionPatient(otherPoint);
      return patient == null || patient.CanPlayerCompletePatientBCNormalSalineConnection(player);
    }

    private void RaiseSignalWithIdentifier(string signalPrefix)
    {
      if (string.IsNullOrWhiteSpace(_identifier))
        return;

      MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Raise($"{signalPrefix}{_identifier}");
    }

    private void RaiseConnectionSignals(IntravenousLineConnectionPoint other)
    {
      string otherIdentifier = other != null ? other.Identifier : null;
      bool normalSalineHandledByPatient = TryCompletePatientScopedNormalSalineConnection(other);

      if (!string.IsNullOrWhiteSpace(otherIdentifier)
          && !(normalSalineHandledByPatient && string.Equals(otherIdentifier,
            "connect_cannula_and_ns1", StringComparison.Ordinal)))
        MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Raise(otherIdentifier);

      if (!string.IsNullOrWhiteSpace(_identifier)
          && !(normalSalineHandledByPatient && string.Equals(_identifier,
            "connect_cannula_and_ns1", StringComparison.Ordinal)))
        MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Raise(_identifier);

      if (!string.IsNullOrWhiteSpace(_identifier) && !string.IsNullOrWhiteSpace(otherIdentifier))
        MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Raise($"{_identifier}__{otherIdentifier}");
    }

    private bool TryCompletePatientScopedNormalSalineConnection(IntravenousLineConnectionPoint other)
    {
      if (!IsNormalSalineConnectionPair(other))
        return false;

      PatientController patient = ResolveConnectionPatient(other);

      string patientIdentifier = patient?.Identifier;
      if (string.Equals(patientIdentifier, "patient_b", StringComparison.Ordinal)
          || string.Equals(patientIdentifier, "patient_c", StringComparison.Ordinal))
      {
        var salinePoint = string.Equals(_identifier, "connect_cannula_and_ns1", StringComparison.Ordinal)
          ? this
          : other;
        patient.TryCompletePatientBCNormalSalineConnection(salinePoint);
        return true;
      }

      return false;
    }

    private bool IsNormalSalineConnectionPair(IntravenousLineConnectionPoint other) =>
      string.Equals(_identifier, "connect_cannula_and_ns1", StringComparison.Ordinal)
      || string.Equals(other?.Identifier, "connect_cannula_and_ns1", StringComparison.Ordinal);

    private PatientController ResolveConnectionPatient(IntravenousLineConnectionPoint other)
    {
      PatientController patient = GetComponentInParent<PatientController>();
      if (patient == null && other != null)
        patient = other.GetComponentInParent<PatientController>();
      if (patient != null)
        return patient;

      var bed = GetComponentInParent<MovingPatientBedController>();
      if (bed == null && other != null)
        bed = other.GetComponentInParent<MovingPatientBedController>();
      return bed?.ReposedTarget as PatientController;
    }

    public override void ApplyLineMaterial(LineRenderer lineRenderer)
    {
      base.ApplyLineMaterial(lineRenderer);
    }

    public bool HasRequiredItem(PlayerController player) =>
      player != null && player.CountItemInInventory(IntravenousSet.Identifier) > 0;

    public bool TryConsumeRequiredItem(PlayerController player) =>
      player != null && player.RemoveItemFromInventory(IntravenousSet.Identifier, 1) > 0;

    public override bool TryConsumeConnectionRequirement(PlayerController player) =>
      TryConsumeRequiredItem(player);

    public LineConnectionService ResolveController()
    {
      if (connectionService != null)
        return connectionService;

      connectionService = FindFirstObjectByType<LineConnectionService>(FindObjectsInactive.Include);
      if (connectionService == null)
        connectionService = FindAnyObjectByType<LineConnectionService>(FindObjectsInactive.Include);

      return connectionService;
    }

#if UNITY_EDITOR
    [ContextMenu("Regenerate Point Identifier")]
    private void RegenerateIdentifier()
    {
      _identifier = string.Empty;
      EnsureIdentifier();
      EditorUtility.SetDirty(this);
    }

    private void OnDrawGizmos()
    {
      DrawSceneViewPreview();
    }

    private void OnDrawGizmosSelected()
    {
      DrawSceneViewPreview();
    }

    private void DrawSceneViewPreview()
    {
      Gizmos.color = new Color(0.35f, 0.7f, 0.5f, 0.9f);
      Gizmos.DrawSphere(transform.position, 0.04f);

      var label = string.IsNullOrWhiteSpace(_identifier) ? "(unset)" : _identifier;
      Handles.color = Color.black;
      Handles.Label(transform.position + Vector3.up * 0.08f, label);
    }
#endif
  }
}
