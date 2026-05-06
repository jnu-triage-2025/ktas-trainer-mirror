using System;
using System.Collections.Generic;
using UnityEngine;

namespace TriageTrainer.Entity.Patient
{
  [Serializable]
  public class PatientMedicalState
  {
    [Header("Medical State")]
    public BloodPressure bloodPressure;
    public BloodPulse pulse;
    public Skin skin = Skin.Default;
    public BodyTemperature bodyTemperature;
    public List<HealthProblem> healthProblem = new();
    public Consciousness consciousness = Consciousness.Default;
    public Respiration respiration;
    public List<RequiredDrug> requiredDrugs = new();
    public bool isCardiacArrest;

    [Header("Medical State/Monitor")]
    public ECGParameters ecg = ECGParameters.Normal;
    public ARTParameters art = ARTParameters.Default;
    public CVPParameters cvp = CVPParameters.Default;
    public PlethParameters pleth = PlethParameters.Default;
    public NumericsParameters numerics = NumericsParameters.Default;
    public NIBPParameters nibp = NIBPParameters.Default;
    public TemperatureParameters temperature = TemperatureParameters.Default;
    public STLeadValues stLeads = STLeadValues.Default;

    public void CopyFrom(PatientMedicalState source)
    {
      if (source == null)
        return;

      bloodPressure = source.bloodPressure;
      pulse = source.pulse;
      skin = source.skin;
      bodyTemperature = source.bodyTemperature;
      healthProblem = source.healthProblem != null ? new List<HealthProblem>(source.healthProblem) : new List<HealthProblem>();
      consciousness = source.consciousness;
      respiration = source.respiration;
      requiredDrugs = source.requiredDrugs != null ? new List<RequiredDrug>(source.requiredDrugs) : new List<RequiredDrug>();
      isCardiacArrest = source.isCardiacArrest;

      ecg = source.ecg;
      art = source.art;
      cvp = source.cvp;
      pleth = source.pleth;
      numerics = source.numerics;
      nibp = source.nibp;
      temperature = source.temperature;
      stLeads = source.stLeads;
    }

    public PatientMedicalState Clone()
    {
      var clone = new PatientMedicalState();
      clone.CopyFrom(this);
      return clone;
    }
  }
}
