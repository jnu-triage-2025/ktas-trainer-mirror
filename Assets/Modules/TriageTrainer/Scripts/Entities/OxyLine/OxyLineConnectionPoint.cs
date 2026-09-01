using TriageTrainer.Entity.LineConnection;
using UnityEngine;

namespace TriageTrainer.Entity.OxyLine
{
  /// <summary>
  /// 산소 라인을 연결할 수 있는 위치를 나타낸다.
  /// 연결 동작은 별도로 구현된다.
  /// </summary>
  public sealed class OxyLineConnectionPoint : LineConnectionPoint
  {
    public const float LineWidth = 0.035f;
    public const float Elasticity = 0.2f;
    public const string MaterialResourcePath = "Materials/LineConnectionService/OxyLine";

    public static Material DefaultMaterial => Resources.Load<Material>(MaterialResourcePath);

    public override void ApplyLineMaterial(LineRenderer lineRenderer)
    {
      base.ApplyLineMaterial(lineRenderer);
    }

    /// <summary>
    /// 서버가 산소 라인을 만든 직후, 환자 측 끝점에서만 B/C 산소 처치 완료를 판정한다.
    /// CareZone의 장비 참조 할당만으로는 완료하지 않고 실제 물리 라인이 존재할 때만 호출된다.
    /// </summary>
    public override void NotifyLineConnected(LineConnectionPoint other)
    {
      base.NotifyLineConnected(other);

      var patient = GetComponentInParent<PatientController>();
      if (patient == null || other?.GetComponentInParent<WallAttachedOxyflowmeter>() == null)
        return;

      patient.NotifyOxygenLineConnected();
      if (string.Equals(patient.Identifier, "patient_a", System.StringComparison.Ordinal))
      {
        patient.SetTreatmentApplied("oxygen_line_connected", true);
        MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Raise(
          "connect_tpiece_and_oxyflow");
      }
    }
  }
}
