using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Quest;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
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
    [SerializeField] private ScenarioTimeValue _questCompletionDisplayDuration = ScenarioTimeValue.Seconds(1d);

    private UIDocument _uiDocument;
    private QuestPreviewHudElement _hudElement;
    private bool _managerHooked;
    private IReadOnlyList<QuestData> _trackedQuests = Array.Empty<QuestData>();
    private readonly List<QuestData> _completedQuests = new();
    private readonly Dictionary<string, double> _completionExpiryByQuestId = new(StringComparer.Ordinal);
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
      if (_uiDocument != null)
        AttachManager();
    }

    private void OnDisable()
    {
      DetachManager();
      StopAllCoroutines();
      _completedQuests.Clear();
      _completionExpiryByQuestId.Clear();
      _trackedQuests = Array.Empty<QuestData>();
      RefreshTrackedWaypoints(_trackedQuests);
      RefreshHud();
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
      _questManager.OnQuestCompleted += HandleQuestCompleted;
      _managerHooked = true;

      HandleTrackedChanged(_questManager.TrackedQuests);
    }

    private void DetachManager()
    {
      if (_managerHooked && _questManager != null)
      {
        _questManager.OnTrackedQuestsChanged -= HandleTrackedChanged;
        _questManager.OnQuestCompleted -= HandleQuestCompleted;
      }

      _managerHooked = false;
      _questManager = null;
    }

    private void HandleTrackedChanged(IReadOnlyList<QuestData> tracked)
    {
      _trackedQuests = tracked ?? Array.Empty<QuestData>();
      for (int i = _completedQuests.Count - 1; i >= 0; i--)
      {
        var completedQuest = _completedQuests[i];
        if (completedQuest == null || IsNoLongerCompleted(completedQuest.Id))
        {
          if (completedQuest != null)
            _completionExpiryByQuestId.Remove(completedQuest.Id);
          _completedQuests.RemoveAt(i);
        }
      }

      RefreshHud();
      RefreshTrackedWaypoints(tracked);
    }

    private void HandleQuestCompleted(QuestData quest)
    {
      if (quest == null || !quest.IsTracked)
        return;

      _completedQuests.RemoveAll(each => each != null && each.Id == quest.Id);
      _completedQuests.Insert(0, quest.Clone());
      double expiresAt = Time.realtimeSinceStartupAsDouble + _questCompletionDisplayDuration.ToSeconds();
      _completionExpiryByQuestId[quest.Id] = expiresAt;
      RefreshHud();
      StartCoroutine(RemoveCompletedQuestAfterDelay(quest.Id, expiresAt));
    }

    private bool IsNoLongerCompleted(string questId)
    {
      return _questManager == null
          || !_questManager.TryGetQuest(questId, out var currentQuest)
          || currentQuest == null
          || !currentQuest.Completed;
    }

    private System.Collections.IEnumerator RemoveCompletedQuestAfterDelay(string questId, double expiresAt)
    {
      while (Time.realtimeSinceStartupAsDouble < expiresAt)
        yield return null;

      if (!_completionExpiryByQuestId.TryGetValue(questId, out double currentExpiry) || currentExpiry != expiresAt)
        yield break;

      _completionExpiryByQuestId.Remove(questId);
      _completedQuests.RemoveAll(each => each != null && each.Id == questId);
      RefreshHud();
    }

    private void RefreshHud()
    {
      _hudElement?.SetQuests(_trackedQuests, _completedQuests);
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
        var waypointIdentifiers = QuestManager.GetActiveWaypointIdentifiers(quest);
        for (int i = 0; i < waypointIdentifiers.Count; i++)
          _trackedWaypointIdentifiers.Add(waypointIdentifiers[i]);
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
