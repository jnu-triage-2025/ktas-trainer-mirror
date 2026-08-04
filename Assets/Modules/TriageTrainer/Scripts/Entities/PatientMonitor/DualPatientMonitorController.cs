using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TriageTrainer.Entity.PatientMonitor.Models
{
  /// <summary>
  /// Graph/Metrics 자식 UIDocument를 각각 출력하는 2-Plane 컨트롤러입니다.
  /// 그래픽 데이터 공급과 환자 상태는 PatientMonitorController에서 공유하고,
  /// 출력 대상만 두 개의 PatientMonitorPlane으로 분리합니다.
  /// </summary>
  [AddComponentMenu("Triage Trainer/Patient Monitor/Dual Patient Monitor Controller")]
  public sealed class DualPatientMonitorController : PatientMonitorController
  {
    protected override void Reset()
    {
      base.Reset();
      SetDefaultPatientTrackingMethod(PatientTrackingMethod.DependsOnPatientCareZone);
    }
    private bool _ensuringDisplayPlanes;

    [Header("Dual Plane Surface")]
    [SerializeField] private bool _createDefaultSurfaceWhenMissing = true;
    [SerializeField] private PanelSettings _panelSettings;

    protected override void OnEnable()
    {
      ValidateParentDocumentConfiguration();
      EnsureDisplayPlaneObjects();
      base.OnEnable();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
      ValidateParentDocumentConfiguration();
      EnsureDisplayPlaneObjects();
      base.OnValidate();
    }
#endif

    protected override bool BuildsSinglePlaneGraphic => false;

    protected override void ConfigureDisplayLayout()
    {
      EnsureDisplayPlaneObjects();
      _displayViews.Clear();

      var parentRoot = uiDocument != null ? uiDocument.rootVisualElement : null;
      if (parentRoot != null)
        parentRoot.style.display = DisplayStyle.None;

      var planes = GetComponentsInChildren<TriageTrainer.Entity.PatientMonitor.PatientMonitorPlane>(true);
      bool hasGraphPlane = false;
      bool hasMetricsPlane = false;

      for (int i = 0; i < planes.Length; i++)
      {
        var plane = planes[i];
        if (plane == null || plane.transform == transform || plane.Document == null)
          continue;

        // Dual 모드는 명시적인 Graph/Metrics 컴포넌트만 출력 대상으로 사용합니다.
        if (!(plane is TriageTrainer.Entity.PatientMonitor.DualPatientMonitorGraphPartController) &&
            !(plane is TriageTrainer.Entity.PatientMonitor.DualPatientMonitorMetricsPartController))
          continue;

        var childRoot = plane.Document.rootVisualElement;
        if (childRoot == null)
          continue;

        var view = new TriageTrainer.Entity.PatientMonitor.PatientMonitorDisplayView(plane.Type);
        childRoot.style.display = DisplayStyle.Flex;
        view.Build(
          plane.Document,
          new[] { ecgColor, plethColor, artColor, cvpColor },
          lineThickness,
          Mathf.Clamp(ResolveHorizontalPoints(), 80, 2400));
        _displayViews.Add(view);

        hasGraphPlane |= plane.Type == TriageTrainer.Entity.PatientMonitor.PatientMonitorPlaneType.Graph;
        hasMetricsPlane |= plane.Type == TriageTrainer.Entity.PatientMonitor.PatientMonitorPlaneType.Metrics;
      }

      if (hasGraphPlane && hasMetricsPlane)
        return;

      Debug.LogWarning(
        "[DualPatientMonitorController] Graph와 Metrics 타입의 PatientMonitorPlane 자식이 각각 필요합니다. 단일 UIDocument를 표시합니다.",
        this);
      _displayViews.Clear();
      if (parentRoot != null)
        parentRoot.style.display = DisplayStyle.Flex;
      HideChildPlaneDocuments(planes);
    }

    protected override void OpenDetailedContentOverlay()
    {
      if (!EnableDetailedContentOverlay || _displayViews.Count == 0)
        return;

      var contents = new System.Collections.Generic.List<UnityEngine.UIElements.VisualElement>(_displayViews.Count);
      for (int i = 0; i < _displayViews.Count; i++)
      {
        if (_displayViews[i].ContentRoot != null)
          contents.Add(_displayViews[i].ContentRoot);
      }

      TriageTrainer.Entity.PatientMonitor.PatientMonitorDetailOverlay.Open(
        this,
        contents,
        RestoreDualMonitorContents,
        RequestClose);
    }

    protected override void CloseDetailedContentOverlay()
    {
      TriageTrainer.Entity.PatientMonitor.PatientMonitorDetailOverlay.Close(this);
    }

    private void RestoreDualMonitorContents()
    {
      for (int i = 0; i < _displayViews.Count; i++)
        _displayViews[i].RestoreContentToMonitor();
    }

    private void EnsureDisplayPlaneObjects()
    {
      if (_ensuringDisplayPlanes)
        return;

      _ensuringDisplayPlanes = true;
      try
      {
        var parentDocument = GetComponent<UIDocument>();
        EnsureDisplayPlane<TriageTrainer.Entity.PatientMonitor.DualPatientMonitorGraphPartController>("Graph", parentDocument);
        EnsureDisplayPlane<TriageTrainer.Entity.PatientMonitor.DualPatientMonitorMetricsPartController>("Metrics", parentDocument);
      }
      finally
      {
        _ensuringDisplayPlanes = false;
      }
    }

    private void EnsureDisplayPlane<TMarker>(string objectName, UIDocument parentDocument)
      where TMarker : TriageTrainer.Entity.PatientMonitor.PatientMonitorPlane
    {
      var existing = GetComponentInChildren<TMarker>(true);
      if (existing != null)
      {
        var existingDocument = existing.GetComponent<UIDocument>();
        ConfigureIndependentDocument(existingDocument, parentDocument);
        EnsureSurfaceAndBinder(existing.gameObject, objectName);
        return;
      }

      var planeObject = new GameObject(objectName);
      planeObject.transform.SetParent(transform, false);
      planeObject.transform.localPosition = new Vector3(
        objectName == "Graph" ? -1f : 1f,
        0f,
        0f);
      AddComponent<TMarker>(planeObject);
      var document = planeObject.GetComponent<UIDocument>();
      ConfigureIndependentDocument(document, parentDocument);

      EnsureSurfaceAndBinder(planeObject, objectName);

#if UNITY_EDITOR
      if (!Application.isPlaying)
      {
        Undo.RegisterCreatedObjectUndo(planeObject, $"Create Patient Monitor {objectName} Plane");
        EditorUtility.SetDirty(this);
      }
#endif
    }

    private static TMarker AddComponent<TMarker>(GameObject target)
      where TMarker : Component
    {
      return target.AddComponent<TMarker>();
    }

    private void ConfigureIndependentDocument(UIDocument document, UIDocument parentDocument)
    {
      if (document == null)
        return;

      var settings = _panelSettings != null
        ? _panelSettings
        : (parentDocument != null ? parentDocument.panelSettings : null);
      if (document.panelSettings == null && settings != null)
        document.panelSettings = settings;
    }

    private void ValidateParentDocumentConfiguration()
    {
      var parentDocument = GetComponent<UIDocument>();
      if (parentDocument != null)
      {
        Debug.LogError(
          "[DualPatientMonitorController] Dual 컨트롤러 부모에는 UIDocument를 두지 마십시오. Graph/Metrics 자식에 각각 독립 PanelSettings를 지정해야 합니다.",
          this);
      }

      if (_panelSettings == null)
      {
        var childDocuments = GetComponentsInChildren<UIDocument>(true);
        for (var i = 0; i < childDocuments.Length; i++)
        {
          if (childDocuments[i] == null || childDocuments[i].transform == transform || childDocuments[i].panelSettings == null)
            continue;

          _panelSettings = childDocuments[i].panelSettings;
          break;
        }

        if (_panelSettings == null)
        {
          Debug.LogError(
            "[DualPatientMonitorController] _panelSettings가 비어 있습니다. Graph/Metrics 자식 UIDocument에 사용할 PanelSettings를 지정하십시오.",
            this);
        }
      }
    }

    private void EnsureSurfaceAndBinder(GameObject planeObject, string objectName)
    {
      if (planeObject == null)
        return;

      var renderer = planeObject.GetComponentInChildren<MeshRenderer>(true);
      if (renderer == null && _createDefaultSurfaceWhenMissing)
      {
        var surface = GameObject.CreatePrimitive(PrimitiveType.Quad);
        surface.name = $"{objectName} Surface";
        surface.transform.SetParent(planeObject.transform, false);
        surface.transform.localPosition = Vector3.zero;
        surface.transform.localRotation = Quaternion.identity;
        surface.transform.localScale = Vector3.one;
        renderer = surface.GetComponent<MeshRenderer>();

#if UNITY_EDITOR
        if (!Application.isPlaying)
          Undo.RegisterCreatedObjectUndo(surface, $"Create Patient Monitor {objectName} Surface");
#endif
      }

      if (renderer == null)
      {
        Debug.LogWarning(
          $"[DualPatientMonitorController] {objectName} 자식에 MeshRenderer가 없습니다. 메시를 배치하거나 기본 표면 생성을 활성화하십시오.",
          planeObject);
        return;
      }

      if (planeObject.GetComponent<TriageTrainer.Entity.PatientMonitor.UIDocumentWorldSurfaceBinder>() == null)
        planeObject.AddComponent<TriageTrainer.Entity.PatientMonitor.UIDocumentWorldSurfaceBinder>();
    }

    private static void HideChildPlaneDocuments(TriageTrainer.Entity.PatientMonitor.PatientMonitorPlane[] planes)
    {
      if (planes == null)
        return;

      for (int i = 0; i < planes.Length; i++)
      {
        var document = planes[i] != null ? planes[i].Document : null;
        var root = document != null ? document.rootVisualElement : null;
        if (root != null)
          root.style.display = DisplayStyle.None;
      }
    }
  }
}
