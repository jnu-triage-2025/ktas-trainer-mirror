#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using UnityEngine;

namespace TriageTrainer.Entity.IntravenousLine
{
  [RequireComponent(typeof(SphereCollider))]
  public class IntravenousLineConnectionPoint : MonoBehaviour, IInteractable
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

    private sealed class StartConnectionInteract : IInteract, IInteractorConditional
    {
      private readonly IntravenousLineConnectionPoint _owner;
      public StartConnectionInteract(IntravenousLineConnectionPoint owner) { _owner = owner; }

      public string DisplayText => "수액 줄 연결 시작";
      public Sprite DisplayIcon => _owner._displayIcon;
      public bool AllowDisplayIconFallback => true;
      public Color DisplayColor => Color.white;

      public bool CanInteract(Transform interactor)
      {
        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        if (player == null)
          return false;

        if (!_owner.IsInteractEnabled(InteractIdStartConnectionMode))
          return false;

        if (_owner.HasAnyConnection)
          return false;

        if (player.IsIntravenousLineConnectionMode)
          return false;

        var controller = _owner.ResolveController();
        return controller != null && controller.HasAnyRequiredItem(player);
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

        if (_owner.HasAnyConnection)
          return false;

        if (!player.IsIntravenousLineConnectionMode)
          return false;

        var controller = _owner.ResolveController();
        if (controller == null)
          return false;

        if (!controller.HasAnyRequiredItem(player))
          return false;

        return controller.HasPendingStartPoint(player, out var startPoint)
          && startPoint != null
          && !ReferenceEquals(startPoint, _owner);
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
      public Sprite DisplayIcon => _owner._displayIcon;
      public bool AllowDisplayIconFallback => true;
      public Color DisplayColor => Color.white;

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

        controller.DisconnectFromPoint(_owner);

        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        player?.RefreshInteractableHintsNow();
      }
    }

    public const string InteractIdStartConnectionMode = "intravenous_line_connect_mode_start";
    public const string InteractIdConnectHere = "intravenous_line_connect_here";
    public const string InteractIdDisconnect = "intravenous_line_disconnect";

    [Header("Service")]
    [SerializeField] private IntravenousLineConnectionService connectionService;

    [Header("Identifier")]
    [SerializeField] private string _identifier;
    [SerializeField] private bool _autoGenerateIdentifier = true;

    [Header("Interact")]
    [SerializeField] private Sprite _displayIcon;
    [SerializeField] private List<InteractConfig> _interactConfigs = new();

    [Header("Runtime")]
    [SerializeField] private List<GameObject> _connectedLineObjects = new();

    private List<IInteract> _interacts = new();
    private Dictionary<string, InteractConfig> _interactConfigMap = new(StringComparer.Ordinal);

    public string Identifier => _identifier;
    public IInteract[] Interacts => _interacts.ToArray();
    public bool HasAnyConnection
    {
      get
      {
        for (int i = _connectedLineObjects.Count - 1; i >= 0; i--)
        {
          if (_connectedLineObjects[i] != null)
            return true;

          _connectedLineObjects.RemoveAt(i);
        }

        return false;
      }
    }

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
      _connectedLineObjects ??= new List<GameObject>();
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

    public void RegisterConnectedLineObject(GameObject lineObject)
    {
      if (lineObject == null)
        return;

      if (_connectedLineObjects.Contains(lineObject))
        return;

      _connectedLineObjects.Add(lineObject);
    }

    public void UnregisterConnectedLineObject(GameObject lineObject)
    {
      if (lineObject == null)
        return;

      _connectedLineObjects.Remove(lineObject);
    }

    public bool TryGetAnyConnectedLineObject(out GameObject lineObject)
    {
      EnsureRuntimeCollections();

      for (int i = 0; i < _connectedLineObjects.Count; i++)
      {
        var each = _connectedLineObjects[i];
        if (each == null)
          continue;

        lineObject = each;
        return true;
      }

      lineObject = null;
      return false;
    }

    public IntravenousLineConnectionService ResolveController()
    {
      if (connectionService != null)
        return connectionService;

      connectionService = FindFirstObjectByType<IntravenousLineConnectionService>(FindObjectsInactive.Include);
      if (connectionService == null)
        connectionService = FindAnyObjectByType<IntravenousLineConnectionService>(FindObjectsInactive.Include);

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
