using System;
using UnityEngine;

namespace TriageTrainer.Entity.Patient
{
  [Serializable]
  public struct TemperatureParameters
  {
    [Range(20f, 45f)] public float t1;
    [Range(20f, 45f)] public float t2;

    public static TemperatureParameters Default => new TemperatureParameters
    {
      t1 = 36.5f,
      t2 = 32.3f
    };
  }
}
