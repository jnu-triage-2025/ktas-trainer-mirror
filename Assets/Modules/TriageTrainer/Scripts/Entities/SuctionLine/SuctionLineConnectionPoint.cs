using FishNet.Object;
using TriageTrainer.Entity.LineConnection;
using UnityEngine;

namespace TriageTrainer.Entity.SuctionLine
{
  /// <summary>
  /// Marks the position where a suction line can be connected.
  /// Connection behavior will be implemented separately.
  /// </summary>
  public sealed class SuctionLineConnectionPoint : LineConnectionPoint
  {
    public const float LineWidth = 0.015f;
    public const float Elasticity = 0.8f;
    public const string MaterialResourcePath = "Materials/LineConnectionService/SuctionLine";

    public static Material DefaultMaterial => Resources.Load<Material>(MaterialResourcePath);

    public override void ApplyLineMaterial(LineRenderer lineRenderer)
    {
      base.ApplyLineMaterial(lineRenderer);
    }
  }
}
