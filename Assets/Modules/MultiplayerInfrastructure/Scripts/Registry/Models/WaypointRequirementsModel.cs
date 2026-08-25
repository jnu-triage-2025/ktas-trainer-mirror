using System;
using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  [Serializable]
  public struct WaypointRequirements
  {
    public string Identifier;
    public Vector3 Position;
  }
}
