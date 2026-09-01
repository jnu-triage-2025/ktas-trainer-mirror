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
    // 프로퍼티 그리기 시작
    EditorGUI.BeginProperty(position, label, property);

    // 접두 라벨 그리기
    position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

    var indent = EditorGUI.indentLevel;
    EditorGUI.indentLevel = 0;

    float lineHeight = EditorGUIUtility.singleLineHeight;
    float spacing = 2f;
    float x = position.x;
    float y = position.y;
    float width = position.width;

    // ItemInstance (오브젝트 참조) - SerializeReference 이므로 boxedValue 사용
    SerializedProperty itemProp = property.FindPropertyRelative("_itemInstance");
    Rect itemRect = new Rect(x, y, width, lineHeight);
    EditorGUI.PropertyField(itemRect, itemProp, new GUIContent("Item Instance"));
    y += lineHeight + spacing;

    // 아이템이 null 이 아니면 리플렉션으로 일부 필드를 표시한다
    if (itemProp.boxedValue != null)
    {
      Item item = itemProp.boxedValue as Item;
      if (item != null)
      {
        // 리플렉션으로 프로퍼티 값을 가져오는 헬퍼
        object GetPropValue(string propName)
        {
          var prop = item.GetType().GetProperty(propName,
              BindingFlags.Public | BindingFlags.Instance);
          return prop?.GetValue(item, null);
        }

        // 식별자
        var idVal = GetPropValue(nameof(Item.Identifier));
        if (idVal != null)
        {
          EditorGUI.LabelField(new Rect(x, y, width, lineHeight), "ID:", idVal.ToString());
          y += lineHeight + spacing;
        }

        // 표시 이름
        var nameVal = GetPropValue(nameof(Item.DisplayName));
        if (nameVal != null)
        {
          EditorGUI.LabelField(new Rect(x, y, width, lineHeight), "Display Name:", nameVal.ToString());
          y += lineHeight + spacing;
        }

        // 설명
        var descVal = GetPropValue(nameof(Item.Description));
        if (descVal != null)
        {
          EditorGUI.LabelField(new Rect(x, y, width, lineHeight), "Description:", descVal.ToString());
          y += lineHeight + spacing;
        }

        // 현재 스택 수
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
    float height = lineHeight; // ItemInstance 높이
    SerializedProperty itemProp = property.FindPropertyRelative("_itemInstance");
    if (itemProp != null && itemProp.boxedValue != null)
    {
      Item item = itemProp.boxedValue as Item;
      if (item != null)
      {
        // 최대 4개 필드
        height += lineHeight * 4;
        height += spacing * 4;
      }
    }
    return height;
  }
}
#endif
