using System;
using UnityEngine;

namespace TriageTrainer.Entity.Patient
{
  [Serializable]
  public struct NumericsParameters
  {
    [Range(0f, 300f)] public float bpm;
    [Range(0f, 100f)] public float pvcs;
    [Range(0f, 300f)] public float pulseRate;
    [Range(0f, 20f)] public float perfusionIndex;
    [Range(0f, 100f)] public float spo2;

    public static NumericsParameters Default => new NumericsParameters
    {
      bpm = 60f,
      pvcs = 0f,
      pulseRate = 74f,
      perfusionIndex = 3.0f,
      spo2 = 99f
    };
  }
}
