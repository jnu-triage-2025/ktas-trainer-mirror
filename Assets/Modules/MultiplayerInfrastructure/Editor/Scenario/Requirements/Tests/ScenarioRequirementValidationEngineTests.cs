using System;
using System.Collections.Generic;
using System.Linq;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.Scenario.Requirements;
using NUnit.Framework;

namespace MultiplayerInfrastructure.Tests.Scenario.Requirements
{
  public sealed class ScenarioRequirementValidationEngineTests
  {
    [Test]
    public void RuntimeValidationMapsInactiveProviderToNotReady()
    {
      var graph = new ScenarioGraph { Identifier = "runtime-not-ready" };
      graph.Add(new ScenarioPlayerMoveNode
      {
        Identifier = "move",
        DestinationType = ScenarioMoveDestinationType.Waypoint,
        DestinationIdentifier = "waypoint-a"
      });
      var manifest = ScenarioRequirementCompiler.CompileInferred(graph);
      var key = new ScenarioRequirementKey(ScenarioRequirementKind.SpatialAnchor, "waypoint-a");
      var provider = new ScenarioRequirementProvider(
        "inactive-waypoint",
        key,
        "Assets/Test.unity",
        ScenarioRequirementScope.AnyLoadedScene,
        "Waypoint",
        new[] { ScenarioRequirementCapability.ProvidesPosition },
        false,
        true,
        false,
        ScenarioRequirementProviderOrigin.SceneComponent);
      var snapshot = new ScenarioRequirementProviderSnapshot(
        new[] { provider },
        Array.Empty<ScenarioRequirementValidationDiagnostic>(),
        new Dictionary<ScenarioRequirementKind, ScenarioRequirementEvidenceCompleteness>
        {
          [ScenarioRequirementKind.SpatialAnchor] = ScenarioRequirementEvidenceCompleteness.Complete
        });
      var composition = new ScenarioRequirementSceneComposition("test", Array.Empty<ScenarioRequirementCompositionScene>());

      var report = ScenarioRequirementValidationEngine.ValidateRuntime(manifest, snapshot, composition);

      Assert.That(report.Results.Single(value => value.Requirement.Key.Equals(key)).Status,
        Is.EqualTo(ScenarioRequirementValidationStatus.NotReady));
    }

    [Test]
    public void RuntimeAuthorityApplicabilityHonorsHostOnly()
    {
      var host = new ScenarioRuntimeValidationContext(ScenarioRequirementAuthority.Any, true, true, true);
      var dedicatedServer = new ScenarioRuntimeValidationContext(ScenarioRequirementAuthority.Any, true, false, false);

      Assert.That(ScenarioRuntimeRequirementsValidator.AppliesToContext(ScenarioRequirementAuthority.HostOnly, host), Is.True);
      Assert.That(ScenarioRuntimeRequirementsValidator.AppliesToContext(ScenarioRequirementAuthority.HostOnly, dedicatedServer), Is.False);
    }

    [Test]
    public void RuntimeValidationChecksApplicableFixedAuthorityRequirement()
    {
      var graph = new ScenarioGraph { Identifier = "server-authority" };
      graph.Add(new ScenarioEntityInitNode { Identifier = "spawn", PresetIdentifier = "server-preset" });
      var manifest = ScenarioRequirementCompiler.CompileInferred(graph);
      var snapshot = new ScenarioRequirementProviderSnapshot(
        Array.Empty<ScenarioRequirementProvider>(),
        Array.Empty<ScenarioRequirementValidationDiagnostic>(),
        new Dictionary<ScenarioRequirementKind, ScenarioRequirementEvidenceCompleteness>
        {
          [ScenarioRequirementKind.EntityPreset] = ScenarioRequirementEvidenceCompleteness.Complete
        });
      var composition = new ScenarioRequirementSceneComposition("test", Array.Empty<ScenarioRequirementCompositionScene>());

      Assert.That(ScenarioRequirementValidationEngine.Validate(manifest, snapshot, composition).Results.Single(value => value.Requirement.Key.Kind == ScenarioRequirementKind.EntityPreset).Status,
        Is.EqualTo(ScenarioRequirementValidationStatus.Indeterminate));
      Assert.That(ScenarioRequirementValidationEngine.ValidateRuntime(manifest, snapshot, composition).Results.Single(value => value.Requirement.Key.Kind == ScenarioRequirementKind.EntityPreset).Status,
        Is.EqualTo(ScenarioRequirementValidationStatus.Missing));
    }

    [Test]
    public void RuntimeFallbackPreservesCompilerRequirementKeys()
    {
      var graph = new ScenarioGraph { Identifier = "runtime-parity" };
      graph.Add(new ScenarioPlayerMoveNode
      {
        Identifier = "move",
        DestinationType = ScenarioMoveDestinationType.Waypoint,
        DestinationIdentifier = "runtime-waypoint"
      });

      var inferred = ScenarioRequirementCompiler.CompileInferred(graph);
      var runtime = ScenarioRuntimeRequirementsValidator.Validate(
        graph,
        ScenarioRuntimeValidationMode.ReportOnly,
        new ScenarioRuntimeValidationContext(ScenarioRequirementAuthority.Any));

      Assert.That(runtime.Manifest.Requirements.Select(value => value.Key),
        Is.EquivalentTo(inferred.Requirements.Select(value => value.Key)));
    }

    [Test]
    public void StrictRuntimeValidationDoesNotTreatPartialEvidenceAsMissing()
    {
      var graph = new ScenarioGraph { Identifier = "runtime-abort" };
      graph.Add(new ScenarioPlayerMoveNode
      {
        Identifier = "move",
        DestinationType = ScenarioMoveDestinationType.Waypoint,
        DestinationIdentifier = "missing-waypoint"
      });

      var result = ScenarioRuntimeRequirementsValidator.Validate(
        graph,
        ScenarioRuntimeValidationMode.AbortScenarioStart,
        new ScenarioRuntimeValidationContext(ScenarioRequirementAuthority.Any));

      Assert.That(result.ShouldAbort, Is.False);
      Assert.That(result.Report.Results.Any(value => value.Status == ScenarioRequirementValidationStatus.Indeterminate), Is.True);
    }

    [Test]
    public void StrictRuntimeValidationAbortsOnManifestCompilationError()
    {
      var graph = new ScenarioGraph { Identifier = "runtime-invalid-manifest" };
      graph.Add(new ScenarioPlayerMoveNode
      {
        Identifier = "move",
        DestinationType = ScenarioMoveDestinationType.Waypoint,
        DestinationIdentifier = " "
      });

      var result = ScenarioRuntimeRequirementsValidator.Validate(
        graph,
        ScenarioRuntimeValidationMode.AbortScenarioStart,
        new ScenarioRuntimeValidationContext(ScenarioRequirementAuthority.Any));

      Assert.That(result.Diagnostics.Any(value => value.Code == "SIR100" && value.Severity >= ScenarioRequirementDiagnosticSeverity.Error), Is.True);
      Assert.That(result.ShouldAbort, Is.True);
    }

    [Test]
    public void PartialEvidenceNeverProvesDuplicateProviders()
    {
      var graph = new ScenarioGraph { Identifier = "partial-duplicate" };
      graph.Add(new ScenarioPlayerMoveNode { Identifier = "move", DestinationType = ScenarioMoveDestinationType.Waypoint, DestinationIdentifier = "waypoint-a" });
      var manifest = ScenarioRequirementCompiler.CompileInferred(graph);
      var key = new ScenarioRequirementKey(ScenarioRequirementKind.SpatialAnchor, "waypoint-a");
      var providers = new[]
      {
        new ScenarioRequirementProvider("scene:waypoint-a", key, "Assets/Test.unity", ScenarioRequirementScope.AnyLoadedScene, "Waypoint", new[] { ScenarioRequirementCapability.ProvidesPosition }, true, true, true, ScenarioRequirementProviderOrigin.SceneComponent),
        new ScenarioRequirementProvider("registry:waypoint-a", key, string.Empty, ScenarioRequirementScope.AnyLoadedScene, string.Empty, new[] { ScenarioRequirementCapability.ProvidesPosition }, true, true, true, ScenarioRequirementProviderOrigin.RegistryPreloaderDeclaration)
      };
      var snapshot = new ScenarioRequirementProviderSnapshot(providers, Array.Empty<ScenarioRequirementValidationDiagnostic>(), new Dictionary<ScenarioRequirementKind, ScenarioRequirementEvidenceCompleteness>
      {
        [ScenarioRequirementKind.SpatialAnchor] = ScenarioRequirementEvidenceCompleteness.Partial
      });

      var report = ScenarioRequirementValidationEngine.ValidateRuntime(manifest, snapshot, new ScenarioRequirementSceneComposition("test", Array.Empty<ScenarioRequirementCompositionScene>()));

      Assert.That(report.Results.Single(value => value.Requirement.Key.Equals(key)).Status, Is.EqualTo(ScenarioRequirementValidationStatus.Indeterminate));
    }

  }
}
