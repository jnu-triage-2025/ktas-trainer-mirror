using System;
using UnityEngine;

namespace TriageTrainer.Entity.PatientMonitor
{
  [Serializable]
  public struct ECGParameters
  {
    [Range(0, 250)] public float bpm;
    [Range(0, 1)] public float irregularity; // 부정맥 정도
    public float pAmp, pWidth;
    public float qAmp, rAmp, sAmp;
    public float tAmp, tWidth;
    public float uAmp;
    public float stElevation;
    public float noise;
    public float qrsWidthScale;

    // 기본값 (Normal Sinus Rhythm)
    public static ECGParameters Normal => new ECGParameters
    {
      bpm = 75,
      pAmp = 0.15f,
      pWidth = 0.04f,
      qAmp = -0.15f,
      rAmp = 1.2f,
      sAmp = -0.25f,
      tAmp = 0.3f,
      tWidth = 0.08f,
      uAmp = 0f,
      stElevation = 0f,
      noise = 0.02f,
      irregularity = 0f,
      qrsWidthScale = 1.0f
    };
  }
}
