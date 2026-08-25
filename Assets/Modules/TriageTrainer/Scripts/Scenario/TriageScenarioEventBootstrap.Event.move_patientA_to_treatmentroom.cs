using System.Collections;
using MultiplayerInfrastructure.Scenario;
using TriageTrainer.Entity;
using UnityEngine;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_MovePatientAToTreatmentRoom()
    {
      Register("move_patient_a_to_treatmentroom", Event_MovePatientAToTreatmentRoom);
    }

    private IEnumerator Event_MovePatientAToTreatmentRoom()
    {
      ResolveRuntimeReferencesIfNeeded();

      SetActiveIfPresent(_patientABedObject, true);
      SetActiveIfPresent(_patientATreatmentBedObject, true);

      var moveTarget = _patientABedObject != null
        ? _patientABedObject
        : (_patientATreatmentBedObject != null ? _patientATreatmentBedObject : _patientAObject);

      if (_autoAttachPatientAToTreatmentBed)
      {
        TryAttachPatientToBed(_patientAObject, moveTarget, "patientA-transfer");
      }

      if (_waitForManualPatientATransfer)
      {
        var bed = moveTarget != null ? moveTarget.GetComponent<MovingPatientBedController>() : null;
        if (bed == null)
        {
          Debug.LogError("[TriageScenarioEventBootstrap] E005: patient A bed target has no MovingPatientBedController. Waiting indefinitely.");
          while (true)
            yield return null;
        }

        string pointIdentifier = _patientATreatmentPositioningPointIdentifier?.Trim();
        if (string.IsNullOrWhiteSpace(pointIdentifier))
        {
          Debug.LogError("[TriageScenarioEventBootstrap] E005: _patientATreatmentPositioningPointIdentifier is empty. Waiting indefinitely.");
          while (true)
            yield return null;
        }

        // 베드가 허용 목록 필터를 사용하는 경우에도 수동 스냅 포인트를 사용할 수 있게 보장한다.
        bed.EnsureAllowedPositioningPointIdentifier(pointIdentifier);
        bed.SetMovementInteractionEnabled(true, releaseParticipantsIfDisabled: false);
        bed.ResetDismountCompletionTracking();

        string allDismountedSignal = bed.DismountCompletionSignal;
        ScenarioInteractionSignals.Clear(allDismountedSignal);

        string globalSignal = $"patient_bed_position_reached_{pointIdentifier}";
        string scopedSignal = string.IsNullOrWhiteSpace(bed.Identifier)
          ? null
          : $"patient_bed_position_reached_{bed.Identifier}_{pointIdentifier}";

        // 이전 실행의 잔류 신호를 제거하여 자동 통과를 방지한다.
        ScenarioInteractionSignals.Clear(globalSignal);
        if (!string.IsNullOrWhiteSpace(scopedSignal))
          ScenarioInteractionSignals.Clear(scopedSignal);

        EmitSystemMessage("환자 A 베드를 처치실 위치까지 직접 이동시키세요.");

        float startedAt = Time.time;
        while (true)
        {
          // 베드가 스냅 범위에 들어온 즉시 목표 포인트로 강제 스냅을 시도한다.
          // (문틀/미세 입력 오차로 기본 Update 스냅이 한 프레임 늦거나 누락되는 경우를 보완)
          bed.TryForceSnapToPositioningPoint(pointIdentifier);

          bool reached = ScenarioInteractionSignals.IsRaised(globalSignal)
                         || (!string.IsNullOrWhiteSpace(scopedSignal)
                             && ScenarioInteractionSignals.IsRaised(scopedSignal));
          if (reached)
            break;

          if (_patientATransferWaitTimeoutSeconds > 0f
              && Time.time - startedAt >= _patientATransferWaitTimeoutSeconds)
          {
            Debug.LogError("[TriageScenarioEventBootstrap] E005: timed out waiting for patient A bed positioning signal.");
            while (true)
              yield return null;
          }

          yield return null;
        }

        // 신호 수신 직후 한 번 더 스냅을 강제해 최종 위치/회전을 보정한다.
        bed.TryForceSnapToPositioningPoint(pointIdentifier);

        EmitSystemMessage("이동 종료: 플레이어 A, B, C, D는 모두 Left Shift로 침대 조종을 해제하세요.");
        {
          float dismountStartedAt = Time.time;
          while (!ScenarioInteractionSignals.IsRaised(allDismountedSignal))
          {
            if (_patientADismountWaitTimeoutSeconds > 0f
                && Time.time - dismountStartedAt >= _patientADismountWaitTimeoutSeconds)
            {
              Debug.LogWarning("[TriageScenarioEventBootstrap] 하차 대기 타임아웃: 강제 진행합니다.");
              break;
            }
            yield return null;
          }
        }

        EmitSystemMessage("환자 A 베드 이동 완료가 확인되었습니다.");
        yield break;
      }

      if (_patientATreatmentMoveDurationSeconds > 0f)
      {
        yield return MoveToIfPresent(moveTarget, _patientATreatmentRoomPoint, _patientATreatmentMoveDurationSeconds);
      }
      else
      {
        SnapToIfPresent(moveTarget, _patientATreatmentRoomPoint);
      }

      EmitSystemMessage("환자 A를 처치 구역으로 이동시켰습니다.");
    }
  }
}
