using TriageTrainer.Entity;
using UnityEditor;
using UnityEngine;

namespace TriageTrainer.Editor
{
  [CustomPropertyDrawer(typeof(StaticEntityTransformDefinition))]
  public sealed class StaticEntityTransformDefinitionDrawer : PropertyDrawer
  {
    private const float Gap = 2f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
      int rows = 4; // type, identifier, position, rotation
      StaticEntityLayoutType type = ReadType(property);
      if (type == StaticEntityLayoutType.MovingPatientBedPositioningPoint || type == StaticEntityLayoutType.DefibCartSnapPoint) rows += 2;
      if (type == StaticEntityLayoutType.PatientCareDescriptionZone) rows += 2;
      return rows * EditorGUIUtility.singleLineHeight + (rows - 1) * Gap;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
      EditorGUI.BeginProperty(position, label, property);
      float line = EditorGUIUtility.singleLineHeight;
      float y = position.y;
      var type = ReadType(property);

      Draw(position, ref y, line, property.FindPropertyRelative("type"), "Type");
      Draw(position, ref y, line, property.FindPropertyRelative("identifier"), "Identifier");
      DrawOptional(position, ref y, line, property.FindPropertyRelative("useDefaultPosition"), property.FindPropertyRelative("position"), "Position");
      DrawOptional(position, ref y, line, property.FindPropertyRelative("useDefaultRotation"), property.FindPropertyRelative("rotationEuler"), "Rotation");

      if (type == StaticEntityLayoutType.MovingPatientBedPositioningPoint || type == StaticEntityLayoutType.DefibCartSnapPoint)
      {
        DrawOptional(position, ref y, line, property.FindPropertyRelative("useDefaultOccupiedSize"), property.FindPropertyRelative("occupiedSize"), "Occupied Size");
        DrawOptional(position, ref y, line, property.FindPropertyRelative("useDefaultDisplayHeight"), property.FindPropertyRelative("displayHeight"), "Display Height");
      }
      else if (type == StaticEntityLayoutType.PatientCareDescriptionZone)
      {
        DrawOptional(position, ref y, line, property.FindPropertyRelative("useDefaultZoneCenter"), property.FindPropertyRelative("zoneCenter"), "Zone Center");
        DrawOptional(position, ref y, line, property.FindPropertyRelative("useDefaultZoneSize"), property.FindPropertyRelative("zoneSize"), "Zone Size");
      }

      EditorGUI.EndProperty();
    }

    private static void Draw(Rect container, ref float y, float line, SerializedProperty property, string label)
    {
      var rect = new Rect(container.x, y, container.width, line);
      EditorGUI.PropertyField(rect, property, new GUIContent(label));
      y += line + Gap;
    }

    private static void DrawOptional(Rect container, ref float y, float line, SerializedProperty useDefault, SerializedProperty value, string label)
    {
      var rect = new Rect(container.x, y, container.width, line);
      var checkRect = new Rect(rect.x, rect.y, 18f, rect.height);
      var valueRect = new Rect(rect.x + 20f, rect.y, rect.width - 20f, rect.height);
      bool useCustomValue = !useDefault.boolValue;
      useCustomValue = EditorGUI.Toggle(checkRect, useCustomValue);
      useDefault.boolValue = !useCustomValue;
      using (new EditorGUI.DisabledScope(!useCustomValue))
        EditorGUI.PropertyField(valueRect, value, new GUIContent(label));
      y += line + Gap;
    }

    private static StaticEntityLayoutType ReadType(SerializedProperty property)
    {
      return (StaticEntityLayoutType)property.FindPropertyRelative("type").enumValueIndex;
    }
  }
}
