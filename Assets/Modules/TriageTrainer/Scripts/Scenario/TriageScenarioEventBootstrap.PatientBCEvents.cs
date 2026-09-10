using System.Collections;
using FishNet.Connection;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Tag;
using UnityEngine;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    public const string PreinstalledOxygenWarning = "이미 산소장치가 설치되어있다. 이것을 해제하고 새로 설치하자.";

    private void RegisterPatientBCEvents()
    {
      RegisterEvent_TriagePatientBPatientCPatientDummyDB();
      RegisterEvent_ShowPatientBUi();
      RegisterEvent_ShowPatientDummyDBUi();
      RegisterEvent_ShowPatientCUi();
      RegisterEvent_MovePatientB();
      RegisterEvent_MovePatientC();
      RegisterEvent_SetupMovePatients();
      RegisterEvent_ActivateVitalMonitorUiPatientB();
      RegisterEvent_ActivateVitalMonitorUiPatientC();
      RegisterEvent_PupilReflexPatientB();
      RegisterEvent_PupilReflexPatientC();
      RegisterEvent_MovePatientsToCt();
      RegisterEvent_FadeOutPatientBC();
      RegisterEvent_ApplyGauzePatientB();
      RegisterEvent_ApplyGauzeWithPlasterPatientB();
      RegisterEvent_ApplyGauzePatientC();
      RegisterEvent_ApplyGauzeWithPlasterPatientC();
      RegisterEvent_Insert20gRightPatientB();
      RegisterEvent_ConnectNs1RightPatientB();
      RegisterEvent_Insert20gLeftPatientC();
      RegisterEvent_ConnectNs1LeftPatientC();
      RegisterEvent_AttachPatientBedPairs();
      RegisterEvent_DetachPatientBCBeds();
      Register("activate_patient_b_nurse_c_treatment", () => Event_ActivatePatientBCTreatment(false, true));
      Register("activate_patient_b_nurse_d_treatment", () => Event_ActivatePatientBCTreatment(false, false));
      Register("activate_patient_c_nurse_c_treatment", () => Event_ActivatePatientBCTreatment(true, true));
      Register("activate_patient_c_nurse_d_treatment", () => Event_ActivatePatientBCTreatment(true, false));
      RegisterPatientBCRecognitionEvents();
    }

    private IEnumerator Event_ActivatePatientBCTreatment(bool targetPatientC, bool nurseC)
    {
      ResolveRuntimeReferencesIfNeeded();
      var target = targetPatientC ? _patientCObject : _patientBObject;
      var patient = target != null ? target.GetComponentInChildren<TriageTrainer.Entity.PatientController>(true) : null;
      if (patient == null)
        yield break;

      if (nurseC)
        patient.ActivatePatientBCNurseCStage();
      else if (patient.ActivatePatientBCNurseDStage())
        SendPrivateSystemMessageToTaggedPlayer("nurse_d", PreinstalledOxygenWarning);
    }

    private static void SendPrivateSystemMessageToTaggedPlayer(string tag, string message)
    {
      if (!Registry.TryGet<ChatService>(RegistryType.Service, Registry.TypeKey<ChatService>(), out var chat))
        return;

      var players = FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
      for (int i = 0; i < players.Length; i++)
      {
        PlayerController player = players[i];
        NetworkConnection connection = player != null ? player.Owner : null;
        if (connection == null || !connection.IsValid || !PlayerTagService.HasTag(player.UserIdentifier, tag))
          continue;

        chat.SendSystemMessage(connection, message);
      }
    }

    private static bool LocalPlayerHasTag(string tag)
    {
      var players = FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
      for (int i = 0; i < players.Length; i++)
      {
        var player = players[i];
        if (player != null && player.IsOwner && !string.IsNullOrWhiteSpace(player.UserIdentifier))
          return PlayerTagService.HasTag(player.UserIdentifier, tag);
      }

      return false;
    }

  }
}
