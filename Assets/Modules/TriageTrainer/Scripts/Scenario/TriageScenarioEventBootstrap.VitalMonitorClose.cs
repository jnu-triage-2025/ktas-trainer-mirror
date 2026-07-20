using MultiplayerInfrastructure.Scenario;
using TriageTrainer.Entity.PatientMonitor.Models;
using UnityEngine;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void ConfigureVitalMonitorClose(PatientMonitorController monitorController,
      GameObject monitorObject,
      GameObject panelObject,
      string completionSignal)
    {
      if (monitorController == null || string.IsNullOrWhiteSpace(completionSignal))
      {
        return;
      }

      monitorController.SetCloseRequestedHandler(() =>
      {
        SetActiveIfPresent(panelObject, false);
        SetActiveIfPresent(monitorObject, false);
        ScenarioInteractionSignals.Raise(completionSignal);
      });
    }
  }
}
