using System.Collections;
using FishNet;
using MultiplayerInfrastructure.Logging;
using MultiplayerInfrastructure.Scenario;
using TriageTrainer.Entity;
using TriageTrainer.Entity.LineConnection;
using UnityEngine;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
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

      var cart = ResolveDefibrillatorCart(patient);
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
        $"Patient A defibrillator pads connected to {cart.Identifier} with {connectedCount} AED line(s).",
        "patient_a_critical");
    }

    /// <summary>
    /// 환자가 있는 CareZone 안의 카트를 먼저 사용한다. 복수의 후보가 있으면
    /// 구역 중심이 아닌 환자의 월드 좌표에서 가장 가까운 카트를 선택한다.
    /// 구역 안에 카트가 없을 때 허용되는 fallback도 같은 기준을 사용해,
    /// 씬 탐색 순서나 트랜스폼 계층에 연결 대상이 좌우되지 않게 한다.
    /// </summary>
    private static DefibrillatorCartController ResolveDefibrillatorCart(PatientController patient)
    {
      PatientCareDescriptionZone careZone = FindCareZone(null, patient);
      if (careZone == null)
      {
        if (!ScenarioGameRules.AllowsMissingCareZoneEquipmentFallback(
              CareZoneMissingEquipmentFallback.Defibrillator))
          return null;

        DefibrillatorCartController[] fallbackCarts = FindObjectsByType<DefibrillatorCartController>(
          FindObjectsInactive.Include, FindObjectsSortMode.None);
        return FindClosestDefibrillatorCart(fallbackCarts, patient.transform.position);
      }

      DefibrillatorCartController cart = FindClosestDefibrillatorCart(
        careZone.DefibrillatorCarts, patient.transform.position);
      if (cart != null)
        return cart;

      if (!ScenarioGameRules.AllowsMissingCareZoneEquipmentFallback(
            CareZoneMissingEquipmentFallback.Defibrillator))
        return null;

      DefibrillatorCartController[] allCarts = FindObjectsByType<DefibrillatorCartController>(
        FindObjectsInactive.Include, FindObjectsSortMode.None);
      return FindClosestDefibrillatorCart(allCarts, patient.transform.position);
    }

    private static DefibrillatorCartController FindClosestDefibrillatorCart(
      System.Collections.Generic.IReadOnlyList<DefibrillatorCartController> carts,
      Vector3 origin)
    {
      DefibrillatorCartController closest = null;
      float closestDistanceSquared = float.PositiveInfinity;
      for (int i = 0; i < carts.Count; i++)
      {
        DefibrillatorCartController candidate = carts[i];
        if (candidate == null)
          continue;

        float distanceSquared = (candidate.transform.position - origin).sqrMagnitude;
        if (distanceSquared < closestDistanceSquared)
        {
          closest = candidate;
          closestDistanceSquared = distanceSquared;
        }
      }
      return closest;
    }
  }
}
