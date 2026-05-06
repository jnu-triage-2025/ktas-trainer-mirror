using UnityEngine;
using TriageTrainer.Entity.Patient;

namespace TriageTrainer.Entity.PatientMonitor.Models
{
  public static class CVPWaveformCalculator
  {
    public static float Calculate(in CVPParameters parameters, float cycleNorm)
    {
      float amplitude = Mathf.Max(0.5f, parameters.mean * 0.22f);
      float dcOffset = amplitude * 0.31f;

      return parameters.mean
             - dcOffset
             + Gaussian(cycleNorm, 0.12f, amplitude * 1.10f, 0.05f)
             + Gaussian(cycleNorm, 0.27f, amplitude * 0.55f, 0.03f)
             + Gaussian(cycleNorm, 0.65f, amplitude * 0.90f, 0.07f)
             + (Random.value - 0.5f) * 2f * parameters.noise * 0.07f;
    }

    private static float Gaussian(float x, float mu, float amp, float sigma)
    {
      if (sigma <= 0f)
        return 0f;

      return amp * Mathf.Exp(-Mathf.Pow(x - mu, 2f) / (2f * sigma * sigma));
    }
  }
}
