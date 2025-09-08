using UnityEngine;
using InventorySystem.SpawnSystem;
using System.Collections;
using System.Linq;

namespace InventorySystem.SpawnSystem
{
    /// <summary>
    /// 货架随机物品生成器设置组件
    /// 用于在场景中快速配置和测试随机物品生成器
    /// </summary>
    [AddComponentMenu("Inventory System/Spawn System/Shelf Random Item Setup")]
    public class ShelfRandomItemSetup : MonoBehaviour
    {
        [Header("生成器配置")]
        [FieldLabel("目标网格")]
        [Tooltip("要生成物品的目标网格")]
        [SerializeField] private ItemGrid targetGrid;
        
        [FieldLabel("随机配置")]
        [Tooltip("随机物品生成配置文件")]
        [SerializeField] private RandomItemSpawnConfig randomConfig;
        
        [FieldLabel("货架对象")]
        [Tooltip("货架GameObject，留空则使用当前对象")]
        [SerializeField] private GameObject shelfObject;
        
        [Header("触发设置")]
        [FieldLabel("自动开始")]
        [Tooltip("游戏开始时是否自动生成")]
        [SerializeField] private bool autoStartOnAwake = false;
        
        [FieldLabel("开始延迟")]
        [Tooltip("自动开始的延迟时间（秒）")]
        [SerializeField] private float startDelay = 1f;
        
        [FieldLabel("生成时机")]
        [Tooltip("何时触发随机物品生成")]
        [SerializeField] private RandomSpawnTiming spawnTiming = RandomSpawnTiming.OnPlayerEnter;
        
        [Header("调试设置")]
        [FieldLabel("启用调试日志")]
        [SerializeField] private bool enableDebugLog = true;
        
        [FieldLabel("显示生成结果")]
        [SerializeField] private bool showSpawnResults = true;
        
        [FieldLabel("显示生成统计")]
        [SerializeField] private bool showGenerationStatistics = true;
        
        // 运行时状态
        private ShelfRandomItemManager randomItemManager;
        private string assignedShelfId;
        private bool hasGeneratedItems = false;
        
        /// <summary>
        /// 随机生成时机
        /// </summary>
        public enum RandomSpawnTiming
        {
            [InspectorName("游戏开始时")] GameStart,
            [InspectorName("玩家进入时")] OnPlayerEnter,
            [InspectorName("手动触发")] Manual
        }
        
        #region 生命周期
        
        private void Awake()
        {
            // 初始化管理器引用
            randomItemManager = ShelfRandomItemManager.Instance;
            
            // 设置默认货架对象
            if (shelfObject == null)
            {
                shelfObject = gameObject;
            }
            
            // 分配货架编号
            AssignShelfNumber();
            
            if (autoStartOnAwake && spawnTiming == RandomSpawnTiming.GameStart)
            {
                StartCoroutine(DelayedStart());
            }
        }
        
        private void Start()
        {
            // 验证配置
            ValidateSetup();
            
            // 注册事件监听
            RegisterEventListeners();
        }
        
        private void OnDestroy()
        {
            // 取消事件监听
            UnregisterEventListeners();
        }
        
        private void OnValidate()
        {
            // 编辑器中验证设置
            ValidateSetup();
        }
        
        #endregion
        
        #region 公共方法
        
        /// <summary>
        /// 手动开始生成
        /// </summary>
        [ContextMenu("开始生成")]
        public void StartGeneration()
        {
            if (!CanStartGeneration(out string reason))
            {
                LogWarning($"无法开始生成: {reason}");
                return;
            }
            
            LogDebug("手动开始随机物品生成");
            ExecuteGeneration();
        }
        
        /// <summary>
        /// 强制重新生成
        /// </summary>
        [ContextMenu("强制重新生成")]
        public void ForceRegeneration()
        {
            if (!ValidateConfiguration(out string reason))
            {
                LogWarning($"无法重新生成: {reason}");
                return;
            }
            
            LogDebug("强制重新生成随机物品");
            
            if (randomItemManager != null)
            {
                bool success = randomItemManager.ForceRegenerateItems(shelfObject, targetGrid, randomConfig);
                if (success)
                {
                    hasGeneratedItems = true;
                    LogDebug("强制重新生成请求已发送");
                }
                else
                {
                    LogWarning("强制重新生成请求失败");
                }
            }
        }
        
        /// <summary>
        /// 获取生成状态
        /// </summary>
        [ContextMenu("显示生成状态")]
        public void ShowGenerationStatus()
        {
            if (randomItemManager == null)
            {
                LogWarning("ShelfRandomItemManager实例不存在");
                return;
            }
            
            var status = randomItemManager.GetShelfStatus(shelfObject);
            
            string statusInfo = $"货架随机生成状态:\n" +
                               $"货架ID: {status.shelfId}\n" +
                               $"已生成: {status.isGenerated}\n" +
                               $"正在生成: {status.isGenerating}\n" +
                               $"有配置覆盖: {status.hasConfigOverride}\n" +
                               $"本地已触发: {hasGeneratedItems}";
            
            if (status.generationInfo.generatedItems != null && status.generationInfo.generatedItems.Count > 0)
            {
                statusInfo += $"\n生成物品数: {status.generationInfo.generatedItems.Count}";
                statusInfo += $"\n配置名称: {status.generationInfo.configName}";
            }
            
            LogDebug(statusInfo);
            
            if (showGenerationStatistics && randomItemManager != null)
            {
                var stats = randomItemManager.GetManagerStatistics();
                LogDebug($"管理器统计:\n会话生成货架数: {stats.sessionStatistics.totalGeneratedShelves}\n总生成物品数: {stats.sessionStatistics.totalItemsGenerated}");
            }
        }
        
        /// <summary>
        /// 重置生成状态
        /// </summary>
        [ContextMenu("重置生成状态")]
        public void ResetGenerationState()
        {
            hasGeneratedItems = false;
            
            if (!string.IsNullOrEmpty(assignedShelfId))
            {
                SessionStateManager.UnmarkShelfGenerated(assignedShelfId);
                LogDebug($"已重置货架 {assignedShelfId} 的生成状态");
            }
        }
        
        /// <summary>
        /// 设置配置覆盖
        /// </summary>
        /// <param name="configOverride">配置覆盖</param>
        public void SetConfigOverride(RandomItemSpawnConfig configOverride)
        {
            if (randomItemManager != null && !string.IsNullOrEmpty(assignedShelfId))
            {
                randomItemManager.SetConfigOverride(assignedShelfId, configOverride);
                LogDebug($"已设置货架 {assignedShelfId} 的配置覆盖");
            }
        }
        
        #endregion
        
        #region 私有方法
        
        /// <summary>
        /// 延迟启动协程
        /// </summary>
        private IEnumerator DelayedStart()
        {
            yield return new WaitForSeconds(startDelay);
            
            if (CanStartGeneration(out string reason))
            {
                LogDebug($"延迟 {startDelay} 秒后开始生成");
                ExecuteGeneration();
            }
            else
            {
                LogWarning($"延迟启动失败: {reason}");
            }
        }
        
        /// <summary>
        /// 分配货架编号
        /// </summary>
        private void AssignShelfNumber()
        {
            if (shelfObject != null)
            {
                assignedShelfId = ShelfNumberingSystem.GetOrAssignShelfNumber(shelfObject);
                LogDebug($"分配货架编号: {assignedShelfId}");
            }
        }
        
        /// <summary>
        /// 验证设置
        /// </summary>
        private void ValidateSetup()
        {
            if (targetGrid == null)
            {
                LogWarning("目标网格未设置");
            }
            
            if (randomConfig == null)
            {
                LogWarning("随机配置未设置");
            }
            else if (!randomConfig.ValidateConfiguration(out var errors))
            {
                LogWarning($"随机配置验证失败:\n{string.Join("\n", errors)}");
            }
            
            if (shelfObject == null)
            {
                LogWarning("货架对象未设置，将使用当前GameObject");
                shelfObject = gameObject;
            }
        }
        
        /// <summary>
        /// 验证配置
        /// </summary>
        private bool ValidateConfiguration(out string reason)
        {
            reason = "";
            
            if (targetGrid == null)
            {
                reason = "目标网格未设置";
                return false;
            }
            
            if (randomConfig == null)
            {
                reason = "随机配置未设置";
                return false;
            }
            
            if (shelfObject == null)
            {
                reason = "货架对象未设置";
                return false;
            }
            
            if (randomItemManager == null)
            {
                reason = "ShelfRandomItemManager实例不存在";
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 检查是否可以开始生成
        /// </summary>
        private bool CanStartGeneration(out string reason)
        {
            if (!ValidateConfiguration(out reason))
            {
                return false;
            }
            
            // 检查会话状态
            if (!string.IsNullOrEmpty(assignedShelfId) && SessionStateManager.IsShelfGenerated(assignedShelfId))
            {
                reason = "货架已在当前会话中生成过物品";
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 执行生成
        /// </summary>
        private void ExecuteGeneration()
        {
            if (randomItemManager == null) return;
            
            bool success = randomItemManager.TryGenerateRandomItems(shelfObject, targetGrid, randomConfig);
            
            if (success)
            {
                hasGeneratedItems = true;
                LogDebug("随机生成请求已发送");
            }
            else
            {
                LogWarning("随机生成请求失败");
            }
        }
        
        /// <summary>
        /// 注册事件监听
        /// </summary>
        private void RegisterEventListeners()
        {
            if (randomItemManager != null)
            {
                ShelfRandomItemManager.OnItemsGenerated += OnItemsGenerated;
                ShelfRandomItemManager.OnGenerationFailed += OnGenerationFailed;
            }
        }
        
        /// <summary>
        /// 取消事件监听
        /// </summary>
        private void UnregisterEventListeners()
        {
            if (randomItemManager != null)
            {
                ShelfRandomItemManager.OnItemsGenerated -= OnItemsGenerated;
                ShelfRandomItemManager.OnGenerationFailed -= OnGenerationFailed;
            }
        }
        
        #endregion
        
        #region 事件处理
        
        /// <summary>
        /// 处理物品生成完成事件
        /// </summary>
        private void OnItemsGenerated(string shelfId, System.Collections.Generic.List<RandomItemSelector.SelectedItemInfo> generatedItems)
        {
            if (shelfId == assignedShelfId)
            {
                LogDebug($"货架 {shelfId} 生成完成，共生成 {generatedItems.Count} 个物品");
                
                if (showSpawnResults)
                {
                    ShowGenerationResults(generatedItems);
                }
            }
        }
        
        /// <summary>
        /// 处理生成失败事件
        /// </summary>
        private void OnGenerationFailed(string shelfId, string errorMessage)
        {
            if (shelfId == assignedShelfId)
            {
                LogError($"货架 {shelfId} 生成失败: {errorMessage}");
            }
        }
        
        /// <summary>
        /// 显示生成结果
        /// </summary>
        private void ShowGenerationResults(System.Collections.Generic.List<RandomItemSelector.SelectedItemInfo> generatedItems)
        {
            if (generatedItems == null || generatedItems.Count == 0) return;
            
            var categoryGroups = generatedItems.GroupBy(item => item.category);
            string results = $"生成结果 (货架 {assignedShelfId}):\n";
            
            foreach (var group in categoryGroups)
            {
                results += $"\n{group.Key}:";
                var rarityGroups = group.GroupBy(item => item.rarity);
                foreach (var rarityGroup in rarityGroups)
                {
                    results += $"\n  {rarityGroup.Key}: {rarityGroup.Count()} 个";
                }
            }
            
            LogDebug(results);
        }
        
        #endregion
        
        #region 触发器集成
        
        /// <summary>
        /// 由触发器调用的生成方法
        /// </summary>
        public void TriggerGenerationFromTrigger()
        {
            if (spawnTiming == RandomSpawnTiming.OnPlayerEnter)
            {
                if (CanStartGeneration(out string reason))
                {
                    LogDebug("触发器触发随机物品生成");
                    ExecuteGeneration();
                }
                else
                {
                    LogDebug($"触发器触发生成被跳过: {reason}");
                }
            }
        }
        
        #endregion
        
        #region 日志方法
        
        private void LogDebug(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[ShelfRandomItemSetup] {message}");
            }
        }
        
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[ShelfRandomItemSetup] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[ShelfRandomItemSetup] {message}");
        }
        
        #endregion
        
        #region 编辑器辅助
        
        #if UNITY_EDITOR
        
        /// <summary>
        /// 编辑器中创建示例配置
        /// </summary>
        [ContextMenu("创建示例配置")]
        private void CreateExampleConfig()
        {
            if (randomConfig != null)
            {
                LogWarning("已存在随机配置，无需创建示例");
                return;
            }
            
            // 这里可以调用RandomSpawnEditorTools来创建示例配置
            LogDebug("请使用 Tools > Random Item Spawn > 创建配置 > 创建示例配置 来创建示例配置");
        }
        
        /// <summary>
        /// 编辑器中测试生成
        /// </summary>
        [ContextMenu("编辑器测试生成")]
        private void EditorTestGeneration()
        {
            if (!Application.isPlaying)
            {
                LogWarning("测试生成只能在运行时执行");
                return;
            }
            
            StartGeneration();
        }
        
        #endif
        
        #endregion
    }
}
