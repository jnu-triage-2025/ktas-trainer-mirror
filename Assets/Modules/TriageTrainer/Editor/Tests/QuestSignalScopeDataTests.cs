using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using UnityEngine;

namespace TriageTrainer.Tests
{
  /// <summary>
  /// 퀘스트 완료 조건의 신호 판정 범위가 시나리오 그래프의 실제 구조와 일치하는지 확인한다.
  ///
  /// <para>한 Parallel 노드 안에서 두 개 이상의 분기가 같은 퀘스트를 부여하면, 그 퀘스트는 여러 참여자가
  /// 동시에 보유하는 공동 목표이므로 신호 발신자를 구분하지 않아야 한다. 반대로 한 분기만 부여하는
  /// 퀘스트는 한 참여자에게만 배정되므로, 다른 참여자의 행동으로 완료되지 않도록 Owner 범위를
  /// 지정해야 한다. 그래프가 나중에 바뀌어 이 관계가 달라지면 이 검사가 먼저 실패한다.</para>
  /// </summary>
  public sealed class QuestSignalScopeDataTests
  {
    private static readonly (string Scenario, string Quest)[] Pairs =
    {
      ("patient_a_critical.scenario.json", "patient_a_critical.quests.quest.json"),
      ("patient_b_c_ct.scenario.json", "patient_b_c_ct.quests.quest.json")
    };

    [Test]
    public void RoleExclusiveQuestSignalsUseOwnerScopeAndSharedQuestsDoNot()
    {
      foreach ((string scenarioFile, string questFile) in Pairs)
      {
        using var scenario = JsonDocument.Parse(File.ReadAllText(Path.Combine(
          Application.dataPath, "Modules/TriageTrainer/Resources/Scenario/", scenarioFile)));
        using var quests = JsonDocument.Parse(File.ReadAllText(Path.Combine(
          Application.dataPath, "Modules/TriageTrainer/Resources/Quest/", questFile)));

        var nodes = scenario.RootElement.GetProperty("nodes")
          .EnumerateObject()
          .ToDictionary(property => property.Name, property => property.Value);
        ClassifyQuests(nodes, out var exclusive, out var shared);

        Assert.That(exclusive, Is.Not.Empty, scenarioFile);

        foreach (JsonElement definition in quests.RootElement.GetProperty("definitions").EnumerateArray())
        {
          string identifier = definition.GetProperty("identifier").GetString();
          bool expectOwner = exclusive.Contains(identifier);
          if (!expectOwner && !shared.Contains(identifier))
          {
            // 이 시나리오의 QuestControl 노드가 부여하지 않는 정의는 판정 대상이 아니다.
            continue;
          }

          string expected = expectOwner ? "Owner" : "Any";
          foreach (JsonElement criterion in EnumerateSignalCriteria(definition))
          {
            string actual = criterion.TryGetProperty("signalScope", out JsonElement scope)
              ? scope.GetString()
              : "Any";
            Assert.That(actual, Is.EqualTo(expected),
              $"{questFile} / {identifier} / {criterion.GetProperty("signalId").GetString()} 의 signalScope 가 "
              + $"그래프 구조와 어긋납니다. 이 퀘스트는 {(expectOwner ? "한 분기" : "여러 분기")}에서 부여됩니다.");
          }
        }
      }
    }

    /// <summary>
    /// 각 Parallel 노드의 분기를 따라가며, 같은 Parallel 안에서 몇 개의 분기가 그 퀘스트를 부여하는지 센다.
    /// 분기 밖(본류)에서 부여되는 퀘스트는 시나리오 owner 가 지정되지 않으면 모든 참여자에게 적용되므로
    /// 공동 목표로 분류한다.
    /// </summary>
    private static void ClassifyQuests(
      IReadOnlyDictionary<string, JsonElement> nodes,
      out HashSet<string> exclusive,
      out HashSet<string> shared)
    {
      exclusive = new HashSet<string>();
      shared = new HashSet<string>();
      var visitedInsideBranches = new HashSet<string>();

      foreach (JsonElement node in nodes.Values)
      {
        if (GetString(node, "nodeType") != "Parallel" || !node.TryGetProperty("branches", out JsonElement branches))
          continue;

        string join = GetString(node, "nextIdentifier");
        var branchCountByQuest = new Dictionary<string, int>();
        foreach (JsonElement branch in branches.EnumerateArray())
        {
          string stopLabel = GetString(branch, "completionConditionIdentifier");
          var reached = new HashSet<string>();
          var pending = new Stack<string>();
          pending.Push(GetString(branch, "identifier"));
          var questsInThisBranch = new HashSet<string>();
          while (pending.Count > 0)
          {
            string current = pending.Pop();
            if (string.IsNullOrEmpty(current) || current == stopLabel || current == join || !reached.Add(current))
              continue;
            if (!nodes.TryGetValue(current, out JsonElement branchNode))
              continue;

            visitedInsideBranches.Add(current);
            if (GetString(branchNode, "nodeType") == "QuestControl"
                && GetString(branchNode, "operation") == "Add")
            {
              string quest = GetString(branchNode, "questDefinitionIdentifier");
              if (!string.IsNullOrEmpty(quest))
                questsInThisBranch.Add(quest);
            }

            foreach (string successor in Successors(branchNode))
              pending.Push(successor);
          }

          foreach (string quest in questsInThisBranch)
          {
            branchCountByQuest.TryGetValue(quest, out int count);
            branchCountByQuest[quest] = count + 1;
          }
        }

        foreach (KeyValuePair<string, int> pair in branchCountByQuest)
          (pair.Value > 1 ? shared : exclusive).Add(pair.Key);
      }

      foreach (KeyValuePair<string, JsonElement> pair in nodes)
      {
        if (visitedInsideBranches.Contains(pair.Key)
            || GetString(pair.Value, "nodeType") != "QuestControl"
            || GetString(pair.Value, "operation") != "Add")
          continue;

        string quest = GetString(pair.Value, "questDefinitionIdentifier");
        if (!string.IsNullOrEmpty(quest))
          shared.Add(quest);
      }

      exclusive.ExceptWith(shared);
    }

    private static IEnumerable<string> Successors(JsonElement node)
    {
      foreach (string key in new[] { "nextIdentifier", "failureNextIdentifier" })
      {
        string value = GetString(node, key);
        if (!string.IsNullOrEmpty(value))
          yield return value;
      }

      if (node.TryGetProperty("options", out JsonElement options) && options.ValueKind == JsonValueKind.Array)
      {
        foreach (JsonElement option in options.EnumerateArray())
        {
          string value = GetString(option, "nextNodeIdentifier");
          if (!string.IsNullOrEmpty(value))
            yield return value;
        }
      }

      if (node.TryGetProperty("branches", out JsonElement branches) && branches.ValueKind == JsonValueKind.Array)
      {
        foreach (JsonElement branch in branches.EnumerateArray())
        {
          string value = GetString(branch, "identifier");
          if (!string.IsNullOrEmpty(value))
            yield return value;
        }
      }
    }

    private static IEnumerable<JsonElement> EnumerateSignalCriteria(JsonElement definition)
    {
      foreach (string listName in new[] { "tasks", "completionCriteria" })
      {
        if (!definition.TryGetProperty(listName, out JsonElement list) || list.ValueKind != JsonValueKind.Array)
          continue;

        foreach (JsonElement criterion in Flatten(list))
        {
          if (GetString(criterion, "type") == "InteractionSignalReceived"
              && !string.IsNullOrEmpty(GetString(criterion, "signalId")))
            yield return criterion;
        }
      }
    }

    private static IEnumerable<JsonElement> Flatten(JsonElement list)
    {
      foreach (JsonElement criterion in list.EnumerateArray())
      {
        yield return criterion;
        if (criterion.TryGetProperty("conditions", out JsonElement nested)
            && nested.ValueKind == JsonValueKind.Array)
        {
          foreach (JsonElement child in Flatten(nested))
            yield return child;
        }
      }
    }

    private static string GetString(JsonElement element, string property)
      => element.ValueKind == JsonValueKind.Object
         && element.TryGetProperty(property, out JsonElement value)
         && value.ValueKind == JsonValueKind.String
        ? value.GetString()
        : null;
  }
}
