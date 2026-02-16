using System;
using System.Collections.Generic;
using System.Linq;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

using MI = MultiplayerInfrastructure;

namespace MultiplayerInfrastructure.Quest
{
  /// <summary>
  /// Manages quest lifecycle and tracked selections shared by UI controllers.
  /// </summary>
  public class QuestManager : MonoBehaviour
  {
    [SerializeField] private int _maxTracked = DefaultsQuestControl.MaxTrackedQuests;

    private readonly Dictionary<string, QuestData> _quests = new();
    private readonly List<string> _trackedQuestOrder = new();

    public event Action<IReadOnlyList<QuestData>> OnQuestListChanged;
    public event Action<IReadOnlyList<QuestData>> OnTrackedQuestsChanged;

    public IReadOnlyList<QuestData> Quests => Snapshot(_quests.Values);
    public IReadOnlyList<QuestData> TrackedQuests => Snapshot(_trackedQuestOrder.Select(id => _quests.TryGetValue(id, out var quest) ? quest : null));

    public bool HasQuest(string questId)
    {
      if (string.IsNullOrWhiteSpace(questId))
        return false;

      return _quests.ContainsKey(questId);
    }

    public bool TryGetQuest(string questId, out QuestData quest)
    {
      quest = null;
      if (string.IsNullOrWhiteSpace(questId))
        return false;

      if (_quests.TryGetValue(questId, out var existing))
      {
        quest = existing.Clone();
        return true;
      }

      return false;
    }

    private void Awake()
    {
      Registry.Registry.Register(RegistryType.Entity, Registry.Registry.TypeKey<QuestManager>(), this);
    }

    private void OnDestroy()
    {
      Registry.Registry.Unregister(RegistryType.Entity, Registry.Registry.TypeKey<QuestManager>());
    }

    public void SetQuests(IEnumerable<QuestData> quests, bool clearExisting = true)
    {
      if (quests == null) return;

      if (clearExisting)
      {
        _quests.Clear();
        _trackedQuestOrder.Clear();
      }

      foreach (var quest in quests)
      {
        AddOrUpdateQuest(quest, notify: false);
      }

      ClampTrackedToLimit();
      NotifyListChanged();
      NotifyTrackedChanged();
    }

    public void AddOrUpdateQuest(QuestData quest, bool notify = true)
    {
      if (quest == null || string.IsNullOrWhiteSpace(quest.Id))
        return;

      var cloned = quest.Clone();
      _quests[cloned.Id] = cloned;

      if (cloned.IsTracked)
      {
        EnsureTracked(cloned.Id, suppressNotify: true);
      }

      if (notify)
      {
        ClampTrackedToLimit();
        NotifyListChanged();
        NotifyTrackedChanged();
      }
    }

    public void RemoveQuest(string questId)
    {
      if (string.IsNullOrWhiteSpace(questId)) return;

      _quests.Remove(questId);
      _trackedQuestOrder.Remove(questId);

      NotifyListChanged();
      NotifyTrackedChanged();
    }

    public void ClearAll()
    {
      _quests.Clear();
      _trackedQuestOrder.Clear();
      NotifyListChanged();
      NotifyTrackedChanged();
    }

    public void SetTracked(string questId, bool tracked)
    {
      if (string.IsNullOrWhiteSpace(questId) || !_quests.TryGetValue(questId, out var quest))
        return;

      if (tracked)
      {
        EnsureTracked(questId, suppressNotify: false);
      }
      else
      {
        quest.IsTracked = false;
        _trackedQuestOrder.Remove(questId);
      }

      ClampTrackedToLimit();
      NotifyTrackedChanged();
      NotifyListChanged();
    }

    private void EnsureTracked(string questId, bool suppressNotify)
    {
      if (_trackedQuestOrder.Contains(questId))
        return;

      if (_trackedQuestOrder.Count >= _maxTracked)
      {
        var removedId = _trackedQuestOrder[0];
        _trackedQuestOrder.RemoveAt(0);
        if (_quests.TryGetValue(removedId, out var removedQuest))
          removedQuest.IsTracked = false;
      }

      _trackedQuestOrder.Add(questId);
      if (_quests.TryGetValue(questId, out var quest))
        quest.IsTracked = true;

      if (!suppressNotify)
        ClampTrackedToLimit();
    }

    private void ClampTrackedToLimit()
    {
      while (_trackedQuestOrder.Count > _maxTracked)
      {
        var removedId = _trackedQuestOrder[0];
        _trackedQuestOrder.RemoveAt(0);
        if (_quests.TryGetValue(removedId, out var removedQuest))
          removedQuest.IsTracked = false;
      }
    }

    private void NotifyListChanged() => OnQuestListChanged?.Invoke(Quests);
    private void NotifyTrackedChanged() => OnTrackedQuestsChanged?.Invoke(TrackedQuests);

    private static List<QuestData> Snapshot(IEnumerable<QuestData> source)
    {
      var list = new List<QuestData>();
      foreach (var quest in source)
      {
        if (quest != null)
          list.Add(quest.Clone());
      }
      return list;
    }
  }
}
