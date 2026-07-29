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
    public override void ApplyLineMaterial(LineRenderer lineRenderer)
    {
      base.ApplyLineMaterial(lineRenderer);
    }
  }
}
