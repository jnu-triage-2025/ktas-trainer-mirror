using System;

using UnityEngine;

namespace TriageTrainer.Tests.LineConnectorSandbox
{
  [Obsolete("Use IntravenousLineGrounded instead. This class remains only for scene compatibility.")]
  [AddComponentMenu("")]
  public class TwoPointLineConnector : IntravenousLineGrounded
  {
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;

    private void OnEnable()
    {
      BindLegacyEndpointsIfPresent();
    }

    private void OnValidate()
    {
      BindLegacyEndpointsIfPresent();
    }

    private void BindLegacyEndpointsIfPresent()
    {
      if (pointA == null || pointB == null)
      {
        return;
      }

      SetEndpoints(pointA, pointB);
    }
  }
}
