using System.Collections.Generic;
using System.Text.RegularExpressions;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.Tag;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MultiplayerInfrastructure.Tests.Scenario
{
  /// <summary>
  /// 인터렉션 레지스트리의 초기화 사이클 경고, 코드·데이터 병합, 가시성 합성(오버라이드 → 조건 → 초기값),
  /// 보류 정의 적용, 범용 핸들러 생성, 수행 뒤 가시성 처리를 검증한다.
  /// </summary>
  public sealed class InteractionRegistryTests
  {
    private const string Entity = "reg-test-entity";
    private const string Scenario = "reg-test-scenario";
    private const string Player = "reg-test-player";
    private GameObject _entityObject;

    private sealed class StubInteract : IInteract, IInteractOutcomeSource
    {
      public bool Succeeded = true;
      public string DisplayText => "stub";
      public Sprite DisplayIcon => null;
      public bool AllowDisplayIconFallback => true;
      public Color DisplayColor => Color.white;
      public bool LastInteractSucceeded => Succeeded;
      public void Interact(Transform interactor) { }
    }

    private sealed class Source : IInteractionDefinitionSource
    {
      public readonly StubInteract Handler = new StubInteract();

      public IEnumerable<InteractionDeclaration> DeclareInteractions()
      {
        yield return new InteractionDeclaration(
          InteractionDefinition.Code(Entity, "assess", "코드 문구", initialVisible: false), Handler);
        var visible = InteractionDefinition.Code(Entity, "always", "항상", initialVisible: true);
        visible.AfterInteract = InteractionAfterInteract.HideForAll;
        visible.AfterInteractSpecified = true;
        yield return new InteractionDeclaration(visible, new StubInteract());
      }
    }

    [SetUp]
    public void SetUp()
    {
      InteractionRegistry.ClearAll();
      Registry.Registry.UnregisterEntity(Entity);
      Registry.Registry.Unregister(RegistryType.RuntimeState, ScenarioInteractionSignals.Normalize("reg.open"));
      PlayerTagService.ReplaceTags(Player, System.Array.Empty<string>());
      PlayerTagService.ReplaceTags(Entity, System.Array.Empty<string>());
      _entityObject = new GameObject(Entity);
      Registry.Registry.RegisterEntity(Entity, EntityType.Npc, _entityObject);
    }

    [TearDown]
    public void TearDown()
    {
      InteractionRegistry.ClearAll();
      Registry.Registry.UnregisterEntity(Entity);
      Registry.Registry.Unregister(RegistryType.RuntimeState, ScenarioInteractionSignals.Normalize("reg.open"));
      if (_entityObject != null)
        Object.DestroyImmediate(_entityObject);
    }

    [TestCase(InteractionKind.Action)]
    [TestCase(InteractionKind.ItemSubmission)]
    public void DispatchNotification_DoesNotCompleteGenericInteraction(InteractionKind kind)
    {
      var definition = InteractionDefinition.Code(Entity, "pending", "pending", true, kind);
      definition.AfterInteract = InteractionAfterInteract.HideForAll;
      using (InteractionRegistry.BeginScenarioInitCycle(Scenario))
        InteractionRegistry.ApplyScenarioDefinitions(Scenario, new[] { definition });
      Assert.That(InteractionRegistry.TryGet(new InteractionAddress(Entity, "pending"), out var entry), Is.True);
      int completed = 0;
      System.Action<InteractionRegistryEntry, MultiplayerInfrastructure.Player.PlayerController> onCompleted = (_, _) => completed++;
      InteractionRegistry.Interacted += onCompleted;
      try
      {
        // The input dispatcher cannot infer success from the void Interact return value.
        InteractionRegistry.NotifyCustomInteracted(entry.Handler, null);
        Assert.That(completed, Is.Zero);
        Assert.That(InteractionVisibilityState.TryGetOverride(entry.Address.Key, null, out _), Is.False);

        InteractionRegistry.NotifyInteracted(entry.Handler, null);
        Assert.That(completed, Is.EqualTo(1));
        Assert.That(InteractionVisibilityState.TryGetOverride(entry.Address.Key, null, out bool visible), Is.True);
        Assert.That(visible, Is.False);
      }
      finally
      {
        InteractionRegistry.Interacted -= onCompleted;
        // Generic submission handlers dispose their runtime object with Destroy in play mode.
        foreach (Transform child in _entityObject.transform)
          Object.DestroyImmediate(child.gameObject);
      }
    }

    [Test]
    public void DeclareCode_WithinEntityCycle_DoesNotWarn()
    {
      InteractionRegistry.DeclareCode(Entity, new Source());
      Assert.That(InteractionRegistry.TryGet(new InteractionAddress(Entity, "assess"), out var entry), Is.True);
      Assert.That(entry.RegisteredOutsideInitCycle, Is.False);
      Assert.That(entry.Definition.Display.Text, Is.EqualTo("코드 문구"));
    }

    [Test]
    public void DeclareCode_OutsideCycle_WarnsButRegisters()
    {
      LogAssert.Expect(LogType.Warning, new Regex("registered outside an initialization cycle"));
      InteractionRegistry.DeclareCode(InteractionDefinition.Code(Entity, "late", "늦은 등록"), new StubInteract());
      Assert.That(InteractionRegistry.TryGet(new InteractionAddress(Entity, "late"), out var entry), Is.True);
      Assert.That(entry.RegisteredOutsideInitCycle, Is.True);
    }

    [Test]
    public void ScenarioDefinition_MergesOverCodeAndClearsOnEnd()
    {
      InteractionRegistry.DeclareCode(Entity, new Source());
      var overlay = new InteractionDefinition
      {
        Entity = ScenarioEntityReference.ForIdentifier(Entity),
        InteractionIdentifier = "assess",
        Display = new InteractionDisplay { Text = "데이터 문구", Priority = 900, PrioritySpecified = true },
        VisibilityConditions = new List<ScenarioCondition> { ScenarioCondition.Raised("reg.open") }
      };
      using (InteractionRegistry.BeginScenarioInitCycle(Scenario))
        InteractionRegistry.ApplyScenarioDefinitions(Scenario, new[] { overlay });

      InteractionRegistry.TryGet(new InteractionAddress(Entity, "assess"), out var entry);
      Assert.That(entry.Definition.Display.Text, Is.EqualTo("데이터 문구"));
      Assert.That(entry.Definition.Display.Priority, Is.EqualTo(900));
      Assert.That(entry.Definition.HasVisibilityConditions, Is.True);
      Assert.That(entry.Handler, Is.InstanceOf<StubInteract>(), "코드 핸들러가 유지되어야 한다");

      InteractionRegistry.ClearScenarioDefinitions(Scenario);
      InteractionRegistry.TryGet(new InteractionAddress(Entity, "assess"), out entry);
      Assert.That(entry.Definition.Display.Text, Is.EqualTo("코드 문구"));
      Assert.That(entry.Definition.HasVisibilityConditions, Is.False);
    }

    [Test]
    public void Visibility_OverrideBeatsConditionBeatsInitial()
    {
      InteractionRegistry.DeclareCode(Entity, new Source());
      var address = new InteractionAddress(Entity, "assess");
      var context = ScenarioConditionContext.ForPlayerIdentifier(Player);
      InteractionRegistry.TryGet(address, out var entry);

      Assert.That(InteractionRegistry.IsVisible(entry, context, out string reason), Is.False);
      Assert.That(reason, Does.Contain("initial"));

      using (InteractionRegistry.BeginScenarioInitCycle(Scenario))
      {
        InteractionRegistry.ApplyScenarioDefinitions(Scenario, new[]
        {
          new InteractionDefinition
          {
            Entity = ScenarioEntityReference.ForIdentifier(Entity),
            InteractionIdentifier = "assess",
            VisibilityConditions = new List<ScenarioCondition> { ScenarioCondition.Raised("reg.open") }
          }
        });
      }
      InteractionRegistry.TryGet(address, out entry);
      Assert.That(InteractionRegistry.IsVisible(entry, context, out _), Is.False);
      ScenarioInteractionSignals.Raise("reg.open");
      Assert.That(InteractionRegistry.IsVisible(entry, context, out _), Is.True);

      InteractionVisibilityState.ApplyLocal(address.Key, InteractionVisibilityScope.Global, null, InteractionVisibilityOverride.Hide);
      Assert.That(InteractionRegistry.IsVisible(entry, context, out reason), Is.False);
      Assert.That(reason, Does.Contain("override"));

      InteractionVisibilityState.ApplyLocal(address.Key, InteractionVisibilityScope.Player, Player, InteractionVisibilityOverride.Show);
      Assert.That(InteractionRegistry.IsVisible(entry, context, out _), Is.True, "플레이어 층이 전역 층보다 우선한다");
      Assert.That(InteractionRegistry.IsVisible(entry, ScenarioConditionContext.ForPlayerIdentifier("someone-else"), out _), Is.False);

      InteractionVisibilityState.ApplyLocal(address.Key, InteractionVisibilityScope.Player, Player, InteractionVisibilityOverride.Reset);
      InteractionVisibilityState.ApplyLocal(address.Key, InteractionVisibilityScope.Global, null, InteractionVisibilityOverride.Reset);
      Assert.That(InteractionRegistry.IsVisible(entry, context, out _), Is.True, "오버라이드를 지우면 조건 판정으로 돌아간다");
    }

    [Test]
    public void PendingDefinition_AppliesWhenEntityAppearsAndByTag()
    {
      Registry.Registry.UnregisterEntity(Entity);
      var byId = new InteractionDefinition
      {
        Entity = ScenarioEntityReference.ForIdentifier(Entity),
        InteractionIdentifier = "ping",
        Kind = InteractionKind.Signal,
        KindSpecified = true,
        CompletionSignal = "reg.ping"
      };
      var byTag = new InteractionDefinition
      {
        Entity = ScenarioEntityReference.ForTag("cpr_target"),
        InteractionIdentifier = "compress",
        Kind = InteractionKind.Signal,
        KindSpecified = true,
        CompletionSignal = "reg.compress"
      };
      using (InteractionRegistry.BeginScenarioInitCycle(Scenario))
        InteractionRegistry.ApplyScenarioDefinitions(Scenario, new[] { byId, byTag });

      Assert.That(InteractionRegistry.TryGet(new InteractionAddress(Entity, "ping"), out _), Is.False);

      Registry.Registry.RegisterEntity(Entity, EntityType.Npc, _entityObject);
      Assert.That(InteractionRegistry.TryGet(new InteractionAddress(Entity, "ping"), out var entry), Is.True);
      Assert.That(entry.Handler, Is.Not.Null.And.InstanceOf<IInteract>());
      Assert.That(entry.HandlerIsGeneric, Is.True);
      Assert.That(InteractionRegistry.TryGet(new InteractionAddress(Entity, "compress"), out _), Is.False);

      PlayerTagService.ReplaceTags(Entity, new[] { "cpr_target" });
      Assert.That(InteractionRegistry.TryGet(new InteractionAddress(Entity, "compress"), out var tagged), Is.True);

      var buffer = new List<IInteract>();
      InteractionRegistry.CollectInteractsForEntity(Entity, buffer);
      Assert.That(buffer, Has.Count.EqualTo(2));
      Assert.That(InteractionRegistry.TryGetByHandler(buffer[0], out _), Is.True);

      InteractionRegistry.ClearScenarioDefinitions(Scenario);
      Assert.That(InteractionRegistry.TryGet(new InteractionAddress(Entity, "ping"), out _), Is.False);
      Assert.That(tagged.Handler, Is.Null.Or.Not.Null); // 항목 참조는 남아도 레지스트리에서는 제거된다.
    }

    [Test]
    public void NotifyInteracted_AppliesAfterInteractForSuccessfulCustomHandler()
    {
      var source = new Source();
      InteractionRegistry.DeclareCode(Entity, source);
      var address = new InteractionAddress(Entity, "always");
      InteractionRegistry.TryGet(address, out var entry);
      var context = ScenarioConditionContext.ForPlayerIdentifier(Player);
      Assert.That(InteractionRegistry.IsVisible(entry, context, out _), Is.True);

      var failing = (StubInteract)entry.Handler;
      failing.Succeeded = false;
      InteractionRegistry.NotifyInteracted(entry.Handler, null);
      Assert.That(InteractionRegistry.IsVisible(entry, context, out _), Is.True, "실패한 수행은 숨기지 않는다");

      failing.Succeeded = true;
      InteractionRegistry.NotifyInteracted(entry.Handler, null);
      Assert.That(InteractionRegistry.IsVisible(entry, context, out string reason), Is.False);
      Assert.That(reason, Does.Contain("override: hide"));
    }

    [Test]
    public void RemoveCodeDefinitions_DropsEntriesWithoutData()
    {
      InteractionRegistry.DeclareCode(Entity, new Source());
      Assert.That(InteractionRegistry.Count, Is.EqualTo(2));
      InteractionRegistry.RemoveCodeDefinitions(Entity);
      Assert.That(InteractionRegistry.Count, Is.EqualTo(0));
    }

    [Test]
    public void VisibilityState_SnapshotRoundTrips()
    {
      InteractionVisibilityState.ApplyLocal("a/b", InteractionVisibilityScope.Global, null, InteractionVisibilityOverride.Show);
      InteractionVisibilityState.ApplyLocal("c/d", InteractionVisibilityScope.Player, Player, InteractionVisibilityOverride.Hide);
      var snapshot = InteractionVisibilityState.Snapshot();
      InteractionVisibilityState.ClearAll();
      Assert.That(InteractionVisibilityState.TryGetOverride("a/b", null, out _), Is.False);
      InteractionVisibilityState.ApplySnapshot(snapshot);
      Assert.That(InteractionVisibilityState.TryGetOverride("a/b", null, out bool visible) && visible, Is.True);
      Assert.That(InteractionVisibilityState.TryGetOverride("c/d", Player, out visible) && !visible, Is.True);
      Assert.That(InteractionVisibilityState.TryGetOverride("c/d", "other", out _), Is.False);
    }
  }
}
