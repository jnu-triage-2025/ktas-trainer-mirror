using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  /// <summary>
  /// 엔티티 프리셋 정의: 하나의 프리팹에 사전 설정(EntityType 폴백/표시명/네트워크 여부)과 식별자를 부여해 저장한 단위.
  ///
  /// 하위 오브젝트는 <see cref="ChildReferences"/> 로 표현하며, 각 항목은 <b>다른 EntityPreset 의 식별자</b>를 가리킨다.
  /// (원본 프리팹의 Transform 자식을 경로로 가리키지 않는다.) 스폰 시 루트 프리셋과 각 하위 프리셋이 독립적으로
  /// 인스턴스화되며, unwrap 옵션에 따라 하위를 루트의 자식으로 둘지 또는 루트와 동일 계층(형제 루트)에 둘지 결정한다.
  /// </summary>
  [Serializable]
  public sealed class EntityPresetDefinition
  {
    public string Identifier { get; }
    public EntityType EntityType { get; }
    public GameObject Prefab { get; }
    public string DisplayName { get; }
    public bool IsNetworked { get; }

    /// <summary>
    /// 이 프리셋과 함께 스폰할 하위 엔티티 프리셋 참조 목록(다른 EntityPreset 식별자 기반).
    /// 비어 있으면 하위 없이 단일 프리셋으로 스폰된다.
    /// </summary>
    public IReadOnlyList<EntityPresetChildReference> ChildReferences { get; }

    public EntityPresetDefinition(
      string identifier,
      EntityType entityType,
      GameObject prefab,
      string displayName = null,
      bool isNetworked = false,
      IReadOnlyList<EntityPresetChildReference> childReferences = null)
    {
      Identifier = identifier;
      EntityType = entityType;
      Prefab = prefab;
      DisplayName = displayName;
      IsNetworked = isNetworked;
      ChildReferences = childReferences ?? Array.Empty<EntityPresetChildReference>();
    }
  }
}
