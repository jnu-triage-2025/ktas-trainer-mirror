#if UNITY_INCLUDE_TESTS
using System.Linq;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.Scenario.Requirements;
using NUnit.Framework;

namespace MultiplayerInfrastructure.Tests.Scenario.Requirements
{
  public sealed class ScenarioRequirementsRuntimePlayModeSmokeTests
  {
    [Test]
    public void RuntimeFallbackKeepsRequirementIdentity()
    {
      var graph = new ScenarioGraph { Identifier = "playmode-runtime-parity" };
      graph.Add(new ScenarioPlayerMoveNode
      {
        Identifier = "move",
        DestinationType = ScenarioMoveDestinationType.Waypoint,
        DestinationIdentifier = "playmode-waypoint"
      });
      var inferred = ScenarioRequirementCompiler.CompileInferred(graph);
      var runtime = ScenarioRuntimeRequirementsValidator.Validate(
        graph,
        ScenarioRuntimeValidationMode.ReportOnly,
        new ScenarioRuntimeValidationContext(ScenarioRequirementAuthority.Any));

      Assert.That(runtime.Manifest.Requirements.Select(value => value.Key),
        Is.EquivalentTo(inferred.Requirements.Select(value => value.Key)));
    }
  }
}
#endif
