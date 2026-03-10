using System;
using System.Collections;
using System.Collections.Generic;
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

    private string _registeredIdentifier;
    private static readonly Dictionary<string, WaypointAnchor> _anchorsByIdentifier = new(StringComparer.Ordinal);

    private GameObject _highlightObject;
    private SpriteRenderer _highlightRenderer;
    private Material _highlightMaterial;
    private Coroutine _highlightRoutine;

    public string Identifier => identifier;

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
    }

    private void OnDestroy()
    {
      UnregisterFromRegistry();
      CleanupHighlightVisual();
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
      Registry.Register(RegistryType.Waypoint, _registeredIdentifier, transform.position);
      Registry.Register(RegistryType.InteractableEntity, _registeredIdentifier, transform.position);
      Registry.RegisterEntity(_registeredIdentifier, EntityType.Waypoint, gameObject, displayName: gameObject.name);
      _anchorsByIdentifier[_registeredIdentifier] = this;
    }

    private void UnregisterFromRegistry()
    {
      if (string.IsNullOrWhiteSpace(_registeredIdentifier))
        return;

      Registry.Unregister(RegistryType.Waypoint, _registeredIdentifier);
      Registry.Unregister(RegistryType.InteractableEntity, _registeredIdentifier);
      Registry.UnregisterEntity(_registeredIdentifier);
      _anchorsByIdentifier.Remove(_registeredIdentifier);
      _registeredIdentifier = null;
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
