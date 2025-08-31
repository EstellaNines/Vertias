using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace InventorySystem.Editor
{
    public class ItemPrefabGenerator : EditorWindow
    {
        // 默认路径
        private const string DEFAULT_ITEM_DATA_PATH = "Assets/InventorySystem/GeneratedItems";
        private const string DEFAULT_PREFAB_OUTPUT_PATH = "Assets/InventorySystem/Prefabs";
        private const string DEFAULT_FONT_ASSET_PATH = "Assets/TextMesh Pro/Fonts/SILVER.TTF";

        // 用户自定义路径
        private string itemDataPath = DEFAULT_ITEM_DATA_PATH;
        private string prefabOutputPath = DEFAULT_PREFAB_OUTPUT_PATH;
        private TMP_FontAsset fontAsset;

        // 分类文件夹映射（与ItemDataSOGenerator保持一致）
        private static readonly Dictionary<string, string> CategoryFolderNames = new Dictionary<string, string>
        {
            { "Helmet", "Helmet_头盔" },
            { "Armor", "Armor_护甲" },
            { "TacticalRig", "TacticalRig_战术背心" },
            { "Backpack", "Backpack_背包" },
            { "Weapon", "Weapon_武器" },
            { "Ammunition", "Ammunition_弹药" },
            { "Food", "Food_食物" },
            { "Drink", "Drink_饮料" },
            { "Sedative", "Sedative_镇静剂" },
            { "Hemostatic", "Hemostatic_止血剂" },
            { "Healing", "Healing_治疗药物" },
            { "Intelligence", "Intelligence_情报" },
            { "Currency", "Currency_货币" }
        };

        [MenuItem("Inventory System/Item Prefab Generator")]
        public static void ShowWindow()
        {
            GetWindow<ItemPrefabGenerator>("物品预制体生成器");
        }

        private void OnEnable()
        {
            // 从EditorPrefs中恢复保存的路径设置
            itemDataPath = EditorPrefs.GetString("ItemPrefabGenerator_ItemDataPath", DEFAULT_ITEM_DATA_PATH);
            prefabOutputPath = EditorPrefs.GetString("ItemPrefabGenerator_PrefabOutputPath", DEFAULT_PREFAB_OUTPUT_PATH);

            // 尝试加载默认字体
            LoadDefaultFont();
        }

        private void LoadDefaultFont()
        {
            // 查找TextMeshPro默认字体
            string[] fontGuids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            foreach (string guid in fontGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
                if (font != null)
                {
                    fontAsset = font;
                    break;
                }
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("物品UI预制体生成器", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // 路径设置
            GUILayout.Label("路径设置:", EditorStyles.boldLabel);

            // ItemDataSO路径设置
            GUILayout.BeginHorizontal();
            GUILayout.Label("物品数据路径:", GUILayout.Width(100));
            string newItemDataPath = EditorGUILayout.TextField(itemDataPath);
            if (newItemDataPath != itemDataPath)
            {
                itemDataPath = newItemDataPath;
                EditorPrefs.SetString("ItemPrefabGenerator_ItemDataPath", itemDataPath);
            }
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("选择物品数据文件夹", "Assets", "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    if (selectedPath.StartsWith(Application.dataPath))
                    {
                        selectedPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                    }
                    itemDataPath = selectedPath;
                    EditorPrefs.SetString("ItemPrefabGenerator_ItemDataPath", itemDataPath);
                }
            }
            GUILayout.EndHorizontal();

            // 预制体输出路径设置
            GUILayout.BeginHorizontal();
            GUILayout.Label("预制体输出路径:", GUILayout.Width(100));
            string newPrefabOutputPath = EditorGUILayout.TextField(prefabOutputPath);
            if (newPrefabOutputPath != prefabOutputPath)
            {
                prefabOutputPath = newPrefabOutputPath;
                EditorPrefs.SetString("ItemPrefabGenerator_PrefabOutputPath", prefabOutputPath);
            }
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("选择预制体输出文件夹", "Assets", "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    if (selectedPath.StartsWith(Application.dataPath))
                    {
                        selectedPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                    }
                    prefabOutputPath = selectedPath;
                    EditorPrefs.SetString("ItemPrefabGenerator_PrefabOutputPath", prefabOutputPath);
                }
            }
            GUILayout.EndHorizontal();

            // 字体设置
            GUILayout.BeginHorizontal();
            GUILayout.Label("TMP字体资源:", GUILayout.Width(100));
            fontAsset = (TMP_FontAsset)EditorGUILayout.ObjectField(fontAsset, typeof(TMP_FontAsset), false);
            GUILayout.EndHorizontal();

            // 重置按钮
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("重置为默认路径", GUILayout.Width(120)))
            {
                itemDataPath = DEFAULT_ITEM_DATA_PATH;
                prefabOutputPath = DEFAULT_PREFAB_OUTPUT_PATH;
                EditorPrefs.SetString("ItemPrefabGenerator_ItemDataPath", itemDataPath);
                EditorPrefs.SetString("ItemPrefabGenerator_PrefabOutputPath", prefabOutputPath);
                LoadDefaultFont();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // 显示当前设置
            GUILayout.Label("当前设置:", EditorStyles.boldLabel);
            GUILayout.Label($"物品数据路径: {itemDataPath}");
            GUILayout.Label($"预制体输出路径: {prefabOutputPath}");
            GUILayout.Label($"字体资源: {(fontAsset != null ? fontAsset.name : "未设置")}");

            // 验证路径
            bool itemDataExists = Directory.Exists(itemDataPath);
            if (!itemDataExists)
            {
                EditorGUILayout.HelpBox($"警告: 找不到物品数据文件夹 {itemDataPath}", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox($"物品数据文件夹已找到: {itemDataPath}", MessageType.Info);
            }

            if (fontAsset == null)
            {
                EditorGUILayout.HelpBox("警告: 未设置TMP字体资源", MessageType.Warning);
            }

            GUILayout.Space(10);

            // 生成和清理按钮
            EditorGUI.BeginDisabledGroup(!itemDataExists || fontAsset == null);
            if (GUILayout.Button("生成所有物品UI预制体", GUILayout.Height(30)))
            {
                GenerateAllItemPrefabs();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(10);

            if (GUILayout.Button("清理所有已生成的预制体", GUILayout.Height(30)))
            {
                ClearAllGeneratedPrefabs();
            }

            GUILayout.Space(20);
            GUILayout.Label("说明:", EditorStyles.boldLabel);
            GUILayout.Label("? 从ItemDataSO为每个物品生成对应的UI预制体");
            GUILayout.Label("? 预制体包含ItemBackground、ItemIcon和TMP文本");
            GUILayout.Label("? 根据物品类型显示相应数值（弹药、货币等）");
            GUILayout.Label("? 按分类文件夹存储预制体");
            GUILayout.Label("? TMP文本格式为居右与文本框居中");
        }

        private void GenerateAllItemPrefabs()
        {
            try
            {
                if (!Directory.Exists(itemDataPath))
                {
                    EditorUtility.DisplayDialog("错误", $"无法找到物品数据文件夹: {itemDataPath}", "确定");
                    return;
                }

                // 确保预制体输出目录存在
                if (!Directory.Exists(prefabOutputPath))
                {
                    Directory.CreateDirectory(prefabOutputPath);
                }

                int totalItems = 0;
                int generatedPrefabs = 0;

                // 遍历所有分类文件夹
                string[] categoryFolders = Directory.GetDirectories(itemDataPath);
                foreach (string categoryFolder in categoryFolders)
                {
                    string categoryName = Path.GetFileName(categoryFolder);

                    // 创建对应的预制体分类文件夹
                    string prefabCategoryPath = Path.Combine(prefabOutputPath, categoryName);
                    if (!Directory.Exists(prefabCategoryPath))
                    {
                        Directory.CreateDirectory(prefabCategoryPath);
                    }

                    // 查找该分类下的所有ItemDataSO文件
                    string[] assetFiles = Directory.GetFiles(categoryFolder, "*.asset");
                    foreach (string assetFile in assetFiles)
                    {
                        string relativePath = assetFile.Replace("\\", "/");
                        if (relativePath.StartsWith(Application.dataPath))
                        {
                            relativePath = "Assets" + relativePath.Substring(Application.dataPath.Length);
                        }

                        ItemDataSO itemData = AssetDatabase.LoadAssetAtPath<ItemDataSO>(relativePath);
                        if (itemData != null)
                        {
                            totalItems++;
                            if (GenerateItemPrefab(itemData, prefabCategoryPath))
                            {
                                generatedPrefabs++;
                            }
                        }
                    }
                }

                // 刷新资产数据库
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("完成",
                    $"物品预制体生成完成！\n总物品数: {totalItems}\n成功生成: {generatedPrefabs}\n输出路径: {prefabOutputPath}",
                    "确定");

                Debug.Log($"[ItemPrefabGenerator] 生成完成: {generatedPrefabs}/{totalItems} 个预制体");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误", $"生成过程中发生错误: {e.Message}", "确定");
                Debug.LogError($"[ItemPrefabGenerator] 生成错误: {e}");
            }
        }

        private bool GenerateItemPrefab(ItemDataSO itemData, string outputPath)
        {
            try
            {
                // 固定网格大小为64x64像素
                float gridSize = 64f;
                float itemWidth = itemData.width * gridSize;
                float itemHeight = itemData.height * gridSize;
                Vector2 itemSize = new Vector2(itemWidth, itemHeight);
        
                // 创建根GameObject
                GameObject rootObject = new GameObject(itemData.itemName);
                rootObject.layer = 5; // UI层
        
                // 添加RectTransform组件
                RectTransform rootRect = rootObject.AddComponent<RectTransform>();
                rootRect.sizeDelta = itemSize;
                rootRect.anchorMin = new Vector2(0.5f, 0.5f);
                rootRect.anchorMax = new Vector2(0.5f, 0.5f);
                rootRect.pivot = new Vector2(0.5f, 0.5f); // 轴心为(0.5,0.5)
        
                // 创建ItemBackground子对象
                GameObject backgroundObject = new GameObject("ItemBackground");
                backgroundObject.transform.SetParent(rootObject.transform);
                backgroundObject.layer = 5;
        
                RectTransform backgroundRect = backgroundObject.AddComponent<RectTransform>();
                backgroundRect.sizeDelta = itemSize; // 背景大小与主对象相同
                backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
                backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
                backgroundRect.pivot = new Vector2(0.5f, 0.5f); // 轴心为(0.5,0.5)
                backgroundRect.anchoredPosition = Vector2.zero; // 居中位置
        
                Image backgroundImage = backgroundObject.AddComponent<Image>();
                Color backgroundColor = itemData.backgroundColor;
                backgroundColor.a = 0.8f; // 设置透明度为0.8
                backgroundImage.color = backgroundColor;
                backgroundImage.raycastTarget = true;
        
                // 创建ItemHighlight子对象
                GameObject highlightObject = new GameObject("ItemHighlight");
                highlightObject.transform.SetParent(rootObject.transform);
                highlightObject.layer = 5;
        
                RectTransform highlightRect = highlightObject.AddComponent<RectTransform>();
                highlightRect.sizeDelta = itemSize; // 高亮大小与主对象相同
                highlightRect.anchorMin = new Vector2(0.5f, 0.5f);
                highlightRect.anchorMax = new Vector2(0.5f, 0.5f);
                highlightRect.pivot = new Vector2(0.5f, 0.5f); // 居中对齐
                highlightRect.anchoredPosition = Vector2.zero; // 居中位置
        
                Image highlightImage = highlightObject.AddComponent<Image>();
                highlightImage.color = new Color(1f, 1f, 1f, 0f); // 初始透明度为0
                highlightImage.raycastTarget = true;
        
                // 创建ItemIcon子对象
                GameObject iconObject = new GameObject("ItemIcon");
                iconObject.transform.SetParent(rootObject.transform);
                iconObject.layer = 5;
        
                RectTransform iconRect = iconObject.AddComponent<RectTransform>();
                iconRect.sizeDelta = itemSize; // 图标大小与主对象相同
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f); // 轴心为(0.5,0.5)
                iconRect.anchoredPosition = Vector2.zero; // 居中位置
        
                Image iconImage = iconObject.AddComponent<Image>();
                iconImage.sprite = itemData.itemIcon;
                iconImage.color = Color.white;
                iconImage.raycastTarget = false; // 禁用图标的射线检测
        
                // 创建TMP文本组件（始终位于右下角）
                TextMeshProUGUI tmpText = null;
                string textContent = GetItemDisplayText(itemData);
                
                // 始终创建文本组件
                GameObject textObject = new GameObject("ItemText");
                textObject.transform.SetParent(rootObject.transform);
                textObject.layer = 5;
        
                RectTransform textRect = textObject.AddComponent<RectTransform>();
                // 文本框大小：固定为右下角区域的合适大小
                float textWidth = Mathf.Max(itemWidth * 0.4f, 24f); // 至少24像素宽
                float textHeight = Mathf.Max(itemHeight * 0.3f, 16f); // 至少16像素高
                textRect.sizeDelta = new Vector2(textWidth, textHeight);
                textRect.anchorMin = new Vector2(0.5f, 0.5f);
                textRect.anchorMax = new Vector2(0.5f, 0.5f);
                textRect.pivot = new Vector2(1f, 0f); // 右下角对齐
                
                // 确保文本始终位于右下角，距离边缘3像素
                // 使用绝对位置确保无论物品如何旋转，文字都在视觉右下角
                float textPosX = itemWidth / 2f - 3f; // 距离右边缘3像素
                float textPosY = -itemHeight / 2f + 3f; // 距离下边缘3像素
                textRect.anchoredPosition = new Vector2(textPosX, textPosY);
                
                // 设置文本对象的名称，方便在运行时查找
                textObject.name = "ItemText";
        
                tmpText = textObject.AddComponent<TextMeshProUGUI>();
                tmpText.text = textContent;
                tmpText.font = fontAsset;
                // 根据物品实际网格大小动态计算字体大小
                float itemGridSize = Mathf.Max(itemData.width, itemData.height); // 取宽高的最大值
                float baseFontSize = 16f + (itemGridSize - 1) * 8f; // 1格=16号，2格=24号，3格=32号
                tmpText.fontSize = Mathf.Clamp(baseFontSize, 16f, 48f); // 限制在16-48之间
                tmpText.color = Color.white;
                tmpText.alignment = TextAlignmentOptions.BottomRight; // 右下对齐
                tmpText.raycastTarget = false;
                tmpText.enableWordWrapping = false;
                tmpText.overflowMode = TextOverflowModes.Overflow;
                
                // 确保文本渲染在最上层
                textObject.transform.SetAsLastSibling();
        
                // 添加ItemDataReader脚本到主对象
                ItemDataReader itemDataReader = rootObject.AddComponent<ItemDataReader>();
        
                // 添加Item脚本到主对象
                Item item = rootObject.AddComponent<Item>();
        
                // 添加DraggableItem脚本到主对象
                DraggableItem draggableItem = rootObject.AddComponent<DraggableItem>();
        
                // 添加ItemHighlight脚本到主对象
                ItemHighlight itemHighlight = rootObject.AddComponent<ItemHighlight>();
        
                // 设置ItemHighlight组件的highlightImage引用
                var highlightImageField = typeof(ItemHighlight).GetField("highlightImage",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var highlightColorField = typeof(ItemHighlight).GetField("highlightColor",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var fadeSpeedField = typeof(ItemHighlight).GetField("fadeSpeed",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
                if (highlightImageField != null)
                    highlightImageField.SetValue(itemHighlight, highlightImage);
                if (highlightColorField != null)
                    highlightColorField.SetValue(itemHighlight, new Color(1f, 1f, 1f, 0.3f));
                if (fadeSpeedField != null)
                    fadeSpeedField.SetValue(itemHighlight, 2f);
        
                // 先设置UI组件引用（在SetItemData之前）
                var backgroundImageField = typeof(ItemDataReader).GetField("backgroundImage",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var iconImageField = typeof(ItemDataReader).GetField("iconImage",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var displayTextField = typeof(ItemDataReader).GetField("displayText",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
                if (backgroundImageField != null)
                    backgroundImageField.SetValue(itemDataReader, backgroundImage);
                if (iconImageField != null)
                    iconImageField.SetValue(itemDataReader, iconImage);
                if (displayTextField != null && tmpText != null)
                    displayTextField.SetValue(itemDataReader, tmpText);
        
                // 设置物品数据（此时UI组件引用已经正确设置）
                itemDataReader.SetItemData(itemData);
        
                // 直接设置所有public数值字段，确保在编辑器中显示完整信息
                itemDataReader.gridWidth = itemData.width;
                itemDataReader.gridHeight = itemData.height;
                itemDataReader.gridSizeDisplay = $"{itemData.width} × {itemData.height}";
                itemDataReader.currentStack = 1;
                itemDataReader.currentDurability = itemData.durability;
                itemDataReader.currentUsageCount = itemData.usageCount;
                itemDataReader.currentHealAmount = itemData.maxHealAmount;
                itemDataReader.intelligenceValue = itemData.intelligenceValue;
                itemDataReader.currencyAmount = 1;
                itemDataReader.maxStackAmount = itemData.maxStack;
                itemDataReader.maxDurability = itemData.durability;
                itemDataReader.maxUsageCount = itemData.usageCount;
                itemDataReader.maxHealAmount = itemData.maxHealAmount;
        
                // 生成预制体文件名
                string fileName = $"{itemData.id}__{SanitizeFileName(itemData.itemName)}.prefab";
                string prefabPath = Path.Combine(outputPath, fileName).Replace("\\", "/");
        
                // 创建预制体
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(rootObject, prefabPath);
        
                // 清理临时对象
                DestroyImmediate(rootObject);
        
                return prefab != null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ItemPrefabGenerator] 生成预制体失败 {itemData.itemName}: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 根据物品类型获取要显示的文本内容
        /// </summary>
        private string GetItemDisplayText(ItemDataSO itemData)
        {
            switch (itemData.category)
            {
                case ItemCategory.Helmet:
                case ItemCategory.Armor:
                    // 头盔护甲显示耐久值
                    return itemData.durability > 0 ? itemData.durability.ToString() : "";

                case ItemCategory.Ammunition:
                    // 弹药显示堆叠数量
                    return itemData.maxStack > 1 ? $"{itemData.maxStack}/{itemData.maxStack}" : "";

                case ItemCategory.Currency:
                    // 货币显示堆叠数量
                    return itemData.maxStack > 1 ? itemData.maxStack.ToString() : "";

                case ItemCategory.Food:
                case ItemCategory.Drink:
                case ItemCategory.Sedative:
                case ItemCategory.Hemostatic:
                    // 消耗品显示使用次数（预制体生成时显示最大值）
                    return itemData.usageCount > 0 ? $"{itemData.usageCount}/{itemData.usageCount}" : "";

                case ItemCategory.Healing:
                    // 治疗药物显示治疗量
                    return itemData.maxHealAmount > 0 ? itemData.maxHealAmount.ToString() : "";

                case ItemCategory.Intelligence:
                    // 情报物品显示情报值
                    return itemData.intelligenceValue > 0 ? itemData.intelligenceValue.ToString() : "";

                case ItemCategory.Special:
                    // 特殊物品不显示额外信息
                    return "";

                default:
                    return "";
            }
        }

        private void ClearAllGeneratedPrefabs()
        {
            if (EditorUtility.DisplayDialog("确认删除",
                $"确定要删除所有已生成的预制体吗？\n路径: {prefabOutputPath}\n\n此操作不可撤销！",
                "确认删除", "取消"))
            {
                try
                {
                    if (Directory.Exists(prefabOutputPath))
                    {
                        int deletedFiles = 0;

                        // 递归删除所有子文件夹内的.prefab文件，但保留文件夹结构
                        DeletePrefabFilesRecursively(prefabOutputPath, ref deletedFiles);

                        // 刷新资产数据库
                        AssetDatabase.Refresh();

                        EditorUtility.DisplayDialog("完成", $"已删除 {deletedFiles} 个预制体文件\n文件夹结构已保留", "确定");
                        Debug.Log($"[ItemPrefabGenerator] 已清理 {deletedFiles} 个预制体文件，保留文件夹结构");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("提示", "没有找到需要删除的文件", "确定");
                    }
                }
                catch (Exception e)
                {
                    EditorUtility.DisplayDialog("错误", $"删除过程中发生错误: {e.Message}", "确定");
                    Debug.LogError($"[ItemPrefabGenerator] 删除错误: {e}");
                }
            }
        }

        /// <summary>
        /// 递归删除指定目录及其子目录中的所有.prefab文件，但保留文件夹结构
        /// </summary>
        private void DeletePrefabFilesRecursively(string directoryPath, ref int deletedFiles)
        {
            if (!Directory.Exists(directoryPath))
                return;

            // 删除当前目录下的所有.prefab文件
            string[] prefabFiles = Directory.GetFiles(directoryPath, "*.prefab");
            foreach (string file in prefabFiles)
            {
                File.Delete(file);
                deletedFiles++;

                // 删除对应的.meta文件
                string metaFile = file + ".meta";
                if (File.Exists(metaFile))
                {
                    File.Delete(metaFile);
                }
            }

            // 递归处理所有子目录
            string[] subDirectories = Directory.GetDirectories(directoryPath);
            foreach (string subDir in subDirectories)
            {
                DeletePrefabFilesRecursively(subDir, ref deletedFiles);
            }
        }

        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "Unknown";

            // 移除或替换文件名中的非法字符
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }

            // 移除一些特殊字符
            fileName = fileName.Replace(" ", "_")
                              .Replace(".", "_")
                              .Replace("-", "_")
                              .Replace("(", "")
                              .Replace(")", "")
                              .Replace("（", "")
                              .Replace("）", "");

            return fileName;
        }
    }
}