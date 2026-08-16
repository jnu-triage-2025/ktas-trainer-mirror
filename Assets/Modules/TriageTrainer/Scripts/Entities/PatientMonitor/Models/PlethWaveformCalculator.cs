using UnityEngine;
using TriageTrainer.Entity.Patient;

namespace TriageTrainer.Entity.PatientMonitor.Models
{
  public static class PlethWaveformCalculator
  {
    public static float Calculate(in PlethParameters parameters, float cycleNorm)
    {
      if (parameters.bpm <= 0f)
      {
        return 0f;
      }

      float alpha = Mathf.Max(0f, (parameters.spo2 - 80f) / 20f);
      float ampScale = Mathf.Pow(alpha, 1.5f);

      return Gaussian(cycleNorm, 0.25f, 0.85f * ampScale, 0.09f)
             + Gaussian(cycleNorm, 0.46f, 0.22f * ampScale, 0.045f)
             + (Random.value - 0.5f) * 2f * parameters.noise * 0.012f;
    }

    private static float Gaussian(float x, float mu, float amp, float sigma)
    {
      if (sigma <= 0f)
        return 0f;

      return amp * Mathf.Exp(-Mathf.Pow(x - mu, 2f) / (2f * sigma * sigma));
    }
  }
}
