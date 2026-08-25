using System;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.UI;
using TriageTrainer.Entity.Patient;
using UnityEngine;
using UnityEngine.UIElements;

namespace TriageTrainer.UI
{
  /// <summary>
  /// 트리아지 평가 전체화면 오버레이 UI 컨트롤러.
  ///
  /// <para>
  /// 플레이어가 환자의 트리아지 인터랙션을 수행하면 <see cref="Open"/> 으로 열리며, 가로로 늘어진 KTAS 색상
  /// 사각형(<see cref="TriageAssessmentPanelElement"/>)을 표시한다. 사각형을 클릭하면 선택 콜백으로 등급을
  /// 통지하고 패널을 닫는다.
  /// </para>
  ///
  /// <para>
  /// <see cref="ProblemSheetUIController"/> 와 동일한 <see cref="IUIOverlay"/> / <see cref="UIOverlayStack"/>
  /// 관례를 따른다.
  /// </para>
  /// </summary>
  [RequireComponent(typeof(UIDocument))]
  public sealed class TriageAssessmentUIController : UIControllerABC, IUIOverlay
  {
    /// <summary>활성 인스턴스(단일 로컬 클라이언트에 하나). 환자 컨트롤러가 손쉽게 접근하기 위한 헬퍼.</summary>
    public static TriageAssessmentUIController ActiveInstance { get; private set; }

    [SerializeField] private float _sortingOrder = DefaultsUIDocument.TriageAssessmentUISortOrder;

    private UIDocument _uiDocument;
    private TriageAssessmentPanelElement _panel;
    private Action<TriageLevel> _onSelected;
    private TriageLevel _current = TriageLevel.Unassessed;

    public event Action OverlayPushed;
    public event Action OverlayPopped;

    public bool IsOpen => _panel != null && _panel.style.display != DisplayStyle.None;

    protected override void Awake()
    {
      base.Awake();
      ActiveInstance = this;
    }

    private void Start()
    {
      _uiDocument = GetComponent<UIDocument>();
      _uiDocument.sortingOrder = _sortingOrder;
      BindElement();
      HideImmediately();
    }

    private void OnEnable()
    {
      if (!IsOpen)
        StartCoroutine(NeutralizeDocumentRootWhenReady(_uiDocument));
    }

    protected override void OnDestroy()
    {
      if (ReferenceEquals(ActiveInstance, this))
        ActiveInstance = null;

      base.OnDestroy();
    }

    /// <summary>
    /// 트리아지 평가 패널을 연다.
    /// </summary>
    /// <param name="current">현재(기존) 평가 등급. 패널에서 해당 등급을 선택 상태로 강조 표시한다.</param>
    /// <param name="onSelected">등급 선택 시 호출되는 콜백.</param>
    public void Open(TriageLevel current, Action<TriageLevel> onSelected)
    {
      _onSelected = onSelected;
      _current = current;
      EnsurePanel();

      if (!UIOverlayStack.IsTop(this))
      {
        UIOverlayStack.Push(this);
        return;
      }

      ShowPanel();
    }

    public void Close()
    {
      if (UIOverlayStack.IsTop(this))
        UIOverlayStack.Pop();
      else
        HidePanel();
    }

    public void OnOverlayPushed()
    {
      ShowPanel();
      OverlayPushed?.Invoke();
    }

    public void OnOverlayPopped()
    {
      HidePanel();
      OverlayPopped?.Invoke();
    }

    private void BindElement()
    {
      if (_uiDocument == null)
        _uiDocument = GetComponent<UIDocument>();

      var root = _uiDocument.rootVisualElement;
      _panel = root.Q<TriageAssessmentPanelElement>();
      if (_panel == null)
      {
        _panel = new TriageAssessmentPanelElement();
        root.Add(_panel);
      }

      _panel.LevelSelected += HandleLevelSelected;
      _panel.CancelRequested += HandleCancelRequested;
    }

    private void HandleLevelSelected(TriageLevel level)
    {
      var callback = _onSelected;
      Close();
      callback?.Invoke(level);
    }

    private void HandleCancelRequested()
    {
      Close();
    }

    private void EnsurePanel()
    {
      if (_panel == null)
        BindElement();
    }

    private void ShowPanel()
    {
      EnsurePanel();
      _panel.SetCurrentSelection(_current);
      _panel.style.display = DisplayStyle.Flex;
      SetDocumentRootInteractable(_uiDocument, true);
    }

    private void HidePanel()
    {
      if (_panel != null)
        _panel.style.display = DisplayStyle.None;
      SetDocumentRootInteractable(_uiDocument, false);
    }

    private void HideImmediately()
    {
      EnsurePanel();
      _panel.style.display = DisplayStyle.None;
      SetDocumentRootInteractable(_uiDocument, false);
    }
  }
}
