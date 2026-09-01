using TriageTrainer.Entity.LineConnection;
using UnityEngine;

namespace TriageTrainer.Entity.ElectricalLine
{
  /// <summary>
  /// 전기 라인을 연결할 수 있는 위치를 나타낸다.
  /// </summary>
  public sealed class ElectricalLineConnectionPoint : LineConnectionPoint
  {
    public const float LineWidth = 0.025f;
    public const float Elasticity = 0.05f;
    public const string MaterialResourcePath = "Materials/LineConnectionService/ElectricalLine";

    public static Material DefaultMaterial => Resources.Load<Material>(MaterialResourcePath);

    public override void ApplyLineMaterial(LineRenderer lineRenderer)
    {
      base.ApplyLineMaterial(lineRenderer);
    }
  }
}
