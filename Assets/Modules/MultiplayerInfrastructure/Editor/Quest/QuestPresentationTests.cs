using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Quest;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.UI;
using NUnit.Framework;
using TriageTrainer.Scenario;
using UnityEngine;

namespace MultiplayerInfrastructure.Tests.Quest
{
  public sealed class QuestPresentationTests
  {
    private sealed class PresentationInteract : IInteract, IQuestPresentationTarget
    {
      public string DisplayText => "test";
      public Sprite DisplayIcon => null;
      public bool AllowDisplayIconFallback => true;
      public Color DisplayColor => Color.white;
      public string PresentationEntityIdentifier { get; set; }
      public string InteractionIdentifier { get; set; }
      public void Interact(Transform interactor) { }
    }

    [Test]
    public void QuestClonePreservesPresentationBindingsAndCriteriaIdentifiers()
    {
      var quest = new QuestData
      {
        Id = "quest",
        SourceScenarioIdentifier = "scenario",
        Tasks = new List<QuestCompletionCriteria>
        {
          new() { Identifier = "step-one", Type = QuestCompletionCriteriaType.InteractionSignalReceived }
        },
        PresentationBindings = new List<QuestPresentationBinding>
        {
          new()
          {
            Activation = QuestPresentationActivation.CompletionCriteria,
            CompletionCriteriaIdentifier = "step-one",
            TargetType = QuestPresentationTargetType.Interaction,
            EntityIdentifier = "npc",
            InteractionIdentifier = "report",
            IconIdentifier = "quest-icon",
            Priority = 4
          }
        }
      };

      var clone = quest.Clone();

      Assert.That(clone.SourceScenarioIdentifier, Is.EqualTo("scenario"));
      Assert.That(clone.Tasks[0].Identifier, Is.EqualTo("step-one"));
      Assert.That(clone.PresentationBindings, Has.Count.EqualTo(1));
      Assert.That(clone.PresentationBindings[0].InteractionIdentifier, Is.EqualTo("report"));
      Assert.That(clone.PresentationBindings[0].Priority, Is.EqualTo(4));
      Assert.That(clone.PresentationBindings[0], Is.Not.SameAs(quest.PresentationBindings[0]));
    }

    [Test]
    public void QuestJsonRoundTripPreservesPresentationBinding()
    {
      var options = new JsonSerializerOptions();
      options.Converters.Add(new JsonStringEnumConverter());
      var quest = new QuestData
      {
        Id = "quest",
        PresentationBindings = new List<QuestPresentationBinding>
        {
          new()
          {
            TargetType = QuestPresentationTargetType.Npc,
            EntityIdentifier = "doctor",
            IconIdentifier = "quest-marker"
          }
        }
      };

      string json = JsonSerializer.Serialize(quest, options);
      var restored = JsonSerializer.Deserialize<QuestData>(json, options);

      Assert.That(restored, Is.Not.Null);
      Assert.That(restored.PresentationBindings, Has.Count.EqualTo(1));
      Assert.That(restored.PresentationBindings[0].TargetType, Is.EqualTo(QuestPresentationTargetType.Npc));
      Assert.That(restored.PresentationBindings[0].EntityIdentifier, Is.EqualTo("doctor"));
    }

    [Test]
    public void ScenarioSchemaRoundTripPreservesInlinePresentationBinding()
    {
      var graph = new ScenarioGraph { Identifier = "quest-presentation", DefaultEntrypoint = "quest" };
      graph.Add(new ScenarioQuestControlNode
      {
        Identifier = "quest",
        Operation = ScenarioQuestOperationType.Add,
        Quest = new QuestData
        {
          Id = "quest",
          Tasks = new List<QuestCompletionCriteria>
          {
            new()
            {
              Identifier = "report-step",
              Type = QuestCompletionCriteriaType.InteractionSignalReceived,
              SignalId = "reported"
            }
          },
          PresentationBindings = new List<QuestPresentationBinding>
          {
            new()
            {
              Activation = QuestPresentationActivation.CompletionCriteria,
              CompletionCriteriaIdentifier = "report-step",
              TargetType = QuestPresentationTargetType.Interaction,
              EntityIdentifier = "doctor",
              InteractionIdentifier = "report",
              IconIdentifier = "quest-icon"
            }
          }
        }
      });

      string json = ScenarioGraphLoader.SaveToJson(graph, validateWithSchema: true);
      var restored = ScenarioGraphLoader.LoadFromJson(json, validateWithSchema: true);
      var questNode = (ScenarioQuestControlNode)restored.Nodes["quest"];

      Assert.That(questNode.Quest.Tasks[0].Identifier, Is.EqualTo("report-step"));
      Assert.That(questNode.Quest.PresentationBindings, Has.Count.EqualTo(1));
      Assert.That(questNode.Quest.PresentationBindings[0].InteractionIdentifier, Is.EqualTo("report"));
    }

    [Test]
    public void ScenarioQuestMarkNodeRoundTripsThroughSchema()
    {
      var graph = new ScenarioGraph { Identifier = "quest-mark", DefaultEntrypoint = "mark" };
      graph.Add(new ScenarioQuestMarkNode
      {
        Identifier = "mark",
        Operation = ScenarioQuestMarkOperationType.Show,
        TargetType = QuestPresentationTargetType.Npc,
        EntityIdentifier = "doctor",
        IconIdentifier = "quest-marker",
        Priority = 2,
        NextIdentifier = "unmark"
      });
      graph.Add(new ScenarioQuestMarkNode
      {
        Identifier = "unmark",
        Operation = ScenarioQuestMarkOperationType.Hide,
        TargetType = QuestPresentationTargetType.Interaction,
        EntityIdentifier = "doctor",
        InteractionIdentifier = "report"
      });

      string json = ScenarioGraphLoader.SaveToJson(graph, validateWithSchema: true);
      var restored = ScenarioGraphLoader.LoadFromJson(json, validateWithSchema: true);

      var show = (ScenarioQuestMarkNode)restored.Nodes["mark"];
      Assert.That(show.Operation, Is.EqualTo(ScenarioQuestMarkOperationType.Show));
      Assert.That(show.TargetType, Is.EqualTo(QuestPresentationTargetType.Npc));
      Assert.That(show.EntityIdentifier, Is.EqualTo("doctor"));
      Assert.That(show.IconIdentifier, Is.EqualTo("quest-marker"));
      Assert.That(show.Priority, Is.EqualTo(2));
      Assert.That(show.NextIdentifier, Is.EqualTo("unmark"));

      var hide = (ScenarioQuestMarkNode)restored.Nodes["unmark"];
      Assert.That(hide.Operation, Is.EqualTo(ScenarioQuestMarkOperationType.Hide));
      Assert.That(hide.TargetType, Is.EqualTo(QuestPresentationTargetType.Interaction));
      Assert.That(hide.InteractionIdentifier, Is.EqualTo("report"));
      Assert.That(hide.Priority, Is.EqualTo(0));
    }

    [Test]
    public void ScenarioQuestMarkNodeSupportsWaypointTarget()
    {
      var graph = new ScenarioGraph { Identifier = "quest-mark-waypoint", DefaultEntrypoint = "mark" };
      graph.Add(new ScenarioQuestMarkNode
      {
        Identifier = "mark",
        Operation = ScenarioQuestMarkOperationType.Show,
        TargetType = QuestPresentationTargetType.Waypoint,
        EntityIdentifier = "zone_a:marker",
        NextIdentifier = "unmark"
      });
      graph.Add(new ScenarioQuestMarkNode
      {
        Identifier = "unmark",
        Operation = ScenarioQuestMarkOperationType.Hide,
        TargetType = QuestPresentationTargetType.Waypoint,
        EntityIdentifier = "zone_a:marker"
      });

      string json = ScenarioGraphLoader.SaveToJson(graph, validateWithSchema: true);
      var restored = ScenarioGraphLoader.LoadFromJson(json, validateWithSchema: true);

      var show = (ScenarioQuestMarkNode)restored.Nodes["mark"];
      Assert.That(show.TargetType, Is.EqualTo(QuestPresentationTargetType.Waypoint));
      Assert.That(show.EntityIdentifier, Is.EqualTo("zone_a:marker"));
      Assert.That(show.Operation, Is.EqualTo(ScenarioQuestMarkOperationType.Show));

      var hide = (ScenarioQuestMarkNode)restored.Nodes["unmark"];
      Assert.That(hide.TargetType, Is.EqualTo(QuestPresentationTargetType.Waypoint));
      Assert.That(hide.Operation, Is.EqualTo(ScenarioQuestMarkOperationType.Hide));
    }

    [Test]
    public void WaypointQuestMarkerSharesTheOverheadLabelPathInsteadOfAWorldSprite()
    {
      var waypointObject = new GameObject("QuestPresentationTests.MarkerWaypoint");
      var texture = new Texture2D(2, 2);
      var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);

      try
      {
        var anchor = waypointObject.AddComponent<WaypointAnchor>();
        const System.Reflection.BindingFlags flags =
          System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        typeof(WaypointAnchor).GetField("_questMarkerSprite", flags)?.SetValue(anchor, sprite);

        var resolve = typeof(WaypointAnchor).GetMethod("ResolveQuestMarkerSprite", flags);
        Assert.That(resolve, Is.Not.Null);
        Assert.That(resolve.Invoke(anchor, null), Is.SameAs(sprite),
          "인스펙터에 지정한 스프라이트를 우선 사용해야 합니다.");

        // 라벨 컨트롤러가 없는 상태에서 표시를 요청해도 월드 오브젝트를 만들지 않아야 한다.
        // (월드 스프라이트 방식이 남아 있으면 크기 기준이 달라지고 지형에 가려진다.)
        anchor.SetQuestMarkerVisible(true);
        Assert.That(waypointObject.transform.childCount, Is.Zero,
          "waypoint 마커는 월드 오브젝트를 만들지 않고 오버헤드 라벨로 표시해야 합니다.");
        Assert.That(anchor.GetComponentsInChildren<SpriteRenderer>(true), Is.Empty);
        Assert.That(anchor.GetComponentsInChildren<TextMesh>(true), Is.Empty,
          "\u2756 텍스트 마커는 quest-marker 스프라이트로 전환되었으므로 남아 있으면 안 됩니다.");
        Assert.That(anchor.IsQuestMarkerVisible, Is.False,
          "라벨 컨트롤러가 준비되기 전에는 표시 상태를 올리지 않고 다음 호출에서 다시 시도해야 합니다.");
      }
      finally
      {
        Object.DestroyImmediate(waypointObject);
        Object.DestroyImmediate(sprite);
        Object.DestroyImmediate(texture);
      }
    }

    [Test]
    public void WaypointAndNpcQuestMarksUseTheSameDisplayController()
    {
      string projectRoot = System.IO.Directory.GetParent(Application.dataPath).FullName;
      string anchorSource = System.IO.File.ReadAllText(System.IO.Path.Combine(projectRoot,
        "Assets/Modules/MultiplayerInfrastructure/Scripts/Registry/WaypointAnchor.cs"));

      // 두 마커의 크기 기준이 갈리지 않도록 같은 표시 컨트롤러를 쓰는지 고정한다.
      // (하이라이트 시각 효과는 여전히 월드 스프라이트라서 파일 전체를 검사하면 안 된다.)
      StringAssert.Contains("EntityOverheadLabelUIController.ActiveInstance", anchorSource);

      int markerStart = anchorSource.IndexOf(
        "public void SetQuestMarkerVisible", System.StringComparison.Ordinal);
      int markerEnd = anchorSource.IndexOf(
        "private void EnsureHighlightVisual", System.StringComparison.Ordinal);
      Assert.That(markerStart, Is.GreaterThanOrEqualTo(0));
      Assert.That(markerEnd, Is.GreaterThan(markerStart));
      StringAssert.DoesNotContain("AddComponent<SpriteRenderer>()",
        anchorSource.Substring(markerStart, markerEnd - markerStart));
    }

    [Test]
    public void OverheadAnchorResolutionCoversNpcAndWaypointTargets()
    {
      var waypointObject = new GameObject("QuestPresentationTests.Waypoint");
      const string waypointIdentifier = "quest-presentation-waypoint-mark";

      try
      {
        waypointObject.AddComponent<WaypointAnchor>().ConfigureIdentifier(waypointIdentifier);

        // NPC 머리 위 마크와 waypoint 마크는 같은 표시 경로를 쓰고, 앵커를 찾는 방법만 다르다.
        var resolve = typeof(QuestPresentationService).GetMethod(
          "TryResolveOverheadAnchor",
          System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.That(resolve, Is.Not.Null,
          "일반화된 앵커 해석 진입점이 있어야 표시 경로를 공유할 수 있습니다.");

        var found = new object[] { QuestPresentationTargetType.Waypoint, waypointIdentifier, null };
        Assert.That(resolve.Invoke(null, found), Is.True);
        Assert.That(found[2], Is.SameAs(waypointObject.transform));

        var missing = new object[] { QuestPresentationTargetType.Waypoint, "no-such-waypoint", null };
        Assert.That(resolve.Invoke(null, missing), Is.False);
        Assert.That(missing[2], Is.Null);

        // NPC 대상은 레지스트리에 등록된 대상이 없으면 앵커를 찾지 못한 채 조용히 실패해야 한다.
        var npc = new object[] { QuestPresentationTargetType.Npc, "no-such-npc", null };
        Assert.That(resolve.Invoke(null, npc), Is.False);
      }
      finally
      {
        Object.DestroyImmediate(waypointObject);
      }
    }

    [Test]
    public void ScenarioMarkOverridesInteractionIconUntilCleared()
    {
      var managerObject = new GameObject("QuestPresentationTests.ScenarioMarkManager");
      var texture = new Texture2D(2, 2);
      var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
      const string iconIdentifier = "quest-presentation-scenario-mark-icon";
      Registry.Registry.RegisterIconSprite(iconIdentifier, sprite);

      try
      {
        managerObject.AddComponent<QuestManager>();
        var presentation = managerObject.GetComponent<QuestPresentationService>()
                           ?? managerObject.AddComponent<QuestPresentationService>();
        var interact = new PresentationInteract
        {
          PresentationEntityIdentifier = "doctor",
          InteractionIdentifier = "report"
        };

        Assert.That(presentation.TryGetPrimaryIconOverride(interact, out _), Is.False);

        QuestPresentationService.SetScenarioMark(
          QuestPresentationTargetType.Interaction, "doctor", "report", iconIdentifier, priority: 0);

        Assert.That(presentation.TryGetPrimaryIconOverride(interact, out var resolved), Is.True,
          "그래프가 켠 마크는 퀘스트가 없어도 상호작용 아이콘을 대체해야 한다.");
        Assert.That(resolved, Is.SameAs(sprite));

        QuestPresentationService.ClearScenarioMark(
          QuestPresentationTargetType.Interaction, "doctor", "report");

        Assert.That(presentation.TryGetPrimaryIconOverride(interact, out _), Is.False);
      }
      finally
      {
        QuestPresentationService.ClearScenarioMarks();
        Registry.Registry.InvalidateIconSprite(iconIdentifier);
        Object.DestroyImmediate(managerObject);
        Object.DestroyImmediate(sprite);
        Object.DestroyImmediate(texture);
      }
    }

    [Test]
    public void QuestStateFlagChangeRefreshesActivePresentation()
    {
      const string playerIdentifier = "quest-presentation-flags-changed-player";
      var managerObject = new GameObject("QuestPresentationTests.FlagRefreshManager");
      try
      {
        managerObject.AddComponent<QuestManager>();
        var presentation = managerObject.GetComponent<QuestPresentationService>()
                           ?? managerObject.AddComponent<QuestPresentationService>();
        int refreshCount = 0;
        presentation.OnPresentationChanged += () => refreshCount++;

        PlayerQuestStateFlagService.ReplaceFlags(playerIdentifier, new[] { "test.quest-state" });

        Assert.That(refreshCount, Is.EqualTo(1),
          "퀘스트 플래그 복제는 현재 표시 중인 상호작용 안내도 즉시 다시 계산해야 합니다.");
      }
      finally
      {
        PlayerQuestStateFlagService.ClearFlags(playerIdentifier);
        Object.DestroyImmediate(managerObject);
      }
    }

    [Test]
    public void ActiveQuestOverridesInteractionAndCompletionRestoresOriginal()
    {
      var managerObject = new GameObject("QuestPresentationTests.Manager");
      var texture = new Texture2D(2, 2);
      var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
      const string iconIdentifier = "quest-presentation-test-icon";
      Registry.Registry.RegisterIconSprite(iconIdentifier, sprite);

      try
      {
        var manager = managerObject.AddComponent<QuestManager>();
        var presentation = managerObject.GetComponent<QuestPresentationService>()
                           ?? managerObject.AddComponent<QuestPresentationService>();
        var interact = new PresentationInteract
        {
          PresentationEntityIdentifier = "doctor",
          InteractionIdentifier = "report"
        };
        manager.AddOrUpdateQuest(new QuestData
        {
          Id = "quest",
          IsTracked = true,
          Tasks = new List<QuestCompletionCriteria>
          {
            new()
            {
              Type = QuestCompletionCriteriaType.InteractionSignalReceived,
              SignalId = "quest-presentation-test-unraised-signal"
            }
          },
          PresentationBindings = new List<QuestPresentationBinding>
          {
            new()
            {
              TargetType = QuestPresentationTargetType.Interaction,
              EntityIdentifier = "doctor",
              InteractionIdentifier = "report",
              IconIdentifier = iconIdentifier
            }
          }
        });
        presentation.RefreshPresentation();

        Assert.That(manager.TryGetQuest("quest", out var activeQuest), Is.True);
        Assert.That(activeQuest.Completed, Is.False);
        Assert.That(activeQuest.PresentationBindings, Has.Count.EqualTo(1));
        Assert.That(activeQuest.PresentationBindings[0].TargetType, Is.EqualTo(QuestPresentationTargetType.Interaction));
        Assert.That(activeQuest.PresentationBindings[0].EntityIdentifier, Is.EqualTo("doctor"));
        Assert.That(activeQuest.PresentationBindings[0].InteractionIdentifier, Is.EqualTo("report"));
        Assert.That(activeQuest.PresentationBindings[0].IconIdentifier, Is.EqualTo(iconIdentifier));
        Assert.That(activeQuest.PresentationBindings[0].ShowWhenUntracked, Is.True);
        Assert.That(Registry.Registry.TryGet<Sprite>(RegistryType.IconSprite, iconIdentifier, out var registeredIcon), Is.True);
        Assert.That(registeredIcon, Is.SameAs(sprite));
        Assert.That(presentation.HasActiveInteractionBinding("doctor", "report"), Is.True);
        Assert.That(presentation.HasActiveInteractionBinding("doctor", "unrelated"), Is.False);
        Assert.That(presentation.TryGetPrimaryIconOverride("doctor", "report", out _), Is.True);
        Assert.That(presentation.TryGetPrimaryIconOverride(interact, out var resolved), Is.True);
        Assert.That(resolved, Is.SameAs(sprite));

        manager.CompleteQuest("quest");
        presentation.RefreshPresentation();
        Assert.That(presentation.TryGetPrimaryIconOverride(interact, out _), Is.True);
        presentation.ExpireQuestPresentation("quest");
        Assert.That(presentation.HasActiveInteractionBinding("doctor", "report"), Is.False);
        Assert.That(presentation.TryGetPrimaryIconOverride(interact, out _), Is.False);
      }
      finally
      {
        Registry.Registry.InvalidateIconSprite(iconIdentifier);
        Object.DestroyImmediate(managerObject);
        Object.DestroyImmediate(sprite);
        Object.DestroyImmediate(texture);
      }
    }

    [Test]
    public void AutoCompleteTrackedQuestWithoutPreviewHudDoesNotKeepPresentation()
    {
      var managerObject = new GameObject("QuestPresentationTests.AutoCompleteManager");
      var texture = new Texture2D(2, 2);
      var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
      const string iconIdentifier = "quest-presentation-auto-complete-icon";
      Registry.Registry.RegisterIconSprite(iconIdentifier, sprite);

      try
      {
        var manager = managerObject.AddComponent<QuestManager>();
        var presentation = managerObject.GetComponent<QuestPresentationService>();
        var interact = new PresentationInteract
        {
          PresentationEntityIdentifier = "doctor",
          InteractionIdentifier = "report"
        };
        manager.AddOrUpdateQuest(new QuestData
        {
          Id = "auto-complete-quest",
          IsTracked = true,
          IsAutoComplete = true,
          Tasks = new List<QuestCompletionCriteria>
          {
            new()
            {
              Type = QuestCompletionCriteriaType.InteractionSignalReceived,
              SignalId = "quest-presentation-auto-complete-unraised-signal"
            }
          },
          PresentationBindings = new List<QuestPresentationBinding>
          {
            new()
            {
              TargetType = QuestPresentationTargetType.Interaction,
              EntityIdentifier = "doctor",
              InteractionIdentifier = "report",
              IconIdentifier = iconIdentifier
            }
          }
        });

        manager.CompleteQuest("auto-complete-quest");

        Assert.That(presentation.TryGetPrimaryIconOverride(interact, out _), Is.False);
      }
      finally
      {
        Registry.Registry.InvalidateIconSprite(iconIdentifier);
        Object.DestroyImmediate(managerObject);
        Object.DestroyImmediate(sprite);
        Object.DestroyImmediate(texture);
      }
    }

    [Test]
    public void CompletionCriteriaBindingRemainsVisibleUntilPreviewExpiry()
    {
      var managerObject = new GameObject("QuestPresentationTests.CompletionGraceManager");
      var hudObject = new GameObject("QuestPresentationTests.CompletionGraceHud");
      var texture = new Texture2D(2, 2);
      var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
      const string iconIdentifier = "quest-presentation-completion-grace-icon";
      Registry.Registry.RegisterIconSprite(iconIdentifier, sprite);

      try
      {
        hudObject.AddComponent<UnityEngine.UIElements.UIDocument>();
        hudObject.AddComponent<QuestPreviewHudUIController>();
        var manager = managerObject.AddComponent<QuestManager>();
        var presentation = managerObject.GetComponent<QuestPresentationService>();
        var interact = new PresentationInteract
        {
          PresentationEntityIdentifier = "doctor",
          InteractionIdentifier = "report"
        };
        manager.AddOrUpdateQuest(new QuestData
        {
          Id = "completion-grace-quest",
          IsTracked = true,
          Tasks = new List<QuestCompletionCriteria>
          {
            new()
            {
              Identifier = "report-step",
              Type = QuestCompletionCriteriaType.InteractionSignalReceived,
              SignalId = "quest-presentation-completion-grace-unraised-signal"
            }
          },
          PresentationBindings = new List<QuestPresentationBinding>
          {
            new()
            {
              Activation = QuestPresentationActivation.CompletionCriteria,
              CompletionCriteriaIdentifier = "report-step",
              TargetType = QuestPresentationTargetType.Interaction,
              EntityIdentifier = "doctor",
              InteractionIdentifier = "report",
              IconIdentifier = iconIdentifier
            }
          }
        });

        manager.CompleteQuest("completion-grace-quest");

        Assert.That(presentation.TryGetPrimaryIconOverride(interact, out _), Is.True);
        manager.ExpireQuestPresentation("completion-grace-quest");
        Assert.That(presentation.TryGetPrimaryIconOverride(interact, out _), Is.False);
      }
      finally
      {
        Registry.Registry.InvalidateIconSprite(iconIdentifier);
        Object.DestroyImmediate(hudObject);
        Object.DestroyImmediate(managerObject);
        Object.DestroyImmediate(sprite);
        Object.DestroyImmediate(texture);
      }
    }

    [Test]
    public void OrdinalQuestActivatesBindingForIncompleteNestedCriterion()
    {
      var managerObject = new GameObject("QuestPresentationTests.OrdinalManager");
      var texture = new Texture2D(2, 2);
      var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
      const string iconIdentifier = "quest-presentation-ordinal-icon";
      Registry.Registry.RegisterIconSprite(iconIdentifier, sprite);

      try
      {
        var manager = managerObject.AddComponent<QuestManager>();
        var presentation = managerObject.GetComponent<QuestPresentationService>();
        var interact = new PresentationInteract
        {
          PresentationEntityIdentifier = "doctor",
          InteractionIdentifier = "report"
        };
        manager.AddOrUpdateQuest(new QuestData
        {
          Id = "ordinal-quest",
          IsOrdinal = true,
          Tasks = new List<QuestCompletionCriteria>
          {
            new()
            {
              Identifier = "current-stage",
              Type = QuestCompletionCriteriaType.AllOf,
              Conditions = new List<QuestCompletionCriteria>
              {
                new()
                {
                  Identifier = "report-stage",
                  Type = QuestCompletionCriteriaType.InteractionSignalReceived,
                  SignalId = "quest-presentation-ordinal-unraised-signal"
                }
              }
            }
          },
          PresentationBindings = new List<QuestPresentationBinding>
          {
            new()
            {
              Activation = QuestPresentationActivation.CompletionCriteria,
              CompletionCriteriaIdentifier = "report-stage",
              TargetType = QuestPresentationTargetType.Interaction,
              EntityIdentifier = "doctor",
              InteractionIdentifier = "report",
              IconIdentifier = iconIdentifier
            }
          }
        });

        Assert.That(presentation.TryGetPrimaryIconOverride(interact, out var resolved), Is.True);
        Assert.That(resolved, Is.SameAs(sprite));
      }
      finally
      {
        Registry.Registry.InvalidateIconSprite(iconIdentifier);
        Object.DestroyImmediate(managerObject);
        Object.DestroyImmediate(sprite);
        Object.DestroyImmediate(texture);
      }
    }

    [Test]
    public void OrdinalQuestMovesInteractionMarkerToNextCriterion()
    {
      var managerObject = new GameObject("QuestPresentationTests.OrdinalMarkerManager");
      var texture = new Texture2D(2, 2);
      var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
      const string iconIdentifier = "quest-presentation-ordinal-marker-icon";
      const string firstSignal = "quest-presentation-ordinal-marker-first";
      const string secondSignal = "quest-presentation-ordinal-marker-second";
      Registry.Registry.RegisterIconSprite(iconIdentifier, sprite);

      try
      {
        var manager = managerObject.AddComponent<QuestManager>();
        var presentation = managerObject.GetComponent<QuestPresentationService>();
        manager.AddOrUpdateQuest(new QuestData
        {
          Id = "ordinal-marker-quest",
          IsOrdinal = true,
          Tasks = new List<QuestCompletionCriteria>
          {
            new() { Identifier = "move-b", Type = QuestCompletionCriteriaType.InteractionSignalReceived, SignalId = firstSignal },
            new() { Identifier = "move-c", Type = QuestCompletionCriteriaType.InteractionSignalReceived, SignalId = secondSignal }
          },
          PresentationBindings = new List<QuestPresentationBinding>
          {
            new()
            {
              Activation = QuestPresentationActivation.CompletionCriteria,
              CompletionCriteriaIdentifier = "move-b",
              TargetType = QuestPresentationTargetType.Interaction,
              EntityIdentifier = "bed_b",
              InteractionIdentifier = "move_bed",
              IconIdentifier = iconIdentifier
            },
            new()
            {
              Activation = QuestPresentationActivation.CompletionCriteria,
              CompletionCriteriaIdentifier = "move-c",
              TargetType = QuestPresentationTargetType.Interaction,
              EntityIdentifier = "bed_c",
              InteractionIdentifier = "move_bed",
              IconIdentifier = iconIdentifier
            }
          }
        });

        Assert.That(presentation.TryGetPrimaryIconOverride("bed_b", "move_bed", out _), Is.True);
        Assert.That(presentation.TryGetPrimaryIconOverride("bed_c", "move_bed", out _), Is.False);

        ScenarioInteractionSignals.Raise(firstSignal);
        manager.EvaluateQuestProgress("ordinal-marker-quest");

        Assert.That(presentation.TryGetPrimaryIconOverride("bed_b", "move_bed", out _), Is.False);
        Assert.That(presentation.TryGetPrimaryIconOverride("bed_c", "move_bed", out _), Is.True);
      }
      finally
      {
        ScenarioInteractionSignals.Clear(firstSignal);
        ScenarioInteractionSignals.Clear(secondSignal);
        Registry.Registry.InvalidateIconSprite(iconIdentifier);
        Object.DestroyImmediate(managerObject);
        Object.DestroyImmediate(sprite);
        Object.DestroyImmediate(texture);
      }
    }

    [Test]
    public void ReplacedNpcKeepsReplacementRegistryEntryWhenOriginalIsDestroyed()
    {
      var originalObject = new GameObject("QuestPresentationTests.OriginalNpc");
      var replacementObject = new GameObject("QuestPresentationTests.ReplacementNpc");
      const string identifier = "quest-presentation-replacement-npc";

      try
      {
        var original = originalObject.AddComponent<Npc>();
        original.ApplySpawnedEntityIdentifier(identifier);
        var replacement = replacementObject.AddComponent<Npc>();
        replacement.ApplySpawnedEntityIdentifier(identifier);

        Object.DestroyImmediate(originalObject);

        Assert.That(Registry.Registry.Get<GameObject>(RegistryType.Npc, identifier), Is.SameAs(replacementObject));
        Assert.That(Registry.Registry.TryGetEntity(identifier, out var descriptor), Is.True);
        Assert.That(descriptor.GameObject, Is.SameAs(replacementObject));
      }
      finally
      {
        if (originalObject != null)
          Object.DestroyImmediate(originalObject);
        if (replacementObject != null)
          Object.DestroyImmediate(replacementObject);
      }
    }

    [Test]
    public void ScenarioActionUsesConfiguredPresentationAddressOrCompletionSignal()
    {
      var actionObject = new GameObject("QuestPresentationTests.ScenarioAction");
      try
      {
        var action = actionObject.AddComponent<ScenarioActionInteractable>();
        SetPrivateField(action, "_presentationEntityIdentifier", "patient-a");
        SetPrivateField(action, "_completionSignal", "interact-chest");

        Assert.That(action.PresentationEntityIdentifier, Is.EqualTo("patient-a"));
        Assert.That(action.InteractionIdentifier, Is.EqualTo("interact-chest"));
      }
      finally
      {
        Object.DestroyImmediate(actionObject);
      }
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
      var field = target.GetType().GetField(fieldName,
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
      Assert.That(field, Is.Not.Null, $"Missing field '{fieldName}'.");
      field.SetValue(target, value);
    }
  }
}
