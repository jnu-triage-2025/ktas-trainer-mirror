using NUnit.Framework;
using TriageTrainer.Entity;
using TriageTrainer.Entity.IntravenousLine;
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

    [Test]
    public void PatientIvAttachmentPointAllowsMultipleLines()
    {
      var patientObject = new GameObject("patient-iv-attachment-test");
      var firstLine = new GameObject("first-iv-line");
      var secondLine = new GameObject("second-iv-line");

      try
      {
        var patient = patientObject.AddComponent<PatientController>();

        Assert.That(patient.IvAttachmentPoint, Is.Not.Null);
        Assert.That(patient.IvAttachmentPoint.CanAcceptAdditionalConnection, Is.True);

        patient.IvAttachmentPoint.RegisterConnectedLineObject(firstLine);
        patient.IvAttachmentPoint.RegisterConnectedLineObject(secondLine);

        Assert.That(patient.IvAttachmentPoint.CanAcceptAdditionalConnection, Is.True);
      }
      finally
      {
        Object.DestroyImmediate(secondLine);
        Object.DestroyImmediate(firstLine);
        Object.DestroyImmediate(patientObject);
      }
    }

    [Test]
    public void CarryingPatientDisablesTriageAndAssessmentGate()
    {
      var patientObject = new GameObject("patient-carry-assessment-gate-test");

      try
      {
        var patient = patientObject.AddComponent<PatientController>();

        Assert.That(patient.CanPerformTriageOrAssessment, Is.True);

        patient.OnPlayerAttachedEnter();
        Assert.That(patient.CanPerformTriageOrAssessment, Is.False);

        patient.OnPlayerAttachedExit();
        Assert.That(patient.CanPerformTriageOrAssessment, Is.True);
      }
      finally
      {
        Object.DestroyImmediate(patientObject);
      }
    }
  }
}
