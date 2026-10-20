using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SingleFlagAttribute))]
public sealed class SingleFlagAttributeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position,SerializedProperty property,GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.Enum)
        {
            EditorGUI.PropertyField(position,property,label);
            return;
        }

        int selectedIndex = Mathf.Max(0,property.enumValueIndex);
        property.enumValueIndex = EditorGUI.Popup(position,label.text,selectedIndex,property.enumDisplayNames);
    }
}
