using System;

namespace TriageTrainer.Entity.Patient
{
  [Serializable]
  public struct PatientMonitorParameters
  {
    public ECGParameters ecg;
    public ARTParameters art;
    public CVPParameters cvp;
    public PlethParameters pleth;
    public NumericsParameters numerics;
    public NIBPParameters nibp;
    public TemperatureParameters temperature;
    public STLeadValues stLeads;

    public static PatientMonitorParameters Default => new PatientMonitorParameters
    {
      ecg = ECGParameters.Normal,
      art = ARTParameters.Default,
      cvp = CVPParameters.Default,
      pleth = PlethParameters.Default,
      numerics = NumericsParameters.Default,
      nibp = NIBPParameters.Default,
      temperature = TemperatureParameters.Default,
      stLeads = STLeadValues.Default
    };
  }
}
