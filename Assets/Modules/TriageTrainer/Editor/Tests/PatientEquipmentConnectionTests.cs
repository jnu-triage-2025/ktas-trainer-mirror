using System.Linq;
using System.Reflection;
using System.Text.Json;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Scenario;
using NUnit.Framework;
using TriageTrainer.Entity;
using TriageTrainer.Entity.IntravenousLine;
using TriageTrainer.Entity.LineConnection;
using TriageTrainer.Entity.OxyLine;
using TriageTrainer.Scenario;
using UnityEngine;

namespace TriageTrainer.Tests
{
  public sealed class PatientEquipmentConnectionTests
  {
    [Test]
    public void CareZoneMissingEquipmentFallbackGameRuleParsesCommaSeparatedValues()
    {
      try
      {
        Assert.That(ScenarioGameRules.TrySetMissingCareZoneEquipmentFallback(
          "oxyflowmeter,wall_suction", out _), Is.True);
        Assert.That(ScenarioGameRules.AllowsMissingCareZoneEquipmentFallback(
          CareZoneMissingEquipmentFallback.WallSuction), Is.True);
        Assert.That(ScenarioGameRules.AllowsMissingCareZoneEquipmentFallback(
          CareZoneMissingEquipmentFallback.Oxyflowmeter), Is.True);
        Assert.That(ScenarioGameRules.AllowsMissingCareZoneEquipmentFallback(
          CareZoneMissingEquipmentFallback.Defibrillator), Is.False);
        Assert.That(ScenarioGameRules.FormatMissingCareZoneEquipmentFallback(),
          Is.EqualTo("wall_suction,oxyflowmeter"));
      }
      finally
      {
        ScenarioGameRules.TrySetMissingCareZoneEquipmentFallback("defibrillator", out _);
      }
    }

    [Test]
    public void DefibrillatorResolutionPrefersCareZoneCartThenNearestFallback()
    {
      var root = new GameObject("defibrillator-resolution-test");
      root.SetActive(false);
      root.transform.position = new Vector3(10000f, 10000f, 10000f);
      try
      {
        var zoneObject = new GameObject("care-zone");
        zoneObject.transform.SetParent(root.transform, false);
        var zone = zoneObject.AddComponent<PatientCareDescriptionZone>();
        zone.ConfigureArea(Vector3.zero, new Vector3(4f, 4f, 4f));

        var patientObject = new GameObject("patient");
        patientObject.transform.SetParent(root.transform, false);
        var patient = patientObject.AddComponent<PatientController>();

        var inZoneObject = new GameObject("in-zone-cart");
        inZoneObject.transform.SetParent(root.transform, false);
        var inZoneCart = inZoneObject.AddComponent<DefibrillatorCartController>();

        var nearerFallbackObject = new GameObject("nearer-fallback-cart");
        nearerFallbackObject.transform.SetParent(root.transform, false);
        nearerFallbackObject.transform.localPosition = new Vector3(5f, 0f, 0f);
        var nearerFallbackCart = nearerFallbackObject.AddComponent<DefibrillatorCartController>();

        var resolve = typeof(TriageScenarioEventBootstrap).GetMethod(
          "ResolveDefibrillatorCart", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(resolve, Is.Not.Null);
        Assert.That(resolve.Invoke(null, new object[] { patient }), Is.SameAs(inZoneCart),
          "CareZone 안의 제세동 카트가 구역 밖 카트보다 우선되어야 합니다.");

        inZoneObject.transform.localPosition = new Vector3(10f, 0f, 0f);
        Assert.That(resolve.Invoke(null, new object[] { patient }), Is.SameAs(nearerFallbackCart),
          "CareZone 안에 카트가 없으면 구역 중심에서 가장 가까운 카트를 사용해야 합니다.");
        Assert.That(zone.DefibrillatorCarts, Is.Empty);
      }
      finally
      {
        Object.DestroyImmediate(root);
      }
    }

    [Test]
    public void DefibrillatorResolutionTreatsPartialColliderOverlapAsInsideZone()
    {
      var root = new GameObject("defibrillator-overlap-test");
      root.transform.position = new Vector3(12000f, 9000f, -8000f);
      try
      {
        var zoneObject = new GameObject("care-zone");
        zoneObject.transform.SetParent(root.transform, false);
        var zone = zoneObject.AddComponent<PatientCareDescriptionZone>();
        zone.ConfigureArea(Vector3.zero, new Vector3(4f, 4f, 4f));

        var patientObject = new GameObject("patient");
        patientObject.transform.SetParent(root.transform, false);
        var patient = patientObject.AddComponent<PatientController>();

        var overlappingObject = new GameObject("partially-overlapping-cart");
        overlappingObject.transform.SetParent(root.transform, false);
        overlappingObject.transform.localPosition = new Vector3(2.5f, 0f, 0f);
        var overlappingCollider = overlappingObject.AddComponent<BoxCollider>();
        overlappingCollider.size = new Vector3(2f, 1f, 1f);
        var overlappingCart = overlappingObject.AddComponent<DefibrillatorCartController>();

        var outsideObject = new GameObject("outside-cart");
        outsideObject.transform.SetParent(root.transform, false);
        outsideObject.transform.localPosition = new Vector3(2.1f, 0f, 0f);
        outsideObject.AddComponent<BoxCollider>().size = Vector3.one * 0.1f;
        outsideObject.AddComponent<DefibrillatorCartController>();

        var resolve = typeof(TriageScenarioEventBootstrap).GetMethod(
          "ResolveDefibrillatorCart", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(resolve, Is.Not.Null);
        Assert.That(resolve.Invoke(null, new object[] { patient }), Is.SameAs(overlappingCart),
          "카트 원점이 밖에 있어도 Collider 일부가 Zone과 겹치면 Zone 내부 장비가 우선되어야 합니다.");
        Assert.That(zone.DefibrillatorCarts, Does.Contain(overlappingCart));
      }
      finally
      {
        Object.DestroyImmediate(root);
      }
    }

    [Test]
    public void DefibrillatorResolutionUsesPatientWorldPositionInsideCareZone()
    {
      var root = new GameObject("defibrillator-patient-distance-test");
      root.transform.position = new Vector3(14000f, -3000f, 7000f);
      try
      {
        var zoneObject = new GameObject("care-zone");
        zoneObject.transform.SetParent(root.transform, false);
        var zone = zoneObject.AddComponent<PatientCareDescriptionZone>();
        zone.ConfigureArea(Vector3.zero, new Vector3(20f, 4f, 4f));

        var patientObject = new GameObject("patient");
        patientObject.transform.SetParent(root.transform, false);
        patientObject.transform.localPosition = new Vector3(8f, 0f, 0f);
        var patient = patientObject.AddComponent<PatientController>();

        var zoneCenterCartObject = new GameObject("zone-center-cart");
        zoneCenterCartObject.transform.SetParent(root.transform, false);
        var zoneCenterCart = zoneCenterCartObject.AddComponent<DefibrillatorCartController>();

        var patientNearCartObject = new GameObject("patient-near-cart");
        patientNearCartObject.transform.SetParent(root.transform, false);
        patientNearCartObject.transform.localPosition = new Vector3(7f, 0f, 0f);
        var patientNearCart = patientNearCartObject.AddComponent<DefibrillatorCartController>();

        var resolve = typeof(TriageScenarioEventBootstrap).GetMethod(
          "ResolveDefibrillatorCart", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(resolve, Is.Not.Null);
        Assert.That(zone.DefibrillatorCarts, Does.Contain(zoneCenterCart));
        Assert.That(zone.DefibrillatorCarts, Does.Contain(patientNearCart));
        Assert.That(resolve.Invoke(null, new object[] { patient }), Is.SameAs(patientNearCart),
          "CareZone 안의 복수 카트 중 환자의 월드 좌표에서 가장 가까운 카트를 선택해야 합니다.");
      }
      finally
      {
        Object.DestroyImmediate(root);
      }
    }

    [Test]
    public void DefibrillatorFallbackUsesWorldPositionDistance()
    {
      var root = new GameObject("defibrillator-world-position-fallback-test");
      root.SetActive(false);
      root.transform.position = new Vector3(15000f, -6000f, 11000f);
      try
      {
        var patientObject = new GameObject("patient");
        patientObject.transform.SetParent(root.transform, false);
        patientObject.transform.localPosition = new Vector3(20f, 0f, 0f);
        var patient = patientObject.AddComponent<PatientController>();

        var worldNearObject = new GameObject("world-near-cart");
        worldNearObject.transform.position = patient.transform.position + Vector3.right;
        var worldNearCart = worldNearObject.AddComponent<DefibrillatorCartController>();

        var localLookingNearObject = new GameObject("local-looking-near-cart");
        localLookingNearObject.transform.SetParent(root.transform, false);
        localLookingNearObject.transform.localPosition = Vector3.zero;
        localLookingNearObject.AddComponent<DefibrillatorCartController>();

        var resolve = typeof(TriageScenarioEventBootstrap).GetMethod(
          "ResolveDefibrillatorCart", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(resolve, Is.Not.Null);
        Assert.That(resolve.Invoke(null, new object[] { patient }), Is.SameAs(worldNearCart),
          "CareZone이 없을 때 fallback 거리는 환자와 카트의 global position으로 계산해야 합니다.");

        Object.DestroyImmediate(worldNearObject);
      }
      finally
      {
        Object.DestroyImmediate(root);
      }
    }

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
        var patientPoint = AttachPatientBCIvPoint(patient);
        patient.ActivatePatientBCNurseCStage();
        InvokePrivate(patient, "NotifyPatientBCPupilCompleted");
        Assert.That((bool)InvokePrivate(patient, "TryAdvancePatientBCIvStageAuthoritative"), Is.True);
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
        AttachPatientBCIvPoint(patient);
        patient.ActivatePatientBCNurseCStage();
        InvokePrivate(patient, "NotifyPatientBCPupilCompleted");
        var salinePoint = salinePointObject.AddComponent<IntravenousLineConnectionPoint>();
        salinePoint.SetIdentifier("connect_cannula_and_ns1");
        RegisterPhysicalLine(lineObject, salinePoint, patient.PatientBCIvAttachmentPoint);

        using (ScenarioSignalPlayerContext.Push("nurse-c", "Nurse C"))
          salinePoint.NotifyConnectionCompleted(patient.PatientBCIvAttachmentPoint);

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
    public void UnwiredPatientBCIvAttachmentPointStaysNull()
    {
      var patientObject = new GameObject("patient_b");
      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier("patient_b");

        Assert.That(patient.PatientBCIvAttachmentPoint, Is.Null,
          "정맥로 포인트는 프리팹 참조로만 배선한다(식별자 검색/런타임 생성 없음)");

        var point = AttachPatientBCIvPoint(patient);
        Assert.That(patient.PatientBCIvAttachmentPoint, Is.SameAs(point));
        Assert.That(patient.ConfiguredPatientBCIvAttachmentPoint, Is.SameAs(point));
      }
      finally
      {
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
        AttachPatientBCIvPoint(patient);
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
          patient, saline, saline, null), Is.False,
          "an unwired patient IV point must not match a null endpoint");

        var patientPoint = AttachPatientBCIvPoint(patient);
        Assert.That(LineConnectionService.IsExactPatientNormalSalineEndpointPair(
          patient, saline, saline, patientPoint), Is.True,
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
          BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
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

    [Test]
    public void ConnectedIntravenousLineExposesOneDisconnectHintNamedAfterItsPatient()
    {
      var patientObject = new GameObject("patient_c");
      var loosePointObject = new GameObject("unowned-normal-saline-point");
      var lineObject = new GameObject("physical-line");
      var otherStartObject = new GameObject("other-line-start");
      var otherEndObject = new GameObject("other-line-end");
      var otherLineObject = new GameObject("other-physical-line");

      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier("patient_c");
        patient.Descriptor.name = "이영희";
        var patientPoint = AttachPatientBCIvPoint(patient);
        var loosePoint = loosePointObject.AddComponent<IntravenousLineConnectionPoint>();

        // 연결 전에는 해제 항목 자체가 노출되지 않으므로 묶을 그룹도 없다.
        Assert.That(DisconnectGroupOf(patientPoint), Is.Null);

        RegisterPhysicalLine(lineObject, loosePoint, patientPoint);

        // 줄 하나의 두 끝점은 같은 그룹에 들어가므로 가까운 쪽 하나만 힌트에 남는다.
        string group = DisconnectGroupOf(patientPoint);
        Assert.That(group, Is.Not.Null.And.Not.Empty);
        Assert.That(DisconnectGroupOf(loosePoint), Is.EqualTo(group),
          "한 줄의 두 끝점(환자 정맥로 쪽·침대 걸이 쪽)은 한 항목으로 합쳐져야 한다.");

        // 환자에 붙은 끝점은 누구의 줄인지 이름으로 알려준다.
        Assert.That(DisconnectTextOf(patientPoint), Is.EqualTo("이영희의 수액 줄 해제"));
        // 환자도 침대도 아닌 곳에 놓인 끝점은 원래 문구를 그대로 쓴다.
        Assert.That(DisconnectTextOf(loosePoint), Is.EqualTo("수액 줄 해제"));

        // 다른 줄은 다른 그룹이라 환자별 줄이 서로를 가리지 않는다.
        var otherStart = otherStartObject.AddComponent<IntravenousLineConnectionPoint>();
        var otherEnd = otherEndObject.AddComponent<IntravenousLineConnectionPoint>();
        RegisterPhysicalLine(otherLineObject, otherStart, otherEnd);
        Assert.That(DisconnectGroupOf(otherStart), Is.Not.EqualTo(group));
      }
      finally
      {
        UnityEngine.Object.DestroyImmediate(otherLineObject);
        UnityEngine.Object.DestroyImmediate(otherEndObject);
        UnityEngine.Object.DestroyImmediate(otherStartObject);
        UnityEngine.Object.DestroyImmediate(lineObject);
        UnityEngine.Object.DestroyImmediate(loosePointObject);
        UnityEngine.Object.DestroyImmediate(patientObject);
      }
    }

    /// <summary>해제 항목은 이 지점에서 유일하게 "가장 가까운 하나만" 규칙을 쓰는 항목이다.</summary>
    private static INearestOnlyInteract FindDisconnectInteract(IntravenousLineConnectionPoint point)
    {
      var matches = point.Interacts.OfType<INearestOnlyInteract>().ToArray();
      Assert.That(matches, Has.Length.EqualTo(1), point.name);
      return matches[0];
    }

    private static string DisconnectGroupOf(IntravenousLineConnectionPoint point)
      => FindDisconnectInteract(point).NearestOnlyGroup;

    private static string DisconnectTextOf(IntravenousLineConnectionPoint point)
      => ((IInteract)FindDisconnectInteract(point)).DisplayText;

    /// <summary>
    /// 환자 B/C 정맥로 IV 연결 지점은 환자 유형별 State 컴포넌트가 프리팹 참조로 주입한다.
    /// 테스트에서도 동일한 주입 경로로 배선한다(식별자 문자열 검색이나 런타임 생성은 없다).
    /// </summary>
    private static IntravenousLineConnectionPoint AttachPatientBCIvPoint(PatientController patient)
    {
      var pointObject = new GameObject("patient-bc-iv-point");
      pointObject.transform.SetParent(patient.transform, false);
      var point = pointObject.AddComponent<IntravenousLineConnectionPoint>();
      point.SetAllowsMultipleConnections(true);
      patient.SetPatientBCIvAttachmentPointFromPatientComponent(point);
      return point;
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
