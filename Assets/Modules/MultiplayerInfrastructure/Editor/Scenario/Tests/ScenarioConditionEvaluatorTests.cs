using System.Collections.Generic;
using MultiplayerInfrastructure.Quest;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.Tag;
using NUnit.Framework;
using UnityEngine;

namespace MultiplayerInfrastructure.Tests.Scenario
{
  /// <summary>
  /// 조건 절 판정 엔진의 종류별 판정, 결합, 부정, 관찰자 처리를 검증한다.
  /// EditMode 에서는 네트워크가 없으므로 신호는 로컬 폴백으로 즉시 기록되고, 태그·플래그는 복제 적용 API 로 직접 넣는다.
  /// </summary>
  public sealed class ScenarioConditionEvaluatorTests
  {
    private const string Player = "cond-test-player";
    private const string Entity = "cond-test-entity";
    private GameObject _entityObject;

    private sealed class TestStateProvider : MonoBehaviour, IConditionStateProvider
    {
      public int Stage = 2;
      public bool Ready = true;

      public IEnumerable<string> ConditionKeys => new[] { "stage", "ready" };

      public bool TryGetConditionValue(string key, string qualifier, out ConditionValue value)
      {
        switch (key)
        {
          case "stage": value = ConditionValue.From(Stage); return true;
          case "ready": value = ConditionValue.From(Ready); return true;
          default: value = default; return false;
        }
      }
    }

    [SetUp]
    public void SetUp()
    {
      Cleanup();
      _entityObject = new GameObject("cond-test-entity");
      _entityObject.AddComponent<TestStateProvider>();
      Registry.Registry.RegisterEntity(Entity, EntityType.Npc, _entityObject);
    }

    [TearDown]
    public void TearDown()
    {
      Cleanup();
      if (_entityObject != null)
        Object.DestroyImmediate(_entityObject);
    }

    private static void Cleanup()
    {
      Registry.Registry.UnregisterEntity(Entity);
      Registry.Registry.Unregister(RegistryType.RuntimeState, ScenarioInteractionSignals.Normalize("cond.a"));
      Registry.Registry.Unregister(RegistryType.RuntimeState, ScenarioInteractionSignals.Normalize("cond.b"));
      Registry.Registry.Unregister(RegistryType.Waypoint, "cond-test-waypoint");
      PlayerTagService.ReplaceTags(Player, System.Array.Empty<string>());
      PlayerTagService.ReplaceTags(Entity, System.Array.Empty<string>());
      PlayerQuestStateFlagService.ReplaceFlags(Player, System.Array.Empty<string>());
    }

    private static ScenarioConditionContext PlayerContext => ScenarioConditionContext.ForPlayerIdentifier(Player);

    [Test]
    public void EmptyConditionsAreSatisfied()
    {
      Assert.That(ScenarioConditionEvaluator.Evaluate(new List<ScenarioCondition>(), ScenarioConditionMatchMode.All, ScenarioConditionContext.Global), Is.True);
    }

    [Test]
    public void SignalRaised_FollowsRegistryState()
    {
      var condition = ScenarioCondition.Raised("cond.a");
      Assert.That(ScenarioConditionEvaluator.EvaluateOne(condition, ScenarioConditionContext.Global, out string reason), Is.False);
      Assert.That(reason, Does.Contain("sig.cond.a"));

      ScenarioInteractionSignals.Raise("cond.a");
      Assert.That(ScenarioConditionEvaluator.EvaluateOne(condition, ScenarioConditionContext.Global, out _), Is.True);
      Assert.That(ScenarioConditionEvaluator.EvaluateOne(ScenarioCondition.Raised("cond.a", negate: true), ScenarioConditionContext.Global, out _), Is.False);
    }

    [Test]
    public void RegistryContains_ChecksIdentifier()
    {
      var condition = new ScenarioCondition
      {
        Type = ScenarioConditionType.RegistryContains,
        RegistryType = RegistryType.Waypoint,
        Identifier = "cond-test-waypoint"
      };
      Assert.That(ScenarioConditionEvaluator.EvaluateOne(condition, ScenarioConditionContext.Global, out _), Is.False);
      Registry.Registry.Register(RegistryType.Waypoint, "cond-test-waypoint", new object());
      Assert.That(ScenarioConditionEvaluator.EvaluateOne(condition, ScenarioConditionContext.Global, out _), Is.True);
    }

    [Test]
    public void PlayerHasTag_RequiresObserverAndTag()
    {
      var condition = ScenarioCondition.PlayerTag("nurse_b");
      Assert.That(ScenarioConditionEvaluator.EvaluateOne(condition, ScenarioConditionContext.Global, out string reason), Is.False);
      Assert.That(reason, Does.Contain("no observer"));

      Assert.That(ScenarioConditionEvaluator.EvaluateOne(condition, PlayerContext, out _), Is.False);
      PlayerTagService.ReplaceTags(Player, new[] { "nurse_b" });
      Assert.That(ScenarioConditionEvaluator.EvaluateOne(condition, PlayerContext, out _), Is.True);
    }

    [Test]
    public void PlayerHasQuestFlag_ReadsFlagPool()
    {
      var condition = ScenarioCondition.QuestFlag("scen.step");
      Assert.That(ScenarioConditionEvaluator.EvaluateOne(condition, PlayerContext, out _), Is.False);
      PlayerQuestStateFlagService.ReplaceFlags(Player, new[] { "scen.step" });
      Assert.That(ScenarioConditionEvaluator.EvaluateOne(condition, PlayerContext, out _), Is.True);
    }

    [Test]
    public void EntityHasTag_ByIdentifier()
    {
      var condition = new ScenarioCondition
      {
        Type = ScenarioConditionType.EntityHasTag,
        Entity = ScenarioEntityReference.ForIdentifier(Entity),
        Tag = "cpr_target"
      };
      Assert.That(ScenarioConditionEvaluator.EvaluateOne(condition, ScenarioConditionContext.Global, out _), Is.False);
      PlayerTagService.ReplaceTags(Entity, new[] { "cpr_target" });
      Assert.That(ScenarioConditionEvaluator.EvaluateOne(condition, ScenarioConditionContext.Global, out _), Is.True);
    }

    [Test]
    public void EntityState_UsesProviderValuesAndCompare()
    {
      var reference = ScenarioEntityReference.ForIdentifier(Entity);
      Assert.That(ScenarioConditionEvaluator.EvaluateOne(ScenarioCondition.State(reference, "ready"), ScenarioConditionContext.Global, out _), Is.True);
      Assert.That(ScenarioConditionEvaluator.EvaluateOne(ScenarioCondition.State(reference, "ready", "false"), ScenarioConditionContext.Global, out _), Is.False);
      Assert.That(ScenarioConditionEvaluator.EvaluateOne(
        ScenarioCondition.State(reference, "stage", "1", compare: ScenarioConditionCompare.GreaterThan), ScenarioConditionContext.Global, out _), Is.True);
      Assert.That(ScenarioConditionEvaluator.EvaluateOne(
        ScenarioCondition.State(reference, "stage", "3", compare: ScenarioConditionCompare.GreaterThanOrEqual), ScenarioConditionContext.Global, out _), Is.False);
      Assert.That(ScenarioConditionEvaluator.EvaluateOne(ScenarioCondition.State(reference, "unknown"), ScenarioConditionContext.Global, out string reason), Is.False);
      Assert.That(reason, Does.Contain("unknown"));
    }

    [Test]
    public void EntityState_ByTagMatchesAnyTaggedEntity()
    {
      PlayerTagService.ReplaceTags(Entity, new[] { "patient" });
      var condition = ScenarioCondition.State(ScenarioEntityReference.ForTag("patient"), "ready");
      Assert.That(ScenarioConditionEvaluator.EvaluateOne(condition, ScenarioConditionContext.Global, out _), Is.True);
    }

    [Test]
    public void Group_CombinesWithMatchMode()
    {
      ScenarioInteractionSignals.Raise("cond.a");
      var any = ScenarioCondition.AnyOf(ScenarioCondition.Raised("cond.a"), ScenarioCondition.Raised("cond.b"));
      var all = ScenarioCondition.AllOf(ScenarioCondition.Raised("cond.a"), ScenarioCondition.Raised("cond.b"));
      Assert.That(ScenarioConditionEvaluator.EvaluateOne(any, ScenarioConditionContext.Global, out _), Is.True);
      Assert.That(ScenarioConditionEvaluator.EvaluateOne(all, ScenarioConditionContext.Global, out string reason), Is.False);
      Assert.That(reason, Does.Contain("cond.b"));
    }

    [Test]
    public void ListEvaluation_AnyModeReportsAllFailures()
    {
      var conditions = new List<ScenarioCondition> { ScenarioCondition.Raised("cond.a"), ScenarioCondition.Raised("cond.b") };
      Assert.That(ScenarioConditionEvaluator.Evaluate(conditions, ScenarioConditionMatchMode.Any, ScenarioConditionContext.Global, out string reason), Is.False);
      Assert.That(reason, Does.Contain("cond.a").And.Contain("cond.b"));
      ScenarioInteractionSignals.Raise("cond.b");
      Assert.That(ScenarioConditionEvaluator.Evaluate(conditions, ScenarioConditionMatchMode.Any, ScenarioConditionContext.Global, out _), Is.True);
      Assert.That(ScenarioConditionEvaluator.Evaluate(conditions, ScenarioConditionMatchMode.All, ScenarioConditionContext.Global, out _), Is.False);
    }

    [Test]
    public void RequiresPlayer_DetectsNestedPlayerConditions()
    {
      var group = ScenarioCondition.AllOf(ScenarioCondition.Raised("cond.a"), ScenarioCondition.AnyOf(ScenarioCondition.PlayerTag("x")));
      Assert.That(group.RequiresPlayer, Is.True);
      Assert.That(ScenarioCondition.Raised("cond.a").RequiresPlayer, Is.False);
    }

    [Test]
    public void ConditionValue_ComparesTextCaseInsensitively()
    {
      var value = ConditionValue.From("AwaitingIv");
      Assert.That(value.Satisfies(ScenarioConditionCompare.Equal, "awaitingiv"), Is.True);
      Assert.That(value.Satisfies(ScenarioConditionCompare.NotEqual, "done"), Is.True);
      Assert.That(ConditionValue.From(2.5f).Satisfies(ScenarioConditionCompare.LessThanOrEqual, "2.5"), Is.True);
      Assert.That(ConditionValue.From(true).Satisfies(ScenarioConditionCompare.Equal, null), Is.True);
    }
  }
}
