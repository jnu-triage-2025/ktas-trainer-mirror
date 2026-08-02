using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MultiplayerInfrastructure.Scenario.Requirements;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using NUnit.Framework;

namespace MultiplayerInfrastructure.Tests.Scenario.Requirements
{
  public sealed class ScenarioRequirementCompilerTests
  {
    [Test]
    public void ScenarioActingNpcProducesNpcAndConsumesPreset()
    {
      var graph = new ScenarioGraph
      {
        Identifier = "actingNpc-requirements",
        DefaultEntrypoint = "spawn-doctor",
        ActingNpcs = new List<ScenarioActingNpcDefinition>
        {
          new ScenarioActingNpcDefinition
          {
            Identifier = "npc_doctor",
            PresetIdentifier = "npc_doctor_preset",
            SpawnOnStart = false
          }
        }
      };
      graph.Add(new ScenarioEntityPresetSpawnNode
      {
        Identifier = "spawn-doctor",
        ActingNpcIdentifier = "npc_doctor",
        PositionSourceEntityIdentifier = "doctor-spawnpoint",
        NextIdentifier = "move"
      });
      graph.Add(new ScenarioNPCControlNode
      {
        Identifier = "move",
        Mode = ScenarioNPCControlMode.Control,
        NPCIdentifier = "npc_doctor",
        DestinationType = ScenarioMoveDestinationType.Position
      });

      var manifest = ScenarioRequirementCompiler.CompileInferred(graph);
      var npc = manifest.Requirements.Single(value =>
        value.Key.Equals(new ScenarioRequirementKey(ScenarioRequirementKind.Npc, "npc_doctor")));
      var preset = manifest.Requirements.Single(value =>
        value.Key.Equals(new ScenarioRequirementKey(ScenarioRequirementKind.EntityPreset, "npc_doctor_preset")));
      var spawnpoint = manifest.Requirements.Single(value =>
        value.Key.Equals(new ScenarioRequirementKey(ScenarioRequirementKind.Entity, "doctor-spawnpoint")));

      Assert.That(npc.Occurrences.Any(value =>
        value.Direction == ScenarioRequirementDirection.Produces
        && value.ExpectedSupply == ScenarioRequirementExpectedSupply.Scenario), Is.True);
      Assert.That(npc.Occurrences.Any(value =>
        value.Direction == ScenarioRequirementDirection.Consumes), Is.True);
      Assert.That(preset.Occurrences.Single().Direction, Is.EqualTo(ScenarioRequirementDirection.Consumes));
      Assert.That(preset.Capabilities, Does.Contain(ScenarioRequirementCapability.SpawnablePreset));
      Assert.That(spawnpoint.Capabilities, Does.Contain(ScenarioRequirementCapability.ProvidesPosition));
      Assert.That(spawnpoint.EffectiveAvailability, Is.EqualTo(ScenarioRequirementAvailability.WhenNodeReached));
      Assert.That(spawnpoint.Occurrences.Single().FieldPath, Is.EqualTo("positionSourceEntityIdentifier"));
      Assert.That(manifest.IsValid, Is.True);
    }

    [Test]
    public void ActingNpcUsedBeforeSpawnProducesStructuralDiagnostic()
    {
      var graph = new ScenarioGraph
      {
        Identifier = "acting-npc-order",
        DefaultEntrypoint = "move",
        ActingNpcs = new List<ScenarioActingNpcDefinition>
        {
          new ScenarioActingNpcDefinition
          {
            Identifier = "npc_doctor",
            PresetIdentifier = "npc_doctor_preset",
            SpawnOnStart = false
          }
        }
      };
      graph.Add(new ScenarioNPCControlNode
      {
        Identifier = "move",
        Mode = ScenarioNPCControlMode.Control,
        NPCIdentifier = "npc_doctor",
        DestinationType = ScenarioMoveDestinationType.Position,
        NextIdentifier = "spawn"
      });
      graph.Add(new ScenarioEntityPresetSpawnNode
      {
        Identifier = "spawn",
        ActingNpcIdentifier = "npc_doctor"
      });

      var manifest = ScenarioRequirementCompiler.CompileInferred(graph);

      Assert.That(manifest.IsValid, Is.False);
      Assert.That(manifest.Diagnostics.Select(value => value.Code), Does.Contain("SGR116"));
    }

    [Test]
    public void NpcMoveAndWaypointHighlightPreserveAllOccurrencesAndCapabilities()
    {
      var graph = new ScenarioGraph { Identifier = "compiler-test" };
      graph.Add(new ScenarioNPCControlNode
      {
        Identifier = "move",
        Mode = ScenarioNPCControlMode.Control,
        NPCIdentifier = "npc-1",
        DestinationType = ScenarioMoveDestinationType.Waypoint,
        DestinationIdentifier = " treatment-room "
      });
      graph.Add(new ScenarioQuestWaypointHighlightNode
      {
        Identifier = "highlight",
        WaypointIdentifier = "treatment-room"
      });

      var manifest = ScenarioRequirementCompiler.CompileInferred(graph);
      var npc = manifest.Requirements.Single(value => value.Key.Equals(new ScenarioRequirementKey(ScenarioRequirementKind.Npc, "npc-1")));
      var anchor = manifest.Requirements.Single(value => value.Key.Equals(new ScenarioRequirementKey(ScenarioRequirementKind.SpatialAnchor, "treatment-room")));

      Assert.That(npc.Capabilities, Does.Contain(ScenarioRequirementCapability.ResolvableNpcMoveTarget));
      Assert.That(anchor.Capabilities, Does.Contain(ScenarioRequirementCapability.ProvidesPosition));
      Assert.That(anchor.Capabilities, Does.Contain(ScenarioRequirementCapability.HighlightableWaypoint));
      Assert.That(anchor.Occurrences, Has.Count.EqualTo(2));
      Assert.That(anchor.Occurrences.Select(value => value.FieldPath), Is.EquivalentTo(new[] { "destinationIdentifier", "waypointIdentifier" }));
    }

    [Test]
    public void ScenarioWaypointProducesBeforeStartSpatialAnchor()
    {
      var graph = new ScenarioGraph
      {
        Identifier = "scenario-waypoint-requirements",
        Waypoints = new List<ScenarioWaypointDefinition>
        {
          new ScenarioWaypointDefinition { Identifier = "triage-desk" }
        }
      };
      graph.Add(new ScenarioPlayerMoveNode
      {
        Identifier = "move",
        DestinationType = ScenarioMoveDestinationType.Waypoint,
        DestinationIdentifier = "triage-desk"
      });

      var manifest = ScenarioRequirementCompiler.CompileInferred(graph);
      var waypoint = manifest.Requirements.Single(value =>
        value.Key.Equals(new ScenarioRequirementKey(ScenarioRequirementKind.SpatialAnchor, "triage-desk")));

      Assert.That(waypoint.Occurrences.Any(value =>
        value.Direction == ScenarioRequirementDirection.Produces
        && value.ExpectedSupply == ScenarioRequirementExpectedSupply.Scenario
        && value.Availability == ScenarioRequirementAvailability.BeforeScenarioStart), Is.True);
      Assert.That(waypoint.Occurrences.Any(value => value.Direction == ScenarioRequirementDirection.Consumes), Is.True);
    }

    [Test]
    public void EmptyRequiredIdentifierProducesStableDiagnostic()
    {
      var graph = new ScenarioGraph { Identifier = "invalid-identifier" };
      graph.Add(new ScenarioSoundNode { Identifier = "sound", SoundResourceIdentifier = " " });

      var manifest = ScenarioRequirementCompiler.CompileInferred(graph);

      Assert.That(manifest.IsValid, Is.False);
      Assert.That(manifest.Diagnostics.Select(value => value.Code), Does.Contain("SIR100"));
    }

    [Test]
    public void RuntimeSignalUsesUnvalidatedCardinalityAndIndeterminateSupply()
    {
      var graph = new ScenarioGraph { Identifier = "signal-test" };
      var validator = new ScenarioValidatorNode
      {
        Identifier = "validate",
        RootConditions = new List<ScenarioValidatorRootCondition>
        {
          new ScenarioValidatorRootCondition
          {
            Condition = ScenarioValidatorCondition.RegistryContains,
            ValidationRules = new List<ScenarioValidatorRule>
            {
              new ScenarioValidatorRule
              {
                RegistryType = RegistryType.RuntimeState,
                RegistryIdentifier = "sig.device-ready"
              }
            }
          }
        }
      };
      graph.Add(validator);

      var manifest = ScenarioRequirementCompiler.CompileInferred(graph);
      var signal = manifest.Requirements.Single(value => value.Kind == ScenarioRequirementKind.RuntimeSignal);

      Assert.That(signal.Cardinality.IsValidated, Is.False);
      Assert.That(signal.EffectiveAvailability, Is.EqualTo(ScenarioRequirementAvailability.WhenNodeReached));
      Assert.That(signal.Occurrences.Single().ExpectedSupply, Is.EqualTo(ScenarioRequirementExpectedSupply.Gameplay));
    }

    [Test]
    public void MultiBranchByRoleRequiresAuthoritativeScenarioExecutionService()
    {
      var graph = new ScenarioGraph { Identifier = "by-role-authority" };
      graph.Add(new ScenarioParallelNode
      {
        Identifier = "parallel",
        AllocationType = ScenarioParallelAllocationType.ByRole,
        Branches = new List<ScenarioParallelBranch>
        {
          new ScenarioParallelBranch { Identifier = "a", RequiredPlayerTags = new List<string> { "nurse_a" } },
          new ScenarioParallelBranch { Identifier = "b", RequiredPlayerTags = new List<string> { "nurse_b" } }
        }
      });

      var manifest = ScenarioRequirementCompiler.CompileInferred(graph);
      var service = manifest.Requirements.Single(value => value.Key.Equals(new ScenarioRequirementKey(
        ScenarioRequirementKind.Service, "mi.service.scenario-server-authoritative-execution")));

      Assert.That(service.EffectiveAvailability, Is.EqualTo(ScenarioRequirementAvailability.BeforeScenarioStart));
      Assert.That(service.Authority, Is.EqualTo(ScenarioRequirementAuthority.Server));
      Assert.That(service.Occurrences.Single().Usage, Is.EqualTo("parallel-by-role-authority"));
    }

    [Test]
    public void ValidatorAnyMatchModeLoadsAndRoundTrips()
    {
      const string json = @"{
        ""identifier"": ""validator-any-test"",
        ""tags"": [],
        ""nodes"": {
          ""validator"": {
            ""identifier"": ""validator"",
            ""nodeType"": ""Validator"",
            ""rootConditions"": [{
              ""condition"": ""RegistryContains"",
              ""matchMode"": ""Any"",
              ""validationRules"": [{
                ""type"": ""Registry"",
                ""condition"": ""Contains"",
                ""registryType"": ""RuntimeState"",
                ""registryIdentifier"": ""sig.one""
              }, {
                ""type"": ""Registry"",
                ""condition"": ""Contains"",
                ""registryType"": ""RuntimeState"",
                ""registryIdentifier"": ""sig.two""
              }]
            }],
            ""onFailure"": ""Ignore""
          }
        }
      }";

      var graph = ScenarioGraphLoader.LoadFromJson(json);
      var validator = (ScenarioValidatorNode)graph.Nodes["validator"];

      Assert.That(validator.RootConditions.Single().MatchMode, Is.EqualTo(ScenarioValidatorMatchMode.Any));
      Assert.That(ScenarioGraphLoader.SaveToJson(graph), Does.Contain("\"matchMode\": \"Any\""));
    }

    [Test]
    public void ValidatorDefaultMatchModeOmitsFieldWhenRoundTripped()
    {
      var graph = new ScenarioGraph { Identifier = "validator-all-test" };
      graph.Add(new ScenarioValidatorNode
      {
        Identifier = "validator",
        RootConditions = new List<ScenarioValidatorRootCondition>
        {
          new ScenarioValidatorRootCondition
          {
            Condition = ScenarioValidatorCondition.RegistryContains,
            ValidationRules = new List<ScenarioValidatorRule>
            {
              new ScenarioValidatorRule { RegistryType = RegistryType.RuntimeState, RegistryIdentifier = "sig.one" }
            }
          }
        }
      });

      Assert.That(ScenarioGraphLoader.SaveToJson(graph), Does.Not.Contain("\"matchMode\""));
    }

    [Test]
    public void ValidatorAnyMatchModePassesWhenOneRegistryRuleMatches()
    {
      const string matchingIdentifier = "sig.validator-any-match";
      MultiplayerInfrastructure.Registry.Registry.Register(RegistryType.RuntimeState, matchingIdentifier, true);
      var gameObject = new UnityEngine.GameObject("validator-any-match-test");
      var controller = gameObject.AddComponent<ScenarioController>();
      var evaluate = typeof(ScenarioController).GetMethod(
        "EvaluateValidatorRootCondition",
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

      try
      {
        var anyCondition = new ScenarioValidatorRootCondition
        {
          Condition = ScenarioValidatorCondition.RegistryContains,
          MatchMode = ScenarioValidatorMatchMode.Any,
          ValidationRules = new List<ScenarioValidatorRule>
          {
            new ScenarioValidatorRule { RegistryType = RegistryType.RuntimeState, RegistryIdentifier = "sig.validator-missing" },
            new ScenarioValidatorRule { RegistryType = RegistryType.RuntimeState, RegistryIdentifier = matchingIdentifier }
          }
        };
        var anyArguments = new object[] { anyCondition, null };

        Assert.That(evaluate, Is.Not.Null);
        Assert.That((bool)evaluate.Invoke(controller, anyArguments), Is.True);

        anyCondition.MatchMode = ScenarioValidatorMatchMode.All;
        var allArguments = new object[] { anyCondition, null };
        Assert.That((bool)evaluate.Invoke(controller, allArguments), Is.False);
      }
      finally
      {
        MultiplayerInfrastructure.Registry.Registry.Unregister(RegistryType.RuntimeState, matchingIdentifier);
        UnityEngine.Object.DestroyImmediate(gameObject);
      }
    }

    [Test]
    public void GraphFingerprintIsIndependentOfNodeInsertionOrder()
    {
      var first = new ScenarioGraph { Identifier = "fingerprint" };
      first.Add(new ScenarioSoundNode { Identifier = "b", SoundResourceIdentifier = "sound-b" });
      first.Add(new ScenarioSoundNode { Identifier = "a", SoundResourceIdentifier = "sound-a" });
      var second = new ScenarioGraph { Identifier = "fingerprint" };
      second.Add(new ScenarioSoundNode { Identifier = "a", SoundResourceIdentifier = "sound-a" });
      second.Add(new ScenarioSoundNode { Identifier = "b", SoundResourceIdentifier = "sound-b" });

      Assert.That(ScenarioGraphFingerprint.Compute(first), Is.EqualTo(ScenarioGraphFingerprint.Compute(second)));
    }
  }
}
