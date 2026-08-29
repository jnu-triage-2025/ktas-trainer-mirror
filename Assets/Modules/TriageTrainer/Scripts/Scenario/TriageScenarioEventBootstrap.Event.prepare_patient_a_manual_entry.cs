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
      ResetPatientAScenarioActionConsumption();

      ResolvePatientVitalMonitor(_patientAObject,
        ref _patientAVitalMonitorObject,
        ref _patientAVitalMonitorController);
      if (_patientAVitalMonitorController != null)
        _patientAVitalMonitorController.CloseForScenarioReset();
      SetActiveIfPresent(_patientAVitalPanel, false);
    }

    /// <summary>
    /// 환자 A의 단계별 상호작용에서 "이미 수행함" 표시를 지운다. 이 상호작용들은 한 번 수행하면
    /// 다시 노출되지 않으므로, 같은 세션에서 앞 단계로 수동 진입하면 그 단계의 처치를 수행할 수
    /// 없게 된다. 노출 여부는 플레이어별 퀘스트 상태 플래그가 계속 결정하므로, 표시만 지워도
    /// 단계 밖 상호작용이 열리지는 않는다.
    ///
    /// </summary>
    private static void ResetPatientAScenarioActionConsumption()
    {
      foreach (var action in FindObjectsByType<ScenarioActionInteractable>(
                 FindObjectsInactive.Include, FindObjectsSortMode.None))
      {
        if (action != null && IsPatientAScenarioEntity(action.PresentationEntityIdentifier))
          action.ResetCompletionForScenario();
      }
    }

    private static bool IsPatientAScenarioEntity(string entityIdentifier)
    {
      for (int i = 0; i < PatientAScenarioEntityIdentifiers.Length; i++)
      {
        if (string.Equals(entityIdentifier, PatientAScenarioEntityIdentifiers[i],
              System.StringComparison.Ordinal))
          return true;
      }
      return false;
    }

    /// <summary>퀘스트 표시와 상호작용 게이트가 환자 A를 가리킬 때 쓰는 엔티티 식별자.</summary>
    private const string PatientAScenarioEntityIdentifier = "patient_a";

    /// <summary>
    /// 이 시나리오의 단계별 <see cref="ScenarioActionInteractable"/> 이 소속된 엔티티 식별자.
    /// </summary>
    private static readonly string[] PatientAScenarioEntityIdentifiers =
    {
      PatientAScenarioEntityIdentifier,
    };

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
          RestorePatientAPreArrestEquipment(patient);
          return;
        case ManualPatientAStage.Cpr1:
          ApplyPreArrestTreatmentState(patient);
          // 스냅샷 교체를 장비 복원보다 먼저 수행한다. ApplyCpr1EntryTreatmentState 는 처치 상태값
          // 집합을 통째로 덮어쓰므로, 복원 과정이 기록한 값(산소 라인 연결 등) 가운데 목록에 없는
          // 것이 뒤에 오면 조용히 지워진다.
          ApplyCpr1EntryTreatmentState(patient);
          RestorePatientAPreArrestEquipment(patient);
          ApplyPeaState(patient);
          OpenPatientACpr1Actions();
          return;
        case ManualPatientAStage.Cpr2:
        case ManualPatientAStage.RoscFollowup:
          ApplyPreArrestTreatmentState(patient);
          RestorePatientAPreArrestEquipment(patient);
          ApplyCpr1TreatmentState(patient);
          ApplyCpr2TreatmentState(patient);
          ApplyPeaState(patient);
          return;
      }
    }

    /// <summary>
    /// 초기 평가(P003) 완료 상태. 경추 고정과 구강 흡인이 끝난 것으로 기록한다.
    /// 처치 상태값에는 실제 처치 경로가 쓰는 식별자를 함께 넣는다. 이 값이 비어 있으면
    /// 이미 끝난 처치를 다시 적용할 수 있고(경추 고정기 재적용 등), 물품 적용 상호작용도
    /// 그 판정을 <see cref="PatientController.IsTreatmentApplied"/> 로 수행한다.
    /// </summary>
    private void ApplyInitialAssessmentState(PatientController patient)
    {
      ApplyDisplay(patient, PatientController.TreatmentDisplay.CervicalCollarOnNeck);
      ApplyManualTreatmentSnapshot(patient, PatientAInitialAssessmentTreatments);
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
      SetActiveIfPresent(_patientAGauzeWithPlasterVisual, true);
      SetActiveIfPresent(_patientA18gLeftVisual, true);
      SetActiveIfPresent(_patientANs1LeftConnectedVisual, true);
      SetActiveIfPresent(_patientA18gRightVisual, true);
      SetActiveIfPresent(_patientAPs1RightConnectedVisual, true);
      SetActiveIfPresent(_patientACentralLineVisual, true);
      SetActiveIfPresent(_level1ReadyVisual, true);
      ApplyManualTreatmentSnapshot(patient, PatientAPreArrestTreatments);
    }

    /// <summary>
    /// CPR 1주기 진입 시점의 처치 상태값. 처치 표현은 <see cref="ApplyPreArrestTreatmentState"/> 와
    /// 같지만(T-piece가 아직 붙어 있고 앰부백·패드는 없다), 심정지 직후의 첫 맥박 확인은 이미
    /// 끝난 처치이므로 완료 상태로 함께 기록한다.
    ///
    /// <para>
    /// 이 기록이 맥박 확인 상호작용을 닫는 것은 아니다. 그 노출은
    /// <see cref="PatientACriticalQuestStateFlags.ArrestPulseAssess"/> 플래그만으로 결정되고,
    /// 준비 체인이 앞서 모든 플래그를 내려 둔 뒤 다시 올리지 않기 때문에 닫혀 있다.
    /// 여기서의 상태값은 단계 진행 기록일 뿐이다.
    /// </para>
    /// </summary>
    private void ApplyCpr1EntryTreatmentState(PatientController patient)
    {
      ApplyManualTreatmentSnapshot(patient, PatientACpr1EntryTreatments);
    }

    /// <summary>
    /// CPR 1주기 처치 동작(T-piece 분리·앰부백 연결·산소 저장낭 연결·가슴압박·제세동 패드)을
    /// 전원에게 연다. 일반 진행에서는 <c>activate_patient_a_arrest_actions</c> 가 이 역할을 하지만
    /// 수동 진입은 그 노드를 건너뛰므로 준비 체인이 직접 열어야 한다. 역할 배정은 이어지는
    /// 병렬 노드(P005)가 다시 수행한다.
    ///
    /// <para>
    /// 같은 이벤트가 여는 첫 맥박 확인(<c>assess_pulse_r1</c>)은 열지 않는다. 그 확인은 CPR 1주기
    /// 진입 전에 이미 끝난 처치이고, 열어 둔 채 진입하면 CPR 1주기 목표와 맥박 확인 메뉴가 함께
    /// 노출된다. 준비 체인은 앞서 모든 플래그를 내려 두므로 여기서 따로 닫을 필요는 없다.
    /// </para>
    /// </summary>
    private void OpenPatientACpr1Actions()
    {
      PlayerQuestStateFlagService.SetForAll(PatientACriticalQuestStateFlags.Cpr1Actions);
    }

    private void ApplyCpr1TreatmentState(PatientController patient)
    {
      ApplyDisplay(patient, PatientController.TreatmentDisplay.TPieceAttachedToNasalCannula);
      SetActiveIfPresent(_patientATPieceConnectedVisual, true);
      ApplyDisplay(patient, PatientController.TreatmentDisplay.AmbuBagAttachedToEndotrachealTube);
      ApplyManualTreatmentSnapshot(patient, PatientACpr1Treatments);
    }

    private void ApplyCpr2TreatmentState(PatientController patient)
    {
      // CPR 2주기부터는 T-piece가 분리되고 앰부백/패드와 1주기 처치가 완료된 상태다.
      // 준비 체인이 되돌린 T-piece 산소 라인도 함께 끊어야 상태와 라인이 어긋나지 않는다.
      DisconnectPatientAOxygenLineForCpr2(patient);
      patient.ApplyScenarioDisplayState(PatientController.TreatmentDisplay.TPieceAttachedToNasalCannula.ToString(), false);
      SetActiveIfPresent(_patientATPieceConnectedVisual, false);
      ApplyDisplay(patient, PatientController.TreatmentDisplay.AmbuBagAttachedToEndotrachealTube);
      SetActiveIfPresent(_patientAAmbuConnectedVisual, true);
      SetActiveIfPresent(_patientADefibrillatorPadVisual, true);
      patient.SetNamedChildActive("defibrillatorpad_midaxillary_A", true);
      patient.SetNamedChildActive("defibrillatorpad_subclavicle_A", true);
      SetAnimatorsBool(_ambuBaggingAnimators, _ambuBaggingBoolName, false);
      SetAnimatorsBool(_chestCompressionAnimators, _chestCompressionBoolName, false);
      ApplyManualTreatmentSnapshot(patient, PatientACpr2Treatments);
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

    // ── 처치 상태값 식별자 ─────────────────────────────────────────────────
    //
    // 처치 상태값은 처치 표현과 별개로 "이 처치가 끝났는지"를 보관한다. 물품 적용 경로
    // (PatientController.TryResolveItemUse)가 이 값으로 중복 적용을 막으므로, 준비 체인이
    // 단계 요약 식별자만 넣으면 이미 끝난 처치를 다시 수행할 수 있다. 아래 목록은 일반
    // 진행에서 실제로 기록되는 식별자와 단계 요약을 함께 담는다.

    /// <summary>경추 고정기 적용. 물품 적용 경로가 쓰는 아이템 식별자와 같다.</summary>
    private const string PatientATreatmentCervicalCollar = TriageTrainer.ItemDefinitions.CervicalCollar.Identifier;

    /// <summary>구강 흡인 수행. 조립된 양커 팁 사용이 기록하는 식별자다.</summary>
    private const string PatientATreatmentSuction = TriageTrainer.ItemDefinitions.YankauerSuctionReady.Identifier;

    /// <summary>기관내관 삽관 완료(스타일렛 제거까지).</summary>
    private const string PatientATreatmentEtTubeDone = "endotracheal_tube_insert_done";

    /// <summary>기관내관 플라스터 고정.</summary>
    private const string PatientATreatmentPlasterOnIntubation = PatientController.TreatmentPlasterOnIntubation;

    /// <summary>T-piece 장착.</summary>
    private const string PatientATreatmentTPieceAttached = "tpiece_attached";

    /// <summary>T-piece와 벽면 유량계의 산소 연결.</summary>
    private const string PatientATreatmentOxygenLine = "oxygen_line_connected";

    /// <summary>흉부 거즈 압박 지혈.</summary>
    private const string PatientATreatmentGauze = PatientController.TreatmentGauze;

    /// <summary>거즈 플라스터 고정.</summary>
    private const string PatientATreatmentPlasterOnGauze = PatientController.TreatmentPlasterOnGauze;

    /// <summary>초기 평가 단계(P003) 완료 요약.</summary>
    private const string PatientAStageInitialAssessment = "initial_assessment_complete";

    /// <summary>의사 지시와 역할별 처치(P004) 완료 요약.</summary>
    private const string PatientAStagePreArrest = "pre_arrest_treatment_complete";

    /// <summary>심정지 직후 첫 맥박 확인 완료.</summary>
    private const string PatientAStageArrestPulseChecked = "arrest_pulse_checked";

    /// <summary>CPR 1주기 완료 요약.</summary>
    private const string PatientAStageCpr1 = "cpr_1_complete";

    /// <summary>CPR 2주기 완료 요약.</summary>
    private const string PatientAStageCpr2 = "cpr_2_complete";

    private static readonly string[] PatientAInitialAssessmentTreatments =
    {
      PatientAStageInitialAssessment,
      PatientATreatmentCervicalCollar,
      PatientATreatmentSuction,
    };

    private static readonly string[] PatientAPreArrestTreatments =
    {
      PatientAStageInitialAssessment,
      PatientATreatmentCervicalCollar,
      PatientATreatmentSuction,
      PatientATreatmentEtTubeDone,
      PatientATreatmentPlasterOnIntubation,
      PatientATreatmentTPieceAttached,
      PatientATreatmentOxygenLine,
      PatientATreatmentGauze,
      PatientATreatmentPlasterOnGauze,
      PatientAStagePreArrest,
    };

    private static readonly string[] PatientACpr1EntryTreatments =
    {
      PatientAStageInitialAssessment,
      PatientATreatmentCervicalCollar,
      PatientATreatmentSuction,
      PatientATreatmentEtTubeDone,
      PatientATreatmentPlasterOnIntubation,
      PatientATreatmentTPieceAttached,
      PatientATreatmentOxygenLine,
      PatientATreatmentGauze,
      PatientATreatmentPlasterOnGauze,
      PatientAStagePreArrest,
      PatientAStageArrestPulseChecked,
    };

    private static readonly string[] PatientACpr1Treatments =
    {
      PatientAStageInitialAssessment,
      PatientATreatmentCervicalCollar,
      PatientATreatmentSuction,
      PatientATreatmentEtTubeDone,
      PatientATreatmentPlasterOnIntubation,
      PatientATreatmentTPieceAttached,
      PatientATreatmentOxygenLine,
      PatientATreatmentGauze,
      PatientATreatmentPlasterOnGauze,
      PatientAStagePreArrest,
      PatientAStageArrestPulseChecked,
      PatientAStageCpr1,
    };

    private static readonly string[] PatientACpr2Treatments =
    {
      PatientAStageInitialAssessment,
      PatientATreatmentCervicalCollar,
      PatientATreatmentSuction,
      PatientATreatmentEtTubeDone,
      PatientATreatmentPlasterOnIntubation,
      PatientATreatmentTPieceAttached,
      PatientATreatmentOxygenLine,
      PatientATreatmentGauze,
      PatientATreatmentPlasterOnGauze,
      PatientAStagePreArrest,
      PatientAStageArrestPulseChecked,
      PatientAStageCpr1,
      PatientAStageCpr2,
      "defibrillator_pads_attached",
      "epinephrine_round_1_complete",
      "normal_saline_round_1_complete",
      "defibrillation_round_1_complete",
    };

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

