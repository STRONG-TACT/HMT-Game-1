using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(Combat.Dice))]
public class DiceDrawer : PropertyDrawer {
    //public override VisualElement CreatePropertyGUI(SerializedProperty property) {
    //    var container = new VisualElement();

    //    var typeField = new PropertyField(property.FindPropertyRelative("type"));
    //    var bonusField = new PropertyField(property.FindPropertyRelative("bonus"));

    //    container.Add(typeField);
    //    container.Add(bonusField);

    //    return container;
    //}

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        EditorGUI.BeginProperty(position, label, property);
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        var typeRect = new Rect(position.x, position.y, 50, position.height);
        var bonusRect = new Rect(position.x + 55, position.y, 50, position.height);

        EditorGUI.PropertyField(typeRect, property.FindPropertyRelative("type"), GUIContent.none);
        EditorGUI.PropertyField(bonusRect, property.FindPropertyRelative("bonus"), GUIContent.none);

        EditorGUI.indentLevel = indent;

        EditorGUI.EndProperty();
        
        
        //base.OnGUI(position, property, label);
    }
}
