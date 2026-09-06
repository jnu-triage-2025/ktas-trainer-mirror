using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TriageTrainer.Entity;
using UnityEngine;

namespace TriageTrainer.Tests
{
  public sealed class PatientItemApprovalRecoveryTests
  {
    private GameObject _object;
    private PatientController _patient;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [SetUp]
    public void SetUp()
    {
      _object = new GameObject("item-approval-recovery");
      _patient = _object.AddComponent<PatientController>();
    }

    [TearDown]
    public void TearDown() => UnityEngine.Object.DestroyImmediate(_object);

    [Test]
    public void ExpiredApprovalStaysRejectedWhenConfirmationArrivesLater()
    {
      AddPending("expired", 7, Time.unscaledTime - 11f);
      typeof(PatientController).GetMethod("PruneExpiredPendingItemUses", Private).Invoke(_patient, null);
      Assert.That(Pending, Is.Empty);
      Assert.That(Completed["expired"], Is.EqualTo((7, false)));
      Assert.That(Resolve("expired", 7, true), Is.False);
      Assert.That(Completed["expired"], Is.EqualTo((7, false)));
    }

    [Test]
    public void RepeatedSuccessfulConfirmationDoesNotRefundOrReapply()
    {
      Completed["done"] = (7, true);
      Assert.That(Resolve("done", 7, true), Is.True);
      Assert.That(Resolve("done", 7, false), Is.True);
      Assert.That(Resolve("done", 8, true), Is.False);
      Assert.That(Completed["done"], Is.EqualTo((7, true)));
    }

    [Test]
    public void AnotherPlayerCannotConsumeOrCancelAnApproval()
    {
      AddPending("active", 7, Time.unscaledTime);
      Assert.That(Resolve("active", 8, false), Is.False);
      Assert.That(Pending.Contains("active"), Is.True);
      Assert.That(Resolve("active", 7, false), Is.False);
      Assert.That(Pending, Is.Empty);
      Assert.That(Completed["active"], Is.EqualTo((7, false)));
      Assert.That(Resolve("missing", 7, true), Is.False);
    }

    private IDictionary Pending => (IDictionary)typeof(PatientController).GetField("_pendingItemUses", Private).GetValue(_patient);
    private Dictionary<string, (int ClientId, bool Accepted)> Completed =>
      (Dictionary<string, (int ClientId, bool Accepted)>)typeof(PatientController).GetField("_completedItemUses", Private).GetValue(_patient);
    private bool Resolve(string token, int client, bool consumed) =>
      (bool)typeof(PatientController).GetMethod("ResolveApprovedPatientItemConsumption", Private)
        .Invoke(_patient, new object[] { token, client, consumed });
    private void AddPending(string token, int client, float createdAt)
    {
      var type = typeof(PatientController).GetNestedType("PendingItemUse", BindingFlags.NonPublic);
      var pending = Activator.CreateInstance(type, true);
      type.GetField("ClientId").SetValue(pending, client);
      type.GetField("CreatedAt").SetValue(pending, createdAt);
      Pending[token] = pending;
    }
  }
}
