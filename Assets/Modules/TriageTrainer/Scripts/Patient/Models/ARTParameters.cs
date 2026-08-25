using System;
using UnityEngine;

namespace TriageTrainer.Entity.Patient
{
  [Serializable]
  public struct ARTParameters
  {
    [Range(0f, 250f)] public float bpm;
    [Range(40f, 260f)] public float systolic;
    [Range(20f, 180f)] public float diastolic;
    [Range(0f, 1f)] public float noise;

    public static ARTParameters Default => new ARTParameters
    {
      bpm = 75f,
      systolic = 120f,
      diastolic = 80f,
      noise = 0.2f
    };
  }
}
