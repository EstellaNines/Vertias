using System;
using System.Collections.Generic;
using UnityEngine;
using InventorySystem;
using InventorySystem.SpawnSystem;

namespace InventorySystem.SpawnSystem
{
    /// <summary>
    /// 生成系统与现有保存系统的集成组件
    /// 负责协调固定物品生成系统与现有的网格保存/加载系统
    /// </summary>
    public class SpawnSystemIntegration : MonoBehaviour
    {
        #region 序列化字段
        
        [Header("集成配置")]
        [FieldLabel("启用自动生成")]
        [Tooltip("是否在网格加载后自动触发固定物品生成")]
        [SerializeField] private bool enableAutoSpawn = true;
        
        [FieldLabel("生成时机")]
        [Tooltip("何时触发自动生成")]
        [SerializeField] private SpawnTiming autoSpawnTiming = SpawnTiming.ContainerFirstOpen;
        
        [FieldLabel("默认仓库配置")]
        [Tooltip("仓库网格的默认生成配置")]
        [SerializeField] private FixedItemSpawnConfig warehouseSpawnConfig;
        
        [FieldLabel("默认地面配置")]
        [Tooltip("地面网格的默认生成配置")]
        [SerializeField] private FixedItemSpawnConfig groundSpawnConfig;
        
        [Header("调试设置")]
        [FieldLabel("启用调试日志")]
        [SerializeField] private bool enableDebugLog = false;
        
        #endregion
        
        #region 私有字段
        
        private Dictionary<string, FixedItemSpawnConfig> containerConfigs;
        private HashSet<string> processedContainers;
        private FixedItemSpawnManager spawnManager;
        
        #endregion
        
        #region 生命周期
        
        private void Awake()
        {
            InitializeIntegration();
            RegisterEventHandlers();
        }
        
        private void OnDestroy()
        {
            UnregisterEventHandlers();
        }
        
        #endregion
        
        #region 初始化
        
        /// <summary>
        /// 初始化集成系统
        /// </summary>
        private void InitializeIntegration()
        {
            containerConfigs = new Dictionary<string, FixedItemSpawnConfig>();
            processedContainers = new HashSet<string>();
            
            // 获取生成管理器
            spawnManager = FixedItemSpawnManager.Instance;
            
            // 注册默认配置
            RegisterDefaultConfigs();
            
            LogDebug("SpawnSystemIntegration 初始化完成");
        }
        
        /// <summary>
        /// 注册默认配置
        /// </summary>
        private void RegisterDefaultConfigs()
        {
            if (warehouseSpawnConfig != null)
            {
                RegisterSpawnConfig("warehouse", warehouseSpawnConfig);
                RegisterSpawnConfig("warehouse_grid_main", warehouseSpawnConfig);
            }
            
            if (groundSpawnConfig != null)
            {
                RegisterSpawnConfig("ground", groundSpawnConfig);
                RegisterSpawnConfig("ground_grid_main", groundSpawnConfig);
            }
        }
        
        /// <summary>
        /// 注册事件处理器
        /// </summary>
        private void RegisterEventHandlers()
        {
            // 监听网格初始化事件
            // 注意：这里假设ItemGrid有相应的事件，需要根据实际情况调整
            
            // 监听保存系统事件
            if (InventorySaveManager.Instance != null)
            {
                // 在网格加载完成后触发生成检查
                // 这里需要根据实际的保存系统API进行调整
            }
        }
        
        /// <summary>
        /// 注销事件处理器
        /// </summary>
        private void UnregisterEventHandlers()
        {
            // 清理事件订阅
        }
        
        #endregion
        
        #region 公共接口
        
        /// <summary>
        /// 注册容器的生成配置
        /// </summary>
        public void RegisterSpawnConfig(string containerId, FixedItemSpawnConfig config)
        {
            if (string.IsNullOrEmpty(containerId) || config == null)
            {
                LogError("容器ID和配置都不能为空");
                return;
            }
            
            containerConfigs[containerId] = config;
            LogDebug($"注册生成配置: {containerId} -> {config.configName}");
        }
        
        /// <summary>
        /// 手动触发容器的固定物品生成
        /// </summary>
        public void TriggerSpawnForContainer(ItemGrid targetGrid, string containerId = null)
        {
            if (targetGrid == null)
            {
                LogError("目标网格不能为空");
                return;
            }
            
            containerId = containerId ?? GetContainerIdFromGrid(targetGrid);
            
            var config = GetSpawnConfigForContainer(containerId);
            if (config == null)
            {
                LogDebug($"容器 {containerId} 没有配置生成规则");
                return;
            }
            
            LogDebug($"手动触发容器 {containerId} 的固定物品生成");
            
            spawnManager.SpawnFixedItems(targetGrid, config, containerId, OnSpawnComplete);
        }
        
        /// <summary>
        /// 检查并自动触发生成（在网格加载后调用）
        /// </summary>
        public void CheckAndAutoSpawn(ItemGrid targetGrid, string containerId = null)
        {
            if (!enableAutoSpawn || targetGrid == null)
            {
                return;
            }
            
            containerId = containerId ?? GetContainerIdFromGrid(targetGrid);
            
            // 检查是否已经处理过此容器
            if (autoSpawnTiming == SpawnTiming.ContainerFirstOpen && 
                processedContainers.Contains(containerId))
            {
                LogDebug($"容器 {containerId} 已处理过，跳过自动生成");
                return;
            }
            
            var config = GetSpawnConfigForContainer(containerId);
            if (config == null)
            {
                LogDebug($"容器 {containerId} 没有配置生成规则");
                return;
            }
            
            // 检查是否需要生成
            if (!spawnManager.ShouldSpawnForContainer(targetGrid, config, containerId))
            {
                LogDebug($"容器 {containerId} 不需要生成物品");
                return;
            }
            
            LogDebug($"自动触发容器 {containerId} 的固定物品生成");
            
            spawnManager.SpawnFixedItems(targetGrid, config, containerId, (result) =>
            {
                OnSpawnComplete(result);
                
                // 标记为已处理
                if (autoSpawnTiming == SpawnTiming.ContainerFirstOpen)
                {
                    processedContainers.Add(containerId);
                }
            });
        }
        
        /// <summary>
        /// 重置容器的处理状态
        /// </summary>
        public void ResetContainerProcessState(string containerId)
        {
            processedContainers.Remove(containerId);
            spawnManager.ResetContainerSpawnState(containerId);
            LogDebug($"重置容器 {containerId} 的处理状态");
        }
        
        /// <summary>
        /// 重置所有容器的处理状态
        /// </summary>
        public void ResetAllProcessStates()
        {
            processedContainers.Clear();
            spawnManager.ResetAllSpawnStates();
            LogDebug("重置所有容器的处理状态");
        }
        
        #endregion
        
        #region 与现有保存系统的集成点
        
        /// <summary>
        /// 在网格加载完成后调用此方法
        /// 这个方法应该从GridSaveManager或相关的加载逻辑中调用
        /// </summary>
        public void OnGridLoaded(ItemGrid loadedGrid, string gridGUID)
        {
            if (loadedGrid == null) return;
            
            LogDebug($"网格加载完成: {gridGUID}");
            
            // 延迟一帧确保网格完全初始化
            StartCoroutine(DelayedSpawnCheck(loadedGrid, gridGUID));
        }
        
        /// <summary>
        /// 延迟检查和生成
        /// </summary>
        private System.Collections.IEnumerator DelayedSpawnCheck(ItemGrid targetGrid, string containerId)
        {
            yield return null; // 等待一帧
            
            CheckAndAutoSpawn(targetGrid, containerId);
        }
        
        /// <summary>
        /// 在网格保存前调用此方法
        /// 确保生成状态也被保存
        /// </summary>
        public void OnGridSaving(ItemGrid savingGrid, string gridGUID)
        {
            LogDebug($"网格保存: {gridGUID}");
            
            // 保存生成状态
            SpawnStateTracker.Instance.SaveStateData();
        }
        
        #endregion
        
        #region 工具方法
        
        /// <summary>
        /// 从网格获取容器ID
        /// </summary>
        private string GetContainerIdFromGrid(ItemGrid grid)
        {
            if (grid == null) return "unknown";
            
            // 优先使用网格的GUID
            if (!string.IsNullOrEmpty(grid.GridGUID))
            {
                return grid.GridGUID;
            }
            
            // 使用网格名称
            return grid.name;
        }
        
        /// <summary>
        /// 获取容器的生成配置
        /// </summary>
        private FixedItemSpawnConfig GetSpawnConfigForContainer(string containerId)
        {
            if (containerConfigs.ContainsKey(containerId))
            {
                return containerConfigs[containerId];
            }
            
            // 尝试匹配部分ID
            foreach (var kvp in containerConfigs)
            {
                if (containerId.Contains(kvp.Key) || kvp.Key.Contains(containerId))
                {
                    return kvp.Value;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 生成完成回调
        /// </summary>
        private void OnSpawnComplete(SpawnResult result)
        {
            if (result == null) return;
            
            LogDebug($"生成完成: {result.summaryMessage}");
            
            if (!result.isSuccess && result.failedItems > 0)
            {
                LogError($"生成过程中有 {result.failedItems} 个物品失败");
            }
        }
        
        #endregion
        
        #region 调试方法
        
        private void LogDebug(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"SpawnSystemIntegration: {message}");
            }
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"SpawnSystemIntegration: {message}");
        }
        
        /// <summary>
        /// 获取集成系统状态
        /// </summary>
        public string GetIntegrationStatus()
        {
            return $"SpawnSystemIntegration 状态:\n" +
                   $"自动生成: {enableAutoSpawn}\n" +
                   $"生成时机: {autoSpawnTiming}\n" +
                   $"已注册配置: {containerConfigs.Count}\n" +
                   $"已处理容器: {processedContainers.Count}\n" +
                   $"仓库配置: {(warehouseSpawnConfig != null ? warehouseSpawnConfig.configName : "未配置")}\n" +
                   $"地面配置: {(groundSpawnConfig != null ? groundSpawnConfig.configName : "未配置")}";
        }
        
        #endregion
        
        #region 编辑器支持
        
        #if UNITY_EDITOR
        /// <summary>
        /// 编辑器中测试生成
        /// </summary>
        [ContextMenu("测试仓库生成")]
        private void TestWarehouseSpawn()
        {
            if (warehouseSpawnConfig == null)
            {
                Debug.LogWarning("未配置仓库生成配置");
                return;
            }
            
            var warehouseGrid = FindObjectOfType<ItemGrid>();
            if (warehouseGrid != null)
            {
                TriggerSpawnForContainer(warehouseGrid, "test_warehouse");
            }
        }
        
        [ContextMenu("重置所有生成状态")]
        private void EditorResetAllStates()
        {
            ResetAllProcessStates();
            Debug.Log("已重置所有生成状态");
        }
        
        [ContextMenu("显示集成状态")]
        private void EditorShowStatus()
        {
            Debug.Log(GetIntegrationStatus());
        }
        #endif
        
        #endregion
    }
}

/// <summary>
/// GridSaveManager的扩展方法
/// 用于集成固定物品生成系统
/// </summary>
public static class GridSaveManagerExtensions
{
    /// <summary>
    /// 在GridSaveManager中调用此方法来触发生成检查
    /// </summary>
    public static void TriggerSpawnSystemCheck(this GridSaveManager gridSaveManager, ItemGrid loadedGrid, string gridGUID)
    {
        var integration = UnityEngine.Object.FindObjectOfType<SpawnSystemIntegration>();
        if (integration != null)
        {
            integration.OnGridLoaded(loadedGrid, gridGUID);
        }
    }
    
    /// <summary>
    /// 在GridSaveManager保存前调用此方法
    /// </summary>
    public static void NotifySpawnSystemSaving(this GridSaveManager gridSaveManager, ItemGrid savingGrid, string gridGUID)
    {
        var integration = UnityEngine.Object.FindObjectOfType<SpawnSystemIntegration>();
        if (integration != null)
        {
            integration.OnGridSaving(savingGrid, gridGUID);
        }
    }
}
