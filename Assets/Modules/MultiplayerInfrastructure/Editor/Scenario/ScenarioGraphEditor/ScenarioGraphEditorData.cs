using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerInfrastructure.Editor
{
  public sealed class ScenarioGraphEditorData
  {
    public Dictionary<string, SerializableVector2> NodePositions { get; set; } = new Dictionary<string, SerializableVector2>();
  }

  // Lightweight serializable vector to avoid UnityEngine.Vector2 cycles in System.Text.Json
  public struct SerializableVector2
  {
    public float x;
    public float y;

    public SerializableVector2(float x, float y)
    {
      this.x = x;
      this.y = y;
    }

    public SerializableVector2(Vector2 v)
    {
      x = v.x;
      y = v.y;
    }

    public Vector2 ToVector2() => new Vector2(x, y);
  }
}
