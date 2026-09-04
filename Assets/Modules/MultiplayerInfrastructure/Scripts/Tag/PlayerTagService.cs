using System.Collections.Generic;
using FishNet;
using MultiplayerInfrastructure.Player;
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
    /// <summary>
    /// 서버에서 태그가 실제로 추가되었을 때 (플레이어 식별자, 태그) 를 전달한다.
    /// 재접속한 참가자에게 역할을 다시 부여하는 순간을 관찰해 그 역할의 진행 상태를 복원하는 데 쓴다.
    /// </summary>
    public static event System.Action<string, string> TagAdded;

    private static bool IsServerMutationAllowed()
    {
      if (InstanceFinder.IsServerStarted)
      {
        return true;
      }

      Debug.LogWarning("[PlayerTagService] Tag mutation attempted outside server context. Ignored.");
      return false;
    }

    // ── 내부 헬퍼 ────────────────────────────────────────────────────────

    private static List<string> GetOrCreateTagList(string identifier)
    {
      if (Registry.Registry.TryGet<List<string>>(RegistryType.PlayerTag, identifier, out var existing))
        return existing;

      var list = new List<string>();
      Registry.Registry.Register(RegistryType.PlayerTag, identifier, list);
      return list;
    }

    private static void SyncOwnerPlayerTags(string uuid)
    {
      if (string.IsNullOrWhiteSpace(uuid))
      {
        return;
      }

      if (!Registry.Registry.TryGetEntityByOwnerUserIdentifier(uuid, out var descriptor))
      {
        return;
      }

      var playerObject = descriptor?.GameObject;
      if (playerObject == null || !playerObject.TryGetComponent<PlayerController>(out var controller) || controller == null)
      {
        return;
      }

      controller.SyncPlayerTagsToObservers();
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// 플레이어에게 태그를 추가합니다. 이미 존재하는 태그는 무시합니다.
    /// </summary>
    public static void AddTag(string uuid, string tag)
      => AddTagToIdentifier(uuid, tag);

    public static void AddTagToIdentifier(string identifier, string tag)
    {
      if (!IsServerMutationAllowed())
        return;

      if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(tag))
        return;

      var tags = GetOrCreateTagList(identifier);
      if (!tags.Contains(tag))
      {
        tags.Add(tag);
        SyncOwnerPlayerTags(identifier);
        TagAdded?.Invoke(identifier, tag);
      }
    }

    /// <summary>
    /// 플레이어에게서 태그를 제거합니다.
    /// </summary>
    /// <returns>태그가 실제로 제거되었으면 true, 없었으면 false.</returns>
    public static bool RemoveTag(string uuid, string tag)
      => RemoveTagFromIdentifier(uuid, tag);

    public static bool RemoveTagFromIdentifier(string identifier, string tag)
    {
      if (!IsServerMutationAllowed())
        return false;

      if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(tag))
        return false;

      var tags = GetOrCreateTagList(identifier);
      bool removed = tags.Remove(tag);
      if (removed)
      {
        SyncOwnerPlayerTags(identifier);
      }

      return removed;
    }

    /// <summary>
    /// 플레이어의 'fromTag'를 'toTag'로 교체합니다.
    /// </summary>
    /// <returns>교체에 성공했으면 true, fromTag가 없었으면 false.</returns>
    public static bool ChangeTag(string uuid, string fromTag, string toTag)
      => ChangeTagForIdentifier(uuid, fromTag, toTag);

    public static bool ChangeTagForIdentifier(string identifier, string fromTag, string toTag)
    {
      if (!IsServerMutationAllowed())
        return false;

      if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(fromTag) || string.IsNullOrWhiteSpace(toTag))
        return false;

      var tags = GetOrCreateTagList(identifier);
      int idx = tags.IndexOf(fromTag);
      if (idx < 0)
        return false;

      tags[idx] = toTag;
      SyncOwnerPlayerTags(identifier);
      return true;
    }

    /// <summary>
    /// 플레이어 태그 저장소를 완전히 제거합니다.
    /// 주로 연결 종료/디스폰 시 정리 용도로 사용합니다.
    /// </summary>
    public static void ClearTags(string uuid)
    {
      if (string.IsNullOrWhiteSpace(uuid))
      {
        return;
      }

      Registry.Registry.Unregister(RegistryType.PlayerTag, uuid);
    }

    /// <summary>
    /// 네트워크 동기화된 태그 스냅샷으로 로컬 태그 목록을 교체합니다.
    /// 서버/클라이언트 공용으로 사용됩니다.
    /// </summary>
    public static void ReplaceTags(string uuid, IReadOnlyList<string> tags)
    {
      if (string.IsNullOrWhiteSpace(uuid))
      {
        return;
      }

      var current = GetOrCreateTagList(uuid);
      current.Clear();

      if (tags == null)
      {
        return;
      }

      for (int i = 0; i < tags.Count; i++)
      {
        var value = tags[i];
        if (string.IsNullOrWhiteSpace(value) || current.Contains(value))
        {
          continue;
        }

        current.Add(value);
      }
    }

    /// <summary>
    /// 플레이어의 태그 목록을 읽기 전용으로 반환합니다. 없으면 빈 리스트.
    /// </summary>
    public static IReadOnlyList<string> GetTags(string uuid)
      => GetTagsByIdentifier(uuid);

    public static IReadOnlyList<string> GetTagsByIdentifier(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return System.Array.Empty<string>();

      if (Registry.Registry.TryGet<List<string>>(RegistryType.PlayerTag, identifier, out var tags))
        return tags;

      return System.Array.Empty<string>();
    }

    /// <summary>
    /// 플레이어가 특정 태그를 보유하고 있는지 확인합니다.
    /// </summary>
    public static bool HasTag(string uuid, string tag)
      => HasTagOnIdentifier(uuid, tag);

    public static bool HasTagOnIdentifier(string identifier, string tag)
    {
      if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(tag))
        return false;

      if (Registry.Registry.TryGet<List<string>>(RegistryType.PlayerTag, identifier, out var tags))
        return tags.Contains(tag);

      return false;
    }
  }
}
