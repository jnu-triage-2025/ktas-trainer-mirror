using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace TriageTrainer.Entity.PatientMonitor
{
  [RequireComponent(typeof(UIDocument))]
  public class UIDocumentWorldSurfaceBinder : MonoBehaviour
  {
    [Header("Target Surface")]
    [SerializeField] private MeshRenderer _targetRenderer;
    [SerializeField] private string _texturePropertyName = "_BaseMap";
    [SerializeField] private bool _instantiateMaterial = true;

    [Header("RenderTexture")]
    [SerializeField] private Vector2Int _resolution = new Vector2Int(2400, 1200);
    [SerializeField] private int _depthBuffer = 24;
    [SerializeField] private RenderTextureFormat _format = RenderTextureFormat.ARGB32;
    [SerializeField] private FilterMode _filterMode = FilterMode.Bilinear;

    private UIDocument _document;
    private PanelSettings _originalPanelSettings;
    private PanelSettings _runtimePanelSettings;
    private RenderTexture _renderTexture;
    private Material _runtimeMaterial;

    private void Awake()
    {
      _document = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
      if (_document == null)
      {
        _document = GetComponent<UIDocument>();
      }

      if (_document == null || _document.panelSettings == null)
      {
        Debug.LogError("[UIDocumentWorldSurfaceBinder] UIDocument 또는 PanelSettings가 없습니다.", this);
        enabled = false;
        return;
      }

      if (_targetRenderer == null)
      {
        _targetRenderer = GetComponent<MeshRenderer>();
        Debug.LogWarning("[UIDocumentWorldSurfaceBinder] 대상 MeshRenderer가 지정되지 않아, 오브젝트의 MeshRenderer를 사용합니다.", this);
      }
      if (_targetRenderer == null)
      {
        Debug.LogError("[UIDocumentWorldSurfaceBinder] 대상 MeshRenderer가 지정되지 않았습니다.", this);
        enabled = false;
        return;
      }

      _originalPanelSettings = _document.panelSettings;
      _runtimePanelSettings = Instantiate(_originalPanelSettings);
      _runtimePanelSettings.name = _originalPanelSettings.name + " (Runtime)";
      _document.panelSettings = _runtimePanelSettings;

      _renderTexture = new RenderTexture(_resolution.x, _resolution.y, _depthBuffer, _format)
      {
        name = $"{gameObject.name}_UIDocumentRT",
        filterMode = _filterMode
      };
      _renderTexture.Create();

      _runtimePanelSettings.targetTexture = _renderTexture;

      if (_instantiateMaterial)
      {
        _runtimeMaterial = new Material(_targetRenderer.material);
        _targetRenderer.material = _runtimeMaterial;
      }

      _targetRenderer.material.SetTexture(_texturePropertyName, _renderTexture);
      ClearRuntimeMonitorPanelSelection();
    }

    private void LateUpdate()
    {
      ClearRuntimeMonitorPanelSelection();
    }

    private void ClearRuntimeMonitorPanelSelection()
    {
      var eventSystem = EventSystem.current;
      if (eventSystem == null)
        return;

      var selected = eventSystem.currentSelectedGameObject;
      if (selected == null)
        return;

      if (selected.name.StartsWith("PatientMonitorPanelSettings", System.StringComparison.Ordinal))
        eventSystem.SetSelectedGameObject(null);
    }

    private void OnDisable()
    {
      if (_document != null && _originalPanelSettings != null)
      {
        _document.panelSettings = _originalPanelSettings;
      }

      if (_runtimePanelSettings != null)
      {
        _runtimePanelSettings.targetTexture = null;
      }

      if (_renderTexture != null)
      {
        if (_renderTexture.IsCreated())
        {
          _renderTexture.Release();
        }

        Destroy(_renderTexture);
        _renderTexture = null;
      }

      if (_runtimeMaterial != null)
      {
        Destroy(_runtimeMaterial);
        _runtimeMaterial = null;
      }

      if (_runtimePanelSettings != null)
      {
        Destroy(_runtimePanelSettings);
        _runtimePanelSettings = null;
      }

      _originalPanelSettings = null;
    }
  }
}
