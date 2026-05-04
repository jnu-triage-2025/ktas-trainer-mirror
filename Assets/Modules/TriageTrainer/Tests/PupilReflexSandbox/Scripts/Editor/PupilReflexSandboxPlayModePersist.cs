#if UNITY_EDITOR
using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TriageTrainer.Tests.PupilReflexSandbox
{
  [InitializeOnLoad]
  internal static class PupilReflexSandboxPlayModePersist
  {
    private struct SerializedValue
    {
      public SerializedPropertyType Type;
      public object Value;
    }

    private sealed class ComponentSnapshot
    {
      public string GameObjectPath;
      public string ComponentTypeName;
      public Dictionary<string, SerializedValue> Values = new Dictionary<string, SerializedValue>();
    }

    private sealed class TransformSnapshot
    {
      public string Path;
      public Vector3 LocalPosition;
      public Quaternion LocalRotation;
      public Vector3 LocalScale;
    }

    private sealed class SandboxSnapshot
    {
      public List<TransformSnapshot> Transforms = new List<TransformSnapshot>();
      public List<ComponentSnapshot> Components = new List<ComponentSnapshot>();
    }

    private static readonly List<SandboxSnapshot> snapshots = new List<SandboxSnapshot>();

    static PupilReflexSandboxPlayModePersist()
    {
      EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
      if (state == PlayModeStateChange.ExitingPlayMode)
      {
        CaptureSnapshots();
      }
      else if (state == PlayModeStateChange.EnteredEditMode)
      {
        ApplySnapshots();
      }
    }

    private static void CaptureSnapshots()
    {
      snapshots.Clear();

      PupilReflexSandboxBootstrap[] bootstraps = UnityEngine.Object.FindObjectsOfType<PupilReflexSandboxBootstrap>(true);
      for (int i = 0; i < bootstraps.Length; i++)
      {
        PupilReflexSandboxBootstrap bootstrap = bootstraps[i];
        if (bootstrap == null || !bootstrap.PersistPlayModeChanges)
        {
          continue;
        }

        SandboxSnapshot snapshot = new SandboxSnapshot();
        if (bootstrap.PersistPlayModeTransforms)
        {
          CaptureTransforms(bootstrap.transform, snapshot.Transforms);
        }
        CaptureComponents(bootstrap.transform, snapshot.Components);
        snapshots.Add(snapshot);
      }
    }

    private static void CaptureTransforms(Transform root, List<TransformSnapshot> output)
    {
      Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
      for (int i = 0; i < transforms.Length; i++)
      {
        Transform current = transforms[i];
        if (current == null)
        {
          continue;
        }

        output.Add(new TransformSnapshot
        {
          Path = GetHierarchyPath(current),
          LocalPosition = current.localPosition,
          LocalRotation = current.localRotation,
          LocalScale = current.localScale
        });
      }
    }

    private static void CaptureComponents(Transform root, List<ComponentSnapshot> output)
    {
      MonoBehaviour[] components = root.GetComponentsInChildren<MonoBehaviour>(true);
      for (int i = 0; i < components.Length; i++)
      {
        MonoBehaviour component = components[i];
        if (component == null)
        {
          continue;
        }

        Type type = component.GetType();
        if (type.Namespace != "TriageTrainer.Tests.PupilReflexSandbox")
        {
          continue;
        }

        ComponentSnapshot snapshot = new ComponentSnapshot
        {
          GameObjectPath = GetHierarchyPath(component.transform),
          ComponentTypeName = type.AssemblyQualifiedName
        };

        ReadSerializedValues(component, snapshot.Values);
        output.Add(snapshot);
      }
    }

    private static void ReadSerializedValues(MonoBehaviour component, Dictionary<string, SerializedValue> output)
    {
      SerializedObject serializedObject = new SerializedObject(component);
      SerializedProperty property = serializedObject.GetIterator();
      bool enterChildren = true;

      while (property.NextVisible(enterChildren))
      {
        enterChildren = false;

        if (property.name == "m_Script")
        {
          continue;
        }

        if (property.propertyType == SerializedPropertyType.ObjectReference)
        {
          continue;
        }

        if (property.isArray && property.propertyType == SerializedPropertyType.Generic)
        {
          continue;
        }

        if (!TryReadValue(property, out SerializedValue value))
        {
          continue;
        }

        output[property.propertyPath] = value;
      }
    }

    private static bool TryReadValue(SerializedProperty property, out SerializedValue value)
    {
      value = default;

      switch (property.propertyType)
      {
        case SerializedPropertyType.Integer:
          value = new SerializedValue { Type = property.propertyType, Value = property.intValue };
          return true;
        case SerializedPropertyType.Boolean:
          value = new SerializedValue { Type = property.propertyType, Value = property.boolValue };
          return true;
        case SerializedPropertyType.Float:
          value = new SerializedValue { Type = property.propertyType, Value = property.floatValue };
          return true;
        case SerializedPropertyType.String:
          value = new SerializedValue { Type = property.propertyType, Value = property.stringValue };
          return true;
        case SerializedPropertyType.Color:
          value = new SerializedValue { Type = property.propertyType, Value = property.colorValue };
          return true;
        case SerializedPropertyType.Vector2:
          value = new SerializedValue { Type = property.propertyType, Value = property.vector2Value };
          return true;
        case SerializedPropertyType.Vector3:
          value = new SerializedValue { Type = property.propertyType, Value = property.vector3Value };
          return true;
        case SerializedPropertyType.Vector4:
          value = new SerializedValue { Type = property.propertyType, Value = property.vector4Value };
          return true;
        case SerializedPropertyType.Quaternion:
          value = new SerializedValue { Type = property.propertyType, Value = property.quaternionValue };
          return true;
        case SerializedPropertyType.Rect:
          value = new SerializedValue { Type = property.propertyType, Value = property.rectValue };
          return true;
        case SerializedPropertyType.Bounds:
          value = new SerializedValue { Type = property.propertyType, Value = property.boundsValue };
          return true;
        case SerializedPropertyType.AnimationCurve:
        {
          AnimationCurve curve = property.animationCurveValue;
          value = new SerializedValue
          {
            Type = property.propertyType,
            Value = curve != null ? new AnimationCurve(curve.keys) : new AnimationCurve()
          };
          return true;
        }
        case SerializedPropertyType.Enum:
          value = new SerializedValue { Type = property.propertyType, Value = property.enumValueIndex };
          return true;
        default:
          return false;
      }
    }

    private static void ApplySnapshots()
    {
      if (snapshots.Count == 0)
      {
        return;
      }

      Dictionary<string, Transform> transformMap = BuildSceneTransformMap();

      for (int i = 0; i < snapshots.Count; i++)
      {
        SandboxSnapshot snapshot = snapshots[i];
        ApplyTransforms(snapshot.Transforms, transformMap);
        ApplyComponents(snapshot.Components, transformMap);
      }

      snapshots.Clear();
    }

    private static Dictionary<string, Transform> BuildSceneTransformMap()
    {
      Dictionary<string, Transform> map = new Dictionary<string, Transform>();
      Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();

      for (int i = 0; i < allTransforms.Length; i++)
      {
        Transform transform = allTransforms[i];
        if (transform == null)
        {
          continue;
        }

        if (!transform.gameObject.scene.IsValid())
        {
          continue;
        }

        if (EditorSceneManager.IsPreviewScene(transform.gameObject.scene))
        {
          continue;
        }

        if (EditorUtility.IsPersistent(transform))
        {
          continue;
        }

        string path = GetHierarchyPath(transform);
        if (!map.ContainsKey(path))
        {
          map.Add(path, transform);
        }
      }

      return map;
    }

    private static void ApplyTransforms(List<TransformSnapshot> snapshotsToApply, Dictionary<string, Transform> map)
    {
      for (int i = 0; i < snapshotsToApply.Count; i++)
      {
        TransformSnapshot snapshot = snapshotsToApply[i];
        if (!map.TryGetValue(snapshot.Path, out Transform target) || target == null)
        {
          continue;
        }

        Undo.RecordObject(target, "Persist Pupil Reflex Transform");
        target.localPosition = snapshot.LocalPosition;
        target.localRotation = snapshot.LocalRotation;
        target.localScale = snapshot.LocalScale;

        EditorUtility.SetDirty(target);
        PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        if (target.gameObject.scene.IsValid())
        {
          EditorSceneManager.MarkSceneDirty(target.gameObject.scene);
        }
      }
    }

    private static void ApplyComponents(List<ComponentSnapshot> snapshotsToApply, Dictionary<string, Transform> map)
    {
      for (int i = 0; i < snapshotsToApply.Count; i++)
      {
        ComponentSnapshot snapshot = snapshotsToApply[i];
        if (!map.TryGetValue(snapshot.GameObjectPath, out Transform targetTransform) || targetTransform == null)
        {
          continue;
        }

        Type componentType = Type.GetType(snapshot.ComponentTypeName);
        if (componentType == null)
        {
          continue;
        }

        Component component = targetTransform.GetComponent(componentType);
        if (component == null)
        {
          continue;
        }

        SerializedObject serializedObject = new SerializedObject(component);
        Undo.RecordObject(component, "Persist Pupil Reflex Values");

        foreach (KeyValuePair<string, SerializedValue> entry in snapshot.Values)
        {
          SerializedProperty property = serializedObject.FindProperty(entry.Key);
          if (property == null)
          {
            continue;
          }

          ApplySerializedValue(property, entry.Value);
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(component);
        PrefabUtility.RecordPrefabInstancePropertyModifications(component);

        if (component.gameObject.scene.IsValid())
        {
          EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
        }
      }
    }

    private static void ApplySerializedValue(SerializedProperty property, SerializedValue value)
    {
      if (property.propertyType != value.Type)
      {
        return;
      }

      switch (value.Type)
      {
        case SerializedPropertyType.Integer:
          property.intValue = (int)value.Value;
          break;
        case SerializedPropertyType.Boolean:
          property.boolValue = (bool)value.Value;
          break;
        case SerializedPropertyType.Float:
          property.floatValue = (float)value.Value;
          break;
        case SerializedPropertyType.String:
          property.stringValue = (string)value.Value;
          break;
        case SerializedPropertyType.Color:
          property.colorValue = (Color)value.Value;
          break;
        case SerializedPropertyType.Vector2:
          property.vector2Value = (Vector2)value.Value;
          break;
        case SerializedPropertyType.Vector3:
          property.vector3Value = (Vector3)value.Value;
          break;
        case SerializedPropertyType.Vector4:
          property.vector4Value = (Vector4)value.Value;
          break;
        case SerializedPropertyType.Quaternion:
          property.quaternionValue = (Quaternion)value.Value;
          break;
        case SerializedPropertyType.Rect:
          property.rectValue = (Rect)value.Value;
          break;
        case SerializedPropertyType.Bounds:
          property.boundsValue = (Bounds)value.Value;
          break;
        case SerializedPropertyType.AnimationCurve:
          property.animationCurveValue = (AnimationCurve)value.Value;
          break;
        case SerializedPropertyType.Enum:
          property.enumValueIndex = (int)value.Value;
          break;
      }
    }

    private static string GetHierarchyPath(Transform transform)
    {
      if (transform == null)
      {
        return string.Empty;
      }

      Scene scene = transform.gameObject.scene;
      Stack<string> names = new Stack<string>();
      Transform current = transform;

      while (current != null)
      {
        names.Push(current.name);
        current = current.parent;
      }

      string path = string.Join("/", names);
      return scene.IsValid() ? scene.name + "/" + path : path;
    }
  }
}
#endif
