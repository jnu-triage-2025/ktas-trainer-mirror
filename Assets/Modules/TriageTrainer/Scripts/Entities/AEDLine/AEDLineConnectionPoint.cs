using FishNet.Object;
using TriageTrainer.Entity.LineConnection;
using UnityEngine;

namespace TriageTrainer.Entity.AEDLine
{
  /// <summary>
  /// Marks the position where an AED line can be connected.
  /// This component is network-aware; connection behavior will be implemented separately.
  /// </summary>
  public sealed class AEDLineConnectionPoint : LineConnectionPoint
  {
    public const float LineWidth = 0.015f;
    public const float Elasticity = 0.75f;
    public const string MaterialResourcePath = "Materials/LineConnectionService/AEDLine";

    public static Material DefaultMaterial => Resources.Load<Material>(MaterialResourcePath);

    public override void ApplyLineMaterial(LineRenderer lineRenderer)
    {
      base.ApplyLineMaterial(lineRenderer);
    }
  }
}
