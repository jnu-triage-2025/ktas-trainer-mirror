using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// 키 바인딩 설정을 PlayerPrefs를 통해 로컬에 저장하고 불러오는 저장소입니다.
  ///
  /// 저장 키 형식: "KeyBinding_{actionId}" → int(KeyCode)
  /// </summary>
  public static class KeyBindingRepository
  {
    private const string PrefsKeyPrefix = "KeyBinding_";

    // ──────────────────────────────────────────────────────────────────────────
    // 저장
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 모든 바인딩을 PlayerPrefs에 저장합니다.
    /// </summary>
    public static void SaveAll(IReadOnlyList<KeyBindingEntry> bindings)
    {
      if (bindings == null)
        return;
      foreach (var entry in bindings)
        SaveEntry(entry);
      PlayerPrefs.Save();
    }

    /// <summary>
    /// 단일 바인딩을 PlayerPrefs에 저장합니다. PlayerPrefs.Save()를 즉시 플러시하지 않으므로,
    /// 배치 저장 후에는 <see cref="Flush"/>를 호출하거나 <see cref="SaveAll"/>을 사용하세요.
    /// </summary>
    public static void SaveEntry(KeyBindingEntry entry)
    {
      if (entry == null || string.IsNullOrEmpty(entry.actionId))
        return;
      PlayerPrefs.SetInt(PrefsKeyPrefix + entry.actionId, (int)entry.boundKey);
    }

    /// <summary>PlayerPrefs.Save()를 명시적으로 호출합니다.</summary>
    public static void Flush() => PlayerPrefs.Save();

    // ──────────────────────────────────────────────────────────────────────────
    // 불러오기
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 저장된 키 바인딩으로 <paramref name="bindings"/> 리스트의 <see cref="KeyBindingEntry.boundKey"/>를
    /// 덮어씁니다. 저장된 값이 없는 항목은 기본값을 유지합니다.
    /// </summary>
    /// <returns>하나라도 덮어쓴 항목이 있으면 true.</returns>
    public static bool LoadInto(IList<KeyBindingEntry> bindings)
    {
      if (bindings == null)
        return false;
      bool anyLoaded = false;
      foreach (var entry in bindings)
      {
        if (string.IsNullOrEmpty(entry.actionId))
          continue;
        string prefsKey = PrefsKeyPrefix + entry.actionId;
        if (!PlayerPrefs.HasKey(prefsKey))
          continue;
        entry.boundKey = (KeyCode)PlayerPrefs.GetInt(prefsKey);
        anyLoaded = true;
      }
      return anyLoaded;
    }

    /// <summary>
    /// 설정 화면과 실제 게임 입력이 같은 PlayerPrefs 바인딩을 사용하도록 현재 키를 조회한다.
    /// 아직 저장된 값이 없으면 <paramref name="defaultKey"/>를 사용한다.
    /// </summary>
    public static KeyCode GetBoundKey(string actionId, KeyCode defaultKey)
    {
      if (string.IsNullOrEmpty(actionId))
        return defaultKey;

      string prefsKey = PrefsKeyPrefix + actionId;
      return PlayerPrefs.HasKey(prefsKey)
        ? (KeyCode)PlayerPrefs.GetInt(prefsKey)
        : defaultKey;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 초기화
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 저장된 모든 키 바인딩을 PlayerPrefs에서 삭제합니다.
    /// </summary>
    public static void DeleteAll(IReadOnlyList<KeyBindingEntry> bindings)
    {
      if (bindings == null)
        return;
      foreach (var entry in bindings)
      {
        if (!string.IsNullOrEmpty(entry.actionId))
          PlayerPrefs.DeleteKey(PrefsKeyPrefix + entry.actionId);
      }
      PlayerPrefs.Save();
    }
  }
}
