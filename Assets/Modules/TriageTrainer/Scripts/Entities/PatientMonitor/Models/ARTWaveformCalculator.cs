using UnityEngine;
using TriageTrainer.Entity.Patient;

namespace TriageTrainer.Entity.PatientMonitor.Models
{
  public static class ARTWaveformCalculator
  {
    public static float Calculate(in ARTParameters parameters, float cycleNorm)
    {
      if (parameters.bpm <= 0f)
      {
        return 0f;
      }

      float pulsePressure = Mathf.Max(0f, parameters.systolic - parameters.diastolic);
      float decay = cycleNorm > 0.25f
        ? -pulsePressure * 0.30f * (1f - Mathf.Exp(-5f * (cycleNorm - 0.25f)))
        : 0f;

      return parameters.diastolic
             + Gaussian(cycleNorm, 0.20f, pulsePressure, 0.065f)
             - Gaussian(cycleNorm, 0.38f, pulsePressure * 0.08f, 0.025f)
             + Gaussian(cycleNorm, 0.44f, pulsePressure * 0.12f, 0.025f)
             + decay
             + (Random.value - 0.5f) * 2f * parameters.noise * 1.2f;
    }

    private static float Gaussian(float x, float mu, float amp, float sigma)
    {
      if (sigma <= 0f)
        return 0f;

      return amp * Mathf.Exp(-Mathf.Pow(x - mu, 2f) / (2f * sigma * sigma));
    }
  }
}
