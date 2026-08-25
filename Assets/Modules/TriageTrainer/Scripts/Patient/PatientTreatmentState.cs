using System;
using System.Collections.Generic;
using UnityEngine;

namespace TriageTrainer.Patient
{
  /// <summary>시각 표현과 독립적으로 환자에게 완료된 처치를 보관하는 직렬화 데이터.</summary>
  [Serializable]
  public sealed class PatientTreatmentState
  {
    [SerializeField] private List<string> appliedTreatments = new();

    public IReadOnlyList<string> AppliedTreatments => appliedTreatments;

    public bool IsApplied(string treatmentIdentifier)
      => !string.IsNullOrWhiteSpace(treatmentIdentifier)
         && appliedTreatments.Contains(treatmentIdentifier);

    public bool SetApplied(string treatmentIdentifier, bool applied)
    {
      if (string.IsNullOrWhiteSpace(treatmentIdentifier))
        return false;

      treatmentIdentifier = treatmentIdentifier.Trim();
      bool previous = appliedTreatments.Contains(treatmentIdentifier);
      if (previous == applied)
        return false;

      if (applied)
        appliedTreatments.Add(treatmentIdentifier);
      else
        appliedTreatments.Remove(treatmentIdentifier);
      return true;
    }

    public string[] CreateSnapshot() => appliedTreatments.ToArray();

    public void ApplySnapshot(IEnumerable<string> treatmentIdentifiers)
    {
      appliedTreatments.Clear();
      if (treatmentIdentifiers == null)
        return;

      foreach (string identifier in treatmentIdentifiers)
      {
        if (!string.IsNullOrWhiteSpace(identifier) && !appliedTreatments.Contains(identifier.Trim()))
          appliedTreatments.Add(identifier.Trim());
      }
    }
  }
}
