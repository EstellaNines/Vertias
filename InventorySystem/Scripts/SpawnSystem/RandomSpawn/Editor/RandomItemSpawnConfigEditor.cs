using UnityEngine;
using UnityEditor;
using InventorySystem.SpawnSystem;
using System.Collections.Generic;
using System.Linq;

namespace InventorySystem.SpawnSystem.Editor
{
    /// <summary>
    /// 随机物品生成配置的自定义编辑器
    /// </summary>
    [CustomEditor(typeof(RandomItemSpawnConfig))]
    public class RandomItemSpawnConfigEditor : UnityEditor.Editor
    {
        private SerializedProperty configNameProp;
        private SerializedProperty descriptionProp;
        private SerializedProperty versionProp;
        private SerializedProperty targetContainerTypeProp;
        private SerializedProperty containerIdentifierProp;
        private SerializedProperty minimumGridSizeProp;
        
        private SerializedProperty minTotalItemsProp;
        private SerializedProperty maxTotalItemsProp;
        private SerializedProperty quantityModeProp;
        
        private SerializedProperty itemCategoriesProp;
        private SerializedProperty categoryStrategyProp;
        private SerializedProperty rarityBalanceProp;
        private SerializedProperty avoidDuplicateItemsProp;
        private SerializedProperty maxRetryAttemptsProp;
        
        private SerializedProperty enablePreviewProp;
        private SerializedProperty previewSeedProp;
        private SerializedProperty previewGridSizeProp;
        
        private SerializedProperty enableDetailedLoggingProp;
        private SerializedProperty generationTimeoutProp;
        private SerializedProperty strictCompatibilityCheckProp;
        
        private bool showBasicInfo = true;
        private bool showContainerConfig = true;
        private bool showQuantityControl = true;
        private bool showItemCategories = true;
        private bool showGenerationStrategy = true;
        private bool showPreviewSettings = true;
        private bool showAdvancedSettings = false;
        
        // 预览相关
        private List<PreviewGridRenderer.PreviewItemDisplay> cachedPreviewItems;
        private int lastPreviewSeed = -1;
        private Vector2Int lastGridSize = Vector2Int.zero;
        
        private void OnEnable()
        {
            // 基础信息
            configNameProp = serializedObject.FindProperty("configName");
            descriptionProp = serializedObject.FindProperty("description");
            versionProp = serializedObject.FindProperty("version");
            
            // 容器配置
            targetContainerTypeProp = serializedObject.FindProperty("targetContainerType");
            containerIdentifierProp = serializedObject.FindProperty("containerIdentifier");
            minimumGridSizeProp = serializedObject.FindProperty("minimumGridSize");
            
            // 数量控制
            minTotalItemsProp = serializedObject.FindProperty("minTotalItems");
            maxTotalItemsProp = serializedObject.FindProperty("maxTotalItems");
            quantityModeProp = serializedObject.FindProperty("quantityMode");
            
            // 物品类别
            itemCategoriesProp = serializedObject.FindProperty("itemCategories");
            
            // 生成策略
            categoryStrategyProp = serializedObject.FindProperty("categoryStrategy");
            rarityBalanceProp = serializedObject.FindProperty("rarityBalance");
            avoidDuplicateItemsProp = serializedObject.FindProperty("avoidDuplicateItems");
            maxRetryAttemptsProp = serializedObject.FindProperty("maxRetryAttempts");
            
            // 预览设置
            enablePreviewProp = serializedObject.FindProperty("enablePreview");
            previewSeedProp = serializedObject.FindProperty("previewSeed");
            previewGridSizeProp = serializedObject.FindProperty("previewGridSize");
            
            // 高级设置
            enableDetailedLoggingProp = serializedObject.FindProperty("enableDetailedLogging");
            generationTimeoutProp = serializedObject.FindProperty("generationTimeout");
            strictCompatibilityCheckProp = serializedObject.FindProperty("strictCompatibilityCheck");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            var config = target as RandomItemSpawnConfig;
            
            // 标题
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("随机物品生成配置", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // 基础信息
            showBasicInfo = EditorGUILayout.Foldout(showBasicInfo, "配置基础信息", true);
            if (showBasicInfo)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(configNameProp, new GUIContent("配置名称"));
                EditorGUILayout.PropertyField(descriptionProp, new GUIContent("配置描述"));
                EditorGUILayout.PropertyField(versionProp, new GUIContent("配置版本"));
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // 容器配置
            showContainerConfig = EditorGUILayout.Foldout(showContainerConfig, "目标容器配置", true);
            if (showContainerConfig)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(targetContainerTypeProp, new GUIContent("适用容器类型"));
                EditorGUILayout.PropertyField(containerIdentifierProp, new GUIContent("容器标识符"));
                EditorGUILayout.PropertyField(minimumGridSizeProp, new GUIContent("网格尺寸验证"));
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // 数量控制
            showQuantityControl = EditorGUILayout.Foldout(showQuantityControl, "生成数量控制", true);
            if (showQuantityControl)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(minTotalItemsProp, new GUIContent("最小总物品数"));
                EditorGUILayout.PropertyField(maxTotalItemsProp, new GUIContent("最大总物品数"));
                EditorGUILayout.PropertyField(quantityModeProp, new GUIContent("生成数量随机模式"));
                
                // 显示数量范围提示
                if (config.minTotalItems > 0 && config.maxTotalItems > 0)
                {
                    EditorGUILayout.HelpBox(
                        $"将生成 {config.minTotalItems} - {config.maxTotalItems} 个物品", 
                        MessageType.Info);
                }
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("固定物品优先生成", EditorStyles.boldLabel);
                config.enableFixedItemGeneration = EditorGUILayout.Toggle("启用固定物品", config.enableFixedItemGeneration);
                EditorGUI.BeginDisabledGroup(!config.enableFixedItemGeneration);
                config.fixedItemData = EditorGUILayout.ObjectField("固定物品", config.fixedItemData, typeof(ItemDataSO), false) as ItemDataSO;
                config.fixedItemStackAmount = EditorGUILayout.IntSlider("固定物品堆叠数量", Mathf.Max(1, config.fixedItemStackAmount), 1, 1000);
                if (config.fixedItemData != null && !config.fixedItemData.IsStackable())
                {
                    EditorGUILayout.HelpBox("所选固定物品不可堆叠，将按 1 计数生成。", MessageType.Info);
                }
                if (config.enableFixedItemGeneration && config.fixedItemData == null)
                {
                    EditorGUILayout.HelpBox("已启用固定物品，但未指定物品。", MessageType.Warning);
                }
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.HelpBox("固定物品会在随机生成前优先放入，并计入总数（总数至少为1时有效）", MessageType.None);

                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // 物品类别配置
            showItemCategories = EditorGUILayout.Foldout(showItemCategories, "物品类别配置", true);
            if (showItemCategories)
            {
                EditorGUI.indentLevel++;
                
                // 显示统计信息
                if (config.itemCategories != null && config.itemCategories.Length > 0)
                {
                    var stats = config.GetStatistics();
                    EditorGUILayout.HelpBox(
                        $"总类别: {stats.totalCategories} | 启用类别: {stats.enabledCategories} | " +
                        $"可用物品: {stats.totalAvailableItems} | " +
                        $"普通: {stats.commonItemsCount} | 稀有: {stats.rareItemsCount} | " +
                        $"史诗: {stats.epicItemsCount} | 传说: {stats.legendaryItemsCount}",
                        MessageType.Info);
                }
                
                // 自定义绘制物品类别数组
                DrawItemCategoriesArray(config);
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // 生成策略
            showGenerationStrategy = EditorGUILayout.Foldout(showGenerationStrategy, "生成策略", true);
            if (showGenerationStrategy)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(categoryStrategyProp, new GUIContent("类别选择策略"));
                EditorGUILayout.PropertyField(rarityBalanceProp, new GUIContent("珍稀度平衡模式"));
                EditorGUILayout.PropertyField(avoidDuplicateItemsProp, new GUIContent("避免重复物品"));
                EditorGUILayout.PropertyField(maxRetryAttemptsProp, new GUIContent("最大重试次数"));
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // 预览设置
            showPreviewSettings = EditorGUILayout.Foldout(showPreviewSettings, "编辑器预览", true);
            if (showPreviewSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(enablePreviewProp, new GUIContent("启用预览"));
                
                if (config.enablePreview)
                {
                    EditorGUILayout.PropertyField(previewSeedProp, new GUIContent("预览随机种子"));
                    EditorGUILayout.PropertyField(previewGridSizeProp, new GUIContent("预览网格尺寸"));
                    
                    // 预览控制按钮
                    if (PreviewGridRenderer.DrawPreviewControls(config))
                    {
                        // 重新生成预览
                        cachedPreviewItems = null;
                        Repaint();
                    }
                    
                    EditorGUILayout.Space();
                    
                    // 预览网格渲染
                    DrawPreviewGrid(config);
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // 高级设置
            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "高级设置", true);
            if (showAdvancedSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(enableDetailedLoggingProp, new GUIContent("启用详细日志"));
                EditorGUILayout.PropertyField(generationTimeoutProp, new GUIContent("生成超时时间"));
                EditorGUILayout.PropertyField(strictCompatibilityCheckProp, new GUIContent("兼容性检查"));
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // 操作按钮
            EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("验证配置"))
            {
                if (config.ValidateConfiguration(out var errors))
                {
                    EditorUtility.DisplayDialog("验证结果", "配置验证通过！", "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("验证失败", 
                        $"配置验证失败:\n{string.Join("\n", errors)}", "确定");
                }
            }
            
            if (GUILayout.Button("显示统计"))
            {
                config.ShowStatistics();
            }
            
            if (GUILayout.Button("自动修复"))
            {
                if (config.AutoFixConfiguration())
                {
                    EditorUtility.SetDirty(config);
                    EditorUtility.DisplayDialog("修复完成", "配置已自动修复", "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("无需修复", "配置无需修复", "确定");
                }
            }
            
            if (GUILayout.Button("创建示例"))
            {
                config.CreateExampleConfiguration();
                EditorUtility.SetDirty(config);
            }
            
            EditorGUILayout.EndHorizontal();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        /// <summary>
        /// 自定义绘制物品类别数组
        /// </summary>
        private void DrawItemCategoriesArray(RandomItemSpawnConfig config)
        {
            EditorGUILayout.LabelField("随机物品类别", EditorStyles.boldLabel);
            
            if (config.itemCategories == null)
            {
                config.itemCategories = new RandomItemCategory[0];
            }
            
            // 绘制数组大小控制
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"数组大小: {config.itemCategories.Length}");
            
            if (GUILayout.Button("添加类别", GUILayout.Width(80)))
            {
                AddNewItemCategory(config);
            }
            
            if (config.itemCategories.Length > 0 && GUILayout.Button("删除最后一个", GUILayout.Width(80)))
            {
                RemoveLastItemCategory(config);
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 绘制每个类别
            for (int i = 0; i < config.itemCategories.Length; i++)
            {
                // 使用带背景的框
                EditorGUILayout.BeginVertical("box");
                
                // 类别标题
                EditorGUILayout.BeginHorizontal();
                
                var category = config.itemCategories[i];
                string categoryTitle = category != null && !string.IsNullOrEmpty(category.categoryName) 
                    ? $"{category.categoryName} ({category.category})" 
                    : $"物品类别 {i}";
                
                // 启用状态指示
                if (category != null)
                {
                    var enabledColor = category.isEnabled ? Color.green : Color.red;
                    var originalColor = GUI.color;
                    GUI.color = enabledColor;
                    EditorGUILayout.LabelField("●", GUILayout.Width(15));
                    GUI.color = originalColor;
                }
                
                EditorGUILayout.LabelField(categoryTitle, EditorStyles.boldLabel);
                
                if (GUILayout.Button("删除", GUILayout.Width(50)))
                {
                    RemoveItemCategoryAt(config, i);
                    break; // 跳出循环，因为数组已改变
                }
                
                EditorGUILayout.EndHorizontal();
                
                // 绘制类别属性
                if (category != null)
                {
                    DrawItemCategoryInspector(category, i);
                }
                else
                {
                    EditorGUILayout.HelpBox("空的物品类别", MessageType.Warning);
                    if (GUILayout.Button("创建新类别"))
                    {
                        config.itemCategories[i] = CreateNewItemCategory();
                        EditorUtility.SetDirty(config);
                    }
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
        }
        
        /// <summary>
        /// 绘制单个物品类别的Inspector
        /// </summary>
        private void DrawItemCategoryInspector(RandomItemCategory category, int index)
        {
            EditorGUI.indentLevel++;
            
            // 基础设置
            EditorGUILayout.LabelField("类别基础设置", EditorStyles.boldLabel);
            category.isEnabled = EditorGUILayout.Toggle("启用此类别", category.isEnabled);
            category.category = (ItemCategory)EditorGUILayout.EnumPopup("物品类别", category.category);
            category.categoryName = EditorGUILayout.TextField("类别名称", category.categoryName);
            category.description = EditorGUILayout.TextField("类别描述", category.description);
            
            EditorGUILayout.Space();
            
            // 数量限制
            EditorGUILayout.LabelField("数量限制", EditorStyles.boldLabel);
            category.minSpawnCount = EditorGUILayout.IntSlider("最小生成数量", category.minSpawnCount, 0, 20);
            category.maxSpawnCount = EditorGUILayout.IntSlider("最大生成数量", category.maxSpawnCount, 0, 20);
            
            // 确保最大值不小于最小值
            if (category.maxSpawnCount < category.minSpawnCount)
            {
                category.maxSpawnCount = category.minSpawnCount;
            }
            
            EditorGUILayout.Space();
            
            // 珍稀度分布
            EditorGUILayout.LabelField("珍稀度分布", EditorStyles.boldLabel);
            category.autoLoadItems = EditorGUILayout.Toggle("自动获取物品", category.autoLoadItems);

            float totalWeight = 0f;
            if (category.autoLoadItems)
            {
                // 仅显示权重（无需维护物品数组）
                if (category.rarityWeights == null || category.rarityWeights.Length != 4)
                {
                    category.GetType().GetMethod("InitializeDefaultRarityWeights", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.Invoke(category, null);
                }
                for (int i = 0; i < 4; i++)
                {
                    var cfg = category.rarityWeights[i];
                    EditorGUILayout.BeginVertical("helpbox");
                    EditorGUILayout.LabelField($"{cfg.rarity} 珍稀度", EditorStyles.boldLabel);
                    cfg.weight = EditorGUILayout.Slider("生成权重", cfg.weight, 0f, 1f);
                    totalWeight += cfg.weight;
                    category.rarityWeights[i] = cfg;
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(3);
                }
            }
            else
            {
                // 兼容旧模式：显示物品数组
                if (category.rarityGroups == null || category.rarityGroups.Length != 4)
                {
                    var newGroups = new RarityItemGroup[4];
                    for (int i = 0; i < 4; i++)
                    {
                        newGroups[i] = (category.rarityGroups != null && i < category.rarityGroups.Length && category.rarityGroups[i] != null)
                            ? category.rarityGroups[i]
                            : new RarityItemGroup { rarity = (ItemRarity)i, weight = 0.25f, items = new ItemDataSO[0] };
                    }
                    category.rarityGroups = newGroups;
                }
                for (int i = 0; i < category.rarityGroups.Length && i < 4; i++)
                {
                    var rarityGroup = category.rarityGroups[i];
                    EditorGUILayout.BeginVertical("helpbox");
                    EditorGUILayout.LabelField($"{rarityGroup.rarity} 珍稀度", EditorStyles.boldLabel);
                    rarityGroup.weight = EditorGUILayout.Slider("生成权重", rarityGroup.weight, 0f, 1f);
                    totalWeight += rarityGroup.weight;

                    EditorGUILayout.LabelField($"物品列表 (数量: {(rarityGroup.items?.Length ?? 0)})");
                    if (rarityGroup.items == null) rarityGroup.items = new ItemDataSO[0];
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"数组大小: {rarityGroup.items.Length}");
                    if (GUILayout.Button("+", GUILayout.Width(25)))
                    {
                        var newArray = new ItemDataSO[rarityGroup.items.Length + 1];
                        System.Array.Copy(rarityGroup.items, newArray, rarityGroup.items.Length);
                        rarityGroup.items = newArray;
                    }
                    if (rarityGroup.items.Length > 0 && GUILayout.Button("-", GUILayout.Width(25)))
                    {
                        var newArray = new ItemDataSO[rarityGroup.items.Length - 1];
                        System.Array.Copy(rarityGroup.items, newArray, newArray.Length);
                        rarityGroup.items = newArray;
                    }
                    EditorGUILayout.EndHorizontal();
                    int displayCount = Mathf.Min(3, rarityGroup.items.Length);
                    for (int j = 0; j < displayCount; j++)
                    {
                        rarityGroup.items[j] = EditorGUILayout.ObjectField($"物品 {j}", rarityGroup.items[j], typeof(ItemDataSO), false) as ItemDataSO;
                    }
                    if (rarityGroup.items.Length > 3)
                    {
                        EditorGUILayout.LabelField($"... 还有 {rarityGroup.items.Length - 3} 个物品");
                    }
                    category.rarityGroups[i] = rarityGroup;
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(3);
                }
            }

            // 权重总和提示
            if (Mathf.Abs(totalWeight - 1f) > 0.01f)
            {
                EditorGUILayout.HelpBox($"权重总和: {totalWeight:F3} (建议为 1.0)", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox($"权重总和: {totalWeight:F3}", MessageType.Info);
            }

            EditorGUILayout.Space();

            // 堆叠数量配置（当类别内无可堆叠物品时自动禁用）
            EditorGUILayout.LabelField("堆叠数量配置", EditorStyles.boldLabel);

            bool hasStackables = false;
            // 检测该类别下是否存在任一可堆叠物品（支持自动加载或手动分组两种模式）
            for (int iR = 0; iR < 4 && !hasStackables; iR++)
            {
                var rarity = (ItemRarity)iR;
                var items = category.GetItemsByRarity(rarity);
                if (items != null)
                {
                    for (int k = 0; k < items.Length; k++)
                    {
                        var it = items[k];
                        if (it != null && it.IsStackable()) { hasStackables = true; break; }
                    }
                }
            }

            if (!hasStackables)
            {
                // 没有可堆叠物品：禁用相关控件并强制关闭开关
                category.enableStackRandomization = false;
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.Toggle("启用堆叠随机化", false);
                EditorGUILayout.IntSlider("最小堆叠数量", Mathf.Max(1, category.minStackAmount), 1, 1000);
                EditorGUILayout.IntSlider("最大堆叠数量", Mathf.Max(1, category.maxStackAmount), 1, 1000);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.HelpBox("该类别当前无可堆叠物品（如子弹、货币）。已禁用堆叠随机化与最小/最大选项。", MessageType.Info);
            }
            else
            {
                category.enableStackRandomization = EditorGUILayout.Toggle("启用堆叠随机化", category.enableStackRandomization);
                if (category.enableStackRandomization)
                {
                    int minVal = EditorGUILayout.IntSlider("最小堆叠数量", Mathf.Max(1, category.minStackAmount), 1, 1000);
                    int maxVal = EditorGUILayout.IntSlider("最大堆叠数量", Mathf.Max(1, category.maxStackAmount), 1, 1000);
                    if (maxVal < minVal) maxVal = minVal;
                    category.minStackAmount = minVal;
                    category.maxStackAmount = maxVal;
                    EditorGUILayout.HelpBox("实际生成时会再与物品的 maxStack 取最小值进行截断", MessageType.Info);
                }
                else
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.IntSlider("最小堆叠数量", Mathf.Max(1, category.minStackAmount), 1, 1000);
                    EditorGUILayout.IntSlider("最大堆叠数量", Mathf.Max(1, category.maxStackAmount), 1, 1000);
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.HelpBox("未启用堆叠随机化时，可堆叠物品将以 1 的堆叠生成", MessageType.None);
                }
            }
            
            EditorGUI.indentLevel--;
        }
        
        /// <summary>
        /// 标准化权重
        /// </summary>
        private void NormalizeWeights(RarityItemGroup[] rarityGroups)
        {
            float totalWeight = 0f;
            foreach (var group in rarityGroups)
            {
                totalWeight += group.weight;
            }
            
            if (totalWeight > 0)
            {
                foreach (var group in rarityGroups)
                {
                    group.weight = group.weight / totalWeight;
                }
            }
        }
        
        /// <summary>
        /// 添加新的物品类别
        /// </summary>
        private void AddNewItemCategory(RandomItemSpawnConfig config)
        {
            var list = new System.Collections.Generic.List<RandomItemCategory>(config.itemCategories);
            list.Add(CreateNewItemCategory());
            config.itemCategories = list.ToArray();
            EditorUtility.SetDirty(config);
        }
        
        /// <summary>
        /// 移除最后一个物品类别
        /// </summary>
        private void RemoveLastItemCategory(RandomItemSpawnConfig config)
        {
            if (config.itemCategories.Length > 0)
            {
                var list = new System.Collections.Generic.List<RandomItemCategory>(config.itemCategories);
                list.RemoveAt(list.Count - 1);
                config.itemCategories = list.ToArray();
                EditorUtility.SetDirty(config);
            }
        }
        
        /// <summary>
        /// 移除指定索引的物品类别
        /// </summary>
        private void RemoveItemCategoryAt(RandomItemSpawnConfig config, int index)
        {
            if (index >= 0 && index < config.itemCategories.Length)
            {
                var list = new System.Collections.Generic.List<RandomItemCategory>(config.itemCategories);
                list.RemoveAt(index);
                config.itemCategories = list.ToArray();
                EditorUtility.SetDirty(config);
            }
        }
        
        /// <summary>
        /// 创建新的物品类别
        /// </summary>
        private RandomItemCategory CreateNewItemCategory()
        {
            var newCategory = new RandomItemCategory();
            newCategory.isEnabled = true;
            newCategory.category = ItemCategory.Food;
            newCategory.categoryName = "新物品类别";
            newCategory.description = "描述此类别的物品";
            newCategory.minSpawnCount = 1;
            newCategory.maxSpawnCount = 5;
            
            // 初始化珍稀度组
            newCategory.rarityGroups = new RarityItemGroup[4];
            for (int i = 0; i < 4; i++)
            {
                newCategory.rarityGroups[i] = new RarityItemGroup
                {
                    rarity = (ItemRarity)i,
                    weight = 0.25f,
                    items = new ItemDataSO[0]
                };
            }
            
            return newCategory;
        }
        
        /// <summary>
        /// 绘制预览网格
        /// </summary>
        private void DrawPreviewGrid(RandomItemSpawnConfig config)
        {
            if (config == null) return;
            
            // 检查是否需要重新生成预览
            bool needRegenerate = cachedPreviewItems == null || 
                                  lastPreviewSeed != config.previewSeed ||
                                  lastGridSize != config.previewGridSize;
            
            if (needRegenerate)
            {
                cachedPreviewItems = PreviewGridRenderer.GeneratePreview(config, config.previewGridSize);
                lastPreviewSeed = config.previewSeed;
                lastGridSize = config.previewGridSize;
            }
            
            // 计算预览区域尺寸
            float gridWidth = config.previewGridSize.x * 32f;
            float gridHeight = config.previewGridSize.y * 32f;
            float totalHeight = gridHeight + 40f; // 额外空间用于边距
            
            // 创建预览区域
            Rect previewRect = GUILayoutUtility.GetRect(0, totalHeight, GUILayout.ExpandWidth(true));
            
            // 绘制预览背景
            EditorGUI.DrawRect(previewRect, new Color(0.1f, 0.1f, 0.1f, 0.3f));
            
            // 渲染预览网格
            PreviewGridRenderer.RenderPreviewGrid(previewRect, config.previewGridSize, cachedPreviewItems, true, true);
            
            // 显示预览统计信息
            if (cachedPreviewItems != null && cachedPreviewItems.Count > 0)
            {
                DrawPreviewStatistics(cachedPreviewItems);
            }
        }
        
        /// <summary>
        /// 绘制预览统计信息
        /// </summary>
        private void DrawPreviewStatistics(List<PreviewGridRenderer.PreviewItemDisplay> previewItems)
        {
            if (previewItems == null || previewItems.Count == 0) return;
            
            EditorGUILayout.Space(5);
            
            var grouped = previewItems.GroupBy(item => item.rarity).OrderBy(g => g.Key);
            
            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField("预览统计:", EditorStyles.boldLabel, GUILayout.Width(80));
            
            foreach (var group in grouped)
            {
                Color rarityColor = GetRarityColor(group.Key);
                var style = new GUIStyle(EditorStyles.label);
                style.normal.textColor = rarityColor;
                
                EditorGUILayout.LabelField($"{GetRarityDisplayName(group.Key)}: {group.Count()}", style, GUILayout.Width(60));
            }
            
            EditorGUILayout.LabelField($"总计: {previewItems.Count}", EditorStyles.boldLabel, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 获取珍稀度显示名称
        /// </summary>
        private string GetRarityDisplayName(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return "普通";
                case ItemRarity.Rare: return "稀有";
                case ItemRarity.Epic: return "史诗";
                case ItemRarity.Legendary: return "传说";
                default: return "未知";
            }
        }
        
        /// <summary>
        /// 获取珍稀度颜色
        /// </summary>
        private Color GetRarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return Color.white;
                case ItemRarity.Rare: return Color.green;
                case ItemRarity.Epic: return Color.magenta;
                case ItemRarity.Legendary: return Color.yellow;
                default: return Color.gray;
            }
        }
        
        /// <summary>
        /// 生成预览
        /// </summary>
        private void GeneratePreview(RandomItemSpawnConfig config)
        {
            var previewItems = config.GeneratePreviewItems();
            
            string message = $"预览生成结果 (种子: {config.previewSeed}):\n";
            message += $"总物品数: {previewItems.Count}\n\n";
            
            var categoryGroups = previewItems.GroupBy(item => item.categoryName);
            foreach (var group in categoryGroups)
            {
                message += $"{group.Key}:\n";
                var rarityGroups = group.GroupBy(item => item.rarity);
                foreach (var rarityGroup in rarityGroups)
                {
                    message += $"  {rarityGroup.Key}: {rarityGroup.Count()} 个\n";
                }
                message += "\n";
            }
            
            EditorUtility.DisplayDialog("预览结果", message, "确定");
        }
    }
}
