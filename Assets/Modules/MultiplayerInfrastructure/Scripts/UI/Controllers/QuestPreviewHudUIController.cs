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
  public class QuestPreviewHudUIController : UIControllerABC
  {
    [SerializeField] private string _rootName = DefaultsQuestControl.QuestPreviewRootName;
    [SerializeField] private float _sortingOrder = DefaultsUIDocument.QuestPreviewHudSortOrder;
    [SerializeField] private StyleSheet _styleSheet;
    [SerializeField] private QuestManager _questManager;

    private UIDocument _uiDocument;
    private QuestPreviewHudElement _hudElement;
    private bool _managerHooked;
    private readonly HashSet<string> _trackedWaypointIdentifiers = new(StringComparer.Ordinal);

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
        _questManager = Registry.Registry.Get<QuestManager>(RegistryType.Service, Registry.Registry.TypeKey<QuestManager>());

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
      RefreshTrackedWaypoints(tracked);
    }

    private void Update()
    {
      if (_trackedWaypointIdentifiers.Count == 0)
      {
        return;
      }

      // 키 입력 검사를 먼저 수행한다. (UIOverlayStack.IsEmpty 는 내부 정리 과정에서
      // 리스트를 할당하므로 매 프레임 호출하면 프레임당 GC 할당이 발생한다.)
      if (!Input.GetKeyDown(KeyCode.Y))
      {
        return;
      }

      // 채팅 입력 등 다른 UI 오버레이가 열려 있으면 단축키를 무시한다.
      // (채팅에 'y' 를 입력할 때마다 하이라이트가 발동하는 버그 방지)
      if (!UIOverlayStack.IsEmpty())
      {
        return;
      }

      HighlightTrackedWaypoints();
    }

    private void RefreshTrackedWaypoints(IReadOnlyList<QuestData> tracked)
    {
      _trackedWaypointIdentifiers.Clear();
      if (tracked == null)
      {
        return;
      }

      foreach (var quest in tracked)
      {
        if (quest == null || string.IsNullOrWhiteSpace(quest.WaypointIdentifier))
          continue;

        _trackedWaypointIdentifiers.Add(quest.WaypointIdentifier);
      }
    }

    private void HighlightTrackedWaypoints()
    {
      foreach (var waypointId in _trackedWaypointIdentifiers)
      {
        if (WaypointAnchor.TryGet(waypointId, out var anchor))
        {
          anchor.Highlight();
        }
      }
    }
  }
}
