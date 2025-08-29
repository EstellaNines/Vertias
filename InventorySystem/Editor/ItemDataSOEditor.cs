using UnityEngine;
using UnityEditor;
using InventorySystem;

namespace InventorySystem.Editor
{
    [CustomEditor(typeof(ItemDataSO))]
    public class ItemDataSOEditor : UnityEditor.Editor
    {
        private ItemDataSO itemData;
        private bool showBasicInfo = true;
        private bool showSizeInfo = true;
        private bool showSpecialInfo = true;
        private bool showRuntimeInfo = true;
        private bool showGridPreview = true;

        private void OnEnable()
        {
            itemData = (ItemDataSO)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 标题
            EditorGUILayout.Space();
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("物品数据编辑器", titleStyle);
            EditorGUILayout.Space();

            // 物品图标预览区域
            DrawItemIconPreview();
            EditorGUILayout.Space();

            // 网格预览区域
            showGridPreview = EditorGUILayout.Foldout(showGridPreview, "网格预览", true);
            if (showGridPreview)
            {
                EditorGUI.indentLevel++;
                DrawGridPreview();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // 基础信息
            showBasicInfo = EditorGUILayout.Foldout(showBasicInfo, "基础信息", true);
            if (showBasicInfo)
            {
                EditorGUI.indentLevel++;
                DrawBasicInfo();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // 尺寸属性
            showSizeInfo = EditorGUILayout.Foldout(showSizeInfo, "尺寸属性", true);
            if (showSizeInfo)
            {
                EditorGUI.indentLevel++;
                DrawSizeInfo();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // 特殊属性（根据分类显示）
            showSpecialInfo = EditorGUILayout.Foldout(showSpecialInfo, "特殊属性", true);
            if (showSpecialInfo)
            {
                EditorGUI.indentLevel++;
                DrawSpecialAttributes();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // 运行时数据
            showRuntimeInfo = EditorGUILayout.Foldout(showRuntimeInfo, "运行时数据", true);
            if (showRuntimeInfo)
            {
                EditorGUI.indentLevel++;
                DrawRuntimeInfo();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // 实用工具按钮
            DrawUtilityButtons();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawItemIconPreview()
        {
            EditorGUILayout.BeginHorizontal();

            // 左侧：图标预览
            EditorGUILayout.BeginVertical(GUILayout.Width(120));

            // 背景颜色预览
            Rect colorRect = GUILayoutUtility.GetRect(100, 20);
            EditorGUI.DrawRect(colorRect, itemData.backgroundColor);
            EditorGUILayout.LabelField("背景颜色", EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.Space(5);

            // 图标预览
            Rect iconRect = GUILayoutUtility.GetRect(100, 100);
            if (itemData.itemIcon != null)
            {
                // 绘制背景
                EditorGUI.DrawRect(iconRect, itemData.backgroundColor);
                // 绘制图标
                GUI.DrawTexture(iconRect, itemData.itemIcon.texture, ScaleMode.ScaleToFit);
            }
            else
            {
                EditorGUI.DrawRect(iconRect, Color.gray);
                GUI.Label(iconRect, "无图标", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.LabelField("物品图标", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();

            // 右侧：基本信息
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("物品信息概览", EditorStyles.boldLabel);
            // 显示三位数格式的ID
            EditorGUILayout.LabelField($"ID: {itemData.id}");
            EditorGUILayout.LabelField($"名称: {itemData.itemName}");
            EditorGUILayout.LabelField($"简称: {itemData.shortName}");
            EditorGUILayout.LabelField($"分类: {GetCategoryDisplayName(itemData.category)}");
            EditorGUILayout.LabelField($"稀有度: {GetRarityDisplayName(itemData.rarity)}");
            EditorGUILayout.LabelField($"尺寸: {itemData.width} × {itemData.height}");
            if (itemData.GlobalId > 0)
            {
                EditorGUILayout.LabelField($"全局ID: {itemData.GlobalId}");
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawGridPreview()
        {
            const float cellSize = 20f;
            const float gridSpacing = 2f;

            EditorGUILayout.LabelField("物品占用网格", EditorStyles.boldLabel);

            // 计算网格总尺寸
            float gridWidth = itemData.width * cellSize + (itemData.width - 1) * gridSpacing;
            float gridHeight = itemData.height * cellSize + (itemData.height - 1) * gridSpacing;

            // 绘制物品占用网格
            Rect gridRect = GUILayoutUtility.GetRect(gridWidth + 20, gridHeight + 20);
            Rect gridArea = new Rect(gridRect.x + 10, gridRect.y + 10, gridWidth, gridHeight);

            // 绘制网格背景
            EditorGUI.DrawRect(new Rect(gridArea.x - 2, gridArea.y - 2, gridArea.width + 4, gridArea.height + 4), Color.black);

            // 绘制每个格子
            for (int x = 0; x < itemData.width; x++)
            {
                for (int y = 0; y < itemData.height; y++)
                {
                    Rect cellRect = new Rect(
                        gridArea.x + x * (cellSize + gridSpacing),
                        gridArea.y + y * (cellSize + gridSpacing),
                        cellSize,
                        cellSize
                    );

                    // 使用物品背景颜色绘制格子
                    EditorGUI.DrawRect(cellRect, itemData.backgroundColor);

                    // 绘制格子边框
                    EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, cellRect.width, 1), Color.white);
                    EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, 1, cellRect.height), Color.white);
                    EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.yMax - 1, cellRect.width, 1), Color.white);
                    EditorGUI.DrawRect(new Rect(cellRect.xMax - 1, cellRect.y, 1, cellRect.height), Color.white);
                }
            }

            EditorGUILayout.LabelField($"物品尺寸: {itemData.width} × {itemData.height} 格", EditorStyles.helpBox);

            // 如果是容器类物品，显示容量网格
            if (itemData.IsContainer())
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("容器容量网格", EditorStyles.boldLabel);

                // 计算容器网格尺寸
                float containerGridWidth = itemData.cellH * cellSize + (itemData.cellH - 1) * gridSpacing;
                float containerGridHeight = itemData.cellV * cellSize + (itemData.cellV - 1) * gridSpacing;

                // 绘制容器容量网格
                Rect containerGridRect = GUILayoutUtility.GetRect(containerGridWidth + 20, containerGridHeight + 20);
                Rect containerGridArea = new Rect(containerGridRect.x + 10, containerGridRect.y + 10, containerGridWidth, containerGridHeight);

                // 绘制容器网格背景
                EditorGUI.DrawRect(new Rect(containerGridArea.x - 2, containerGridArea.y - 2, containerGridArea.width + 4, containerGridArea.height + 4), Color.black);

                // 绘制容器每个格子
                Color containerCellColor = itemData.backgroundColor * 0.3f; // 使用较淡的背景色
                containerCellColor.a = 1f;

                for (int x = 0; x < itemData.cellH; x++)
                {
                    for (int y = 0; y < itemData.cellV; y++)
                    {
                        Rect cellRect = new Rect(
                            containerGridArea.x + x * (cellSize + gridSpacing),
                            containerGridArea.y + y * (cellSize + gridSpacing),
                            cellSize,
                            cellSize
                        );

                        // 使用较淡的背景颜色绘制容器格子
                        EditorGUI.DrawRect(cellRect, containerCellColor);

                        // 绘制格子边框
                        EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, cellRect.width, 1), Color.gray);
                        EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, 1, cellRect.height), Color.gray);
                        EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.yMax - 1, cellRect.width, 1), Color.gray);
                        EditorGUI.DrawRect(new Rect(cellRect.xMax - 1, cellRect.y, 1, cellRect.height), Color.gray);
                    }
                }

                EditorGUILayout.LabelField($"容器容量: {itemData.cellH} × {itemData.cellV} = {itemData.cellH * itemData.cellV} 格", EditorStyles.helpBox);
            }
        }

        private void DrawBasicInfo()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("id"), new GUIContent("物品ID"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("itemName"), new GUIContent("物品名称"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("shortName"), new GUIContent("物品简称"));

            // 分类下拉菜单
            EditorGUILayout.PropertyField(serializedObject.FindProperty("category"), new GUIContent("物品分类"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("rarity"), new GUIContent("稀有度 (1-4)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("backgroundColor"), new GUIContent("背景颜色"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("itemIcon"), new GUIContent("物品图标"));
        }

        private void DrawSizeInfo()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("height"), new GUIContent("物品高度"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("width"), new GUIContent("物品宽度"));
        }

        private void DrawSpecialAttributes()
        {
            switch (itemData.category)
            {
                case ItemCategory.Helmet:
                case ItemCategory.Armor:
                    EditorGUILayout.LabelField("装备属性", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("durability"), new GUIContent("耐久度"));
                    break;

                case ItemCategory.TacticalRig:
                case ItemCategory.Backpack:
                    EditorGUILayout.LabelField("容器属性", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("cellH"), new GUIContent("水平格子数"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("cellV"), new GUIContent("垂直格子数"));
                    EditorGUILayout.LabelField($"总容量: {itemData.cellH * itemData.cellV} 格", EditorStyles.helpBox);

                    // 如果有耐久度也显示
                    if (itemData.durability > 0)
                    {
                        EditorGUILayout.Space(5);
                        EditorGUILayout.LabelField("装备属性", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("durability"), new GUIContent("耐久度"));
                    }
                    break;

                case ItemCategory.Weapon:
                    EditorGUILayout.LabelField("武器属性", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("durability"), new GUIContent("耐久度"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("ammunitionType"), new GUIContent("弹药类型"));
                    break;

                case ItemCategory.Ammunition:
                    EditorGUILayout.LabelField("弹药属性", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("ammunitionType"), new GUIContent("弹药类型"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("maxStack"), new GUIContent("最大堆叠数量"));
                    break;

                case ItemCategory.Food:
                case ItemCategory.Drink:
                case ItemCategory.Sedative:
                case ItemCategory.Hemostatic:
                    EditorGUILayout.LabelField("消耗品属性", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("usageCount"), new GUIContent("使用次数"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("maxStack"), new GUIContent("最大堆叠数量"));
                    break;

                case ItemCategory.Currency:
                    EditorGUILayout.LabelField("货币属性", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("maxStack"), new GUIContent("最大堆叠数量"));
                    break;

                case ItemCategory.Healing:
                    EditorGUILayout.LabelField("治疗属性", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("maxHealAmount"), new GUIContent("最大治疗量"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("usageCount"), new GUIContent("使用次数"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("maxStack"), new GUIContent("最大堆叠数量"));
                    break;

                case ItemCategory.Intelligence:
                    EditorGUILayout.LabelField("情报属性", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("intelligenceValue"), new GUIContent("智力值"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("usageCount"), new GUIContent("使用次数"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("maxStack"), new GUIContent("最大堆叠数量"));
                    break;

                default:
                    EditorGUILayout.LabelField("无特殊属性", EditorStyles.helpBox);
                    break;
            }
        }

        private void DrawRuntimeInfo()
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.LabelField("全局唯一ID", itemData.GlobalId.ToString());
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("物品状态检查", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"是否为容器: {(itemData.IsContainer() ? "是" : "否")}");
            EditorGUILayout.LabelField($"是否可堆叠: {(itemData.IsStackable() ? "是" : "否")}");
            EditorGUILayout.LabelField($"是否为消耗品: {(itemData.IsConsumable() ? "是" : "否")}");
            EditorGUILayout.LabelField($"是否有耐久度: {(itemData.HasDurability() ? "是" : "否")}");
        }

        private void DrawUtilityButtons()
        {
            EditorGUILayout.LabelField("实用工具", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("根据稀有度设置背景色"))
            {
                itemData.SetBackgroundColorByRarity();
                EditorUtility.SetDirty(itemData);
            }

            if (GUILayout.Button("创建实例副本"))
            {
                ItemDataSO instance = itemData.CreateInstance();
                string path = EditorUtility.SaveFilePanelInProject(
                    "保存物品实例",
                    $"{itemData.itemName}_Instance",
                    "asset",
                    "选择保存位置");

                if (!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.CreateAsset(instance, path);
                    AssetDatabase.SaveAssets();
                    EditorUtility.FocusProjectWindow();
                    Selection.activeObject = instance;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private string GetCategoryDisplayName(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Helmet: return "头盔";
                case ItemCategory.Armor: return "护甲";
                case ItemCategory.TacticalRig: return "战术背心";
                case ItemCategory.Backpack: return "背包";
                case ItemCategory.Weapon: return "武器";
                case ItemCategory.Ammunition: return "弹药";
                case ItemCategory.Food: return "食物";
                case ItemCategory.Drink: return "饮料";
                case ItemCategory.Sedative: return "镇静剂";
                case ItemCategory.Hemostatic: return "止血剂";
                case ItemCategory.Healing: return "治疗";
                case ItemCategory.Intelligence: return "情报";
                case ItemCategory.Currency: return "货币";
                default: return category.ToString();
            }
        }

        private string GetRarityDisplayName(string rarity)
        {
            switch (rarity)
            {
                case "1": return "普通";
                case "2": return "稀有";
                case "3": return "史诗";
                case "4": return "传说";
                default: return $"未知({rarity})";
            }
        }
    }
}