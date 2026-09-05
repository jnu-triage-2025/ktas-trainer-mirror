using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RecoverTypedReferences()
    {
      RecoverPatientATreatmentVisuals();
      RecoverPatientBTreatmentVisuals();
      RecoverPatientCTreatmentVisuals();
      RecoverSceneReferences();
    }

    private void RecoverPatientATreatmentVisuals()
    {
      _patientAEtTubePreparedVisual = TypedUnityReference.RecoverUniqueChild<PatientAEtTubePreparedVisualMarker>(
        (object)_patientAEtTubePreparedVisual, (object)_patientAObject, nameof(_patientAEtTubePreparedVisual), ReportReferenceError);
      _patientAEtTubeInsertedVisual = TypedUnityReference.RecoverUniqueChild<PatientAEtTubeInsertedVisualMarker>(
        (object)_patientAEtTubeInsertedVisual, (object)_patientAObject, nameof(_patientAEtTubeInsertedVisual), ReportReferenceError);
      _patientATPieceConnectedVisual = TypedUnityReference.RecoverUniqueChild<PatientATPieceConnectedVisualMarker>(
        (object)_patientATPieceConnectedVisual, (object)_patientAObject, nameof(_patientATPieceConnectedVisual), ReportReferenceError);
      _patientAGauzeVisual = TypedUnityReference.RecoverUniqueChild<PatientAGauzeVisualMarker>(
        (object)_patientAGauzeVisual, (object)_patientAObject, nameof(_patientAGauzeVisual), ReportReferenceError);
      _patientAGauzeWithPlasterVisual = TypedUnityReference.RecoverUniqueChild<PatientAGauzeWithPlasterVisualMarker>(
        (object)_patientAGauzeWithPlasterVisual, (object)_patientAObject, nameof(_patientAGauzeWithPlasterVisual), ReportReferenceError);
      _patientA18gLeftVisual = TypedUnityReference.RecoverUniqueChild<PatientA18gLeftVisualMarker>(
        (object)_patientA18gLeftVisual, (object)_patientAObject, nameof(_patientA18gLeftVisual), ReportReferenceError);
      _patientA18gRightVisual = TypedUnityReference.RecoverUniqueChild<PatientA18gRightVisualMarker>(
        (object)_patientA18gRightVisual, (object)_patientAObject, nameof(_patientA18gRightVisual), ReportReferenceError);
      _patientACentralLineVisual = TypedUnityReference.RecoverUniqueChild<PatientACentralLineVisualMarker>(
        (object)_patientACentralLineVisual, (object)_patientAObject, nameof(_patientACentralLineVisual), ReportReferenceError);
      _patientAAmbuConnectedVisual = TypedUnityReference.RecoverUniqueChild<PatientAAmbuConnectedVisualMarker>(
        (object)_patientAAmbuConnectedVisual, (object)_patientAObject, nameof(_patientAAmbuConnectedVisual), ReportReferenceError);
    }

    private void RecoverPatientBTreatmentVisuals()
    {
      _patientBGauzeVisual = TypedUnityReference.RecoverUniqueChild<PatientBGauzeVisualMarker>(
        (object)_patientBGauzeVisual, (object)_patientBObject, nameof(_patientBGauzeVisual), ReportReferenceError);
      _patientBGauzeWithPlasterVisual = TypedUnityReference.RecoverUniqueChild<PatientBGauzeWithPlasterVisualMarker>(
        (object)_patientBGauzeWithPlasterVisual, (object)_patientBObject, nameof(_patientBGauzeWithPlasterVisual), ReportReferenceError);
      _patientB20gRightVisual = TypedUnityReference.RecoverUniqueChild<PatientB20gRightVisualMarker>(
        (object)_patientB20gRightVisual, (object)_patientBObject, nameof(_patientB20gRightVisual), ReportReferenceError);
    }

    private void RecoverPatientCTreatmentVisuals()
    {
      _patientCGauzeVisual = TypedUnityReference.RecoverUniqueChild<PatientCGauzeVisualMarker>(
        (object)_patientCGauzeVisual, (object)_patientCObject, nameof(_patientCGauzeVisual), ReportReferenceError);
      _patientCGauzeWithPlasterVisual = TypedUnityReference.RecoverUniqueChild<PatientCGauzeWithPlasterVisualMarker>(
        (object)_patientCGauzeWithPlasterVisual, (object)_patientCObject, nameof(_patientCGauzeWithPlasterVisual), ReportReferenceError);
      _patientC20gLeftVisual = TypedUnityReference.RecoverUniqueChild<PatientC20gLeftVisualMarker>(
        (object)_patientC20gLeftVisual, (object)_patientCObject, nameof(_patientC20gLeftVisual), ReportReferenceError);
    }

    private void ReportReferenceError(string message)
      => Debug.LogError($"[{nameof(TriageScenarioEventBootstrap)}] {message}", this);

    private void RecoverSceneReferences()
    {
      var markers = FindObjectsByType<TriageScenarioReferenceMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
      var byRole = new Dictionary<TriageScenarioReferenceRole, TriageScenarioReferenceMarker>();
      var ambiguous = new HashSet<TriageScenarioReferenceRole>();
      foreach (var marker in markers)
      {
        if (marker == null)
          continue;
        if (!byRole.TryAdd(marker.Role, marker))
          ambiguous.Add(marker.Role);
      }

      foreach (var role in ambiguous)
      {
        byRole.Remove(role);
        Debug.LogError($"[{nameof(TriageScenarioEventBootstrap)}] Multiple scene reference markers use role '{role}'. Automatic recovery was skipped.", this);
      }

      Transform Point(TriageScenarioReferenceRole role) => byRole.TryGetValue(role, out var marker) ? marker.transform : null;
      GameObject Panel(TriageScenarioReferenceRole role) => byRole.TryGetValue(role, out var marker) ? marker.gameObject : null;

      _triageArrivalPoint ??= Point(TriageScenarioReferenceRole.TriageArrivalPoint);
      _playerATriagePoint ??= Point(TriageScenarioReferenceRole.PlayerATriagePoint);
      _patientATreatmentRoomPoint ??= Point(TriageScenarioReferenceRole.PatientATreatmentRoomPoint);
      _patientBTreatmentRoomPoint ??= Point(TriageScenarioReferenceRole.PatientBTreatmentRoomPoint);
      _patientCTreatmentRoomPoint ??= Point(TriageScenarioReferenceRole.PatientCTreatmentRoomPoint);
      _patientBCtRoomPoint ??= Point(TriageScenarioReferenceRole.PatientBCtRoomPoint);
      _patientCCtRoomPoint ??= Point(TriageScenarioReferenceRole.PatientCCtRoomPoint);
      _patientAUiPanel ??= Panel(TriageScenarioReferenceRole.PatientAUiPanel);
      _patientDummyDAUiPanel ??= Panel(TriageScenarioReferenceRole.PatientDummyDAUiPanel);
      _patientBUiPanel ??= Panel(TriageScenarioReferenceRole.PatientBUiPanel);
      _patientCUiPanel ??= Panel(TriageScenarioReferenceRole.PatientCUiPanel);
      _patientDummyDBUiPanel ??= Panel(TriageScenarioReferenceRole.PatientDummyDBUiPanel);
      _suctionChecklistUiPanel ??= Panel(TriageScenarioReferenceRole.SuctionChecklistUiPanel);
      _intuChecklistUiPanel ??= Panel(TriageScenarioReferenceRole.IntubationChecklistUiPanel);
      _ivChecklistUiPanel ??= Panel(TriageScenarioReferenceRole.IntravenousChecklistUiPanel);
      _ctTransferFadePanel ??= Panel(TriageScenarioReferenceRole.CtTransferFadePanel);

      // OverworldScene에서 의미가 식별자로 확정되는 기존 Waypoint는 마커가 유실되어도
      // 정확한 식별자와 유일성 검사를 통해 복구한다. 이름이나 열거 순서에는 의존하지 않는다.
      _triageArrivalPoint ??= ResolveUniqueWaypoint("scen_b:quest_arrival_triage_area");
      _patientBCtRoomPoint ??= ResolveUniqueWaypoint("ct:patient_target_pos_b");
      _patientCCtRoomPoint ??= ResolveUniqueWaypoint("ct:patient_target_pos_c");
    }

    private Transform ResolveUniqueWaypoint(string identifier)
    {
      WaypointAnchor match = null;
      var waypoints = FindObjectsByType<WaypointAnchor>(
        FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
      foreach (var waypoint in waypoints)
      {
        if (waypoint == null || !string.Equals(waypoint.Identifier, identifier, StringComparison.Ordinal))
          continue;
        if (match != null)
        {
          Debug.LogError(
            $"[{nameof(TriageScenarioEventBootstrap)}] Waypoint identifier '{identifier}' is duplicated; automatic recovery was skipped.",
            this);
          return null;
        }
        match = waypoint;
      }
      return match != null ? match.transform : null;
    }
  }
}
