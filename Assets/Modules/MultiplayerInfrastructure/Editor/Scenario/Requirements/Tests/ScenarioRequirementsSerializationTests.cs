using System;
using System.Linq;
using System.Text;
using MultiplayerInfrastructure.Scenario.Requirements;
using NUnit.Framework;

namespace MultiplayerInfrastructure.Tests.Scenario.Requirements
{
  public sealed class ScenarioRequirementsSerializationTests
  {
    [Test]
    public void DuplicateJsonPropertyIsRejected()
    {
      const string json = "{\"format\":\"scenario-ingame-requirements\",\"schemaVersion\":1,\"schemaVersion\":1,\"scenarioIdentifier\":\"test\",\"source\":{\"scenarioSha256\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\"},\"declarations\":[],\"suppressions\":[]}";

      var result = ScenarioRequirementsLoader.LoadSidecar(json);

      Assert.That(result.IsValid, Is.False);
      Assert.That(result.Diagnostics.Any(value => value.Code == "SIR107"), Is.True);
    }

    [Test]
    public void UnsupportedVersionIsRejectedBeforeDeserialization()
    {
      const string json = "{\"format\":\"scenario-ingame-requirements\",\"schemaVersion\":2,\"scenarioIdentifier\":\"test\",\"source\":{\"scenarioSha256\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\"},\"declarations\":[],\"suppressions\":[]}";

      var result = ScenarioRequirementsLoader.LoadSidecar(json);

      Assert.That(result.IsValid, Is.False);
      Assert.That(result.Diagnostics.Any(value => value.Code == "SIR103"), Is.True);
    }

    [Test]
    public void SchemaRejectsUnknownEnumAndUnknownProperty()
    {
      const string json = "{\"format\":\"scenario-ingame-requirements\",\"schemaVersion\":1,\"scenarioIdentifier\":\"test\",\"source\":{\"scenarioSha256\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\"},\"declarations\":[{\"selector\":{\"kind\":\"NotAKind\",\"identifier\":\"x\"},\"operation\":\"Override\",\"unexpected\":true}],\"suppressions\":[]}";

      var result = ScenarioRequirementsLoader.LoadSidecar(json);

      Assert.That(result.IsValid, Is.False);
      Assert.That(result.Diagnostics.Any(value => value.Code == "SIR105"), Is.True);
    }

    [Test]
    public void StaleSourceHashProducesWarningDuringCompile()
    {
      var graph = new MultiplayerInfrastructure.Scenario.ScenarioGraph { Identifier = "stale-test" };
      var sourceBytes = Encoding.UTF8.GetBytes("source-a");
      var sidecarJson = "{\"format\":\"scenario-ingame-requirements\",\"schemaVersion\":1,\"scenarioIdentifier\":\"stale-test\",\"source\":{\"scenarioSha256\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\"},\"declarations\":[],\"suppressions\":[]}";
      var sidecar = ScenarioRequirementsLoader.LoadSidecar(sidecarJson);

      var manifest = ScenarioRequirementCompiler.Compile(
        graph,
        sourceBytes,
        sidecar.Document,
        new ScenarioRequirementCompilationContext(DateTime.UtcNow));

      Assert.That(manifest.Diagnostics.Any(value => value.Code == "SIR201"), Is.True);
    }

    [Test]
    public void WriterProducesByteDeterministicOutput()
    {
      const string json = "{\"format\":\"scenario-ingame-requirements\",\"schemaVersion\":1,\"scenarioIdentifier\":\"writer-test\",\"source\":{\"scenarioSha256\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\"},\"declarations\":[],\"suppressions\":[]}";
      var loaded = ScenarioRequirementsLoader.LoadSidecar(json);

      Assert.That(loaded.IsValid, Is.True);
      Assert.That(ScenarioRequirementsWriter.WriteUtf8(loaded.Document), Is.EqualTo(ScenarioRequirementsWriter.WriteUtf8(loaded.Document)));
    }
  }
}
