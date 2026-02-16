using System.Collections.Generic;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Quest;
using MultiplayerInfrastructure.Registry;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  [RequireComponent(typeof(UIDocument))]
  public class QuestPreviewHudUIController : UIControllerABC
  {
    [SerializeField] private string _rootName = DefaultsQuestControl.QuestPreviewRootName;
    [SerializeField] private float _sortingOrder = DefaultsUIDocument.QuestPreviewHudSortOrder;
    [SerializeField] private StyleSheet _styleSheet;
    [SerializeField] private QuestManager _questManager;

    private UIDocument _uiDocument;
    private QuestPreviewHudElement _hudElement;
    private bool _managerHooked;

    private void Start()
    {
      _uiDocument = GetComponent<UIDocument>();
      _uiDocument.sortingOrder = _sortingOrder;
      BindElement();
      AttachManager();
    }

    private void OnEnable()
    {
      AttachManager();
    }

    private void OnDisable()
    {
      DetachManager();
    }

    private void BindElement()
    {
      if (_uiDocument == null)
        _uiDocument = GetComponent<UIDocument>();

      var root = _uiDocument.rootVisualElement;
      EnsureStyleSheet(root);

      _hudElement = root.Q<QuestPreviewHudElement>(_rootName);
      if (_hudElement == null)
      {
        _hudElement = new QuestPreviewHudElement();
        EnsureStyleSheet(_hudElement);
        root.Add(_hudElement);
      }
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
        _questManager = Registry.Registry.Get<QuestManager>(RegistryType.Entity, Registry.Registry.TypeKey<QuestManager>());

      if (_questManager == null)
        return;

      _questManager.OnTrackedQuestsChanged += HandleTrackedChanged;
      _managerHooked = true;

      HandleTrackedChanged(_questManager.TrackedQuests);
    }

    private void DetachManager()
    {
      if (!_managerHooked || _questManager == null)
        return;

      _questManager.OnTrackedQuestsChanged -= HandleTrackedChanged;
      _managerHooked = false;
    }

    private void HandleTrackedChanged(IReadOnlyList<QuestData> tracked)
    {
      _hudElement?.SetTrackedQuests(tracked);
    }
  }
}
