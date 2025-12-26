using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  /// <summary>
  /// </summary>
  public static class UIControlRegistry
  {
    private static readonly Dictionary<Type, UnityEngine.Object> _controls = new();

    /// <summary>
    /// 주어진 인스턴스를 타입 키로 등록합니다. 동일 타입이 이미 있으면 새 인스턴스로 교체합니다.
    /// </summary>
    public static void Register<T>(T instance) where T : UnityEngine.Object
    {
      if (instance == null)
      {
        Debug.LogWarning("UIControlRegistry.Register called with null instance");
        return;
      }

      // Use the runtime type of the instance as the key so derived types
      // are registered under their concrete type instead of the compile-time
      // type inferred at the call site.
      _controls[instance.GetType()] = instance;
    }

    /// <summary>
    /// 등록된 UI 컨트롤러를 반환합니다. 없으면 null을 반환하고 경고를 로그로 남깁니다.
    /// </summary>
    public static T Get<T>() where T : UnityEngine.Object
    {
      if (_controls.TryGetValue(typeof(T), out var obj))
        return obj as T;

      Debug.LogWarning($"UIControlRegistry: no instance registered for type {typeof(T).Name}");
      return null;
    }
  }
}
