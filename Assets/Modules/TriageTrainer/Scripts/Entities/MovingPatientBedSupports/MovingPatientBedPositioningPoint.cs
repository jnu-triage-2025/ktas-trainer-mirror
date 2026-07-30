using UnityEngine;
using UnityEngine.Rendering;
using TriageTrainer.Scenario;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TriageTrainer.Entity
{
  /// <summary>
  /// 이동식 환자 침대가 도착했을 때 정렬될 월드상의 고정 위치와 방향을 정의한다.
  /// </summary>
  public sealed class MovingPatientBedPositioningPoint : MonoBehaviour
  {
    [Header("Identity")]
    [SerializeField] private string _identifier;

    [Header("Snap")]
    [SerializeField, Min(0.01f)] private float _snapDistance = 1f;

    [Header("Occupied Area")]
    [SerializeField] private Vector2 _occupiedSize = new(2.2f, 1f);
    [SerializeField, Min(0f)] private float _displayHeight = 0.03f;

    public float SnapDistance => _snapDistance;
    /// <summary>시나리오 신호와 세션 로그에서 이 위치를 식별하는 안정적인 키.</summary>
    public string Identifier => _identifier == null ? string.Empty : _identifier.Trim();
    public Vector3 Position => transform.position;
    public Quaternion Rotation => transform.rotation;

    public void SetIdentifierForEditor(string identifier)
    {
      _identifier = identifier == null ? string.Empty : identifier.Trim();
    }

    public void SetIdentifier(string identifier)
    {
      _identifier = identifier == null ? string.Empty : identifier.Trim();
    }

    public void ConfigureOccupiedArea(Vector2 occupiedSizeValue, float displayHeightValue)
    {
      _occupiedSize = new Vector2(Mathf.Max(0.01f, occupiedSizeValue.x), Mathf.Max(0.01f, occupiedSizeValue.y));
      _displayHeight = Mathf.Max(0f, displayHeightValue);
    }

    public bool IsWithinSnapDistance(Vector3 worldPosition)
    {
      Vector3 offset = worldPosition - transform.position;
      offset.y = 0f;
      return offset.sqrMagnitude <= _snapDistance * _snapDistance;
    }

    private void OnValidate()
    {
      _identifier = _identifier == null ? string.Empty : _identifier.Trim();
      _snapDistance = Mathf.Max(0.01f, _snapDistance);
      _occupiedSize.x = Mathf.Max(0.01f, _occupiedSize.x);
      _occupiedSize.y = Mathf.Max(0.01f, _occupiedSize.y);
      _displayHeight = Mathf.Max(0f, _displayHeight);
    }

    private GameObject _runtimeHint;
    private Renderer _runtimeFill;
    private LineRenderer _runtimeOutline;
    private Material _runtimeFillMaterial;
    private Material _runtimeOutlineMaterial;

    private void Awake()
    {
      CreateRuntimeHint();
    }

    private void OnEnable()
    {
      TriageWorldInteractionSignals.RaisePatientBedPositioningPointEnabled(Identifier);
    }

    private void OnDisable()
    {
      TriageWorldInteractionSignals.RaisePatientBedPositioningPointDisabled(Identifier);
    }

    private void Update()
    {
      if (_runtimeHint == null)
        CreateRuntimeHint();

      bool visible = false;
      MovingPatientBedController[] beds = FindObjectsByType<MovingPatientBedController>(
        FindObjectsInactive.Exclude, FindObjectsSortMode.None);
      for (int i = 0; i < beds.Length; i++)
      {
        if (beds[i] != null && beds[i].isActiveAndEnabled && beds[i].IsLocallyControlled)
        {
          visible = true;
          break;
        }
      }

      if (_runtimeHint != null)
        _runtimeHint.SetActive(visible);
    }

    private void CreateRuntimeHint()
    {
      if (_runtimeHint != null)
        return;

      _runtimeHint = new GameObject("PositioningPointHint");
      _runtimeHint.transform.SetParent(transform, false);
      _runtimeHint.transform.localPosition = Vector3.up * _displayHeight;
      _runtimeHint.transform.localRotation = Quaternion.identity;

      GameObject fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
      fill.name = "Fill";
      fill.transform.SetParent(_runtimeHint.transform, false);
      fill.transform.localScale = new Vector3(_occupiedSize.x, 0.01f, _occupiedSize.y);
      Destroy(fill.GetComponent<Collider>());
      _runtimeFill = fill.GetComponent<Renderer>();
      _runtimeFill.shadowCastingMode = ShadowCastingMode.Off;
      _runtimeFill.receiveShadows = false;

      GameObject outline = new GameObject("Outline");
      outline.transform.SetParent(_runtimeHint.transform, false);
      _runtimeOutline = outline.AddComponent<LineRenderer>();
      _runtimeOutline.useWorldSpace = false;
      _runtimeOutline.loop = true;
      _runtimeOutline.positionCount = 4;
      _runtimeOutline.widthMultiplier = 0.025f;
      _runtimeOutline.shadowCastingMode = ShadowCastingMode.Off;
      _runtimeOutline.receiveShadows = false;
      _runtimeOutline.SetPositions(new[]
      {
        new Vector3(-_occupiedSize.x * 0.5f, 0.008f, -_occupiedSize.y * 0.5f),
        new Vector3(-_occupiedSize.x * 0.5f, 0.008f,  _occupiedSize.y * 0.5f),
        new Vector3( _occupiedSize.x * 0.5f, 0.008f,  _occupiedSize.y * 0.5f),
        new Vector3( _occupiedSize.x * 0.5f, 0.008f, -_occupiedSize.y * 0.5f)
      });

      Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
      if (shader == null)
        return;
      _runtimeFillMaterial = CreateHintMaterial(shader, new Color(0.1f, 0.8f, 1f, 0.22f), "Fill");
      _runtimeOutlineMaterial = CreateHintMaterial(shader, new Color(0.1f, 0.8f, 1f, 0.95f), "Outline");
      _runtimeFill.sharedMaterial = _runtimeFillMaterial;
      _runtimeOutline.sharedMaterial = _runtimeOutlineMaterial;
      _runtimeOutline.startColor = new Color(0.1f, 0.8f, 1f, 0.95f);
      _runtimeOutline.endColor = _runtimeOutline.startColor;
      _runtimeHint.SetActive(false);
    }

    private static Material CreateHintMaterial(Shader shader, Color color, string suffix)
    {
      var material = new Material(shader) { name = "PositioningPointHintMaterial_" + suffix };
      if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
      if (material.HasProperty("_Color")) material.SetColor("_Color", color);
      material.SetOverrideTag("RenderType", "Transparent");
      material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
      material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
      material.SetInt("_ZWrite", 0);
      material.renderQueue = (int)RenderQueue.Transparent;
      return material;
    }

    private void OnDestroy()
    {
      if (_runtimeFillMaterial != null)
        Destroy(_runtimeFillMaterial);
      if (_runtimeOutlineMaterial != null)
        Destroy(_runtimeOutlineMaterial);
      if (_runtimeHint != null)
        Destroy(_runtimeHint);
    }

    private void OnDrawGizmos()
    {
      DrawGizmo();
    }

    private void OnDrawGizmosSelected()
    {
      DrawGizmo();
    }

    private void DrawGizmo()
    {
      Vector3 center = transform.position + transform.up * _displayHeight;
      Vector3 size = new(_occupiedSize.x, 0.01f, _occupiedSize.y);

      Matrix4x4 previousMatrix = Gizmos.matrix;
      Color previousColor = Gizmos.color;
      Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
      Gizmos.color = new Color(0.1f, 0.8f, 1f, 0.2f);
      Gizmos.DrawCube(Vector3.zero, size);
      Gizmos.color = new Color(0.1f, 0.8f, 1f, 0.95f);
      Gizmos.DrawWireCube(Vector3.zero, size);
      Gizmos.matrix = previousMatrix;

      // 표시용 화살표만 반시계 90도 회전한다. 스냅 방향(transform.rotation)은 유지한다.
      Vector3 arrowForward = Quaternion.AngleAxis(-90f, transform.up) * transform.forward;
      Vector3 arrowRight = Quaternion.AngleAxis(-90f, transform.up) * transform.right;
      Vector3 arrowStart = center - arrowForward * (_occupiedSize.y * 0.25f);
      Vector3 arrowEnd = center + arrowForward * (_occupiedSize.y * 0.45f);
      Gizmos.DrawLine(arrowStart, arrowEnd);
      Gizmos.DrawLine(arrowEnd, arrowEnd - arrowForward * 0.22f + arrowRight * 0.12f);
      Gizmos.DrawLine(arrowEnd, arrowEnd - arrowForward * 0.22f - arrowRight * 0.12f);
      Gizmos.DrawWireSphere(center, 0.06f);
      Gizmos.color = previousColor;

#if UNITY_EDITOR
      Handles.color = new Color(0.05f, 0.45f, 0.65f, 1f);
      Handles.Label(center + Vector3.up * 0.15f, "Patient Bed Positioning Point");
#endif
    }
  }
}
