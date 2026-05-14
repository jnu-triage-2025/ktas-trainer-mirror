using UnityEngine;

namespace TriageTrainer.Entity.PatientMonitor.Models
{
  public partial class PatientMonitorController
  {
    [Header("Tracking Line")]
    [SerializeField] private bool _showTrackingLine = true;
    [SerializeField] private bool _showTrackingLineForDefaultPatient = false;
    [SerializeField] private Transform _trackingLineStart;
    [SerializeField] private Vector3 _trackingLineStartOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private Vector3 _trackingLineEndOffset = new Vector3(0f, 1f, 0f);
    [SerializeField, Min(0.001f)] private float _trackingLineWidth = 0.02f;
    [SerializeField] private Color _trackingLineColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField, Min(0.01f)] private float _trackingDashLength = 0.2f;
    [SerializeField, Min(0.01f)] private float _trackingDashGap = 0.2f;
    [SerializeField] private bool _trackingDashFlowFromTarget = true;
    [SerializeField, Min(0f)] private float _trackingDashScrollSpeed = 0.9f;
    [SerializeField, Range(0f, 0.6f)] private float _trackingSparkleIntensity = 0.2f;
    [SerializeField, Min(0f)] private float _trackingSparkleSpeed = 2.2f;

    private const string TrackingLineObjectName = "PatientMonitorTrackingLine";

    private LineRenderer _trackingLineRenderer;
    private Material _trackingLineMaterial;
    private Texture2D _trackingLineTexture;
    private float _trackingDashOffset;
    private float _trackingTextureScaleX = 1f;
    private MaterialPropertyBlock _trackingLinePropertyBlock;
    private static readonly int MainTexStId = Shader.PropertyToID("_MainTex_ST");

    private void EnsureTrackingLineRenderer()
    {
      if (!_showTrackingLine)
        return;

      if (_trackingLineRenderer == null && !TryResolveTrackingLineRenderer())
        _trackingLineRenderer = CreateTrackingLineRenderer();

      if (_trackingLineRenderer == null)
        return;

      EnsureTrackingLineMaterial();
      ApplyTrackingLineSettings();
    }

    private bool TryResolveTrackingLineRenderer()
    {
      var existing = transform.Find(TrackingLineObjectName);
      if (existing == null)
        return false;

      _trackingLineRenderer = existing.GetComponent<LineRenderer>();
      if (_trackingLineRenderer == null)
        _trackingLineRenderer = existing.gameObject.AddComponent<LineRenderer>();

      return _trackingLineRenderer != null;
    }

    private LineRenderer CreateTrackingLineRenderer()
    {
      var lineObject = new GameObject(TrackingLineObjectName);
      lineObject.transform.SetParent(transform, false);
      return lineObject.AddComponent<LineRenderer>();
    }

    private void EnsureTrackingLineMaterial()
    {
      if (_trackingLineMaterial != null)
        return;

      var shader = Shader.Find("Sprites/Default");
      if (shader == null)
        return;

      _trackingLineMaterial = new Material(shader)
      {
        name = "PatientMonitorTrackingLine",
        hideFlags = HideFlags.DontSave
      };

      _trackingLineTexture = CreateDashTexture();
      _trackingLineMaterial.mainTexture = _trackingLineTexture;
      _trackingLineRenderer.material = _trackingLineMaterial;
    }

    private Texture2D CreateDashTexture()
    {
      // Repeating alpha pattern for dashed line rendering.
      var texture = new Texture2D(8, 1, TextureFormat.RGBA32, false)
      {
        name = "PatientMonitorTrackingLineDash",
        wrapMode = TextureWrapMode.Repeat,
        filterMode = FilterMode.Bilinear,
        hideFlags = HideFlags.DontSave
      };

      texture.SetPixels(new[]
      {
        new Color(1f, 1f, 1f, 0f),
        new Color(1f, 1f, 1f, 0.35f),
        new Color(1f, 1f, 1f, 0.7f),
        new Color(1f, 1f, 1f, 1f),
        new Color(1f, 1f, 1f, 0.7f),
        new Color(1f, 1f, 1f, 0.35f),
        new Color(1f, 1f, 1f, 0f),
        new Color(1f, 1f, 1f, 0f)
      });

      texture.Apply();
      return texture;
    }

    private void ApplyTrackingLineSettings()
    {
      if (_trackingLineRenderer == null)
        return;

      _trackingLineRenderer.useWorldSpace = true;
      _trackingLineRenderer.alignment = LineAlignment.View;
      _trackingLineRenderer.textureMode = LineTextureMode.Tile;
      _trackingLineRenderer.numCornerVertices = 0;
      _trackingLineRenderer.numCapVertices = 0;
      _trackingLineRenderer.startWidth = _trackingLineWidth;
      _trackingLineRenderer.endWidth = _trackingLineWidth;
      _trackingLineRenderer.startColor = _trackingLineColor;
      _trackingLineRenderer.endColor = _trackingLineColor;
      _trackingLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
      _trackingLineRenderer.receiveShadows = false;
      _trackingLineRenderer.motionVectorGenerationMode = UnityEngine.MotionVectorGenerationMode.ForceNoMotion;

      if (_trackingLineMaterial != null)
        _trackingLineMaterial.color = _trackingLineColor;

      ApplyTextureTransform();
    }

    private void UpdateTrackingLine()
    {
      if (!_showTrackingLine)
      {
        SetTrackingLineEnabled(false);
        return;
      }

      var target = ResolveTrackingTarget();
      if (target == null)
      {
        SetTrackingLineEnabled(false);
        return;
      }

      EnsureTrackingLineRenderer();
      if (_trackingLineRenderer == null)
        return;

      Vector3 start = ResolveTrackingStartPosition();
      Vector3 end = ResolveTrackingEndPosition(target);

      _trackingLineRenderer.positionCount = 2;
      _trackingLineRenderer.SetPosition(0, start);
      _trackingLineRenderer.SetPosition(1, end);
      SetTrackingLineEnabled(true);

      UpdateDashTiling(start, end);
      UpdateDashScroll();
      UpdateSparkle();
    }

    private PatientController ResolveTrackingTarget()
    {
      if (_monitoringPatient != null)
        return _monitoringPatient;

      if (_showTrackingLineForDefaultPatient)
        return patientState;

      return null;
    }

    private Vector3 ResolveTrackingStartPosition()
    {
      var startTransform = _trackingLineStart != null ? _trackingLineStart : transform;
      return startTransform.TransformPoint(_trackingLineStartOffset);
    }

    private Vector3 ResolveTrackingEndPosition(PatientController target)
    {
      var targetTransform = target != null ? target.transform : null;
      if (targetTransform == null)
        return ResolveTrackingStartPosition();

      return targetTransform.TransformPoint(_trackingLineEndOffset);
    }

    private void UpdateDashTiling(Vector3 start, Vector3 end)
    {
      if (_trackingLineMaterial == null)
        return;

      float length = Vector3.Distance(start, end);
      float patternLength = Mathf.Max(0.01f, _trackingDashLength + _trackingDashGap);
      _trackingTextureScaleX = length / patternLength;
      ApplyTextureTransform();
    }

    private void UpdateDashScroll()
    {
      if (_trackingLineMaterial == null || _trackingDashScrollSpeed <= 0f)
        return;

      float direction = _trackingDashFlowFromTarget ? -1f : 1f;
      _trackingDashOffset = Mathf.Repeat(
        _trackingDashOffset + (Time.unscaledDeltaTime * _trackingDashScrollSpeed * direction),
        1f);

      ApplyTextureTransform();
    }

    private void ApplyTextureTransform()
    {
      if (_trackingLineRenderer == null)
        return;

      if (_trackingLinePropertyBlock == null)
        _trackingLinePropertyBlock = new MaterialPropertyBlock();

      _trackingLineRenderer.GetPropertyBlock(_trackingLinePropertyBlock);
      _trackingLinePropertyBlock.SetVector(
        MainTexStId,
        new Vector4(_trackingTextureScaleX, 1f, _trackingDashOffset, 0f));
      _trackingLineRenderer.SetPropertyBlock(_trackingLinePropertyBlock);
    }

    private void UpdateSparkle()
    {
      if (_trackingLineRenderer == null)
        return;

      float pulse = 1f;
      if (_trackingSparkleIntensity > 0f && _trackingSparkleSpeed > 0f)
      {
        pulse = 1f + Mathf.Sin(Time.unscaledTime * _trackingSparkleSpeed) * _trackingSparkleIntensity;
        pulse = Mathf.Max(0.1f, pulse);
      }

      Color color = new Color(
        _trackingLineColor.r * pulse,
        _trackingLineColor.g * pulse,
        _trackingLineColor.b * pulse,
        _trackingLineColor.a);

      _trackingLineRenderer.startColor = color;
      _trackingLineRenderer.endColor = color;

      if (_trackingLineMaterial != null)
        _trackingLineMaterial.color = color;
    }

    private void SetTrackingLineEnabled(bool enabled)
    {
      if (_trackingLineRenderer != null)
        _trackingLineRenderer.enabled = enabled;
    }

    private void DisableTrackingLine()
    {
      SetTrackingLineEnabled(false);
    }

    private void ReleaseTrackingLineResources()
    {
      if (_trackingLineMaterial != null)
        Destroy(_trackingLineMaterial);

      if (_trackingLineTexture != null)
        Destroy(_trackingLineTexture);

      _trackingLineMaterial = null;
      _trackingLineTexture = null;
    }
  }
}
