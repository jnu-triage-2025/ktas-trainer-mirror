using System.Collections;
using MultiplayerInfrastructure.Scenario;
using TriageTrainer.Entity;
using UnityEngine;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private const string DisconnectWallSuctionPatientAEventIdentifier =
      "disconnect_wall_suction_patient_a";

    private void RegisterEvent_DisconnectWallSuctionPatientA()
    {
      Register(DisconnectWallSuctionPatientAEventIdentifier, Event_DisconnectWallSuctionPatientA);
    }

    private IEnumerator Event_DisconnectWallSuctionPatientA()
    {
      var wallSuctions = FindObjectsByType<WallAttachedWallSuction>(
        FindObjectsInactive.Exclude, FindObjectsSortMode.None);
      for (int i = 0; i < wallSuctions.Length; i++)
      {
        var wallSuction = wallSuctions[i];
        if (wallSuction != null && wallSuction.IsPatientAInstallationTarget)
          wallSuction.DisconnectYankauer();
      }

      ScenarioNetworkRelay.InvokePresentationEventAuthoritative(
        DisconnectWallSuctionPatientAEventIdentifier);
      yield break;
    }
  }
}
