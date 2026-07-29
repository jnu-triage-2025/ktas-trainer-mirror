using FishNet.Object;
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
    public override void ApplyLineMaterial(LineRenderer lineRenderer)
    {
      base.ApplyLineMaterial(lineRenderer);
    }
  }
}
