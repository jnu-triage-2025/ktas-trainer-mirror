using System;
using FishNet;
using MultiplayerInfrastructure.Logging;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Scenario;
using TriageTrainer.Entity;
using TriageTrainer.Entity.CentralLine;
using TriageTrainer.Entity.LineConnection;
using TriageTrainer.Entity.OxyLine;
using UnityEngine;

namespace TriageTrainer.Scenario
{
  /// <summary>
  /// 환자 A 수동 진입 준비 체인의 장비 설치와 라인 연결 복원.
  ///
  /// <para>
  /// 처치 표현(Display State)과 처치 상태값만 켜면 화면은 그럴듯해 보이지만, 실제 라인
  /// 오브젝트와 장비 참조가 없으면 이후 상호작용이 조용히 실패한다. CPR 1주기의 "T-piece 분리"는
  /// T-piece가 유량계와 실제로 이어져 있다는 전제이고, Level 1 급속 주입기는 수액·혈액백·C-line
  /// 연결을 자체 상태로 따로 들고 있다. 그래서 준비 체인이 표현과 함께 아래 연결까지 서버
  /// 권위로 되돌린다.
  /// </para>
  ///
  /// <para>
  /// 지나간 단계의 진행 신호를 인위적으로 다시 올리지는 않는다. 신호를 되살리면 다음 단계
  /// 게이트가 실제 처치 없이 통과할 수 있다. 예외는 벽면 유량계의 "조작" 신호 하나인데, 그 값이
  /// <see cref="WallAttachedOxyflowmeter.IsAttachedInteractCompleted"/> 가 읽는 조작 완료 상태
  /// 자체이고 산소 라인 성립 조건이기 때문이다(일반 진행의 <c>setup_move_patients</c> 도 같다).
  /// </para>
  /// </summary>
  public partial class TriageScenarioEventBootstrap
  {
    /// <summary>환자 A 처치실의 벽면 산소 유량계 배치 식별자.</summary>
    private const string PatientAOxyflowmeterEntityId = "zone_a:oxyflowmeter";

    /// <summary>환자 A 처치실의 벽면 흡인기 배치 식별자.</summary>
    private const string PatientAWallSuctionEntityId = "zone_a:wall_suction";

    /// <summary>환자 A 처치실의 처치 구역 식별자. 산소 라인 성립을 이 구역이 판정한다.</summary>
    private const string PatientACareZoneIdentifier = "zone_a";

    /// <summary>환자 A에게 수액을 빠르게 넣는 Level 1 급속 주입기 엔티티 식별자.</summary>
    private const string PatientALevel1RapidInfuserEntityId = "level1_rapid_infuser_a";

    /// <summary>서버(또는 오프라인) 컨텍스트에서만 월드 상태를 바꿀 수 있다.</summary>
    private static bool CanMutateManualEntryWorldState =>
      InstanceFinder.IsOffline || InstanceFinder.IsServerStarted;

    private static LineConnectionService ResolveLineConnectionService()
      => LineConnectionService.TopologyService
         ?? FindFirstObjectByType<LineConnectionService>(FindObjectsInactive.Include);

    /// <summary>
    /// 초기 평가와 의사 지시 단계(P003·P004)에서 완료된 장비 설치와 라인 연결을 되돌린다.
    /// 벽면 흡인기·산소 유량계, 기관내관 T-piece 산소, 양측 정맥로, C-line 과 Level 1 급속
    /// 주입기가 대상이다.
    /// </summary>
    private void RestorePatientAPreArrestEquipment(PatientController patient)
    {
      if (patient == null || !CanMutateManualEntryWorldState)
        return;

      RestorePatientAWallEquipment(patient);
      RestorePatientAOxygenLine(patient);
      RestorePatientAIntravenousLines(patient);
      RestorePatientALevel1RapidInfuser(patient);

      // 처치 표현을 여러 개 켠 뒤이므로 늦게 들어온 피어까지 전체 표시 상태로 정렬한다.
      // 개별 표시 RPC 는 BufferLast 가 마지막 1건만 남긴다.
      if (InstanceFinder.IsServerStarted)
        patient.SyncAllDisplayStatesNetworked();
    }

    /// <summary>
    /// 벽면 흡인기와 산소 유량계를 설치 상태로 둔다. 흡인기는 초기 평가 단계(구강 흡인),
    /// 유량계는 의사 지시 단계(산소 공급)에서 각각 설치된다.
    /// 흡인기의 환자 참조는 처치 구역이 관리하므로 여기서 덮어쓰지 않는다.
    /// </summary>
    private void RestorePatientAWallEquipment(PatientController patient)
    {
      var suction = FindWallSuctionByEntityIdentifier(PatientAWallSuctionEntityId);
      if (suction != null)
      {
        ApplyStaticEquipmentForManualEntry(suction.EntityIdentifier, suction.IsAttached,
          suction.ApplyShownFromNetwork, suction.OnShownConfirmed);
      }

      var flowmeter = FindOxyflowmeterByEntityIdentifier(PatientAOxyflowmeterEntityId);
      if (flowmeter == null)
      {
        Debug.LogWarning(
          "[TriageScenarioEventBootstrap] Manual entry could not resolve the patient A oxyflowmeter "
          + $"'{PatientAOxyflowmeterEntityId}'; the oxygen line cannot be restored.", this);
        return;
      }

      ApplyStaticEquipmentForManualEntry(flowmeter.EntityIdentifier, flowmeter.IsAttached,
        flowmeter.ApplyShownFromNetwork, flowmeter.OnShownConfirmed);
      if (!ReferenceEquals(patient.ConnectedOxyflowmeter, flowmeter))
        patient.SetConnectedOxyflowmeter(flowmeter);
    }

    /// <summary>
    /// 벽면 설치 장비를 설치 완료 상태로 표시한다. 서버는 표시 상태 서비스를 거쳐 전 피어에
    /// 전파하고, 오프라인은 RPC 경로가 없으므로 로컬 표현을 직접 적용한다
    /// (<c>setup_move_patients</c> 의 <c>ApplyFlowmeterForScenario</c> 와 같은 규약).
    ///
    /// <para>
    /// 순수 클라이언트에서는 아무것도 하지 않는다. 설치 상태는 서버 권위 데이터이므로, 승인 없이
    /// 로컬 표현만 켜면 그 피어에서만 장비가 설치된 것처럼 보인다.
    /// </para>
    /// </summary>
    private static void ApplyStaticEquipmentForManualEntry(
      string entityIdentifier, bool alreadyAttached,
      Action applyShownFromNetwork, Action onShownConfirmed)
    {
      if (alreadyAttached || string.IsNullOrWhiteSpace(entityIdentifier))
        return;

      if (InstanceFinder.IsServerStarted)
      {
        var broadcaster = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);
        broadcaster?.ApplyStaticObjectDisplaymentForScenario(entityIdentifier);
        return;
      }

      if (InstanceFinder.IsClientStarted)
        return;

      applyShownFromNetwork?.Invoke();
      onShownConfirmed?.Invoke();
    }

    /// <summary>
    /// T-piece와 벽면 유량계 사이의 산소 라인을 되돌린다. 연결 판정은 처치 구역이 담당하므로
    /// 구역에 먼저 위임하고, 구역이 없거나 성립하지 않으면 두 포트를 직접 잇는다.
    ///
    /// <para>
    /// 라인이 만들어지면 환자 측 끝점이 산소 공급 완료 처치 상태와 진행 신호를 스스로 기록한다.
    /// 이는 신호를 인위적으로 되살리는 것이 아니라 실제 연결이 성립한 결과이므로 그대로 둔다.
    /// 그 신호를 기다리는 노드는 이미 지나간 단계에 있어서 지금은 대기하지 않는다.
    /// </para>
    /// </summary>
    private void RestorePatientAOxygenLine(PatientController patient)
    {
      var flowmeter = patient.ConnectedOxyflowmeter;
      if (flowmeter == null || !flowmeter.IsAttached)
        return;

      // 산소 라인은 설치가 아니라 "조작"까지 끝나야 성립한다. 이 신호가 곧 조작 완료 상태다.
      string operateSignal = flowmeter.ResolveAttachedInteractSignal();
      if (!string.IsNullOrWhiteSpace(operateSignal) && !flowmeter.IsAttachedInteractCompleted)
        ScenarioInteractionSignals.Raise(operateSignal);

      var flowmeterPort = flowmeter.OxyLineConnectionPoint;
      var patientPort = ResolvePatientAOxyLinePort(patient);
      if (flowmeterPort != null && patientPort != null
          && flowmeterPort.IsPhysicallyConnectedTo(patientPort))
        return;

      var careZone = FindCareZone(PatientACareZoneIdentifier, patient);
      careZone?.TryReconcileOxygenLineFor(flowmeter);

      // 처치 구역이 환자를 아직 감지하지 못한 프레임에도 준비 체인은 결과를 확정해야 한다.
      if (flowmeterPort == null || patientPort == null)
      {
        GameLogService.WriteScenario(
          "Manual entry could not restore the patient A oxygen line: an oxy port is not wired.",
          "patient_a_critical");
        return;
      }

      if (flowmeterPort.IsPhysicallyConnectedTo(patientPort))
        return;

      // 라인이 실제로 만들어지면 환자 측 끝점의 OxyLineConnectionPoint 가 산소 공급 완료를
      // 스스로 기록한다. 준비 체인이 그 처리를 대신하지 않는다.
      var service = ResolveLineConnectionService();
      if (service != null && service.TryCreateAutomaticConnection(flowmeterPort, patientPort))
        return;

      GameLogService.WriteScenario(
        "Manual entry could not restore the patient A oxygen line: the line service rejected it.",
        "patient_a_critical");
    }

    /// <summary>
    /// CPR 2주기 이후 단계에서 T-piece 산소 라인을 끊는다. 그 단계는 T-piece가 분리되고 앰부백에
    /// 산소가 연결된 상태이므로, 준비 체인이 되돌린 T-piece 라인을 그대로 두면 표시와 라인이
    /// 어긋난다. 산소 공급 자체는 계속되므로 처치 상태값은 단계 스냅샷이 그대로 관리한다.
    ///
    /// <para>
    /// T-piece 처치 표현을 끄기 전에 호출해야 한다. 환자 측 포트는 그 표현의 자식이라
    /// 표현을 먼저 끄면 비활성이 되어 해석되지 않는다.
    /// </para>
    /// </summary>
    private void DisconnectPatientAOxygenLineForCpr2(PatientController patient)
    {
      if (patient == null || !CanMutateManualEntryWorldState)
        return;

      var flowmeterPort = patient.ConnectedOxyflowmeter?.OxyLineConnectionPoint;
      var patientPort = ResolvePatientAOxyLinePort(patient);
      if (flowmeterPort == null || patientPort == null
          || !flowmeterPort.IsPhysicallyConnectedTo(patientPort))
        return;

      ResolveLineConnectionService()?.DisconnectAutomaticConnection(flowmeterPort, patientPort);
    }

    /// <summary>
    /// 환자 A의 산소 포트를 찾는다. 환자 A 프리팹은 T-piece 처치 표현 자식에 포트를 두고
    /// <c>OxygenMaskAttachmentPoint</c> 참조는 비워 두므로, 처치 구역과 같은 규칙으로
    /// "활성 산소 포트가 정확히 하나일 때"만 그 포트를 사용한다.
    /// </summary>
    private static OxyLineConnectionPoint ResolvePatientAOxyLinePort(PatientController patient)
    {
      if (patient == null)
        return null;

      var configured = patient.OxygenMaskAttachmentPoint;
      if (configured != null)
        return configured;

      OxyLineConnectionPoint resolved = null;
      var points = patient.GetComponentsInChildren<OxyLineConnectionPoint>(true);
      for (int i = 0; i < points.Length; i++)
      {
        if (points[i] == null || !points[i].isActiveAndEnabled)
          continue;
        if (resolved != null)
          return null;
        resolved = points[i];
      }
      return resolved;
    }

    /// <summary>
    /// 좌측 생리식염수와 우측 플라즈마 솔루션을 침대 수액걸이에 걸고 환자 정맥로에 잇는다.
    /// 캐뉼라 표현이 켜져 있어야 환자 측 포트가 활성이므로 표현 복원 뒤에 호출한다.
    /// </summary>
    private void RestorePatientAIntravenousLines(PatientController patient)
    {
      var bed = patient.CurrentBed;
      if (bed == null)
      {
        GameLogService.WriteScenario(
          "Manual entry could not restore the patient A intravenous lines: the patient is not on a bed.",
          "patient_a_critical");
        return;
      }

      bed.EnsureNormalSalineInstalledForScenario();
      bed.EnsurePlasmaSolutionInstalledForScenario();

      bool leftConnected = patient.RestorePatientAFluidConnectionForScenario(isLeftArm: true);
      bool rightConnected = patient.RestorePatientAFluidConnectionForScenario(isLeftArm: false);
      GameLogService.WriteScenario(
        "Manual entry restored the patient A intravenous lines: "
        + $"normal saline={leftConnected}, plasma solution={rightConnected}.",
        "patient_a_critical");
    }

    /// <summary>
    /// Level 1 급속 주입기에 플라즈마 솔루션과 혈액백을 채우고 환자 C-line 에 잇는다.
    /// 주입기의 수액 보유는 라인이 아니라 자체 상태이므로 상태와 라인을 함께 맞춘다.
    /// </summary>
    private void RestorePatientALevel1RapidInfuser(PatientController patient)
    {
      var infuserObject = ResolveEntityObject(null, PatientALevel1RapidInfuserEntityId);
      var infuser = infuserObject != null
        ? infuserObject.GetComponent<Level1RapidInfuserController>()
        : null;
      if (infuser == null)
      {
        GameLogService.WriteScenario(
          $"Manual entry could not resolve '{PatientALevel1RapidInfuserEntityId}'; "
          + "the rapid infuser state was not restored.",
          "patient_a_critical");
        return;
      }

      infuser.ApplyState(new Level1RapidInfuserState
      {
        HasNormalSaline = infuser.HasNormalSaline,
        HasPlasmaSolution = true,
        HasBloodBag = true,
        ConnectedPatientIdentifier = patient.Identifier
      });

      var patientPoint = patient.CentralLineAttachmentPoint;
      var infuserPoint = infuser.GetComponentInChildren<CentralLineConnectionPoint>(true);
      if (patientPoint == null || infuserPoint == null)
      {
        GameLogService.WriteScenario(
          "Manual entry could not restore the patient A C-line: a central line port is not wired.",
          "patient_a_critical");
        return;
      }

      if (infuserPoint.IsPhysicallyConnectedTo(patientPoint))
        return;

      var service = ResolveLineConnectionService();
      if (service != null && service.TryCreateAutomaticConnection(infuserPoint, patientPoint))
      {
        GameLogService.WriteScenario(
          "Manual entry connected the patient A C-line to the Level 1 rapid infuser.",
          "patient_a_critical");
        return;
      }

      GameLogService.WriteScenario(
        "Manual entry could not restore the patient A C-line: the line service rejected it.",
        "patient_a_critical");
    }

    /// <summary>
    /// 제세동 카트를 환자 침대에서 상호작용할 수 있는 초기 위치에 둔다.
    ///
    /// <para>
    /// 정박 지점 위에 그대로 올려 두면 카트가 즉시 정박하면서 도달 신호가 올라가고, CPR 1주기의
    /// 제세동기 목표(카트를 환자 옆으로 밀고 오기)가 아무 행동 없이 통과한다. 그래서 정박 판정
    /// 거리 밖, 그러나 곧바로 밀어 옮길 수 있는 거리에 배치한다.
    /// </para>
    /// </summary>
    private void PlacePatientADefibrillatorCartAtInitialPosition()
    {
      if (!CanMutateManualEntryWorldState)
        return;

      var cartObject = ResolveEntityObject(null, PatientADefibrillatorCartEntityId);
      var cart = cartObject != null ? cartObject.GetComponent<DefibrillatorCartController>() : null;
      if (cart == null)
      {
        GameLogService.WriteScenario(
          $"Manual entry could not resolve '{PatientADefibrillatorCartEntityId}'; "
          + "the defibrillator cart was not repositioned.",
          "patient_a_critical");
        return;
      }

      var snapPoint = FindDefibrillatorCartSnapPoint(PatientADefibrillatorCartSnapPointId);
      if (snapPoint == null)
      {
        Debug.LogWarning(
          "[TriageScenarioEventBootstrap] Manual entry could not find the defibrillator cart snap point "
          + $"'{PatientADefibrillatorCartSnapPointId}'; the cart was left where it was.", this);
        return;
      }

      float offsetDistance = snapPoint.SnapDistance + PatientADefibrillatorCartInitialOffsetMeters;
      Vector3 position = snapPoint.Position + snapPoint.Rotation * (Vector3.back * offsetDistance);
      cart.PlaceForScenario(position, snapPoint.Rotation);
      GameLogService.WriteScenario(
        $"Manual entry placed the patient A defibrillator cart near '{PatientADefibrillatorCartSnapPointId}'.",
        "patient_a_critical");
    }

    /// <summary>
    /// 제세동 카트가 환자 침대 옆에서 상호작용 가능해지는 정박 지점.
    /// CPR 1주기의 제세동기 목표(V025)가 이 지점 도달 신호를 기다린다.
    /// </summary>
    private const string PatientADefibrillatorCartSnapPointId = "defibrillatorcart_to_patient";

    /// <summary>
    /// 제세동 카트를 정박 지점에서 얼마나 떨어뜨려 둘지. 정박 판정 거리 밖이어야 하고,
    /// 플레이어가 곧바로 밀어 옮길 수 있을 만큼 가까워야 한다.
    /// </summary>
    private const float PatientADefibrillatorCartInitialOffsetMeters = 0.6f;

    private static DefibrillatorCartSnapPoint FindDefibrillatorCartSnapPoint(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return null;

      foreach (var point in FindObjectsByType<DefibrillatorCartSnapPoint>(
                 FindObjectsInactive.Include, FindObjectsSortMode.None))
      {
        if (point != null
            && string.Equals(point.Identifier, identifier, StringComparison.Ordinal))
          return point;
      }
      return null;
    }

    private static WallAttachedWallSuction FindWallSuctionByEntityIdentifier(string entityIdentifier)
    {
      if (string.IsNullOrWhiteSpace(entityIdentifier))
        return null;

      foreach (var candidate in FindObjectsByType<WallAttachedWallSuction>(
                 FindObjectsInactive.Include, FindObjectsSortMode.None))
      {
        if (candidate != null
            && string.Equals(candidate.EntityIdentifier, entityIdentifier, StringComparison.Ordinal))
          return candidate;
      }
      return null;
    }
  }
}
