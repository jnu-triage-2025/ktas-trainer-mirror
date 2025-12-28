using System;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario
{
  [Serializable]
  public class Waypoint
  {
    public string Identifier;
    public Vector3 Position;

    public Waypoint(string identifier, Vector3 position)
    {
      Identifier = identifier;
      Position = position;
    }
  }
}