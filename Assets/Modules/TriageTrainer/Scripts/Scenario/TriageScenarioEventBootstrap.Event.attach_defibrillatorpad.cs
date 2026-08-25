using System.Collections;
using FishNet;
using MultiplayerInfrastructure.Logging;
using TriageTrainer.Entity;
using TriageTrainer.Entity.LineConnection;
using UnityEngine;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private const string PatientADefibrillatorCartEntityId = "defibrillator_cart_a";

    private void RegisterEvent_AttachDefibrillatorPad()
    {
      Register("attach_defibrillatorpad", Event_AttachDefibrillatorPad);
    }

    private IEnumerator Event_AttachDefibrillatorPad()
    {
      SetActiveIfPresent(_patientADefibrillatorPadVisual, true);
      var patient = ResolvePatientAController();
      patient?.SetNamedChildActive("defibrillatorpad_midaxillary_A", true);
      patient?.SetNamedChildActive("defibrillatorpad_subclavicle_A", true);

      ConnectPatientADefibrillatorPads(patient);

      EmitSystemMessage("환자 A 제세동 패드 부착 연출을 적용했습니다.");
      yield break;
    }

    /// <summary>
    /// 부착된 제세동 패드와 제세동 카트 사이의 AED 라인을 서버 권위로 연결한다.
    /// 환자 쪽 패드 두 개와 카트 쪽 연결 지점 두 개를 순서대로 짝지어
    /// <see cref="LineConnectionService.TryCreateAutomaticConnection"/> 로 연결하며,
    /// 이미 연결되어 있는 짝은 다시 연결하지 않는다.
    /// </summary>
    private void ConnectPatientADefibrillatorPads(PatientController patient)
    {
      if (patient == null || (!InstanceFinder.IsOffline && !InstanceFinder.IsServerStarted))
        return;

      var cartObject = ResolveEntityObject(null, PatientADefibrillatorCartEntityId);
      var cart = cartObject != null ? cartObject.GetComponent<DefibrillatorCartController>() : null;
      var padPoints = patient.AedConnectionPoints;
      var cartPoints = cart != null ? cart.AedConnectionPoints : System.Array.Empty<TriageTrainer.Entity.AEDLine.AEDLineConnectionPoint>();
      if (padPoints.Count == 0 || cartPoints.Count == 0)
      {
        Debug.LogWarning(
          "[TriageScenarioEventBootstrap] AED line connection skipped: no AEDLineConnectionPoint on the pads or the defibrillator cart.",
          this);
        GameLogService.WriteScenario(
          "Patient A defibrillator pad AED line connection skipped: missing AEDLineConnectionPoint.",
          "patient_a_critical");
        return;
      }

      var service = LineConnectionService.TopologyService
                    ?? FindFirstObjectByType<LineConnectionService>(FindObjectsInactive.Include);
      if (service == null)
      {
        Debug.LogWarning("[TriageScenarioEventBootstrap] AED line connection skipped: LineConnectionService was not found.", this);
        return;
      }

      int connectedCount = 0;
      int pairCount = Mathf.Min(padPoints.Count, cartPoints.Count);
      for (int i = 0; i < pairCount; i++)
      {
        if (padPoints[i] == null || cartPoints[i] == null)
          continue;
        if (padPoints[i].IsPhysicallyConnectedTo(cartPoints[i]))
          continue;
        if (service.TryCreateAutomaticConnection(padPoints[i], cartPoints[i]))
          connectedCount++;
      }

      GameLogService.WriteScenario(
        $"Patient A defibrillator pads connected to {PatientADefibrillatorCartEntityId} with {connectedCount} AED line(s).",
        "patient_a_critical");
    }
  }
}
