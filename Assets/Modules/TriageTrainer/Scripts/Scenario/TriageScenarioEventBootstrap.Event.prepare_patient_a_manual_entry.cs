using System.Collections;
using MultiplayerInfrastructure.Logging;
using MultiplayerInfrastructure.Quest;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using TriageTrainer.Entity;
using TriageTrainer.Entity.Patient;
using UnityEngine;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private bool _manualPatientASetupSucceeded;

    private void RegisterEvent_PreparePatientAManualEntry()
    {
      Register("prepare_patient_a_manual_scen_entry", Event_PreparePatientAManualScenEntry);
      Register("prepare_patient_a_manual_doc_inst", Event_PreparePatientAManualDocInst);
      Register("prepare_patient_a_manual_arrest", Event_PreparePatientAManualArrest);
      Register("prepare_patient_a_manual_cpr_1st_cycle", Event_PreparePatientAManualCpr1);
      Register("prepare_patient_a_manual_cpr_2nd_cycle", Event_PreparePatientAManualCpr2);
      Register("prepare_patient_a_manual_rosc_followup", Event_PreparePatientAManualRosc);
    }

    private IEnumerator Event_PreparePatientAManualScenEntry()
    {
      yield return EnsurePatientAForManualEntry();
      if (!_manualPatientASetupSucceeded)
        yield break;
      ApplyManualStage(ManualPatientAStage.ScenEntry);
    }

    private IEnumerator Event_PreparePatientAManualDocInst()
    {
      yield return EnsurePatientAForManualEntry();
      if (!_manualPatientASetupSucceeded)
        yield break;
      ApplyManualStage(ManualPatientAStage.DocInstruction);
    }

    private IEnumerator Event_PreparePatientAManualArrest()
    {
      yield return EnsurePatientAForManualEntry();
      if (!_manualPatientASetupSucceeded)
        yield break;
      ApplyManualStage(ManualPatientAStage.PreArrest);
    }

    private IEnumerator Event_PreparePatientAManualCpr1()
    {
      yield return EnsurePatientAForManualEntry();
      if (!_manualPatientASetupSucceeded)
        yield break;
      ApplyManualStage(ManualPatientAStage.Cpr1);
    }

    private IEnumerator Event_PreparePatientAManualCpr2()
    {
      yield return EnsurePatientAForManualEntry();
      if (!_manualPatientASetupSucceeded)
        yield break;
      ApplyManualStage(ManualPatientAStage.Cpr2);
    }

    private IEnumerator Event_PreparePatientAManualRosc()
    {
      yield return EnsurePatientAForManualEntry();
      if (!_manualPatientASetupSucceeded)
        yield break;
      ApplyManualStage(ManualPatientAStage.RoscFollowup);
    }

    private IEnumerator EnsurePatientAForManualEntry()
    {
      _manualPatientASetupSucceeded = false;
      ResolveRuntimeReferencesIfNeeded();
      if (_patientAObject == null)
      {
        Vector3 position = _patientATreatmentRoomPoint != null
          ? _patientATreatmentRoomPoint.position
          : Vector3.zero;
        if (!Registry.TrySpawnEntityPreset(
              "patient_a", position, Quaternion.identity, "patient_a",
              out var spawned, out _, out var error))
        {
          Debug.LogError($"[TriageScenarioEventBootstrap] Manual patient A setup failed: {error}", this);
          yield break;
        }

        _patientAObject = spawned;
        GameLogService.WriteScenario(
          "Patient A spawned for a manual scenario entry.",
          "patient_a_critical");
      }

      SetActiveIfPresent(_patientABedObject, true);
      SetActiveIfPresent(_patientATreatmentBedObject, true);
      var bed = _patientATreatmentBedObject != null ? _patientATreatmentBedObject : _patientABedObject;
      if (_autoAttachPatientAToTreatmentBed)
        TryAttachPatientToBed(_patientAObject, bed, "patientA-manual-entry");
      else
        SnapToIfPresent(_patientAObject, _patientATreatmentRoomPoint);

      // 정박 대상은 환자가 실제로 올라가 있는 침대를 우선으로 고른다. 설정으로 지정한 침대와
      // 다른 침대에 결합된 채 진입할 수 있고, 그때 지정 침대만 옮기면 환자는 제자리에 남는다.
      PlacePatientABedAtTreatmentPoint(ResolvePatientACurrentBed() ?? bed);
      EnsurePatientADoctorForManualEntry();
      ResetPatientAStagePresentation();
      _manualPatientASetupSucceeded = ResolvePatientAController() != null;
      if (!_manualPatientASetupSucceeded)
        Debug.LogError("[TriageScenarioEventBootstrap] Manual patient A setup completed without a PatientController.", this);
      yield break;
    }

    /// <summary>환자 A가 실제로 결합되어 있는 침대. 결합되어 있지 않으면 null 이다.</summary>
    private GameObject ResolvePatientACurrentBed()
    {
      var patient = ResolvePatientAController();
      var bed = patient != null ? patient.CurrentBed : null;
      return bed != null ? bed.gameObject : null;
    }

    /// <summary>
    /// 환자 A 침대를 처치실의 확정 위치에 둔다. 수동 진입은 침대를 밀고 오는 과정을 건너뛰므로,
    /// 일반 진행에서 발생하는 이동·정박 신호에 기대지 않고 포지셔닝 포인트에 직접 붙인다.
    /// 이 시나리오에는 별도의 처치실 침대 엔티티가 없고 환자가 타고 온 이동 침대가 그 자리를
    /// 대신하므로, 침대 오브젝트가 아니라 포인트 식별자를 위치의 기준으로 삼는다.
    ///
    /// <para>
    /// 일반 진행의 이송 목표(<c>_patientATreatmentPositioningPointIdentifier</c>)는 "어느 스냅 포인트든
    /// 허용"을 뜻하도록 비워 두는 배치가 있어서 수동 진입의 목적지로는 쓸 수 없다. 수동 진입은
    /// 도착 지점이 하나로 정해져야 하므로 전용 설정값을 사용한다.
    /// </para>
    /// </summary>
    private void PlacePatientABedAtTreatmentPoint(GameObject bedObject)
    {
      string pointIdentifier = _patientAManualEntryBedSnapPointIdentifier?.Trim();
      var bed = bedObject != null ? bedObject.GetComponent<MovingPatientBedController>() : null;
      if (string.IsNullOrWhiteSpace(pointIdentifier) || bed == null)
      {
        Debug.LogWarning(
          "[TriageScenarioEventBootstrap] Manual entry has no patient A bed or snap point identifier; "
          + "falling back to the treatment room point.", this);
        SnapToIfPresent(_patientAObject, _patientATreatmentRoomPoint);
        return;
      }

      bed.EnsureAllowedPositioningPointIdentifier(pointIdentifier);
      if (bed.TryForceSnapToPositioningPoint(pointIdentifier, teleportToPoint: true))
      {
        GameLogService.WriteScenario(
          $"Manual entry snapped the patient A bed to '{pointIdentifier}'.",
          "patient_a_critical");
        return;
      }

      Debug.LogWarning(
        $"[TriageScenarioEventBootstrap] Manual entry could not snap the patient A bed to "
        + $"positioning point '{pointIdentifier}'. Falling back to the treatment room point.", this);
      SnapToIfPresent(_patientAObject, _patientATreatmentRoomPoint);
    }

    /// <summary>
    /// 단계 밖 표시와 상호작용을 정리한다. 수동 진입은 이전 회차나 이전 단계에서 남은 퀘스트 마크와
    /// 플레이어별 상호작용 플래그를 그대로 물려받으므로, 준비 체인이 단계 기준으로 다시 닫아야 한다.
    /// 활력징후 모니터도 닫힌 상태에서 시작하고, 모니터가 필요한 단계는 본 흐름의 모니터 이벤트가 다시 연다.
    /// </summary>
    private void ResetPatientAStagePresentation()
    {
      QuestPresentationService.ClearScenarioMarks();
      PlayerQuestStateFlagService.ClearAll();

      ResolvePatientVitalMonitor(_patientAObject,
        ref _patientAVitalMonitorObject,
        ref _patientAVitalMonitorController);
      if (_patientAVitalMonitorController != null)
        _patientAVitalMonitorController.CloseForScenarioReset();
      SetActiveIfPresent(_patientAVitalPanel, false);
    }

    /// <summary>ScenarioController 의 NPC 보행 애니메이션 파라미터와 같은 이름이다.</summary>
    private const string ManualEntryNpcWalkAnimationParameterName = "walk";

    /// <summary>
    /// 처치실에 자리잡은 의사가 바라볼 방향. 그래프의 MOVE_DOCTOR_TO_TREATROOM_ENTERED 노드가
    /// 지정한 facingYawDegrees 와 같은 값을 쓴다. 일반 진행과 수동 진입이 같은 방향으로 끝나야 한다.
    /// </summary>
    private const float PatientADoctorTreatroomFacingYawDegrees = 180f;

    /// <summary>
    /// 의사 NPC를 처치실 도착 지점에 둔다. 이미 월드에 있는 의사는 이전 단계의 이동 경로 위에
    /// 서 있으므로, 새로 만들 때뿐 아니라 매번 도착 지점으로 옮겨야 한다. 수동 진입은 그 이동
    /// 연출을 재생하지 않기 때문이다. 강제 중단된 이동 코루틴이 켜 둔 보행 애니메이션도 되돌린다.
    /// </summary>
    private void EnsurePatientADoctorForManualEntry()
    {
      const string doctorIdentifier = "npc-doctor-patient-a-critical";

      var anchor = FindWaypoint(PatientADoctorTreatroomEnteredAnchorId);
      Vector3 position = anchor != null
        ? anchor.transform.position
        : _patientATreatmentRoomPoint != null ? _patientATreatmentRoomPoint.position : Vector3.zero;
      var rotation = Quaternion.Euler(0f, PatientADoctorTreatroomFacingYawDegrees, 0f);

      var doctorObject = ResolveEntityObject(null, doctorIdentifier);
      if (doctorObject == null
          && !Registry.TrySpawnEntityPreset(
            "npc_doctor_preset", position, rotation, doctorIdentifier,
            out doctorObject, out _, out var error))
      {
        Debug.LogError($"[TriageScenarioEventBootstrap] Manual doctor setup failed: {error}", this);
        return;
      }

      if (doctorObject == null)
        return;

      // 일반 진행의 NPC 이동은 매 프레임 지면 레이캐스트로 y 값을 보정한다. 수동 진입도
      // 같은 도착 웨이포인트를 사용하지만 이동을 건너뛰므로, 좌표를 그대로 대입하면
      // 웨이포인트의 편집용 높이(현재 1m)가 남아 의사가 공중에 뜬다.
      SnapPatientADoctorToGround(doctorObject.transform, position);
      doctorObject.transform.rotation = rotation;
      var animation = doctorObject.GetComponentInChildren<HumanoidAnimationController>(true);
      if (animation != null)
        animation.SetBool(ManualEntryNpcWalkAnimationParameterName, false);

      GameLogService.WriteScenario(
        $"Manual entry placed the patient A doctor at '{PatientADoctorTreatroomEnteredAnchorId}'.",
        "patient_a_critical");
    }

    /// <summary>
    /// 일반 시나리오의 NPC 보행과 같은 규칙으로 의사 루트를 지면에 맞춘다. NPC 자신의
    /// 콜라이더는 제외하고, 목적지 아래에서 가장 높은 비트리거 충돌면을 지면으로 사용한다.
    /// </summary>
    private static void SnapPatientADoctorToGround(Transform doctorTransform, Vector3 position)
    {
      var ownColliders = doctorTransform.GetComponentsInChildren<Collider>(true);
      if (ScenarioController.TryFindGroundY(position, ownColliders, out float groundY))
        position.y = groundY;

      var characterController = doctorTransform.GetComponent<CharacterController>();
      bool wasEnabled = characterController != null && characterController.enabled;
      if (characterController != null)
        characterController.enabled = false;

      try
      {
        doctorTransform.position = position;
      }
      finally
      {
        if (characterController != null)
          characterController.enabled = wasEnabled;
      }
    }

    private enum ManualPatientAStage
    {
      ScenEntry,
      DocInstruction,
      PreArrest,
      Cpr1,
      Cpr2,
      RoscFollowup
    }

    private void ApplyManualStage(ManualPatientAStage stage)
    {
      var patient = ResolvePatientAController();
      if (patient == null)
        return;

      // 준비 체인이 어디까지 수행됐는지는 세션 로그로만 확인할 수 있다. 단계 표시를 남겨,
      // 진입 후 상태가 어긋났을 때 준비 체인 미실행과 단계 정의 부족을 구분할 수 있게 한다.
      GameLogService.WriteScenario(
        $"Manual entry stage applied: {stage}.",
        "patient_a_critical");

      ResetManualVisualState(patient);
      switch (stage)
      {
        case ManualPatientAStage.ScenEntry:
          ApplyManualTreatmentSnapshot(patient);
          return;
        case ManualPatientAStage.DocInstruction:
          ApplyInitialAssessmentState(patient);
          return;
        case ManualPatientAStage.PreArrest:
          ApplyPreArrestTreatmentState(patient);
          return;
        case ManualPatientAStage.Cpr1:
          ApplyPreArrestTreatmentState(patient);
          ApplyPeaState(patient);
          return;
        case ManualPatientAStage.Cpr2:
        case ManualPatientAStage.RoscFollowup:
          ApplyPreArrestTreatmentState(patient);
          ApplyCpr1TreatmentState(patient);
          ApplyCpr2TreatmentState(patient);
          ApplyPeaState(patient);
          return;
      }
    }

    private void ApplyInitialAssessmentState(PatientController patient)
    {
      ApplyDisplay(patient, PatientController.TreatmentDisplay.CervicalCollarOnNeck);
      ApplyManualTreatmentSnapshot(patient, "initial_assessment_complete");
    }

    private void ApplyPreArrestTreatmentState(PatientController patient)
    {
      ApplyInitialAssessmentState(patient);
      ApplyDisplay(patient, PatientController.TreatmentDisplay.EndotrachealTubeInsertDone);
      ApplyDisplay(patient, PatientController.TreatmentDisplay.TPieceAttachedToNasalCannula);
      ApplyDisplay(patient, PatientController.TreatmentDisplay.Syringe18GInsertedIntoLeftArm);
      ApplyDisplay(patient, PatientController.TreatmentDisplay.Syringe18GInsertedIntoRightArm);
      ApplyDisplay(patient, PatientController.TreatmentDisplay.CentralVenousCatheterInsertedIntoSubclavian);
      ApplyDisplay(patient, PatientController.TreatmentDisplay.GauzePatchedOnThorax);
      ApplyDisplay(patient, PatientController.TreatmentDisplay.GauzeDressingDoneOnThorax);
      SetActiveIfPresent(_patientAEtTubeWithoutStyletVisual, true);
      SetActiveIfPresent(_patientATPieceConnectedVisual, true);
      SetActiveIfPresent(_patientA18gLeftVisual, true);
      SetActiveIfPresent(_patientANs1LeftConnectedVisual, true);
      SetActiveIfPresent(_patientA18gRightVisual, true);
      SetActiveIfPresent(_patientAPs1RightConnectedVisual, true);
      SetActiveIfPresent(_patientACentralLineVisual, true);
      SetActiveIfPresent(_level1ReadyVisual, true);
      ApplyManualTreatmentSnapshot(patient, "initial_assessment_complete", "oxygen_line_connected", "pre_arrest_treatment_complete");
    }

    private void ApplyCpr1TreatmentState(PatientController patient)
    {
      ApplyDisplay(patient, PatientController.TreatmentDisplay.TPieceAttachedToNasalCannula);
      SetActiveIfPresent(_patientATPieceConnectedVisual, true);
      ApplyDisplay(patient, PatientController.TreatmentDisplay.AmbuBagAttachedToEndotrachealTube);
      ApplyManualTreatmentSnapshot(patient, "initial_assessment_complete", "oxygen_line_connected", "pre_arrest_treatment_complete", "cpr_1_complete");
    }

    private void ApplyCpr2TreatmentState(PatientController patient)
    {
      // CPR 2주기부터는 T-piece가 분리되고 앰부백/패드와 1주기 처치가 완료된 상태다.
      patient.ApplyScenarioDisplayState(PatientController.TreatmentDisplay.TPieceAttachedToNasalCannula.ToString(), false);
      SetActiveIfPresent(_patientATPieceConnectedVisual, false);
      ApplyDisplay(patient, PatientController.TreatmentDisplay.AmbuBagAttachedToEndotrachealTube);
      SetActiveIfPresent(_patientAAmbuConnectedVisual, true);
      SetActiveIfPresent(_patientADefibrillatorPadVisual, true);
      patient.SetNamedChildActive("defibrillatorpad_midaxillary_A", true);
      patient.SetNamedChildActive("defibrillatorpad_subclavicle_A", true);
      SetAnimatorsBool(_ambuBaggingAnimators, _ambuBaggingBoolName, false);
      SetAnimatorsBool(_chestCompressionAnimators, _chestCompressionBoolName, false);
      ApplyManualTreatmentSnapshot(patient,
        "initial_assessment_complete", "oxygen_line_connected", "pre_arrest_treatment_complete",
        "cpr_1_complete", "cpr_2_complete", "defibrillator_pads_attached",
        "epinephrine_round_1_complete", "normal_saline_round_1_complete", "defibrillation_round_1_complete");
    }

    private void ApplyPeaState(PatientController patient)
    {
      float unavailable = TriageTrainer.Entity.Patient.PatientMedicalState.MonitorValueUnavailable;
      patient.MedicalStateIsCardiacArrest = true;
      patient.SetMonitorMedicalState(
        _patientACrashMonitorParameters,
        new ARTParameters { bpm = unavailable, systolic = unavailable, diastolic = unavailable, noise = 0f },
        new CVPParameters { bpm = unavailable, mean = unavailable, noise = 0f },
        new PlethParameters { bpm = unavailable, spo2 = unavailable, noise = 0f },
        new NumericsParameters { bpm = _patientACrashMonitorParameters.bpm, pvcs = unavailable, pulseRate = unavailable, perfusionIndex = unavailable, spo2 = unavailable },
        new NIBPParameters { systolic = unavailable, diastolic = unavailable },
        new TemperatureParameters { t1 = unavailable, t2 = unavailable },
        new STLeadValues { i = unavailable, ii = unavailable, iii = unavailable, avr = unavailable, avl = unavailable, avf = unavailable, v1 = unavailable, v2 = unavailable, v3 = unavailable, v4 = unavailable, v5 = unavailable, v6 = unavailable });
      GameLogService.WriteScenario(
        "Patient A medical state restored for manual entry: cardiac arrest (PEA).",
        "patient_a_critical");
    }

    private static void ApplyDisplay(PatientController patient, PatientController.TreatmentDisplay display)
    {
      patient.ApplyScenarioDisplayState(display.ToString(), true);
    }

    private void ResetManualVisualState(PatientController patient)
    {
      foreach (PatientController.TreatmentDisplay display in System.Enum.GetValues(typeof(PatientController.TreatmentDisplay)))
      {
        if (display != PatientController.TreatmentDisplay.None)
          patient.ApplyScenarioDisplayState(display.ToString(), false);
      }
      SetActiveIfPresent(_patientAEtTubePreparedVisual, false);
      SetActiveIfPresent(_patientAEtTubeInsertedVisual, false);
      SetActiveIfPresent(_patientAEtTubeWithoutStyletVisual, false);
      SetActiveIfPresent(_patientATPieceConnectedVisual, false);
      SetActiveIfPresent(_patientAGauzeVisual, false);
      SetActiveIfPresent(_patientAGauzeWithPlasterVisual, false);
      SetActiveIfPresent(_patientA18gLeftVisual, false);
      SetActiveIfPresent(_patientANs1LeftConnectedVisual, false);
      SetActiveIfPresent(_patientA18gRightVisual, false);
      SetActiveIfPresent(_patientAPs1RightConnectedVisual, false);
      SetActiveIfPresent(_patientACentralLineVisual, false);
      SetActiveIfPresent(_level1ReadyVisual, false);
      SetActiveIfPresent(_patientAAmbuConnectedVisual, false);
      SetActiveIfPresent(_patientADefibrillatorPadVisual, false);
      patient.SetNamedChildActive("defibrillatorpad_midaxillary_A", false);
      patient.SetNamedChildActive("defibrillatorpad_subclavicle_A", false);
      SetAnimatorsBool(_ambuBaggingAnimators, _ambuBaggingBoolName, false);
      SetAnimatorsBool(_chestCompressionAnimators, _chestCompressionBoolName, false);
    }

    private static void ApplyManualTreatmentSnapshot(PatientController patient, params string[] identifiers)
      => patient.SetTreatmentStateSnapshot(identifiers ?? System.Array.Empty<string>());
  }
}
