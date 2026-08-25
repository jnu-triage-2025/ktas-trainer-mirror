using System.Collections;
using System.Collections.Generic;
using FishNet;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Scenario;
using TriageTrainer.Entity;
using TriageTrainer.Entity.LineConnection;
using TriageTrainer.Entity.PatientMonitor.Models;
using UnityEngine;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private const float SetupMovePatientsDiagnosticDelaySeconds = 5f;

    /// <summary>
    /// 준비가 끝나지 않아도 그래프가 영구히 멈추지 않게 하는 상한.
    /// 이 이벤트는 WaitUntilDone 으로 호출되므로 대기가 끝나지 않으면 시나리오 전체가 정지한다.
    /// </summary>
    private const float SetupMovePatientsTimeoutSeconds = 20f;

    [Header("setup_move_patients")]
    [Tooltip("환자 B가 CT 이송 직전에 머무는 처치 구역(PatientCareDescriptionZone) 식별자.")]
    [SerializeField] private string _patientBCareZoneIdentifier = "zone_0";
    [Tooltip("환자 C가 CT 이송 직전에 머무는 처치 구역(PatientCareDescriptionZone) 식별자.")]
    [SerializeField] private string _patientCCareZoneIdentifier = "zone_1";

    private void RegisterEvent_SetupMovePatients()
    {
      Register("setup_move_patients", Event_SetupMovePatients);
    }

    private IEnumerator Event_SetupMovePatients()
    {
      float startedAt = Time.realtimeSinceStartup;
      bool diagnosticLogged = false;
      string reasonB = null;
      string reasonC = null;
      _setupMovePatientsAppliedDisplays.Clear();

      while (true)
      {
        ResolveRuntimeReferencesIfNeeded();
        ResolvePatientVitalMonitor(_patientBObject, ref _patientBVitalMonitorObject,
          ref _patientBVitalMonitorController);
        ResolvePatientVitalMonitor(_patientCObject, ref _patientCVitalMonitorObject,
          ref _patientCVitalMonitorController);

        bool preparedB = PreparePatientForCtTransport(_patientBObject, _patientBTreatmentBedObject,
          _patientBVitalMonitorObject, _patientBVitalMonitorController, _patientBCareZoneIdentifier,
          PatientController.TreatmentDisplay.Syringe20GInsertedIntoRightArm, out reasonB);
        bool preparedC = PreparePatientForCtTransport(_patientCObject, _patientCTreatmentBedObject,
          _patientCVitalMonitorObject, _patientCVitalMonitorController, _patientCCareZoneIdentifier,
          PatientController.TreatmentDisplay.Syringe20GInsertedIntoLeftArm, out reasonC);
        if (preparedB && preparedC)
        {
          break;
        }

        float elapsed = Time.realtimeSinceStartup - startedAt;
        if (!diagnosticLogged && elapsed >= SetupMovePatientsDiagnosticDelaySeconds)
        {
          diagnosticLogged = true;
          Debug.LogWarning(
            "[TriageScenarioEventBootstrap] setup_move_patients is still waiting for all B/C care equipment. "
            + $"patient_b: {reasonB ?? "ok"}, patient_c: {reasonC ?? "ok"}", this);
        }

        if (elapsed >= SetupMovePatientsTimeoutSeconds)
        {
          // 연출이 어긋나더라도 그래프를 영구히 멈추는 것보다는 다음 단계로 넘기는 편이 낫다.
          Debug.LogError(
            "[TriageScenarioEventBootstrap] setup_move_patients timed out; the CT stage will continue with an "
            + $"incomplete setup. patient_b: {reasonB ?? "ok"}, patient_c: {reasonC ?? "ok"}", this);
          break;
        }

        // 준비가 끝나기 전에는 CT 단계로 진행하면 안 된다.
        yield return null;
      }

      // 처치 표현을 여러 개 켰으므로 늦게 들어온 피어까지 전체 상태로 정렬한다(BufferLast 는 1개만 남는다).
      SyncPatientDisplayStatesIfServer(_patientBObject);
      SyncPatientDisplayStatesIfServer(_patientCObject);

      yield return ApplyMonitorProfile(_patientBVitalMonitorObject,
        _patientBVitalPanel,
        _patientBVitalMonitorController,
        _patientBInitialMonitorParameters,
        _applyPatientBInitialMonitorProfile);
      yield return ApplyMonitorProfile(_patientCVitalMonitorObject,
        _patientCVitalPanel,
        _patientCVitalMonitorController,
        _patientCInitialMonitorParameters,
        _applyPatientCInitialMonitorProfile);
    }

    /// <summary>
    /// 환자 한 명을 CT 이송 직전 상태로 맞춘다.
    ///
    /// <para>맞추는 항목은 세 가지다. (1) 처치 구역의 환자 모니터가 이 환자를 감시하고,
    /// (2) 처치 구역의 산소 유량계가 설치·조작된 채 이 환자에게 연결되어 있고,
    /// (3) 환자가 누워 있는 침대의 N/S 수액이 걸려 있고 그 수액이 환자 정맥로에 연결되어 있어야 한다.</para>
    ///
    /// <para>모두 성립하면 true 를 돌려준다. 아직이면 <paramref name="unmetReason"/> 에 남은 조건을
    /// 담아 false 를 돌려주고, 호출자가 다음 프레임에 다시 시도한다.</para>
    /// </summary>
    private bool PreparePatientForCtTransport(GameObject patientObject, GameObject bedObject,
      GameObject monitorObject, PatientMonitorController monitor, string careZoneIdentifier,
      PatientController.TreatmentDisplay cannulaDisplay, out string unmetReason)
    {
      unmetReason = null;
      var patient = patientObject != null ? patientObject.GetComponentInChildren<PatientController>(true) : null;
      if (patient == null)
      {
        unmetReason = "환자 오브젝트를 해석하지 못했다";
        return false;
      }

      var bed = ResolveBedForCtTransport(patient, patientObject, bedObject);
      if (bed == null)
      {
        unmetReason = "환자가 누워 있는 침대를 해석하지 못했다";
        return false;
      }

      // ── (1) 처치 구역 환자 모니터 ──
      SetActiveIfPresent(monitorObject, true);
      if (monitor != null && !ReferenceEquals(patient.MonitoringPatientMonitor, monitor))
      {
        // 환자 측 역참조만 바꾸면 모니터 화면은 그대로 비어 있다. 모니터 측 API 가 양방향을 묶는다.
        monitor.SetMonitoringPatient(patient);
      }

      // ── (2) 처치 구역 산소 유량계 ──
      var careZone = FindCareZone(careZoneIdentifier, patient);
      var flowmeter = careZone != null && careZone.Oxyflowmeters.Count == 1 ? careZone.Oxyflowmeters[0] : null;
      // 구역이 아직 장비를 스캔하지 못했을 수 있다. 정적 레이아웃 식별자(zone_0:oxyflowmeter)로 한 번 더 찾는다.
      flowmeter ??= FindOxyflowmeterByEntityIdentifier(careZoneIdentifier + ":oxyflowmeter");
      if (flowmeter != null)
      {
        ApplyFlowmeterForScenario(flowmeter);
        if (!ReferenceEquals(patient.ConnectedOxyflowmeter, flowmeter))
        {
          patient.SetConnectedOxyflowmeter(flowmeter);
        }

        // 산소 라인은 설치가 아니라 "조작"까지 끝나야 만들어진다. 정상 진행에서 nurse D 가 마친 상태다.
        string operateSignal = flowmeter.ResolveAttachedInteractSignal();
        if (!string.IsNullOrWhiteSpace(operateSignal) && !flowmeter.IsAttachedInteractCompleted)
        {
          ScenarioInteractionSignals.Raise(operateSignal);
        }

        // 비강 캐뉼라가 없으면 환자 측 산소 포트가 비활성이라 라인이 성립하지 않는다.
        ApplyTreatmentDisplayOnce(patient, PatientController.TreatmentDisplay.NasalCannulaApplied);

        var flowmeterPort = flowmeter.OxyLineConnectionPoint;
        var patientOxygenPort = patient.OxygenMaskAttachmentPoint;
        if (careZone != null
            && (flowmeterPort == null || patientOxygenPort == null
                || !flowmeterPort.IsPhysicallyConnectedTo(patientOxygenPort)))
        {
          careZone.TryReconcileOxygenLineFor(flowmeter);
        }
      }

      // ── (3) 침대 N/S 수액 → 환자 정맥로 ──
      bed.EnsureNormalSalineInstalledForScenario();
      ApplyTreatmentDisplayOnce(patient, cannulaDisplay);

      bool salineInstalled = bed.TryGetNormalSalineConnectionPoint(out var salinePoint);
      var patientIvPoint = patient.PatientBCIvAttachmentPoint;
      if (salineInstalled && patientIvPoint != null && !patientIvPoint.IsPhysicallyConnectedTo(salinePoint))
      {
        var lineService = LineConnectionService.TopologyService
                          ?? FindFirstObjectByType<LineConnectionService>(FindObjectsInactive.Include);
        lineService?.TryCreateAutomaticConnection(salinePoint, patientIvPoint);
        patient.TryCompletePatientBCNormalSalineConnection(salinePoint);
      }

      // ── 판정 ──
      if (!ReferenceEquals(patient.CurrentBed, bed))
      {
        unmetReason = "환자와 침대 결합이 아직 성립하지 않았다";
        return false;
      }

      if (monitor == null || !ReferenceEquals(patient.MonitoringPatientMonitor, monitor))
      {
        unmetReason = $"처치 구역 '{careZoneIdentifier}' 의 환자 모니터 연결이 성립하지 않았다";
        return false;
      }

      if (flowmeter == null)
      {
        unmetReason = careZone == null
          ? $"처치 구역 '{careZoneIdentifier}' 을 찾지 못했다"
          : $"처치 구역 '{careZoneIdentifier}' 안의 산소 유량계가 정확히 1개가 아니다";
        return false;
      }

      if (!flowmeter.IsAttached || !ReferenceEquals(patient.ConnectedOxyflowmeter, flowmeter))
      {
        unmetReason = "산소 유량계 설치 또는 환자 연결이 성립하지 않았다";
        return false;
      }

      // 유량계나 환자 프리팹에 산소 포트가 배선되지 않은 구성에서는 라인을 만들 수 없으므로,
      // 양쪽 포트가 모두 있을 때만 실제 라인 연결까지 요구한다.
      var flowmeterOxyPoint = flowmeter.OxyLineConnectionPoint;
      var patientOxyPoint = patient.OxygenMaskAttachmentPoint;
      if (flowmeterOxyPoint != null && patientOxyPoint != null
          && !flowmeterOxyPoint.IsPhysicallyConnectedTo(patientOxyPoint))
      {
        unmetReason = "산소 라인이 아직 연결되지 않았다";
        return false;
      }

      if (!salineInstalled)
      {
        unmetReason = "침대 N/S 수액이 설치되지 않았다";
        return false;
      }

      if (patientIvPoint == null)
      {
        unmetReason = "환자 정맥로 IV 연결 지점이 배선되지 않았다";
        return false;
      }

      if (!patientIvPoint.IsPhysicallyConnectedTo(salinePoint))
      {
        unmetReason = "환자 정맥로와 N/S 수액이 아직 연결되지 않았다";
        return false;
      }

      return true;
    }

    /// <summary>
    /// CT 이송 준비에 쓸 침대를 고른다. 프리셋 스폰 침대는 씬 참조가 없으므로 환자가 실제로
    /// 누워 있는 침대를 우선하고, 없을 때만 해석된 침대 오브젝트에 결합을 시도한다.
    /// </summary>
    private MovingPatientBedController ResolveBedForCtTransport(PatientController patient,
      GameObject patientObject, GameObject bedObject)
    {
      var attachedBed = patient.CurrentBed;
      if (attachedBed != null)
      {
        return attachedBed;
      }

      var bed = bedObject != null ? bedObject.GetComponentInChildren<MovingPatientBedController>(true) : null;
      if (bed == null)
      {
        return null;
      }

      SetActiveIfPresent(bedObject, true);
      TryAttachPatientToBed(patientObject, bedObject, patient.Identifier);
      return patient.CurrentBed != null ? patient.CurrentBed : bed;
    }

    private static WallAttachedOxyflowmeter FindOxyflowmeterByEntityIdentifier(string entityIdentifier)
    {
      if (string.IsNullOrWhiteSpace(entityIdentifier))
      {
        return null;
      }

      foreach (var candidate in FindObjectsByType<WallAttachedOxyflowmeter>(
                 FindObjectsInactive.Include, FindObjectsSortMode.None))
      {
        if (candidate != null
            && string.Equals(candidate.EntityIdentifier, entityIdentifier, System.StringComparison.Ordinal))
        {
          return candidate;
        }
      }

      return null;
    }

    /// <summary>
    /// 대기 루프가 매 프레임 같은 처치 표현을 다시 켜서 RPC 를 반복하지 않도록 한 번만 적용한다.
    /// 환자 모델이 표현을 지원하지 않으면 플래그가 끝내 켜지지 않으므로, 실제 상태가 아니라
    /// 이 이벤트가 이미 시도했는지를 기준으로 판단한다.
    /// </summary>
    private void ApplyTreatmentDisplayOnce(PatientController patient,
      PatientController.TreatmentDisplay display)
    {
      if (patient == null
          || patient.IsTreatmentDisplayActive(display)
          || !_setupMovePatientsAppliedDisplays.Add((patient.GetInstanceID(), display)))
      {
        return;
      }

      patient.SetTreatmentDisplayNetworked(display, true);
    }

    private readonly HashSet<(int, PatientController.TreatmentDisplay)> _setupMovePatientsAppliedDisplays = new();

    private static void SyncPatientDisplayStatesIfServer(GameObject patientObject)
    {
      if (!InstanceFinder.IsServerStarted)
      {
        return;
      }

      var patient = patientObject != null ? patientObject.GetComponentInChildren<PatientController>(true) : null;
      patient?.SyncAllDisplayStatesNetworked();
    }

    private static void ApplyFlowmeterForScenario(WallAttachedOxyflowmeter flowmeter)
    {
      if (flowmeter == null || flowmeter.IsAttached)
      {
        return;
      }

      if (InstanceFinder.IsServerStarted)
      {
        var broadcaster = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);
        broadcaster?.ApplyStaticObjectDisplaymentForScenario(flowmeter.EntityIdentifier);
      }
      else if (!InstanceFinder.IsClientStarted)
      {
        // 오프라인 테스트/단독 실행에는 RPC 경로가 없으므로 로컬 표현을 직접 적용한다.
        flowmeter.ApplyShownFromNetwork();
        flowmeter.OnShownConfirmed();
      }
    }

    /// <summary>
    /// 처치 구역을 찾는다. 정적 엔티티 레이아웃은 구역 식별자에 접미사를 붙여
    /// <c>zone_0:zone</c> 형태로 배치하므로, 짧은 이름(<c>zone_0</c>)도 함께 받아들인다.
    /// 그래도 못 찾으면 환자가 실제로 서 있는 구역으로 대체한다.
    /// </summary>
    private static PatientCareDescriptionZone FindCareZone(string identifier, PatientController patient)
    {
      var zones = FindObjectsByType<PatientCareDescriptionZone>(
        FindObjectsInactive.Include, FindObjectsSortMode.None);

      if (!string.IsNullOrWhiteSpace(identifier))
      {
        string suffixed = identifier + ":zone";
        foreach (var zone in zones)
        {
          if (zone == null)
          {
            continue;
          }

          if (string.Equals(zone.Identifier, identifier, System.StringComparison.Ordinal)
              || string.Equals(zone.Identifier, suffixed, System.StringComparison.Ordinal))
          {
            return zone;
          }
        }
      }

      if (patient == null)
      {
        return null;
      }

      foreach (var zone in zones)
      {
        if (zone != null
            && (ReferenceEquals(zone.CurrentPatient, patient)
                || zone.ContainsWorldPosition(patient.transform.position)))
        {
          return zone;
        }
      }

      return null;
    }
  }
}
