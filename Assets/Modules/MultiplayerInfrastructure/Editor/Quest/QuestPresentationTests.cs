using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Entity;
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
        Assert.That(presentation.TryGetPrimaryIconOverride("doctor", "report", out _), Is.True);
        Assert.That(presentation.TryGetPrimaryIconOverride(interact, out var resolved), Is.True);
        Assert.That(resolved, Is.SameAs(sprite));

        manager.CompleteQuest("quest");
        presentation.RefreshPresentation();
        Assert.That(presentation.TryGetPrimaryIconOverride(interact, out _), Is.True);
        presentation.ExpireQuestPresentation("quest");
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
