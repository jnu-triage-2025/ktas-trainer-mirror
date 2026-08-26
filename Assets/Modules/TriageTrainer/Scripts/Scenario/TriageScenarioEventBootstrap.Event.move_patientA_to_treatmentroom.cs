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

        // 지정한 포인트가 씬에 없으면 특정 포인트 신호를 영원히 기다리게 되므로, 목표 안내는 시나리오의
        // 목표 지점 마크에 맡기고 통과 판정은 "이 침대가 허용된 스냅 포인트 어딘가에 정박했는가"로 완화한다.
        string pointIdentifier = _patientATreatmentPositioningPointIdentifier?.Trim();
        bool hasResolvablePoint = !string.IsNullOrWhiteSpace(pointIdentifier)
                                  && FindPositioningPoint(pointIdentifier) != null;
        if (hasResolvablePoint)
        {
          // 베드가 허용 목록 필터를 사용하는 경우에도 수동 스냅 포인트를 사용할 수 있게 보장한다.
          bed.EnsureAllowedPositioningPointIdentifier(pointIdentifier);
        }
        else if (!string.IsNullOrWhiteSpace(pointIdentifier))
        {
          // 식별자를 지정해 두었는데 씬에 그 포인트가 없으면 설정 오류이므로 드러낸다.
          // 식별자를 비워 둔 경우는 "어느 스냅 포인트든 허용"이 의도된 설정이므로 조용히 진행한다.
          Debug.LogWarning(
            $"[TriageScenarioEventBootstrap] E005: positioning point '{pointIdentifier}' is not present in the scene. "
            + "Completing on any allowed bed snap point instead.");
        }

        bed.SetMovementInteractionEnabled(true, releaseParticipantsIfDisabled: false);
        bed.ResetDismountCompletionTracking();

        string allDismountedSignal = bed.DismountCompletionSignal;
        ScenarioInteractionSignals.Clear(allDismountedSignal);

        string globalSignal = hasResolvablePoint
          ? $"patient_bed_position_reached_{pointIdentifier}"
          : null;
        string scopedSignal = hasResolvablePoint && !string.IsNullOrWhiteSpace(bed.Identifier)
          ? $"patient_bed_position_reached_{bed.Identifier}_{pointIdentifier}"
          : null;
        // 포인트를 특정하지 않을 때 쓰는 침대 범위 정박 신호.
        string anySnapSignal = hasResolvablePoint || string.IsNullOrWhiteSpace(bed.Identifier)
          ? null
          : $"patient_bed_positioning_point_latched_{bed.Identifier}";

        if (string.IsNullOrWhiteSpace(globalSignal)
            && string.IsNullOrWhiteSpace(scopedSignal)
            && string.IsNullOrWhiteSpace(anySnapSignal))
        {
          Debug.LogError(
            "[TriageScenarioEventBootstrap] E005: patient A bed has no identifier and no reachable "
            + "positioning point, so the transfer can never be confirmed. Waiting indefinitely.");
          while (true)
            yield return null;
        }

        // 이전 실행의 잔류 신호를 제거하여 자동 통과를 방지한다.
        if (!string.IsNullOrWhiteSpace(globalSignal))
          ScenarioInteractionSignals.Clear(globalSignal);
        if (!string.IsNullOrWhiteSpace(scopedSignal))
          ScenarioInteractionSignals.Clear(scopedSignal);
        if (!string.IsNullOrWhiteSpace(anySnapSignal))
          ScenarioInteractionSignals.Clear(anySnapSignal);

        EmitSystemMessage("환자 A 베드를 처치실 위치까지 직접 이동시키세요.");

        float startedAt = Time.time;
        while (true)
        {
          // 베드가 스냅 범위에 들어온 즉시 목표 포인트로 강제 스냅을 시도한다.
          // (문틀/미세 입력 오차로 기본 Update 스냅이 한 프레임 늦거나 누락되는 경우를 보완)
          if (hasResolvablePoint)
            bed.TryForceSnapToPositioningPoint(pointIdentifier);

          bool reached = (!string.IsNullOrWhiteSpace(globalSignal)
                          && ScenarioInteractionSignals.IsRaised(globalSignal))
                         || (!string.IsNullOrWhiteSpace(scopedSignal)
                             && ScenarioInteractionSignals.IsRaised(scopedSignal))
                         || (!string.IsNullOrWhiteSpace(anySnapSignal)
                             && ScenarioInteractionSignals.IsRaised(anySnapSignal));
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
        if (hasResolvablePoint)
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

    /// <summary>씬에 배치된 침대 정박 포인트를 식별자로 찾는다. 없으면 null 을 돌려준다.</summary>
    private static MovingPatientBedPositioningPoint FindPositioningPoint(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return null;

      foreach (var point in FindObjectsByType<MovingPatientBedPositioningPoint>(
                 FindObjectsInactive.Include, FindObjectsSortMode.None))
      {
        if (point != null
            && string.Equals(point.Identifier, identifier, System.StringComparison.Ordinal))
          return point;
      }

      return null;
    }
  }
}
