using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[CustomEditor(typeof(ItemSpawner))]
public class ItemSpawnerEditor : Editor
{
    private ItemSpawner spawner;
    private bool showItemTypesFoldout = true;
    private bool showDebugFoldout = false;

    private void OnEnable()
    {
        spawner = (ItemSpawner)target;
    }

    public override void OnInspectorGUI()
    {
        // 绘制默认检查器
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("物品生成控制", EditorStyles.boldLabel);

        // 生成按钮区域
        EditorGUILayout.BeginHorizontal();

        // 生成随机物品按钮
        if (GUILayout.Button("生成随机物品", GUILayout.Height(30)))
        {
            if (Application.isPlaying)
            {
                spawner.SpawnRandomItems();
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "请在运行时使用此功能！", "确定");
            }
        }

        // 清空所有物品按钮
        if (GUILayout.Button("清空所有物品", GUILayout.Height(30)))
        {
            if (Application.isPlaying)
            {
                if (EditorUtility.DisplayDialog("确认清空", "确定要清空所有生成的物品吗？", "确定", "取消"))
                {
                    spawner.ClearAllItems();
                }
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "请在运行时使用此功能！", "确定");
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 快速设置按钮区域
        EditorGUILayout.LabelField("快速设置", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("全选物品类型"))
        {
            SetAllItemTypes(true);
        }

        if (GUILayout.Button("全不选物品类型"))
        {
            SetAllItemTypes(false);
        }

        if (GUILayout.Button("仅选择装备"))
        {
            SetEquipmentOnly();
        }

        if (GUILayout.Button("仅选择消耗品"))
        {
            SetConsumablesOnly();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // 运行时信息显示
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("运行时信息", EditorStyles.boldLabel);

            var spawnedItems = spawner.GetSpawnedItems();
            EditorGUILayout.LabelField($"已生成物品数量: {spawnedItems.Count}");

            var gridOccupancy = spawner.GetGridOccupancy();
            if (gridOccupancy != null)
            {
                int occupiedCells = 0;
                int totalCells = gridOccupancy.GetLength(0) * gridOccupancy.GetLength(1);

                for (int x = 0; x < gridOccupancy.GetLength(0); x++)
                {
                    for (int y = 0; y < gridOccupancy.GetLength(1); y++)
                    {
                        if (gridOccupancy[x, y] == 1)
                        {
                            occupiedCells++;
                        }
                    }
                }

                float occupancyPercentage = (float)occupiedCells / totalCells * 100f;
                EditorGUILayout.LabelField($"网格占用率: {occupancyPercentage:F1}% ({occupiedCells}/{totalCells})");
            }

            EditorGUILayout.Space(5);

            // 显示生成的物品列表
            if (spawnedItems.Count > 0)
            {
                showDebugFoldout = EditorGUILayout.Foldout(showDebugFoldout, "生成的物品列表");
                if (showDebugFoldout)
                {
                    EditorGUI.indentLevel++;
                    foreach (var item in spawnedItems)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"{item.itemData.itemName} - 位置:({item.gridPosition.x},{item.gridPosition.y}) 尺寸:({item.size.x}x{item.size.y})");
                        if (GUILayout.Button("删除", GUILayout.Width(50)))
                        {
                            spawner.RemoveItem(item.itemObject);
                            break;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel--;
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("请运行游戏以查看生成器状态和使用生成功能。", MessageType.Info);
        }

        // 应用修改
        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }

    // 设置所有物品类型
    private void SetAllItemTypes(bool enabled)
    {
        SerializedObject so = new SerializedObject(target);

        so.FindProperty("spawnHelmet").boolValue = enabled;
        so.FindProperty("spawnArmor").boolValue = enabled;
        so.FindProperty("spawnTacticalRig").boolValue = enabled;
        so.FindProperty("spawnBackpack").boolValue = enabled;
        so.FindProperty("spawnWeapon").boolValue = enabled;
        so.FindProperty("spawnAmmunition").boolValue = enabled;
        so.FindProperty("spawnFood").boolValue = enabled;
        so.FindProperty("spawnDrink").boolValue = enabled;
        so.FindProperty("spawnHealing").boolValue = enabled;
        so.FindProperty("spawnHemostatic").boolValue = enabled;
        so.FindProperty("spawnSedative").boolValue = enabled;
        so.FindProperty("spawnIntelligence").boolValue = enabled;
        so.FindProperty("spawnCurrency").boolValue = enabled;

        so.ApplyModifiedProperties();
    }

    // 仅选择装备类型
    private void SetEquipmentOnly()
    {
        SerializedObject so = new SerializedObject(target);

        so.FindProperty("spawnHelmet").boolValue = true;
        so.FindProperty("spawnArmor").boolValue = true;
        so.FindProperty("spawnTacticalRig").boolValue = true;
        so.FindProperty("spawnBackpack").boolValue = true;
        so.FindProperty("spawnWeapon").boolValue = true;
        so.FindProperty("spawnAmmunition").boolValue = true;

        so.FindProperty("spawnFood").boolValue = false;
        so.FindProperty("spawnDrink").boolValue = false;
        so.FindProperty("spawnHealing").boolValue = false;
        so.FindProperty("spawnHemostatic").boolValue = false;
        so.FindProperty("spawnSedative").boolValue = false;
        so.FindProperty("spawnIntelligence").boolValue = false;
        so.FindProperty("spawnCurrency").boolValue = false;

        so.ApplyModifiedProperties();
    }

    // 仅选择消耗品类型
    private void SetConsumablesOnly()
    {
        SerializedObject so = new SerializedObject(target);

        so.FindProperty("spawnHelmet").boolValue = false;
        so.FindProperty("spawnArmor").boolValue = false;
        so.FindProperty("spawnTacticalRig").boolValue = false;
        so.FindProperty("spawnBackpack").boolValue = false;
        so.FindProperty("spawnWeapon").boolValue = false;
        so.FindProperty("spawnAmmunition").boolValue = false;

        so.FindProperty("spawnFood").boolValue = true;
        so.FindProperty("spawnDrink").boolValue = true;
        so.FindProperty("spawnHealing").boolValue = true;
        so.FindProperty("spawnHemostatic").boolValue = true;
        so.FindProperty("spawnSedative").boolValue = true;
        so.FindProperty("spawnIntelligence").boolValue = true;
        so.FindProperty("spawnCurrency").boolValue = true;

        so.ApplyModifiedProperties();
    }
}
#endif
