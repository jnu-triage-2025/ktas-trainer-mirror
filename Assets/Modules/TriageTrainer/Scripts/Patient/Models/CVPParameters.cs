using System;
using UnityEngine;

namespace TriageTrainer.Entity.Patient
{
  [Serializable]
  public struct CVPParameters
  {
    [Range(0f, 250f)] public float bpm;
    [Range(0f, 30f)] public float mean;
    [Range(0f, 1f)] public float noise;

    public static CVPParameters Default => new CVPParameters
    {
      bpm = 75f,
      mean = 12f,
      noise = 0.2f
    };
  }
}
