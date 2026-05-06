using System;
using UnityEngine;

namespace TriageTrainer.Entity.Patient
{
  public enum ECGRhythmType
  {
    NormalSinus,
    SinusTachycardia,
    SinusBradycardia,
    AtrialFibrillation,
    PrematureVentricularContraction,
    VentricularTachycardia,
    VentricularFibrillation,
    Asystole,
    HyperkalemiaLike,
    HypokalemiaLike
  }

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

    public static ECGParameters FromRhythm(ECGRhythmType rhythm)
    {
      switch (rhythm)
      {
        case ECGRhythmType.SinusTachycardia:
          return new ECGParameters
          {
            bpm = 130f,
            pAmp = 0.14f,
            pWidth = 0.035f,
            qAmp = -0.14f,
            rAmp = 1.1f,
            sAmp = -0.24f,
            tAmp = 0.28f,
            tWidth = 0.07f,
            uAmp = 0f,
            stElevation = 0f,
            noise = 0.02f,
            irregularity = 0.02f,
            qrsWidthScale = 1.0f
          };
        case ECGRhythmType.SinusBradycardia:
          return new ECGParameters
          {
            bpm = 42f,
            pAmp = 0.16f,
            pWidth = 0.045f,
            qAmp = -0.14f,
            rAmp = 1.2f,
            sAmp = -0.24f,
            tAmp = 0.3f,
            tWidth = 0.09f,
            uAmp = 0f,
            stElevation = 0f,
            noise = 0.015f,
            irregularity = 0.01f,
            qrsWidthScale = 1.0f
          };
        case ECGRhythmType.AtrialFibrillation:
          return new ECGParameters
          {
            bpm = 120f,
            pAmp = 0f,
            pWidth = 0.04f,
            qAmp = -0.12f,
            rAmp = 0.95f,
            sAmp = -0.22f,
            tAmp = 0.22f,
            tWidth = 0.07f,
            uAmp = 0f,
            stElevation = 0f,
            noise = 0.06f,
            irregularity = 0.5f,
            qrsWidthScale = 1.0f
          };
        case ECGRhythmType.PrematureVentricularContraction:
          return new ECGParameters
          {
            bpm = 90f,
            pAmp = 0.14f,
            pWidth = 0.04f,
            qAmp = -0.14f,
            rAmp = 1.15f,
            sAmp = -0.26f,
            tAmp = 0.3f,
            tWidth = 0.08f,
            uAmp = 0f,
            stElevation = 0f,
            noise = 0.03f,
            irregularity = 0.2f,
            qrsWidthScale = 1.15f
          };
        case ECGRhythmType.VentricularTachycardia:
          return new ECGParameters
          {
            bpm = 180f,
            pAmp = 0f,
            pWidth = 0.02f,
            qAmp = -0.2f,
            rAmp = 1.35f,
            sAmp = -0.45f,
            tAmp = 0.08f,
            tWidth = 0.05f,
            uAmp = 0f,
            stElevation = 0f,
            noise = 0.04f,
            irregularity = 0.08f,
            qrsWidthScale = 1.6f
          };
        case ECGRhythmType.VentricularFibrillation:
          return new ECGParameters
          {
            bpm = 220f,
            pAmp = 0f,
            pWidth = 0.02f,
            qAmp = -0.2f,
            rAmp = 0.5f,
            sAmp = -0.4f,
            tAmp = 0f,
            tWidth = 0.03f,
            uAmp = 0f,
            stElevation = 0f,
            noise = 0.35f,
            irregularity = 1.0f,
            qrsWidthScale = 2.0f
          };
        case ECGRhythmType.Asystole:
          return new ECGParameters
          {
            bpm = 0f,
            pAmp = 0f,
            pWidth = 0.04f,
            qAmp = 0f,
            rAmp = 0f,
            sAmp = 0f,
            tAmp = 0f,
            tWidth = 0.08f,
            uAmp = 0f,
            stElevation = 0f,
            noise = 0.01f,
            irregularity = 0f,
            qrsWidthScale = 1.0f
          };
        case ECGRhythmType.HyperkalemiaLike:
          return new ECGParameters
          {
            bpm = 78f,
            pAmp = 0.05f,
            pWidth = 0.035f,
            qAmp = -0.1f,
            rAmp = 0.9f,
            sAmp = -0.2f,
            tAmp = 0.65f,
            tWidth = 0.06f,
            uAmp = 0f,
            stElevation = 0f,
            noise = 0.02f,
            irregularity = 0.05f,
            qrsWidthScale = 1.2f
          };
        case ECGRhythmType.HypokalemiaLike:
          return new ECGParameters
          {
            bpm = 78f,
            pAmp = 0.14f,
            pWidth = 0.04f,
            qAmp = -0.14f,
            rAmp = 1.0f,
            sAmp = -0.22f,
            tAmp = 0.18f,
            tWidth = 0.08f,
            uAmp = 0.18f,
            stElevation = 0f,
            noise = 0.02f,
            irregularity = 0.03f,
            qrsWidthScale = 1.0f
          };
        default:
          return Normal;
      }
    }
  }
}
