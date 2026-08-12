using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.Registry;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MultiplayerInfrastructure.Tests.Scenario
{
  public sealed class ScenarioSignalParameterStoreTests
  {
    private const string Signal = "test.parameter-store";

    [SetUp]
    public void SetUp()
    {
      ScenarioNetworkRelay.FlushSignalParametersAuthoritative();
      Registry.Registry.Unregister(RegistryType.RuntimeState, ScenarioInteractionSignals.Normalize(Signal));
    }

    [TearDown]
    public void TearDown()
    {
      ScenarioNetworkRelay.FlushSignalParametersAuthoritative();
      Registry.Registry.Unregister(RegistryType.RuntimeState, ScenarioInteractionSignals.Normalize(Signal));
    }

    [Test]
    public void Raise_WithJson_StoresLatestValueBySignalAndPlayer()
    {
      ScenarioNetworkRelay.RaiseAuthoritativeForPlayer(ScenarioInteractionSignals.Normalize(Signal), "{\"value\":1}", "player-a", "Alice");
      ScenarioNetworkRelay.RaiseAuthoritativeForPlayer(ScenarioInteractionSignals.Normalize(Signal), "{\"value\":2}", "player-b", "Bob");

      Assert.That(ScenarioSignalParameterStore.TryGetLatest(Signal, out var latest), Is.True);
      Assert.That(latest.PlayerIdentifier, Is.EqualTo("player-b"));
      Assert.That(latest.ParameterJson, Is.EqualTo("{\"value\":2}"));

      Assert.That(ScenarioSignalParameterStore.TryGetForPlayer(Signal, "player-a", out var alice), Is.True);
      Assert.That(alice.ParameterJson, Is.EqualTo("{\"value\":1}"));
    }

    [Test]
    public void Raise_WithInvalidJson_DoesNotRecordOrRaiseSignal()
    {
      LogAssert.Expect(LogType.Error,
        "[ScenarioNetworkRelay] 시그널 (sig.test.parameter-store)의 매개변수 ({invalid)는 올바른 JSON 형식이 아닙니다.");
      ScenarioNetworkRelay.RaiseAuthoritativeForPlayer(ScenarioInteractionSignals.Normalize(Signal), "{invalid", "player-a", "Alice");

      Assert.That(ScenarioSignalParameterStore.TryGetLatest(Signal, out _), Is.False);
      Assert.That(ScenarioInteractionSignals.IsRaised(Signal), Is.False);
    }

    [Test]
    public void Flush_ClearsStoredValues()
    {
      ScenarioNetworkRelay.RaiseAuthoritativeForPlayer(ScenarioInteractionSignals.Normalize(Signal), "true", "player-a", "Alice");
      ScenarioNetworkRelay.FlushSignalParametersAuthoritative();

      Assert.That(ScenarioSignalParameterStore.TryGetLatest(Signal, out _), Is.False);
    }

    [Test]
    public void Raise_WithoutParameter_RemainsCompatibleAndRecordsNoParameter()
    {
      ScenarioInteractionSignals.Raise(Signal);

      Assert.That(ScenarioInteractionSignals.IsRaised(Signal), Is.True);
      Assert.That(ScenarioSignalParameterStore.TryGetLatest(Signal, out var value), Is.True);
      Assert.That(value.HasParameter, Is.False);
    }
  }
}
