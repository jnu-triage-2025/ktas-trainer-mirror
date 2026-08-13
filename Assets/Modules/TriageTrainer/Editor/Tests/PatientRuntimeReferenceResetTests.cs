using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TriageTrainer.Entity;
using TriageTrainer.Entity.OxyLine;
using TriageTrainer.Patient;
using UnityEngine;
using UnityEngine.TestTools;

namespace TriageTrainer.Tests
{
  public sealed class PatientRuntimeReferenceResetTests
  {
    [TestCase(typeof(PatientTypeBMaleState))]
    [TestCase(typeof(PatientTypeBFemaleState))]
    public void InspectorResetRebuildsSerializedBPatientOxygenReference(System.Type stateType)
    {
      var patientObject = new GameObject("patient-b-runtime-reference-test");
      var interfaceObject = new GameObject("nasal-cannula-oxy-point");

      try
      {
        interfaceObject.transform.SetParent(patientObject.transform, false);
        var port = interfaceObject.AddComponent<OxyLineConnectionPoint>();
        patientObject.AddComponent(stateType);
        var controller = patientObject.AddComponent<PatientController>();

        var serializedField = typeof(PatientController).GetField(
          "_oxygenMaskAttachmentPoint", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(serializedField, Is.Not.Null);
        serializedField.SetValue(controller, null);

        InvokeReset(controller);

        Assert.That(controller.ConfiguredOxygenMaskAttachmentPoint, Is.SameAs(port));
        Assert.That(serializedField.GetValue(controller), Is.SameAs(port),
          "Reset must restore the serialized reference from the oxygen connection-point component");
      }
      finally
      {
        Object.DestroyImmediate(patientObject);
      }
    }

    private static void InvokeReset(PatientController controller)
    {
      var reset = typeof(PatientController).GetMethod("Reset", BindingFlags.Instance | BindingFlags.NonPublic);
      Assert.That(reset, Is.Not.Null);
      reset.Invoke(controller, null);
    }

    [Test]
    public void ResetRejectsAmbiguousOxygenConnectionPoints()
    {
      var patientObject = new GameObject("patient-b-ambiguous-oxygen-reference-test");

      try
      {
        patientObject.AddComponent<PatientTypeBMaleState>();
        patientObject.AddComponent<OxyLineConnectionPoint>();
        var secondPortObject = new GameObject("second-oxygen-port");
        secondPortObject.transform.SetParent(patientObject.transform, false);
        secondPortObject.AddComponent<OxyLineConnectionPoint>();
        LogAssert.Expect(LogType.Error, new Regex("PatientTypeBMaleState.*OxyLineConnectionPoint.*2"));
        var controller = patientObject.AddComponent<PatientController>();

        LogAssert.Expect(LogType.Error, new Regex("PatientTypeBMaleState.*OxyLineConnectionPoint.*2"));
        InvokeReset(controller);

        Assert.That(controller.ConfiguredOxygenMaskAttachmentPoint, Is.Null);
      }
      finally
      {
        Object.DestroyImmediate(patientObject);
      }
    }
  }
}
