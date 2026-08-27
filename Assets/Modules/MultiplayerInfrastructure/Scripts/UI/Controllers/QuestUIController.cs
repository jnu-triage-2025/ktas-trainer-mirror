using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Quest;
using MultiplayerInfrastructure.Registry;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  [RequireComponent(typeof(UIDocument))]
  public class QuestUIController : UIControllerABC, IUIOverlay
  {
    [SerializeField] private string _questRootName = DefaultsQuestControl.QuestRootName;
    [SerializeField] private float _sortingOrder = DefaultsUIDocument.QuestPanelUISortOrder;
    [SerializeField] private StyleSheet _styleSheet;
    [SerializeField] private QuestManager _questManager;

    private UIDocument _uiDocument;
    private QuestPanelElement _questPanel;
    private bool _managerHooked;

    public bool IsOpen => _questPanel != null && _questPanel.IsOpen;

    public event Action OverlayPushed;
    public event Action OverlayPopped;

    private void Start()
    {
      _uiDocument = GetComponent<UIDocument>();
      _uiDocument.sortingOrder = _sortingOrder;

      BindElement();
      AttachManager();
      HideImmediately();
    }

    private void OnEnable()
    {
      AttachManager();
    }

    private void OnDisable()
    {
      DetachManager();
    }

    public void Open()
    {
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
      {
        UIOverlayStack.Pop();
      }
      else
      {
        HidePanel();
      }
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

      VisualElement root = _uiDocument.rootVisualElement;
      EnsureStyleSheet(root);
      _questPanel = root.Q<QuestPanelElement>(_questRootName);

      if (_questPanel == null)
      {
        _questPanel = new QuestPanelElement();
        EnsureStyleSheet(_questPanel);
        root.Add(_questPanel);
      }

      _questPanel.OnTrackToggled += HandleTrackToggled;
      _questPanel.OnCloseRequested += HandleCloseRequested;
    }

    private void EnsurePanel()
    {
      if (_questPanel == null)
        BindElement();
    }

    private void EnsureStyleSheet(VisualElement ve)
    {
      if (ve == null)
        return;

      if (_styleSheet != null && !ve.styleSheets.Contains(_styleSheet))
        ve.styleSheets.Add(_styleSheet);
    }

    private void AttachManager()
    {
      if (_managerHooked)
        return;

      if (_questManager == null)
        _questManager = Registry.Registry.Get<QuestManager>(RegistryType.Service, Registry.Registry.TypeKey<QuestManager>());

      if (_questManager == null)
        return;

      _questManager.OnQuestListChanged += HandleQuestListChanged;
      _questManager.OnTrackedQuestsChanged += HandleTrackedChanged;
      _managerHooked = true;

      HandleQuestListChanged(_questManager.Quests);
      HandleTrackedChanged(_questManager.TrackedQuests);
    }

    private void DetachManager()
    {
      if (!_managerHooked || _questManager == null)
        return;

      _questManager.OnQuestListChanged -= HandleQuestListChanged;
      _questManager.OnTrackedQuestsChanged -= HandleTrackedChanged;
      _managerHooked = false;
    }

    private void HandleQuestListChanged(IReadOnlyList<QuestData> quests)
    {
      EnsurePanel();
      _questPanel?.SetQuests(quests);
    }

    private void HandleTrackedChanged(IReadOnlyList<QuestData> tracked)
    {
      EnsurePanel();
      _questPanel?.UpdateTracked(tracked);
    }

    private void HandleTrackToggled(string questId, bool targetState)
    {
      _questManager?.SetTracked(questId, targetState);
    }

    /// <summary>패널 헤더의 × 버튼. 오버레이 스택을 통해 J 키·Esc 와 동일한 경로로 닫는다.</summary>
    private void HandleCloseRequested()
    {
      Close();
    }

    private void ShowPanel()
    {
      EnsurePanel();
      SetDocumentVisible(_uiDocument, true);
      _questPanel?.SetOpen(true);
    }

    private void HidePanel()
    {
      if (_questPanel != null)
        _questPanel.SetOpen(false);
      SetDocumentVisible(_uiDocument, false);
    }

    private void HideImmediately()
    {
      EnsurePanel();
      _questPanel?.SetOpen(false);
      SetDocumentVisible(_uiDocument, false);
    }

  }
}
