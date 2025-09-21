using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;

namespace InventorySystem.Editor
{
    public class ItemDataSOGenerator : EditorWindow
    {
        // 默认路径
        private const string DEFAULT_JSON_PATH = "Assets/InventorySystem/Database/ItemDatabase.json";
        private const string DEFAULT_OUTPUT_PATH = "Assets/InventorySystem/GeneratedItems";
        private const string SPRITE_PATH = "Assets/InventorySystem/Sprite";

        // 用户自定义路径
        private string jsonPath = DEFAULT_JSON_PATH;
        private string outputPath = DEFAULT_OUTPUT_PATH;

        private static long globalIdCounter = 1L; // 改为long类型
        
        // 分类文件夹映射（英文+中文）
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
            { "Stimulant", "Stimulant_兴奋剂" },
            { "Healing", "Healing_治疗药物" },
            { "Key", "Key_钥匙" },
            { "Currency", "Currency_货币" },
            { "Intelligence", "Intelligence_情报" }
        };

        // UI 状态
        private Dictionary<string, bool> categorySelection = new Dictionary<string, bool>();
        private Dictionary<string, (int jsonCount, int generatedCount)> categoryOverview = new Dictionary<string, (int, int)>();
        private string lastOperationSummary = string.Empty;
        private Vector2 scrollPosition = Vector2.zero;

        [MenuItem("Inventory System/Item Data SO Generator")]
        public static void ShowWindow()
        {
            GetWindow<ItemDataSOGenerator>("物品数据生成器");
        }

        private void OnEnable()
        {
            // 从EditorPrefs中恢复保存的路径设置
            jsonPath = EditorPrefs.GetString("ItemDataSOGenerator_JsonPath", DEFAULT_JSON_PATH);
            outputPath = EditorPrefs.GetString("ItemDataSOGenerator_OutputPath", DEFAULT_OUTPUT_PATH);

            // 初始化类别选择
            foreach (var kv in CategoryFolderNames)
            {
                if (!categorySelection.ContainsKey(kv.Key))
                {
                    categorySelection[kv.Key] = false;
                }
            }

            RefreshOverview();
        }

        private void OnGUI()
        {
            GUILayout.Label("ScriptableObject 物品数据生成器", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // 垂直滚动区域开始
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, false, true);

            // JSON路径设置
            GUILayout.Label("路径设置:", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            GUILayout.Label("JSON文件路径:", GUILayout.Width(100));
            string newJsonPath = EditorGUILayout.TextField(jsonPath);
            if (newJsonPath != jsonPath)
            {
                jsonPath = newJsonPath;
                EditorPrefs.SetString("ItemDataSOGenerator_JsonPath", jsonPath);
            }
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string selectedPath = EditorUtility.OpenFilePanel("选择JSON文件", "Assets", "json");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // 转换为相对于项目的路径
                    if (selectedPath.StartsWith(Application.dataPath))
                    {
                        selectedPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                    }
                    jsonPath = selectedPath;
                    EditorPrefs.SetString("ItemDataSOGenerator_JsonPath", jsonPath);
                }
            }
            GUILayout.EndHorizontal();

            // 输出路径设置
            GUILayout.BeginHorizontal();
            GUILayout.Label("输出路径:", GUILayout.Width(100));
            string newOutputPath = EditorGUILayout.TextField(outputPath);
            if (newOutputPath != outputPath)
            {
                outputPath = newOutputPath;
                EditorPrefs.SetString("ItemDataSOGenerator_OutputPath", outputPath);
            }
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("选择输出文件夹", "Assets", "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // 转换为相对于项目的路径
                    if (selectedPath.StartsWith(Application.dataPath))
                    {
                        selectedPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                    }
                    outputPath = selectedPath;
                    EditorPrefs.SetString("ItemDataSOGenerator_OutputPath", outputPath);
                }
            }
            GUILayout.EndHorizontal();

            // 重置按钮
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("重置为默认路径", GUILayout.Width(120)))
            {
                jsonPath = DEFAULT_JSON_PATH;
                outputPath = DEFAULT_OUTPUT_PATH;
                EditorPrefs.SetString("ItemDataSOGenerator_JsonPath", jsonPath);
                EditorPrefs.SetString("ItemDataSOGenerator_OutputPath", outputPath);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // 显示当前设置
            GUILayout.Label("当前设置:", EditorStyles.boldLabel);
            GUILayout.Label($"JSON数据路径: {jsonPath}");
            GUILayout.Label($"输出路径: {outputPath}");

            // 验证路径
            bool jsonExists = File.Exists(jsonPath);
            if (!jsonExists)
            {
                EditorGUILayout.HelpBox($"警告: 找不到JSON文件 {jsonPath}", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox($"JSON文件已找到: {jsonPath}", MessageType.Info);
            }

            GUILayout.Space(10);

            // 生成和清理按钮
            EditorGUI.BeginDisabledGroup(!jsonExists);
            if (GUILayout.Button("生成所有物品 ScriptableObject", GUILayout.Height(30)))
            {
                GenerateAllItemDataSO();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(10);

            if (GUILayout.Button("清理所有已生成的 ScriptableObject", GUILayout.Height(30)))
            {
                ClearAllGeneratedItems();
            }

            GUILayout.Space(20);
            GUILayout.Label("类别选择（可按类别生成/删除）", EditorStyles.boldLabel);
            DrawCategorySelectionUI();

            EditorGUI.BeginDisabledGroup(!jsonExists);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("仅生成选中类别", GUILayout.Height(24)))
            {
                GenerateSelectedCategories();
            }
            if (GUILayout.Button("仅删除选中类别", GUILayout.Height(24)))
            {
                DeleteSelectedCategories();
            }
            GUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(10);
            if (!string.IsNullOrEmpty(lastOperationSummary))
            {
                EditorGUILayout.HelpBox(lastOperationSummary, MessageType.Info);
            }

            GUILayout.Space(10);
            if (GUILayout.Button("刷新概况", GUILayout.Height(20)))
            {
                RefreshOverview();
            }

            // 概况表
            DrawOverviewTable();

            GUILayout.Space(20);
            GUILayout.Label("说明:", EditorStyles.boldLabel);
            GUILayout.Label("• 从JSON数据库为每个物品生成对应的ScriptableObject资产");
            GUILayout.Label("• 按分类文件夹存储");
            GUILayout.Label("• 每个物品拥有全局唯一ID");
            GUILayout.Label("• 支持一键清理所有生成的资产");
            GUILayout.Label("• 支持自定义JSON路径和输出路径");
            GUILayout.Label("• 路径设置会自动保存");
            GUILayout.Label("• 根据珍贵等级自动设置背景颜色");
            EditorGUILayout.EndScrollView();
        }

        private void GenerateAllItemDataSO()
        {
            try
            {
                // 直接从文件系统读取JSON数据
                if (!File.Exists(jsonPath))
                {
                    EditorUtility.DisplayDialog("错误", $"无法找到JSON文件: {jsonPath}", "确定");
                    return;
                }

                string jsonContent = File.ReadAllText(jsonPath);
                JObject jsonData = JObject.Parse(jsonContent);
                JObject categories = jsonData["category"] as JObject;

                if (categories == null)
                {
                    EditorUtility.DisplayDialog("错误", "JSON数据格式错误，找不到category节点", "确定");
                    return;
                }

                // 按现有资产初始化全局ID计数器（避免与已存在资产ID重叠）
                InitializeGlobalIdCounterFromExisting();

                // 确保输出目录存在
                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }

                int totalItems = 0;
                int generatedItems = 0;

                // 遍历所有分类
                foreach (var categoryPair in categories)
                {
                    string categoryName = categoryPair.Key;
                    JObject categoryData = categoryPair.Value as JObject;

                    if (categoryData == null) continue;

                    // 获取分类ID和物品列表
                    int categoryId = categoryData["id"]?.Value<int>() ?? 0;
                    JArray items = categoryData["items"] as JArray;

                    if (items == null) continue;

                    // 创建分类文件夹（英文+中文命名）
                    string folderName = CategoryFolderNames.ContainsKey(categoryName) 
                        ? CategoryFolderNames[categoryName] 
                        : categoryName;
                    string categoryPath = Path.Combine(outputPath, folderName);
                    if (!Directory.Exists(categoryPath))
                    {
                        Directory.CreateDirectory(categoryPath);
                    }

                    // 生成该分类下的所有物品
                    foreach (JObject item in items)
                    {
                        totalItems++;
                        if (GenerateItemDataSO(item, categoryName, (ItemCategory)categoryId, categoryPath))
                        {
                            generatedItems++;
                        }
                    }
                }

                // 刷新资产数据库
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("完成",
                    $"物品数据生成完成！\n总物品数: {totalItems}\n成功生成: {generatedItems}\n输出路径: {outputPath}",
                    "确定");

                Debug.Log($"[ItemDataSOGenerator] 生成完成: {generatedItems}/{totalItems} 个物品");

                lastOperationSummary = $"全量生成完成：成功 {generatedItems}/{totalItems}";
                RefreshOverview();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误", $"生成过程中发生错误: {e.Message}", "确定");
                Debug.LogError($"[ItemDataSOGenerator] 生成错误: {e}");
            }
        }

        private void GenerateSelectedCategories()
        {
            try
            {
                if (!File.Exists(jsonPath))
                {
                    EditorUtility.DisplayDialog("错误", $"无法找到JSON文件: {jsonPath}", "确定");
                    return;
                }

                string jsonContent = File.ReadAllText(jsonPath);
                JObject jsonData = JObject.Parse(jsonContent);
                JObject categories = jsonData["category"] as JObject;
                if (categories == null)
                {
                    EditorUtility.DisplayDialog("错误", "JSON数据格式错误，找不到category节点", "确定");
                    return;
                }

                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }

                // 按现有资产初始化全局ID计数器（避免与已存在资产ID重叠）
                InitializeGlobalIdCounterFromExisting();

                int total = 0; int done = 0;

                foreach (var categoryPair in categories)
                {
                    string categoryName = categoryPair.Key;
                    if (!categorySelection.ContainsKey(categoryName) || !categorySelection[categoryName]) continue;

                    JObject categoryData = categoryPair.Value as JObject;
                    if (categoryData == null) continue;
                    int categoryId = categoryData["id"]?.Value<int>() ?? 0;
                    JArray items = categoryData["items"] as JArray;
                    if (items == null) continue;

                    string folderName = CategoryFolderNames.ContainsKey(categoryName) ? CategoryFolderNames[categoryName] : categoryName;
                    string categoryPath = Path.Combine(outputPath, folderName);
                    if (!Directory.Exists(categoryPath)) Directory.CreateDirectory(categoryPath);

                    foreach (JObject item in items)
                    {
                        total++;
                        if (GenerateItemDataSO(item, categoryName, (ItemCategory)categoryId, categoryPath)) done++;
                    }
                }

                AssetDatabase.Refresh();
                lastOperationSummary = $"按类别生成完成：成功 {done}/{total}";
                EditorUtility.DisplayDialog("完成", lastOperationSummary, "确定");
                RefreshOverview();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误", $"生成过程中发生错误: {e.Message}", "确定");
                Debug.LogError($"[ItemDataSOGenerator] 生成错误: {e}");
            }
        }

        private bool GenerateItemDataSO(JObject itemJson, string categoryName, ItemCategory category, string outputPath)
        {
            try
            {
                // 创建ScriptableObject实例
                ItemDataSO itemData = CreateInstance<ItemDataSO>();

                // 设置基础信息
                // 支持新的三位数ID格式，从JSON读取为字符串后转换为int
                string idStr = itemJson["id"]?.Value<string>() ?? "0";
                itemData.id = int.Parse(idStr);
                itemData.itemName = itemJson["name"]?.Value<string>() ?? "";
                itemData.shortName = itemJson["shortName"]?.Value<string>() ?? "";
                itemData.category = category;
                itemData.rarity = itemJson["rarity"]?.Value<string>() ?? "1";
                
                // 查找并设置精灵图片资源
                string iconName = itemJson["ItemIcon"]?.Value<string>() ?? "";
                itemData.itemIcon = FindSpriteByName(iconName);

                // 设置尺寸属性
                itemData.height = itemJson["height"]?.Value<int>() ?? 1;
                itemData.width = itemJson["width"]?.Value<int>() ?? 1;

                // 设置装备属性
                itemData.durability = itemJson["durability"]?.Value<int>() ?? 0;

                // 设置容器属性
                itemData.cellH = itemJson["cellH"]?.Value<int>() ?? 0;
                itemData.cellV = itemJson["cellV"]?.Value<int>() ?? 0;

                // 设置弹药属性
                itemData.ammunitionType = itemJson["type"]?.Value<string>() ?? "";
                itemData.maxStack = itemJson["maxStack"]?.Value<int>() ?? 1;

                // 设置消耗品属性
                itemData.usageCount = itemJson["usageCount"]?.Value<int>() ?? 0;

                // 设置治疗属性
                itemData.maxHealAmount = itemJson["maxHealAmount"]?.Value<int>() ?? 0;
                itemData.healPerUse = itemJson["healPerUse"]?.Value<int>() ?? 0;

                // 生存恢复属性（食物/饮品/药品等）
                itemData.hungerRestore = itemJson["hungerRestore"]?.Value<int>() ?? 0;
                itemData.mentalRestore = itemJson["mentalRestore"]?.Value<int>() ?? 0;

                // 设置情报属性
                itemData.intelligenceValue = itemJson["intelligenceValue"]?.Value<int>() ?? 0;

                // 设置唯一ID（现在是long类型）
                itemData.SetGlobalId(globalIdCounter++);
                
                // 根据珍贵等级自动设置背景颜色
                itemData.SetBackgroundColorByRarity();

                // 读取武器扩展（weapon 节点）
                if (category == ItemCategory.Weapon)
                {
                    JObject weaponNode = itemJson["weapon"] as JObject;
                    if (weaponNode != null)
                    {
                        if (itemData.weapon == null) itemData.weapon = new ItemDataSO.WeaponSpec();

                        itemData.weapon.weaponPrefabAddress = weaponNode["weaponPrefabAddress"]?.Value<string>() ?? string.Empty;
                        itemData.weapon.playerBulletPrefabAddress = weaponNode["playerBulletPrefabAddress"]?.Value<string>() ?? string.Empty;
                        itemData.weapon.enemyBulletPrefabAddress = weaponNode["enemyBulletPrefabAddress"]?.Value<string>() ?? string.Empty;

                        itemData.weapon.fireMode = weaponNode["fireMode"]?.Value<string>() ?? string.Empty;
                        itemData.weapon.fireRate = weaponNode["fireRate"]?.Value<float>() ?? 0f;
                        itemData.weapon.spreadAngle = weaponNode["spreadAngle"]?.Value<float>() ?? 0f;
                        itemData.weapon.bulletSpeed = weaponNode["bulletSpeed"]?.Value<float>() ?? 0f;
                        itemData.weapon.range = weaponNode["range"]?.Value<float>() ?? 0f;
                        itemData.weapon.damage = weaponNode["damage"]?.Value<float>() ?? 0f;
                        itemData.weapon.magazineCapacity = weaponNode["magazineCapacity"]?.Value<int>() ?? 0;
                        itemData.weapon.reloadTime = weaponNode["reloadTime"]?.Value<float>() ?? 0f;
                    }
                }

                // 生成文件名（ID_物品名称格式）
                string fileName = $"{itemData.id}_{SanitizeFileName(itemData.itemName)}.asset";
                string assetPath = Path.Combine(outputPath, fileName).Replace("\\", "/");

                // 创建资产文件
                AssetDatabase.CreateAsset(itemData, assetPath);

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ItemDataSOGenerator] 生成物品失败 {itemJson["id"]}: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 根据名称查找精灵图片
        /// </summary>
        private Sprite FindSpriteByName(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
                return null;

            // 在精灵文件夹中查找匹配的精灵
            string[] guids = AssetDatabase.FindAssets($"{spriteName} t:Sprite", new[] { SPRITE_PATH });
            
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                
                if (sprite != null && sprite.name.Contains(spriteName))
                {
                    return sprite;
                }
            }
            
            // 如果没找到精确匹配，尝试模糊匹配
            guids = AssetDatabase.FindAssets("t:Sprite", new[] { SPRITE_PATH });
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                
                if (sprite != null && (sprite.name.Contains(spriteName) || spriteName.Contains(sprite.name)))
                {
                    return sprite;
                }
            }
            
            Debug.LogWarning($"[ItemDataSOGenerator] 未找到匹配的精灵: {spriteName}");
            return null;
        }

        private void ClearAllGeneratedItems()
        {
            if (EditorUtility.DisplayDialog("确认删除",
                $"确定要删除所有已生成的ScriptableObject资产吗？\n路径: {outputPath}\n\n此操作不可撤销！",
                "确认删除", "取消"))
            {
                try
                {
                    if (Directory.Exists(outputPath))
                    {
                        int deletedFiles = 0;
                        
                        // 递归删除所有子文件夹内的.asset文件，但保留文件夹结构
                        DeleteAssetFilesRecursively(outputPath, ref deletedFiles);

                        // 刷新资产数据库
                        AssetDatabase.Refresh();

                        EditorUtility.DisplayDialog("完成", $"已删除 {deletedFiles} 个ScriptableObject资产\n文件夹结构已保留", "确定");
                        Debug.Log($"[ItemDataSOGenerator] 已清理 {deletedFiles} 个物品数据文件，保留文件夹结构");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("提示", "没有找到需要删除的文件", "确定");
                    }
                }
                catch (Exception e)
                {
                    EditorUtility.DisplayDialog("错误", $"删除过程中发生错误: {e.Message}", "确定");
                    Debug.LogError($"[ItemDataSOGenerator] 删除错误: {e}");
                }
            }
        }

        private void DeleteSelectedCategories()
        {
            if (!Directory.Exists(outputPath))
            {
                EditorUtility.DisplayDialog("提示", "输出路径不存在，无需删除", "确定");
                return;
            }

            int deletedFiles = 0;
            foreach (var kv in categorySelection)
            {
                if (!kv.Value) continue;
                string folderName = CategoryFolderNames.ContainsKey(kv.Key) ? CategoryFolderNames[kv.Key] : kv.Key;
                string categoryPath = Path.Combine(outputPath, folderName);
                DeleteAssetFilesRecursively(categoryPath, ref deletedFiles);
            }

            AssetDatabase.Refresh();
            lastOperationSummary = $"按类别删除完成：删除 {deletedFiles} 个资产";
            EditorUtility.DisplayDialog("完成", lastOperationSummary, "确定");
            RefreshOverview();
        }

        private void DrawCategorySelectionUI()
        {
            EditorGUILayout.BeginVertical("box");
            foreach (var kv in CategoryFolderNames)
            {
                bool selected = categorySelection.ContainsKey(kv.Key) && categorySelection[kv.Key];
                bool newSelected = EditorGUILayout.ToggleLeft($"{kv.Key}", selected);
                if (newSelected != selected)
                {
                    categorySelection[kv.Key] = newSelected;
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void RefreshOverview()
        {
            categoryOverview.Clear();

            // JSON 统计
            try
            {
                if (File.Exists(jsonPath))
                {
                    string jsonContent = File.ReadAllText(jsonPath);
                    JObject jsonData = JObject.Parse(jsonContent);
                    JObject categories = jsonData["category"] as JObject;
                    if (categories != null)
                    {
                        foreach (var pair in categories)
                        {
                            string name = pair.Key;
                            JArray items = (pair.Value as JObject)?["items"] as JArray;
                            int jsonCount = items?.Count ?? 0;
                            categoryOverview[name] = (jsonCount, 0);
                        }
                    }
                }
            }
            catch { }

            // 已生成统计
            foreach (var kv in CategoryFolderNames)
            {
                string folderName = kv.Value;
                string categoryPath = Path.Combine(outputPath, folderName);
                int generated = CountAssetsRecursively(categoryPath);
                if (categoryOverview.ContainsKey(kv.Key))
                {
                    var t = categoryOverview[kv.Key];
                    categoryOverview[kv.Key] = (t.jsonCount, generated);
                }
                else
                {
                    categoryOverview[kv.Key] = (0, generated);
                }
            }
        }

        private int CountAssetsRecursively(string directoryPath)
        {
            if (!Directory.Exists(directoryPath)) return 0;
            int count = Directory.GetFiles(directoryPath, "*.asset").Length;
            foreach (var subDir in Directory.GetDirectories(directoryPath))
            {
                count += CountAssetsRecursively(subDir);
            }
            return count;
        }

        private void DrawOverviewTable()
        {
            GUILayout.Label("类别概况（JSON数量 / 已生成数量）", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            foreach (var kv in CategoryFolderNames)
            {
                (int jsonCount, int genCount) info = (0, 0);
                if (categoryOverview.ContainsKey(kv.Key)) info = categoryOverview[kv.Key];
                EditorGUILayout.LabelField($"{kv.Key}", $"{info.jsonCount} / {info.genCount}");
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 递归删除指定目录及其子目录中的所有.asset文件，但保留文件夹结构
        /// </summary>
        private void DeleteAssetFilesRecursively(string directoryPath, ref int deletedFiles)
        {
            if (!Directory.Exists(directoryPath))
                return;
        
            // 删除当前目录下的所有.asset文件
            string[] assetFiles = Directory.GetFiles(directoryPath, "*.asset");
            foreach (string file in assetFiles)
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
                DeleteAssetFilesRecursively(subDir, ref deletedFiles);
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

        /// <summary>
        /// 初始化全局ID计数器为当前输出目录下已存在资产的最大GlobalId+1
        /// </summary>
        private void InitializeGlobalIdCounterFromExisting()
        {
            try
            {
                if (!Directory.Exists(outputPath))
                {
                    globalIdCounter = 1L;
                    return;
                }

                long maxExisting = GetMaxExistingGlobalIdUnderPath(outputPath);
                globalIdCounter = Math.Max(maxExisting + 1, 1L);
                Debug.Log($"[ItemDataSOGenerator] GlobalId 初始化为 {globalIdCounter} (maxExisting={maxExisting})");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ItemDataSOGenerator] 初始化 GlobalId 失败，回退为1: {e.Message}");
                globalIdCounter = 1L;
            }
        }

        /// <summary>
        /// 扫描指定路径（含子目录）下的所有 ItemDataSO 资产，返回最大 GlobalId
        /// </summary>
        private long GetMaxExistingGlobalIdUnderPath(string rootPath)
        {
            long maxId = 0L;
            try
            {
                string[] searchPaths = new[] { rootPath };
                string[] guids = AssetDatabase.FindAssets($"t:{nameof(ItemDataSO)}", searchPaths);
                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    var data = AssetDatabase.LoadAssetAtPath<ItemDataSO>(assetPath);
                    if (data != null && data.GlobalId > maxId)
                    {
                        maxId = data.GlobalId;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ItemDataSOGenerator] 扫描最大 GlobalId 出错: {e.Message}");
            }
            return maxId;
        }
    }
}