using System.Linq;
using UnityEngine;
using MultiplayerInfrastructure.Scenario;

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
    public static void RaisePatientBedPositioningPointLatched(string bedIdentifier, string pointIdentifier) =>
      Raise("patient_bed_positioning_point_latched", bedIdentifier, pointIdentifier);
    public static void RaisePatientBedPositioningPointUnlatched(string bedIdentifier, string pointIdentifier) =>
      Raise("patient_bed_positioning_point_unlatched", bedIdentifier, pointIdentifier);
    public static void RaisePatientBedPositioningPointEnabled(string pointIdentifier) =>
      Raise("patient_bed_positioning_point_enabled", pointIdentifier);
    public static void RaisePatientBedPositioningPointDisabled(string pointIdentifier) =>
      Raise("patient_bed_positioning_point_disabled", pointIdentifier);
    public static void RaiseDefibCartSnapPointLatched(string cartIdentifier, string pointIdentifier) =>
      Raise("defib_cart_snap_point_latched", cartIdentifier, pointIdentifier);
    public static void RaiseDefibCartSnapPointUnlatched(string cartIdentifier, string pointIdentifier) =>
      Raise("defib_cart_snap_point_unlatched", cartIdentifier, pointIdentifier);
    public static void RaiseDefibCartSnapPointEnabled(string pointIdentifier) =>
      Raise("defib_cart_snap_point_enabled", pointIdentifier);
    public static void RaiseDefibCartSnapPointDisabled(string pointIdentifier) =>
      Raise("defib_cart_snap_point_disabled", pointIdentifier);

    private static void Raise(string name, params string[] identifiers)
    {
      if (identifiers.Any(string.IsNullOrWhiteSpace)) return;
      ScenarioInteractionSignals.Raise(string.Join("_", new[] { name }.Concat(identifiers)));
    }

    private static string GetIdentifier(MonoBehaviour equipment)
    {
      if (equipment == null) return string.Empty;
      if (equipment is MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment displayment)
        return displayment.EntityIdentifier;
      if (equipment is TriageTrainer.Entity.MovingPatientBedController bed)
        return bed.Identifier;
      return equipment.name;
    }
  }
}
