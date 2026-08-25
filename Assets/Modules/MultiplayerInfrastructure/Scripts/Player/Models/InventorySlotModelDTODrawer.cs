using System.Reflection;
using MultiplayerInfrastructure.ItemSystem;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(InventorySlotModelDTO))]
public class InventorySlotModelDTODrawer : PropertyDrawer
{
  public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
  {
    // Begin drawing property
    EditorGUI.BeginProperty(position, label, property);

    // Draw prefix label
    position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

    var indent = EditorGUI.indentLevel;
    EditorGUI.indentLevel = 0;

    float lineHeight = EditorGUIUtility.singleLineHeight;
    float spacing = 2f;
    float x = position.x;
    float y = position.y;
    float width = position.width;

    // ItemInstance (object reference) - use boxedValue for SerializeReference
    SerializedProperty itemProp = property.FindPropertyRelative("_itemInstance");
    Rect itemRect = new Rect(x, y, width, lineHeight);
    EditorGUI.PropertyField(itemRect, itemProp, new GUIContent("Item Instance"));
    y += lineHeight + spacing;

    // If item is not null, show some of its fields via reflection
    if (itemProp.boxedValue != null)
    {
      Item item = itemProp.boxedValue as Item;
      if (item != null)
      {
        // Helper to get property value via reflection
        object GetPropValue(string propName)
        {
          var prop = item.GetType().GetProperty(propName,
              BindingFlags.Public | BindingFlags.Instance);
          return prop?.GetValue(item, null);
        }

        // Identifier
        var idVal = GetPropValue(nameof(Item.Identifier));
        if (idVal != null)
        {
          EditorGUI.LabelField(new Rect(x, y, width, lineHeight), "ID:", idVal.ToString());
          y += lineHeight + spacing;
        }

        // DisplayName
        var nameVal = GetPropValue(nameof(Item.DisplayName));
        if (nameVal != null)
        {
          EditorGUI.LabelField(new Rect(x, y, width, lineHeight), "Display Name:", nameVal.ToString());
          y += lineHeight + spacing;
        }

        // Description
        var descVal = GetPropValue(nameof(Item.Description));
        if (descVal != null)
        {
          EditorGUI.LabelField(new Rect(x, y, width, lineHeight), "Description:", descVal.ToString());
          y += lineHeight + spacing;
        }

        // CurrentStackCount
        var stackVal = GetPropValue(nameof(Item.CurrentStackCount));
        if (stackVal != null)
        {
          EditorGUI.LabelField(new Rect(x, y, width, lineHeight), "Stack Count:", stackVal.ToString());
          y += lineHeight + spacing;
        }
      }
    }

    EditorGUI.indentLevel = indent;
    EditorGUI.EndProperty();
  }

  public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
  {
    float lineHeight = EditorGUIUtility.singleLineHeight;
    float spacing = 2f;
    float height = lineHeight; // ItemInstance
    SerializedProperty itemProp = property.FindPropertyRelative("_itemInstance");
    if (itemProp != null && itemProp.boxedValue != null)
    {
      Item item = itemProp.boxedValue as Item;
      if (item != null)
      {
        // Up to 4 fields
        height += lineHeight * 4;
        height += spacing * 4;
      }
    }
    return height;
  }
}
#endif
