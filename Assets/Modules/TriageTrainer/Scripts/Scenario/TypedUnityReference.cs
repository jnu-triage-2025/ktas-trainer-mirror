using System;
using UnityEngine;

namespace TriageTrainer.Scenario
{
  /// <summary>Checks the managed type before calling any native Unity object API.</summary>
  public static class TypedUnityReference
  {
    public static bool TryGet<T>(object value, out T result, string location,
      Action<string> reportError) where T : UnityEngine.Object
    {
      result = null;
      if (ReferenceEquals(value, null))
        return false;

      // Do not trust the declared field type after deserializing old scene data.
      // In particular, never access Component.gameObject before this check.
      if (!typeof(T).IsAssignableFrom(value.GetType()))
      {
        reportError?.Invoke($"{location}: expected {typeof(T).Name}, got {value.GetType().Name}. Reference rejected.");
        return false;
      }

      var candidate = (T)value;
      if (candidate == null) // Unity's destroyed-object check, only after validating the managed type.
        return false;
      result = candidate;
      return true;
    }

    public static T RecoverUniqueChild<T>(object value, object root, string location,
      Action<string> reportError) where T : Component
    {
      if (TryGet<T>(value, out var current, location, reportError))
        return current;
      if (!TryGet<GameObject>(root, out var patient, location + " owner", reportError))
        return null;

      var matches = patient.GetComponentsInChildren<T>(true);
      if (matches.Length == 1)
        return TryGet<T>(matches[0], out var recovered, location, reportError) ? recovered : null;
      if (matches.Length > 1)
        reportError?.Invoke($"{location}: '{patient.name}' contains {matches.Length} {typeof(T).Name} markers; exactly one is required. Recovery skipped.");
      else if (!ReferenceEquals(value, null))
        reportError?.Invoke($"{location}: no {typeof(T).Name} marker under '{patient.name}'. Recovery failed.");
      return null;
    }

    public static bool SetActive<T>(object value, bool active, string location,
      Action<string> reportError) where T : UnityEngine.Object
    {
      if (!TryGet<T>(value, out var target, location, reportError))
        return false;
      if (target is GameObject gameObject)
      {
        gameObject.SetActive(active);
        return true;
      }
      if (target is Component component)
      {
        component.gameObject.SetActive(active);
        return true;
      }
      reportError?.Invoke($"{location}: {typeof(T).Name} cannot be activated.");
      return false;
    }
  }
}
