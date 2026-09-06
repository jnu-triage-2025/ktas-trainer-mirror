using System;
using System.Collections.Generic;

namespace MultiplayerInfrastructure.InteractableEntity
{
  public enum InteractionVisibilityOverride : byte
  {
    Reset = 0,
    Show = 1,
    Hide = 2
  }

  public enum InteractionVisibilityScope : byte
  {
    Global = 0,
    Player = 1
  }

  /// <summary>
  /// 트리거 방식과 수행 뒤 처리(afterInteract)가 기록하는 가시성 오버라이드 저장소.
  /// 전역 층과 플레이어별 층을 갖는다. 변경은 서버 권위이며 <see cref="Scenario.ScenarioNetworkRelay"/> 가
  /// 미러링과 늦은 접속 스냅샷을 담당한다. 이 클래스는 로컬 저장만 한다.
  /// </summary>
  public static class InteractionVisibilityState
  {
    private static readonly Dictionary<string, bool> Global = new Dictionary<string, bool>(StringComparer.Ordinal);
    private static readonly Dictionary<string, Dictionary<string, bool>> ByPlayer =
      new Dictionary<string, Dictionary<string, bool>>(StringComparer.Ordinal);

    /// <summary>오버라이드가 바뀔 때 (주소 키, 플레이어 식별자 또는 null) 를 전달한다.</summary>
    public static event Action<string, string> Changed;

    public static bool TryGetOverride(string addressKey, string playerIdentifier, out bool visible)
    {
      visible = false;
      if (string.IsNullOrWhiteSpace(addressKey))
        return false;

      if (!string.IsNullOrWhiteSpace(playerIdentifier)
          && ByPlayer.TryGetValue(playerIdentifier, out var overrides)
          && overrides.TryGetValue(addressKey, out visible))
        return true;

      return Global.TryGetValue(addressKey, out visible);
    }

    /// <summary>권한 검사 없이 로컬 저장소에 적용한다. 서버 또는 미러 수신부에서만 호출한다.</summary>
    public static void ApplyLocal(string addressKey, InteractionVisibilityScope scope, string playerIdentifier,
      InteractionVisibilityOverride value)
    {
      if (string.IsNullOrWhiteSpace(addressKey))
        return;

      bool changed;
      if (scope == InteractionVisibilityScope.Player)
      {
        if (string.IsNullOrWhiteSpace(playerIdentifier))
          return;
        if (!ByPlayer.TryGetValue(playerIdentifier, out var overrides))
        {
          if (value == InteractionVisibilityOverride.Reset)
            return;
          overrides = new Dictionary<string, bool>(StringComparer.Ordinal);
          ByPlayer.Add(playerIdentifier, overrides);
        }
        changed = Apply(overrides, addressKey, value);
        if (overrides.Count == 0)
          ByPlayer.Remove(playerIdentifier);
      }
      else
      {
        changed = Apply(Global, addressKey, value);
        playerIdentifier = null;
      }

      if (changed)
        Changed?.Invoke(addressKey, playerIdentifier);
    }

    private static bool Apply(Dictionary<string, bool> store, string addressKey, InteractionVisibilityOverride value)
    {
      if (value == InteractionVisibilityOverride.Reset)
        return store.Remove(addressKey);

      bool visible = value == InteractionVisibilityOverride.Show;
      if (store.TryGetValue(addressKey, out bool current) && current == visible)
        return false;
      store[addressKey] = visible;
      return true;
    }

    /// <summary>모든 오버라이드를 지운다. 시나리오 시작·종료·재시작에서 호출한다.</summary>
    public static void ClearAll()
    {
      bool had = Global.Count > 0 || ByPlayer.Count > 0;
      Global.Clear();
      ByPlayer.Clear();
      if (had)
        Changed?.Invoke(null, null);
    }

    /// <summary>플레이어 한 명의 오버라이드를 지운다. 접속 종료 정리 용도.</summary>
    public static void ClearPlayer(string playerIdentifier)
    {
      if (!string.IsNullOrWhiteSpace(playerIdentifier) && ByPlayer.Remove(playerIdentifier))
        Changed?.Invoke(null, playerIdentifier);
    }

    /// <summary>
    /// 복제용 스냅샷. 각 항목은 "scope|player|state|addressKey" 형식이다. 주소 키에 '|' 가 없다는 전제이며,
    /// 주소 키는 마지막 자리에 두어 '/' 를 포함해도 안전하게 해석된다.
    /// </summary>
    public static string[] Snapshot()
    {
      var entries = new List<string>();
      foreach (var pair in Global)
        entries.Add(Encode(InteractionVisibilityScope.Global, null, pair.Value, pair.Key));
      foreach (var playerPair in ByPlayer)
      {
        foreach (var pair in playerPair.Value)
          entries.Add(Encode(InteractionVisibilityScope.Player, playerPair.Key, pair.Value, pair.Key));
      }
      return entries.ToArray();
    }

    /// <summary>스냅샷을 로컬 저장소에 통째로 적용한다(기존 값은 지운다).</summary>
    public static void ApplySnapshot(string[] entries)
    {
      Global.Clear();
      ByPlayer.Clear();
      if (entries != null)
      {
        for (int i = 0; i < entries.Length; i++)
        {
          if (!TryDecode(entries[i], out var scope, out var player, out var value, out var addressKey))
            continue;
          if (scope == InteractionVisibilityScope.Player)
          {
            if (!ByPlayer.TryGetValue(player, out var overrides))
            {
              overrides = new Dictionary<string, bool>(StringComparer.Ordinal);
              ByPlayer.Add(player, overrides);
            }
            overrides[addressKey] = value == InteractionVisibilityOverride.Show;
          }
          else
          {
            Global[addressKey] = value == InteractionVisibilityOverride.Show;
          }
        }
      }
      Changed?.Invoke(null, null);
    }

    public static string Encode(InteractionVisibilityScope scope, string playerIdentifier, bool visible, string addressKey)
      => $"{(byte)scope}|{playerIdentifier ?? string.Empty}|{(visible ? (byte)InteractionVisibilityOverride.Show : (byte)InteractionVisibilityOverride.Hide)}|{addressKey}";

    public static bool TryDecode(string entry, out InteractionVisibilityScope scope, out string playerIdentifier,
      out InteractionVisibilityOverride value, out string addressKey)
    {
      scope = InteractionVisibilityScope.Global;
      playerIdentifier = null;
      value = InteractionVisibilityOverride.Reset;
      addressKey = null;
      if (string.IsNullOrWhiteSpace(entry))
        return false;
      var parts = entry.Split(new[] { '|' }, 4);
      if (parts.Length != 4)
        return false;
      if (!byte.TryParse(parts[0], out byte scopeByte) || !byte.TryParse(parts[2], out byte valueByte))
        return false;
      scope = (InteractionVisibilityScope)scopeByte;
      playerIdentifier = string.IsNullOrWhiteSpace(parts[1]) ? null : parts[1];
      value = (InteractionVisibilityOverride)valueByte;
      addressKey = parts[3];
      return !string.IsNullOrWhiteSpace(addressKey);
    }

    /// <summary>디버그 인스펙터용 열거.</summary>
    public static IEnumerable<KeyValuePair<string, bool>> GlobalOverrides => Global;
    public static IReadOnlyDictionary<string, Dictionary<string, bool>> PlayerOverrides => ByPlayer;
  }
}
