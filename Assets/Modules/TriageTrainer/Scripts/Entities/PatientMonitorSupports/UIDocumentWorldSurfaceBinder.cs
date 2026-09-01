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

    [Header("RenderTexture")]
    [SerializeField] private Vector2Int _resolution = new Vector2Int(2400, 1200);
    [SerializeField] private int _depthBuffer = 24;
    [SerializeField] private RenderTextureFormat _format = RenderTextureFormat.ARGB32;
    [SerializeField] private FilterMode _filterMode = FilterMode.Bilinear;

    private UIDocument _document;
    private PanelSettings _originalPanelSettings;
    private PanelSettings _runtimePanelSettings;
    private RenderTexture _renderTexture;
    private MaterialPropertyBlock _originalPropertyBlock;
    private MaterialPropertyBlock _runtimePropertyBlock;

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

      if (_document == null)
      {
        Debug.LogError("[UIDocumentWorldSurfaceBinder] UIDocument이 없습니다.", this);
        enabled = false;
        return;
      }

      if (_document.parentUI != null)
      {
        Debug.LogError(
          "[UIDocumentWorldSurfaceBinder] RenderTexture 출력 UIDocument는 다른 UIDocument의 자식일 수 없습니다. 별도 PanelSettings를 사용하도록 계층을 분리하십시오.",
          this);
        enabled = false;
        return;
      }

      if (_document.panelSettings == null)
      {
        Debug.LogError("[UIDocumentWorldSurfaceBinder] PanelSettings가 없습니다.", this);
        enabled = false;
        return;
      }

      if (_targetRenderer == null)
      {
        _targetRenderer = GetComponent<MeshRenderer>();
        if (_targetRenderer == null)
          _targetRenderer = GetComponentInChildren<MeshRenderer>(true);

        Debug.LogWarning("[UIDocumentWorldSurfaceBinder] 대상 MeshRenderer가 지정되지 않아, 오브젝트 또는 하위 오브젝트의 MeshRenderer를 사용합니다.", this);
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

      var plane = GetComponent<PatientMonitorPlane>();
      Vector2Int resolution = plane != null ? plane.LowResolution : _resolution;
      _renderTexture = new RenderTexture(resolution.x, resolution.y, _depthBuffer, _format)
      {
        name = $"{gameObject.name}_UIDocumentRT",
        filterMode = _filterMode
      };
      _renderTexture.Create();

      _runtimePanelSettings.targetTexture = _renderTexture;

      // Renderer.material 은 Material 인스턴스를 만들고 셰이더를 동기 컴파일할 수
      // 있다. macOS 의 MPPM 가상 플레이어에서 이것이 씬 로드 중 Unity 네이티브
      // 모달 진행 백엔드에 진입하여 에디터가 crash 할 수 있다. 프로퍼티 블록은
      // 머티리얼 인스턴스화 없이 렌더러별 텍스처를 적용한다.
      _originalPropertyBlock = new MaterialPropertyBlock();
      _targetRenderer.GetPropertyBlock(_originalPropertyBlock);

      _runtimePropertyBlock = new MaterialPropertyBlock();
      _targetRenderer.GetPropertyBlock(_runtimePropertyBlock);
      _runtimePropertyBlock.SetTexture(Shader.PropertyToID(_texturePropertyName), _renderTexture);
      _targetRenderer.SetPropertyBlock(_runtimePropertyBlock);
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

      if (_targetRenderer != null && _originalPropertyBlock != null)
      {
        _targetRenderer.SetPropertyBlock(_originalPropertyBlock);
      }

      _originalPropertyBlock = null;
      _runtimePropertyBlock = null;

      if (_renderTexture != null)
      {
        if (_renderTexture.IsCreated())
        {
          _renderTexture.Release();
        }

        Destroy(_renderTexture);
        _renderTexture = null;
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
