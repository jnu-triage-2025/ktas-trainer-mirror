using TriageTrainer.Entity.LineConnection;
using TriageTrainer.Entity;
using UnityEngine;

namespace TriageTrainer.Entity.OxyLine
{
  /// <summary>
  /// Marks the position where an oxygen line can be connected.
  /// Connection behavior will be implemented separately.
  /// </summary>
  public sealed class OxyLineConnectionPoint : LineConnectionPoint
  {
    public const float LineWidth = 0.01f;
    public const float Elasticity = 0.8f;
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
    }
  }
}
