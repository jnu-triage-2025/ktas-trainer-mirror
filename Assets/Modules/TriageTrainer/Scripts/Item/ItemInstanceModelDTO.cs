using System;

[Serializable]
public class ItemInstanceModelDTO
{
  public string identifier;
  public string displayName;
  public string itemTextureIdentifier;
  public int currCount;
  public int maxCount;
  public bool hasDurability;
  public int currentDurability;

  public ItemInstanceModelDTO() { }

  public ItemInstanceModelDTO
  (
    string identifier,
    string displayName,
    int currCount,
    int maxCount,
    bool hasDurability,
    int currentDurability,
    string itemTextureIdentifier = null
  )
  {
    this.identifier = identifier ?? string.Empty;
    this.displayName = displayName ?? string.Empty;
    this.itemTextureIdentifier = !string.IsNullOrEmpty(itemTextureIdentifier) ? itemTextureIdentifier : this.identifier;
    this.currCount = currCount < 0 ? 0 : currCount;
    this.maxCount = maxCount < 1 ? 1 : maxCount;
    this.hasDurability = hasDurability;
    this.currentDurability = hasDurability ? Math.Max(0, currentDurability) : 0;
  }

  public ItemInstanceModelDTO(ItemInstanceModelDTO other)
  {
    if (other == null) return;

    identifier = other.identifier;
    displayName = other.displayName;
    itemTextureIdentifier = other.itemTextureIdentifier;
    currCount = other.currCount;
    hasDurability = other.hasDurability;
    currentDurability = other.currentDurability;
  }

  public ItemInstanceModelDTO(ItemBaseModelSO baseModel)
  {
    if (baseModel == null) return;

    identifier = baseModel.identifier;
    displayName = baseModel.displayName;
    itemTextureIdentifier = baseModel.identifier;
    currCount = 1;
    maxCount = baseModel.maxStackCount;
    hasDurability = baseModel.hasDurability;
    currentDurability = baseModel.hasDurability ? baseModel.maxDurability : 0;
  }

  public bool IsValid()
  {
    return !string.IsNullOrEmpty(identifier) && currCount >= 0 && (!hasDurability || currentDurability >= 0);
  }

  public bool CanStackWith(ItemInstanceModelDTO other)
  {
    if (other == null) return false;
    return identifier == other.identifier;
  }
}
