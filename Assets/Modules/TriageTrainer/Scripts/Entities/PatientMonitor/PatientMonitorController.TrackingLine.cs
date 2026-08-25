using System.Collections.Generic;
using FishNet;
using MultiplayerInfrastructure.Performance;
using TriageTrainer.Entity.ElectricalLine;
using TriageTrainer.Entity.LineConnection;
using UnityEngine;

namespace TriageTrainer.Entity.PatientMonitor.Models
{
  [System.Flags]
  public enum PatientMonitorTrackingLineDisplayOption
  {
    None = 0,
    Renderer = 1 << 0,
    ElectricalLine = 1 << 1,
  }

  [System.Flags]
  public enum PatientMonitorLineConnectionServiceUnavailableOption
  {
    None = 0,
    LogWarning = 1 << 0,
    ShowRenderer = 1 << 1,
  }

  public partial class PatientMonitorController
  {
    [Header("Tracking Line")]
    [SerializeField]
    private PatientMonitorTrackingLineDisplayOption _trackingLineDisplayOptions =
      PatientMonitorTrackingLineDisplayOption.ElectricalLine;
    [SerializeField]
    private PatientMonitorLineConnectionServiceUnavailableOption
      _lineConnectionServiceUnavailableOptions = PatientMonitorLineConnectionServiceUnavailableOption.None;
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
    private const float ElectricalLineConnectionRetrySeconds = 1f;

    private LineRenderer _trackingLineRenderer;
    private Material _trackingLineMaterial;
    private float _trackingDashDistance;
    private readonly List<LineRenderer> _trackingDashRenderers = new();
    private ElectricalLineConnectionPoint _trackingElectricalLineStartPoint;
    private ElectricalLineConnectionPoint _trackingElectricalLineEndPoint;
    private LineConnectionService _trackingElectricalLineService;
    private bool _hasLoggedUnavailableLineConnectionService;
    private float _nextElectricalLineConnectionAttemptAt;

    private bool UsesTrackingLineRenderer =>
      (_trackingLineDisplayOptions & PatientMonitorTrackingLineDisplayOption.Renderer) != 0;

    private bool UsesElectricalTrackingLine =>
      (_trackingLineDisplayOptions & PatientMonitorTrackingLineDisplayOption.ElectricalLine) != 0;

    private bool ShouldShowRendererWhenLineConnectionServiceUnavailable =>
      (_lineConnectionServiceUnavailableOptions
       & PatientMonitorLineConnectionServiceUnavailableOption.ShowRenderer) != 0;

    private void EnsureTrackingLineRenderer()
    {
      if (MppmLiteMode.IsHeadless)
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
      if (MppmLiteMode.IsHeadless)
      {
        SetTrackingLineEnabled(false);
        DisconnectElectricalTrackingLine();
        return;
      }

      var target = ResolveTrackingTarget();
      if (target == null)
      {
        SetTrackingLineEnabled(false);
        DisconnectElectricalTrackingLine();
        return;
      }

      bool lineConnectionServiceAvailable = UpdateElectricalTrackingLine(target);
      if (!UsesTrackingLineRenderer
          && (!UsesElectricalTrackingLine || lineConnectionServiceAvailable
              || !ShouldShowRendererWhenLineConnectionServiceUnavailable))
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

    private bool UpdateElectricalTrackingLine(PatientController target)
    {
      if (!UsesElectricalTrackingLine)
      {
        DisconnectElectricalTrackingLine();
        return true;
      }

      var service = LineConnectionService.TopologyService
                    ?? FindFirstObjectByType<LineConnectionService>(FindObjectsInactive.Include);
      if (service == null || !service.isActiveAndEnabled)
      {
        SetElectricalTrackingLineObjectActive(false);
        HandleUnavailableLineConnectionService();
        return false;
      }

      _hasLoggedUnavailableLineConnectionService = false;
      var startPoint = ResolveElectricalTrackingLineStartPoint();
      var endPoint = target.GetComponentInChildren<ElectricalLineConnectionPoint>(true);
      if (startPoint == null || endPoint == null)
      {
        DisconnectElectricalTrackingLine();
        return true;
      }

      if (_trackingElectricalLineService != null
          && (!ReferenceEquals(_trackingElectricalLineService, service)
              || !ReferenceEquals(_trackingElectricalLineStartPoint, startPoint)
              || !ReferenceEquals(_trackingElectricalLineEndPoint, endPoint)))
        DisconnectElectricalTrackingLine();

      _trackingElectricalLineService = service;
      _trackingElectricalLineStartPoint = startPoint;
      _trackingElectricalLineEndPoint = endPoint;
      SetElectricalTrackingLineObjectActive(true);
      if (!startPoint.IsPhysicallyConnectedTo(endPoint)
          && (InstanceFinder.IsOffline || IsServerStarted)
          && Time.unscaledTime >= _nextElectricalLineConnectionAttemptAt)
      {
        if (service.TryCreateAutomaticConnection(startPoint, endPoint))
          _nextElectricalLineConnectionAttemptAt = 0f;
        else
          _nextElectricalLineConnectionAttemptAt =
            Time.unscaledTime + ElectricalLineConnectionRetrySeconds;
      }

      return true;
    }

    private ElectricalLineConnectionPoint ResolveElectricalTrackingLineStartPoint()
    {
      var startTransform = _trackingLineStart != null ? _trackingLineStart : transform;
      if (_trackingElectricalLineStartPoint != null
          && (_trackingLineStart == null
              || _trackingElectricalLineStartPoint.transform == startTransform))
        return _trackingElectricalLineStartPoint;

      _trackingElectricalLineStartPoint = startTransform.GetComponent<ElectricalLineConnectionPoint>();
      if (_trackingElectricalLineStartPoint == null && _trackingLineStart == null)
        _trackingElectricalLineStartPoint = GetComponentInChildren<ElectricalLineConnectionPoint>(true);
      if (_trackingElectricalLineStartPoint == null)
        _trackingElectricalLineStartPoint = startTransform.gameObject.AddComponent<ElectricalLineConnectionPoint>();
      return _trackingElectricalLineStartPoint;
    }

    private void HandleUnavailableLineConnectionService()
    {
      if ((_lineConnectionServiceUnavailableOptions
           & PatientMonitorLineConnectionServiceUnavailableOption.LogWarning) == 0
          || _hasLoggedUnavailableLineConnectionService)
        return;

      _hasLoggedUnavailableLineConnectionService = true;
      Debug.LogWarning("[PatientMonitorController] LineConnectionService is unavailable; electrical tracking line was not updated.", this);
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
      DisconnectElectricalTrackingLine();
    }

    private void ReleaseTrackingLineResources()
    {
      DisconnectElectricalTrackingLine();
      if (_trackingLineMaterial != null)
        Destroy(_trackingLineMaterial);

      _trackingLineMaterial = null;
      _trackingDashRenderers.Clear();
    }

    private void DisconnectElectricalTrackingLine()
    {
      SetElectricalTrackingLineObjectActive(false);
      if (_trackingElectricalLineService != null && _trackingElectricalLineService.isActiveAndEnabled
          && _trackingElectricalLineStartPoint != null
          && _trackingElectricalLineEndPoint != null)
        _trackingElectricalLineService.DisconnectAutomaticConnection(
          _trackingElectricalLineStartPoint, _trackingElectricalLineEndPoint);

      _trackingElectricalLineService = null;
      _trackingElectricalLineStartPoint = null;
      _trackingElectricalLineEndPoint = null;
      _nextElectricalLineConnectionAttemptAt = 0f;
    }

    private void SetElectricalTrackingLineObjectActive(bool active)
    {
      if (_trackingElectricalLineStartPoint == null || _trackingElectricalLineEndPoint == null
          || !_trackingElectricalLineStartPoint.TryGetConnectedLineObjectTo(
            _trackingElectricalLineEndPoint, out var lineObject)
          || lineObject == null || lineObject.activeSelf == active)
        return;

      lineObject.SetActive(active);
    }
  }
}
