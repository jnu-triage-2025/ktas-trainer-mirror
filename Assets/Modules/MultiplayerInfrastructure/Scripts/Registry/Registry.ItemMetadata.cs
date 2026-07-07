namespace MultiplayerInfrastructure.Registry
{
  public static partial class Registry
  {
    // =========================================================================
    // Item metadata helpers
    // =========================================================================

    /// <summary>
    /// identifier에 해당하는 아이템의 표시 이름을 반환합니다.
    ///
    /// Item.DisplayName 은 인스턴스 프로퍼티이므로, 이 헬퍼는 내부적으로
    /// <see cref="CreateItemInstance(string)"/> 로 임시 인스턴스를 생성해 이름만 읽어온다.
    /// 등록되지 않은 identifier 이면 identifier 자체를 그대로 반환한다(표시용 폴백).
    /// </summary>
    public static string GetItemDisplayName(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return string.Empty;

      var item = CreateItemInstance(identifier);
      if (item == null)
        return identifier;

      string name = item.CurrentDisplayName;
      return string.IsNullOrWhiteSpace(name) ? identifier : name;
    }
  }
}
