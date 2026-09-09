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

      // 수동 이송을 확인할 수 없는 상황(침대 구성 오류, 대기 시간 초과)에서는 무한 대기 대신
      // 프로그램 이동으로 대체한다. 이 이벤트는 메인 흐름에서 WaitUntilDone 으로 실행되므로,
      // 여기서 멈추면 네 명의 세션 전체가 멈춘다.
      bool fallBackToAutomaticTransfer = !_waitForManualPatientATransfer;

      if (_waitForManualPatientATransfer)
      {
        var bed = moveTarget != null ? moveTarget.GetComponent<MovingPatientBedController>() : null;
        if (bed == null)
        {
          Debug.LogError(
            "[TriageScenarioEventBootstrap] E005: patient A bed target has no MovingPatientBedController. "
            + "Falling back to the automatic transfer.");
          fallBackToAutomaticTransfer = true;
        }
        else
        {
          yield return WaitForManualPatientATransfer(bed, completed => fallBackToAutomaticTransfer = !completed);
        }
      }

      if (!fallBackToAutomaticTransfer)
        yield break;

      // 수동 이송을 확인하지 못해 대체하는 경우에는 수동 진입과 같은 방법으로 침대를 처치실 정박
      // 포인트에 붙인다. 정박은 서버가 침대 정박 신호를 올리므로 뒤따르는 처치 구역 신호도 그대로
      // 이어진다. 씬에 처치실 지점(_patientATreatmentRoomPoint)이 비어 있어도 동작한다.
      if (_waitForManualPatientATransfer
          && moveTarget != null
          && moveTarget.GetComponent<MovingPatientBedController>() != null)
      {
        PlacePatientABedAtTreatmentPoint(ResolvePatientACurrentBed() ?? moveTarget);
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

#if UNITY_EDITOR
      Debug.Log("[EmitSystemMessage] 환자 A를 처치 구역으로 이동시켰습니다.");
#endif
    }

    /// <summary>
    /// 플레이어가 침대를 밀어 정박 포인트에 붙일 때까지 기다린다.
    /// </summary>
    /// <param name="onFinished">
    /// 정박을 확인했으면 true, 확인할 수 없어 호출자가 프로그램 이동으로 대체해야 하면 false 를 전달한다.
    /// 구성 오류(침대 식별자와 포인트 모두 없음)와 <see cref="_patientATransferWaitTimeoutSeconds"/> 초과가
    /// 후자에 해당한다. 어느 경우든 무한 대기하지 않는다.
    /// </param>
    private IEnumerator WaitForManualPatientATransfer(
      MovingPatientBedController bed,
      System.Action<bool> onFinished)
    {
      {
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
            + "positioning point, so the transfer can never be confirmed. Falling back to the automatic transfer.");
          onFinished?.Invoke(false);
          yield break;
        }

        // 이전 실행의 잔류 신호를 제거하여 자동 통과를 방지한다.
        if (!string.IsNullOrWhiteSpace(globalSignal))
          ScenarioInteractionSignals.Clear(globalSignal);
        if (!string.IsNullOrWhiteSpace(scopedSignal))
          ScenarioInteractionSignals.Clear(scopedSignal);
        if (!string.IsNullOrWhiteSpace(anySnapSignal))
          ScenarioInteractionSignals.Clear(anySnapSignal);

        EmitSystemMessage("환자 베드를 처치실 위치까지 이동시키세요.");

        // 대기 시간 초과는 시간 배율이나 일시정지와 무관하게 판정해야 하므로 unscaled 시간을 쓴다.
        float startedAt = Time.unscaledTime;
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
              && Time.unscaledTime - startedAt >= _patientATransferWaitTimeoutSeconds)
          {
            // 예전에는 여기서도 무한 대기했다. 시간 초과를 선언해 두고도 세션이 멈추면 선언의
            // 의미가 없으므로, 밀고 있던 플레이어를 풀어 준 뒤 프로그램 이동으로 대체한다.
            string message =
              $"E005: timed out after {_patientATransferWaitTimeoutSeconds:0.#}s waiting for the patient A bed "
              + "positioning signal. Falling back to the automatic transfer (미수행 기록).";
            Debug.LogError($"[TriageScenarioEventBootstrap] {message}");
            EmitSystemMessage("환자 베드 이동이 시간 안에 확인되지 않아 처치실 위치로 자동 이동합니다.");
            bed.ForceReleaseAllParticipants();
            onFinished?.Invoke(false);
            yield break;
          }

          yield return null;
        }

        // 신호 수신 직후 한 번 더 스냅을 강제해 최종 위치/회전을 보정한다.
        if (hasResolvablePoint)
          bed.TryForceSnapToPositioningPoint(pointIdentifier);

        // 침대가 정박한 시점에 이송을 완료로 처리한다. 누가 밀었는지, 다른 플레이어가
        // 침대에서 내렸는지는 진행 조건으로 삼지 않는다. 다만 조종 상태로 침대에 묶여 있는
        // 플레이어는 다음 단계를 수행할 수 없으므로 서버 권위로 함께 풀어 준다.
        // 일반 수동 이송은 어느 유효 스냅 포인트에서 완료될 수 있다. 완료된 침대를
        // 그 임시 정박 지점에 그대로 두면 환자와 처치실 장비가 분리되어 다음 퀘스트가
        // 물리적으로 불가능해진다. 참가자가 붙어 있는 상태에서 자동 대체 경로와 동일하게
        // 처치실 정박으로 옮긴 뒤 해제해야, 각 클라이언트가 새 정박 위치에서 내린다.
        PlacePatientABedAtTreatmentPoint(ResolvePatientACurrentBed() ?? bed.gameObject);
        bed.ForceReleaseAllParticipants();

        onFinished?.Invoke(true);
      }
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
