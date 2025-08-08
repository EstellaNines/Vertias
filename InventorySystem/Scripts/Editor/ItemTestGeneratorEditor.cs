using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(ItemTestGenerator))]
public class ItemTestGeneratorEditor : Editor
{
    private SerializedProperty selectedCategoriesProperty;
    private bool[] categoryToggles;
    private string[] categoryNames;

    private void OnEnable()
    {
        selectedCategoriesProperty = serializedObject.FindProperty("selectedCategories");
        
        // 初始化类型选项
        var categoryValues = System.Enum.GetValues(typeof(InventorySystemItemCategory));
        categoryNames = new string[categoryValues.Length];
        categoryToggles = new bool[categoryValues.Length];
        
        for (int i = 0; i < categoryValues.Length; i++)
        {
            var category = (InventorySystemItemCategory)categoryValues.GetValue(i);
            categoryNames[i] = GetCategoryDisplayName(category);
        }
        
        UpdateTogglesFromProperty();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // 绘制默认属性
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("物品类型多选", EditorStyles.boldLabel);
        
        // 绘制类型选择界面
        DrawCategorySelection();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("生成操作", EditorStyles.boldLabel);
        
        // 生成按钮
        if (GUILayout.Button("生成所有物品", GUILayout.Height(30)))
        {
            ((ItemTestGenerator)target).GenerateAllItemsTest();
        }
        
        if (GUILayout.Button("随机生成所有物品", GUILayout.Height(30)))
        {
            ((ItemTestGenerator)target).GenerateAllItemsRandomTest();
        }
        
        if (GUILayout.Button("随机生成单个物品", GUILayout.Height(30)))
        {
            ((ItemTestGenerator)target).GenerateSingleRandomItemTest();
        }
        
        if (GUILayout.Button("清除所有测试物品", GUILayout.Height(30)))
        {
            ((ItemTestGenerator)target).ClearAllTestItems();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "使用说明：\n" +
            "• 生成所有物品：按最优布局生成所有物品\n" +
            "• 随机生成所有物品：在指定数量范围内随机生成物品\n" +
            "• 随机生成单个物品：随机生成一个物品\n" +
            "• 启用类型筛选后，只会生成选中类型的物品",
            MessageType.Info);
        
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawCategorySelection()
    {
        EditorGUILayout.BeginVertical("box");
        
        // 全选/全不选按钮
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("全选", GUILayout.Width(60)))
        {
            for (int i = 0; i < categoryToggles.Length; i++)
            {
                categoryToggles[i] = true;
            }
            UpdatePropertyFromToggles();
        }
        
        if (GUILayout.Button("全不选", GUILayout.Width(60)))
        {
            for (int i = 0; i < categoryToggles.Length; i++)
            {
                categoryToggles[i] = false;
            }
            UpdatePropertyFromToggles();
        }
        
        if (GUILayout.Button("反选", GUILayout.Width(60)))
        {
            for (int i = 0; i < categoryToggles.Length; i++)
            {
                categoryToggles[i] = !categoryToggles[i];
            }
            UpdatePropertyFromToggles();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // 绘制类型选择复选框（分两列显示）
        int halfCount = (categoryNames.Length + 1) / 2;
        
        EditorGUILayout.BeginHorizontal();
        
        // 左列
        EditorGUILayout.BeginVertical();
        for (int i = 0; i < halfCount; i++)
        {
            if (i < categoryNames.Length)
            {
                bool newValue = EditorGUILayout.Toggle(categoryNames[i], categoryToggles[i]);
                if (newValue != categoryToggles[i])
                {
                    categoryToggles[i] = newValue;
                    UpdatePropertyFromToggles();
                }
            }
        }
        EditorGUILayout.EndVertical();
        
        // 右列
        EditorGUILayout.BeginVertical();
        for (int i = halfCount; i < categoryNames.Length; i++)
        {
            bool newValue = EditorGUILayout.Toggle(categoryNames[i], categoryToggles[i]);
            if (newValue != categoryToggles[i])
            {
                categoryToggles[i] = newValue;
                UpdatePropertyFromToggles();
            }
        }
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }

    private void UpdateTogglesFromProperty()
    {
        var selectedCategories = new List<InventorySystemItemCategory>();
        
        for (int i = 0; i < selectedCategoriesProperty.arraySize; i++)
        {
            var element = selectedCategoriesProperty.GetArrayElementAtIndex(i);
            selectedCategories.Add((InventorySystemItemCategory)element.enumValueIndex);
        }
        
        var categoryValues = System.Enum.GetValues(typeof(InventorySystemItemCategory));
        for (int i = 0; i < categoryValues.Length; i++)
        {
            var category = (InventorySystemItemCategory)categoryValues.GetValue(i);
            categoryToggles[i] = selectedCategories.Contains(category);
        }
    }

    private void UpdatePropertyFromToggles()
    {
        selectedCategoriesProperty.ClearArray();
        
        var categoryValues = System.Enum.GetValues(typeof(InventorySystemItemCategory));
        for (int i = 0; i < categoryToggles.Length; i++)
        {
            if (categoryToggles[i])
            {
                selectedCategoriesProperty.InsertArrayElementAtIndex(selectedCategoriesProperty.arraySize);
                var newElement = selectedCategoriesProperty.GetArrayElementAtIndex(selectedCategoriesProperty.arraySize - 1);
                newElement.enumValueIndex = i;
            }
        }
        
        serializedObject.ApplyModifiedProperties();
    }

    private string GetCategoryDisplayName(InventorySystemItemCategory category)
    {
        switch (category)
        {
            case InventorySystemItemCategory.Helmet: return "头盔";
            case InventorySystemItemCategory.Armor: return "护甲";
            case InventorySystemItemCategory.TacticalRig: return "战术挂具";
            case InventorySystemItemCategory.Backpack: return "背包";
            case InventorySystemItemCategory.Weapon: return "武器";
            case InventorySystemItemCategory.Ammunition: return "弹药";
            case InventorySystemItemCategory.Food: return "食物";
            case InventorySystemItemCategory.Drink: return "饮料";
            case InventorySystemItemCategory.Sedative: return "镇静剂";
            case InventorySystemItemCategory.Hemostatic: return "止血剂";
            case InventorySystemItemCategory.Healing: return "治疗用品";
            case InventorySystemItemCategory.Intelligence: return "情报";
            case InventorySystemItemCategory.Currency: return "货币";
            default: return category.ToString();
        }
    }
}