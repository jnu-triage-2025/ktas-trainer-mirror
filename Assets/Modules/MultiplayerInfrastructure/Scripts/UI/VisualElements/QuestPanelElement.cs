using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Quest;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// 임무(퀘스트) 저널 UI.
  ///
  /// 레이아웃:
  ///  - 배경: 화면 전체를 덮는 반투명 딤(dim)
  ///  - 헤더: 제목 + 추적 수(좌) / 필터 탭(중앙) / × 닫기(우)
  ///  - 좌측 목록: 분류(추적 중 · 진행 중 · 완료)별로 묶인 임무 항목.
  ///    항목마다 마름모 마커 + 제목 + 현재 목표 요약 + 상태 배지를 표시하고,
  ///    선택한 항목은 강조 테두리로 구분한다.
  ///  - 우측 상세: 선택한 임무의 제목 → 현재 목표 → 설명 → 세부 목표 →
  ///    정보 타일(진행도·세부 목표·상태·범위) → 고지 배너 → 추적 토글 버튼.
  ///
  /// 스타일은 QuestPanelUI.uss 에 정의한다(팔레트는 ItemSubmissionUI / InventoryUI 와 동일).
  /// </summary>
  [UxmlElement]
  public partial class QuestPanelElement : VisualElement
  {
    /// <summary>목록 상단 필터 탭.</summary>
    private enum QuestListFilter
    {
      All,
      InProgress,
      Completed
    }

    private readonly List<QuestData> _quests = new();
    private readonly Dictionary<string, VisualElement> _entryElements = new();
    private readonly Dictionary<QuestListFilter, Button> _tabButtons = new();
    private readonly HashSet<string> _trackedIds = new();

    private bool _hasTrackedSnapshot;
    private bool _isOpen;
    private string _selectedQuestId;
    private string _renderedDetailQuestId;
    private QuestListFilter _filter = QuestListFilter.All;

    private VisualElement _journal;
    private Label _countLabel;
    private ScrollView _questList;
    private Label _listEmptyLabel;

    private VisualElement _detailPane;
    private VisualElement _detailContent;
    private VisualElement _detailEmpty;
    private Label _detailTitle;
    private VisualElement _detailMeta;
    private VisualElement _detailObjective;
    private Label _detailObjectiveText;
    private ScrollView _detailScroll;
    private Label _detailDescription;
    private VisualElement _detailTaskList;
    private Label _detailTaskSectionLabel;
    private Label _detailParticipantSectionLabel;
    private VisualElement _detailParticipantList;
    private VisualElement _detailInfoTiles;
    private VisualElement _detailNotice;
    private Label _detailNoticeText;
    private Button _trackButton;

    public event Action<string, bool> OnTrackToggled;
    public event Action OnCloseRequested;

    public bool IsOpen => _isOpen;

    public QuestPanelElement()
    {
      name = DefaultsQuestControl.QuestRootName;
      AddToClassList("quest-root");
      AddToClassList("collapsed");
      ApplyRootFallbackStyles();
      BuildLayout();
    }

    /// <summary>
    /// 스타일시트가 적용되지 않은 경로(문서에 요소가 없어 컨트롤러가 직접 생성하는 경우)에서도
    /// 루트가 화면 전체를 덮는 모달로 동작하도록 최소한의 값을 인라인으로 지정한다.
    /// QuestPanelUI.uss 가 적용되면 동일한 값을 덮어쓰므로 표시 결과는 달라지지 않는다.
    /// </summary>
    private void ApplyRootFallbackStyles()
    {
      style.position = Position.Absolute;
      style.left = 0;
      style.top = 0;
      style.right = 0;
      style.bottom = 0;
      style.alignItems = Align.Center;
      style.justifyContent = Justify.Center;
      style.backgroundColor = new Color(0f, 0f, 0f, 0.55f);
      style.display = DisplayStyle.None;
    }

    public void SetOpen(bool open)
    {
      if (_isOpen == open)
        return;

      _isOpen = open;
      if (open)
      {
        RemoveFromClassList("collapsed");
        AddToClassList("expanded");
        style.display = DisplayStyle.Flex;
        // 열릴 때마다 선택 상태를 현재 목록에 맞춰 정리한다(완료·삭제된 임무 선택 방지).
        EnsureSelection();
        RebuildList();
        RebuildDetail();
      }
      else
      {
        RemoveFromClassList("expanded");
        AddToClassList("collapsed");
        style.display = DisplayStyle.None;
      }
    }

    public void SetQuests(IReadOnlyList<QuestData> quests)
    {
      _quests.Clear();
      if (quests != null)
      {
        for (int i = 0; i < quests.Count; i++)
        {
          var quest = quests[i];
          if (quest != null && !string.IsNullOrWhiteSpace(quest.Id))
            _quests.Add(quest);
        }
      }

      EnsureSelection();
      RebuildList();
      RebuildDetail();
      UpdateCountLabel();
    }

    public void UpdateTracked(IReadOnlyList<QuestData> trackedQuests)
    {
      _trackedIds.Clear();
      _hasTrackedSnapshot = true;

      if (trackedQuests != null)
      {
        for (int i = 0; i < trackedQuests.Count; i++)
        {
          var quest = trackedQuests[i];
          if (quest != null && !string.IsNullOrWhiteSpace(quest.Id))
            _trackedIds.Add(quest.Id);
        }
      }

      RebuildList();
      RebuildDetail();
      UpdateCountLabel();
    }

    // ── 레이아웃 구성 ────────────────────────────────────────────────────

    private void BuildLayout()
    {
      Clear();

      _journal = new VisualElement { name = "QuestJournal" };
      _journal.AddToClassList("quest-journal");
      Add(_journal);

      BuildHeader();

      var body = new VisualElement { name = "QuestJournalBody" };
      body.AddToClassList("quest-journal__body");
      _journal.Add(body);

      BuildListPane(body);
      BuildDetailPane(body);
    }

    private void BuildHeader()
    {
      var header = new VisualElement { name = "QuestJournalHeader" };
      header.AddToClassList("quest-journal__header");
      _journal.Add(header);

      var titleGroup = new VisualElement { pickingMode = PickingMode.Ignore };
      titleGroup.AddToClassList("quest-journal__title-group");
      header.Add(titleGroup);

      var title = new Label("임무") { pickingMode = PickingMode.Ignore };
      title.AddToClassList("quest-journal__title");
      titleGroup.Add(title);

      _countLabel = new Label { pickingMode = PickingMode.Ignore };
      _countLabel.AddToClassList("quest-journal__count");
      titleGroup.Add(_countLabel);

      var tabs = new VisualElement { name = "QuestJournalTabs" };
      tabs.AddToClassList("quest-journal__tabs");
      header.Add(tabs);

      AddTabButton(tabs, QuestListFilter.All, "전체");
      AddTabButton(tabs, QuestListFilter.InProgress, "진행 중");
      AddTabButton(tabs, QuestListFilter.Completed, "완료");

      var closeX = new Button(() => OnCloseRequested?.Invoke()) { name = "QuestJournalCloseX", text = "×" };
      closeX.AddToClassList("quest-journal__close-x");
      header.Add(closeX);

      UpdateTabStates();
      UpdateCountLabel();
    }

    private void AddTabButton(VisualElement parent, QuestListFilter filter, string label)
    {
      var button = new Button(() => SetFilter(filter)) { text = label };
      button.AddToClassList("quest-tab");
      parent.Add(button);
      _tabButtons[filter] = button;
    }

    private void BuildListPane(VisualElement parent)
    {
      var pane = new VisualElement { name = "QuestListPane" };
      pane.AddToClassList("quest-list-pane");
      parent.Add(pane);

      _questList = new ScrollView(ScrollViewMode.Vertical)
      {
        name = DefaultsQuestControl.QuestListName,
        verticalScrollerVisibility = ScrollerVisibility.Auto,
        horizontalScrollerVisibility = ScrollerVisibility.Hidden
      };
      _questList.AddToClassList("quest-list");
      pane.Add(_questList);

      _listEmptyLabel = new Label("표시할 임무가 없습니다.") { pickingMode = PickingMode.Ignore };
      _listEmptyLabel.AddToClassList("quest-list__empty");
      _listEmptyLabel.style.display = DisplayStyle.None;
      pane.Add(_listEmptyLabel);
    }

    private void BuildDetailPane(VisualElement parent)
    {
      _detailPane = new VisualElement { name = "QuestDetailPane" };
      _detailPane.AddToClassList("quest-detail-pane");
      parent.Add(_detailPane);

      // 선택된 임무가 없을 때 표시하는 안내.
      _detailEmpty = new VisualElement { pickingMode = PickingMode.Ignore };
      _detailEmpty.AddToClassList("quest-detail__empty");
      var emptyText = new Label("좌측 목록에서 임무를 선택하면\n자세한 내용을 확인할 수 있습니다.")
      {
        pickingMode = PickingMode.Ignore
      };
      emptyText.AddToClassList("quest-detail__empty-text");
      _detailEmpty.Add(emptyText);
      _detailPane.Add(_detailEmpty);

      _detailContent = new VisualElement { name = "QuestDetailContent" };
      _detailContent.style.flexGrow = 1;
      _detailContent.style.minHeight = 0;
      _detailPane.Add(_detailContent);

      var detailHeader = new VisualElement { pickingMode = PickingMode.Ignore };
      detailHeader.AddToClassList("quest-detail__header");
      _detailContent.Add(detailHeader);

      _detailTitle = new Label { pickingMode = PickingMode.Ignore };
      _detailTitle.AddToClassList("quest-detail__title");
      detailHeader.Add(_detailTitle);

      _detailMeta = new VisualElement { pickingMode = PickingMode.Ignore };
      _detailMeta.AddToClassList("quest-detail__meta");
      detailHeader.Add(_detailMeta);

      // 현재 목표 줄.
      _detailObjective = new VisualElement { pickingMode = PickingMode.Ignore };
      _detailObjective.AddToClassList("quest-detail__objective");
      _detailContent.Add(_detailObjective);

      var objectiveMarker = new VisualElement { pickingMode = PickingMode.Ignore };
      objectiveMarker.AddToClassList("quest-detail__objective-marker");
      _detailObjective.Add(objectiveMarker);

      _detailObjectiveText = new Label { pickingMode = PickingMode.Ignore };
      _detailObjectiveText.AddToClassList("quest-detail__objective-text");
      _detailObjective.Add(_detailObjectiveText);

      // 설명 · 세부 목표 · 정보 타일을 담는 스크롤 영역.
      _detailScroll = new ScrollView(ScrollViewMode.Vertical)
      {
        name = "QuestDetailScroll",
        verticalScrollerVisibility = ScrollerVisibility.Auto,
        horizontalScrollerVisibility = ScrollerVisibility.Hidden
      };
      _detailScroll.AddToClassList("quest-detail__scroll");
      _detailContent.Add(_detailScroll);

      _detailDescription = new Label { pickingMode = PickingMode.Ignore };
      _detailDescription.AddToClassList("quest-detail__description");
      _detailScroll.Add(_detailDescription);

      _detailTaskSectionLabel = new Label("세부 목표") { pickingMode = PickingMode.Ignore };
      _detailTaskSectionLabel.AddToClassList("quest-detail__section-label");
      _detailScroll.Add(_detailTaskSectionLabel);

      _detailTaskList = new VisualElement { pickingMode = PickingMode.Ignore };
      _detailScroll.Add(_detailTaskList);

      // 함께 완료해야 넘어가는 참여자 목록. 공동 진행 게이트에 속한 임무에서만 표시한다.
      _detailParticipantSectionLabel = new Label("함께 완료해야 하는 참여자") { pickingMode = PickingMode.Ignore };
      _detailParticipantSectionLabel.AddToClassList("quest-detail__section-label");
      _detailScroll.Add(_detailParticipantSectionLabel);

      _detailParticipantList = new VisualElement { pickingMode = PickingMode.Ignore };
      _detailScroll.Add(_detailParticipantList);

      var infoSectionLabel = new Label("임무 정보") { pickingMode = PickingMode.Ignore };
      infoSectionLabel.AddToClassList("quest-detail__section-label");
      _detailScroll.Add(infoSectionLabel);

      _detailInfoTiles = new VisualElement { pickingMode = PickingMode.Ignore };
      _detailInfoTiles.AddToClassList("quest-info-tiles");
      _detailScroll.Add(_detailInfoTiles);

      // 고지 배너.
      _detailNotice = new VisualElement { pickingMode = PickingMode.Ignore };
      _detailNotice.AddToClassList("quest-detail__notice");
      _detailContent.Add(_detailNotice);

      var noticeMarker = new VisualElement { pickingMode = PickingMode.Ignore };
      noticeMarker.AddToClassList("quest-detail__notice-marker");
      _detailNotice.Add(noticeMarker);

      _detailNoticeText = new Label { pickingMode = PickingMode.Ignore };
      _detailNoticeText.AddToClassList("quest-detail__notice-text");
      _detailNotice.Add(_detailNoticeText);

      // 푸터: 추적 토글 + 조작 안내.
      var footer = new VisualElement();
      footer.AddToClassList("quest-detail__footer");
      _detailContent.Add(footer);

      _trackButton = new Button(HandleTrackClicked) { name = "QuestTrackButton", text = "추적 고정" };
      _trackButton.AddToClassList("quest-track-button");
      footer.Add(_trackButton);

      var hint = new Label($"[{DefaultsKeyConfiguration.OpenQuestUI}] 또는 [Esc] 키로 닫습니다")
      {
        pickingMode = PickingMode.Ignore
      };
      hint.AddToClassList("quest-detail__hint");
      footer.Add(hint);
    }

    // ── 목록 ────────────────────────────────────────────────────────────

    private void SetFilter(QuestListFilter filter)
    {
      if (_filter == filter)
        return;

      _filter = filter;
      UpdateTabStates();
      RebuildList();
    }

    private void UpdateTabStates()
    {
      foreach (var kvp in _tabButtons)
      {
        if (kvp.Key == _filter)
          kvp.Value.AddToClassList("quest-tab--active");
        else
          kvp.Value.RemoveFromClassList("quest-tab--active");
      }
    }

    private void RebuildList()
    {
      if (_questList == null)
        return;

      // 목록을 다시 만드는 동안 스크롤 위치가 초기화되지 않도록 보존한다.
      var scrollOffset = _questList.scrollOffset;

      _questList.Clear();
      _entryElements.Clear();

      int rendered = 0;
      if (_filter != QuestListFilter.Completed)
      {
        rendered += AddGroup("추적 중", quest => !quest.Completed && IsTracked(quest), false);
        rendered += AddGroup("진행 중", quest => !quest.Completed && !IsTracked(quest), false);
      }

      if (_filter != QuestListFilter.InProgress)
        rendered += AddGroup("완료", quest => quest.Completed, true);

      if (_listEmptyLabel != null)
        _listEmptyLabel.style.display = rendered > 0 ? DisplayStyle.None : DisplayStyle.Flex;

      _questList.scrollOffset = scrollOffset;
    }

    private int AddGroup(string label, Func<QuestData, bool> predicate, bool completedGroup)
    {
      var matched = new List<QuestData>();
      for (int i = 0; i < _quests.Count; i++)
      {
        if (predicate(_quests[i]))
          matched.Add(_quests[i]);
      }

      if (matched.Count == 0)
        return 0;

      var group = new VisualElement { pickingMode = PickingMode.Ignore };
      group.AddToClassList("quest-group");
      if (completedGroup)
        group.AddToClassList("quest-group--completed");
      _questList.Add(group);

      var header = new VisualElement { pickingMode = PickingMode.Ignore };
      header.AddToClassList("quest-group__header");
      group.Add(header);

      var marker = new VisualElement { pickingMode = PickingMode.Ignore };
      marker.AddToClassList("quest-group__marker");
      header.Add(marker);

      var labelElement = new Label(label) { pickingMode = PickingMode.Ignore };
      labelElement.AddToClassList("quest-group__label");
      header.Add(labelElement);

      var countElement = new Label($"{matched.Count}") { pickingMode = PickingMode.Ignore };
      countElement.AddToClassList("quest-group__count");
      header.Add(countElement);

      for (int i = 0; i < matched.Count; i++)
        group.Add(CreateEntry(matched[i]));

      return matched.Count;
    }

    private VisualElement CreateEntry(QuestData quest)
    {
      bool tracked = IsTracked(quest);

      var entry = new VisualElement();
      entry.AddToClassList("quest-entry");
      if (quest.Completed)
        entry.AddToClassList("quest-entry--completed");
      if (quest.Id == _selectedQuestId)
        entry.AddToClassList("quest-entry--selected");

      var marker = new VisualElement { pickingMode = PickingMode.Ignore };
      marker.AddToClassList("quest-entry__marker");
      if (quest.Completed)
        marker.AddToClassList("quest-entry__marker--completed");
      else if (tracked)
        marker.AddToClassList("quest-entry__marker--tracked");
      entry.Add(marker);

      var text = new VisualElement { pickingMode = PickingMode.Ignore };
      text.AddToClassList("quest-entry__text");
      entry.Add(text);

      var title = new Label(GetTitle(quest)) { pickingMode = PickingMode.Ignore };
      title.AddToClassList("quest-entry__title");
      text.Add(title);

      var objective = new Label(GetEntrySubText(quest)) { pickingMode = PickingMode.Ignore };
      objective.AddToClassList("quest-entry__objective");
      if (quest.Completed)
        objective.AddToClassList("quest-entry__objective--completed");
      else if (tracked)
        objective.AddToClassList("quest-entry__objective--tracked");
      text.Add(objective);

      var badge = CreateEntryBadge(quest, tracked);
      if (badge != null)
        entry.Add(badge);

      entry.RegisterCallback<ClickEvent>(_ => SelectQuest(quest.Id));
      _entryElements[quest.Id] = entry;
      return entry;
    }

    private static Label CreateEntryBadge(QuestData quest, bool tracked)
    {
      string text;
      string modifier;

      if (quest.Completed)
      {
        text = "완료";
        modifier = "quest-entry__badge--completed";
      }
      else if (tracked)
      {
        text = "추적 중";
        modifier = null;
      }
      else if (!quest.IsTrackable)
      {
        text = "고정 불가";
        modifier = "quest-entry__badge--locked";
      }
      else
      {
        return null;
      }

      var badge = new Label(text) { pickingMode = PickingMode.Ignore };
      badge.AddToClassList("quest-entry__badge");
      if (!string.IsNullOrEmpty(modifier))
        badge.AddToClassList(modifier);
      return badge;
    }

    private void SelectQuest(string questId)
    {
      if (string.IsNullOrWhiteSpace(questId) || _selectedQuestId == questId)
        return;

      if (!string.IsNullOrEmpty(_selectedQuestId) && _entryElements.TryGetValue(_selectedQuestId, out var previous))
        previous.RemoveFromClassList("quest-entry--selected");

      _selectedQuestId = questId;

      if (_entryElements.TryGetValue(questId, out var current))
        current.AddToClassList("quest-entry--selected");

      RebuildDetail();
    }

    /// <summary>선택된 임무가 없거나 목록에서 사라졌으면 표시 우선순위에 따라 다시 고른다.</summary>
    private void EnsureSelection()
    {
      if (!string.IsNullOrEmpty(_selectedQuestId) && FindQuest(_selectedQuestId) != null)
        return;

      _selectedQuestId = null;

      // 추적 중인 미완료 임무 → 미완료 임무 → 아무 임무 순으로 선택한다.
      for (int i = 0; i < _quests.Count; i++)
      {
        if (!_quests[i].Completed && IsTracked(_quests[i]))
        {
          _selectedQuestId = _quests[i].Id;
          return;
        }
      }

      for (int i = 0; i < _quests.Count; i++)
      {
        if (!_quests[i].Completed)
        {
          _selectedQuestId = _quests[i].Id;
          return;
        }
      }

      if (_quests.Count > 0)
        _selectedQuestId = _quests[0].Id;
    }

    // ── 상세 ────────────────────────────────────────────────────────────

    private void RebuildDetail()
    {
      if (_detailContent == null)
        return;

      var quest = FindQuest(_selectedQuestId);
      bool hasQuest = quest != null;

      _detailEmpty.style.display = hasQuest ? DisplayStyle.None : DisplayStyle.Flex;
      _detailContent.style.display = hasQuest ? DisplayStyle.Flex : DisplayStyle.None;

      if (!hasQuest)
        return;

      bool tracked = IsTracked(quest);

      _detailTitle.text = GetTitle(quest);
      BuildDetailMeta(quest);

      bool waitingForOthers = IsWaitingForOthers(quest);
      string objective = QuestPreviewHudElement.GetCurrentObjective(quest);
      _detailObjectiveText.text = waitingForOthers
        ? quest.GroupWait.WaitingDisplayText
        : quest.Completed
          ? "모든 목표를 달성했습니다."
          : string.IsNullOrWhiteSpace(objective) ? "진행 중" : objective;
      _detailObjective.EnableInClassList("quest-detail__objective--completed", quest.Completed && !waitingForOthers);

      string description = quest.Description?.Trim();
      _detailDescription.text = string.IsNullOrWhiteSpace(description) ? "등록된 설명이 없습니다." : description;

      BuildTaskRows(quest);
      BuildParticipantRows(quest);
      BuildInfoTiles(quest);
      UpdateNotice(quest, tracked);
      UpdateTrackButton(quest, tracked);

      // 진행도 갱신으로 다시 그릴 때에는 읽던 위치를 유지하고, 다른 임무로 바뀔 때에만 맨 위로 올린다.
      if (_renderedDetailQuestId != quest.Id)
      {
        _detailScroll.scrollOffset = Vector2.zero;
        _renderedDetailQuestId = quest.Id;
      }
    }

    private void BuildDetailMeta(QuestData quest)
    {
      _detailMeta.Clear();

      if (!quest.IsGroupWaitPlaceholder)
        AddMetaItem(FormatProgress(quest) + " 달성");

      if (quest.IsOrdinal)
        AddMetaItem("순차 진행");

      if (!string.IsNullOrWhiteSpace(quest.WaypointIdentifier))
        AddMetaItem($"위치: {quest.WaypointIdentifier.Trim()}");
    }

    private void AddMetaItem(string text)
    {
      if (_detailMeta.childCount > 0)
      {
        var separator = new Label("·") { pickingMode = PickingMode.Ignore };
        separator.AddToClassList("quest-detail__meta-item");
        _detailMeta.Add(separator);
      }

      var item = new Label(text) { pickingMode = PickingMode.Ignore };
      item.AddToClassList("quest-detail__meta-item");
      _detailMeta.Add(item);
    }

    private void BuildTaskRows(QuestData quest)
    {
      _detailTaskList.Clear();

      var tasks = QuestManager.GetQuestTasks(quest);
      if (tasks == null || tasks.Count == 0)
      {
        _detailTaskSectionLabel.style.display = DisplayStyle.None;
        _detailTaskList.style.display = DisplayStyle.None;
        return;
      }

      _detailTaskSectionLabel.style.display = DisplayStyle.Flex;
      _detailTaskList.style.display = DisplayStyle.Flex;

      // 순차 진행 임무는 아직 도달하지 않은 목표를 감춘다(HUD 표시 규칙과 동일).
      int visibleCount = quest.IsOrdinal ? GetOrdinalVisibleTaskCount(tasks) : tasks.Count;
      int currentIndex = FindFirstIncompleteIndex(tasks);
      for (int i = 0; i < visibleCount; i++)
      {
        var task = tasks[i];
        if (task == null)
          continue;

        AddTaskRow(task, 0, i == currentIndex);
      }

      if (visibleCount < tasks.Count)
      {
        var hidden = new Label("이후 목표는 진행하면 공개됩니다.") { pickingMode = PickingMode.Ignore };
        hidden.AddToClassList("quest-task__text");
        hidden.AddToClassList("quest-task__text--done");
        hidden.style.marginTop = 4;
        _detailTaskList.Add(hidden);
      }
    }

    /// <summary>
    /// 공동 진행 게이트의 참여자를 한 줄씩 나열한다. 자기 몫을 끝낸 참여자는 취소선과 "완료함" 으로,
    /// 이탈한 참여자는 취소선과 "이탈함" 으로 표시하고, 아직 진행 중인 참여자는 이름만 보여 준다.
    /// </summary>
    private void BuildParticipantRows(QuestData quest)
    {
      _detailParticipantList.Clear();

      var status = quest.GroupWait;
      if (status == null || status.Participants == null || status.Participants.Count == 0)
      {
        _detailParticipantSectionLabel.style.display = DisplayStyle.None;
        _detailParticipantList.style.display = DisplayStyle.None;
        return;
      }

      _detailParticipantSectionLabel.style.display = DisplayStyle.Flex;
      _detailParticipantList.style.display = DisplayStyle.Flex;
      _detailParticipantSectionLabel.text = $"함께 완료해야 하는 참여자 ({status.CompletedCount}/{status.TotalCount})";

      for (int i = 0; i < status.Participants.Count; i++)
      {
        var participant = status.Participants[i];
        if (participant == null)
          continue;

        var row = new VisualElement { pickingMode = PickingMode.Ignore };
        row.AddToClassList("quest-task");
        row.AddToClassList("quest-participant");
        _detailParticipantList.Add(row);

        var check = new VisualElement { pickingMode = PickingMode.Ignore };
        check.AddToClassList("quest-task__check");
        if (participant.CountsAsCompleted)
          check.AddToClassList("quest-task__check--done");
        row.Add(check);

        // 완료한 참여자는 세부 목표와 같은 TextCore 취소선 태그로 표시한다.
        var label = new Label(FormatParticipantRow(participant)) { pickingMode = PickingMode.Ignore };
        label.enableRichText = true;
        label.AddToClassList("quest-task__text");
        if (participant.CountsAsCompleted)
          label.AddToClassList("quest-task__text--done");
        row.Add(label);
      }
    }

    /// <summary>예: "<s>플레이어 a</s> 완료함", "플레이어 b", "<s>플레이어 c (나)</s> 완료함".</summary>
    public static string FormatParticipantRow(QuestGroupWaitParticipant participant)
    {
      string name = FormatParticipantName(participant);
      if (participant.Left)
        return $"<s>{name}</s> 이탈함";
      if (participant.Completed)
        return $"<s>{name}</s> 완료함";
      return name;
    }

    private static string FormatParticipantName(QuestGroupWaitParticipant participant)
    {
      string name = !string.IsNullOrWhiteSpace(participant.DisplayName)
        ? participant.DisplayName.Trim()
        : !string.IsNullOrWhiteSpace(participant.Role)
          ? participant.Role.Trim()
          : $"플레이어 {participant.ClientId}";
      return participant.IsLocal ? $"{name} (나)" : name;
    }

    private static bool IsWaitingForOthers(QuestData quest)
      => quest?.GroupWait != null && quest.GroupWait.IsWaitingForOthers;

    private void AddTaskRow(QuestCompletionCriteria criterion, int depth, bool isCurrent)
    {
      string text = FormatCriterionText(criterion);
      if (!string.IsNullOrWhiteSpace(text))
      {
        var row = new VisualElement { pickingMode = PickingMode.Ignore };
        row.AddToClassList("quest-task");
        if (depth > 0)
          row.AddToClassList("quest-task--child");
        _detailTaskList.Add(row);

        var check = new VisualElement { pickingMode = PickingMode.Ignore };
        check.AddToClassList("quest-task__check");
        if (criterion.Completed)
          check.AddToClassList("quest-task__check--done");
        else if (isCurrent)
          check.AddToClassList("quest-task__check--current");
        row.Add(check);

        // 완료한 목표는 TextCore 취소선 태그로 표시한다(QuestPreviewHudElement 와 동일한 방식).
        var label = new Label(criterion.Completed ? $"<s>{text}</s>" : text) { pickingMode = PickingMode.Ignore };
        label.enableRichText = true;
        label.AddToClassList("quest-task__text");
        if (criterion.Completed)
          label.AddToClassList("quest-task__text--done");
        else if (isCurrent)
          label.AddToClassList("quest-task__text--current");
        row.Add(label);

        string progress = FormatCriterionProgress(criterion);
        if (!string.IsNullOrEmpty(progress))
        {
          var progressLabel = new Label(progress) { pickingMode = PickingMode.Ignore };
          progressLabel.AddToClassList("quest-task__progress");
          row.Add(progressLabel);
        }
      }

      if (criterion.Conditions == null)
        return;

      for (int i = 0; i < criterion.Conditions.Count; i++)
      {
        var child = criterion.Conditions[i];
        if (child != null)
          AddTaskRow(child, depth + 1, isCurrent && !child.Completed);
      }
    }

    private void BuildInfoTiles(QuestData quest)
    {
      _detailInfoTiles.Clear();

      if (!quest.IsGroupWaitPlaceholder)
        AddInfoTile(FormatProgress(quest), "진행도");

      var tasks = QuestManager.GetQuestTasks(quest);
      int total = tasks?.Count ?? 0;
      int completed = 0;
      for (int i = 0; i < total; i++)
      {
        if (tasks[i] != null && tasks[i].Completed)
          completed++;
      }

      if (total > 0)
        AddInfoTile($"{completed}/{total}", "세부 목표");

      AddInfoTile(IsWaitingForOthers(quest) ? "대기 중" : quest.Completed ? "완료" : "진행 중", "상태");
      AddInfoTile(quest.Scope == QuestScopeType.Global ? "공용" : "개인", "범위");
      if (quest.GroupWait != null)
        AddInfoTile($"{quest.GroupWait.CompletedCount}/{quest.GroupWait.TotalCount}", "참여자 완료");
    }

    private void AddInfoTile(string value, string caption)
    {
      var tile = new VisualElement { pickingMode = PickingMode.Ignore };
      tile.AddToClassList("quest-info-tile");

      var valueLabel = new Label(value) { pickingMode = PickingMode.Ignore };
      valueLabel.AddToClassList("quest-info-tile__value");
      tile.Add(valueLabel);

      var captionLabel = new Label(caption) { pickingMode = PickingMode.Ignore };
      captionLabel.AddToClassList("quest-info-tile__caption");
      tile.Add(captionLabel);

      _detailInfoTiles.Add(tile);
    }

    private void UpdateNotice(QuestData quest, bool tracked)
    {
      string message = null;
      bool completedStyle = false;

      if (IsWaitingForOthers(quest))
      {
        message = "내 목표는 마쳤습니다. 함께 진행하는 참여자가 모두 완료하면 다음 단계로 넘어갑니다.";
        completedStyle = true;
      }
      else if (quest.Completed)
      {
        message = "완료한 임무입니다. 진행 내역만 확인할 수 있습니다.";
        completedStyle = true;
      }
      else if (!quest.IsTrackable)
      {
        message = "이 임무는 추적 고정을 지원하지 않습니다.";
      }
      else if (!tracked && CountTracked() >= DefaultsQuestControl.MaxTrackedQuests)
      {
        message = $"임무는 최대 {DefaultsQuestControl.MaxTrackedQuests}개까지 추적할 수 있습니다. 다른 임무의 추적을 먼저 해제하세요.";
      }

      if (string.IsNullOrEmpty(message))
      {
        _detailNotice.style.display = DisplayStyle.None;
        return;
      }

      _detailNotice.style.display = DisplayStyle.Flex;
      _detailNotice.EnableInClassList("quest-detail__notice--completed", completedStyle);
      _detailNoticeText.text = message;
    }

    private void UpdateTrackButton(QuestData quest, bool tracked)
    {
      bool trackable = quest.IsTrackable && !quest.Completed;
      _trackButton.SetEnabled(trackable);
      _trackButton.EnableInClassList("quest-track-button--tracked", tracked && trackable);

      if (quest.Completed)
        _trackButton.text = "완료됨";
      else if (!quest.IsTrackable)
        _trackButton.text = "고정 불가";
      else
        _trackButton.text = tracked ? "추적 해제" : "추적 고정";
    }

    private void HandleTrackClicked()
    {
      var quest = FindQuest(_selectedQuestId);
      if (quest == null || !quest.IsTrackable || quest.Completed)
        return;

      OnTrackToggled?.Invoke(quest.Id, !IsTracked(quest));
    }

    // ── 보조 ────────────────────────────────────────────────────────────

    private void UpdateCountLabel()
    {
      if (_countLabel == null)
        return;

      int count = CountTracked();
      _countLabel.text = $"추적 {count}/{DefaultsQuestControl.MaxTrackedQuests}";
      _countLabel.EnableInClassList("quest-journal__count--over", count > DefaultsQuestControl.MaxTrackedQuests);
    }

    private int CountTracked()
    {
      int count = 0;
      for (int i = 0; i < _quests.Count; i++)
      {
        if (!_quests[i].Completed && IsTracked(_quests[i]))
          count++;
      }
      return count;
    }

    private bool IsTracked(QuestData quest)
    {
      if (quest == null)
        return false;

      // UpdateTracked 로 전달된 스냅샷이 있으면 그것을 우선한다.
      return _hasTrackedSnapshot ? _trackedIds.Contains(quest.Id) : quest.IsTracked;
    }

    private QuestData FindQuest(string questId)
    {
      if (string.IsNullOrWhiteSpace(questId))
        return null;

      for (int i = 0; i < _quests.Count; i++)
      {
        if (_quests[i].Id == questId)
          return _quests[i];
      }

      return null;
    }

    private string GetEntrySubText(QuestData quest)
    {
      if (IsWaitingForOthers(quest))
        return quest.GroupWait.WaitingDisplayText;

      if (quest.Completed)
        return "완료";

      string objective = QuestPreviewHudElement.GetCurrentObjective(quest);
      return string.IsNullOrWhiteSpace(objective) ? FormatProgress(quest) : objective;
    }

    private static string GetTitle(QuestData quest)
    {
      return string.IsNullOrWhiteSpace(quest.Title) ? "이름 없는 임무" : quest.Title.Trim();
    }

    private static string FormatProgress(QuestData quest)
    {
      return quest?.Progress?.ToDisplayText() ?? "0/1";
    }

    /// <summary>세부 목표 한 줄의 본문 텍스트. 진행도는 별도 라벨로 표시하므로 포함하지 않는다.</summary>
    private static string FormatCriterionText(QuestCompletionCriteria criterion)
    {
      if (!string.IsNullOrWhiteSpace(criterion.DisplayTextContent))
        return criterion.DisplayTextContent.Trim();

      return criterion.Type switch
      {
        QuestCompletionCriteriaType.InventoryContains when !string.IsNullOrWhiteSpace(criterion.ItemId) => criterion.ItemId,
        QuestCompletionCriteriaType.InteractionSignalReceived when !string.IsNullOrWhiteSpace(criterion.SignalId) => criterion.SignalId,
        QuestCompletionCriteriaType.WaypointReached when !string.IsNullOrWhiteSpace(criterion.WaypointIdentifier) => $"위치 이동: {criterion.WaypointIdentifier}",
        QuestCompletionCriteriaType.AllOf => "모든 조건 달성",
        QuestCompletionCriteriaType.AnyOf => "조건 중 하나 달성",
        _ => string.Empty
      };
    }

    /// <summary>목표가 여러 번 반복되는 경우에만 진행도를 표시한다.</summary>
    private static string FormatCriterionProgress(QuestCompletionCriteria criterion)
    {
      int target = criterion.Progress?.Target ?? criterion.Count;
      if (target <= 1)
        return string.Empty;

      return criterion.Progress?.ToDisplayText() ?? $"0/{target}";
    }

    /// <summary>아직 달성하지 않은 첫 목표의 위치. 없으면 -1.</summary>
    private static int FindFirstIncompleteIndex(IReadOnlyList<QuestCompletionCriteria> tasks)
    {
      for (int i = 0; i < tasks.Count; i++)
      {
        if (tasks[i] != null && !tasks[i].Completed)
          return i;
      }

      return -1;
    }

    private static int GetOrdinalVisibleTaskCount(IReadOnlyList<QuestCompletionCriteria> tasks)
    {
      for (int i = 0; i < tasks.Count; i++)
      {
        if (tasks[i] != null && !tasks[i].Completed)
          return i + 1;
      }

      return tasks.Count;
    }
  }
}
