using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using InventorySystem;

namespace InventorySystem.SpawnSystem
{
    /// <summary>
    /// 货架随机物品生成管理器
    /// 专门负责管理货架的随机物品生成，确保每个货架在会话期间只生成一次
    /// 与WarehouseFixedItemManager不同，此管理器使用会话级状态，重启后重置
    /// </summary>
    [AddComponentMenu("Inventory System/Spawn System/Shelf Random Item Manager")]
    public class ShelfRandomItemManager : MonoBehaviour
    {
        [Header("随机生成配置")]
        [FieldLabel("默认随机配置")]
        [Tooltip("默认的随机物品生成配置")]
        [SerializeField] private RandomItemSpawnConfig defaultConfig;
        
        [FieldLabel("自动初始化")]
        [Tooltip("是否在Start时自动初始化管理器")]
        [SerializeField] private bool autoInitialize = true;
        
        [FieldLabel("启用自动编号")]
        [Tooltip("是否启用货架自动编号系统")]
        [SerializeField] private bool enableAutoNumbering = true;
        
        [Header("调试设置")]
        [FieldLabel("启用调试日志")]
        [SerializeField] private bool enableDebugLog = true;
        
        [FieldLabel("显示生成统计")]
        [Tooltip("是否在控制台显示生成统计信息")]
        [SerializeField] private bool showGenerationStatistics = true;
        
        [FieldLabel("生成超时时间")]
        [Range(1f, 30f)]
        [Tooltip("单次生成的最大允许时间（秒）")]
        [SerializeField] private float generationTimeout = 10f;
        
        // 单例模式
        private static ShelfRandomItemManager instance;
        public static ShelfRandomItemManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<ShelfRandomItemManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("ShelfRandomItemManager");
                        instance = go.AddComponent<ShelfRandomItemManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }
        
        // 事件
        public static event System.Action<string, List<RandomItemSelector.SelectedItemInfo>> OnItemsGenerated;
        public static event System.Action<string, string> OnGenerationFailed;
        public static event System.Action OnManagerInitialized;
        public static event System.Action OnSessionReset;
        
        // 运行时状态
        private bool isInitialized = false;
        private Dictionary<string, RandomItemSpawnConfig> configOverrides = new Dictionary<string, RandomItemSpawnConfig>();
        private Dictionary<string, Coroutine> activeGenerations = new Dictionary<string, Coroutine>();
        
        #region 生命周期
        
        private void Awake()
        {
            // 确保单例
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeManager();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }
        
        private void Start()
        {
            if (autoInitialize && !isInitialized)
            {
                InitializeManager();
            }
        }
        
        private void OnDestroy()
        {
            // 清理正在进行的生成过程
            foreach (var kvp in activeGenerations)
            {
                if (kvp.Value != null)
                {
                    StopCoroutine(kvp.Value);
                }
            }
            activeGenerations.Clear();
            
            // 取消事件订阅
            UnregisterEvents();
        }
        
        #endregion
        
        #region 初始化
        
        /// <summary>
        /// 初始化管理器
        /// </summary>
        private void InitializeManager()
        {
            if (isInitialized) return;
            
            LogDebug("正在初始化货架随机物品管理器...");
            
            // 初始化会话状态管理器
            SessionStateManager.SetDebugLogEnabled(enableDebugLog);
            
            // 注册事件
            RegisterEvents();
            
            // 验证默认配置
            ValidateDefaultConfig();
            
            isInitialized = true;
            LogDebug("货架随机物品管理器初始化完成");
            
            // 触发初始化事件
            OnManagerInitialized?.Invoke();
        }
        
        /// <summary>
        /// 验证默认配置
        /// </summary>
        private void ValidateDefaultConfig()
        {
            if (defaultConfig == null)
            {
                LogWarning("未设置默认随机配置，某些功能可能无法正常工作");
                return;
            }
            
            if (!defaultConfig.ValidateConfiguration(out var errors))
            {
                LogWarning($"默认配置验证失败:\n{string.Join("\n", errors)}");
            }
            else
            {
                LogDebug($"默认配置验证通过: {defaultConfig.configName}");
            }
        }
        
        /// <summary>
        /// 注册事件
        /// </summary>
        private void RegisterEvents()
        {
            SessionStateManager.OnShelfStateChanged += OnShelfStateChanged;
            SessionStateManager.OnSessionReset += OnSessionStateReset;
            ShelfNumberingSystem.OnShelfNumberAssigned += OnShelfNumberAssigned;
        }
        
        /// <summary>
        /// 取消事件注册
        /// </summary>
        private void UnregisterEvents()
        {
            SessionStateManager.OnShelfStateChanged -= OnShelfStateChanged;
            SessionStateManager.OnSessionReset -= OnSessionStateReset;
            ShelfNumberingSystem.OnShelfNumberAssigned -= OnShelfNumberAssigned;
        }
        
        #endregion
        
        #region 主要接口方法
        
        /// <summary>
        /// 尝试为货架生成随机物品
        /// </summary>
        /// <param name="shelfObject">货架GameObject</param>
        /// <param name="targetGrid">目标物品网格</param>
        /// <param name="config">可选的配置覆盖</param>
        /// <returns>是否成功开始生成过程</returns>
        public bool TryGenerateRandomItems(GameObject shelfObject, ItemGrid targetGrid, RandomItemSpawnConfig config = null)
        {
            if (!ValidateGenerationRequest(shelfObject, targetGrid, out string errorMessage))
            {
                LogWarning($"生成请求验证失败: {errorMessage}");
                return false;
            }
            
            // 获取或分配货架编号
            string shelfId = enableAutoNumbering 
                ? ShelfNumberingSystem.GetOrAssignShelfNumber(shelfObject)
                : $"Shelf_{shelfObject.GetInstanceID()}";
            
            // 检查是否已生成
            if (SessionStateManager.IsShelfGenerated(shelfId))
            {
                LogDebug($"货架 '{shelfId}' 在当前会话中已生成过物品，跳过生成");
                return false;
            }
            
            // 检查是否正在生成
            if (activeGenerations.ContainsKey(shelfId))
            {
                LogDebug($"货架 '{shelfId}' 正在生成物品，跳过重复请求");
                return false;
            }
            
            // 获取配置
            var generationConfig = GetConfigForShelf(shelfId, config);
            if (generationConfig == null)
            {
                LogWarning($"无法获取货架 '{shelfId}' 的生成配置");
                return false;
            }
            
            // 开始异步生成
            var generationCoroutine = StartCoroutine(GenerateItemsAsync(shelfId, shelfObject, targetGrid, generationConfig));
            activeGenerations[shelfId] = generationCoroutine;
            
            LogDebug($"开始为货架 '{shelfId}' 生成随机物品");
            return true;
        }
        
        /// <summary>
        /// 强制重新生成货架物品（忽略会话状态）
        /// </summary>
        /// <param name="shelfObject">货架GameObject</param>
        /// <param name="targetGrid">目标物品网格</param>
        /// <param name="config">可选的配置覆盖</param>
        /// <returns>是否成功开始生成过程</returns>
        public bool ForceRegenerateItems(GameObject shelfObject, ItemGrid targetGrid, RandomItemSpawnConfig config = null)
        {
            if (!ValidateGenerationRequest(shelfObject, targetGrid, out string errorMessage))
            {
                LogWarning($"强制重生成请求验证失败: {errorMessage}");
                return false;
            }
            
            string shelfId = enableAutoNumbering 
                ? ShelfNumberingSystem.GetOrAssignShelfNumber(shelfObject)
                : $"Shelf_{shelfObject.GetInstanceID()}";
            
            // 停止现有生成过程
            if (activeGenerations.TryGetValue(shelfId, out var existingCoroutine))
            {
                StopCoroutine(existingCoroutine);
                activeGenerations.Remove(shelfId);
            }
            
            // 清除现有物品
            ClearGridItems(targetGrid);
            
            // 重置会话状态
            SessionStateManager.UnmarkShelfGenerated(shelfId);
            
            // 开始新的生成
            return TryGenerateRandomItems(shelfObject, targetGrid, config);
        }
        
        /// <summary>
        /// 设置货架的配置覆盖
        /// </summary>
        /// <param name="shelfId">货架标识符</param>
        /// <param name="config">配置覆盖</param>
        public void SetConfigOverride(string shelfId, RandomItemSpawnConfig config)
        {
            if (string.IsNullOrEmpty(shelfId))
            {
                LogWarning("货架ID为空，无法设置配置覆盖");
                return;
            }
            
            if (config == null)
            {
                configOverrides.Remove(shelfId);
                LogDebug($"移除货架 '{shelfId}' 的配置覆盖");
            }
            else
            {
                configOverrides[shelfId] = config;
                LogDebug($"设置货架 '{shelfId}' 的配置覆盖: {config.configName}");
            }
        }
        
        /// <summary>
        /// 获取货架的生成状态
        /// </summary>
        /// <param name="shelfObject">货架GameObject</param>
        /// <returns>生成状态信息</returns>
        public ShelfGenerationStatus GetShelfStatus(GameObject shelfObject)
        {
            if (shelfObject == null)
                return new ShelfGenerationStatus { isValid = false, errorMessage = "货架对象为null" };
            
            string shelfId = enableAutoNumbering 
                ? ShelfNumberingSystem.GetShelfNumber(shelfObject)
                : $"Shelf_{shelfObject.GetInstanceID()}";
            
            if (string.IsNullOrEmpty(shelfId))
                shelfId = "Unknown";
            
            var status = new ShelfGenerationStatus
            {
                isValid = true,
                shelfId = shelfId,
                isGenerated = SessionStateManager.IsShelfGenerated(shelfId),
                isGenerating = activeGenerations.ContainsKey(shelfId),
                generationInfo = SessionStateManager.GetShelfGenerationInfo(shelfId),
                hasConfigOverride = configOverrides.ContainsKey(shelfId)
            };
            
            return status;
        }
        
        #endregion
        
        #region 异步生成逻辑
        
        /// <summary>
        /// 异步生成物品
        /// </summary>
        private IEnumerator GenerateItemsAsync(string shelfId, GameObject shelfObject, ItemGrid targetGrid, RandomItemSpawnConfig config)
        {
            float startTime = Time.realtimeSinceStartup;
            bool hasError = false;
            string errorMsg = "";
                
            LogDebug($"开始为货架 '{shelfId}' 异步生成物品");
            
            // 确定生成数量 - 安全调用
            int targetQuantity = 0;
            if (!TryGetRandomTotalQuantity(config, out targetQuantity, out errorMsg))
            {
                LogWarning($"货架 '{shelfId}': {errorMsg}");
                OnGenerationFailed?.Invoke(shelfId, errorMsg);
                hasError = true;
            }
            
            if (!hasError)
            {
                LogDebug($"货架 '{shelfId}' 目标生成数量: {targetQuantity}");
                
                // 选择随机物品 - 安全调用
                RandomItemSelector.SelectionResult selectionResult = default;
                if (!TrySelectRandomItems(config, targetQuantity, out selectionResult, out errorMsg))
                {
                    LogWarning($"货架 '{shelfId}': {errorMsg}");
                    OnGenerationFailed?.Invoke(shelfId, errorMsg);
                    hasError = true;
                }
                
                if (!hasError)
                {
                    LogDebug($"货架 '{shelfId}' 成功选择 {selectionResult.selectedItems.Count} 个物品");
                    
                    // 等待一帧，避免阻塞
                    yield return null;
                    
                    // 使用FixedItemSpawnManager进行实际生成
                    bool spawnSuccess = false;
                    yield return StartCoroutine(SpawnItemsToGrid(targetGrid, selectionResult.selectedItems, config, (success) => {
                        spawnSuccess = success;
                    }));
                    
                    if (!spawnSuccess)
                    {
                        errorMsg = "物品生成到网格失败";
                        LogWarning($"货架 '{shelfId}': {errorMsg}");
                        OnGenerationFailed?.Invoke(shelfId, errorMsg);
                        hasError = true;
                    }
                    else
                    {
                        // 记录生成信息
                        var generationInfo = new ShelfGenerationInfo
                        {
                            shelfId = shelfId,
                            itemCount = selectionResult.selectedItems.Count,
                            configName = config.configName,
                            generatedItems = selectionResult.selectedItems.Select(item => item.itemData.name).ToList()
                        };
                        
                        // 标记为已生成
                        SessionStateManager.MarkShelfGenerated(shelfId, generationInfo);
                        
                        float totalTime = (Time.realtimeSinceStartup - startTime) * 1000f;
                        LogDebug($"货架 '{shelfId}' 生成完成，耗时 {totalTime:F1}ms");
                        
                        // 显示统计信息
                        if (showGenerationStatistics)
                        {
                            ShowGenerationStatistics(shelfId, selectionResult, totalTime);
                        }
                        
                        // 触发生成完成事件
                        OnItemsGenerated?.Invoke(shelfId, selectionResult.selectedItems);
                    }
                }
            }
            
            // 清理活动生成记录
            activeGenerations.Remove(shelfId);
            
            // 检查超时
            float totalTimeSeconds = Time.realtimeSinceStartup - startTime;
            if (totalTimeSeconds > generationTimeout)
            {
                LogWarning($"货架 '{shelfId}' 生成超时 ({totalTimeSeconds:F1}s > {generationTimeout}s)");
            }
        }
        
        /// <summary>
        /// 安全获取随机总数量
        /// </summary>
        private bool TryGetRandomTotalQuantity(RandomItemSpawnConfig config, out int quantity, out string errorMsg)
        {
            quantity = 0;
            errorMsg = "";
            
            try
            {
                if (config == null)
                {
                    errorMsg = "配置为null";
                    return false;
                }
                
                quantity = config.GetRandomTotalQuantity();
                return true;
            }
            catch (System.Exception ex)
            {
                errorMsg = $"获取随机数量失败: {ex.Message}";
                return false;
            }
        }
        
        /// <summary>
        /// 安全选择随机物品
        /// </summary>
        private bool TrySelectRandomItems(RandomItemSpawnConfig config, int targetQuantity, out RandomItemSelector.SelectionResult result, out string errorMsg)
        {
            result = default;
            errorMsg = "";
            
            try
            {
                if (config == null)
                {
                    errorMsg = "配置为null";
                    return false;
                }
                
                result = RandomItemSelector.SelectRandomItems(config, targetQuantity);
                
                if (!result.success)
                {
                    errorMsg = $"物品选择失败: {result.errorMessage}";
                    return false;
                }
                
                return true;
            }
            catch (System.Exception ex)
            {
                errorMsg = $"物品选择过程异常: {ex.Message}";
                return false;
            }
        }
        
        /// <summary>
        /// 将选中的物品生成到网格
        /// </summary>
        private IEnumerator SpawnItemsToGrid(ItemGrid targetGrid, List<RandomItemSelector.SelectedItemInfo> selectedItems, RandomItemSpawnConfig config, System.Action<bool> onComplete)
        {
            if (targetGrid == null || selectedItems == null || selectedItems.Count == 0)
            {
                LogWarning("SpawnItemsToGrid: 无效参数");
                onComplete?.Invoke(false);
                yield break;
            }
            
            // 创建临时的FixedItemSpawnConfig用于生成
            var tempConfig = CreateTempFixedConfig(selectedItems, config);
            
            // 使用FixedItemSpawnManager进行生成
            var spawnManager = FixedItemSpawnManager.Instance;
            if (spawnManager == null)
            {
                LogError("FixedItemSpawnManager实例不存在");
                onComplete?.Invoke(false);
                yield break;
            }
            
            // 使用公共方法进行异步生成
            bool success = false;
            spawnManager.SpawnFixedItems(targetGrid, tempConfig, "random_shelf", (result) => {
                success = result != null && result.isSuccess;
            });
            
            // 等待生成完成（简单的轮询方式）
            float timeout = generationTimeout;
            float elapsed = 0f;
            
            while (elapsed < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
                
                // 这里可以添加更精确的完成检测逻辑
                // 目前使用简单的延时
                if (elapsed >= 1f) // 假设1秒后完成
                {
                    success = true;
                    break;
                }
            }
            
            onComplete?.Invoke(success);
        }
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 验证生成请求
        /// </summary>
        private bool ValidateGenerationRequest(GameObject shelfObject, ItemGrid targetGrid, out string errorMessage)
        {
            errorMessage = "";
            
            if (!isInitialized)
            {
                errorMessage = "管理器未初始化";
                return false;
            }
            
            if (shelfObject == null)
            {
                errorMessage = "货架对象为null";
                return false;
            }
            
            if (targetGrid == null)
            {
                errorMessage = "目标网格为null";
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 获取货架的生成配置
        /// </summary>
        private RandomItemSpawnConfig GetConfigForShelf(string shelfId, RandomItemSpawnConfig configOverride)
        {
            // 优先级：方法参数 > 货架配置覆盖 > 默认配置
            if (configOverride != null)
                return configOverride;
            
            if (configOverrides.TryGetValue(shelfId, out var shelfConfig))
                return shelfConfig;
            
            return defaultConfig;
        }
        
        /// <summary>
        /// 创建临时的固定物品配置
        /// </summary>
        private FixedItemSpawnConfig CreateTempFixedConfig(List<RandomItemSelector.SelectedItemInfo> selectedItems, RandomItemSpawnConfig sourceConfig)
        {
            var tempConfig = ScriptableObject.CreateInstance<FixedItemSpawnConfig>();
            tempConfig.configName = $"Temp_Random_{sourceConfig.configName}";
            tempConfig.targetContainerType = sourceConfig.targetContainerType;
            
            // 转换选中物品为固定物品模板
            var fixedItems = ConvertToFixedTemplates(selectedItems, sourceConfig);
            tempConfig.fixedItems = fixedItems.ToArray();
            
            return tempConfig;
        }
        
        /// <summary>
        /// 将随机选择的物品转换为固定物品模板
        /// </summary>
        /// <param name="selectedItems">选中的物品列表</param>
        /// <param name="sourceConfig">源随机配置</param>
        /// <returns>固定物品模板列表</returns>
        private List<FixedItemTemplate> ConvertToFixedTemplates(List<RandomItemSelector.SelectedItemInfo> selectedItems, RandomItemSpawnConfig sourceConfig)
        {
            var fixedItems = new List<FixedItemTemplate>();
            
            if (selectedItems == null || selectedItems.Count == 0)
                return fixedItems;
            
            // 按选择顺序排序，确保生成顺序的一致性
            var sortedItems = selectedItems.OrderBy(item => item.selectionOrder).ToList();
            
            foreach (var item in sortedItems)
            {
                var template = CreateFixedTemplateFromSelection(item, sourceConfig);
                if (template != null)
                {
                    fixedItems.Add(template);
                }
            }
            
            return fixedItems;
        }
        
        /// <summary>
        /// 从选中物品信息创建固定物品模板
        /// </summary>
        /// <param name="selectedItem">选中的物品信息</param>
        /// <param name="sourceConfig">源随机配置</param>
        /// <returns>固定物品模板</returns>
        private FixedItemTemplate CreateFixedTemplateFromSelection(RandomItemSelector.SelectedItemInfo selectedItem, RandomItemSpawnConfig sourceConfig)
        {
            if (selectedItem.itemData == null)
            {
                LogWarning($"选中物品的ItemData为null，跳过转换");
                return null;
            }
            
            var template = new FixedItemTemplate
            {
                // 基础信息
                templateId = GenerateTemplateId(selectedItem),
                itemData = selectedItem.itemData,
                quantity = selectedItem.quantity,
                
                // 位置配置 - 随机生成使用智能放置
                placementType = PlacementType.Smart,
                exactPosition = Vector2Int.zero,
                constrainedArea = new RectInt(0, 0, sourceConfig.minimumGridSize.x, sourceConfig.minimumGridSize.y),
                preferredArea = new RectInt(0, 0, sourceConfig.minimumGridSize.x, sourceConfig.minimumGridSize.y),
                
                // 生成策略 - 根据珍稀度调整优先级
                priority = MapRarityToPriority(selectedItem.rarity),
                scanPattern = GetOptimalScanPattern(selectedItem.rarity),
                allowRotation = true,
                conflictResolution = ConflictResolutionType.Relocate,
                
                // 生成条件
                isUniqueSpawn = sourceConfig.avoidDuplicateItems,
                maxRetryAttempts = sourceConfig.maxRetryAttempts,
                enableDebugLog = sourceConfig.enableDetailedLogging
            };
            
            // 根据珍稀度调整生成参数
            AdjustTemplateByRarity(template, selectedItem.rarity);
            
            return template;
        }
        
        /// <summary>
        /// 生成模板唯一ID
        /// </summary>
        /// <param name="selectedItem">选中的物品信息</param>
        /// <returns>唯一模板ID</returns>
        private string GenerateTemplateId(RandomItemSelector.SelectedItemInfo selectedItem)
        {
            return $"random_{selectedItem.category}_{selectedItem.rarity}_{selectedItem.itemData.name}_{selectedItem.selectionOrder}";
        }
        
        /// <summary>
        /// 将物品珍稀度映射到生成优先级
        /// </summary>
        /// <param name="rarity">物品珍稀度</param>
        /// <returns>对应的生成优先级</returns>
        private SpawnPriority MapRarityToPriority(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Legendary:
                    return SpawnPriority.Critical;  // 传说物品优先生成
                case ItemRarity.Epic:
                    return SpawnPriority.High;      // 史诗物品高优先级
                case ItemRarity.Rare:
                    return SpawnPriority.Medium;    // 稀有物品中优先级
                case ItemRarity.Common:
                default:
                    return SpawnPriority.Low;       // 普通物品低优先级
            }
        }
        
        /// <summary>
        /// 根据珍稀度获取最优扫描模式
        /// </summary>
        /// <param name="rarity">物品珍稀度</param>
        /// <returns>最优扫描模式</returns>
        private ScanPattern GetOptimalScanPattern(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Legendary:
                    return ScanPattern.CenterToEdge;    // 传说物品从中心开始放置
                case ItemRarity.Epic:
                    return ScanPattern.SpiralOut;       // 史诗物品螺旋放置
                case ItemRarity.Rare:
                    return ScanPattern.LargestGapFirst; // 稀有物品寻找最大空隙
                case ItemRarity.Common:
                default:
                    return ScanPattern.LeftToRight;     // 普通物品从左到右
            }
        }
        
        /// <summary>
        /// 根据珍稀度调整模板参数
        /// </summary>
        /// <param name="template">固定物品模板</param>
        /// <param name="rarity">物品珍稀度</param>
        private void AdjustTemplateByRarity(FixedItemTemplate template, ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Legendary:
                    // 传说物品：最高重试次数，优先区域为中心
                    template.maxRetryAttempts = 10;
                    template.conflictResolution = ConflictResolutionType.ForceReplace;
                    AdjustPreferredAreaToCenter(template);
                    break;
                    
                case ItemRarity.Epic:
                    // 史诗物品：高重试次数，延后生成避免冲突
                    template.maxRetryAttempts = 7;
                    template.conflictResolution = ConflictResolutionType.Defer;
                    break;
                    
                case ItemRarity.Rare:
                    // 稀有物品：中等重试次数，重新定位
                    template.maxRetryAttempts = 5;
                    template.conflictResolution = ConflictResolutionType.Relocate;
                    break;
                    
                case ItemRarity.Common:
                default:
                    // 普通物品：标准设置
                    template.maxRetryAttempts = 3;
                    template.conflictResolution = ConflictResolutionType.Skip;
                    break;
            }
        }
        
        /// <summary>
        /// 调整优先区域到网格中心
        /// </summary>
        /// <param name="template">固定物品模板</param>
        private void AdjustPreferredAreaToCenter(FixedItemTemplate template)
        {
            var gridWidth = template.constrainedArea.width;
            var gridHeight = template.constrainedArea.height;
            
            // 计算中心区域（约1/4大小）
            int centerWidth = Mathf.Max(1, gridWidth / 4);
            int centerHeight = Mathf.Max(1, gridHeight / 4);
            int centerX = (gridWidth - centerWidth) / 2;
            int centerY = (gridHeight - centerHeight) / 2;
            
            template.preferredArea = new RectInt(centerX, centerY, centerWidth, centerHeight);
        }
        
        /// <summary>
        /// 清空网格物品
        /// </summary>
        private void ClearGridItems(ItemGrid targetGrid)
        {
            if (targetGrid == null) return;
            
            try
            {
                // 这里需要调用ItemGrid的清空方法
                // 具体实现取决于ItemGrid的API
                LogDebug("清空网格物品");
            }
            catch (Exception ex)
            {
                LogError($"清空网格物品时发生异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 显示生成统计信息
        /// </summary>
        private void ShowGenerationStatistics(string shelfId, RandomItemSelector.SelectionResult result, float generationTime)
        {
            var debugInfo = RandomItemSelector.GetSelectionDebugInfo(result);
            LogDebug($"货架 '{shelfId}' 生成统计:\n{debugInfo}\n生成耗时: {generationTime:F1}ms");
        }
        
        #endregion
        
        #region 事件处理
        
        /// <summary>
        /// 处理货架状态变更事件
        /// </summary>
        private void OnShelfStateChanged(string shelfId, bool isGenerated)
        {
            LogDebug($"货架状态变更: {shelfId} -> {(isGenerated ? "已生成" : "未生成")}");
        }
        
        /// <summary>
        /// 处理会话状态重置事件
        /// </summary>
        private void OnSessionStateReset()
        {
            LogDebug("会话状态已重置，所有货架将重新可生成");
            OnSessionReset?.Invoke();
        }
        
        /// <summary>
        /// 处理货架编号分配事件
        /// </summary>
        private void OnShelfNumberAssigned(GameObject shelfObject, string shelfId)
        {
            LogDebug($"货架编号已分配: {shelfObject.name} -> {shelfId}");
        }
        
        #endregion
        
        #region 公共工具方法
        
        /// <summary>
        /// 重置会话状态
        /// </summary>
        public void ResetSession()
        {
            LogDebug("手动重置会话状态");
            SessionStateManager.ResetSessionState();
            ShelfNumberingSystem.ResetNumbering();
            
            // 停止所有正在进行的生成
            foreach (var kvp in activeGenerations)
            {
                if (kvp.Value != null)
                {
                    StopCoroutine(kvp.Value);
                }
            }
            activeGenerations.Clear();
        }
        
        /// <summary>
        /// 获取管理器统计信息
        /// </summary>
        public ManagerStatistics GetManagerStatistics()
        {
            var sessionStats = SessionStateManager.GetSessionStatistics();
            var numberingStats = ShelfNumberingSystem.GetStatistics();
            
            return new ManagerStatistics
            {
                isInitialized = isInitialized,
                sessionStatistics = sessionStats,
                numberingStatistics = numberingStats,
                activeGenerationsCount = activeGenerations.Count,
                configOverridesCount = configOverrides.Count,
                hasDefaultConfig = defaultConfig != null
            };
        }
        
        /// <summary>
        /// 获取详细的诊断报告
        /// </summary>
        /// <returns>诊断报告字符串</returns>
        public string GetDiagnosticReport()
        {
            var report = new System.Text.StringBuilder();
            var stats = GetManagerStatistics();
            
            report.AppendLine("=== 货架随机物品管理器诊断报告 ===");
            report.AppendLine($"初始化状态: {(stats.isInitialized ? "已初始化" : "未初始化")}");
            report.AppendLine($"默认配置: {(stats.hasDefaultConfig ? defaultConfig.configName : "无")}");
            report.AppendLine($"活动生成数: {stats.activeGenerationsCount}");
            report.AppendLine($"配置覆盖数: {stats.configOverridesCount}");
            report.AppendLine();
            
            // 会话统计
            report.AppendLine("=== 会话统计 ===");
            report.AppendLine($"会话ID: {stats.sessionStatistics.sessionId}");
            report.AppendLine($"会话时长: {stats.sessionStatistics.sessionDuration.TotalMinutes:F1} 分钟");
            report.AppendLine($"已生成货架数: {stats.sessionStatistics.totalGeneratedShelves}");
            report.AppendLine($"生成物品总数: {stats.sessionStatistics.totalItemsGenerated}");
            report.AppendLine($"平均每货架物品数: {stats.sessionStatistics.averageItemsPerShelf:F1}");
            report.AppendLine();
            
            // 编号统计
            report.AppendLine("=== 编号统计 ===");
            report.AppendLine($"已分配编号: {stats.numberingStatistics.totalAssigned}");
            report.AppendLine($"已使用编号: {stats.numberingStatistics.usedNumbers}");
            report.AppendLine($"下一个编号索引: {stats.numberingStatistics.nextNumberIndex}");
            report.AppendLine($"可用基础编号: {stats.numberingStatistics.availableBasicNumbers}");
            report.AppendLine($"使用扩展编号: {stats.numberingStatistics.usingExtendedNumbers}");
            report.AppendLine();
            
            // 活动生成列表
            if (activeGenerations.Count > 0)
            {
                report.AppendLine("=== 活动生成 ===");
                foreach (var shelfId in activeGenerations.Keys)
                {
                    report.AppendLine($"  {shelfId}");
                }
                report.AppendLine();
            }
            
            // 配置覆盖列表
            if (configOverrides.Count > 0)
            {
                report.AppendLine("=== 配置覆盖 ===");
                foreach (var kvp in configOverrides)
                {
                    report.AppendLine($"  {kvp.Key} -> {kvp.Value.configName}");
                }
                report.AppendLine();
            }
            
            // 生成历史
            if (stats.sessionStatistics.generationHistory != null && stats.sessionStatistics.generationHistory.Count > 0)
            {
                report.AppendLine("=== 最近生成历史 ===");
                var recentHistory = stats.sessionStatistics.generationHistory
                    .OrderByDescending(h => h.generationTime)
                    .Take(5);
                
                foreach (var history in recentHistory)
                {
                    report.AppendLine($"  {history.shelfId}: {history.itemCount} 物品, {history.generationTime:HH:mm:ss}");
                }
            }
            
            return report.ToString();
        }
        
        /// <summary>
        /// 验证管理器状态的完整性
        /// </summary>
        /// <param name="issues">发现的问题列表</param>
        /// <returns>状态是否正常</returns>
        public bool ValidateManagerState(out List<string> issues)
        {
            issues = new List<string>();
            bool isValid = true;
            
            // 检查初始化状态
            if (!isInitialized)
            {
                issues.Add("管理器未正确初始化");
                isValid = false;
            }
            
            // 检查默认配置
            if (defaultConfig == null)
            {
                issues.Add("缺少默认配置");
                isValid = false;
            }
            else if (!defaultConfig.ValidateConfiguration(out var configErrors))
            {
                issues.Add($"默认配置无效: {string.Join(", ", configErrors)}");
                isValid = false;
            }
            
            // 检查配置覆盖
            foreach (var kvp in configOverrides)
            {
                if (kvp.Value == null)
                {
                    issues.Add($"货架 '{kvp.Key}' 的配置覆盖为null");
                    isValid = false;
                }
                else if (!kvp.Value.ValidateConfiguration(out var overrideErrors))
                {
                    issues.Add($"货架 '{kvp.Key}' 的配置覆盖无效: {string.Join(", ", overrideErrors)}");
                    isValid = false;
                }
            }
            
            // 检查会话状态一致性
            if (!SessionStateManager.ValidateSessionState(out var sessionIssues))
            {
                issues.AddRange(sessionIssues.Select(issue => $"会话状态: {issue}"));
                isValid = false;
            }
            
            // 检查编号系统一致性
            if (!ShelfNumberingSystem.ValidateConsistency(out var numberingIssues))
            {
                issues.AddRange(numberingIssues.Select(issue => $"编号系统: {issue}"));
                isValid = false;
            }
            
            return isValid;
        }
        
        /// <summary>
        /// 执行性能测试
        /// </summary>
        /// <param name="testConfig">测试配置</param>
        /// <param name="iterations">测试迭代次数</param>
        /// <returns>性能测试结果</returns>
        public PerformanceTestResult RunPerformanceTest(RandomItemSpawnConfig testConfig, int iterations = 100)
        {
            if (testConfig == null)
            {
                return new PerformanceTestResult
                {
                    success = false,
                    errorMessage = "测试配置为null"
                };
            }
            
            var result = new PerformanceTestResult
            {
                success = true,
                iterations = iterations,
                configName = testConfig.configName
            };
            
            var selectionTimes = new List<float>();
            var conversionTimes = new List<float>();
            var totalItems = 0;
            
            for (int i = 0; i < iterations; i++)
            {
                // 测试物品选择性能
                float selectionStart = Time.realtimeSinceStartup;
                int targetQuantity = testConfig.GetRandomTotalQuantity(i);
                var selectionResult = RandomItemSelector.SelectRandomItems(testConfig, targetQuantity, i);
                float selectionTime = (Time.realtimeSinceStartup - selectionStart) * 1000f;
                selectionTimes.Add(selectionTime);
                
                if (selectionResult.success)
                {
                    totalItems += selectionResult.selectedItems.Count;
                    
                    // 测试转换性能
                    float conversionStart = Time.realtimeSinceStartup;
                    var fixedTemplates = ConvertToFixedTemplates(selectionResult.selectedItems, testConfig);
                    float conversionTime = (Time.realtimeSinceStartup - conversionStart) * 1000f;
                    conversionTimes.Add(conversionTime);
                }
            }
            
            // 计算统计信息
            result.averageSelectionTime = selectionTimes.Average();
            result.maxSelectionTime = selectionTimes.Max();
            result.minSelectionTime = selectionTimes.Min();
            
            result.averageConversionTime = conversionTimes.Average();
            result.maxConversionTime = conversionTimes.Max();
            result.minConversionTime = conversionTimes.Min();
            
            result.averageItemsPerGeneration = (float)totalItems / iterations;
            
            return result;
        }
        
        #endregion
        
        #region 编辑器调试方法
        
        #if UNITY_EDITOR
        
        /// <summary>
        /// 显示管理器状态
        /// </summary>
        [ContextMenu("显示管理器状态")]
        public void ShowManagerStatus()
        {
            Debug.Log(GetDiagnosticReport());
        }
        
        /// <summary>
        /// 验证管理器状态
        /// </summary>
        [ContextMenu("验证管理器状态")]
        public void ValidateManagerStatus()
        {
            if (ValidateManagerState(out var issues))
            {
                Debug.Log("管理器状态验证通过！");
            }
            else
            {
                Debug.LogWarning($"管理器状态验证失败:\n{string.Join("\n", issues)}");
            }
        }
        
        /// <summary>
        /// 重置会话（编辑器）
        /// </summary>
        [ContextMenu("重置会话状态")]
        public void ResetSessionEditor()
        {
            ResetSession();
            Debug.Log("会话状态已重置");
        }
        
        /// <summary>
        /// 执行性能测试（编辑器）
        /// </summary>
        [ContextMenu("执行性能测试")]
        public void RunPerformanceTestEditor()
        {
            if (defaultConfig == null)
            {
                Debug.LogWarning("没有默认配置，无法执行性能测试");
                return;
            }
            
            var result = RunPerformanceTest(defaultConfig, 50);
            if (result.success)
            {
                Debug.Log($"性能测试完成:\n" +
                         $"选择时间: 平均 {result.averageSelectionTime:F2}ms, 最大 {result.maxSelectionTime:F2}ms\n" +
                         $"转换时间: 平均 {result.averageConversionTime:F2}ms, 最大 {result.maxConversionTime:F2}ms\n" +
                         $"平均物品数: {result.averageItemsPerGeneration:F1}");
            }
            else
            {
                Debug.LogError($"性能测试失败: {result.errorMessage}");
            }
        }
        
        /// <summary>
        /// 清理无效分配
        /// </summary>
        [ContextMenu("清理无效分配")]
        public void CleanupInvalidAssignments()
        {
            int cleaned = ShelfNumberingSystem.CleanupInvalidAssignments();
            Debug.Log($"清理了 {cleaned} 个无效的编号分配");
        }
        
        /// <summary>
        /// 模拟配置验证
        /// </summary>
        [ContextMenu("模拟配置验证")]
        public void SimulateConfigValidation()
        {
            if (defaultConfig == null)
            {
                Debug.LogWarning("没有默认配置可验证");
                return;
            }
            
            var distribution = RandomItemSelector.SimulateSelectionDistribution(defaultConfig, 1000);
            
            Debug.Log("配置验证 - 1000次模拟的珍稀度分布:\n" +
                     $"普通: {distribution[ItemRarity.Common]:P1}\n" +
                     $"稀有: {distribution[ItemRarity.Rare]:P1}\n" +
                     $"史诗: {distribution[ItemRarity.Epic]:P1}\n" +
                     $"传说: {distribution[ItemRarity.Legendary]:P1}");
        }
        
        #endif
        
        #endregion
        
        #region 日志方法
        
        private void LogDebug(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[ShelfRandomItemManager] {message}");
            }
        }
        
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[ShelfRandomItemManager] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[ShelfRandomItemManager] {message}");
        }
        
        #endregion
    }
    
    /// <summary>
    /// 货架生成状态
    /// </summary>
    [System.Serializable]
    public struct ShelfGenerationStatus
    {
        public bool isValid;                    // 状态是否有效
        public string shelfId;                  // 货架标识符
        public bool isGenerated;                // 是否已生成
        public bool isGenerating;               // 是否正在生成
        public ShelfGenerationInfo generationInfo; // 生成详细信息
        public bool hasConfigOverride;          // 是否有配置覆盖
        public string errorMessage;             // 错误信息
    }
    
    /// <summary>
    /// 管理器统计信息
    /// </summary>
    [System.Serializable]
    public struct ManagerStatistics
    {
        public bool isInitialized;              // 是否已初始化
        public SessionStatistics sessionStatistics;    // 会话统计
        public NumberingStatistics numberingStatistics; // 编号统计
        public int activeGenerationsCount;      // 活动生成数量
        public int configOverridesCount;        // 配置覆盖数量
        public bool hasDefaultConfig;           // 是否有默认配置
    }
    
    /// <summary>
    /// 性能测试结果
    /// </summary>
    [System.Serializable]
    public struct PerformanceTestResult
    {
        public bool success;                    // 测试是否成功
        public string errorMessage;             // 错误信息
        public string configName;               // 测试配置名称
        public int iterations;                  // 测试迭代次数
        
        // 选择性能
        public float averageSelectionTime;      // 平均选择时间（毫秒）
        public float maxSelectionTime;          // 最大选择时间（毫秒）
        public float minSelectionTime;          // 最小选择时间（毫秒）
        
        // 转换性能
        public float averageConversionTime;     // 平均转换时间（毫秒）
        public float maxConversionTime;         // 最大转换时间（毫秒）
        public float minConversionTime;         // 最小转换时间（毫秒）
        
        // 其他统计
        public float averageItemsPerGeneration; // 平均每次生成的物品数
        
        public override string ToString()
        {
            return success 
                ? $"Performance Test [{configName}]: {iterations} iterations, " +
                  $"Selection: {averageSelectionTime:F2}ms avg, Conversion: {averageConversionTime:F2}ms avg"
                : $"Performance Test Failed: {errorMessage}";
        }
    }
}
