using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  [Serializable]
  public sealed class EntityPresetDefinition
  {
    public string Identifier { get; }
    public EntityType EntityType { get; }
    public GameObject Prefab { get; }
    public string DisplayName { get; }
    public bool IsNetworked { get; }

    /// <summary>
    /// 스폰 시 루트로 분리할 자식 NetworkObject 목록(프리셋 자체에 내장된 분리 설정).
    /// 비어 있으면 분리 없이 단일 객체로 스폰된다. 시나리오 노드가 별도 분리를 지정하지 않으면 이 값이 사용된다.
    /// </summary>
    public IReadOnlyList<EntityPresetChildDetachment> ChildDetachments { get; }

    public EntityPresetDefinition(
      string identifier,
      EntityType entityType,
      GameObject prefab,
      string displayName = null,
      bool isNetworked = false,
      IReadOnlyList<EntityPresetChildDetachment> childDetachments = null)
    {
      Identifier = identifier;
      EntityType = entityType;
      Prefab = prefab;
      DisplayName = displayName;
      IsNetworked = isNetworked;
      ChildDetachments = childDetachments;
    }
  }
}
