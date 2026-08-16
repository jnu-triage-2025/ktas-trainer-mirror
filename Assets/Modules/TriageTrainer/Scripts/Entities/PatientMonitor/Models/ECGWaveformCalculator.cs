using UnityEngine;
using TriageTrainer.Entity.Patient;

namespace TriageTrainer.Entity.PatientMonitor.Models
{
  public struct ECGRuntimeState
  {
    public bool hasPendingPvcBeat;
    public bool isPvcBeat;
    public float fibPhase;
  }

  public static class ECGWaveformCalculator
  {
    public static float Calculate(
      in ECGParameters parameters,
      ECGRhythmType rhythmType,
      float cycleNorm,
      float dt,
      ref ECGRuntimeState runtimeState)
    {
      // BPM 0은 리듬 종류와 무관하게 완전한 기준선이다. 특히 VF 분기는 자체 진폭을
      // 생성하므로 이 검사를 먼저 수행해야 환자 미연결 0 표시가 직선으로 유지된다.
      if (parameters.bpm <= 0f)
      {
        return 0f;
      }

      if (rhythmType == ECGRhythmType.VentricularFibrillation)
      {
        runtimeState.fibPhase += dt * (18f + Random.Range(-6f, 6f));
        return Mathf.Sin(runtimeState.fibPhase) * (0.2f + Random.value * 0.35f)
               + (Random.value - 0.5f) * parameters.noise;
      }

      if (rhythmType == ECGRhythmType.Asystole)
      {
        return (Random.value - 0.5f) * parameters.noise;
      }

      float qrsMult = parameters.qrsWidthScale > 0f ? parameters.qrsWidthScale : 1f;
      float pAmp = runtimeState.isPvcBeat ? 0f : parameters.pAmp;
      float qAmp = parameters.qAmp;
      float rAmp = parameters.rAmp;
      float sAmp = parameters.sAmp;
      float tAmp = parameters.tAmp;
      float tWidth = parameters.tWidth;

      if (runtimeState.isPvcBeat)
      {
        qAmp *= 0.5f;
        rAmp *= 1.35f;
        sAmp *= 1.7f;
        qrsMult *= 1.7f;
        tAmp *= -0.5f;
        tWidth *= 0.9f;
      }

      float value = 0f;
      value += Gaussian(cycleNorm, 0.10f, pAmp, parameters.pWidth);
      value += Gaussian(cycleNorm, 0.18f, qAmp, 0.02f * qrsMult);
      value += Gaussian(cycleNorm, 0.20f, rAmp, 0.03f * qrsMult);
      value += Gaussian(cycleNorm, 0.22f, sAmp, 0.03f * qrsMult);

      if (parameters.stElevation != 0f && cycleNorm > 0.25f && cycleNorm < 0.40f)
      {
        float stShape = Mathf.Exp(-Mathf.Pow(cycleNorm - 0.30f, 2f) / (2f * 0.1f * 0.1f));
        value += parameters.stElevation * stShape;
      }

      value += Gaussian(cycleNorm, 0.45f, tAmp, tWidth);

      if (parameters.uAmp != 0f)
      {
        value += Gaussian(cycleNorm, 0.65f, parameters.uAmp, 0.06f);
      }

      value += (Random.value - 0.5f) * parameters.noise;
      return value;
    }

    public static void OnBeat(ECGRhythmType rhythmType, ref ECGRuntimeState runtimeState)
    {
      if (rhythmType != ECGRhythmType.PrematureVentricularContraction)
      {
        runtimeState.isPvcBeat = false;
        runtimeState.hasPendingPvcBeat = false;
        return;
      }

      if (runtimeState.hasPendingPvcBeat)
      {
        runtimeState.isPvcBeat = true;
        runtimeState.hasPendingPvcBeat = false;
      }
      else
      {
        runtimeState.isPvcBeat = false;
        if (Random.value < 0.22f)
        {
          runtimeState.hasPendingPvcBeat = true;
        }
      }
    }

    public static bool ConsumePendingPrematureBeat(ref ECGRuntimeState runtimeState)
    {
      if (!runtimeState.hasPendingPvcBeat)
        return false;

      runtimeState.hasPendingPvcBeat = false;
      return true;
    }

    private static float Gaussian(float x, float mu, float amp, float sigma)
    {
      if (sigma <= 0f)
        return 0f;

      return amp * Mathf.Exp(-Mathf.Pow(x - mu, 2f) / (2f * sigma * sigma));
    }
  }
}
