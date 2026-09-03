using System.Linq;
using FishNet;
using MultiplayerInfrastructure.Scenario;
using UnityEngine;

namespace TriageTrainer.Scenario
{
  /// <summary>트리아지 월드 오브젝트가 자동으로 발생시키는 시나리오 신호의 규칙.</summary>
  public static class TriageWorldInteractionSignals
  {
    public static void RaiseWallSuctionInstalled(string equipmentIdentifier) => Raise("wall_suction_installed", equipmentIdentifier);
    public static void RaiseWallSuctionRemoved(string equipmentIdentifier) => Raise("wall_suction_removed", equipmentIdentifier);
    public static void RaiseWallSuctionEnabled(string equipmentIdentifier) => Raise("wall_suction_enabled", equipmentIdentifier);
    public static void RaiseWallSuctionDisabled(string equipmentIdentifier) => Raise("wall_suction_disabled", equipmentIdentifier);
    public static void RaiseOxyflowmeterInstalled(string equipmentIdentifier) => Raise("oxyflowmeter_installed", equipmentIdentifier);
    public static void RaiseOxyflowmeterRemoved(string equipmentIdentifier) => Raise("oxyflowmeter_removed", equipmentIdentifier);
    public static void RaiseOxyflowmeterEnabled(string equipmentIdentifier) => Raise("oxyflowmeter_enabled", equipmentIdentifier);
    public static void RaiseOxyflowmeterDisabled(string equipmentIdentifier) => Raise("oxyflowmeter_disabled", equipmentIdentifier);
    public static void RaiseCareZonePatientEntered(string zoneIdentifier, string patientIdentifier)
    {
      Raise("carezone_patient_entered", zoneIdentifier, patientIdentifier);
      // 시나리오가 씬별 zone 식별자에 결합되지 않고 "이 환자가 어떤 처치 구역에 도착했는가"만
      // 기다릴 수 있는 안정적인 환자 범위 신호도 함께 제공한다.
      Raise("carezone_patient_entered", patientIdentifier);
    }
    public static void RaiseCareZonePatientExited(string zoneIdentifier, string patientIdentifier) => Raise("carezone_patient_exited", zoneIdentifier, patientIdentifier);
    public static void RaiseCareZoneEnabled(string zoneIdentifier) => Raise("carezone_enabled", zoneIdentifier);
    public static void RaiseCareZoneDisabled(string zoneIdentifier) => Raise("carezone_disabled", zoneIdentifier);
    public static void RaiseCareZoneBedEntered(string zoneIdentifier, string bedIdentifier) => Raise("carezone_bed_entered", zoneIdentifier, bedIdentifier);
    public static void RaiseCareZoneBedExited(string zoneIdentifier, string bedIdentifier) => Raise("carezone_bed_exited", zoneIdentifier, bedIdentifier);
    public static void RaiseCareZoneBedSnapped(string zoneIdentifier, string bedIdentifier, string pointIdentifier) => Raise("carezone_bed_snapped", zoneIdentifier, bedIdentifier, pointIdentifier);
    public static void RaiseCareZonePatientEquipmentConnected(string zoneIdentifier, string patientIdentifier, string equipmentType, MonoBehaviour equipment) =>
      Raise("carezone_patient_equipment_connected", zoneIdentifier, patientIdentifier, equipmentType, GetIdentifier(equipment));
    public static void RaiseCareZonePatientEquipmentDisconnected(string zoneIdentifier, string patientIdentifier, string equipmentType, MonoBehaviour equipment) =>
      Raise("carezone_patient_equipment_disconnected", zoneIdentifier, patientIdentifier, equipmentType, GetIdentifier(equipment));
    public static void RaisePatientEquipmentConnected(string patientIdentifier, string equipmentType, MonoBehaviour equipment) =>
      Raise("patient_equipment_connected", patientIdentifier, equipmentType, GetIdentifier(equipment));
    public static void RaisePatientEquipmentDisconnected(string patientIdentifier, string equipmentType, MonoBehaviour equipment) =>
      Raise("patient_equipment_disconnected", patientIdentifier, equipmentType, GetIdentifier(equipment));
    public static void RaisePatientBedPositioningPointLatched(string bedIdentifier, string pointIdentifier)
    {
      Raise("patient_bed_positioning_point_latched", bedIdentifier, pointIdentifier);
      // 시나리오가 특정 포인트 식별자에 결합되지 않고 "이 침대가 어딘가에 정박했는가"만 기다릴 수 있는
      // 침대 범위 신호도 함께 제공한다.
      Raise("patient_bed_positioning_point_latched", bedIdentifier);
    }
    public static void RaisePatientBedPositioningPointUnlatched(string bedIdentifier, string pointIdentifier) =>
      Raise("patient_bed_positioning_point_unlatched", bedIdentifier, pointIdentifier);
    public static void RaisePatientBedPositioningPointEnabled(string pointIdentifier) =>
      Raise("patient_bed_positioning_point_enabled", pointIdentifier);
    public static void RaisePatientBedPositioningPointDisabled(string pointIdentifier) =>
      Raise("patient_bed_positioning_point_disabled", pointIdentifier);
    public static void RaiseDefibrillatorCartSnapPointLatched(string cartIdentifier, string pointIdentifier) =>
      Raise("defibrillator_cart_snap_point_latched", cartIdentifier, pointIdentifier);
    public static void RaiseDefibrillatorCartSnapPointUnlatched(string cartIdentifier, string pointIdentifier) =>
      Raise("defibrillator_cart_snap_point_unlatched", cartIdentifier, pointIdentifier);
    public static void RaiseDefibrillatorCartSnapPointEnabled(string pointIdentifier) =>
      Raise("defibrillator_cart_snap_point_enabled", pointIdentifier);
    public static void RaiseDefibrillatorCartSnapPointDisabled(string pointIdentifier) =>
      Raise("defibrillator_cart_snap_point_disabled", pointIdentifier);

    /// <summary>
    /// 이 피어에서 월드 신호를 올려야 하는지 여부.
    ///
    /// <para>
    /// 이 클래스의 신호는 모두 서버 권위 상태(환자·침대·장비 결합, 구역 점유, 설치물 표시 등)를
    /// 각 피어가 로컬로 재구성하는 과정에서 발생한다. 서버도 같은 경로를 실행해 동일한 신호를
    /// 스스로 올리므로, 클라이언트가 올린 같은 신호는 중복일 뿐이다. 게다가 클라이언트가 올린
    /// 신호는 그래프가 client-origin 으로 인가한 것만 서버에서 수락되기 때문에, 모든 피어에서
    /// 올리면 인가받지 못한 신호가 거부되면서 경고와 시스템 메시지만 쌓인다(빌드 클라이언트에서
    /// 관측됨). 그래서 이 경로는 서버 또는 오프라인 단독 실행에서만 수행한다. 서버가 기록한
    /// 신호는 미러 RPC 로 모든 클라이언트 로컬 레지스트리에 복제되므로 각 피어의 게이트 판정은
    /// 그대로 성립한다.
    /// </para>
    /// </summary>
    private static bool IsSignalOriginPeer => InstanceFinder.IsServerStarted || InstanceFinder.IsOffline;

    private static void Raise(string name, params string[] identifiers)
    {
      if (!IsSignalOriginPeer)
        return;
      if (identifiers.Any(string.IsNullOrWhiteSpace))
        return;
      ScenarioInteractionSignals.Raise(string.Join("_", new[] { name }.Concat(identifiers)));
    }

    private static string GetIdentifier(MonoBehaviour equipment)
    {
      if (equipment == null)
        return string.Empty;
      if (equipment is MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment displayment)
        return displayment.EntityIdentifier;
      if (equipment is TriageTrainer.Entity.MovingPatientBedController bed)
        return bed.Identifier;
      return equipment.name;
    }
  }
}
