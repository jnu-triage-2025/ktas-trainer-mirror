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
    public void NpcMoveAndWaypointHighlightPreserveAllOccurrencesAndCapabilities()
    {
      var graph = new ScenarioGraph { Identifier = "compiler-test" };
      graph.Add(new ScenarioNPCMoveNode
      {
        Identifier = "move",
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
