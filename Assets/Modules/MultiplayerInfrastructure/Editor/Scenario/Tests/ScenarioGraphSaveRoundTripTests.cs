using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using MultiplayerInfrastructure.Editor;
using MultiplayerInfrastructure.Scenario;
using NUnit.Framework;
using UnityEngine;

namespace MultiplayerInfrastructure.Tests.Scenario
{
  /// <summary>
  /// "여는 데 성공한 그래프를 다시 저장하면 검증에 실패하는" 문제 상황의 회귀 테스트.
  ///
  /// 저장 경로(<see cref="ScenarioGraphLoader.SaveToJson"/>)는 파일 원문이 아니라
  /// DTO 에서 재생성된 JSON 을 스키마로 검증한다. 따라서 에디터에서 만들 수 있는
  /// 모든 합법적 그래프 상태(링크 해제 포함)가 스키마 유효한 JSON 을 산출해야 한다.
  /// </summary>
  public sealed class ScenarioGraphSaveRoundTripTests
  {
    [Test]
    public void SchemaReloadCanValidateMoreThanOnce()
    {
      const string json = @"{
        ""identifier"": ""schema-reload-regression"",
        ""defaultEntrypoint"": ""start"",
        ""nodes"": {
          ""start"": {
            ""identifier"": ""start"",
            ""nodeType"": ""Dialogue"",
            ""speakerName"": ""system"",
            ""dialogueContent"": ""start""
          }
        }
      }";

      ScenarioGraphLoader.ReloadSchemaForEditor();
      Assert.DoesNotThrow(() => ScenarioGraphLoader.LoadFromJson(json, validateWithSchema: true));

      ScenarioGraphLoader.ReloadSchemaForEditor();
      Assert.DoesNotThrow(() => ScenarioGraphLoader.LoadFromJson(json, validateWithSchema: true));
    }

    [Test]
    public void ItemSubmissionConfigSavesWithSchemaValidation()
    {
      var graph = new ScenarioGraph { Identifier = "item-submission-schema", DefaultEntrypoint = "configure-submission" };
      graph.Add(new ScenarioItemSubmissionConfigNode
      {
        Identifier = "configure-submission",
        PresetIdentifier = "submission-preset",
        TargetIdentifier = "guide-submission",
        RequiredItems = new List<ScenarioItemRequirement>
        {
          new ScenarioItemRequirement { ItemIdentifier = "handy_clock", Count = 1 }
        },
        CompletionSignalIdentifier = "clock-submitted"
      });

      Assert.DoesNotThrow(() => ScenarioGraphLoader.SaveToJson(graph, validateWithSchema: true));
    }

    [Test]
    public void ClientSignalSpecificationSavesAndRoundTripsWithSchemaValidation()
    {
      var graph = new ScenarioGraph
      {
        Identifier = "client-signal-specification",
        DefaultEntrypoint = "start",
        ClientSignalIdentifiers = new[] { "sig.assess_patient" },
        ClientSignalPrefixes = new[] { "sig.zone_player_" }
      };
      graph.Add(new ScenarioDialogueNode { Identifier = "start", SpeakerName = "system", DialogueContent = "start" });

      string json = ScenarioGraphLoader.SaveToJson(graph, validateWithSchema: true);
      var reloaded = ScenarioGraphLoader.LoadFromJson(json, validateWithSchema: true);

      Assert.That(reloaded.ClientSignalIdentifiers, Is.EqualTo(new[] { "sig.assess_patient" }));
      Assert.That(reloaded.ClientSignalPrefixes, Is.EqualTo(new[] { "sig.zone_player_" }));
    }

    [Test]
    public void TutorialScenarioPassesSchemaValidation()
    {
      var json = System.IO.File.ReadAllText(
        "Assets/Modules/TriageTrainer/Resources/Scenario/tutorial.scenario.json");

      Assert.DoesNotThrow(() => ScenarioGraphLoader.LoadFromJson(json, validateWithSchema: true));
    }

    [Test]
    public void TtsVoicePresetStringLoadsAndRoundTrips()
    {
      const string sourceJson = @"{
        ""identifier"": ""tts-voice-preset"",
        ""defaultEntrypoint"": ""start"",
        ""nodes"": {
          ""start"": {
            ""identifier"": ""start"",
            ""nodeType"": ""Dialogue"",
            ""speakerName"": ""시스템"",
            ""dialogueContent"": ""안내 문구"",
            ""playTTS"": true,
            ""ttsVoiceProfile"": { ""preset"": ""F3"" }
          }
        }
      }";

      var graph = ScenarioGraphLoader.LoadFromJson(sourceJson, validateWithSchema: true);
      var dialogue = (ScenarioDialogueNode)graph.Nodes["start"];

      Assert.That(dialogue.TtsVoiceProfile.Preset, Is.EqualTo(TTSVoiceStyle.F3));

      var savedJson = ScenarioGraphLoader.SaveToJson(graph, validateWithSchema: true);
      using var savedDocument = JsonDocument.Parse(savedJson);
      var savedPreset = savedDocument.RootElement
        .GetProperty("nodes")
        .GetProperty("start")
        .GetProperty("ttsVoiceProfile")
        .GetProperty("preset")
        .GetString();

      Assert.That(savedPreset, Is.EqualTo("F3"));
      Assert.That(
        ((ScenarioDialogueNode)ScenarioGraphLoader
          .LoadFromJson(savedJson, validateWithSchema: true)
          .Nodes["start"])
        .TtsVoiceProfile.Preset,
        Is.EqualTo(TTSVoiceStyle.F3));
    }

    [Test]
    public void ScenarioActingNpcsSaveAndRoundTrip()
    {
      var graph = new ScenarioGraph
      {
        Identifier = "actingNpc-round-trip",
        DefaultEntrypoint = "spawn-doctor",
        ActingNpcs = new List<ScenarioActingNpcDefinition>
        {
          new ScenarioActingNpcDefinition
          {
            Identifier = "npc_doctor",
            PresetIdentifier = "npc_doctor_preset",
            SpawnOnStart = false,
            DisplayName = "담당 의사",
            PositionX = 1f,
            RotationY = 180f,
            Interactions = new List<ScenarioActingNpcInteractionDefinition>
            {
              new ScenarioActingNpcInteractionDefinition
              {
                Identifier = "doctor_submission",
                InteractionType = ScenarioActingNpcInteractionType.ItemSubmission,
                DisplayText = "물품 전달",
                RequiredItems = new List<ScenarioActingNpcItemRequirement>
                {
                  new ScenarioActingNpcItemRequirement { ItemIdentifier = "laryngoscope", Count = 1 }
                },
                CompletionSignalIdentifier = "sig.doctor-item-received"
              },
              new ScenarioActingNpcInteractionDefinition
              {
                Identifier = "doctor_talk",
                InteractionType = ScenarioActingNpcInteractionType.Signal,
                DisplayText = "말 걸기",
                CompletionSignalIdentifier = "doctor-talked"
              }
            }
          }
        }
      };
      graph.Add(new ScenarioEntityPresetSpawnNode
      {
        Identifier = "spawn-doctor",
        ActingNpcIdentifier = "npc_doctor",
        RotationY = -90f,
        NextIdentifier = "start"
      });
      graph.Add(new ScenarioDialogueNode { Identifier = "start", SpeakerName = "system", DialogueContent = "시작" });

      var json = ScenarioGraphLoader.SaveToJson(graph, validateWithSchema: true);
      var reloaded = ScenarioGraphLoader.LoadFromJson(json, validateWithSchema: true);

      Assert.That(reloaded.ActingNpcs, Has.Count.EqualTo(1));
      Assert.That(reloaded.ActingNpcs[0].Identifier, Is.EqualTo("npc_doctor"));
      Assert.That(reloaded.ActingNpcs[0].RotationY, Is.EqualTo(180f));
      Assert.That(((ScenarioEntityPresetSpawnNode)reloaded.Nodes["spawn-doctor"]).ActingNpcIdentifier,
        Is.EqualTo("npc_doctor"));
      Assert.That(((ScenarioEntityPresetSpawnNode)reloaded.Nodes["spawn-doctor"]).RotationY,
        Is.EqualTo(-90f));
      Assert.That(reloaded.ActingNpcs[0].Interactions, Has.Count.EqualTo(2));
      Assert.That(
        reloaded.ActingNpcs[0].Interactions[0].RequiredItems[0].ItemIdentifier,
        Is.EqualTo("laryngoscope"));
      Assert.That(reloaded.ActingNpcs[0].Interactions[1].InteractionType,
        Is.EqualTo(ScenarioActingNpcInteractionType.Signal));
      Assert.That(reloaded.ActingNpcs[0].Interactions[1].CompletionSignalIdentifier,
        Is.EqualTo("doctor-talked"));
    }

    [Test]
    public void NPCControlUpdateAndControlModesRoundTrip()
    {
      var graph = new ScenarioGraph { Identifier = "npc-control-round-trip", DefaultEntrypoint = "move" };
      graph.Add(new ScenarioNPCControlNode
      {
        Identifier = "update",
        Mode = ScenarioNPCControlMode.Update,
        NPCIdentifier = "npc",
        InteractOperation = ScenarioNPCInteractCrudOperation.Update,
        InteractableIdentifier = "talk",
        InteractEnabled = false,
        DisplayName = "???",
        ShowOverheadName = true,
        NextIdentifier = "read"
      });
      graph.Add(new ScenarioNPCControlNode
      {
        Identifier = "read",
        Mode = ScenarioNPCControlMode.Update,
        NPCIdentifier = "npc",
        InteractOperation = ScenarioNPCInteractCrudOperation.Read,
        InteractableIdentifier = "talk",
        ResultStateKey = "npc.talk.exists",
        NextIdentifier = "move"
      });
      graph.Add(new ScenarioNPCControlNode
      {
        Identifier = "move",
        Mode = ScenarioNPCControlMode.Control,
        NPCIdentifier = "npc",
        DestinationType = ScenarioMoveDestinationType.Waypoint,
        DestinationIdentifier = "destination",
        MoveMode = ScenarioMoveMode.BySpeed,
        MoveSpeed = 2f
      });

      var json = ScenarioGraphLoader.SaveToJson(graph, validateWithSchema: true);
      var reloaded = ScenarioGraphLoader.LoadFromJson(json, validateWithSchema: true);
      var update = (ScenarioNPCControlNode)reloaded.Nodes["update"];
      var read = (ScenarioNPCControlNode)reloaded.Nodes["read"];
      var move = (ScenarioNPCControlNode)reloaded.Nodes["move"];

      Assert.That(update.InteractOperation, Is.EqualTo(ScenarioNPCInteractCrudOperation.Update));
      Assert.That(update.InteractEnabled, Is.False);
      Assert.That(update.DisplayName, Is.EqualTo("???"));
      Assert.That(update.ShowOverheadName, Is.True);
      Assert.That(read.ResultStateKey, Is.EqualTo("npc.talk.exists"));
      Assert.That(move.Mode, Is.EqualTo(ScenarioNPCControlMode.Control));
      Assert.That(move.DestinationIdentifier, Is.EqualTo("destination"));
      Assert.That(move.MoveSpeed, Is.EqualTo(2f));
    }

    [Test]
    public void NPCDisplayUpdateKeepsOverheadHeightAndRegistryDisplayNameInSync()
    {
      var gameObject = new GameObject("npc-display-test");
      try
      {
        var npc = gameObject.AddComponent<MultiplayerInfrastructure.Entity.Npc>();
        npc.ApplySpawnedEntityIdentifier("npc-display-test");
        npc.SetScenarioDisplay("First", true);
        var anchor = npc.OverheadPresentationAnchor;
        Assert.That(anchor, Is.Not.Null);
        float firstHeight = anchor.localPosition.y;

        npc.SetScenarioDisplay("Second", null);

        Assert.That(anchor.localPosition.y, Is.EqualTo(firstHeight).Within(0.001f));
        Assert.That(
          MultiplayerInfrastructure.Registry.Registry.TryGetEntity(
            "npc-display-test", out var descriptor),
          Is.True);
        Assert.That(descriptor.DisplayName, Is.EqualTo("Second"));
      }
      finally
      {
        Object.DestroyImmediate(gameObject);
      }
    }

    [Test]
    public void NPCControlInteractUpdateRejectsNullEnabledValue()
    {
      const string json = @"{
        ""identifier"": ""invalid-npc-control-update"",
        ""nodes"": {
          ""update"": {
            ""identifier"": ""update"",
            ""nodeType"": ""NPCControl"",
            ""mode"": ""Update"",
            ""npcIdentifier"": ""npc"",
            ""interactOperation"": ""Update"",
            ""interactableIdentifier"": ""talk"",
            ""interactEnabled"": null
          }
        }
      }";

      Assert.Throws<ScenarioSchemaValidationException>(() =>
        ScenarioGraphLoader.LoadFromJson(json, validateWithSchema: true));
    }

    [Test]
    public void ActorInteractionsRejectEmptyRequiredItemsAndGraphWideDuplicateIdentifiers()
    {
      const string emptyItems = @"{
        ""identifier"": ""invalid-items"",
        ""defaultEntrypoint"": ""start"",
        ""actingNpcs"": [{
          ""identifier"": ""npc"",
          ""presetIdentifier"": ""npc-preset"",
          ""interactions"": [{
            ""identifier"": ""submission"",
            ""interactionType"": ""ItemSubmission"",
            ""requiredItems"": []
          }]
        }],
        ""nodes"": {
          ""start"": {
            ""identifier"": ""start"",
            ""nodeType"": ""Dialogue"",
            ""speakerName"": ""system"",
            ""dialogueContent"": ""start""
          }
        }
      }";
      Assert.Throws<ScenarioSchemaValidationException>(() =>
        ScenarioGraphLoader.LoadFromJson(emptyItems, validateWithSchema: true));

      const string duplicateInteraction = @"{
        ""identifier"": ""duplicate-interaction"",
        ""defaultEntrypoint"": ""start"",
        ""actingNpcs"": [
          {
            ""identifier"": ""npc-a"",
            ""presetIdentifier"": ""npc-preset"",
            ""interactions"": [{
              ""identifier"": ""shared"",
              ""interactionType"": ""StartScenario"",
              ""scenarioIdentifier"": ""target""
            }]
          },
          {
            ""identifier"": ""npc-b"",
            ""presetIdentifier"": ""npc-preset"",
            ""interactions"": [{
              ""identifier"": ""shared"",
              ""interactionType"": ""StartScenario"",
              ""scenarioIdentifier"": ""target""
            }]
          }
        ],
        ""nodes"": {
          ""start"": {
            ""identifier"": ""start"",
            ""nodeType"": ""Dialogue"",
            ""speakerName"": ""system"",
            ""dialogueContent"": ""start""
          }
        }
      }";
      Assert.Throws<JsonException>(() =>
        ScenarioGraphLoader.LoadFromJson(duplicateInteraction, validateWithSchema: true));
    }

    [Test]
    public void ActorSpawnNodeRejectsUnknownActingNpcIdentifier()
    {
      const string json = @"{
        ""identifier"": ""unknown-actingNpc-spawn"",
        ""defaultEntrypoint"": ""spawn"",
        ""actingNpcs"": [],
        ""nodes"": {
          ""spawn"": {
            ""identifier"": ""spawn"",
            ""nodeType"": ""EntityPresetSpawn"",
            ""actingNpcIdentifier"": ""not-declared""
          }
        }
      }";

      Assert.Throws<JsonException>(() =>
        ScenarioGraphLoader.LoadFromJson(json, validateWithSchema: true));
    }

    /// <summary>
    /// 선택지 대상이 끊긴(엣지 해제/대상 노드 삭제) 상태 — option.NextNodeIdentifier == null.
    /// 런타임은 null 을 "이 선택지는 시나리오 종료"로 취급하므로 저장도 유효해야 한다.
    /// </summary>
    [Test]
    public void DisconnectedChoiceOptionLinkSavesAndRoundTrips()
    {
      var graph = new ScenarioGraph { Identifier = "choice-null-link", DefaultEntrypoint = "choice" };
      graph.Add(new ScenarioChoiceNode
      {
        Identifier = "choice",
        SpeakerName = "system",
        DialogueContent = "선택하세요",
        Options = new List<ScenarioChoiceOption>
        {
          new ScenarioChoiceOption { DisplayText = "A", NextNodeIdentifier = null },
          new ScenarioChoiceOption { DisplayText = "B", NextNodeIdentifier = "end" }
        }
      });
      graph.Add(new ScenarioDialogueNode { Identifier = "end", SpeakerName = "system", DialogueContent = "종료" });

      var json = ScenarioGraphLoader.SaveToJson(graph, validateWithSchema: true);

      var reloaded = ScenarioGraphLoader.LoadFromJson(json, validateWithSchema: true);
      var reloadedChoice = (ScenarioChoiceNode)reloaded.Nodes["choice"];
      Assert.That(reloadedChoice.Options[0].NextNodeIdentifier, Is.Null);
      Assert.That(reloadedChoice.Options[1].NextNodeIdentifier, Is.EqualTo("end"));
    }

    /// <summary>
    /// 퀴즈 정답 링크가 끊긴 상태 — OnCorrectNextIdentifier == null.
    /// 런타임은 null 이면 NextIdentifier 로 폴백하므로 저장도 유효해야 한다.
    /// </summary>
    [Test]
    public void DisconnectedQuizCorrectLinkSavesAndRoundTrips()
    {
      var graph = new ScenarioGraph { Identifier = "quiz-null-link", DefaultEntrypoint = "quiz" };
      graph.Add(new ScenarioQuizNode
      {
        Identifier = "quiz",
        Question = "질문",
        Options = new List<string> { "X", "Y" },
        CorrectIndex = 0,
        OnCorrectNextIdentifier = null,
        OnIncorrectNextIdentifier = null,
        NextIdentifier = "fallback"
      });
      graph.Add(new ScenarioDialogueNode { Identifier = "fallback", SpeakerName = "system", DialogueContent = "폴백" });

      var json = ScenarioGraphLoader.SaveToJson(graph, validateWithSchema: true);

      var reloaded = ScenarioGraphLoader.LoadFromJson(json, validateWithSchema: true);
      var reloadedQuiz = (ScenarioQuizNode)reloaded.Nodes["quiz"];
      Assert.That(reloadedQuiz.OnCorrectNextIdentifier, Is.Null);
      Assert.That(reloadedQuiz.NextIdentifier, Is.EqualTo("fallback"));
    }

    /// <summary>
    /// 병렬 브랜치가 끊기면 에디터는 identifier 를 null 대신 branch_N placeholder 로 채운다.
    /// 그 상태가 스키마 유효하게 저장되어야 한다.
    /// </summary>
    [Test]
    public void DisconnectedParallelBranchPlaceholderSavesAndRoundTrips()
    {
      var graph = new ScenarioGraph { Identifier = "parallel-placeholder", DefaultEntrypoint = "parallel" };
      graph.Add(new ScenarioParallelNode
      {
        Identifier = "parallel",
        Branches = new List<ScenarioParallelBranch>
        {
          new ScenarioParallelBranch { Identifier = "stage_a" },
          new ScenarioParallelBranch { Identifier = "branch_2" } // placeholder(미연결) 상태
        },
        NextIdentifier = "merge"
      });
      graph.Add(new ScenarioDialogueNode { Identifier = "stage_a", SpeakerName = "system", DialogueContent = "A" });
      graph.Add(new ScenarioDialogueNode { Identifier = "merge", SpeakerName = "system", DialogueContent = "합류" });

      var json = ScenarioGraphLoader.SaveToJson(graph, validateWithSchema: true);

      var reloaded = ScenarioGraphLoader.LoadFromJson(json, validateWithSchema: true);
      var reloadedParallel = (ScenarioParallelNode)reloaded.Nodes["parallel"];
      Assert.That(reloadedParallel.Branches[1].Identifier, Is.EqualTo("branch_2"));
    }

    /// <summary>
    /// 병렬 노드의 nextIdentifier 는 저장 후에도 보존되어야 한다.
    /// (과거 Parallel 전용 수동 Writer 가 nextIdentifier 를 생략해 저장마다
    /// 다음 링크가 조용히 사라지던 데이터 유실 버그의 회귀 테스트.)
    /// </summary>
    [Test]
    public void ParallelNextIdentifierSurvivesSave()
    {
      var graph = new ScenarioGraph { Identifier = "parallel-next", DefaultEntrypoint = "parallel" };
      graph.Add(new ScenarioParallelNode
      {
        Identifier = "parallel",
        Branches = new List<ScenarioParallelBranch>
        {
          new ScenarioParallelBranch { Identifier = "stage_a" },
          new ScenarioParallelBranch { Identifier = "stage_b" }
        },
        NextIdentifier = "after_parallel"
      });
      graph.Add(new ScenarioDialogueNode { Identifier = "stage_a", SpeakerName = "system", DialogueContent = "A" });
      graph.Add(new ScenarioDialogueNode { Identifier = "stage_b", SpeakerName = "system", DialogueContent = "B" });
      graph.Add(new ScenarioDialogueNode { Identifier = "after_parallel", SpeakerName = "system", DialogueContent = "다음" });

      var json = ScenarioGraphLoader.SaveToJson(graph, validateWithSchema: true);

      var reloaded = ScenarioGraphLoader.LoadFromJson(json, validateWithSchema: true);
      var reloadedParallel = (ScenarioParallelNode)reloaded.Nodes["parallel"];
      Assert.That(reloadedParallel.NextIdentifier, Is.EqualTo("after_parallel"),
          "병렬 노드의 nextIdentifier 가 저장 라운드트립에서 유실되면 안 된다.");
    }

    /// <summary>
    /// 화면에 그리지 않는 Parallel continuation은 표시 간선 교차 계산에 섞지 않는다.
    /// continuation은 별도의 레이어 제약으로 처리해야 실제 포트의 교차 점수와 화면이
    /// 일치한다.
    /// </summary>
    [Test]
    public void AutoLayoutVisibleTargetsContainParallelBranchesOnly()
    {
      var parallel = new ScenarioParallelNode
      {
        Identifier = "parallel",
        Branches = new List<ScenarioParallelBranch>
        {
          new ScenarioParallelBranch { Identifier = "stage_a" },
          new ScenarioParallelBranch { Identifier = "stage_b" }
        },
        NextIdentifier = "after_parallel"
      };
      var method = typeof(ScenarioGraphAuthoringWindow).GetMethod(
        "GetVisibleOutgoingTargets",
        BindingFlags.Static | BindingFlags.NonPublic);

      Assert.That(method, Is.Not.Null);
      var targets = ((IEnumerable)method.Invoke(null, new object[] { parallel }))
        .Cast<string>()
        .ToArray();

      Assert.That(targets, Is.EqualTo(new[] { "stage_a", "stage_b" }));
    }

    [Test]
    public void AutoLayoutPreservesChoicePortsThatShareTarget()
    {
      var choice = new ScenarioChoiceNode
      {
        Identifier = "choice",
        Options = new List<ScenarioChoiceOption>
        {
          new ScenarioChoiceOption { DisplayText = "A", NextNodeIdentifier = "retry" },
          new ScenarioChoiceOption { DisplayText = "B", NextNodeIdentifier = "retry" },
          new ScenarioChoiceOption { DisplayText = "C", NextNodeIdentifier = "success" }
        }
      };
      var method = typeof(ScenarioGraphAuthoringWindow).GetMethod(
        "GetVisibleOutgoingTargets",
        BindingFlags.Static | BindingFlags.NonPublic);

      Assert.That(method, Is.Not.Null);
      var targets = ((IEnumerable)method.Invoke(null, new object[] { choice }))
        .Cast<string>()
        .ToArray();

      Assert.That(targets, Is.EqualTo(new[] { "retry", "retry", "success" }));
    }

    [Test]
    public void AutoLayoutPlacesParallelContinuationAfterDeepestBranch()
    {
      var layer = new Dictionary<string, int>
      {
        ["parallel"] = 0,
        ["branch_start"] = 1,
        ["branch_end"] = 2,
        ["continuation"] = 1,
        ["after"] = 2
      };
      var constraintForward = new Dictionary<string, List<string>>
      {
        ["parallel"] = new List<string> { "continuation", "branch_start" },
        ["branch_start"] = new List<string> { "branch_end" },
        ["branch_end"] = new List<string>(),
        ["continuation"] = new List<string> { "after" },
        ["after"] = new List<string>()
      };
      var visibleForward = new Dictionary<string, List<string>>
      {
        ["parallel"] = new List<string> { "branch_start" },
        ["branch_start"] = new List<string> { "branch_end" },
        ["branch_end"] = new List<string>(),
        ["continuation"] = new List<string> { "after" },
        ["after"] = new List<string>()
      };
      var continuationByParallel = new Dictionary<string, string>
      {
        ["parallel"] = "continuation"
      };
      var branchesByParallel = new Dictionary<string, List<string>>
      {
        ["parallel"] = new List<string> { "branch_start" }
      };
      var method = typeof(ScenarioGraphAuthoringWindow).GetMethod(
        "PushParallelContinuations",
        BindingFlags.Static | BindingFlags.NonPublic);

      Assert.That(method, Is.Not.Null);
      method.Invoke(null, new object[]
      {
        layer,
        constraintForward,
        visibleForward,
        continuationByParallel,
        branchesByParallel
      });

      Assert.That(layer["continuation"], Is.EqualTo(3));
      Assert.That(layer["after"], Is.EqualTo(4));
    }

    [Test]
    public void AutoLayoutStacksVisibleComponentsVertically()
    {
      var identifiers = new[] { "a", "b", "c", "d" };
      var outgoing = new Dictionary<string, List<string>>
      {
        ["a"] = new List<string> { "b" },
        ["b"] = new List<string>(),
        ["c"] = new List<string> { "d" },
        ["d"] = new List<string>()
      };
      var layer = new Dictionary<string, int>
      {
        ["a"] = 0,
        ["b"] = 1,
        ["c"] = 0,
        ["d"] = 1
      };
      var y = new Dictionary<string, float>
      {
        ["a"] = 0f,
        ["b"] = 0f,
        ["c"] = 0f,
        ["d"] = 0f
      };
      var method = typeof(ScenarioGraphAuthoringWindow).GetMethod(
        "PackVisibleComponents",
        BindingFlags.Static | BindingFlags.NonPublic);

      Assert.That(method, Is.Not.Null);
      var packed = (Dictionary<string, Vector2>)method.Invoke(null, new object[]
      {
        identifiers,
        outgoing,
        layer,
        y
      });

      Assert.That(packed["a"].x, Is.EqualTo(0f));
      Assert.That(packed["b"].x, Is.EqualTo(420f));
      Assert.That(packed["c"].x, Is.EqualTo(0f));
      Assert.That(packed["d"].x, Is.EqualTo(420f));
      Assert.That(packed["c"].y - packed["a"].y, Is.GreaterThanOrEqualTo(400f));
    }

    [Test]
    public void AutoLayoutAssignsOneLaneToUnambiguousLinearPath()
    {
      var layers = new Dictionary<int, List<string>>
      {
        [0] = new List<string> { "a" },
        [1] = new List<string> { "b" },
        [2] = new List<string> { "c" }
      };
      var forward = new Dictionary<string, List<string>>
      {
        ["a"] = new List<string> { "b" },
        ["b"] = new List<string> { "c" },
        ["c"] = new List<string>()
      };
      var incoming = new Dictionary<string, List<string>>
      {
        ["a"] = new List<string>(),
        ["b"] = new List<string> { "a" },
        ["c"] = new List<string> { "b" }
      };
      var layer = layers
        .SelectMany(pair => pair.Value.Select(id => new { id, layer = pair.Key }))
        .ToDictionary(item => item.id, item => item.layer);
      var method = typeof(ScenarioGraphAuthoringWindow).GetMethod(
        "AssignVerticalCoordinates",
        BindingFlags.Static | BindingFlags.NonPublic);

      Assert.That(method, Is.Not.Null);
      var y = (Dictionary<string, float>)method.Invoke(
        null,
        new object[] { layers, layer, forward, incoming });

      Assert.That(y["b"], Is.EqualTo(y["a"]));
      Assert.That(y["c"], Is.EqualTo(y["a"]));
    }

    [Test]
    public void AutoLayoutMovesAlignmentBlocksWithoutOverlappingOccupiedLane()
    {
      var layers = new Dictionary<int, List<string>>
      {
        [0] = new List<string> { "a" },
        [1] = new List<string> { "b", "occupied" }
      };
      var forward = new Dictionary<string, List<string>>
      {
        ["a"] = new List<string> { "b" },
        ["b"] = new List<string>(),
        ["occupied"] = new List<string>()
      };
      var incoming = new Dictionary<string, List<string>>
      {
        ["a"] = new List<string>(),
        ["b"] = new List<string> { "a" },
        ["occupied"] = new List<string>()
      };
      var layer = layers
        .SelectMany(pair => pair.Value.Select(id => new { id, layer = pair.Key }))
        .ToDictionary(item => item.id, item => item.layer);
      var method = typeof(ScenarioGraphAuthoringWindow).GetMethod(
        "AssignVerticalCoordinates",
        BindingFlags.Static | BindingFlags.NonPublic);

      Assert.That(method, Is.Not.Null);
      var y = (Dictionary<string, float>)method.Invoke(
        null,
        new object[] { layers, layer, forward, incoming });

      Assert.That(y["b"], Is.EqualTo(y["a"]));
      Assert.That(y["occupied"] - y["b"], Is.GreaterThanOrEqualTo(240f));
    }

    [Test]
    public void AutoLayoutVerticalRangeIsBoundedByDensestLayer()
    {
      var layers = new Dictionary<int, List<string>>
      {
        [0] = new List<string> { "a" },
        [1] = new List<string> { "b", "side_1", "side_2" },
        [2] = new List<string> { "c", "side_3" }
      };
      var layer = layers
        .SelectMany(pair => pair.Value.Select(id => new { id, layer = pair.Key }))
        .ToDictionary(item => item.id, item => item.layer);
      var forward = new Dictionary<string, List<string>>
      {
        ["a"] = new List<string> { "b" },
        ["b"] = new List<string> { "c" },
        ["c"] = new List<string>(),
        ["side_1"] = new List<string> { "side_3" },
        ["side_2"] = new List<string>(),
        ["side_3"] = new List<string>()
      };
      var incoming = new Dictionary<string, List<string>>
      {
        ["a"] = new List<string>(),
        ["b"] = new List<string> { "a" },
        ["c"] = new List<string> { "b" },
        ["side_1"] = new List<string>(),
        ["side_2"] = new List<string>(),
        ["side_3"] = new List<string> { "side_1" }
      };
      var method = typeof(ScenarioGraphAuthoringWindow).GetMethod(
        "AssignVerticalCoordinates",
        BindingFlags.Static | BindingFlags.NonPublic);

      Assert.That(method, Is.Not.Null);
      var y = (Dictionary<string, float>)method.Invoke(
        null,
        new object[] { layers, layer, forward, incoming });

      Assert.That(y.Values.Max() - y.Values.Min(), Is.LessThanOrEqualTo(480f));
    }

    /// <summary>
    /// Validator 의 기본 matchMode(All) 는 저장 시 생략되어야 한다.
    /// (과거 "기본값 → null" 매핑이 null 생략 없이 그대로 기록되어
    /// 여는 데 성공한 모든 Validator 그래프의 재저장이 실패하던 버그의 회귀 테스트.)
    /// </summary>
    [Test]
    public void ValidatorDefaultMatchModeSavesCleanly()
    {
      var graph = new ScenarioGraph { Identifier = "validator-default", DefaultEntrypoint = "validator" };
      graph.Add(new ScenarioValidatorNode
      {
        Identifier = "validator",
        RootConditions = new List<ScenarioValidatorRootCondition>
        {
          new ScenarioValidatorRootCondition
          {
            Condition = ScenarioValidatorCondition.PlayerCountGreaterThanOrEqual,
            TargetCount = 1
            // MatchMode 미지정 → 기본 All → 저장 시 생략되어야 함
          }
        }
      });

      var json = ScenarioGraphLoader.SaveToJson(graph, validateWithSchema: true);

      var reloaded = ScenarioGraphLoader.LoadFromJson(json, validateWithSchema: true);
      var reloadedValidator = (ScenarioValidatorNode)reloaded.Nodes["validator"];
      Assert.That(reloadedValidator.RootConditions[0].MatchMode, Is.EqualTo(ScenarioValidatorMatchMode.All));
    }

    /// <summary>
    /// 외부 quest definition을 사용하는 QuestControl은 inline quest가 없어도 유효하다.
    /// 에디터에서 이 노드를 선택한 뒤에도 quest:null이 유지되어야 한다.
    /// </summary>
    [Test]
    public void QuestControlWithDefinitionKeepsNullInlineQuestAndSaves()
    {
      var graph = new ScenarioGraph { Identifier = "quest-definition-only", DefaultEntrypoint = "quest" };
      graph.Add(new ScenarioQuestControlNode
      {
        Identifier = "quest",
        Operation = ScenarioQuestOperationType.Add,
        FailureStrategy = ScenarioQuestFailureStrategy.Overwrite,
        QuestDefinitionIdentifier = "tutorial-quest-crafting",
        Quest = null
      });

      var json = ScenarioGraphLoader.SaveToJson(graph, validateWithSchema: true);
      var reloaded = ScenarioGraphLoader.LoadFromJson(json, validateWithSchema: true);
      var reloadedQuest = (ScenarioQuestControlNode)reloaded.Nodes["quest"];

      Assert.That(reloadedQuest.QuestDefinitionIdentifier, Is.EqualTo("tutorial-quest-crafting"));
      Assert.That(reloadedQuest.Quest, Is.Null);
    }

    /// <summary>
    /// 저장→로드→저장 멱등성: 한 번 저장한 결과는 다시 저장해도 검증이 통과해야 한다.
    /// </summary>
    [Test]
    public void SaveLoadSaveIsIdempotentAndValid()
    {
      var graph = new ScenarioGraph { Identifier = "round-trip", DefaultEntrypoint = "start" };
      graph.Add(new ScenarioDialogueNode { Identifier = "start", SpeakerName = "system", DialogueContent = "시작", NextIdentifier = "choice" });
      graph.Add(new ScenarioChoiceNode
      {
        Identifier = "choice",
        SpeakerName = "system",
        DialogueContent = "선택",
        Options = new List<ScenarioChoiceOption>
        {
          new ScenarioChoiceOption { DisplayText = "A", NextNodeIdentifier = "end" },
          new ScenarioChoiceOption { DisplayText = "B", NextNodeIdentifier = null }
        }
      });
      graph.Add(new ScenarioDialogueNode { Identifier = "end", SpeakerName = "system", DialogueContent = "끝" });

      var firstSave = ScenarioGraphLoader.SaveToJson(graph, validateWithSchema: true);
      var loaded = ScenarioGraphLoader.LoadFromJson(firstSave, validateWithSchema: true);
      var secondSave = ScenarioGraphLoader.SaveToJson(loaded, validateWithSchema: true);

      var reloaded = ScenarioGraphLoader.LoadFromJson(secondSave, validateWithSchema: true);
      Assert.That(reloaded.Nodes.Count, Is.EqualTo(3));
    }

    [Test]
    public void ActiveRoleRosterOptionsSaveAndRoundTrip()
    {
      var graph = new ScenarioGraph
      {
        Identifier = "active-role-roster",
        DefaultEntrypoint = "counter",
        ActiveRoleTags = new[] { "nurse_a", "nurse_b" },
        SkipAbsentRoleBranches = true
      };
      graph.Add(new ScenarioSignalCounterNode
      {
        Identifier = "counter",
        CounterIdentifier = "arrivals",
        SourceSignalPrefix = "arrival_",
        Threshold = 4,
        UseActiveRoleRosterThreshold = true,
        OutputSignalIdentifier = "arrived"
      });

      var json = ScenarioGraphLoader.SaveToJson(graph, validateWithSchema: true);
      var reloaded = ScenarioGraphLoader.LoadFromJson(json, validateWithSchema: true);

      Assert.That(reloaded.ActiveRoleTags, Is.EqualTo(new[] { "nurse_a", "nurse_b" }));
      Assert.That(reloaded.SkipAbsentRoleBranches, Is.True);
      Assert.That(((ScenarioSignalCounterNode)reloaded.Nodes["counter"]).UseActiveRoleRosterThreshold, Is.True);
    }

    [Test]
    public void ScenarioWaypointsSaveAndRoundTrip()
    {
      var graph = new ScenarioGraph
      {
        Identifier = "waypoint-round-trip",
        DefaultEntrypoint = "start",
        Waypoints = new List<ScenarioWaypointDefinition>
        {
          new ScenarioWaypointDefinition
          {
            Identifier = "treatment-room",
            PositionX = 12.5f,
            PositionY = 1f,
            PositionZ = -3.25f,
            RotationY = 90f,
            DespawnOnScenarioEnd = false
          }
        }
      };
      graph.Add(new ScenarioDialogueNode { Identifier = "start", SpeakerName = "system", DialogueContent = "시작" });

      var json = ScenarioGraphLoader.SaveToJson(graph, validateWithSchema: true);
      var reloaded = ScenarioGraphLoader.LoadFromJson(json, validateWithSchema: true);

      Assert.That(reloaded.Waypoints, Has.Count.EqualTo(1));
      Assert.That(reloaded.Waypoints[0].Identifier, Is.EqualTo("treatment-room"));
      Assert.That(reloaded.Waypoints[0].PositionX, Is.EqualTo(12.5f));
      Assert.That(reloaded.Waypoints[0].RotationY, Is.EqualTo(90f));
      Assert.That(reloaded.Waypoints[0].DespawnOnScenarioEnd, Is.False);
    }
  }
}
