using System.Collections.Generic;
using System.IO;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using NUnit.Framework;
using UnityEngine;

namespace MultiplayerInfrastructure.Tests.Scenario
{
  public sealed class ScenarioClientSignalAuthorizationTests
  {
    [Test]
    public void ArbitraryRaiseAndClearAreRejected()
    {
      var authorization = new ScenarioClientSignalAuthorization();

      Assert.That(authorization.CanRaise(7, "player-a", "sig.arbitrary", out _), Is.False);
      Assert.That(authorization.CanClear(7, "sig.arbitrary", out _), Is.False);
    }

    [Test]
    public void ExplicitGraphSignalPreservesParameterizedRaiseAuthorization()
    {
      var authorization = new ScenarioClientSignalAuthorization();
      var graph = GraphWithValidator("sig.registered-action");
      graph.ClientSignalIdentifiers = new[] { "sig.registered-action" };
      authorization.ConfigureScenario(graph);

      Assert.That(authorization.CanRaise(7, "player-a", "sig.registered-action", out _), Is.True,
        "Authorization is identifier-based and must not reject a valid JSON parameter payload later in the RPC path.");
    }

    [Test]
    public void ValidatorMembershipAloneDoesNotAuthorizeClientRaise()
    {
      var authorization = new ScenarioClientSignalAuthorization();
      authorization.ConfigureScenario(GraphWithValidator("sig.validator-only"));

      Assert.That(authorization.CanRaise(7, "player-a", "sig.validator-only", out _), Is.False);
    }

    [Test]
    public void ExplicitPrefixAuthorizesOnlyItsDeclaredNamespace()
    {
      var graph = new ScenarioGraph { Identifier = "player-zone" };
      graph.ClientSignalPrefixes = new[] { "sig.zone_entered_" };
      graph.Add(new ScenarioSignalCounterNode
      {
        Identifier = "counter",
        SourceSignalPrefix = "zone_entered_",
        OutputSignalIdentifier = "all_players_arrived"
      });
      var authorization = new ScenarioClientSignalAuthorization();
      authorization.ConfigureScenario(graph);

      Assert.That(authorization.CanRaise(7, "player-a", "sig.zone_entered_player-a", out _), Is.True);
      Assert.That(authorization.CanRaise(7, "player-a", "sig.zone_entered_player-b", out _), Is.True,
        "Explicit prefixes are capabilities; sender-scoped dynamic signals should use a dedicated RPC instead.");
      Assert.That(authorization.CanRaise(7, "player-a", "sig.all_players_arrived", out _), Is.False);
    }

    [Test]
    public void TreatmentBindingOutputCannotBeSpoofedThroughValidatorExpectation()
    {
      var graph = GraphWithValidator("sig.apply_gauze_patient_b");
      graph.Add(new ScenarioEntityStateSignalBindingNode
      {
        Identifier = "treatment-binding",
        OutputSignalIdentifier = "apply_gauze_patient_b"
      });
      var authorization = new ScenarioClientSignalAuthorization();
      authorization.ConfigureScenario(graph);

      Assert.That(authorization.CanRaise(7, "player-a", "sig.apply_gauze_patient_b", out _), Is.False);
    }

    [Test]
    public void PatientACriticalAllowsExpectedClientAssessItemAndIvSignals()
    {
      var authorization = new ScenarioClientSignalAuthorization();
      authorization.ConfigureScenario(LoadScenario("patient_a_critical"));

      Assert.That(authorization.CanRaise(7, "player-a", "sig.check_avpu_gcs_patient_a", out _), Is.True,
        "Patient A assessment is a legitimate client-origin interaction.");
      Assert.That(authorization.CanRaise(7, "player-a", "sig.apply_gauze", out _), Is.True,
        "Patient A item application is a legitimate client-origin interaction.");
      Assert.That(authorization.CanRaise(7, "player-a", "sig.insert_iv_patient_a_left", out _), Is.True,
        "Patient A IV insertion is a legitimate client-origin interaction.");
    }

    [Test]
    public void PatientBCServerRpcAndBindingOutputsRemainRejected()
    {
      var authorization = new ScenarioClientSignalAuthorization();
      authorization.ConfigureScenario(LoadScenario("patient_b_c_ct"));

      Assert.That(authorization.CanRaise(7, "player-a", "sig.insert_iv_patient_b_right", out _), Is.False);
      Assert.That(authorization.CanRaise(7, "player-a", "sig.connect_cannula_and_ns1_patient_c", out _), Is.False);
      Assert.That(authorization.CanRaise(7, "player-a", "sig.apply_gauze_patient_b", out _), Is.False);
      Assert.That(authorization.CanRaise(7, "player-a", "sig.patient_c_pupil_checked", out _), Is.True);
      Assert.That(authorization.CanRaise(7, "player-a", "sig.carezone_patient_entered_patient_b", out _), Is.False);
      Assert.That(authorization.CanRaise(7, "player-a", "sig.ct_patient_arrived_patient_c", out _), Is.False);
      Assert.That(authorization.CanRaise(7, "player-a", "sig.close_vital_ui_b", out _), Is.False);
      Assert.That(authorization.CanRaise(7, "player-a", "sig.not-in-the-active-graph", out _), Is.False);
      Assert.That(authorization.CanClear(7, "sig.insert_iv_patient_b_right", out _), Is.False);
    }

    [Test]
    public void ExplicitCapabilityCanAuthorizeRaiseAndClearIndependently()
    {
      var authorization = new ScenarioClientSignalAuthorization();
      authorization.Grant(7, "sig.zone_patient_b");
      authorization.Grant(8, "sig.dialogue-local", allowClear: true);

      Assert.That(authorization.CanRaise(7, "player-a", "sig.zone_patient_b", out _), Is.True);
      Assert.That(authorization.CanClear(7, "sig.zone_patient_b", out _), Is.False);
      Assert.That(authorization.CanClear(8, "sig.dialogue-local", out _), Is.True);
    }

    [Test]
    public void ServerInternalRaiseRemainsIndependentOfClientAuthorization()
    {
      const string signal = "server-internal-compatible";
      try
      {
        ScenarioInteractionSignals.Raise(signal, "{\"server\":true}");
        Assert.That(ScenarioInteractionSignals.IsRaised(signal), Is.True);
      }
      finally
      {
        Registry.Registry.Unregister(RegistryType.RuntimeState, ScenarioInteractionSignals.Normalize(signal));
        ScenarioNetworkRelay.FlushSignalParametersAuthoritative();
      }
    }

    private static ScenarioGraph GraphWithValidator(string signal)
    {
      var graph = new ScenarioGraph { Identifier = "authorization-test" };
      graph.Add(new ScenarioValidatorNode
      {
        Identifier = "validator",
        RootConditions = new[]
        {
          new ScenarioValidatorRootCondition
          {
            Condition = ScenarioValidatorCondition.RegistryContains,
            ValidationRules = new List<ScenarioValidatorRule>
            {
              new ScenarioValidatorRule
              {
                RegistryType = RegistryType.RuntimeState,
                RegistryIdentifier = signal
              }
            }
          }
        }
      });
      return graph;
    }

    private static ScenarioGraph LoadScenario(string identifier)
    {
      string path = Path.Combine(Application.dataPath, "Modules", "TriageTrainer", "Resources",
        "Scenario", identifier + ".scenario.json");
      Assert.That(File.Exists(path), Is.True, $"Scenario fixture not found: {path}");
      return ScenarioGraphLoader.LoadFromJson(File.ReadAllText(path), validateWithSchema: true);
    }
  }
}
