using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using InventorySystem;

namespace InventorySystem.SpawnSystem.Editor
{
    /// <summary>
    /// 随机物品生成系统的编辑器工具集
    /// 提供便捷的开发者工具、批量操作和配置管理功能
    /// </summary>
    public static class RandomSpawnEditorTools
    {
        private const string MENU_ROOT = "Tools/Random Item Spawn/";
        private const string CONFIG_PATH = "Assets/InventorySystem/Configs/RandomSpawn/";
        
        #region 配置管理工具
        
        /// <summary>
        /// 创建新的随机配置
        /// </summary>
        [MenuItem(MENU_ROOT + "创建配置/新建随机配置", false, 1)]
        public static void CreateNewRandomConfig()
        {
            var config = CreateRandomSpawnConfig("新随机配置");
            if (config != null)
            {
                Selection.activeObject = config;
                EditorGUIUtility.PingObject(config);
                Debug.Log($"已创建新的随机配置: {AssetDatabase.GetAssetPath(config)}");
            }
        }
        
        /// <summary>
        /// 创建示例配置
        /// </summary>
        [MenuItem(MENU_ROOT + "创建配置/创建示例配置", false, 2)]
        public static void CreateExampleConfig()
        {
            var config = CreateRandomSpawnConfig("示例随机配置");
            if (config != null)
            {
                // 设置示例数据
                SetupExampleConfig(config);
                
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                
                Selection.activeObject = config;
                EditorGUIUtility.PingObject(config);
                
                Debug.Log($"已创建示例配置: {AssetDatabase.GetAssetPath(config)}");
                EditorUtility.DisplayDialog("示例配置创建完成", 
                    "已创建包含示例数据的随机配置文件。\n请根据项目需求调整配置参数。", "确定");
            }
        }
        
        /// <summary>
        /// 从模板创建配置
        /// </summary>
        [MenuItem(MENU_ROOT + "创建配置/从模板创建", false, 3)]
        public static void CreateFromTemplate()
        {
            var templates = FindAllRandomConfigs().Where(c => c.configName.Contains("模板")).ToList();
            
            if (templates.Count == 0)
            {
                EditorUtility.DisplayDialog("未找到模板", "项目中没有找到配置模板。", "确定");
                return;
            }
            
            // 显示模板选择窗口
            ShowTemplateSelectionWindow(templates);
        }
        
        #endregion
        
        #region 批量验证和修复工具
        
        /// <summary>
        /// 验证所有随机配置
        /// </summary>
        [MenuItem(MENU_ROOT + "验证工具/验证所有配置", false, 11)]
        public static void ValidateAllConfigs()
        {
            var configs = FindAllRandomConfigs();
            var results = new List<ValidationResult>();
            
            EditorUtility.DisplayProgressBar("验证配置", "正在验证随机配置...", 0);
            
            try
            {
                for (int i = 0; i < configs.Count; i++)
                {
                    var config = configs[i];
                    EditorUtility.DisplayProgressBar("验证配置", 
                        $"验证: {config.configName} ({i + 1}/{configs.Count})", 
                        (float)i / configs.Count);
                    
                    var result = ValidateConfig(config);
                    results.Add(result);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            
            // 显示验证结果
            ShowValidationResults(results);
        }
        
        /// <summary>
        /// 修复所有配置问题
        /// </summary>
        [MenuItem(MENU_ROOT + "验证工具/自动修复所有配置", false, 12)]
        public static void FixAllConfigs()
        {
            var configs = FindAllRandomConfigs();
            int fixedCount = 0;
            
            if (!EditorUtility.DisplayDialog("自动修复确认", 
                $"即将对 {configs.Count} 个配置文件执行自动修复。\n此操作会修改配置文件，建议先备份。\n\n确定继续？", 
                "确定", "取消"))
            {
                return;
            }
            
            EditorUtility.DisplayProgressBar("修复配置", "正在修复配置问题...", 0);
            
            try
            {
                for (int i = 0; i < configs.Count; i++)
                {
                    var config = configs[i];
                    EditorUtility.DisplayProgressBar("修复配置", 
                        $"修复: {config.configName} ({i + 1}/{configs.Count})", 
                        (float)i / configs.Count);
                    
                    if (FixConfigIssues(config))
                    {
                        fixedCount++;
                        EditorUtility.SetDirty(config);
                    }
                }
                
                if (fixedCount > 0)
                {
                    AssetDatabase.SaveAssets();
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            
            EditorUtility.DisplayDialog("修复完成", 
                $"已修复 {fixedCount} 个配置文件中的问题。", "确定");
        }
        
        /// <summary>
        /// 标准化所有权重
        /// </summary>
        [MenuItem(MENU_ROOT + "验证工具/标准化所有权重", false, 13)]
        public static void NormalizeAllWeights()
        {
            var configs = FindAllRandomConfigs();
            int normalizedCount = 0;
            
            EditorUtility.DisplayProgressBar("标准化权重", "正在标准化权重...", 0);
            
            try
            {
                for (int i = 0; i < configs.Count; i++)
                {
                    var config = configs[i];
                    EditorUtility.DisplayProgressBar("标准化权重", 
                        $"处理: {config.configName} ({i + 1}/{configs.Count})", 
                        (float)i / configs.Count);
                    
                    bool changed = false;
                    foreach (var category in config.itemCategories)
                    {
                        var originalWeights = category.rarityGroups.Select(g => g.weight).ToArray();
                        NormalizeRarityGroupWeights(category.rarityGroups);
                        if (!originalWeights.SequenceEqual(category.rarityGroups.Select(g => g.weight)))
                        {
                            changed = true;
                        }
                    }
                    
                    if (changed)
                    {
                        normalizedCount++;
                        EditorUtility.SetDirty(config);
                    }
                }
                
                if (normalizedCount > 0)
                {
                    AssetDatabase.SaveAssets();
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            
            EditorUtility.DisplayDialog("标准化完成", 
                $"已标准化 {normalizedCount} 个配置文件的权重。", "确定");
        }
        
        #endregion
        
        #region 统计和分析工具
        
        /// <summary>
        /// 显示项目统计信息
        /// </summary>
        [MenuItem(MENU_ROOT + "分析工具/项目统计信息", false, 21)]
        public static void ShowProjectStatistics()
        {
            var configs = FindAllRandomConfigs();
            var stats = AnalyzeProjectConfigs(configs);
            
            ShowProjectStatisticsWindow(stats);
        }
        
        /// <summary>
        /// 分析物品分布
        /// </summary>
        [MenuItem(MENU_ROOT + "分析工具/物品分布分析", false, 22)]
        public static void AnalyzeItemDistribution()
        {
            var configs = FindAllRandomConfigs();
            var distribution = AnalyzeItemDistribution(configs);
            
            ShowItemDistributionWindow(distribution);
        }
        
        /// <summary>
        /// 检查缺失引用
        /// </summary>
        [MenuItem(MENU_ROOT + "分析工具/检查缺失引用", false, 23)]
        public static void CheckMissingReferences()
        {
            var configs = FindAllRandomConfigs();
            var missingRefs = new List<MissingReference>();
            
            EditorUtility.DisplayProgressBar("检查引用", "正在检查缺失引用...", 0);
            
            try
            {
                for (int i = 0; i < configs.Count; i++)
                {
                    var config = configs[i];
                    EditorUtility.DisplayProgressBar("检查引用", 
                        $"检查: {config.configName} ({i + 1}/{configs.Count})", 
                        (float)i / configs.Count);
                    
                    var refs = FindMissingReferences(config);
                    missingRefs.AddRange(refs);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            
            ShowMissingReferencesWindow(missingRefs);
        }
        
        #endregion
        
        #region 系统工具
        
        /// <summary>
        /// 重置会话状态
        /// </summary>
        [MenuItem(MENU_ROOT + "系统工具/重置会话状态", false, 31)]
        public static void ResetSessionState()
        {
            if (EditorUtility.DisplayDialog("重置会话状态", 
                "即将重置所有货架的会话生成状态。\n这将允许所有货架重新生成物品。\n\n确定继续？", 
                "确定", "取消"))
            {
                SessionStateManager.ResetSessionState();
                ShelfNumberingSystem.ResetNumbering();
                
                Debug.Log("会话状态已重置");
                EditorUtility.DisplayDialog("重置完成", "会话状态已重置，所有货架现在可以重新生成物品。", "确定");
            }
        }
        
        /// <summary>
        /// 清理无效编号
        /// </summary>
        [MenuItem(MENU_ROOT + "系统工具/清理无效编号", false, 32)]
        public static void CleanupInvalidNumbers()
        {
            int cleaned = ShelfNumberingSystem.CleanupInvalidAssignments();
            Debug.Log($"清理了 {cleaned} 个无效的编号分配");
            EditorUtility.DisplayDialog("清理完成", $"已清理 {cleaned} 个无效的编号分配。", "确定");
        }
        
        /// <summary>
        /// 显示系统状态
        /// </summary>
        [MenuItem(MENU_ROOT + "系统工具/显示系统状态", false, 33)]
        public static void ShowSystemStatus()
        {
            var manager = ShelfRandomItemManager.Instance;
            if (manager != null)
            {
                var report = manager.GetDiagnosticReport();
                Debug.Log(report);
                
                EditorUtility.DisplayDialog("系统状态", "系统状态已输出到控制台，请查看Console窗口。", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("系统状态", "ShelfRandomItemManager实例不存在。", "确定");
            }
        }
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 创建随机配置文件
        /// </summary>
        private static RandomItemSpawnConfig CreateRandomSpawnConfig(string configName)
        {
            // 确保目录存在
            if (!Directory.Exists(CONFIG_PATH))
            {
                Directory.CreateDirectory(CONFIG_PATH);
                AssetDatabase.Refresh();
            }
            
            // 生成唯一文件名
            string fileName = $"{configName}.asset";
            string fullPath = Path.Combine(CONFIG_PATH, fileName);
            fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);
            
            // 创建配置实例
            var config = ScriptableObject.CreateInstance<RandomItemSpawnConfig>();
            config.configName = configName;
            config.version = "1.0.0";
            
            // 保存到文件
            AssetDatabase.CreateAsset(config, fullPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            return config;
        }
        
        /// <summary>
        /// 设置示例配置数据
        /// </summary>
        private static void SetupExampleConfig(RandomItemSpawnConfig config)
        {
            config.configName = "示例随机配置";
            config.description = "这是一个示例配置，展示了如何设置随机物品生成";
            config.targetContainerType = ContainerType.Custom;
            config.minTotalItems = 5;
            config.maxTotalItems = 15;
            config.quantityMode = RandomQuantityMode.UniformRandom;
            config.enablePreview = true;
            config.previewGridSize = new Vector2Int(10, 6);
            
            // 创建示例物品类别
            var exampleCategory = new RandomItemCategory
            {
                isEnabled = true,
                category = ItemCategory.Food,
                categoryName = "食物类别",
                description = "各种食物物品",
                minSpawnCount = 2,
                maxSpawnCount = 8,
                rarityGroups = new RarityItemGroup[]
                {
                    new RarityItemGroup { rarity = ItemRarity.Common, weight = 0.5f, items = new ItemDataSO[0] },
                    new RarityItemGroup { rarity = ItemRarity.Rare, weight = 0.3f, items = new ItemDataSO[0] },
                    new RarityItemGroup { rarity = ItemRarity.Epic, weight = 0.15f, items = new ItemDataSO[0] },
                    new RarityItemGroup { rarity = ItemRarity.Legendary, weight = 0.05f, items = new ItemDataSO[0] }
                }
            };
            
            config.itemCategories = new RandomItemCategory[] { exampleCategory };
        }
        
        /// <summary>
        /// 查找所有随机配置
        /// </summary>
        private static List<RandomItemSpawnConfig> FindAllRandomConfigs()
        {
            var guids = AssetDatabase.FindAssets("t:RandomItemSpawnConfig");
            var configs = new List<RandomItemSpawnConfig>();
            
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<RandomItemSpawnConfig>(path);
                if (config != null)
                {
                    configs.Add(config);
                }
            }
            
            return configs;
        }
        
        /// <summary>
        /// 验证单个配置
        /// </summary>
        private static ValidationResult ValidateConfig(RandomItemSpawnConfig config)
        {
            var result = new ValidationResult
            {
                config = config,
                configName = config.configName,
                isValid = true,
                issues = new List<string>()
            };
            
            // 基础验证
            if (string.IsNullOrEmpty(config.configName))
            {
                result.issues.Add("配置名称为空");
                result.isValid = false;
            }
            
            if (config.minTotalItems <= 0)
            {
                result.issues.Add("最小数量必须大于0");
                result.isValid = false;
            }
            
            if (config.maxTotalItems < config.minTotalItems)
            {
                result.issues.Add("最大数量不能小于最小数量");
                result.isValid = false;
            }
            
            // 类别验证
            if (config.itemCategories == null || config.itemCategories.Length == 0)
            {
                result.issues.Add("没有定义物品类别");
                result.isValid = false;
            }
            else
            {
                for (int i = 0; i < config.itemCategories.Length; i++)
                {
                    var category = config.itemCategories[i];
                    if (string.IsNullOrEmpty(category.categoryName))
                    {
                        result.issues.Add($"类别 {i} 名称为空");
                        result.isValid = false;
                    }
                    
                    if (!RarityWeightValidator.ValidateRarityGroups(category.rarityGroups, out string groupError))
                    {
                        result.issues.Add($"类别 '{category.categoryName}': 权重配置无效");
                        result.isValid = false;
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 修复配置问题
        /// </summary>
        private static bool FixConfigIssues(RandomItemSpawnConfig config)
        {
            bool changed = false;
            
            // 修复基础问题
            if (string.IsNullOrEmpty(config.configName))
            {
                config.configName = "未命名配置";
                changed = true;
            }
            
            if (config.minTotalItems <= 0)
            {
                config.minTotalItems = 1;
                changed = true;
            }
            
            if (config.maxTotalItems < config.minTotalItems)
            {
                config.maxTotalItems = config.minTotalItems;
                changed = true;
            }
            
            // 修复类别问题
            if (config.itemCategories != null)
            {
                for (int i = 0; i < config.itemCategories.Length; i++)
                {
                    var category = config.itemCategories[i];
                    
                    if (string.IsNullOrEmpty(category.categoryName))
                    {
                        category.categoryName = $"类别 {i + 1}";
                        changed = true;
                    }
                    
                    var originalWeights = category.rarityGroups.Select(g => g.weight).ToArray();
                    NormalizeRarityGroupWeights(category.rarityGroups);
                    if (!originalWeights.SequenceEqual(category.rarityGroups.Select(g => g.weight)))
                    {
                        changed = true;
                    }
                }
            }
            
            return changed;
        }
        
        /// <summary>
        /// 分析项目配置
        /// </summary>
        private static ProjectStatistics AnalyzeProjectConfigs(List<RandomItemSpawnConfig> configs)
        {
            var stats = new ProjectStatistics
            {
                totalConfigs = configs.Count,
                validConfigs = 0,
                invalidConfigs = 0,
                totalCategories = 0,
                totalItems = 0,
                rarityDistribution = new Dictionary<ItemRarity, int>()
            };
            
            foreach (var config in configs)
            {
                var validation = ValidateConfig(config);
                if (validation.isValid)
                {
                    stats.validConfigs++;
                }
                else
                {
                    stats.invalidConfigs++;
                }
                
                if (config.itemCategories != null)
                {
                    stats.totalCategories += config.itemCategories.Length;
                    
                    foreach (var category in config.itemCategories)
                    {
                        if (category.rarityGroups != null)
                        {
                            foreach (var group in category.rarityGroups)
                            {
                                if (group.items != null)
                                {
                                    stats.totalItems += group.items.Length;
                                    
                                    if (!stats.rarityDistribution.ContainsKey(group.rarity))
                                    {
                                        stats.rarityDistribution[group.rarity] = 0;
                                    }
                                    stats.rarityDistribution[group.rarity] += group.items.Length;
                                }
                            }
                        }
                    }
                }
            }
            
            return stats;
        }
        
        /// <summary>
        /// 分析物品分布
        /// </summary>
        private static ItemDistributionAnalysis AnalyzeItemDistribution(List<RandomItemSpawnConfig> configs)
        {
            var analysis = new ItemDistributionAnalysis
            {
                categoryDistribution = new Dictionary<ItemCategory, int>(),
                rarityDistribution = new Dictionary<ItemRarity, int>(),
                duplicateItems = new List<string>()
            };
            
            var allItems = new Dictionary<ItemDataSO, int>();
            
            foreach (var config in configs)
            {
                if (config.itemCategories != null)
                {
                    foreach (var category in config.itemCategories)
                    {
                        if (!analysis.categoryDistribution.ContainsKey(category.category))
                        {
                            analysis.categoryDistribution[category.category] = 0;
                        }
                        
                        if (category.rarityGroups != null)
                        {
                            foreach (var group in category.rarityGroups)
                            {
                                if (!analysis.rarityDistribution.ContainsKey(group.rarity))
                                {
                                    analysis.rarityDistribution[group.rarity] = 0;
                                }
                                
                                if (group.items != null)
                                {
                                    analysis.categoryDistribution[category.category] += group.items.Length;
                                    analysis.rarityDistribution[group.rarity] += group.items.Length;
                                    
                                    foreach (var item in group.items)
                                    {
                                        if (item != null)
                                        {
                                            if (!allItems.ContainsKey(item))
                                            {
                                                allItems[item] = 0;
                                            }
                                            allItems[item]++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            // 找出重复物品
            foreach (var kvp in allItems)
            {
                if (kvp.Value > 1 && kvp.Key != null)
                {
                    analysis.duplicateItems.Add($"{kvp.Key.itemName} (出现 {kvp.Value} 次)");
                }
            }
            
            return analysis;
        }
        
        /// <summary>
        /// 查找缺失引用
        /// </summary>
        private static List<MissingReference> FindMissingReferences(RandomItemSpawnConfig config)
        {
            var missingRefs = new List<MissingReference>();
            
            if (config.itemCategories != null)
            {
                for (int i = 0; i < config.itemCategories.Length; i++)
                {
                    var category = config.itemCategories[i];
                    if (category.rarityGroups != null)
                    {
                        for (int j = 0; j < category.rarityGroups.Length; j++)
                        {
                            var group = category.rarityGroups[j];
                            if (group.items != null)
                            {
                                for (int k = 0; k < group.items.Length; k++)
                                {
                                    if (group.items[k] == null)
                                    {
                                        missingRefs.Add(new MissingReference
                                        {
                                            configName = config.configName,
                                            location = $"类别[{i}] {category.categoryName} -> {group.rarity}[{k}]",
                                            type = "ItemDataSO"
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            return missingRefs;
        }
        
        #endregion
        
        #region 窗口显示方法
        
        /// <summary>
        /// 显示模板选择窗口
        /// </summary>
        private static void ShowTemplateSelectionWindow(List<RandomItemSpawnConfig> templates)
        {
            // 这里可以实现一个自定义的EditorWindow
            // 目前使用简单的对话框
            var templateNames = templates.Select(t => t.configName).ToArray();
            int selectedIndex = EditorUtility.DisplayDialogComplex("选择模板", 
                "请选择要使用的配置模板:", 
                templateNames.Length > 0 ? templateNames[0] : "取消", 
                templateNames.Length > 1 ? templateNames[1] : "取消", 
                "取消");
            
            if (selectedIndex < templates.Count)
            {
                var template = templates[selectedIndex];
                var newConfig = Object.Instantiate(template);
                newConfig.configName = $"{template.configName}_副本";
                
                string path = AssetDatabase.GenerateUniqueAssetPath($"{CONFIG_PATH}{newConfig.configName}.asset");
                AssetDatabase.CreateAsset(newConfig, path);
                AssetDatabase.SaveAssets();
                
                Selection.activeObject = newConfig;
                EditorGUIUtility.PingObject(newConfig);
            }
        }
        
        /// <summary>
        /// 显示验证结果
        /// </summary>
        private static void ShowValidationResults(List<ValidationResult> results)
        {
            int validCount = results.Count(r => r.isValid);
            int invalidCount = results.Count - validCount;
            
            string message = $"验证完成!\n\n有效配置: {validCount}\n无效配置: {invalidCount}";
            
            if (invalidCount > 0)
            {
                message += "\n\n问题详情:";
                foreach (var result in results.Where(r => !r.isValid))
                {
                    message += $"\n\n{result.configName}:";
                    foreach (var issue in result.issues)
                    {
                        message += $"\n  - {issue}";
                    }
                }
            }
            
            Debug.Log(message);
            EditorUtility.DisplayDialog("配置验证结果", 
                $"验证完成!\n\n有效配置: {validCount}\n无效配置: {invalidCount}\n\n详细信息已输出到控制台。", 
                "确定");
        }
        
        /// <summary>
        /// 显示项目统计窗口
        /// </summary>
        private static void ShowProjectStatisticsWindow(ProjectStatistics stats)
        {
            string message = $"项目统计信息:\n\n";
            message += $"总配置数: {stats.totalConfigs}\n";
            message += $"有效配置: {stats.validConfigs}\n";
            message += $"无效配置: {stats.invalidConfigs}\n";
            message += $"总类别数: {stats.totalCategories}\n";
            message += $"总物品数: {stats.totalItems}\n\n";
            message += "珍稀度分布:\n";
            
            foreach (var kvp in stats.rarityDistribution)
            {
                message += $"  {kvp.Key}: {kvp.Value} 个\n";
            }
            
            Debug.Log(message);
            EditorUtility.DisplayDialog("项目统计", message, "确定");
        }
        
        /// <summary>
        /// 显示物品分布窗口
        /// </summary>
        private static void ShowItemDistributionWindow(ItemDistributionAnalysis analysis)
        {
            string message = "物品分布分析:\n\n类别分布:\n";
            
            foreach (var kvp in analysis.categoryDistribution)
            {
                message += $"  {kvp.Key}: {kvp.Value} 个\n";
            }
            
            message += "\n珍稀度分布:\n";
            foreach (var kvp in analysis.rarityDistribution)
            {
                message += $"  {kvp.Key}: {kvp.Value} 个\n";
            }
            
            if (analysis.duplicateItems.Count > 0)
            {
                message += $"\n重复物品 ({analysis.duplicateItems.Count}):\n";
                foreach (var item in analysis.duplicateItems.Take(10))
                {
                    message += $"  {item}\n";
                }
                if (analysis.duplicateItems.Count > 10)
                {
                    message += $"  ... 还有 {analysis.duplicateItems.Count - 10} 个\n";
                }
            }
            
            Debug.Log(message);
            EditorUtility.DisplayDialog("物品分布分析", message, "确定");
        }
        
        /// <summary>
        /// 显示缺失引用窗口
        /// </summary>
        private static void ShowMissingReferencesWindow(List<MissingReference> missingRefs)
        {
            if (missingRefs.Count == 0)
            {
                EditorUtility.DisplayDialog("检查完成", "未发现缺失引用。", "确定");
                return;
            }
            
            string message = $"发现 {missingRefs.Count} 个缺失引用:\n\n";
            
            foreach (var missingRef in missingRefs.Take(20))
            {
                message += $"{missingRef.configName}: {missingRef.location}\n";
            }
            
            if (missingRefs.Count > 20)
            {
                message += $"\n... 还有 {missingRefs.Count - 20} 个缺失引用\n";
            }
            
            Debug.LogWarning(message);
            EditorUtility.DisplayDialog("缺失引用检查", message, "确定");
        }
        
        /// <summary>
        /// 标准化珍稀度组权重
        /// </summary>
        private static void NormalizeRarityGroupWeights(RarityItemGroup[] rarityGroups)
        {
            if (rarityGroups == null || rarityGroups.Length == 0)
                return;
                
            // 提取权重
            float[] weights = rarityGroups.Select(g => g.weight).ToArray();
            
            // 使用RarityWeightValidator标准化
            float[] normalizedWeights = RarityWeightValidator.NormalizeWeights(weights);
            
            // 将标准化后的权重赋值回去
            for (int i = 0; i < rarityGroups.Length && i < normalizedWeights.Length; i++)
            {
                rarityGroups[i].weight = normalizedWeights[i];
            }
        }
        
        #endregion
        
        #region 数据结构
        
        /// <summary>
        /// 验证结果
        /// </summary>
        public class ValidationResult
        {
            public RandomItemSpawnConfig config;
            public string configName;
            public bool isValid;
            public List<string> issues;
        }
        
        /// <summary>
        /// 项目统计信息
        /// </summary>
        public class ProjectStatistics
        {
            public int totalConfigs;
            public int validConfigs;
            public int invalidConfigs;
            public int totalCategories;
            public int totalItems;
            public Dictionary<ItemRarity, int> rarityDistribution;
        }
        
        /// <summary>
        /// 物品分布分析
        /// </summary>
        public class ItemDistributionAnalysis
        {
            public Dictionary<ItemCategory, int> categoryDistribution;
            public Dictionary<ItemRarity, int> rarityDistribution;
            public List<string> duplicateItems;
        }
        
        /// <summary>
        /// 缺失引用
        /// </summary>
        public class MissingReference
        {
            public string configName;
            public string location;
            public string type;
        }
        
        #endregion
    }
}
