using NUnit.Framework;
using TriageTrainer.Entity;
using UnityEngine;

namespace TriageTrainer.Tests
{
  public sealed class PatientEquipmentConnectionTests
  {
    [Test]
    public void ClearIVFluidConnectionIgnoresStaleEquipmentSource()
    {
      var patientObject = new GameObject("patient-equipment-test");
      var firstSourceObject = new GameObject("first-source");
      var replacementSourceObject = new GameObject("replacement-source");

      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        var firstSource = firstSourceObject.AddComponent<PatientController>();
        var replacementSource = replacementSourceObject.AddComponent<PatientController>();

        patient.SetIVFluidConnection(isLeftArm: true, firstSource);
        patient.SetIVFluidConnection(isLeftArm: true, replacementSource);
        patient.ClearIVFluidConnection(isLeftArm: true, expectedSource: firstSource);

        Assert.That(patient.IVFluidLeftArm, Is.SameAs(replacementSource));

        patient.ClearIVFluidConnection(isLeftArm: true, expectedSource: replacementSource);
        Assert.That(patient.IVFluidLeftArm, Is.Null);
      }
      finally
      {
        Object.DestroyImmediate(replacementSourceObject);
        Object.DestroyImmediate(firstSourceObject);
        Object.DestroyImmediate(patientObject);
      }
    }
  }
}
