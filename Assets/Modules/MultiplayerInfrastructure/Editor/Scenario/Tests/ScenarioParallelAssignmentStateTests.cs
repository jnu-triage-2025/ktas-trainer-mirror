using MultiplayerInfrastructure.Scenario;
using NUnit.Framework;

namespace MultiplayerInfrastructure.Tests.Scenario
{
  public sealed class ScenarioParallelAssignmentStateTests
  {
    [SetUp]
    public void SetUp() => ScenarioParallelAssignmentState.ClearAll();

    [TearDown]
    public void TearDown() => ScenarioParallelAssignmentState.ClearAll();

    [Test]
    public void ApplyReplacesOneParallelNodesAssignmentWithoutAffectingAnother()
    {
      ScenarioParallelAssignmentState.Apply("P004", "N008", new[] { "N009", "N010" });
      ScenarioParallelAssignmentState.Apply("P004", "N011", new[] { "N012" });
      ScenarioParallelAssignmentState.Apply("P004", "N008", new[] { "N010" });

      Assert.That(ScenarioParallelAssignmentState.IsAssigned("P004", "N008", "N009"), Is.False);
      Assert.That(ScenarioParallelAssignmentState.IsAssigned("P004", "N008", "N010"), Is.True);
      Assert.That(ScenarioParallelAssignmentState.IsAssigned("P004", "N011", "N012"), Is.True);
    }

    [Test]
    public void ClearGraphDoesNotClearOtherGraphs()
    {
      ScenarioParallelAssignmentState.Apply("P004", "N008", new[] { "N009" });
      ScenarioParallelAssignmentState.Apply("P005", "N008", new[] { "N009" });

      ScenarioParallelAssignmentState.ClearGraph("P004");

      Assert.That(ScenarioParallelAssignmentState.IsAssigned("P004", "N008", "N009"), Is.False);
      Assert.That(ScenarioParallelAssignmentState.IsAssigned("P005", "N008", "N009"), Is.True);
    }
  }
}
