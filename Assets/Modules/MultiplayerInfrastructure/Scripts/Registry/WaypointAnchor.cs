using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using MultiplayerInfrastructure.Scenario.Requirements;
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
    [SerializeField] private Vector3 _questMarkerOffset = new(0f, 0.8f, 0f);
    [SerializeField] private int _questMarkerFontSize = 64;
    [SerializeField] private float _questMarkerCharacterSize = 0.045f;
    [SerializeField] private float _questMarkerOutlineOffset = 0.012f;
    [SerializeField] private float _questMarkerInitialScale = 1.25f;
    [SerializeField] private float _questMarkerSettleDuration = 0.35f;
    [SerializeField] private int _questMarkerSortingOrder = 510;
    [Tooltip("이 거리에서 scale 1로 보이는 기준 거리. 카메라와의 거리에 따라 화면 크기를 일정하게 보정한다.")]
    [SerializeField] private float _questMarkerReferenceDistance = 10f;
    [SerializeField] private float _questMarkerReferenceFieldOfView = 60f;
    [SerializeField] private float _questMarkerReferenceOrthographicSize = 5f;

    private string _registeredIdentifier;
    private ScenarioRequirementRuntimeRegistrationHandle _waypointEvidence;
    private ScenarioRequirementRuntimeRegistrationHandle _entityEvidence;
    private static readonly Dictionary<string, WaypointAnchor> _anchorsByIdentifier = new(StringComparer.Ordinal);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticAnchors() => _anchorsByIdentifier.Clear();

    private GameObject _highlightObject;
    private SpriteRenderer _highlightRenderer;
    private Material _highlightMaterial;
    private Coroutine _highlightRoutine;
    private GameObject _questMarkerObject;
    private Coroutine _questMarkerSettleRoutine;
    private UnityEngine.Camera _questMarkerCamera;
    private bool _questMarkerVisible;
    private float _questMarkerAnimationScale = 1f;

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
      DisableQuestMarkerVisual();
    }

    private void OnDestroy()
    {
      UnregisterFromRegistry();
      CleanupHighlightVisual();
      CleanupQuestMarkerVisual();
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
      _waypointEvidence = ScenarioRequirementRuntimeRegistrationRegistry.Register(RegistryType.Waypoint, ScenarioRequirementKind.SpatialAnchor, _registeredIdentifier, this, SupportsHighlight ? new[] { ScenarioRequirementCapability.ProvidesPosition, ScenarioRequirementCapability.HighlightableWaypoint } : new[] { ScenarioRequirementCapability.ProvidesPosition });
      _entityEvidence = ScenarioRequirementRuntimeRegistrationRegistry.Register(RegistryType.Entity, ScenarioRequirementKind.Entity, _registeredIdentifier, this, new[] { ScenarioRequirementCapability.RegisteredEntity });
    }

    private void UnregisterFromRegistry()
    {
      if (string.IsNullOrWhiteSpace(_registeredIdentifier))
        return;

      Registry.Unregister(RegistryType.Waypoint, _registeredIdentifier);
      Registry.Unregister(RegistryType.InteractableEntity, _registeredIdentifier);
      Registry.UnregisterEntity(_registeredIdentifier);
      _waypointEvidence.Dispose();
      _entityEvidence.Dispose();
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

    /// <summary>
    /// 이 waypoint가 현재 퀘스트의 이동 목표일 때 흰색 ❖ 마커를 표시한다.
    /// 처음 표시될 때만 살짝 큰 크기에서 평소 크기로 자연스럽게 안착한다.
    /// </summary>
    public void SetQuestMarkerVisible(bool visible)
    {
      if (_questMarkerVisible == visible
          && (!visible || (_questMarkerObject != null && _questMarkerObject.activeSelf)))
        return;

      _questMarkerVisible = visible;
      if (!visible)
      {
        DisableQuestMarkerVisual();
        return;
      }

      if (!gameObject.activeInHierarchy)
        return;

      EnsureQuestMarkerVisual();
      if (_questMarkerObject == null)
        return;

      _questMarkerObject.SetActive(true);
      if (_questMarkerSettleRoutine != null)
        StopCoroutine(_questMarkerSettleRoutine);
      _questMarkerSettleRoutine = StartCoroutine(SettleQuestMarker());
    }

    private void EnsureQuestMarkerVisual()
    {
      if (_questMarkerObject != null)
        return;

      _questMarkerObject = new GameObject("Quest Waypoint Marker");
      _questMarkerObject.hideFlags = HideFlags.HideInHierarchy;
      _questMarkerObject.transform.SetParent(transform, false);
      _questMarkerObject.transform.localPosition = _questMarkerOffset;

      // Legacy TextMesh에는 outline 속성이 없으므로 검은색 복제 텍스트를 둘러 배치해
      // 카메라 방향과 무관하게 읽히는 얇은 테두리를 만든다.
      var outlineDirections = new[]
      {
        new Vector2(-1f, -1f), new Vector2(0f, -1f), new Vector2(1f, -1f),
        new Vector2(-1f, 0f),                         new Vector2(1f, 0f),
        new Vector2(-1f, 1f),  new Vector2(0f, 1f),  new Vector2(1f, 1f)
      };
      for (int i = 0; i < outlineDirections.Length; i++)
      {
        var outline = CreateQuestMarkerText($"Outline {i}", Color.black, _questMarkerSortingOrder);
        var direction = outlineDirections[i];
        outline.transform.localPosition =
          new Vector3(direction.x, direction.y, 0.01f) * Mathf.Max(0f, _questMarkerOutlineOffset);
      }

      CreateQuestMarkerText("Symbol", Color.white, _questMarkerSortingOrder + 1);
      _questMarkerObject.SetActive(false);
    }

    private TextMesh CreateQuestMarkerText(string objectName, Color color, int sortingOrder)
    {
      var textObject = new GameObject(objectName);
      textObject.transform.SetParent(_questMarkerObject.transform, false);
      var text = textObject.AddComponent<TextMesh>();
      text.text = "❖";
      text.anchor = TextAnchor.MiddleCenter;
      text.alignment = TextAlignment.Center;
      text.fontSize = Mathf.Max(1, _questMarkerFontSize);
      text.characterSize = Mathf.Max(0.001f, _questMarkerCharacterSize);
      text.color = color;
      var renderer = text.GetComponent<MeshRenderer>();
      if (renderer != null)
        renderer.sortingOrder = sortingOrder;
      return text;
    }

    private IEnumerator SettleQuestMarker()
    {
      if (_questMarkerObject == null)
        yield break;

      float duration = Mathf.Max(0f, _questMarkerSettleDuration);
      float initialScale = Mathf.Max(1f, _questMarkerInitialScale);
      if (duration <= 0f)
      {
        _questMarkerAnimationScale = 1f;
        _questMarkerSettleRoutine = null;
        yield break;
      }

      float elapsed = 0f;
      while (elapsed < duration)
      {
        elapsed += Time.unscaledDeltaTime;
        float normalized = Mathf.Clamp01(elapsed / duration);
        float eased = normalized * normalized * (3f - 2f * normalized);
        _questMarkerAnimationScale = Mathf.Lerp(initialScale, 1f, eased);
        yield return null;
      }

      _questMarkerAnimationScale = 1f;
      _questMarkerSettleRoutine = null;
    }

    private void LateUpdate()
    {
      if (_questMarkerObject == null || !_questMarkerObject.activeSelf)
        return;

      if (_questMarkerCamera == null)
        _questMarkerCamera = UnityEngine.Camera.main;
      if (_questMarkerCamera == null)
        return;

      var cameraTransform = _questMarkerCamera.transform;
      _questMarkerObject.transform.rotation =
        Quaternion.LookRotation(cameraTransform.forward, cameraTransform.up);
      ApplyConstantScreenScale(_questMarkerCamera);
    }

    private void ApplyConstantScreenScale(UnityEngine.Camera camera)
    {
      float scale;
      if (camera.orthographic)
      {
        scale = camera.orthographicSize
            / Mathf.Max(0.01f, _questMarkerReferenceOrthographicSize);
      }
      else
      {
        float distance = Vector3.Distance(camera.transform.position, _questMarkerObject.transform.position);
        float distanceScale = distance / Mathf.Max(0.01f, _questMarkerReferenceDistance);
        float currentFov = Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float referenceFov = Mathf.Tan(
          Mathf.Clamp(_questMarkerReferenceFieldOfView, 1f, 179f) * 0.5f * Mathf.Deg2Rad);
        scale = distanceScale * currentFov / Mathf.Max(0.001f, referenceFov);
      }

      // 부모 waypoint에 스케일이 적용되어 있어도 최종 월드 크기는 동일하게 유지한다.
      Vector3 parentScale = transform.lossyScale;
      float animatedScale = Mathf.Max(0.001f, scale * _questMarkerAnimationScale);
      _questMarkerObject.transform.localScale = new Vector3(
        animatedScale / Mathf.Max(0.001f, Mathf.Abs(parentScale.x)),
        animatedScale / Mathf.Max(0.001f, Mathf.Abs(parentScale.y)),
        animatedScale / Mathf.Max(0.001f, Mathf.Abs(parentScale.z)));
    }

    private void DisableQuestMarkerVisual()
    {
      if (_questMarkerSettleRoutine != null)
      {
        StopCoroutine(_questMarkerSettleRoutine);
        _questMarkerSettleRoutine = null;
      }

      if (_questMarkerObject != null)
      {
        _questMarkerAnimationScale = 1f;
        _questMarkerObject.SetActive(false);
      }
    }

    private void CleanupQuestMarkerVisual()
    {
      DisableQuestMarkerVisual();
      if (_questMarkerObject != null)
      {
        Destroy(_questMarkerObject);
        _questMarkerObject = null;
      }
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
