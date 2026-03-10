using System.Collections.Generic;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace MultiplayerInfrastructure.Tag
{
  /// <summary>
  /// 플레이어 태그를 관리하는 정적 서비스.
  /// 태그는 PlayerTag 레지스트리에 UserDescriptor.Identifier(UUID)를 키로 저장됩니다.
  /// 모든 메서드는 서버 측에서 호출되어야 합니다.
  /// </summary>
  public static class PlayerTagService
  {
    // ── 내부 헬퍼 ────────────────────────────────────────────────────────

    private static List<string> GetOrCreateTagList(string uuid)
    {
      if (Registry.Registry.TryGet<List<string>>(RegistryType.PlayerTag, uuid, out var existing))
        return existing;

      var list = new List<string>();
      Registry.Registry.Register(RegistryType.PlayerTag, uuid, list);
      return list;
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// 플레이어에게 태그를 추가합니다. 이미 존재하는 태그는 무시합니다.
    /// </summary>
    public static void AddTag(string uuid, string tag)
    {
      if (string.IsNullOrWhiteSpace(uuid) || string.IsNullOrWhiteSpace(tag))
        return;

      var tags = GetOrCreateTagList(uuid);
      if (!tags.Contains(tag))
        tags.Add(tag);
    }

    /// <summary>
    /// 플레이어에게서 태그를 제거합니다.
    /// </summary>
    /// <returns>태그가 실제로 제거되었으면 true, 없었으면 false.</returns>
    public static bool RemoveTag(string uuid, string tag)
    {
      if (string.IsNullOrWhiteSpace(uuid) || string.IsNullOrWhiteSpace(tag))
        return false;

      var tags = GetOrCreateTagList(uuid);
      return tags.Remove(tag);
    }

    /// <summary>
    /// 플레이어의 'fromTag'를 'toTag'로 교체합니다.
    /// </summary>
    /// <returns>교체에 성공했으면 true, fromTag가 없었으면 false.</returns>
    public static bool ChangeTag(string uuid, string fromTag, string toTag)
    {
      if (string.IsNullOrWhiteSpace(uuid) || string.IsNullOrWhiteSpace(fromTag) || string.IsNullOrWhiteSpace(toTag))
        return false;

      var tags = GetOrCreateTagList(uuid);
      int idx = tags.IndexOf(fromTag);
      if (idx < 0)
        return false;

      tags[idx] = toTag;
      return true;
    }

    /// <summary>
    /// 플레이어의 태그 목록을 읽기 전용으로 반환합니다. 없으면 빈 리스트.
    /// </summary>
    public static IReadOnlyList<string> GetTags(string uuid)
    {
      if (string.IsNullOrWhiteSpace(uuid))
        return System.Array.Empty<string>();

      if (Registry.Registry.TryGet<List<string>>(RegistryType.PlayerTag, uuid, out var tags))
        return tags;

      return System.Array.Empty<string>();
    }

    /// <summary>
    /// 플레이어가 특정 태그를 보유하고 있는지 확인합니다.
    /// </summary>
    public static bool HasTag(string uuid, string tag)
    {
      if (string.IsNullOrWhiteSpace(uuid) || string.IsNullOrWhiteSpace(tag))
        return false;

      if (Registry.Registry.TryGet<List<string>>(RegistryType.PlayerTag, uuid, out var tags))
        return tags.Contains(tag);

      return false;
    }
  }
}
