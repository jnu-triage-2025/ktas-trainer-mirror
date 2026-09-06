using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Scenario;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MultiplayerInfrastructure.Tests.Scenario
{
  /// <summary>
  /// 최상위 interactions 구역, InteractionVisibility 노드, Validator 조건 절, 옛 actingNpcs[].interactions 변환의
  /// 저장·로드 왕복과 스키마 검증을 확인한다.
  /// </summary>
  public sealed class ScenarioInteractionDefinitionRoundTripTests
  {
    [Test]
    public void InteractionsSectionAndVisibilityNodeRoundTrip()
    {
      var graph = new ScenarioGraph { Identifier = "interaction-round-trip", DefaultEntrypoint = "open" };
      graph.Interactions = new List<InteractionDefinition>
      {
        new InteractionDefinition
        {
          Entity = ScenarioEntityReference.ForIdentifier("patient_a"),
          InteractionIdentifier = "assess_vital",
          Display = new InteractionDisplay { Text = "활력징후 사정", Priority = 10, PrioritySpecified = true, IconIdentifiers = new List<string> { "quest-interaction" } },
          CompletionSignal = "check_vital_patient_a",
          AfterInteract = InteractionAfterInteract.HideForPlayer,
          AfterInteractSpecified = true,
          InitialVisible = false,
          InitialVisibleSpecified = true,
          VisibilityConditions = new List<ScenarioCondition>
          {
            ScenarioCondition.PlayerTag("nurse_b"),
            ScenarioCondition.Quest("Quest_Check_Vital_PatientA", ScenarioQuestConditionState.Active, "assess-vital-patient-a"),
            ScenarioCondition.AnyOf(ScenarioCondition.Raised("a"), ScenarioCondition.State(ScenarioEntityReference.ForTag("patient"), "ready", "true"))
          }
        },
        new InteractionDefinition
        {
          Entity = ScenarioEntityReference.ForIdentifier("npc-doctor"),
          InteractionIdentifier = "submit_laryngoscope",
          Kind = InteractionKind.ItemSubmission,
          KindSpecified = true,
          Submission = new InteractionSubmissionSettings
          {
            Title = "후두경 전달",
            RequiredItems = new List<InteractionItemRequirement> { new InteractionItemRequirement("laryngoscope", 1) }
          },
          CompletionSignal = "pass_laryngoscope"
        }
      };
      graph.Add(new ScenarioInteractionVisibilityNode
      {
        Identifier = "open",
        Operation = ScenarioInteractionVisibilityOperation.Show,
        PlayerScope = ScenarioInteractionVisibilityPlayerScope.ByTag,
        PlayerTags = new List<string> { "nurse_b", "nurse_a" },
        TagMatchMode = ScenarioConditionMatchMode.Any,
        Targets = new List<ScenarioInteractionTarget>
        {
          new ScenarioInteractionTarget { Entity = ScenarioEntityReference.ForIdentifier("patient_a"), InteractionIdentifier = "assess_vital" },
          new ScenarioInteractionTarget { Entity = ScenarioEntityReference.ForTag("cpr_target"), InteractionIdentifier = "compress" }
        },
        NextIdentifier = "gate"
      });
      graph.Add(new ScenarioValidatorNode
      {
        Identifier = "gate",
        WaitForCondition = true,
        RootConditions = new List<ScenarioValidatorRootCondition>
        {
          new ScenarioValidatorRootCondition
          {
            Condition = ScenarioValidatorCondition.Conditions,
            PlayerScope = ScenarioValidatorPlayerScope.Any,
            MatchMode = ScenarioValidatorMatchMode.All,
            Conditions = new List<ScenarioCondition> { ScenarioCondition.PlayerTag("nurse_b"), ScenarioCondition.Raised("check_vital_patient_a") }
          }
        }
      });

      string json = ScenarioGraphLoader.SaveToJson(graph, validateWithSchema: true);
      var reloaded = ScenarioGraphLoader.LoadFromJson(json, validateWithSchema: true);

      Assert.That(reloaded.Interactions, Has.Count.EqualTo(2));
      var assess = reloaded.Interactions[0];
      Assert.That(assess.Entity.Identifier, Is.EqualTo("patient_a"));
      Assert.That(assess.Display.Text, Is.EqualTo("활력징후 사정"));
      Assert.That(assess.Display.Priority, Is.EqualTo(10));
      Assert.That(assess.Display.PrioritySpecified, Is.True);
      Assert.That(assess.AfterInteract, Is.EqualTo(InteractionAfterInteract.HideForPlayer));
      Assert.That(assess.InitialVisibleSpecified, Is.True);
      Assert.That(assess.VisibilityConditions, Has.Count.EqualTo(3));
      Assert.That(assess.VisibilityConditions[1].CompletionCriteriaIdentifier, Is.EqualTo("assess-vital-patient-a"));
      var group = assess.VisibilityConditions[2];
      Assert.That(group.Type, Is.EqualTo(ScenarioConditionType.Group));
      Assert.That(group.MatchMode, Is.EqualTo(ScenarioConditionMatchMode.Any));
      Assert.That(group.Conditions[1].Entity.Tag, Is.EqualTo("patient"));

      var submission = reloaded.Interactions[1];
      Assert.That(submission.Kind, Is.EqualTo(InteractionKind.ItemSubmission));
      Assert.That(submission.Submission.RequiredItems[0].ItemIdentifier, Is.EqualTo("laryngoscope"));

      var node = (ScenarioInteractionVisibilityNode)reloaded.Nodes["open"];
      Assert.That(node.PlayerScope, Is.EqualTo(ScenarioInteractionVisibilityPlayerScope.ByTag));
      Assert.That(node.PlayerTags, Is.EqualTo(new[] { "nurse_b", "nurse_a" }));
      Assert.That(node.Targets[1].Entity.Tag, Is.EqualTo("cpr_target"));

      var gate = (ScenarioValidatorNode)reloaded.Nodes["gate"];
      Assert.That(gate.RootConditions[0].Condition, Is.EqualTo(ScenarioValidatorCondition.Conditions));
      Assert.That(gate.RootConditions[0].Conditions, Has.Count.EqualTo(2));
    }

    [Test]
    public void LegacyActingNpcInteractionsAreConvertedToInteractionsSection()
    {
      const string json = @"{
        ""identifier"": ""legacy-acting-npc"",
        ""defaultEntrypoint"": ""start"",
        ""actingNpcs"": [
          {
            ""identifier"": ""npc-doctor"",
            ""presetIdentifier"": ""doctor"",
            ""interactions"": [
              { ""identifier"": ""submit-et-tube"", ""interactionType"": ""ItemSubmission"", ""displayText"": ""기관내관 전달"",
                ""requiredItems"": [ { ""itemIdentifier"": ""et_tube"", ""count"": 1 } ],
                ""completionSignalIdentifier"": ""sig.pass_et_tube"", ""enabled"": false },
              { ""identifier"": ""talk"", ""interactionType"": ""Signal"", ""completionSignalIdentifier"": ""talk_start"" }
            ]
          }
        ],
        ""nodes"": {
          ""start"": { ""identifier"": ""start"", ""nodeType"": ""Dialogue"", ""speakerName"": ""system"", ""dialogueContent"": ""start"" }
        }
      }";

      LogAssert.Expect(LogType.Warning, new Regex("deprecated actingNpcs\\[\\]\\.interactions"));
      var graph = ScenarioGraphLoader.LoadFromJson(json, validateWithSchema: true);

      Assert.That(graph.ActingNpcs[0].Interactions, Is.Empty, "옛 목록은 비워져 중복 핸들러를 만들지 않아야 한다");
      Assert.That(graph.Interactions, Has.Count.EqualTo(2));
      var submission = graph.Interactions.First(value => value.InteractionIdentifier == "submit-et-tube");
      Assert.That(submission.Entity.Identifier, Is.EqualTo("npc-doctor"));
      Assert.That(submission.Kind, Is.EqualTo(InteractionKind.ItemSubmission));
      Assert.That(submission.InitialVisible, Is.False);
      Assert.That(submission.AfterInteract, Is.EqualTo(InteractionAfterInteract.HideForAll));
      Assert.That(submission.Submission.RequiredItems[0].ItemIdentifier, Is.EqualTo("et_tube"));
      var talk = graph.Interactions.First(value => value.InteractionIdentifier == "talk");
      Assert.That(talk.Kind, Is.EqualTo(InteractionKind.Signal));
      Assert.That(talk.InitialVisible, Is.True);

      // 다시 저장하면 새 형식만 남는다.
      string saved = ScenarioGraphLoader.SaveToJson(graph, validateWithSchema: true);
      Assert.That(saved, Does.Contain("\"interactions\""));
      Assert.That(saved, Does.Not.Contain("\"interactionType\""));
    }

    [Test]
    public void VisibilityNodeWithoutTargetsIsRejected()
    {
      const string json = @"{
        ""identifier"": ""bad-visibility"",
        ""defaultEntrypoint"": ""v"",
        ""nodes"": {
          ""v"": { ""identifier"": ""v"", ""nodeType"": ""InteractionVisibility"", ""operation"": ""Show"", ""targets"": [] }
        }
      }";
      Assert.Throws<System.Text.Json.JsonException>(() => ScenarioGraphLoader.LoadFromJson(json, validateWithSchema: false));
    }

    [Test]
    public void EntityPresetSpawnTagsRoundTrip()
    {
      var graph = new ScenarioGraph { Identifier = "spawn-tags", DefaultEntrypoint = "spawn" };
      graph.Add(new ScenarioEntityPresetSpawnNode
      {
        Identifier = "spawn",
        PresetIdentifier = "patient-b",
        SpawnedEntityIdentifier = "patient_b",
        Tags = new List<string> { "patient", "cpr_target" }
      });
      string json = ScenarioGraphLoader.SaveToJson(graph, validateWithSchema: true);
      var reloaded = ScenarioGraphLoader.LoadFromJson(json, validateWithSchema: true);
      Assert.That(((ScenarioEntityPresetSpawnNode)reloaded.Nodes["spawn"]).Tags, Is.EqualTo(new[] { "patient", "cpr_target" }));
    }
  }
}
