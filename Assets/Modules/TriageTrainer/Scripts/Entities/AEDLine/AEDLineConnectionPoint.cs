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
    public override void ApplyLineMaterial(LineRenderer lineRenderer)
    {
      base.ApplyLineMaterial(lineRenderer);
    }
  }
}
