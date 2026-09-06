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
        Assert.That(controller.IntravenousLineCannulaSupported, Is.True,
          "B Male/Female의 기존 직렬화 기본값은 캐뉼라 지원이다");
        Assert.That(controller.SupportExternalRefs.IntravenousFluids, Is.Empty);
        Assert.That(controller.SupportExternalRefs.SuctionWalls, Is.Empty);
        Assert.That(controller.SupportExternalRefs.Oxyflowmeters, Is.Empty);

        // 사정 동작 목록은 더 이상 직렬화하지 않는다(코드 리터럴 + 시나리오 데이터).
        Assert.That(typeof(PatientController).GetField(
            "_assessActions", BindingFlags.Instance | BindingFlags.NonPublic), Is.Null,
          "사정 동작 설정은 프리팹 필드가 아니라 레지스트리 정의여야 합니다.");

        // Inspector의 Reset 메뉴는 Reset 훅 대신 기본 직렬화값 적용 후 OnValidate가
        // 실행될 수 있으므로, 그 경로도 동일하게 이전 프리팹 기본값을 복구해야 한다.
        controller.IntravenousLineCannulaSupported = false;
        InvokeOnValidate(controller);

        Assert.That(controller.IntravenousLineCannulaSupported, Is.True);
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

    private static void InvokeOnValidate(PatientController controller)
    {
      var onValidate = typeof(PatientController).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic);
      Assert.That(onValidate, Is.Not.Null);
      onValidate.Invoke(controller, null);
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
