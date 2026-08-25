using System;
using UnityEngine;

namespace TriageTrainer.Entity.Patient
{
  [Serializable]
  public struct NIBPParameters
  {
    [Range(40f, 260f)] public float systolic;
    [Range(20f, 180f)] public float diastolic;

    public static NIBPParameters Default => new NIBPParameters
    {
      systolic = 120f,
      diastolic = 82f
    };
  }
}
