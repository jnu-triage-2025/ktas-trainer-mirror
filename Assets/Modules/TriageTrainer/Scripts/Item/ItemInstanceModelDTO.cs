using System;

[Serializable]
public class ItemInstanceModelDTO
{
  #region Properties
  public string identifier;
  public string displayName;
  public string itemTextureIdentifier;
  public int currCount;
  public int maxCount;
  public bool hasDurability;
  public int currentDurability;
  #endregion

  #region Constructors
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
    maxCount = other.maxCount;
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

  #endregion

  #region Basic Operations
  /// <summary>
  /// ItemInstanceModelDTO other의 현재 아이템 개수를 더합니다.
  /// 만약 합칠 수 있다면 other는 this에게 가능한 한 최대한 자신의 현재 아이템 스택 개수(currCount)를 넘깁니다.
  /// this의 currCount는 증가하는 방향으로 other의 currCount는 감소하는 방향으로 처리합니다.
  /// 
  /// 이 함수는 항상 other를 반환합니다. other의 currCount는 이 처리의 결과로서 변화되는
  /// 현재 아이템 스택 개수를 갖습니다.
  /// 
  /// - 아이템이 다른 경우: 더하기 불가, 입력된 other 그대로 반환
  /// - 아이템이 같고, 모든 아이템 개수를 this에 더할 수 있는 경우:
  ///     this에 모든 아이템을 덧셈; other의 아이템 개수는 0
  /// - 아이템이 같지만, 최대 아이템 개수 상한으로 this에 일부만 더할 수 있는 경우:
  ///     this에 가능한 모든 아이템을 덧셈; other의 아이템 개수는 남은 아이템 개수
  /// </summary>
  /// <param name="other"></param>
  /// <returns></returns>
  public ItemInstanceModelDTO Merge(ItemInstanceModelDTO other)
  {
    if (CanStackWith(other))
    {
      int diff = Math.Min(maxCount, currCount + other.currCount) - currCount;
      currCount += diff;
      other.currCount -= diff;
    }
    return other;
  }

  /// <summary>
  /// 아이템 개수를 증가시킵니다.
  /// </summary>
  /// <param name="count"></param>
  /// <param name="ignoreStackOverflow">
  /// (권장되지 않음) 이 값을 참으로 설정하면 최대 스택 개수를 고려하지 않고 개수를 증가시킵니다.
  /// </param>
  /// <returns></returns>
  public ItemInstanceModelDTO Add
  (
    int count,
    bool ignoreStackOverflow = false
  )
  {
    if (ignoreStackOverflow) currCount += count;
    else currCount = Math.Min(maxCount, currCount + count);
    return this;
  }

  /// <summary>
  /// 아이템 개수를 감소시킵니다.
  /// </summary>
  /// <param name="count"></param>
  /// <param name="ignoreStackUnderflow">
  /// (권장되지 않음) 이 값을 참으로 설정하면 아이템 값이 음수가 되는 것을 막지 않고 개수를 감소시킵니다.
  /// </param>
  /// <returns></returns>
  public ItemInstanceModelDTO Sub
  (
    int count,
    bool ignoreStackUnderflow = false
  )
  {
    if (ignoreStackUnderflow) currCount -= count;
    else currCount = Math.Max(0, currCount - count);
    return this;
  }

  /// <summary>
  /// 아이템 개수를 곱셈합니다..
  /// </summary>
  /// <param name="count"></param>
  /// <param name="ignoreStackOverflow">
  /// (권장되지 않음) 이 값을 참으로 설정하면 최대 스택 개수를 고려하지 않고 개수를 증가시킵니다.
  /// </param>
  /// <returns></returns>
  public ItemInstanceModelDTO Mul
  (
    int count,
    bool ignoreStackOverflow = false
  )
  {
    if (ignoreStackOverflow) currCount *= count;
    else currCount = Math.Max(maxCount, currCount * count);
    return this;
  }
  
  /// <summary>
  /// 아이템 개수를 정수 나눗셈 합니다.
  /// 이 처리에서는 소수점에 대해서 버리는 연산이 이루어집니다.
  /// </summary>
  /// <param name="count"></param>
  /// </param>
  /// <returns></returns>
  public ItemInstanceModelDTO Div
  (
    int count
  )
  {
    currCount /= count;
    return this;
  }

  /// <summary>
  /// 아이템 개수를 정수 나눗셈의 나머지로 설정합니다.
  /// </summary>
  /// <param name="count"></param>
  /// </param>
  /// <returns></returns>
  public ItemInstanceModelDTO Rem
  (
    int count
  )
  {
    currCount %= count;
    return this;
  }

  public static ItemInstanceModelDTO operator +(ItemInstanceModelDTO operand) => operand;
  public static ItemInstanceModelDTO operator -(ItemInstanceModelDTO operand)
    => new ItemInstanceModelDTO
    (
      operand.identifier,
      operand.displayName,
      -operand.currCount,
      operand.maxCount,
      operand.hasDurability,
      operand.currentDurability,
      operand.itemTextureIdentifier
    );
  
  public static ItemInstanceModelDTO operator +(ItemInstanceModelDTO left, int right)
  => new ItemInstanceModelDTO
    (
      left.identifier,
      left.displayName,
      left.currCount + right,
      left.maxCount,
      left.hasDurability,
      left.currentDurability,
      left.itemTextureIdentifier
    );

  public static ItemInstanceModelDTO operator -(ItemInstanceModelDTO left, int right)
  => new ItemInstanceModelDTO
    (
      left.identifier,
      left.displayName,
      left.currCount - right,
      left.maxCount,
      left.hasDurability,
      left.currentDurability,
      left.itemTextureIdentifier
    );
  
  public static ItemInstanceModelDTO operator *(ItemInstanceModelDTO left, int right)
  => new ItemInstanceModelDTO
    (
      left.identifier,
      left.displayName,
      left.currCount * right,
      left.maxCount,
      left.hasDurability,
      left.currentDurability,
      left.itemTextureIdentifier
    );

  public static ItemInstanceModelDTO operator /(ItemInstanceModelDTO left, int right)
  => new ItemInstanceModelDTO
    (
      left.identifier,
      left.displayName,
      left.currCount / right,
      left.maxCount,
      left.hasDurability,
      left.currentDurability,
      left.itemTextureIdentifier
    );

  public static ItemInstanceModelDTO operator %(ItemInstanceModelDTO left, int right)
  => new ItemInstanceModelDTO
    (
      left.identifier,
      left.displayName,
      left.currCount % right,
      left.maxCount,
      left.hasDurability,
      left.currentDurability,
      left.itemTextureIdentifier
    );

  public static ItemInstanceModelDTO operator ++(ItemInstanceModelDTO operand)
    => new ItemInstanceModelDTO
    (
      operand.identifier,
      operand.displayName,
      operand.currCount + 1,
      operand.maxCount,
      operand.hasDurability,
      operand.currentDurability,
      operand.itemTextureIdentifier
    );

  public static ItemInstanceModelDTO operator --(ItemInstanceModelDTO operand)
    => new ItemInstanceModelDTO
    (
      operand.identifier,
      operand.displayName,
      operand.currCount - 1,
      operand.maxCount,
      operand.hasDurability,
      operand.currentDurability,
      operand.itemTextureIdentifier
    );
  #endregion


  public bool IsValid()
  {
    return !string.IsNullOrEmpty(identifier) && currCount >= 0 && (!hasDurability || currentDurability >= 0);
  }

  public ItemInstanceModelDTO Clone() => new(this);

  public bool CanStackWith(ItemInstanceModelDTO other)
  {
    if (other == null) return false;
    return identifier == other.identifier;
  }

  public void ApplyRestriction()
  {
    currCount = Math.Max(0, Math.Min(maxCount, currCount));
  }
}
