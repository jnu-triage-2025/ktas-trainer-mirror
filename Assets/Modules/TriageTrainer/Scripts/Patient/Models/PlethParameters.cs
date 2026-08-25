using System;
using UnityEngine;

namespace TriageTrainer.Entity.Patient
{
  [Serializable]
  public struct PlethParameters
  {
    [Range(0f, 250f)] public float bpm;
    [Range(70f, 100f)] public float spo2;
    [Range(0f, 1f)] public float noise;

    public static PlethParameters Default => new PlethParameters
    {
      bpm = 75f,
      spo2 = 99f,
      noise = 0.2f
    };
  }
}
