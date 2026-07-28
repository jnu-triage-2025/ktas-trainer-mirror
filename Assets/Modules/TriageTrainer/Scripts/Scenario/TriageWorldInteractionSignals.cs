using System.Linq;
using MultiplayerInfrastructure.Scenario;

namespace TriageTrainer.Scenario
{
  /// <summary>트리아지 월드 오브젝트가 자동으로 발생시키는 시나리오 신호의 규칙.</summary>
  public static class TriageWorldInteractionSignals
  {
    public static void RaiseWallSuctionInstalled(string equipmentIdentifier) => Raise("wall_suction_installed", equipmentIdentifier);
    public static void RaiseWallSuctionRemoved(string equipmentIdentifier) => Raise("wall_suction_removed", equipmentIdentifier);
    public static void RaiseOxyflowmeterInstalled(string equipmentIdentifier) => Raise("oxyflowmeter_installed", equipmentIdentifier);
    public static void RaiseOxyflowmeterRemoved(string equipmentIdentifier) => Raise("oxyflowmeter_removed", equipmentIdentifier);
    public static void RaiseCareZonePatientEntered(string zoneIdentifier, string patientIdentifier) => Raise("carezone_patient_entered", zoneIdentifier, patientIdentifier);
    public static void RaiseCareZonePatientExited(string zoneIdentifier, string patientIdentifier) => Raise("carezone_patient_exited", zoneIdentifier, patientIdentifier);
    public static void RaiseCareZoneBedEntered(string zoneIdentifier, string bedIdentifier) => Raise("carezone_bed_entered", zoneIdentifier, bedIdentifier);
    public static void RaiseCareZoneBedExited(string zoneIdentifier, string bedIdentifier) => Raise("carezone_bed_exited", zoneIdentifier, bedIdentifier);
    public static void RaiseCareZoneBedSnapped(string zoneIdentifier, string bedIdentifier, string pointIdentifier) => Raise("carezone_bed_snapped", zoneIdentifier, bedIdentifier, pointIdentifier);

    private static void Raise(string name, params string[] identifiers)
    {
      if (identifiers.Any(string.IsNullOrWhiteSpace)) return;
      ScenarioInteractionSignals.Raise(string.Join("_", new[] { name }.Concat(identifiers)));
    }
  }
}
