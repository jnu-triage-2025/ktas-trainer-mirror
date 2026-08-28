using TriageTrainer.Entity.LineConnection;
using UnityEngine;

namespace TriageTrainer.Entity.CentralLine
{
  /// <summary>
  /// 중심정맥관(C-line) 전용 라인을 연결하는 지점입니다.
  /// 일반 수액 라인과 물리적으로 연결되지 않도록 별도 유형으로 분리합니다.
  /// </summary>
  [RequireComponent(typeof(SphereCollider))]
  public sealed class CentralLineConnectionPoint : LineConnectionPoint
  {
    public const float LineWidth = 0.015f;
    public const float Elasticity = 0.21f;
    public const string MaterialResourcePath = "Materials/LineConnectionService/CentralLine";

    public static Material DefaultMaterial => Resources.Load<Material>(MaterialResourcePath);

    public override void ApplyLineMaterial(LineRenderer lineRenderer)
    {
      base.ApplyLineMaterial(lineRenderer);
    }
  }
}
