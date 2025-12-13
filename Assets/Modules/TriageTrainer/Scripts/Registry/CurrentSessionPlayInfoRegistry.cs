using System;
using TriageTrainer.Player;
using TriageTrainer.Definitions;
using UnityEngine;
using System.Collections.Generic;

namespace TriageTrainer.Registry
{
  public class CurrentSessionPlayInfoRegistry
  {
    private static readonly Dictionary<Type, UnityEngine.Object> _registry = new Dictionary<Type, UnityEngine.Object>();
    public SessionInformationModel sessionInformation = new SessionInformationModel
    (
      DefaultsSessionInformationModel.address, DefaultsSessionInformationModel.port
    );

    /// <example>
    /// CurrentSessionPlayInfoRegistry.OnRegistryRegistered += ~~
    /// </example>
    public static event Action<Type> OnRegistryRegistered;
    public static event Action<Type> OnRegistryRemoved;
    public static event Action<Type> OnRegistryChanged;
    
    /// <summary>
    /// 주어진 인스턴스를 타입 키로 등록합니다. 동일 타입이 이미 있으면 새 인스턴스로 교체합니다.
    /// </summary>
    public static void Register<T>(T instance) where T : UnityEngine.Object
    {
      if (instance == null)
      {
        Debug.LogWarning("CurrentSessionPlayInfoRegistry.Register called with null instance");
        return;
      }

      _registry[instance.GetType()] = instance;
      OnRegistryRegistered?.Invoke(instance.GetType());
      OnRegistryChanged?.Invoke(instance.GetType());
    }

    public static void Unregister<T>() where T : UnityEngine.Object
    {
      _registry.Remove(typeof(T));
      OnRegistryRemoved?.Invoke(typeof(T));
      OnRegistryChanged?.Invoke(typeof(T));
    }

    /// <summary>
    /// 지정한 타입 T로 레지스트리에 등록된 인스턴스를 반환합니다.
    /// 등록은 인스턴스의 실제 런타임 타입(instance.GetType())을 키로 사용하므로,
    /// 제네릭 T가 베이스 클래스나 인터페이스인 경우에는 등록된 구체 타입과 일치하지 않아 검색에 실패할 수 있습니다.
    /// 등록된 항목이 없으면 경고를 출력하고 null을 반환합니다.
    /// </summary>
    public static T Get<T>() where T : UnityEngine.Object
    {
      if (_registry.TryGetValue(typeof(T), out var obj))
        return obj as T;

      Debug.LogWarning($"CurrentSessionPlayInfoRegistry: no instance registered for type {typeof(T).Name}");
      return null;
    }
  }
}
