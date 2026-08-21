using TriageTrainer.Entity.LineConnection;
using UnityEngine;

namespace TriageTrainer.Entity.OxyLine
{
  /// <summary>
  /// Marks the position where an oxygen line can be connected.
  /// Connection behavior will be implemented separately.
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
  }
}
