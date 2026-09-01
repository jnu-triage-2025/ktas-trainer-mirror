using TriageTrainer.Entity.LineConnection;
using UnityEngine;

namespace TriageTrainer.Entity.SuctionLine
{
  /// <summary>
  /// 흡인 라인을 연결할 수 있는 위치를 나타낸다.
  /// 연결 동작은 별도로 구현된다.
  /// </summary>
  public sealed class SuctionLineConnectionPoint : LineConnectionPoint
  {
    public const float LineWidth = 0.04f;
    public const float Elasticity = 0.13f;
    public const string MaterialResourcePath = "Materials/LineConnectionService/SuctionLine";

    public static Material DefaultMaterial => Resources.Load<Material>(MaterialResourcePath);

    public override void ApplyLineMaterial(LineRenderer lineRenderer)
    {
      base.ApplyLineMaterial(lineRenderer);
    }
  }
}
