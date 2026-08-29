using System.Collections.Generic;
using System.Reflection;
using FishNet.Object;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.UI;
using NUnit.Framework;
using TriageTrainer.ItemDefinitions;
using UnityEngine;

namespace TriageTrainer.Tests
{
  public class PlasterDurabilityTests
  {
    [Test]
    public void PlasterStartsWithSixteenDurabilityAndLosesOnePerUse()
    {
      var plaster = new Plaster();

      Assert.That(plaster.HasCurrentDurability, Is.True);
      Assert.That(plaster.CurrentMaxDurability, Is.EqualTo(16));
      Assert.That(plaster.CurrentDurability, Is.EqualTo(16));
      Assert.That(plaster.CurrentDurabilityDeltaOnUse, Is.EqualTo(-1));

      for (int use = 1; use < 16; use++)
      {
        Assert.That(plaster.TryApplyDurabilityOnUse(out bool depleted), Is.True);
        Assert.That(plaster.CurrentDurability, Is.EqualTo(16 - use));
        Assert.That(depleted, Is.False, "16번째 사용 전에는 플라스터가 소진되면 안 됩니다.");
      }

      Assert.That(plaster.TryApplyDurabilityOnUse(out bool depletedOnLastUse), Is.True);
      Assert.That(plaster.CurrentDurability, Is.Zero);
      Assert.That(depletedOnLastUse, Is.True);
    }

    [Test]
    public void DepletedDurabilityRemovesOneStackItemAndResetsNextItem()
    {
      var plaster = new Plaster { CurrentStackCount = 2 };

      for (int use = 1; use <= 16; use++)
        Assert.That(plaster.TryConsumeDurabilityOnUse(out _), Is.True);

      Assert.That(plaster.CurrentStackCount, Is.EqualTo(1));
      Assert.That(plaster.CurrentDurability, Is.EqualTo(16));

      Assert.That(plaster.TryConsumeDurabilityOnUse(out bool stackDepleted), Is.True);
      Assert.That(stackDepleted, Is.False);
      Assert.That(plaster.CurrentStackCount, Is.EqualTo(1));
      Assert.That(plaster.CurrentDurability, Is.EqualTo(15));
    }

    [Test]
    public void ScissorsStartsWithSixteenDurabilityAndLosesOnePerUse()
    {
      var scissors = new Scissors();

      Assert.That(scissors.HasCurrentDurability, Is.True);
      Assert.That(scissors.CurrentMaxDurability, Is.EqualTo(16));
      Assert.That(scissors.CurrentDurability, Is.EqualTo(16));
      Assert.That(scissors.CurrentDurabilityDeltaOnUse, Is.EqualTo(-1));

      Assert.That(scissors.TryApplyDurabilityOnUse(out bool depleted), Is.True);
      Assert.That(scissors.CurrentDurability, Is.EqualTo(15));
      Assert.That(depleted, Is.False);
    }

    [Test]
    public void FullDurabilityHidesBarAndDamagedDurabilityShowsBar()
    {
      var plaster = new Plaster();
      var slot = new UnityEngine.UIElements.VisualElement();
      var track = ItemDurabilityBar.Ensure(slot, "track", "fill");

      ItemDurabilityBar.Update(track, plaster);
      Assert.That(track.style.display.value,
        Is.EqualTo(UnityEngine.UIElements.DisplayStyle.None));

      plaster.TryApplyDurabilityOnUse(out _);
      ItemDurabilityBar.Update(track, plaster);
      Assert.That(track.style.display.value,
        Is.EqualTo(UnityEngine.UIElements.DisplayStyle.Flex));
    }

    [Test]
    public void StackMergePreservesOneDamagedActiveItem()
    {
      var fullStack = new Plaster { CurrentStackCount = 2 };
      var damagedStack = new Plaster { CurrentStackCount = 2, CurrentDurability = 10 };

      fullStack.Merge(damagedStack);

      Assert.That(fullStack.CurrentStackCount, Is.EqualTo(4));
      Assert.That(fullStack.CurrentDurability, Is.EqualTo(10));
      Assert.That(damagedStack.CurrentStackCount, Is.Zero);
    }

    [Test]
    public void TwoDamagedStacksDoNotMerge()
    {
      var first = new Plaster { CurrentStackCount = 2, CurrentDurability = 10 };
      var second = new Plaster { CurrentStackCount = 2, CurrentDurability = 20 };

      Assert.That(first.CanStackWith(second), Is.False);
      first.Merge(second);

      Assert.That(first.CurrentStackCount, Is.EqualTo(2));
      Assert.That(first.CurrentDurability, Is.EqualTo(10));
      Assert.That(second.CurrentStackCount, Is.EqualTo(2));
      Assert.That(second.CurrentDurability, Is.EqualTo(20));
    }

    [Test]
    public void RejectedPatientItemUseRestoresDurabilityAndStack()
    {
      var playerObject = new GameObject("patient-item-refund-player");
      try
      {
        var player = playerObject.AddComponent<PlayerController>();
        AttachNetworkObjectCache(player);
        var plaster = new Plaster
        {
          CurrentStackCount = 2,
          CurrentDurability = 10
        };
        var slot = new InventorySlotModelDTO(plaster);
        typeof(PlayerController).GetField("_slots", BindingFlags.Instance | BindingFlags.NonPublic)
          ?.SetValue(player, new List<InventorySlotModelDTO> { slot });

        Assert.That(player.TryConsumeItemUse(
          Plaster.Identifier, out var receipt), Is.True);
        Assert.That(slot.ItemInstance.CurrentDurability, Is.EqualTo(9));

        player.CompleteConsumedItemUse(receipt, accepted: false);

        Assert.That(slot.ItemInstance, Is.Not.Null);
        Assert.That(slot.ItemInstance.CurrentStackCount, Is.EqualTo(2));
        Assert.That(slot.ItemInstance.CurrentDurability, Is.EqualTo(10));
      }
      finally
      {
        Object.DestroyImmediate(playerObject);
      }
    }

    [Test]
    public void AcceptedPatientItemUseKeepsConsumedDurability()
    {
      var playerObject = new GameObject("patient-item-accepted-player");
      try
      {
        var player = playerObject.AddComponent<PlayerController>();
        AttachNetworkObjectCache(player);
        var plaster = new Plaster { CurrentDurability = 10 };
        var slot = new InventorySlotModelDTO(plaster);
        typeof(PlayerController).GetField("_slots", BindingFlags.Instance | BindingFlags.NonPublic)
          ?.SetValue(player, new List<InventorySlotModelDTO> { slot });

        Assert.That(player.TryConsumeItemUse(
          Plaster.Identifier, out var receipt), Is.True);
        player.CompleteConsumedItemUse(receipt, accepted: true);

        Assert.That(slot.ItemInstance.CurrentDurability, Is.EqualTo(9));
      }
      finally
      {
        Object.DestroyImmediate(playerObject);
      }
    }

    [Test]
    public void SplittingStackMovesDamagedActiveItemWithoutDuplicatingIt()
    {
      var source = new InventorySlotModelDTO(
        new Plaster { CurrentStackCount = 3, CurrentDurability = 10 });

      var split = source.Pop(1);

      Assert.That(split, Is.Not.Null);
      Assert.That(split.CurrentStackCount, Is.EqualTo(1));
      Assert.That(split.CurrentDurability, Is.EqualTo(10));
      Assert.That(source.ItemInstance.CurrentStackCount, Is.EqualTo(2));
      Assert.That(source.ItemInstance.CurrentDurability, Is.EqualTo(30));
    }

    private static void AttachNetworkObjectCache(PlayerController player)
    {
      typeof(NetworkBehaviour).GetField("_networkObjectCache",
          BindingFlags.Instance | BindingFlags.NonPublic)
        ?.SetValue(player, player.GetComponent<NetworkObject>());
    }

    [TestCase(1.00f, 72, 199, 71)]
    [TestCase(0.75f, 72, 199, 71)]
    [TestCase(0.50f, 205, 220, 57)]
    [TestCase(0.25f, 255, 167, 38)]
    [TestCase(0.24f, 239, 83, 80)]
    public void DurabilityBarUsesFourColorBands(float ratio, int red, int green, int blue)
    {
      var expected = new Color32((byte)red, (byte)green, (byte)blue, 255);
      Assert.That(ItemDurabilityBar.GetColor(ratio), Is.EqualTo((Color)expected));
    }
  }
}
