using System.Collections;
using MultiplayerInfrastructure.Logging;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.Tag;
using UnityEngine;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private const string PatientATriageReturnGateListenerId = "patient_a_triage_return_gate";
    private const string PatientATriageReturnSignal = "sig.arrive_triagearea_patient_a";
    private const string PatientANurseTag = "nurse_a";
    private const string TriageArrivalPerPlayerSignalTemplate = "quest_arrival_triage_area_{id}";

    // OverworldGameObjectInitializer.TriageArrivalWaypointIdentifier 와 동일한 값이다.
    // 해당 상수는 Editor 어셈블리에 있어 런타임에서 참조할 수 없어 여기서도 정의한다.
    private const string TriageZoneEntityId = "scen_b:quest_arrival_triage_area";

    private void RegisterEvent_ArmPatientATriageReturn()
    {
      Register("arm_patient_a_triage_return", Event_ArmPatientATriageReturn);
    }

    private IEnumerator Event_ArmPatientATriageReturn()
    {
      string nurseAUserIdentifier = ResolveNurseAUserIdentifier();
      if (string.IsNullOrWhiteSpace(nurseAUserIdentifier))
      {
        Debug.LogWarning(
          "[TriageScenarioEventBootstrap] Patient A triage return gate was not armed: no connected nurse_a holder.",
          this);
        yield break;
      }

      // 분류 구역 존이 플레이어별로 올리는 도착 신호(quest_arrival_triage_area_{사용자 식별자})를
      // nurse_a 홀더의 신호만 시나리오 게이트 신호로 전파한다. 이렇게 하면 다른 플레이어의
      // 구역 진입이 nurse_a의 퀘스트를 완료시키지 않는다(송신자 범위 검증).
      string sourceSignal = TriageArrivalPerPlayerSignalTemplate.Replace("{id}", nurseAUserIdentifier);
      ScenarioConditionalSignalListeners.Register(
        PatientATriageReturnGateListenerId,
        sourceSignal,
        PatientATriageReturnSignal,
        System.Array.Empty<string>(),
        consumeOnce: false);
      GameLogService.WriteScenario(
        $"Patient A triage return gate armed for nurse_a holder '{nurseAUserIdentifier}'.",
        "patient_a_critical");

      // 무장 시점에 이미 구역 안에 들어와 있는 경우에는 재진입 없이 도착을 인정한다.
      // (점유 폴맵은 새 진입에서만 신호를 올리므로, 안에 머무는 상태로는 게이트가 열리지 않는다.)
      if (IsNurseAHolderInsideTriageZone())
      {
        MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Raise(PatientATriageReturnSignal);
        GameLogService.WriteScenario(
          "Patient A triage return gate credited: nurse_a holder was already inside the triage zone.",
          "patient_a_critical");
      }
      yield break;
    }

    private static bool IsNurseAHolderInsideTriageZone()
    {
      var players = FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
      Transform nurseAHolder = null;
      for (int i = 0; i < players.Length && nurseAHolder == null; i++)
      {
        var player = players[i];
        var connection = player != null ? player.Owner : null;
        if (connection == null || !connection.IsValid)
          continue;
        if (string.IsNullOrWhiteSpace(player.UserIdentifier)
            || !PlayerTagService.HasTag(player.UserIdentifier, PatientANurseTag))
          continue;
        nurseAHolder = player.transform;
      }

      if (nurseAHolder == null)
        return false;

      var zones = FindObjectsByType<MultiplayerInfrastructure.Scenario.ScenarioTriggerZone>(
        FindObjectsInactive.Include, FindObjectsSortMode.None);
      for (int i = 0; i < zones.Length; i++)
      {
        var zone = zones[i];
        if (zone == null || !string.Equals(zone.Identifier, TriageZoneEntityId, System.StringComparison.Ordinal))
          continue;
        var zoneCollider = zone.GetComponent<Collider>();
        if (zoneCollider != null && zoneCollider.bounds.Contains(nurseAHolder.position))
          return true;
      }

      return false;
    }

    private static string ResolveNurseAUserIdentifier()
    {
      var players = FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
      for (int i = 0; i < players.Length; i++)
      {
        var player = players[i];
        var connection = player != null ? player.Owner : null;
        if (connection == null || !connection.IsValid)
          continue;
        if (string.IsNullOrWhiteSpace(player.UserIdentifier))
          continue;
        if (!PlayerTagService.HasTag(player.UserIdentifier, PatientANurseTag))
          continue;
        return player.UserIdentifier;
      }

      return null;
    }
  }
}
