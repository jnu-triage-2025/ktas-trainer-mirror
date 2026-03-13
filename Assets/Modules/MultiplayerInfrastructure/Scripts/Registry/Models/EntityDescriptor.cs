using System;
using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  /// <summary>
  /// Registry.Entity 저장소에 등록되는 단일 엔티티 설명자입니다.
  ///
  /// - Identifier: 서버/모든 클라이언트에서 동일해야 하는 전역 고유 식별자
  /// - EntityType: 엔티티 종류
  /// - GameObject: 로컬 프로세스에서 이 엔티티를 나타내는 실제 게임 오브젝트
  /// - OwnerUserIdentifier / ClientId: 플레이어 엔티티일 때 연결 정보
  /// </summary>
  [Serializable]
  public sealed class EntityDescriptor
  {
    public string Identifier { get; }
    public EntityType EntityType { get; }
    public GameObject GameObject { get; }
    public string DisplayName { get; set; }
    public string OwnerUserIdentifier { get; }
    public int? ClientId { get; }
    public bool IsNetworked { get; }

    public EntityDescriptor(
      string identifier,
      EntityType entityType,
      GameObject gameObject,
      string displayName = null,
      string ownerUserIdentifier = null,
      int? clientId = null,
      bool isNetworked = false)
    {
      Identifier = identifier;
      EntityType = entityType;
      GameObject = gameObject;
      DisplayName = displayName;
      OwnerUserIdentifier = ownerUserIdentifier;
      ClientId = clientId;
      IsNetworked = isNetworked;
    }

    public override string ToString()
      => $"{EntityType}: {DisplayName ?? GameObject?.name ?? Identifier} ({Identifier})";
  }
}
