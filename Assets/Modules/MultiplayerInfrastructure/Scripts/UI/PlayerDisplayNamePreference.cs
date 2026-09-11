using PlayerPrefs = MultiplayerInfrastructure.Automation.ProfilePlayerPrefs;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// 시작 화면에서 입력한 표시 이름을 이 기기에 저장하고 읽는 설정 항목입니다.
  ///
  /// 저장과 삭제만 맡습니다. 실행 중인 세션이 쓰는 이름은 Registry의 런타임 상태가 들고 있으므로,
  /// 저장값을 지워도 이미 접속한 세션의 이름은 바뀌지 않습니다.
  /// </summary>
  public static class PlayerDisplayNamePreference
  {
    public const string PlayerPrefsKey = "IntroScene.PlayerName";

    /// <summary>저장된 이름을 돌려줍니다. 없으면 빈 문자열입니다.</summary>
    public static string Read()
      => PlayerPrefs.GetString(PlayerPrefsKey, string.Empty).Trim();

    /// <summary>이름을 저장합니다. 비어 있으면 저장된 값을 지웁니다.</summary>
    public static void Persist(string value)
    {
      var name = value?.Trim();
      if (string.IsNullOrWhiteSpace(name))
      {
        Clear();
        return;
      }

      PlayerPrefs.SetString(PlayerPrefsKey, name);
      PlayerPrefs.Save();
    }

    /// <summary>저장된 이름을 지웁니다.</summary>
    public static void Clear()
    {
      PlayerPrefs.DeleteKey(PlayerPrefsKey);
      PlayerPrefs.Save();
    }
  }
}
