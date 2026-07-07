using System;

namespace MultiplayerInfrastructure.InteractableEntity
{
  /// <summary>
  /// 아이템 제출에서 요구되는 단일 아이템을 식별자와 수량으로 표현한다.
  /// 인벤토리 아이템은 <c>CurrentIdentifier</c> 와 <c>CurrentStackCount</c> 로 식별되므로,
  /// 이 구조체는 그와 동일한 (identifier, count) 쌍으로 요구 사항을 정의한다.
  /// </summary>
  [Serializable]
  public struct ItemRequirement
  {
    public string identifier;
    public int count;

    public ItemRequirement(string identifier, int count)
    {
      this.identifier = identifier;
      this.count = count <= 0 ? 1 : count;
    }

    public bool IsValid => !string.IsNullOrWhiteSpace(identifier) && count > 0;
  }
}
