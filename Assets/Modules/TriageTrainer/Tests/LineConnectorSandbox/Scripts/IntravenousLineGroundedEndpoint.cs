using System;

using UnityEngine;

namespace TriageTrainer.Tests.LineConnectorSandbox
{
  [Serializable]
  public struct IntravenousLineGroundedEndpoint
  {
    // StartPoint is primarily intended to represent the IV bag side.
    public GameObject StartPoint;
    public GameObject EndPoint;

    public IntravenousLineGroundedEndpoint(GameObject startPoint, GameObject endPoint)
    {
      StartPoint = startPoint;
      EndPoint = endPoint;
    }

    public bool IsValid => StartPoint != null && EndPoint != null;

    public bool TryGetPositions(out Vector3 startPosition, out Vector3 endPosition)
    {
      if (!IsValid)
      {
        startPosition = default;
        endPosition = default;
        return false;
      }

      startPosition = StartPoint.transform.position;
      endPosition = EndPoint.transform.position;
      return true;
    }
  }
}
