using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ConnectTPieceReady()
    {
      Register("connect_tpiece_ready", Event_ConnectTPieceReady);
    }

    private IEnumerator Event_ConnectTPieceReady()
    {
      SetActiveIfPresent(_patientATPieceConnectedVisual, true);
      var patient = ResolvePatientAController();
      patient?.SetTreatmentApplied("oxygen_line_connected", true);
      yield break;
    }
  }
}
