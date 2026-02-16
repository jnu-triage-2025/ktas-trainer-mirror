using System;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.Player;
using UnityEngine;

namespace MultiplayerInfrastructure.Item
{
  [Serializable]
  public class ItemData
  {
    #region Properties
    public string identifier;
    public string displayName;

    [SerializeField] private string _itemTextureIdentifier;
    public string ItemTextureIdentifier
    {
      get => _itemTextureIdentifier;
      set => _itemTextureIdentifier = value;
    }

    public int currCount;
    public int maxCount;
    public bool hasDurability;
    public int currentDurability;
    #endregion

    [SerializeField] private Sprite _itemTexture;
    public Sprite ItemTexture => _itemTexture != null ? _itemTexture : DefaultsResource.FallbackSprite;
    public bool HasIcon => _itemTexture != null;

    #region Constructors
    public ItemData() { }

    public ItemData
    (
      string identifier,
      string displayName,
      int currCount,
      int maxCount,
      bool hasDurability,
      int currentDurability,
      string itemTextureIdentifier = null,
      Sprite itemTexture = null
    )
    {
      this.identifier = identifier ?? string.Empty;
      this.displayName = displayName ?? string.Empty;
      this.ItemTextureIdentifier = !string.IsNullOrEmpty(itemTextureIdentifier) ? itemTextureIdentifier : this.identifier;
      this.currCount = currCount < 0 ? 0 : currCount;
      this.maxCount = maxCount < 1 ? 1 : maxCount;
      this.hasDurability = hasDurability;
      this.currentDurability = hasDurability ? Math.Max(0, currentDurability) : 0;
      _itemTexture = itemTexture;
    }

    public ItemData(ItemData other)
    {
      if (other == null) return;

      identifier = other.identifier;
      displayName = other.displayName;
      ItemTextureIdentifier = other.ItemTextureIdentifier;
      currCount = other.currCount;
      maxCount = other.maxCount;
      hasDurability = other.hasDurability;
      currentDurability = other.currentDurability;
      _itemTexture = other._itemTexture;
    }

    public ItemData(ItemBaseModelSO baseModel, Sprite itemTexture = null)
    {
      if (baseModel == null) return;

      identifier = baseModel.identifier;
      displayName = baseModel.displayName;
      ItemTextureIdentifier = baseModel.identifier;
      currCount = 1;
      maxCount = baseModel.maxStackCount;
      hasDurability = baseModel.hasDurability;
      currentDurability = baseModel.hasDurability ? baseModel.maxDurability : 0;
      _itemTexture = itemTexture;
    }

    #endregion

    #region Basic Operations
    public ItemData Merge(ItemData other)
    {
      if (CanStackWith(other))
      {
        int diff = Math.Min(maxCount, currCount + other.currCount) - currCount;
        currCount += diff;
        other.currCount -= diff;
      }
      return other;
    }

    public ItemData Add(int count, bool ignoreStackOverflow = false)
    {
      if (ignoreStackOverflow) currCount += count;
      else currCount = Math.Min(maxCount, currCount + count);
      return this;
    }

    public ItemData Sub(int count, bool ignoreStackUnderflow = false)
    {
      if (ignoreStackUnderflow) currCount -= count;
      else currCount = Math.Max(0, currCount - count);
      return this;
    }

    public ItemData Mul(int count, bool ignoreStackOverflow = false)
    {
      if (ignoreStackOverflow) currCount *= count;
      else currCount = Math.Max(maxCount, currCount * count);
      return this;
    }

    public ItemData Div(int count)
    {
      currCount /= count;
      return this;
    }

    public ItemData Rem(int count)
    {
      currCount %= count;
      return this;
    }

    public static ItemData operator +(ItemData operand) => operand;

    public static ItemData operator -(ItemData operand)
      => new ItemData(
        operand.identifier,
        operand.displayName,
        -operand.currCount,
        operand.maxCount,
        operand.hasDurability,
        operand.currentDurability,
        operand.ItemTextureIdentifier,
        operand._itemTexture
      );

    public static ItemData operator +(ItemData left, int right)
      => new ItemData(
        left.identifier,
        left.displayName,
        left.currCount + right,
        left.maxCount,
        left.hasDurability,
        left.currentDurability,
        left.ItemTextureIdentifier,
        left._itemTexture
      );

    public static ItemData operator -(ItemData left, int right)
      => new ItemData(
        left.identifier,
        left.displayName,
        left.currCount - right,
        left.maxCount,
        left.hasDurability,
        left.currentDurability,
        left.ItemTextureIdentifier,
        left._itemTexture
      );

    public static ItemData operator *(ItemData left, int right)
      => new ItemData(
        left.identifier,
        left.displayName,
        left.currCount * right,
        left.maxCount,
        left.hasDurability,
        left.currentDurability,
        left.ItemTextureIdentifier,
        left._itemTexture
      );

    public static ItemData operator /(ItemData left, int right)
      => new ItemData(
        left.identifier,
        left.displayName,
        left.currCount / right,
        left.maxCount,
        left.hasDurability,
        left.currentDurability,
        left.ItemTextureIdentifier,
        left._itemTexture
      );

    public static ItemData operator %(ItemData left, int right)
      => new ItemData(
        left.identifier,
        left.displayName,
        left.currCount % right,
        left.maxCount,
        left.hasDurability,
        left.currentDurability,
        left.ItemTextureIdentifier,
        left._itemTexture
      );

    public static ItemData operator ++(ItemData operand)
      => new ItemData(
        operand.identifier,
        operand.displayName,
        operand.currCount + 1,
        operand.maxCount,
        operand.hasDurability,
        operand.currentDurability,
        operand.ItemTextureIdentifier,
        operand._itemTexture
      );

    public static ItemData operator --(ItemData operand)
      => new ItemData(
        operand.identifier,
        operand.displayName,
        operand.currCount - 1,
        operand.maxCount,
        operand.hasDurability,
        operand.currentDurability,
        operand.ItemTextureIdentifier,
        operand._itemTexture
      );
    #endregion

    public bool IsValid()
    {
      return !string.IsNullOrEmpty(identifier) && currCount >= 0 && (!hasDurability || currentDurability >= 0);
    }

    public ItemData Clone() => new(this);

    public bool CanStackWith(ItemData other)
    {
      if (other == null) return false;
      return identifier == other.identifier;
    }

    public void ApplyRestriction()
    {
      currCount = Math.Max(0, Math.Min(maxCount, currCount));
    }

    public void SetIcon(Sprite icon)
    {
      _itemTexture = icon;
    }

    public ActionResult OnAttack(PlayerController player, Entity.Entity target)
    {
      return ActionResult.Success;
    }

    public ActionResult OnUse(PlayerController player, Entity.Entity target)
    {
      return ActionResult.Success;
    }
  }
}
