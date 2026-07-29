using System.Collections.Generic;
using UnityEngine;

namespace TriageTrainer.Entity.PatientMonitor.Models
{
  public partial class PatientMonitorController
  {
    [Header("Tracking Line")]
    [SerializeField] private bool _showTrackingLine = true;
    [SerializeField] private bool _showTrackingLineForDefaultPatient = false;
    [SerializeField] private Transform _trackingLineStart;
    [SerializeField, Min(0.001f)] private float _trackingLineWidth = 0.02f;
    [SerializeField] private Color _trackingLineColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField, Min(0.01f)] private float _trackingDashLength = 0.1f;
    [SerializeField, Min(0.01f)] private float _trackingDashGap = 0.1f;
    [SerializeField, Min(0f)] private float _trackingDashScrollSpeed = 0.9f;
    [SerializeField, Range(0f, 0.6f)] private float _trackingSparkleIntensity = 0.2f;
    [SerializeField, Min(0f)] private float _trackingSparkleSpeed = 2.2f;

    private const string TrackingLineObjectName = "PatientMonitorTrackingLine";

    private LineRenderer _trackingLineRenderer;
    private Material _trackingLineMaterial;
    private float _trackingDashDistance;
    private readonly List<LineRenderer> _trackingDashRenderers = new();

    private void EnsureTrackingLineRenderer()
    {
      if (!_showTrackingLine)
        return;

      if (_trackingLineRenderer == null && !TryResolveTrackingLineRenderer())
        _trackingLineRenderer = CreateTrackingLineRenderer();

      if (_trackingLineRenderer == null)
        return;

      if (_trackingDashRenderers.Count == 0)
        _trackingDashRenderers.Add(_trackingLineRenderer);

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
    }

    private void ApplyTrackingLineSettings()
    {
      if (_trackingLineRenderer == null || _trackingLineMaterial == null)
        return;

      _trackingLineMaterial.color = _trackingLineColor;
      for (int i = 0; i < _trackingDashRenderers.Count; i++)
        ConfigureDashRenderer(_trackingDashRenderers[i]);
    }

    private void ConfigureDashRenderer(LineRenderer renderer)
    {
      if (renderer == null)
        return;

      renderer.useWorldSpace = true;
      renderer.alignment = LineAlignment.View;
      renderer.textureMode = LineTextureMode.Stretch;
      renderer.numCornerVertices = 0;
      renderer.numCapVertices = 0;
      renderer.startWidth = _trackingLineWidth;
      renderer.endWidth = _trackingLineWidth;
      renderer.startColor = _trackingLineColor;
      renderer.endColor = _trackingLineColor;
      renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
      renderer.receiveShadows = false;
      renderer.motionVectorGenerationMode = UnityEngine.MotionVectorGenerationMode.ForceNoMotion;
      renderer.material = _trackingLineMaterial;
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
      if (_trackingLineRenderer == null || _trackingLineMaterial == null)
        return;

      Vector3 start = ResolveTrackingStartPosition();
      Vector3 end = ResolveTrackingEndPosition(target);
      UpdateDashSegments(start, end);
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
      return startTransform.position;
    }

    private Vector3 ResolveTrackingEndPosition(PatientController target)
    {
      if (target == null)
        return ResolveTrackingStartPosition();

      var endpoint = target.GetComponentInChildren<PatientMonitorTrackingLineEndpoint>(true);
      return endpoint != null ? endpoint.transform.position : target.transform.position;
    }

    private void UpdateDashSegments(Vector3 start, Vector3 end)
    {
      float length = Vector3.Distance(start, end);
      float patternLength = Mathf.Max(0.01f, _trackingDashLength + _trackingDashGap);
      if (length <= Mathf.Epsilon)
      {
        DisableUnusedDashRenderers(0);
        return;
      }

      // Positive distance moves each dash from the monitor toward the patient.
      _trackingDashDistance = Mathf.Repeat(
        _trackingDashDistance + Time.unscaledDeltaTime * _trackingDashScrollSpeed,
        patternLength);

      Vector3 direction = (end - start) / length;
      int rendererIndex = 0;
      int firstPattern = Mathf.FloorToInt(-_trackingDashDistance / patternLength) - 1;
      int lastPattern = Mathf.CeilToInt((length - _trackingDashDistance) / patternLength);
      for (int patternIndex = firstPattern; patternIndex <= lastPattern; patternIndex++)
      {
        float dashStart = _trackingDashDistance + patternIndex * patternLength;
        float dashEnd = dashStart + _trackingDashLength;
        float clippedStart = Mathf.Max(0f, dashStart);
        float clippedEnd = Mathf.Min(length, dashEnd);
        if (clippedEnd <= clippedStart)
          continue;

        var renderer = GetOrCreateDashRenderer(rendererIndex++);
        renderer.positionCount = 2;
        renderer.SetPosition(0, start + direction * clippedStart);
        renderer.SetPosition(1, start + direction * clippedEnd);
        renderer.enabled = true;
      }

      DisableUnusedDashRenderers(rendererIndex);
    }

    private LineRenderer GetOrCreateDashRenderer(int index)
    {
      while (_trackingDashRenderers.Count <= index)
      {
        GameObject dashObject = new GameObject($"{TrackingLineObjectName}_Dash{_trackingDashRenderers.Count}");
        dashObject.transform.SetParent(transform, false);
        var renderer = dashObject.AddComponent<LineRenderer>();
        _trackingDashRenderers.Add(renderer);
        ConfigureDashRenderer(renderer);
      }

      return _trackingDashRenderers[index];
    }

    private void DisableUnusedDashRenderers(int usedCount)
    {
      for (int i = usedCount; i < _trackingDashRenderers.Count; i++)
      {
        if (_trackingDashRenderers[i] != null)
          _trackingDashRenderers[i].enabled = false;
      }
    }

    private void UpdateSparkle()
    {
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

      if (_trackingLineMaterial != null)
        _trackingLineMaterial.color = color;
      for (int i = 0; i < _trackingDashRenderers.Count; i++)
      {
        var renderer = _trackingDashRenderers[i];
        if (renderer == null)
          continue;
        renderer.startColor = color;
        renderer.endColor = color;
      }
    }

    private void SetTrackingLineEnabled(bool enabled)
    {
      for (int i = 0; i < _trackingDashRenderers.Count; i++)
      {
        if (_trackingDashRenderers[i] != null)
          _trackingDashRenderers[i].enabled = enabled;
      }
    }

    private void DisableTrackingLine()
    {
      SetTrackingLineEnabled(false);
    }

    private void ReleaseTrackingLineResources()
    {
      if (_trackingLineMaterial != null)
        Destroy(_trackingLineMaterial);

      _trackingLineMaterial = null;
      _trackingDashRenderers.Clear();
    }
  }
}
