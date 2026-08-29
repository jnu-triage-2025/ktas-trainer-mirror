using System.Reflection;
using NUnit.Framework;
using TriageTrainer.Entity;
using UnityEngine;

namespace TriageTrainer.Tests
{
  public sealed class PatientAEpinephrineSyringeTests
  {
    [TestCase("epinephrine_5cc_syringe")]
    [TestCase("epinephrine_16g_5cc_syringe")]
    [TestCase("epinephrine_24g_20cc_syringe")]
    [TestCase("epinephrine_18g_50cc_syringe")]
    public void EpinephrineSyringeFamilyCanBeAppliedToPatientA(string identifier)
    {
      var patientObject = new GameObject("patient-a-epinephrine-test");
      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        typeof(PatientController).GetField("_identifier", BindingFlags.Instance | BindingFlags.NonPublic)
          ?.SetValue(patient, "patient_a");
        var canApply = typeof(PatientController).GetMethod(
          "CanApplyItemUse", BindingFlags.Instance | BindingFlags.NonPublic);
        var apply = typeof(PatientController).GetMethod(
          "ApplyItemUse", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(canApply, Is.Not.Null);
        Assert.That(apply, Is.Not.Null);
        Assert.That((bool)canApply.Invoke(patient, new object[] { identifier }), Is.True);
        Assert.That((bool)apply.Invoke(patient, new object[] { identifier }), Is.True);
      }
      finally
      {
        Object.DestroyImmediate(patientObject);
      }
    }

    [TestCase("epinephrine_ampule")]
    [TestCase("norepinephrine_20g_5cc_syringe")]
    [TestCase("normal_saline_5cc_syringe")]
    [TestCase("epinephrine_5cc")]
    public void NonEpinephrineSyringeItemsDoNotMatchFamily(string identifier)
    {
      var matches = typeof(PatientController).GetMethod(
        "IsEpinephrineSyringeIdentifier", BindingFlags.Static | BindingFlags.NonPublic);
      Assert.That(matches, Is.Not.Null);
      Assert.That((bool)matches.Invoke(null, new object[] { identifier }), Is.False);
    }
  }
}
