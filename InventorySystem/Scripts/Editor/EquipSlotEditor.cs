using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(EquipSlot))]
public class EquipSlotEditor : Editor
{
    private SerializedProperty acceptedType;
    private SerializedProperty padding;
    private SerializedProperty autoResize;

    // 背包相关属性
    private SerializedProperty backpackContainer;
    private SerializedProperty backpackGridPrefab;

    // 战术挂具相关属性
    private SerializedProperty tacticalRigContainer;
    private SerializedProperty tacticalRigGridPrefab;

    private void OnEnable()
    {
        // 获取所有序列化属性
        acceptedType = serializedObject.FindProperty("acceptedType");
        padding = serializedObject.FindProperty("padding");
        autoResize = serializedObject.FindProperty("autoResize");

        backpackContainer = serializedObject.FindProperty("backpackContainer");
        backpackGridPrefab = serializedObject.FindProperty("backpackGridPrefab");

        tacticalRigContainer = serializedObject.FindProperty("tacticalRigContainer");
        tacticalRigGridPrefab = serializedObject.FindProperty("tacticalRigGridPrefab");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 显示基础设置
        EditorGUILayout.LabelField("基础设置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(acceptedType, new GUIContent("装备类型"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("装备栏适配设置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(padding);
        EditorGUILayout.PropertyField(autoResize);

        // 根据选择的类型显示对应设置
        InventorySystemItemCategory selectedType = (InventorySystemItemCategory)acceptedType.enumValueIndex;

        EditorGUILayout.Space();

        if (selectedType == InventorySystemItemCategory.Backpack)
        {
            EditorGUILayout.LabelField("背包网格容器设置", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(backpackContainer, new GUIContent("背包容器"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("背包网格预制体引用", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(backpackGridPrefab, new GUIContent("背包网格预制体"));
        }
        else if (selectedType == InventorySystemItemCategory.TacticalRig)
        {
            EditorGUILayout.LabelField("战术挂具网格容器设置", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(tacticalRigContainer, new GUIContent("战术挂具容器"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("战术挂具网格预制体引用", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(tacticalRigGridPrefab, new GUIContent("战术挂具网格预制体"));
        }
        else
        {
            EditorGUILayout.HelpBox("当前选择的类型不需要额外的网格设置", MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
