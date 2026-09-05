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
    public static QuestPreviewHudUIController ActiveInstance { get; private set; }
    public static bool HasActiveInstance => ActiveInstance != null && ActiveInstance.isActiveAndEnabled;

    [SerializeField] private string _rootName = DefaultsQuestControl.QuestPreviewRootName;
    [SerializeField] private float _sortingOrder = DefaultsUIDocument.QuestPreviewHudSortOrder;
    [SerializeField] private StyleSheet _styleSheet;
    [SerializeField] private QuestManager _questManager;
    [SerializeField] private ScenarioTimeValue _questCompletionDisplayDuration = ScenarioTimeValue.Seconds(1d);

    private UIDocument _uiDocument;
    private QuestPreviewHudElement _hudElement;
    private bool _managerHooked;
    private bool _skipCompletionDisplayDelay;
    private IReadOnlyList<QuestData> _trackedQuests = Array.Empty<QuestData>();
    private IReadOnlyList<QuestData> _observedTrackedQuests = Array.Empty<QuestData>();
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
      if (ActiveInstance != null && !ReferenceEquals(ActiveInstance, this))
        ActiveInstance.DetachManager();
      ActiveInstance = this;
      if (_uiDocument != null)
        AttachManager();
    }

    private void OnDisable()
    {
      bool isActiveInstance = ReferenceEquals(ActiveInstance, this);
      if (isActiveInstance)
        ActiveInstance = null;
      if (isActiveInstance)
      {
        foreach (string questId in _completionExpiryByQuestId.Keys)
          _questManager?.ExpireQuestPresentation(questId);
      }
      DetachManager();
      StopAllCoroutines();
      _completedQuests.Clear();
      _completionExpiryByQuestId.Clear();
      _trackedQuests = Array.Empty<QuestData>();
      _observedTrackedQuests = Array.Empty<QuestData>();
      _skipCompletionDisplayDelay = false;
      RefreshTrackedWaypoints(_trackedQuests);
      RefreshHud();
    }

    private void BindElement()
    {
      if (_uiDocument == null)
        _uiDocument = GetComponent<UIDocument>();

      var root = _uiDocument.rootVisualElement;
      SetDocumentRootPickingEnabled(_uiDocument, false);
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
      _questManager.OnQuestPreviewImmediateTransitionChanged += HandleImmediateTransitionChanged;
      _managerHooked = true;

      HandleTrackedChanged(_questManager.TrackedQuests);
    }

    private void DetachManager()
    {
      if (_managerHooked && _questManager != null)
      {
        _questManager.OnTrackedQuestsChanged -= HandleTrackedChanged;
        _questManager.OnQuestCompleted -= HandleQuestCompleted;
        _questManager.OnQuestPreviewImmediateTransitionChanged -= HandleImmediateTransitionChanged;
      }

      _managerHooked = false;
      _questManager = null;
    }

    private void HandleTrackedChanged(IReadOnlyList<QuestData> tracked)
    {
      tracked ??= Array.Empty<QuestData>();
      QueueCompletedObjectivesBeforeRefresh(tracked);
      _observedTrackedQuests = tracked;
      for (int i = _completedQuests.Count - 1; i >= 0; i--)
      {
        var completedQuest = _completedQuests[i];
        if (completedQuest == null)
        {
          _completedQuests.RemoveAt(i);
        }
      }

      if (_skipCompletionDisplayDelay || _completedQuests.Count == 0)
        _trackedQuests = _observedTrackedQuests;

      RefreshHud();
      RefreshTrackedWaypoints(tracked);
    }

    private void HandleQuestCompleted(QuestData quest)
    {
      if (!ReferenceEquals(ActiveInstance, this))
        return;

      if (quest == null || !quest.IsTracked || _skipCompletionDisplayDelay)
      {
        if (quest != null)
          _questManager?.ExpireQuestPresentation(quest.Id);
        return;
      }

      // 추적 목록 갱신에서 마지막 목표를 이미 완료 표시로 전환한 경우에는
      // 그 목표를 유지한다. 그래야 퀘스트 전체 완료 시에도 직전 목표가 취소선으로 보인다.
      if (!_completionExpiryByQuestId.ContainsKey(quest.Id))
        QueueCompletedPreview(quest);
    }

    private void QueueCompletedObjectivesBeforeRefresh(IReadOnlyList<QuestData> nextTracked)
    {
      if (_observedTrackedQuests == null || nextTracked == null)
        return;

      for (int i = 0; i < _observedTrackedQuests.Count; i++)
      {
        var previous = _observedTrackedQuests[i];
        if (previous == null || previous.Completed)
          continue;

        // 대기 문구 "다른 플레이어가 완료할 때까지 기다리기(n/N)" 의 숫자만 바뀐 경우는 목표 달성이
        // 아니므로 이전 문구에 취소선 연출을 하지 않는다.
        if (previous.GroupWait != null && previous.GroupWait.IsWaitingForOthers)
          continue;

        QuestData next = null;
        for (int j = 0; j < nextTracked.Count; j++)
        {
          if (nextTracked[j] != null && nextTracked[j].Id == previous.Id)
          {
            next = nextTracked[j];
            break;
          }
        }

        if (next == null)
          continue;

        string previousObjective = QuestPreviewHudElement.GetCurrentObjective(previous);
        string nextObjective = QuestPreviewHudElement.GetCurrentObjective(next);
        if (!string.IsNullOrWhiteSpace(previousObjective) && previousObjective != nextObjective)
          QueueCompletedPreview(previous);
      }
    }

    private void QueueCompletedPreview(QuestData quest)
    {
      _completedQuests.RemoveAll(each => each != null && each.Id == quest.Id);
      _completedQuests.Insert(0, quest.Clone());
      double expiresAt = Time.realtimeSinceStartupAsDouble + _questCompletionDisplayDuration.ToSeconds();
      _completionExpiryByQuestId[quest.Id] = expiresAt;
      RefreshHud();
      StartCoroutine(RemoveCompletedQuestAfterDelay(quest.Id, expiresAt));
    }

    private System.Collections.IEnumerator RemoveCompletedQuestAfterDelay(string questId, double expiresAt)
    {
      while (Time.realtimeSinceStartupAsDouble < expiresAt)
        yield return null;

      if (!_completionExpiryByQuestId.TryGetValue(questId, out double currentExpiry) || currentExpiry != expiresAt)
        yield break;

      _completionExpiryByQuestId.Remove(questId);
      _completedQuests.RemoveAll(each => each != null && each.Id == questId);
      if (ReferenceEquals(ActiveInstance, this))
        _questManager?.ExpireQuestPresentation(questId);
      if (_completedQuests.Count == 0)
        _trackedQuests = _observedTrackedQuests;
      RefreshHud();
    }

    private void HandleImmediateTransitionChanged(bool enabled)
    {
      if (!ReferenceEquals(ActiveInstance, this))
        return;

      _skipCompletionDisplayDelay = enabled;
      if (!enabled)
        return;

      StopAllCoroutines();
      foreach (string questId in _completionExpiryByQuestId.Keys)
        _questManager?.ExpireQuestPresentation(questId);
      _completedQuests.Clear();
      _completionExpiryByQuestId.Clear();
      _trackedQuests = _observedTrackedQuests;
      RefreshHud();
    }

    private void RefreshHud()
    {
      _hudElement?.SetQuests(_trackedQuests, _completedQuests);
    }

    private void Update()
    {
      SynchronizeTrackedWaypointMarkers();

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
      var nextWaypointIdentifiers = new HashSet<string>(StringComparer.Ordinal);
      if (tracked == null)
      {
        HideRemovedWaypointMarkers(nextWaypointIdentifiers);
        return;
      }

      foreach (var quest in tracked)
      {
        var waypointIdentifiers = QuestManager.GetActiveWaypointIdentifiers(quest);
        for (int i = 0; i < waypointIdentifiers.Count; i++)
          nextWaypointIdentifiers.Add(waypointIdentifiers[i]);
      }

      HideRemovedWaypointMarkers(nextWaypointIdentifiers);
      foreach (var waypointIdentifier in nextWaypointIdentifiers)
        _trackedWaypointIdentifiers.Add(waypointIdentifier);

      SynchronizeTrackedWaypointMarkers();
    }

    private void HideRemovedWaypointMarkers(HashSet<string> nextWaypointIdentifiers)
    {
      foreach (var waypointIdentifier in _trackedWaypointIdentifiers)
      {
        if (!nextWaypointIdentifiers.Contains(waypointIdentifier)
            && WaypointAnchor.TryGet(waypointIdentifier, out var anchor))
          anchor.SetQuestMarkerVisible(false);
      }

      _trackedWaypointIdentifiers.Clear();
    }

    private void SynchronizeTrackedWaypointMarkers()
    {
      foreach (var waypointIdentifier in _trackedWaypointIdentifiers)
      {
        // 시나리오 시작 직후에는 퀘스트 HUD가 waypoint 생성보다 먼저 갱신될 수 있다.
        // 매 프레임의 idempotent 호출로 늦게 등록된 anchor도 즉시 목표 마커를 받게 한다.
        if (WaypointAnchor.TryGet(waypointIdentifier, out var anchor))
          anchor.SetQuestMarkerVisible(true);
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
