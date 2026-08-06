using NUnit.Framework;
using MultiplayerInfrastructure.Scenario;
using System.Reflection;
using System.Text.Json;
using TriageTrainer.Entity;
using TriageTrainer.Entity.IntravenousLine;
using TriageTrainer.Entity.LineConnection;
using TriageTrainer.Entity.OxyLine;
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

    [TestCase("patient_b", "player-b", "Nurse B")]
    [TestCase("patient_c", "player-c", "Nurse C")]
    public void NormalSalineConnectionRaisesScopedGateAndParameterizedBaseSignal(
      string patientIdentifier,
      string playerIdentifier,
      string playerDisplayName)
    {
      var patientObject = new GameObject(patientIdentifier);
      var salinePointObject = new GameObject("normal-saline-point");
      var lineObject = new GameObject("physical-line");

      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier(patientIdentifier);
        patient.ActivatePatientBCNurseCStage();
        InvokePrivate(patient, "NotifyPatientBCPupilCompleted");
        Assert.That((bool)InvokePrivate(patient, "TryAdvancePatientBCIvStageAuthoritative"), Is.True);
        var patientPoint = patient.IvAttachmentPoint;
        var salinePoint = salinePointObject.AddComponent<IntravenousLineConnectionPoint>();
        salinePoint.SetIdentifier("connect_cannula_and_ns1");
        RegisterPhysicalLine(lineObject, salinePoint, patientPoint);

        using (ScenarioSignalPlayerContext.Push(playerIdentifier, playerDisplayName))
          salinePoint.NotifyConnectionCompleted(patientPoint);

        Assert.That(ScenarioInteractionSignals.IsRaised(
          $"connect_cannula_and_ns1_{patientIdentifier}"), Is.True);
        Assert.That(ScenarioInteractionSignals.IsRaised("connect_cannula_and_ns1"), Is.True);
        string otherPatientIdentifier = patientIdentifier == "patient_b" ? "patient_c" : "patient_b";
        Assert.That(ScenarioInteractionSignals.IsRaised(
          $"connect_cannula_and_ns1_{otherPatientIdentifier}"), Is.False);
        Assert.That(ScenarioSignalParameterStore.TryGetForPlayer(
          "connect_cannula_and_ns1", playerIdentifier, out var parameter), Is.True);
        Assert.That(parameter.PlayerDisplayName, Is.EqualTo(playerDisplayName));
        using JsonDocument payload = JsonDocument.Parse(parameter.ParameterJson);
        Assert.That(payload.RootElement.GetProperty("patientIdentifier").GetString(),
          Is.EqualTo(patientIdentifier));
      }
      finally
      {
        ScenarioNetworkRelay.FlushSignalParametersAuthoritative();
        ScenarioInteractionSignals.Clear("connect_cannula_and_ns1");
        ScenarioInteractionSignals.Clear($"connect_cannula_and_ns1_{patientIdentifier}");
        Object.DestroyImmediate(lineObject);
        Object.DestroyImmediate(salinePointObject);
        Object.DestroyImmediate(patientObject);
      }
    }

    [Test]
    public void PatientANormalSalineConnectionKeepsParameterlessBaseGate()
    {
      var patientObject = new GameObject("patient_a");
      var salinePointObject = new GameObject("normal-saline-point");

      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier("patient_a");
        var salinePoint = salinePointObject.AddComponent<IntravenousLineConnectionPoint>();
        salinePoint.SetIdentifier("connect_cannula_and_ns1");

        salinePoint.NotifyConnectionCompleted(patient.IvAttachmentPoint);

        Assert.That(ScenarioInteractionSignals.IsRaised("connect_cannula_and_ns1"), Is.True);
        Assert.That(ScenarioSignalParameterStore.TryGetLatest(
          "connect_cannula_and_ns1", out var parameter), Is.True);
        Assert.That(parameter.HasParameter, Is.False);
      }
      finally
      {
        ScenarioNetworkRelay.FlushSignalParametersAuthoritative();
        ScenarioInteractionSignals.Clear("connect_cannula_and_ns1");
        Object.DestroyImmediate(salinePointObject);
        Object.DestroyImmediate(patientObject);
      }
    }

    [Test]
    public void EarlyPhysicalNormalSalineConnectionIsCreditedWhenIvStageAdvances()
    {
      var patientObject = new GameObject("patient_b");
      var salinePointObject = new GameObject("normal-saline-point");
      var lineObject = new GameObject("physical-line");
      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier("patient_b");
        patient.ActivatePatientBCNurseCStage();
        InvokePrivate(patient, "NotifyPatientBCPupilCompleted");
        var salinePoint = salinePointObject.AddComponent<IntravenousLineConnectionPoint>();
        salinePoint.SetIdentifier("connect_cannula_and_ns1");
        RegisterPhysicalLine(lineObject, salinePoint, patient.IvAttachmentPoint);

        using (ScenarioSignalPlayerContext.Push("nurse-c", "Nurse C"))
          salinePoint.NotifyConnectionCompleted(patient.IvAttachmentPoint);

        Assert.That(ScenarioInteractionSignals.IsRaised("connect_cannula_and_ns1_patient_b"), Is.False);
        Assert.That((bool)InvokePrivate(patient, "TryAdvancePatientBCIvStageAuthoritative"), Is.True);
        Assert.That(ScenarioInteractionSignals.IsRaised("connect_cannula_and_ns1_patient_b"), Is.True,
          "the already-physical line must be credited exactly when IV reaches the saline stage");
        Assert.That(ScenarioSignalParameterStore.TryGetForPlayer(
          "connect_cannula_and_ns1", "nurse-c", out _), Is.True);
      }
      finally
      {
        ScenarioNetworkRelay.FlushSignalParametersAuthoritative();
        ScenarioInteractionSignals.Clear("connect_cannula_and_ns1");
        ScenarioInteractionSignals.Clear("connect_cannula_and_ns1_patient_b");
        Object.DestroyImmediate(lineObject);
        Object.DestroyImmediate(salinePointObject);
        Object.DestroyImmediate(patientObject);
      }
    }

    [Test]
    public void NormalSalineCompletionRejectsMissingPhysicalLine()
    {
      var patientObject = new GameObject("patient_c");
      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier("patient_c");
        patient.ActivatePatientBCNurseCStage();
        InvokePrivate(patient, "NotifyPatientBCPupilCompleted");
        Assert.That((bool)InvokePrivate(patient, "TryAdvancePatientBCIvStageAuthoritative"), Is.True);

        Assert.That(patient.TryCompletePatientBCNormalSalineConnection(), Is.False);
        Assert.That(ScenarioInteractionSignals.IsRaised("connect_cannula_and_ns1_patient_c"), Is.False);
      }
      finally
      {
        ScenarioInteractionSignals.Clear("connect_cannula_and_ns1_patient_c");
        Object.DestroyImmediate(patientObject);
      }
    }

    [Test]
    public void NormalSalineCompletionRejectsUnconnectedSpoofEndpoint()
    {
      var patientObject = new GameObject("patient_b");
      var spoofObject = new GameObject("spoof-normal-saline-point");
      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier("patient_b");
        patient.ActivatePatientBCNurseCStage();
        InvokePrivate(patient, "NotifyPatientBCPupilCompleted");
        Assert.That((bool)InvokePrivate(patient, "TryAdvancePatientBCIvStageAuthoritative"), Is.True);
        var spoof = spoofObject.AddComponent<IntravenousLineConnectionPoint>();
        spoof.SetIdentifier("connect_cannula_and_ns1");
        var cachedPoint = typeof(PatientController).GetField(
          "_patientBCPhysicalNormalSalinePoint",
          BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(cachedPoint, Is.Not.Null);
        cachedPoint.SetValue(patient, spoof);

        Assert.That(patient.TryCompletePatientBCNormalSalineConnection(), Is.False,
          "an identifier-matching cached client endpoint is not physical topology");
        Assert.That(patient.TryCompletePatientBCNormalSalineConnection(spoof), Is.False);
        Assert.That(ScenarioInteractionSignals.IsRaised("connect_cannula_and_ns1_patient_b"), Is.False);
      }
      finally
      {
        ScenarioInteractionSignals.Clear("connect_cannula_and_ns1_patient_b");
        Object.DestroyImmediate(spoofObject);
        Object.DestroyImmediate(patientObject);
      }
    }

    [Test]
    public void AuthoritativeNormalSalineTopologyRequiresExactPatientIvEndpoint()
    {
      var patientObject = new GameObject("patient_b");
      var salineObject = new GameObject("normal-saline-point");
      var spoofIvObject = new GameObject("spoof-patient-iv-point");
      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier("patient_b");
        var saline = salineObject.AddComponent<IntravenousLineConnectionPoint>();
        saline.SetIdentifier("connect_cannula_and_ns1");
        var spoofIv = spoofIvObject.AddComponent<IntravenousLineConnectionPoint>();

        Assert.That(LineConnectionService.IsExactPatientNormalSalineEndpointPair(
          patient, saline, saline, patient.IvAttachmentPoint), Is.True,
          "host/server topology accepts the exact authoritative endpoint pair");
        Assert.That(LineConnectionService.IsExactPatientNormalSalineEndpointPair(
          patient, saline, saline, spoofIv), Is.False,
          "a remote request cannot substitute another non-null IV endpoint");
        Assert.That(LineConnectionService.IsExactPatientNormalSalineEndpointPair(
          patient, saline, saline, saline), Is.False);
      }
      finally
      {
        Object.DestroyImmediate(spoofIvObject);
        Object.DestroyImmediate(salineObject);
        Object.DestroyImmediate(patientObject);
      }
    }

    [Test]
    public void ReplicatedConnectionCreatesExactIdempotentObserverTopology()
    {
      var serviceObject = new GameObject("line-service");
      var firstObject = new GameObject("first-point");
      var secondObject = new GameObject("second-point");
      try
      {
        var service = serviceObject.AddComponent<LineConnectionService>();
        var first = firstObject.AddComponent<IntravenousLineConnectionPoint>();
        var second = secondObject.AddComponent<IntravenousLineConnectionPoint>();
        var apply = typeof(LineConnectionService).GetMethod(
          "ApplyReplicatedConnection",
          BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(apply, Is.Not.Null);

        Assert.That((bool)apply.Invoke(service, new object[] { first, second }), Is.True);
        Assert.That(first.IsPhysicallyConnectedTo(second), Is.True);
        Assert.That(second.IsPhysicallyConnectedTo(first), Is.True);
        Assert.That((bool)apply.Invoke(service, new object[] { first, second }), Is.False,
          "a host observer echo or duplicate packet must not create duplicate topology");
      }
      finally
      {
        Object.DestroyImmediate(secondObject);
        Object.DestroyImmediate(firstObject);
        Object.DestroyImmediate(serviceObject);
      }
    }

    [Test]
    public void AutomaticConnectionCreatesAndRemovesMatchingOxyTopology()
    {
      var serviceObject = new GameObject("line-service");
      var firstObject = new GameObject("oxy-source-point");
      var secondObject = new GameObject("oxy-patient-point");
      try
      {
        var service = serviceObject.AddComponent<LineConnectionService>();
        var first = firstObject.AddComponent<OxyLineConnectionPoint>();
        var second = secondObject.AddComponent<OxyLineConnectionPoint>();

        Assert.That(service.TryCreateAutomaticConnection(first, second), Is.True);
        Assert.That(first.IsPhysicallyConnectedTo(second), Is.True);
        Assert.That(service.TryCreateAutomaticConnection(first, second), Is.False,
          "automatic retries must not create duplicate lines");
        Assert.That(service.DisconnectAutomaticConnection(first, second), Is.True);
        Assert.That(first.IsPhysicallyConnectedTo(second), Is.False);
      }
      finally
      {
        Object.DestroyImmediate(secondObject);
        Object.DestroyImmediate(firstObject);
        Object.DestroyImmediate(serviceObject);
      }
    }

    [Test]
    public void AutomaticDisconnectDoesNotRemoveManualMatchingTopology()
    {
      var serviceObject = new GameObject("line-service");
      var firstObject = new GameObject("oxy-source-point");
      var secondObject = new GameObject("oxy-patient-point");
      try
      {
        var service = serviceObject.AddComponent<LineConnectionService>();
        var first = firstObject.AddComponent<OxyLineConnectionPoint>();
        var second = secondObject.AddComponent<OxyLineConnectionPoint>();

        Assert.That(service.CreateAndRegisterConnection(first, second), Is.True,
          "represents a player-created matching line");
        Assert.That(service.DisconnectAutomaticConnection(first, second), Is.False,
          "CareZone cleanup must not destroy a line it did not create");
        Assert.That(first.IsPhysicallyConnectedTo(second), Is.True);
      }
      finally
      {
        Object.DestroyImmediate(secondObject);
        Object.DestroyImmediate(firstObject);
        Object.DestroyImmediate(serviceObject);
      }
    }

    [Test]
    public void LateJoinSnapshotReplaysDisconnectsAndReconnectsIdempotently()
    {
      var serverObject = new GameObject("server-line-service");
      var clientObject = new GameObject("client-line-service");
      var firstObject = new GameObject("first-point");
      var secondObject = new GameObject("second-point");
      try
      {
        var server = serverObject.AddComponent<LineConnectionService>();
        var client = clientObject.AddComponent<LineConnectionService>();
        var first = firstObject.AddComponent<IntravenousLineConnectionPoint>();
        var second = secondObject.AddComponent<IntravenousLineConnectionPoint>();

        Assert.That(server.CreateAndRegisterConnection(first, second), Is.True);
        Assert.That(server.AddAuthoritativePair(first, second), Is.True);
        Assert.That(server.AddAuthoritativePair(second, first), Is.False,
          "server snapshot registry must deduplicate unordered endpoint pairs");
        Assert.That(server.ActiveAuthoritativePairCount, Is.EqualTo(1));

        Object.DestroyImmediate(GetConnectedLine(first, second));
        client.BeginReplicatedTopologySnapshot();
        Assert.That(client.ApplyReplicatedSnapshotPair(first, second), Is.True,
          "join-after-connect snapshot creates local topology");
        Assert.That(client.ApplyReplicatedSnapshotPair(first, second), Is.False,
          "duplicate snapshot replay is idempotent");
        client.EndReplicatedTopologySnapshot();
        Assert.That(first.IsPhysicallyConnectedTo(second), Is.True);

        Assert.That(client.ApplyReplicatedDisconnect(first, second), Is.True);
        Assert.That(client.ApplyReplicatedDisconnect(first, second), Is.False,
          "disconnect is safe when local topology is already missing");
        Assert.That(server.RemoveAuthoritativePair(first, second), Is.True);
        Assert.That(server.ActiveAuthoritativePairCount, Is.Zero);

        Assert.That(server.AddAuthoritativePair(first, second), Is.True);
        client.BeginReplicatedTopologySnapshot();
        Assert.That(client.ApplyReplicatedSnapshotPair(first, second), Is.True,
          "reconnect snapshot recreates the exact pair");
        client.EndReplicatedTopologySnapshot();
        Assert.That(first.IsPhysicallyConnectedTo(second), Is.True);
      }
      finally
      {
        Object.DestroyImmediate(secondObject);
        Object.DestroyImmediate(firstObject);
        Object.DestroyImmediate(clientObject);
        Object.DestroyImmediate(serverObject);
      }
    }

    private static GameObject GetConnectedLine(LineConnectionPoint first, LineConnectionPoint second)
    {
      Assert.That(first.TryGetConnectedLineObjectTo(second, out var line), Is.True);
      return line;
    }

    private static void RegisterPhysicalLine(
      GameObject lineObject,
      LineConnectionPoint start,
      LineConnectionPoint end)
    {
      var runtime = lineObject.AddComponent<LineConnectionRuntime>();
      runtime.Bind(start, end, null, 2, 0f, 0f, false, 1, 1, 0f, 0f, 0f,
        false, 0.001f, ~0, QueryTriggerInteraction.Ignore);
      start.RegisterConnectedLineObject(lineObject);
      end.RegisterConnectedLineObject(lineObject);
    }

    private static object InvokePrivate(PatientController patient, string methodName)
    {
      var method = typeof(PatientController).GetMethod(
        methodName,
        BindingFlags.Instance | BindingFlags.NonPublic);
      Assert.That(method, Is.Not.Null, methodName);
      return method.Invoke(patient, null);
    }
  }
}
