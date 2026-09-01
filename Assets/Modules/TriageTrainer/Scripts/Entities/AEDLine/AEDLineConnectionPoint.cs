using TriageTrainer.Entity.LineConnection;
using UnityEngine;

namespace TriageTrainer.Entity.AEDLine
{
  /// <summary>
  /// AED 라인을 연결할 수 있는 위치를 나타낸다.
  /// 이 컴포넌트는 네트워크를 인식하며, 연결 동작은 별도로 구현된다.
  /// </summary>
  public sealed class AEDLineConnectionPoint : LineConnectionPoint
  {
    public const float LineWidth = 0.03f;
    public const float Elasticity = 0.3f;
    public const string MaterialResourcePath = "Materials/LineConnectionService/AEDLine";

    public static Material DefaultMaterial => Resources.Load<Material>(MaterialResourcePath);

    public override void ApplyLineMaterial(LineRenderer lineRenderer)
    {
      base.ApplyLineMaterial(lineRenderer);
    }
  }
}
