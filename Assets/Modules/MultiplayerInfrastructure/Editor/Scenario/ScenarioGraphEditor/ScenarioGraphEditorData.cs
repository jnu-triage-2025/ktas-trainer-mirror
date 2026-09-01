using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerInfrastructure.Editor
{
  public sealed class ScenarioGraphEditorData
  {
    public Dictionary<string, SerializableVector2> NodePositions { get; set; } = new Dictionary<string, SerializableVector2>();
    public Dictionary<string, List<SerializableVector2>> EdgeRoutes { get; set; } = new Dictionary<string, List<SerializableVector2>>();
  }

  // System.Text.Json 에서 UnityEngine.Vector2 순환 참조를 피하기 위한 가벼운 직렬화 벡터
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
