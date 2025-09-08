using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using InventorySystem;

namespace InventorySystem.SpawnSystem
{
    /// <summary>
    /// 生成结果类型
    /// </summary>
    public enum SpawnResultType
    {
        [InspectorName("成功")] Success = 0,
        [InspectorName("跳过")] Skipped = 1,
        [InspectorName("失败")] Failed = 2,
        [InspectorName("条件不满足")] ConditionNotMet = 3
    }
    
    /// <summary>
    /// 单个物品生成结果
    /// </summary>
    [System.Serializable]
    public class SingleItemSpawnResult
    {
        public string templateId;
        public string itemId;
        public SpawnResultType resultType;
        public Vector2Int position;
        public Vector2Int itemSize;
        public bool wasRotated;
        public string failureReason;
        public float spawnTime;
        
        public bool IsSuccess => resultType == SpawnResultType.Success;
    }
    
    /// <summary>
    /// 完整生成结果
    /// </summary>
    [System.Serializable]
    public class SpawnResult
    {
        public bool isSuccess;
        public int totalItems;
        public int successfulItems;
        public int skippedItems;
        public int failedItems;
        public float totalSpawnTime;
        public List<SingleItemSpawnResult> itemResults;
        public string summaryMessage;
        
        public SpawnResult()
        {
            itemResults = new List<SingleItemSpawnResult>();
        }
        
        public void AddItemResult(SingleItemSpawnResult result)
        {
            itemResults.Add(result);
            totalItems++;
            
            switch (result.resultType)
            {
                case SpawnResultType.Success:
                    successfulItems++;
                    break;
                case SpawnResultType.Skipped:
                    skippedItems++;
                    break;
                case SpawnResultType.Failed:
                    failedItems++;
                    break;
            }
        }
        
        public float GetSuccessRate()
        {
            return totalItems > 0 ? (float)successfulItems / totalItems : 0f;
        }
    }
    
    /// <summary>
    /// 固定物品生成管理器
    /// 负责协调整个固定物品生成流程的核心管理器
    /// </summary>
    public class FixedItemSpawnManager : MonoBehaviour
    {
        #region 单例模式
        
        private static FixedItemSpawnManager instance;
        
        public static FixedItemSpawnManager Instance
        {
            get
            {
                if (instance == null)
                {
                    // 查找现有实例
                    instance = UnityEngine.Object.FindObjectOfType<FixedItemSpawnManager>();
                    
                    // 如果没有找到，创建新实例
                    if (instance == null)
                    {
                        GameObject managerObj = new GameObject("FixedItemSpawnManager");
                        instance = managerObj.AddComponent<FixedItemSpawnManager>();
                        DontDestroyOnLoad(managerObj);
                    }
                }
                return instance;
            }
        }
        
        #endregion
        
        #region 序列化字段
        
        [Header("调试设置")]
        [FieldLabel("启用详细日志")]
        [SerializeField] private bool enableDetailedLogging = true;
        
        [FieldLabel("异步生成")]
        [SerializeField] private bool useAsyncSpawning = true;
        
        [Header("性能设置")]
        [FieldLabel("每帧最大生成数")]
        [Range(1, 10)]
        [SerializeField] private int maxSpawnsPerFrame = 3;
        
        [FieldLabel("生成间隔时间")]
        [Range(0f, 1f)]
        [SerializeField] private float spawnInterval = 0.1f;
        
        #endregion
        
        #region 私有字段
        
        private SpawnStateTracker stateTracker;
        private Dictionary<string, FixedItemSpawnConfig> loadedConfigs;
        private Queue<SpawnRequest> spawnQueue;
        private bool isSpawning;
        
        #endregion
        
        #region 内部类
        
        /// <summary>
        /// 生成请求
        /// </summary>
        private class SpawnRequest
        {
            public ItemGrid targetGrid;
            public FixedItemSpawnConfig config;
            public string containerId;
            public System.Action<SpawnResult> onComplete;
            
            public SpawnRequest(ItemGrid grid, FixedItemSpawnConfig cfg, string id, System.Action<SpawnResult> callback)
            {
                targetGrid = grid;
                config = cfg;
                containerId = id;
                onComplete = callback;
            }
        }
        
        #endregion
        
        #region 生命周期
        
        private void Awake()
        {
            // 单例模式处理
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void Initialize()
        {
            try
            {
                // 初始化状态追踪器
                stateTracker = SpawnStateTracker.Instance;
                if (stateTracker != null)
                {
                    stateTracker.EnableDebugLog = enableDetailedLogging;
                }
                else
                {
                    LogError("无法获取 SpawnStateTracker 实例");
                }
                
                // 初始化集合
                loadedConfigs = new Dictionary<string, FixedItemSpawnConfig>();
                spawnQueue = new Queue<SpawnRequest>();
                isSpawning = false;
                
                LogDebug("FixedItemSpawnManager 初始化完成");
            }
            catch (System.Exception ex)
            {
                LogError($"FixedItemSpawnManager 初始化失败: {ex.Message}");
                
                // 至少确保基本集合被初始化
                if (loadedConfigs == null)
                    loadedConfigs = new Dictionary<string, FixedItemSpawnConfig>();
                if (spawnQueue == null)
                    spawnQueue = new Queue<SpawnRequest>();
                
                isSpawning = false;
            }
        }
        
        private void OnDestroy()
        {
            stateTracker?.Cleanup();
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                stateTracker?.SaveStateData();
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                stateTracker?.SaveStateData();
            }
        }
        
        #endregion
        
        #region 公共接口
        
        /// <summary>
        /// 为容器生成固定物品
        /// </summary>
        public void SpawnFixedItems(ItemGrid targetGrid, FixedItemSpawnConfig config, 
                                  string containerId = null, System.Action<SpawnResult> onComplete = null)
        {
            // 确保初始化完成
            if (spawnQueue == null || loadedConfigs == null)
            {
                LogError("FixedItemSpawnManager 未正确初始化，尝试重新初始化...");
                Initialize();
                
                if (spawnQueue == null)
                {
                    LogError("FixedItemSpawnManager 初始化失败");
                    onComplete?.Invoke(CreateFailureResult("生成管理器初始化失败"));
                    return;
                }
            }
            
            if (targetGrid == null)
            {
                LogError("目标网格不能为空");
                onComplete?.Invoke(CreateFailureResult("目标网格为空"));
                return;
            }
            
            if (config == null)
            {
                LogError("生成配置不能为空");
                onComplete?.Invoke(CreateFailureResult("生成配置为空"));
                return;
            }
            
            // 验证配置
            if (!config.ValidateConfig(out var errors))
            {
                string errorMessage = $"配置验证失败: {string.Join(", ", errors)}";
                LogError(errorMessage);
                onComplete?.Invoke(CreateFailureResult(errorMessage));
                return;
            }
            
            containerId = containerId ?? GetDefaultContainerId(targetGrid);
            
            LogDebug($"请求为容器 {containerId} 生成固定物品，配置: {config.configName}");
            
            var request = new SpawnRequest(targetGrid, config, containerId, onComplete);
            
            if (useAsyncSpawning)
            {
                spawnQueue.Enqueue(request);
                StartCoroutine(ProcessSpawnQueue());
            }
            else
            {
                StartCoroutine(ProcessSpawnRequest(request));
            }
        }
        
        /// <summary>
        /// 立即为容器生成固定物品（同步版本）
        /// </summary>
        public SpawnResult SpawnFixedItemsImmediate(ItemGrid targetGrid, FixedItemSpawnConfig config, 
                                                  string containerId = null)
        {
            containerId = containerId ?? GetDefaultContainerId(targetGrid);
            
            LogDebug($"立即为容器 {containerId} 生成固定物品");
            
            return SpawnItemsInternal(targetGrid, config, containerId);
        }
        
        /// <summary>
        /// 检查容器是否需要生成物品
        /// </summary>
        public bool ShouldSpawnForContainer(ItemGrid targetGrid, FixedItemSpawnConfig config, 
                                          string containerId = null)
        {
            containerId = containerId ?? GetDefaultContainerId(targetGrid);
            
            var sortedItems = config.GetSortedItems();
            
            foreach (var template in sortedItems)
            {
                if (stateTracker.ShouldSpawnItem(containerId, template.templateId, targetGrid))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 重置容器的生成状态
        /// </summary>
        public void ResetContainerSpawnState(string containerId)
        {
            stateTracker.ResetContainerSpawnState(containerId);
            LogDebug($"重置容器 {containerId} 的生成状态");
        }
        
        /// <summary>
        /// 重置所有生成状态
        /// </summary>
        public void ResetAllSpawnStates()
        {
            stateTracker.ResetAllSpawnStates();
            LogDebug("重置所有生成状态");
        }
        
        #endregion
        
        #region 内部实现
        
        /// <summary>
        /// 创建模板实例副本（用于多数量生成）
        /// </summary>
        private FixedItemTemplate CreateInstanceTemplate(FixedItemTemplate original, string instanceId, int quantity)
        {
            // 创建模板副本，避免修改原始模板
            var instanceTemplate = new FixedItemTemplate
            {
                templateId = instanceId,
                itemData = original.itemData,
                quantity = quantity, // 单个实例的数量为1
                placementType = original.placementType,
                exactPosition = original.exactPosition,
                constrainedArea = original.constrainedArea,
                preferredArea = original.preferredArea,
                priority = original.priority,
                scanPattern = original.scanPattern,
                allowRotation = original.allowRotation,
                conflictResolution = original.conflictResolution,
                isUniqueSpawn = false, // 实例不是唯一生成
                conditionTags = original.conditionTags,
                maxRetryAttempts = original.maxRetryAttempts,
                enableDebugLog = original.enableDebugLog
            };
            
            return instanceTemplate;
        }
        
        /// <summary>
        /// 处理生成队列
        /// </summary>
        private IEnumerator ProcessSpawnQueue()
        {
            if (isSpawning) yield break;
            
            isSpawning = true;
            
            while (spawnQueue.Count > 0)
            {
                var request = spawnQueue.Dequeue();
                yield return StartCoroutine(ProcessSpawnRequest(request));
                
                if (spawnInterval > 0)
                {
                    yield return new WaitForSeconds(spawnInterval);
                }
            }
            
            isSpawning = false;
        }
        
        /// <summary>
        /// 处理单个生成请求
        /// </summary>
        private IEnumerator ProcessSpawnRequest(SpawnRequest request)
        {
            LogDebug($"开始处理生成请求: {request.config.configName}");
            
            SpawnResult result = null;
            
            if (useAsyncSpawning)
            {
                yield return StartCoroutine(SpawnItemsAsync(request.targetGrid, request.config, request.containerId, 
                    (spawnResult) => result = spawnResult));
            }
            else
            {
                result = SpawnItemsInternal(request.targetGrid, request.config, request.containerId);
                yield return null;
            }
            
            request.onComplete?.Invoke(result);
        }
        
        /// <summary>
        /// 异步生成物品
        /// </summary>
        private IEnumerator SpawnItemsAsync(ItemGrid targetGrid, FixedItemSpawnConfig config, 
                                          string containerId, System.Action<SpawnResult> onComplete)
        {
            var result = new SpawnResult();
            var startTime = Time.realtimeSinceStartup;
            
            // 分析网格
            var analyzer = new GridOccupancyAnalyzer(targetGrid);
            analyzer.AnalyzeGrid();
            
            yield return null;
            
            // 创建智能放置策略
            var strategy = new SmartPlacementStrategy(analyzer, enableDetailedLogging);
            
            // 获取排序后的物品列表
            var sortedItems = config.GetSortedItems();
            
            int processedCount = 0;
            
            foreach (var template in sortedItems)
            {
                // 检查是否需要生成
                if (!stateTracker.ShouldSpawnItem(containerId, template.templateId, targetGrid))
                {
                    var skipResult = new SingleItemSpawnResult
                    {
                        templateId = template.templateId,
                        itemId = template.itemData?.name ?? "",
                        resultType = SpawnResultType.Skipped,
                        failureReason = "物品已存在或已被消耗"
                    };
                    result.AddItemResult(skipResult);
                    continue;
                }
                
                // 🔧 修复：根据template.quantity生成指定数量的物品
                int successfulCount = 0;
                int totalQuantity = Mathf.Max(1, template.quantity); // 确保至少生成1个
                
                LogDebug($"准备异步生成物品 {template.templateId}，数量: {totalQuantity}");
                
                for (int i = 0; i < totalQuantity; i++)
                {
                    // 为每个物品实例生成唯一的模板ID
                    string instanceTemplateId = totalQuantity > 1 ? $"{template.templateId}_instance_{i + 1}" : template.templateId;
                    
                    // 创建临时模板副本，用于单个实例
                    var instanceTemplate = CreateInstanceTemplate(template, instanceTemplateId, 1); // 单个实例的数量为1
                    
                    // 生成单个物品实例
                    var itemResult = SpawnSingleItem(targetGrid, instanceTemplate, containerId, analyzer, strategy);
                    
                    // 修改结果以反映原始模板ID
                    itemResult.templateId = template.templateId; // 保持原始模板ID用于统计
                    
                    result.AddItemResult(itemResult);
                    
                    // 如果物品生成成功，重新分析网格状态
                    if (itemResult.IsSuccess)
                    {
                        successfulCount++;
                        LogDebug($"物品 {template.templateId} 实例 {i + 1}/{totalQuantity} 异步生成成功，重新分析网格状态");
                        analyzer.AnalyzeGrid(true); // 强制重新分析
                        strategy = new SmartPlacementStrategy(analyzer, enableDetailedLogging); // 重新创建策略
                    }
                    else
                    {
                        LogWarning($"物品 {template.templateId} 实例 {i + 1}/{totalQuantity} 异步生成失败: {itemResult.failureReason}");
                    }
                    
                    processedCount++;
                    
                    // 控制每帧处理数量
                    if (processedCount >= maxSpawnsPerFrame)
                    {
                        processedCount = 0;
                        yield return null;
                    }
                }
                
                LogDebug($"物品 {template.templateId} 异步生成完成: 成功 {successfulCount}/{totalQuantity}");
            }
            
            result.totalSpawnTime = Time.realtimeSinceStartup - startTime;
            result.isSuccess = result.successfulItems > 0;
            result.summaryMessage = GenerateSummaryMessage(result);
            
            LogDebug($"异步生成完成: {result.summaryMessage}");
            
            onComplete?.Invoke(result);
        }
        
        /// <summary>
        /// 内部同步生成方法
        /// </summary>
        private SpawnResult SpawnItemsInternal(ItemGrid targetGrid, FixedItemSpawnConfig config, string containerId)
        {
            var result = new SpawnResult();
            var startTime = Time.realtimeSinceStartup;
            
            LogDebug($"开始为容器 {containerId} 生成固定物品");
            
            // 分析网格状态
            var analyzer = new GridOccupancyAnalyzer(targetGrid);
            analyzer.AnalyzeGrid();
            
            // 创建智能放置策略
            var strategy = new SmartPlacementStrategy(analyzer, enableDetailedLogging);
            
            // 获取排序后的物品列表
            var sortedItems = config.GetSortedItems();
            
            LogDebug($"准备生成 {sortedItems.Count} 个物品模板");
            
            foreach (var template in sortedItems)
            {
                // 检查是否需要生成此物品
                if (!stateTracker.ShouldSpawnItem(containerId, template.templateId, targetGrid))
                {
                    var skipResult = new SingleItemSpawnResult
                    {
                        templateId = template.templateId,
                        itemId = template.itemData?.name ?? "",
                        resultType = SpawnResultType.Skipped,
                        failureReason = "物品已存在或已被消耗"
                    };
                    result.AddItemResult(skipResult);
                    continue;
                }
                
                // 🔧 修复：根据template.quantity生成指定数量的物品
                int successfulCount = 0;
                int totalQuantity = Mathf.Max(1, template.quantity); // 确保至少生成1个
                
                LogDebug($"准备生成物品 {template.templateId}，数量: {totalQuantity}");
                
                for (int i = 0; i < totalQuantity; i++)
                {
                    // 为每个物品实例生成唯一的模板ID
                    string instanceTemplateId = totalQuantity > 1 ? $"{template.templateId}_instance_{i + 1}" : template.templateId;
                    
                    // 创建临时模板副本，用于单个实例
                    var instanceTemplate = CreateInstanceTemplate(template, instanceTemplateId, 1); // 单个实例的数量为1
                    
                    // 生成单个物品实例
                    var itemResult = SpawnSingleItem(targetGrid, instanceTemplate, containerId, analyzer, strategy);
                    
                    // 修改结果以反映原始模板ID
                    itemResult.templateId = template.templateId; // 保持原始模板ID用于统计
                    
                    result.AddItemResult(itemResult);
                    
                    // 如果物品生成成功，重新分析网格状态
                    if (itemResult.IsSuccess)
                    {
                        successfulCount++;
                        LogDebug($"物品 {template.templateId} 实例 {i + 1}/{totalQuantity} 生成成功，重新分析网格状态");
                        analyzer.AnalyzeGrid(true); // 强制重新分析
                        strategy = new SmartPlacementStrategy(analyzer, enableDetailedLogging); // 重新创建策略
                    }
                    else
                    {
                        LogWarning($"物品 {template.templateId} 实例 {i + 1}/{totalQuantity} 生成失败: {itemResult.failureReason}");
                        
                        // 如果是关键物品且生成失败，根据配置决定是否继续
                        if (template.priority == SpawnPriority.Critical && !config.continueOnFailure)
                        {
                            LogError($"关键物品 {template.templateId} 实例 {i + 1} 生成失败，停止后续生成");
                            goto exitLoop; // 跳出双层循环
                        }
                    }
                }
                
                LogDebug($"物品 {template.templateId} 生成完成: 成功 {successfulCount}/{totalQuantity}");
            }
            
            exitLoop:;
            
            result.totalSpawnTime = Time.realtimeSinceStartup - startTime;
            result.isSuccess = result.successfulItems > 0;
            result.summaryMessage = GenerateSummaryMessage(result);
            
            LogDebug($"生成完成: {result.summaryMessage}");
            
            return result;
        }
        
        /// <summary>
        /// 生成单个物品
        /// </summary>
        private SingleItemSpawnResult SpawnSingleItem(ItemGrid targetGrid, FixedItemTemplate template, 
                                                    string containerId, GridOccupancyAnalyzer analyzer, 
                                                    SmartPlacementStrategy strategy)
        {
            var startTime = Time.realtimeSinceStartup;
            
            LogDebug($"开始生成物品: {template.templateId}");
            
            // 验证模板配置
            if (!template.IsValid(out string errorMessage))
            {
                LogError($"物品模板配置无效: {template.templateId}, 错误: {errorMessage}");
                return new SingleItemSpawnResult
                {
                    templateId = template.templateId,
                    itemId = template.itemData?.name ?? "",
                    resultType = SpawnResultType.Failed,
                    failureReason = $"模板配置无效: {errorMessage}",
                    spawnTime = Time.realtimeSinceStartup - startTime
                };
            }
            
            // 寻找最佳位置
            Vector2Int? position = strategy.FindBestPosition(template);
            
            if (!position.HasValue)
            {
                return new SingleItemSpawnResult
                {
                    templateId = template.templateId,
                    itemId = template.itemData?.name ?? "",
                    resultType = SpawnResultType.Failed,
                    failureReason = "无法找到合适的放置位置",
                    spawnTime = Time.realtimeSinceStartup - startTime
                };
            }
            
            // 使用模板获取物品尺寸进行验证
            Vector2Int itemSize = template.GetItemSize();
            
            // 使用分析器验证位置是否可用
            bool canPlace = analyzer.CanPlaceItemAtPosition(position.Value, itemSize);
            LogDebug($"位置验证: ({position.Value.x}, {position.Value.y}) 尺寸 {itemSize} - {(canPlace ? "可用" : "不可用")}");
            
            if (!canPlace)
            {
                LogError($"位置 ({position.Value.x}, {position.Value.y}) 不可用，物品尺寸 {itemSize}");
                return new SingleItemSpawnResult
                {
                    templateId = template.templateId,
                    itemId = template.itemData?.name ?? "",
                    resultType = SpawnResultType.Failed,
                    failureReason = "位置验证失败，可能被其他物品占用",
                    spawnTime = Time.realtimeSinceStartup - startTime
                };
            }
            
            // 注意：不在这里预标记占用，等实际创建成功后再重新分析整个网格
            
            // 创建物品实例
            LogDebug($"准备创建物品实例: {template.templateId} 在位置 ({position.Value.x}, {position.Value.y})");
            GameObject itemInstance = CreateItemInstance(template, position.Value, targetGrid);
            
            if (itemInstance != null)
            {
                LogDebug($"物品实例创建成功: {template.templateId}");
            }
            else
            {
                LogError($"物品实例创建失败: {template.templateId}");
            }
            
            if (itemInstance == null)
            {
                // 创建失败，无需特殊处理，因为没有实际占用网格
                return new SingleItemSpawnResult
                {
                    templateId = template.templateId,
                    itemId = template.itemData?.name ?? "",
                    resultType = SpawnResultType.Failed,
                    failureReason = "物品实例创建失败",
                    spawnTime = Time.realtimeSinceStartup - startTime
                };
            }
            
            // 记录生成状态
            stateTracker.RecordItemSpawned(containerId, template.templateId, 
                                         template.itemData.name, template.quantity, position.Value);
            
            LogDebug($"成功生成物品 {template.templateId} 在位置 {position.Value}");
            
            return new SingleItemSpawnResult
            {
                templateId = template.templateId,
                itemId = template.itemData.name,
                resultType = SpawnResultType.Success,
                position = position.Value,
                itemSize = template.GetItemSize(),
                wasRotated = false, // TODO: 实现旋转检测
                spawnTime = Time.realtimeSinceStartup - startTime
            };
        }
        
        /// <summary>
        /// 创建物品实例
        /// </summary>
        private GameObject CreateItemInstance(FixedItemTemplate template, Vector2Int position, ItemGrid targetGrid)
        {
            try
            {
                LogDebug($"CreateItemInstance: 开始为 {template.templateId} 创建实例");
                
                // 方法1: 尝试从预制件路径加载
                GameObject itemPrefab = TryLoadItemPrefab(template.itemData);
                
                LogDebug($"CreateItemInstance: 预制件加载结果 - {(itemPrefab != null ? "成功" : "失败")}");
                
                // 方法2: 如果预制件加载失败，使用运行时生成
                if (itemPrefab == null)
                {
                    LogWarning($"预制件加载失败，使用运行时生成: {template.itemData.itemName}");
                    itemPrefab = CreateItemPrefabRuntime(template.itemData);
                }
                
                if (itemPrefab == null)
                {
                    LogError($"无法创建物品: {template.itemData.itemName}");
                    return null;
                }
                
                // 实例化物品
                GameObject itemInstance = Instantiate(itemPrefab, targetGrid.transform);
                
                // 设置物品位置
                Vector2 worldPosition = targetGrid.CalculatePositionOnGrid(
                    itemInstance.GetComponent<Item>(), position.x, position.y);
                
                RectTransform itemRect = itemInstance.GetComponent<RectTransform>();
                if (itemRect != null)
                {
                    itemRect.localPosition = worldPosition;
                }
                
                // 将物品放置到网格中
                Item itemComponent = itemInstance.GetComponent<Item>();
                if (itemComponent != null)
                {
                    bool placed = targetGrid.PlaceItem(itemComponent, position.x, position.y);
                    if (!placed)
                    {
                        LogWarning($"物品 {template.templateId} 放置到网格失败 - 位置可能已被占用");
                        Destroy(itemInstance);
                        return null;
                    }
                    
                    // 确保ItemDataReader有正确的数据
                    ItemDataReader itemDataReader = itemInstance.GetComponent<ItemDataReader>();
                    if (itemDataReader != null && itemDataReader.ItemData == null)
                    {
                        itemDataReader.SetItemData(template.itemData);
                    }
                }
                
                // 添加生成标记组件
                AddSpawnTag(itemInstance, template, position, targetGrid.name);
                
                LogDebug($"成功创建物品实例: {template.templateId}");
                return itemInstance;
            }
            catch (Exception e)
            {
                LogError($"创建物品实例时发生错误: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 为生成的物品添加生成标记
        /// </summary>
        private void AddSpawnTag(GameObject itemInstance, FixedItemTemplate template, Vector2Int position, string containerId)
        {
            try
            {
                // 添加生成标记组件
                InventorySpawnTag spawnTag = itemInstance.GetComponent<InventorySpawnTag>();
                if (spawnTag == null)
                {
                    spawnTag = itemInstance.AddComponent<InventorySpawnTag>();
                }
                
                // 初始化标记信息
                string itemId = template.itemData?.id.ToString() ?? template.itemData?.name ?? "";
                string batchId = System.DateTime.Now.Ticks.ToString();
                
                spawnTag.Initialize(template.templateId, itemId, containerId, position, batchId);
                
                LogDebug($"为物品 {template.templateId} 添加生成标记: ItemID={itemId}, Position={position}");
            }
            catch (Exception e)
            {
                LogError($"添加生成标记时发生错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 尝试加载物品预制件
        /// </summary>
        private GameObject TryLoadItemPrefab(ItemDataSO itemData)
        {
            LogDebug($"开始尝试加载物品预制件: {itemData?.itemName ?? "NULL"} (ID: {itemData?.id ?? -1})");
            
            if (itemData == null)
            {
                LogError("ItemDataSO 为空，无法加载预制件");
                return null;
            }
            
            // 方法1: 从Resources文件夹加载（尝试多种路径格式）
            List<string> possiblePaths = GetPossiblePrefabPaths(itemData);
            GameObject prefab = null;
            
            foreach (string resourcesPath in possiblePaths)
            {
                prefab = Resources.Load<GameObject>(resourcesPath);
                
                if (prefab != null)
                {
                    LogDebug($"从Resources加载预制件成功: {resourcesPath}");
                    return prefab;
                }
            }
            
            LogWarning($"从所有Resources路径加载预制件失败: {itemData.itemName} (ID: {itemData.id})");
            
            // 方法2: 从Assets路径加载（使用AssetDatabase，仅编辑器模式）
            #if UNITY_EDITOR
            string assetsPath = GetItemPrefabAssetsPath(itemData);
            LogDebug($"尝试从Assets加载预制件: {assetsPath}");
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetsPath);
            
            if (prefab != null)
            {
                LogDebug($"从Assets加载预制件成功: {assetsPath}");
                return prefab;
            }
            else
            {
                LogWarning($"从Assets加载预制件失败: {assetsPath}");
            }
            #endif
            
            LogError($"无法找到预制件: {itemData.itemName} (ID: {itemData.id})。尝试的路径:");
            foreach (string path in possiblePaths)
            {
                LogError($"  Resources路径: {path}");
            }
            #if UNITY_EDITOR
            LogError($"  Assets路径: {GetItemPrefabAssetsPath(itemData)}");
            #endif
            LogError("请确保预制件在正确位置或使用ItemPrefabGenerator生成预制件。");
            return null;
        }
        
        /// <summary>
        /// 运行时创建物品预制件
        /// </summary>
        private GameObject CreateItemPrefabRuntime(ItemDataSO itemData)
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
                rootRect.pivot = new Vector2(0.5f, 0.5f);
                
                // 创建ItemBackground子对象
                GameObject backgroundObject = new GameObject("ItemBackground");
                backgroundObject.transform.SetParent(rootObject.transform);
                backgroundObject.layer = 5;
                
                RectTransform backgroundRect = backgroundObject.AddComponent<RectTransform>();
                backgroundRect.sizeDelta = itemSize;
                backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
                backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
                backgroundRect.pivot = new Vector2(0.5f, 0.5f);
                backgroundRect.anchoredPosition = Vector2.zero;
                
                Image backgroundImage = backgroundObject.AddComponent<Image>();
                Color backgroundColor = itemData.backgroundColor;
                backgroundColor.a = 0.8f;
                backgroundImage.color = backgroundColor;
                backgroundImage.raycastTarget = true;
                
                // 创建ItemIcon子对象
                GameObject iconObject = new GameObject("ItemIcon");
                iconObject.transform.SetParent(rootObject.transform);
                iconObject.layer = 5;
                
                RectTransform iconRect = iconObject.AddComponent<RectTransform>();
                iconRect.sizeDelta = itemSize;
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.anchoredPosition = Vector2.zero;
                
                Image iconImage = iconObject.AddComponent<Image>();
                iconImage.sprite = itemData.itemIcon;
                iconImage.color = Color.white;
                iconImage.raycastTarget = false;
                
                // 创建ItemHighlight子对象
                GameObject highlightObject = new GameObject("ItemHighlight");
                highlightObject.transform.SetParent(rootObject.transform);
                highlightObject.layer = 5;
                
                RectTransform highlightRect = highlightObject.AddComponent<RectTransform>();
                highlightRect.sizeDelta = itemSize;
                highlightRect.anchorMin = new Vector2(0.5f, 0.5f);
                highlightRect.anchorMax = new Vector2(0.5f, 0.5f);
                highlightRect.pivot = new Vector2(0.5f, 0.5f);
                highlightRect.anchoredPosition = Vector2.zero;
                
                Image highlightImage = highlightObject.AddComponent<Image>();
                highlightImage.color = new Color(1f, 1f, 1f, 0f); // 透明高亮
                highlightImage.raycastTarget = true;
                
                // 创建ItemText子对象
                GameObject textObject = new GameObject("ItemText");
                textObject.transform.SetParent(rootObject.transform);
                textObject.layer = 5;
                
                RectTransform textRect = textObject.AddComponent<RectTransform>();
                // 文本位置在右下角
                float textWidth = 76.8f;
                float textHeight = 57.6f;
                textRect.sizeDelta = new Vector2(textWidth, textHeight);
                textRect.anchorMin = new Vector2(0.5f, 0.5f);
                textRect.anchorMax = new Vector2(0.5f, 0.5f);
                textRect.pivot = new Vector2(1f, 0f); // 右下对齐
                textRect.anchoredPosition = new Vector2(itemWidth * 0.5f - 3f, -itemHeight * 0.5f + 3f);
                
                TMPro.TextMeshProUGUI itemText = textObject.AddComponent<TMPro.TextMeshProUGUI>();
                itemText.text = "1/1"; // 默认数量显示
                itemText.fontSize = 28f;
                itemText.color = Color.white;
                itemText.alignment = TMPro.TextAlignmentOptions.BottomRight;
                itemText.raycastTarget = false;
                
                // 尝试设置TextMeshPro默认字体
                try
                {
                    // 尝试加载TextMeshPro的默认字体资源
                    var defaultFont = Resources.GetBuiltinResource<TMPro.TMP_FontAsset>("Arial SDF");
                    if (defaultFont == null)
                    {
                        // 如果Arial SDF不存在，尝试加载其他可能的默认字体
                        defaultFont = Resources.Load<TMPro.TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                    }
                    if (defaultFont == null)
                    {
                        // 尝试从Resources中查找任意TMP字体
                        var fonts = Resources.LoadAll<TMPro.TMP_FontAsset>("");
                        if (fonts != null && fonts.Length > 0)
                        {
                            defaultFont = fonts[0];
                            LogDebug($"使用找到的字体: {defaultFont.name}");
                        }
                    }
                    
                    if (defaultFont != null)
                    {
                        itemText.font = defaultFont;
                        LogDebug($"成功设置TextMeshPro字体: {defaultFont.name}");
                    }
                    else
                    {
                        LogDebug("未找到可用的TextMeshPro字体，使用默认设置");
                    }
                }
                catch (System.Exception e)
                {
                    LogDebug($"设置TextMeshPro字体时出错: {e.Message}，使用默认设置");
                }
                
                // 添加必要的组件
                ItemDataReader itemDataReader = rootObject.AddComponent<ItemDataReader>();
                Item item = rootObject.AddComponent<Item>();
                DraggableItem draggableItem = rootObject.AddComponent<DraggableItem>();
                
                // 添加ItemHighlight组件
                ItemHighlight itemHighlightComponent = rootObject.AddComponent<ItemHighlight>();
                
                // 设置ItemDataReader的UI引用
                SetItemDataReaderReferences(itemDataReader, backgroundImage, iconImage, itemText);
                
                // 设置ItemHighlight组件的引用
                SetItemHighlightReferences(itemHighlightComponent, highlightImage);
                
                // 设置物品数据
                itemDataReader.SetItemData(itemData);
                
                LogDebug($"运行时创建预制件成功: {itemData.itemName}");
                return rootObject;
            }
            catch (System.Exception e)
            {
                LogError($"运行时创建预制件失败: {itemData.itemName}, 错误: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 设置ItemDataReader的UI组件引用
        /// </summary>
        private void SetItemDataReaderReferences(ItemDataReader itemDataReader, Image backgroundImage, Image iconImage, TMPro.TextMeshProUGUI displayText)
        {
            try
            {
                // 使用反射设置私有字段
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
                if (displayTextField != null && displayText != null)
                    displayTextField.SetValue(itemDataReader, displayText);
            }
            catch (System.Exception e)
            {
                LogWarning($"设置ItemDataReader引用时出错: {e.Message}");
            }
        }
        
        #endregion
        
        #region 预制件路径工具方法
        
        /// <summary>
        /// 获取可能的预制件路径列表
        /// </summary>
        private List<string> GetPossiblePrefabPaths(ItemDataSO itemData)
        {
            List<string> paths = new List<string>();
            string categoryFolder = GetResourcesCategoryFolderName(itemData.category);
            
            // 方案1: 直接使用ScriptableObject名称（优先，应该与ItemDataSO命名一致）
            paths.Add($"InventorySystemResources/Prefabs/{categoryFolder}/{itemData.name}");
            
            // 方案2: 使用ScriptableObject名称（双下划线，兼容旧格式）
            string fileName2 = itemData.name.Replace("_", "__");
            paths.Add($"InventorySystemResources/Prefabs/{categoryFolder}/{fileName2}");
            
            // 方案3: 使用ID + 物品名称（清理特殊字符）
            string cleanName = CleanFileName(itemData.itemName);
            paths.Add($"InventorySystemResources/Prefabs/{categoryFolder}/{itemData.id}__{cleanName}");
            
            // 方案4: 使用ID + 双下划线格式的物品名称
            string doubleUnderscoreName = cleanName.Replace(" ", "__");
            paths.Add($"InventorySystemResources/Prefabs/{categoryFolder}/{itemData.id}__{doubleUnderscoreName}");
            
            // 方案5: 尝试常见的变体
            string baseName = itemData.name;
            if (baseName.StartsWith($"{itemData.id}_"))
            {
                string nameWithoutId = baseName.Substring($"{itemData.id}_".Length);
                paths.Add($"InventorySystemResources/Prefabs/{categoryFolder}/{nameWithoutId}");
                paths.Add($"InventorySystemResources/Prefabs/{categoryFolder}/{nameWithoutId.Replace("_", "__")}");
            }
            
            // 方案6: 基于观察到的模式 - 移除特殊字符并用双下划线替换空格
            string pattern6 = RemoveSpecialCharsAndFormat(itemData.itemName);
            paths.Add($"InventorySystemResources/Prefabs/{categoryFolder}/{itemData.id}__{pattern6}");
            
            // 方案7: 如果ItemDataSO名称已经是正确格式，直接使用
            if (itemData.name.Contains("__"))
            {
                paths.Add($"InventorySystemResources/Prefabs/{categoryFolder}/{itemData.name}");
            }
            
            return paths;
        }
        
        /// <summary>
        /// 清理文件名中的特殊字符
        /// </summary>
        private string CleanFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return "";
            
            // 移除或替换文件名中不允许的字符
            string cleaned = fileName;
            cleaned = cleaned.Replace("(", "");
            cleaned = cleaned.Replace(")", "");
            cleaned = cleaned.Replace("[", "");
            cleaned = cleaned.Replace("]", "");
            cleaned = cleaned.Replace("/", "_");
            cleaned = cleaned.Replace("\\", "_");
            cleaned = cleaned.Replace(":", "_");
            cleaned = cleaned.Replace("*", "_");
            cleaned = cleaned.Replace("?", "_");
            cleaned = cleaned.Replace("\"", "_");
            cleaned = cleaned.Replace("<", "_");
            cleaned = cleaned.Replace(">", "_");
            cleaned = cleaned.Replace("|", "_");
            
            return cleaned.Trim();
        }
        
        /// <summary>
        /// 移除特殊字符并格式化为预制件命名格式
        /// </summary>
        private string RemoveSpecialCharsAndFormat(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            
            string result = input;
            
            // 移除括号和其内容
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\([^)]*\)", "");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\[[^\]]*\]", "");
            
            // 替换空格为双下划线
            result = result.Replace(" ", "__");
            
            // 移除其他特殊字符
            result = result.Replace("-", "__");
            result = result.Replace(".", "__");
            result = result.Replace(",", "__");
            result = result.Replace(":", "__");
            result = result.Replace(";", "__");
            
            // 清理多余的下划线
            while (result.Contains("____"))
            {
                result = result.Replace("____", "__");
            }
            while (result.Contains("___"))
            {
                result = result.Replace("___", "__");
            }
            
            // 移除开头和结尾的下划线
            result = result.Trim('_');
            
            return result;
        }
        
        #endregion
        
        #region 工具方法
        
        /// <summary>
        /// 设置ItemHighlight组件的UI引用
        /// </summary>
        private void SetItemHighlightReferences(ItemHighlight itemHighlight, Image highlightImage)
        {
            try
            {
                // 使用反射设置私有字段
                var highlightImageField = typeof(ItemHighlight).GetField("highlightImage",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (highlightImageField != null)
                {
                    highlightImageField.SetValue(itemHighlight, highlightImage);
                    LogDebug("成功设置ItemHighlight的highlightImage引用");
                }
                else
                {
                    LogWarning("未找到ItemHighlight的highlightImage字段");
                }
                
                // 如果有其他需要设置的字段，可以在这里添加
                // 例如highlightColor, fadeSpeed等
            }
            catch (System.Exception e)
            {
                LogError($"设置ItemHighlight引用时发生错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 获取物品预制件的Resources路径
        /// </summary>
        private string GetItemPrefabResourcesPath(ItemDataSO itemData)
        {
            // 根据实际的Resources文件夹结构构建路径
            string categoryFolder = GetResourcesCategoryFolderName(itemData.category);
            // 使用ScriptableObject的名称，但替换单下划线为双下划线（匹配预制件命名）
            string fileName = itemData.name.Replace("_", "__");
            return $"InventorySystemResources/Prefabs/{categoryFolder}/{fileName}";
        }
        
        /// <summary>
        /// 获取物品预制件的Assets路径
        /// </summary>
        private string GetItemPrefabAssetsPath(ItemDataSO itemData)
        {
            // 根据实际的Assets文件夹结构构建路径
            string categoryFolder = GetResourcesCategoryFolderName(itemData.category);
            // 使用ScriptableObject的名称，但替换单下划线为双下划线（匹配预制件命名）
            string fileName = $"{itemData.name.Replace("_", "__")}.prefab";
            return $"Assets/Resources/InventorySystemResources/Prefabs/{categoryFolder}/{fileName}";
        }
        
        /// <summary>
        /// 获取Resources中的类别文件夹名称（中文命名）
        /// </summary>
        private string GetResourcesCategoryFolderName(ItemCategory category)
        {
            var categoryFolders = new Dictionary<ItemCategory, string>
            {
                { ItemCategory.Helmet, "Helmet_头盔" },
                { ItemCategory.Armor, "Armor_护甲" },
                { ItemCategory.TacticalRig, "TacticalRig_战术背心" },
                { ItemCategory.Backpack, "Backpack_背包" },
                { ItemCategory.Weapon, "Weapon_武器" },
                { ItemCategory.Ammunition, "Ammunition_弹药" },
                { ItemCategory.Food, "Food_食物" },
                { ItemCategory.Drink, "Drink_饮料" },
                { ItemCategory.Sedative, "Sedative_镇静剂" },
                { ItemCategory.Hemostatic, "Hemostatic_止血剂" },
                { ItemCategory.Healing, "Healing_治疗药物" },
                { ItemCategory.Intelligence, "Intelligence_情报" },
                { ItemCategory.Currency, "Currency_货币" },
                { ItemCategory.Special, "Special" }
            };
            
            return categoryFolders.ContainsKey(category) ? categoryFolders[category] : "Special";
        }
        
        /// <summary>
        /// 获取类别文件夹名称（英文编号命名，用于其他系统）
        /// </summary>
        private string GetCategoryFolderName(ItemCategory category)
        {
            var categoryFolders = new Dictionary<ItemCategory, string>
            {
                { ItemCategory.Helmet, "01_Helmets" },
                { ItemCategory.Armor, "02_Armors" },
                { ItemCategory.TacticalRig, "03_TacticalRigs" },
                { ItemCategory.Backpack, "04_Backpacks" },
                { ItemCategory.Weapon, "05_Weapons" },
                { ItemCategory.Ammunition, "06_Ammunition" },
                { ItemCategory.Food, "07_Food" },
                { ItemCategory.Drink, "08_Drinks" },
                { ItemCategory.Sedative, "09_Sedatives" },
                { ItemCategory.Hemostatic, "10_Hemostatic" },
                { ItemCategory.Healing, "11_Healing" },
                { ItemCategory.Intelligence, "12_Intelligence" },
                { ItemCategory.Currency, "13_Currency" },
                { ItemCategory.Special, "14_Special" }
            };
            
            return categoryFolders.ContainsKey(category) ? categoryFolders[category] : "99_Others";
        }
        
        /// <summary>
        /// 清理文件名
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return "Unknown";
            
            return fileName.Replace(" ", "_")
                          .Replace("(", "")
                          .Replace(")", "")
                          .Replace("[", "")
                          .Replace("]", "")
                          .Replace("【", "")
                          .Replace("】", "");
        }
        
        /// <summary>
        /// 获取默认容器ID
        /// </summary>
        private string GetDefaultContainerId(ItemGrid targetGrid)
        {
            if (targetGrid == null) return "unknown";
            
            // 尝试从网格的GUID获取
            if (!string.IsNullOrEmpty(targetGrid.GridGUID))
            {
                return targetGrid.GridGUID;
            }
            
            // 使用网格名称作为ID
            return targetGrid.name;
        }
        
        /// <summary>
        /// 生成总结消息
        /// </summary>
        private string GenerateSummaryMessage(SpawnResult result)
        {
            return $"生成完成: 成功 {result.successfulItems}/{result.totalItems} " +
                   $"(跳过 {result.skippedItems}, 失败 {result.failedItems}), " +
                   $"用时 {result.totalSpawnTime:F2}s";
        }
        
        /// <summary>
        /// 创建失败结果
        /// </summary>
        private SpawnResult CreateFailureResult(string reason)
        {
            return new SpawnResult
            {
                isSuccess = false,
                summaryMessage = $"生成失败: {reason}"
            };
        }
        
        #endregion
        
        #region 调试方法
        
        private void LogDebug(string message)
        {
            if (enableDetailedLogging)
            {
                Debug.Log($"FixedItemSpawnManager: {message}");
            }
        }
        
        private void LogWarning(string message)
        {
            Debug.LogWarning($"FixedItemSpawnManager: {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"FixedItemSpawnManager: {message}");
        }
        
        /// <summary>
        /// 诊断配置问题
        /// </summary>
        [ContextMenu("诊断配置问题")]
        public void DiagnoseConfigurationIssues()
        {
            LogError("=== 开始诊断配置问题 ===");
            
            // 检查是否有处理中的请求
            LogError($"当前队列中的请求数: {spawnQueue.Count}");
            LogError($"是否正在生成: {isSpawning}");
            
            LogError("=== 诊断完成 ===");
        }
        
        /// <summary>
        /// 测试单个物品生成
        /// </summary>
        [ContextMenu("测试单个物品生成")]
        public void TestSingleItemSpawn()
        {
            // 查找第一个ItemGrid进行测试
            var grid = UnityEngine.Object.FindObjectOfType<ItemGrid>();
            if (grid == null)
            {
                LogError("测试失败：未找到ItemGrid");
                return;
            }
            
            // 尝试加载测试物品
            var testItemData = Resources.Load<ItemDataSO>("InventorySystemResources/ItemScriptableObject/Armor_护甲/201_Module_3M_bulletproof_vest");
            if (testItemData == null)
            {
                LogError("测试失败：无法加载测试物品数据");
                return;
            }
            
            LogError($"测试开始：物品 {testItemData.itemName}, 网格尺寸 {grid.CurrentWidth}x{grid.CurrentHeight}");
            
            // 测试预制件加载
            var prefab = TryLoadItemPrefab(testItemData);
            if (prefab != null)
            {
                LogError($"预制件加载成功: {prefab.name}");
            }
            else
            {
                LogError("预制件加载失败");
            }
        }
        
        /// <summary>
        /// 获取管理器统计信息
        /// </summary>
        public string GetManagerStatistics()
        {
            return $"FixedItemSpawnManager 统计:\n" +
                   $"队列中的请求: {spawnQueue?.Count ?? 0}\n" +
                   $"正在生成: {isSpawning}\n" +
                   $"已加载配置: {loadedConfigs?.Count ?? 0}\n" +
                   stateTracker?.GetStatistics();
        }
        
        /// <summary>
        /// 扫描并列出所有可用的预制件（调试用）
        /// </summary>
        [ContextMenu("扫描预制件文件")]
        public void ScanAvailablePrefabs()
        {
            LogDebug("=== 开始扫描预制件文件 ===");
            
            #if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/Resources/InventorySystemResources/Prefabs" });
            
            LogDebug($"找到 {guids.Length} 个预制件文件:");
            
            foreach (string guid in guids)
            {
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                string resourcesPath = assetPath.Replace("Assets/Resources/", "").Replace(".prefab", "");
                LogDebug($"  预制件: {resourcesPath}");
            }
            #else
            LogDebug("预制件扫描功能仅在编辑器模式下可用");
            #endif
            
            LogDebug("=== 预制件扫描完成 ===");
        }
        
        /// <summary>
        /// 验证运行时生成的预制件是否完整（调试用）
        /// </summary>
        [ContextMenu("验证预制件完整性")]
        public void ValidateRuntimePrefabCompleteness()
        {
            // 尝试创建一个测试预制件
            var testItemData = Resources.Load<ItemDataSO>("InventorySystemResources/ItemScriptableObject/Armor_护甲/201_Module_3M_bulletproof_vest");
            if (testItemData != null)
            {
                GameObject testPrefab = CreateItemPrefabRuntime(testItemData);
                if (testPrefab != null)
                {
                    LogDebug("=== 预制件完整性验证 ===");
                    
                    // 检查子对象
                    Transform background = testPrefab.transform.Find("ItemBackground");
                    Transform icon = testPrefab.transform.Find("ItemIcon");
                    Transform highlight = testPrefab.transform.Find("ItemHighlight");
                    Transform text = testPrefab.transform.Find("ItemText");
                    
                    LogDebug($"ItemBackground: {(background != null ? "✓存在" : "✗缺失")}");
                    LogDebug($"ItemIcon: {(icon != null ? "✓存在" : "✗缺失")}");
                    LogDebug($"ItemHighlight: {(highlight != null ? "✓存在" : "✗缺失")}");
                    LogDebug($"ItemText: {(text != null ? "✓存在" : "✗缺失")}");
                    
                    // 检查根对象组件
                    var itemDataReader = testPrefab.GetComponent<ItemDataReader>();
                    var item = testPrefab.GetComponent<Item>();
                    var draggable = testPrefab.GetComponent<DraggableItem>();
                    var itemHighlight = testPrefab.GetComponent<ItemHighlight>();
                    var spawnTag = testPrefab.GetComponent<InventorySpawnTag>();
                    
                    LogDebug($"ItemDataReader: {(itemDataReader != null ? "✓存在" : "✗缺失")}");
                    LogDebug($"Item: {(item != null ? "✓存在" : "✗缺失")}");
                    LogDebug($"DraggableItem: {(draggable != null ? "✓存在" : "✗缺失")}");
                    LogDebug($"ItemHighlight: {(itemHighlight != null ? "✓存在" : "✗缺失")}");
                    LogDebug($"InventorySpawnTag: {(spawnTag != null ? "✓存在" : "✗缺失")}");
                    
                    LogDebug("=== 验证完成 ===");
                    
                    // 清理测试对象
                    DestroyImmediate(testPrefab);
                }
                else
                {
                    LogError("测试预制件创建失败");
                }
            }
            else
            {
                LogError("未找到测试用ItemDataSO");
            }
        }
        
        [ContextMenu("检查管理器状态")]
        public void CheckManagerStatus()
        {
            LogWarning("=== FixedItemSpawnManager 状态检查 ===");
            LogWarning($"实例存在: {(instance != null ? "是" : "否")}");
            LogWarning($"spawnQueue初始化: {(spawnQueue != null ? "是" : "否")}");
            LogWarning($"loadedConfigs初始化: {(loadedConfigs != null ? "是" : "否")}");
            LogWarning($"stateTracker初始化: {(stateTracker != null ? "是" : "否")}");
            LogWarning($"当前正在生成: {isSpawning}");
            LogWarning($"队列中的请求: {spawnQueue?.Count ?? 0}");
            LogWarning($"已加载配置: {loadedConfigs?.Count ?? 0}");
            LogWarning("=====================================");
        }
        
        #endregion
    }
}
