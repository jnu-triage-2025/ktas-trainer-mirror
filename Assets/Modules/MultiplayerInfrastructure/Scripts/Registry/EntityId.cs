using System;
using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  /// <summary>
  /// 씬 고정 엔티티에 사용할 안정적인 식별자를 생성/보정하는 유틸리티입니다.
  ///
  /// - 비어 있으면 현재 GameObject 이름 기반 접두어 + GUID를 생성
  /// - 이미 값이 있으면 그대로 유지
  /// </summary>
  public static class EntityId
  {
    public static string Ensure(string currentIdentifier, GameObject target, string prefix)
    {
      if (!string.IsNullOrWhiteSpace(currentIdentifier))
        return currentIdentifier;

      prefix = string.IsNullOrWhiteSpace(prefix) ? "entity" : prefix.Trim();

      string baseName = target != null && !string.IsNullOrWhiteSpace(target.name)
        ? target.name.Trim().Replace(' ', '-')
        : prefix;

      return $"{prefix}:{baseName}:{Guid.NewGuid():N}";
    }
  }
}
