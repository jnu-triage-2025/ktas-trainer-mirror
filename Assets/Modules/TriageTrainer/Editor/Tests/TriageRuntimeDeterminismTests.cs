using System;
using System.Reflection;
using NUnit.Framework;
using TriageTrainer.Entity;
using TriageTrainer.Entity.LineConnection;
using TriageTrainer.Entity.ElectricalLine;
using TriageTrainer.Entity.Patient;
using TriageTrainer.Scenario;
using TriageTrainer.Scenario.Rubric;
using UnityEngine;
using UnityEngine.TestTools;

namespace TriageTrainer.Tests
{
  public sealed class TriageRuntimeDeterminismTests
  {
    [Test]
    public void GenericPatientStateMutationRpcsAreNotExposed()
    {
      Assert.That(FindPatientMethod("CmdSetTreatmentDisplay"), Is.Null);
      Assert.That(FindPatientMethod("CmdSyncAllDisplayStates"), Is.Null);
      Assert.That(FindPatientMethod("CmdSetMonitorMedicalState"), Is.Null);
    }

    [Test]
    public void MonitorMedicalStateRejectsNonFiniteNumbers()
    {
      MethodInfo validator = typeof(PatientController).GetMethod(
        "IsMonitorMedicalStateFinite", BindingFlags.Static | BindingFlags.NonPublic);
      Assert.That(validator, Is.Not.Null);
      Assert.That(validator.Invoke(null, new object[] { new object[] { ECGParameters.Normal } }), Is.True);
      var invalid = ECGParameters.Normal;
      invalid.bpm = float.NaN;
      Assert.That(validator.Invoke(null, new object[] { new object[] { invalid } }), Is.False);
      invalid.bpm = float.PositiveInfinity;
      Assert.That(validator.Invoke(null, new object[] { new object[] { invalid } }), Is.False);
    }

    [Test]
    public void AliasMatchingRequiresAnExactIdentifier()
    {
      MethodInfo matches = typeof(TriageScenarioEventBootstrap).GetMethod(
        "MatchesAnyAlias", BindingFlags.Static | BindingFlags.NonPublic);
      Assert.That(matches, Is.Not.Null);
      var aliases = new[] { "NurseA", "nurse_a", "a" };

      Assert.That(matches.Invoke(null, new object[] { "nurse_a", aliases }), Is.True);
      Assert.That(matches.Invoke(null, new object[] { "patient_a", aliases }), Is.False,
        "짧은 별칭 'a'가 다른 식별자의 부분 문자열과 일치해서는 안 됩니다.");
    }

    [Test]
    public void DuplicateLineConnectionIdentifiersAreRejected()
    {
      var firstObject = new GameObject("duplicate-line-owner-first");
      var secondObject = new GameObject("duplicate-line-owner-second");
      try
      {
        firstObject.AddComponent<PatientController>();
        secondObject.AddComponent<PatientController>();
        var firstPointObject = new GameObject("duplicate_test_point");
        firstPointObject.transform.SetParent(firstObject.transform, false);
        var secondPointObject = new GameObject("duplicate_test_point");
        secondPointObject.transform.SetParent(secondObject.transform, false);
        var first = firstPointObject.AddComponent<ElectricalLineConnectionPoint>();
        var second = secondPointObject.AddComponent<ElectricalLineConnectionPoint>();
        Assert.That(first.ConnectionIdentifier, Is.EqualTo(second.ConnectionIdentifier));

        MethodInfo find = typeof(LineConnectionService).GetMethod(
          "TryFindConnectionPoint", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(find, Is.Not.Null);
        LogAssert.Expect(LogType.Error,
          new System.Text.RegularExpressions.Regex("duplicate_test_point.*중복"));
        object[] arguments = { first.ConnectionIdentifier, null };
        Assert.That(find.Invoke(null, arguments), Is.False);
        Assert.That(arguments[1], Is.Null);
      }
      finally
      {
        UnityEngine.Object.DestroyImmediate(firstObject);
        UnityEngine.Object.DestroyImmediate(secondObject);
      }
    }

    [Test]
    public void RubricUnsubscribeClearsStaticSubscriptionWithoutController()
    {
      var host = new GameObject("rubric-unsubscribe-test");
      host.SetActive(false);
      try
      {
        var recorder = host.AddComponent<RubricRecorder>();
        FieldInfo subscribed = typeof(RubricRecorder).GetField(
          "_signalRegisteredSubscribed", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo unsubscribe = typeof(RubricRecorder).GetMethod(
          "Unsubscribe", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(subscribed, Is.Not.Null);
        Assert.That(unsubscribe, Is.Not.Null);

        subscribed.SetValue(recorder, true);
        unsubscribe.Invoke(recorder, null);
        Assert.That(subscribed.GetValue(recorder), Is.False);
      }
      finally
      {
        UnityEngine.Object.DestroyImmediate(host);
      }
    }

    private static MethodInfo FindPatientMethod(string name) =>
      typeof(PatientController).GetMethod(name,
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

  }
}
