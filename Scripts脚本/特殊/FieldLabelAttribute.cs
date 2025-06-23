using System;

using UnityEditor;

using UnityEngine;

[AttributeUsage(AttributeTargets.Field)]

public class FieldLabelAttribute : PropertyAttribute

{

    public string label;

    public FieldLabelAttribute(string label)

    {

        this.label = label;

    }

}

[CustomPropertyDrawer(typeof(FieldLabelAttribute))]

public class FieldLabelDrawer : PropertyDrawer

{

    private FieldLabelAttribute FLAttribute

    {

        get { return (FieldLabelAttribute)attribute; }

    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)

    {

        EditorGUI.PropertyField(position, property, new GUIContent(FLAttribute.label), true);

    }

}
