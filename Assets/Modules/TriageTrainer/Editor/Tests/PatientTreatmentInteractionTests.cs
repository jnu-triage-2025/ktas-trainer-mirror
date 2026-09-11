using System.Collections.Generic;
using System.Reflection;
using MultiplayerInfrastructure.ItemSystem;
using MultiplayerInfrastructure.Player;
using NUnit.Framework;
using TriageTrainer.Entity;
using TriageTrainer.ItemDefinitions;
using TriageTrainer.Patient;
using UnityEngine;

namespace TriageTrainer.Tests
{
  public class PatientTreatmentInteractionTests
  {
    [Test]
    public void TreatmentStateStoresClinicalStateIndependentlyFromDisplay()
    {
      var state = new PatientTreatmentState();

      Assert.That(state.IsApplied(PatientController.TreatmentGauze), Is.False);
      Assert.That(state.SetApplied(PatientController.TreatmentGauze, true), Is.True);
      Assert.That(state.IsApplied(PatientController.TreatmentGauze), Is.True);
      Assert.That(state.SetApplied(PatientController.TreatmentGauze, true), Is.False,
        "같은 처치를 재적용해 중복 소비/신호가 발생해서는 안 됩니다.");
    }

    [Test]
    public void SettingTreatmentStateUpdatesSupportedDisplayAndVisual()
    {
      var patientObject = new GameObject("treatment-state-patient");
      var visual = new GameObject("gauze-visual");
      visual.transform.SetParent(patientObject.transform);
      visual.SetActive(false);

      try
      {
        var displayState = patientObject.AddComponent<PatientDisplayState>();
        displayState.DisplaySupports.GauzePatchedOnThorax = true;
        displayState.ChildGameObjects.GauzePatchedOnThorax = visual;
        var controller = patientObject.AddComponent<PatientController>();

        bool changed = controller.SetTreatmentApplied(
          PatientController.TreatmentGauze,
          true,
          PatientController.TreatmentDisplay.GauzePatchedOnThorax);

        Assert.That(changed, Is.True);
        Assert.That(controller.IsTreatmentApplied(PatientController.TreatmentGauze), Is.True);
        Assert.That(displayState.DisplayState.GauzePatchedOnThorax, Is.True);
        Assert.That(visual.activeSelf, Is.True);
      }
      finally
      {
        Object.DestroyImmediate(patientObject);
      }
    }

    [Test]
    public void PlasterInteractionRequiresGauzeTreatmentFirst()
    {
      var patientObject = new GameObject("plaster-order-patient");
      var dressingVisual = new GameObject("dressing-visual");
      dressingVisual.transform.SetParent(patientObject.transform);

      try
      {
        var displayState = patientObject.AddComponent<PatientDisplayState>();
        displayState.DisplaySupports.GauzeDressingDoneOnThorax = true;
        displayState.ChildGameObjects.GauzeDressingDoneOnThorax = dressingVisual;
        var controller = patientObject.AddComponent<PatientController>();
        var canApply = typeof(PatientController).GetMethod(
          "CanApplyHeldTreatmentItem", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(canApply, Is.Not.Null);

        Assert.That((bool)canApply.Invoke(controller, new object[] { "plaster" }), Is.False,
          "거즈 처치 전에는 플라스터 적용 메뉴가 노출되면 안 됩니다.");

        controller.SetTreatmentApplied(PatientController.TreatmentGauze, true);
        Assert.That((bool)canApply.Invoke(controller, new object[] { "plaster" }), Is.True,
          "거즈 처치 완료 후에는 플라스터 적용 메뉴가 노출되어야 합니다.");

        controller.SetTreatmentApplied(PatientController.TreatmentPlasterOnGauze, true,
          PatientController.TreatmentDisplay.GauzeDressingDoneOnThorax);
        Assert.That((bool)canApply.Invoke(controller, new object[] { "plaster" }), Is.False,
          "완료된 플라스터 처치를 다시 노출해 중복 소비하면 안 됩니다.");
      }
      finally
      {
        Object.DestroyImmediate(patientObject);
      }
    }

    [Test]
    public void DirectItemUseRejectsUnsupportedTreatmentDisplay()
    {
      var patientObject = new GameObject("unsupported-treatment-patient");
      try
      {
        patientObject.AddComponent<PatientDisplayState>();
        var controller = patientObject.AddComponent<PatientController>();
        var apply = typeof(PatientController).GetMethod(
          "ApplyItemUse", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(apply, Is.Not.Null);

        bool applied = (bool)apply.Invoke(controller, new object[] { "gauze" });

        Assert.That(applied, Is.False,
          "상호작용 메뉴를 우회한 직접 아이템 사용도 미지원 Display 처치를 거부해야 합니다.");
        Assert.That(controller.IsTreatmentApplied(PatientController.TreatmentGauze), Is.False,
          "미지원 처치를 실제 처치 상태에 기록하면 안 됩니다.");
      }
      finally
      {
        Object.DestroyImmediate(patientObject);
      }
    }

    [Test]
    public void BleedingControlRequiresEquippedGloves()
    {
      var patientObject = new GameObject("glove-required-patient");
      var playerObject = new GameObject("glove-required-player");

      try
      {
        var displayState = patientObject.AddComponent<PatientDisplayState>();
        displayState.DisplaySupports.GauzePatchedOnThorax = true;
        var patient = patientObject.AddComponent<PatientController>();
        var player = playerObject.AddComponent<PlayerController>();

        var gauze = (Item)System.Activator.CreateInstance(typeof(Gauze));
        gauze.CurrentStackCount = 1;
        var inventorySlot = new InventorySlotModelDTO();
        inventorySlot.SetItem(gauze);
        typeof(PlayerController).GetField("_slots", BindingFlags.Instance | BindingFlags.NonPublic)
          ?.SetValue(player, new List<InventorySlotModelDTO> { inventorySlot });

        var gloveSlot = new EquipmentSlotModelDTO(EquipmentSlotType.Glove);
        typeof(PlayerController).GetField("_equipmentSlots", BindingFlags.Instance | BindingFlags.NonPublic)
          ?.SetValue(player, new List<EquipmentSlotModelDTO> { gloveSlot });

        Assert.That(patient.OnItemUsed(player.PlayerEntity, Gauze.Identifier), Is.False,
          "장갑을 착용하지 않은 플레이어는 거즈로 지혈할 수 없어야 합니다.");
        Assert.That(player.CountItemInInventory(Gauze.Identifier), Is.EqualTo(1),
          "장갑 미착용으로 지혈이 거부되면 거즈를 소비하면 안 됩니다.");
        Assert.That(patient.IsTreatmentApplied(PatientController.TreatmentGauze), Is.False);

        var gloves = (Item)System.Activator.CreateInstance(typeof(SterileGloves));
        gloves.CurrentStackCount = 1;
        gloveSlot.Equip(gloves);

        Assert.That(patient.OnItemUsed(player.PlayerEntity, Gauze.Identifier), Is.True,
          "장갑을 착용한 플레이어는 거즈로 지혈할 수 있어야 합니다.");
        Assert.That(patient.IsTreatmentApplied(PatientController.TreatmentGauze), Is.True);
      }
      finally
      {
        Object.DestroyImmediate(playerObject);
        Object.DestroyImmediate(patientObject);
      }
    }
  }
}
