using System;
using System.Collections;
using System.Collections.Generic;
using MultiplayerInfrastructure.Commons;
using MultiplayerInfrastructure.UI;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MultiplayerInfrastructure.Registry
{
  public class WaypointAnchor : MonoBehaviour
  {
    [SerializeField] private string identifier;
    [Header("Highlight")]
    [SerializeField] private Sprite _highlightSprite;
    [SerializeField] private Color _highlightColor = new Color(0.36f, 0.85f, 1f, 0.92f);
    [SerializeField] private float _highlightBaseScale = 0.6f;
    [SerializeField] private float _highlightPulseScale = 1.2f;
    [SerializeField] private float _highlightPulseDuration = 0.3f;
    [SerializeField] private Vector3 _highlightOffset = new(0f, 0.25f, 0f);
    [SerializeField] private int _highlightSortingOrder = 500;

    [Header("Quest Marker")]
    [Tooltip("비워 두면 레지스트리에 등록된 quest-marker 아이콘을 사용한다. NPC 머리 위 마크와 같은 스프라이트다.")]
    [SerializeField] private Sprite _questMarkerSprite;

    private string _registeredIdentifier;
    private static readonly Dictionary<string, WaypointAnchor> _anchorsByIdentifier = new(StringComparer.Ordinal);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticAnchors() => _anchorsByIdentifier.Clear();

    private GameObject _highlightObject;
    private SpriteRenderer _highlightRenderer;
    private Material _highlightMaterial;
    private Coroutine _highlightRoutine;
    private bool _questMarkerVisible;

    /// <summary>오버헤드 라벨에서 이 마커가 차지하는 채널. NPC 마크와 겹치지 않게 별도 채널을 쓴다.</summary>
    private const string QuestMarkerChannel = "quest-waypoint";

    /// <summary>이름표(0)보다 위에 마커를 쌓기 위한 채널 순서. NPC 퀘스트 마크와 같은 기준을 쓴다.</summary>
    private const int QuestMarkerChannelOrder = 100;

    public string Identifier => identifier;
    public bool SupportsHighlight => _highlightSprite != null;
    public bool IsQuestMarkerVisible => _questMarkerVisible;

    public void ConfigureIdentifier(string value)
    {
      if (string.Equals(identifier, value, StringComparison.Ordinal))
        return;
      UnregisterFromRegistry();
      identifier = value;
      if (isActiveAndEnabled)
        RegisterToRegistry();
    }

    private void Awake()
    {
      RegisterToRegistry();
    }

    private void OnEnable()
    {
      RegisterToRegistry();
    }

    private void OnDisable()
    {
      UnregisterFromRegistry();
      DisableHighlightVisual();
      RemoveQuestMarkerLabel();
    }

    private void OnDestroy()
    {
      UnregisterFromRegistry();
      CleanupHighlightVisual();
      RemoveQuestMarkerLabel();
    }

    private void OnValidate()
    {
      if (string.IsNullOrWhiteSpace(identifier))
        identifier = EntityId.Ensure(identifier, gameObject, "waypoint");
    }

    private void RegisterToRegistry()
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return;

      _registeredIdentifier = identifier;
      if (_anchorsByIdentifier.TryGetValue(_registeredIdentifier, out var existing)
          && existing != null
          && !ReferenceEquals(existing, this))
      {
        Debug.LogError($"[WaypointAnchor] Duplicate identifier '{_registeredIdentifier}'. Registration was rejected.", this);
        _registeredIdentifier = null;
        return;
      }
      Registry.Register(RegistryType.Waypoint, _registeredIdentifier, transform.position);
      Registry.Register(RegistryType.InteractableEntity, _registeredIdentifier, transform.position);
      Registry.RegisterEntity(_registeredIdentifier, EntityType.Waypoint, gameObject, displayName: gameObject.name);
      _anchorsByIdentifier[_registeredIdentifier] = this;
    }

    private void UnregisterFromRegistry()
    {
      if (string.IsNullOrWhiteSpace(_registeredIdentifier))
        return;

      string releasedIdentifier = _registeredIdentifier;
      bool releasedOwner = false;
      if (_anchorsByIdentifier.TryGetValue(_registeredIdentifier, out var current)
          && ReferenceEquals(current, this))
      {
        Registry.Unregister(RegistryType.Waypoint, _registeredIdentifier);
        Registry.Unregister(RegistryType.InteractableEntity, _registeredIdentifier);
        Registry.UnregisterEntity(_registeredIdentifier);
        _anchorsByIdentifier.Remove(_registeredIdentifier);
        releasedOwner = true;
      }
      _registeredIdentifier = null;

      if (releasedOwner)
        PromoteUniqueActiveCandidate(releasedIdentifier, this);
    }

    private static void PromoteUniqueActiveCandidate(string releasedIdentifier, WaypointAnchor released)
    {
      WaypointAnchor candidate = null;
      var anchors = FindObjectsByType<WaypointAnchor>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
      foreach (var anchor in anchors)
      {
        if (anchor == null || ReferenceEquals(anchor, released) || !anchor.isActiveAndEnabled
            || !string.Equals(anchor.identifier, releasedIdentifier, StringComparison.Ordinal))
          continue;

        if (candidate != null)
        {
          Debug.LogError($"[WaypointAnchor] Multiple active successors for identifier '{releasedIdentifier}'. Promotion was rejected.");
          return;
        }
        candidate = anchor;
      }

      candidate?.RegisterToRegistry();
    }

    private void OnDrawGizmos()
    {
      DrawAnchorGizmo();
    }

    private void OnDrawGizmosSelected()
    {
      DrawAnchorGizmo();
    }

    private void DrawAnchorGizmo()
    {
      Gizmos.color = Color.yellow;
      Gizmos.DrawSphere(transform.position, 0.2f);

#if UNITY_EDITOR
      var label = string.IsNullOrWhiteSpace(identifier) ? "(unset)" : identifier;
      Handles.color = Color.black;
      Handles.Label(transform.position + Vector3.up * 0.25f, label);
#endif
    }

    public void Highlight()
    {
      if (!gameObject.activeInHierarchy || _highlightSprite == null)
        return;

      EnsureHighlightVisual();
      if (_highlightObject == null || _highlightRenderer == null)
        return;

      _highlightObject.SetActive(true);
      var baseScale = Mathf.Max(0.01f, _highlightBaseScale);
      _highlightObject.transform.localScale = Vector3.one * baseScale;
      _highlightRenderer.color = _highlightColor;

      if (_highlightRoutine != null)
      {
        StopCoroutine(_highlightRoutine);
      }

      _highlightRoutine = StartCoroutine(PulseHighlight(baseScale));
    }

    public static bool TryGet(string identifier, out WaypointAnchor anchor)
    {
      anchor = null;
      if (string.IsNullOrWhiteSpace(identifier))
        return false;

      return _anchorsByIdentifier.TryGetValue(identifier, out anchor);
    }

    /// <summary>
    /// 이 waypoint가 현재 퀘스트의 이동 목표일 때 quest-marker 아이콘을 표시한다.
    /// NPC 머리 위 마크와 같은 오버헤드 라벨 경로를 쓰므로 크기가 동일하고 지형에 가려지지 않는다.
    /// </summary>
    public void SetQuestMarkerVisible(bool visible)
    {
      if (_questMarkerVisible == visible)
        return;

      if (!visible)
      {
        _questMarkerVisible = false;
        RemoveQuestMarkerLabel();
        return;
      }

      var controller = EntityOverheadLabelUIController.ActiveInstance;
      var sprite = ResolveQuestMarkerSprite();
      if (controller == null || sprite == null)
      {
        // 라벨 컨트롤러나 아이콘이 아직 준비되지 않았을 수 있다. 호출자가 매 프레임 갱신하므로
        // 표시 상태를 올리지 않고 다음 호출에서 다시 시도하게 한다.
        return;
      }

      controller.SetLabel(
        transform,
        QuestMarkerChannel,
        QuestMarkerChannelOrder,
        new EntityOverheadLabelUIController.LabelContent(sprite));
      _questMarkerVisible = true;
    }

    private void RemoveQuestMarkerLabel()
    {
      EntityOverheadLabelUIController.ActiveInstance?.RemoveLabel(transform, QuestMarkerChannel);
    }

    /// <summary>
    /// 인스펙터에 지정한 스프라이트를 우선 쓰고, 비어 있으면 NPC 머리 위 마크와 같은
    /// quest-marker 아이콘을 레지스트리에서 가져온다.
    /// </summary>
    private Sprite ResolveQuestMarkerSprite()
    {
      if (_questMarkerSprite != null)
        return _questMarkerSprite;

      return Registry.TryGet<Sprite>(
        RegistryType.IconSprite, IconSpriteIdentifiers.QuestNpcMark, out var sprite)
        ? sprite
        : null;
    }

    private void EnsureHighlightVisual()
    {
      if (_highlightObject != null)
        return;

      if (_highlightSprite == null)
        return;

      _highlightObject = new GameObject("highlight");
      _highlightObject.hideFlags = HideFlags.HideInHierarchy;
      _highlightObject.transform.SetParent(transform, false);
      _highlightObject.transform.localPosition = _highlightOffset;
      _highlightObject.transform.localScale = Vector3.one * Mathf.Max(0.01f, _highlightBaseScale);

      _highlightRenderer = _highlightObject.AddComponent<SpriteRenderer>();
      _highlightRenderer.sprite = _highlightSprite;
      _highlightRenderer.color = _highlightColor;
      _highlightRenderer.shadowCastingMode = ShadowCastingMode.Off;
      _highlightRenderer.receiveShadows = false;
      _highlightRenderer.sortingOrder = _highlightSortingOrder;

      _highlightMaterial = new Material(Shader.Find("Sprites/Default"));
      _highlightMaterial.SetInt("_ZTest", (int)CompareFunction.Always);
      _highlightMaterial.SetInt("_ZWrite", 0);
      _highlightRenderer.material = _highlightMaterial;

      _highlightObject.SetActive(false);
    }

    private IEnumerator PulseHighlight(float baseScale)
    {
      if (_highlightObject == null)
        yield break;

      if (_highlightPulseDuration <= 0f)
      {
        _highlightObject.transform.localScale = Vector3.one * baseScale;
        _highlightRoutine = null;
        yield break;
      }

      var total = _highlightPulseDuration;
      var targetScale = baseScale * Mathf.Max(1f, _highlightPulseScale);
      var elapsed = 0f;

      while (elapsed < total)
      {
        elapsed += Time.deltaTime;
        var normalized = Mathf.Clamp01(elapsed / total);
        var scale = normalized <= 0.5f
            ? Mathf.Lerp(baseScale, targetScale, normalized / 0.5f)
            : Mathf.Lerp(targetScale, baseScale, (normalized - 0.5f) / 0.5f);
        _highlightObject.transform.localScale = Vector3.one * scale;
        yield return null;
      }

      _highlightObject.transform.localScale = Vector3.one * baseScale;
      _highlightRoutine = null;
    }

    private void DisableHighlightVisual()
    {
      if (_highlightRoutine != null)
      {
        StopCoroutine(_highlightRoutine);
        _highlightRoutine = null;
      }

      if (_highlightObject != null)
      {
        _highlightObject.SetActive(false);
      }
    }

    private void CleanupHighlightVisual()
    {
      if (_highlightRoutine != null)
      {
        StopCoroutine(_highlightRoutine);
        _highlightRoutine = null;
      }

      if (_highlightObject != null)
      {
        Destroy(_highlightObject);
        _highlightObject = null;
      }

      if (_highlightMaterial != null)
      {
        Destroy(_highlightMaterial);
        _highlightMaterial = null;
      }

      _highlightRenderer = null;
    }
  }
}
