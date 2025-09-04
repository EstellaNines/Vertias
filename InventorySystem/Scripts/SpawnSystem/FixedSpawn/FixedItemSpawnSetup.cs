using UnityEngine;
using InventorySystem.SpawnSystem;
using System.Collections.Generic;

namespace InventorySystem.SpawnSystem
{
    /// <summary>
    /// 固定物品生成器设置组件
    /// 用于在场景中快速配置和测试生成器
    /// </summary>
    [AddComponentMenu("Inventory System/Spawn System/Fixed Item Spawn Setup")]
    public class FixedItemSpawnSetup : MonoBehaviour
    {
        [Header("生成器配置")]
        [FieldLabel("目标网格")]
        [Tooltip("要生成物品的目标网格")]
        [SerializeField] private ItemGrid targetGrid;
        
        [FieldLabel("生成配置")]
        [Tooltip("固定物品生成配置文件")]
        [SerializeField] private FixedItemSpawnConfig spawnConfig;
        
        [FieldLabel("容器ID")]
        [Tooltip("容器的唯一标识符，留空则自动获取")]
        [SerializeField] private string containerId;
        
        [Header("触发设置")]
        [FieldLabel("自动开始")]
        [Tooltip("游戏开始时是否自动生成")]
        [SerializeField] private bool autoStartOnAwake = false;
        
        [FieldLabel("开始延迟")]
        [Tooltip("自动开始的延迟时间（秒）")]
        [SerializeField] private float startDelay = 1f;
        
        [Header("调试设置")]
        [FieldLabel("启用调试日志")]
        [SerializeField] private bool enableDebugLog = true;
        
        [FieldLabel("显示生成结果")]
        [SerializeField] private bool showSpawnResults = true;
        
        private FixedItemSpawnManager spawnManager;
        
        #region 生命周期
        
        private void Awake()
        {
            spawnManager = FixedItemSpawnManager.Instance;
            
            if (autoStartOnAwake)
            {
                // 检查生成时机
                CheckAndTriggerSpawn();
            }
        }
        
        /// <summary>
        /// 检查并触发生成
        /// </summary>
        private void CheckAndTriggerSpawn()
        {
            if (spawnConfig == null)
            {
                LogError("生成配置为空，无法触发生成");
                return;
            }
            
            switch (spawnConfig.spawnTiming)
            {
                case SpawnTiming.GameStart:
                    Invoke(nameof(StartSpawning), startDelay);
                    break;
                    
                case SpawnTiming.ContainerFirstOpen:
                    // 检查是否为仓库网格
                    if (IsWarehouseGrid())
                    {
                        // 仓库网格使用独立的仓库固定物品管理器
                        if (ShouldSpawnWarehouseItems())
                        {
                            Invoke(nameof(StartWarehouseSpawning), startDelay);
                        }
                        else
                        {
                            LogDebug("仓库已生成过固定物品，跳过生成");
                        }
                    }
                    else
                    {
                        // 非仓库网格使用原来的逻辑
                        if (ShouldSpawnOnFirstOpen())
                        {
                            Invoke(nameof(StartSpawning), startDelay);
                        }
                        else
                        {
                            LogDebug("容器已首次打开过，跳过生成");
                        }
                    }
                    break;
                    
                case SpawnTiming.ContainerEveryOpen:
                    Invoke(nameof(StartSpawning), startDelay);
                    break;
                    
                case SpawnTiming.Manual:
                    LogDebug("手动触发模式，等待手动调用");
                    break;
                    
                case SpawnTiming.OnCondition:
                    LogDebug("条件触发模式，等待条件满足");
                    break;
            }
        }
        
        /// <summary>
        /// 检查是否应该在首次打开时生成
        /// </summary>
        private bool ShouldSpawnOnFirstOpen()
        {
            string actualContainerId = string.IsNullOrEmpty(containerId) 
                ? (targetGrid?.name ?? "unknown") 
                : containerId;
                
            // 检查容器是否已经有过生成记录
            return !HasContainerBeenOpenedBefore(actualContainerId);
        }
        
        /// <summary>
        /// 检查容器是否之前已经打开过
        /// </summary>
        private bool HasContainerBeenOpenedBefore(string containerId)
        {
            // 使用PlayerPrefs检查容器首次打开状态
            string key = $"Container_FirstOpen_{containerId}";
            return PlayerPrefs.GetInt(key, 0) == 1;
        }
        
        /// <summary>
        /// 标记容器已首次打开
        /// </summary>
        private void MarkContainerAsOpened(string containerId)
        {
            string key = $"Container_FirstOpen_{containerId}";
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
            LogDebug($"标记容器 {containerId} 为已首次打开");
        }
        
        /// <summary>
        /// 重置容器首次打开状态（用于测试）
        /// </summary>
        [ContextMenu("重置容器首次打开状态")]
        public void ResetContainerFirstOpenStatus()
        {
            string actualContainerId = string.IsNullOrEmpty(containerId) 
                ? GetContainerIdFromGrid() 
                : containerId;
                
            string key = $"Container_FirstOpen_{actualContainerId}";
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            LogDebug($"已重置容器 {actualContainerId} 的首次打开状态");
        }
        
        /// <summary>
        /// 重置所有生成状态（完全清空，用于测试）
        /// </summary>
        [ContextMenu("重置所有生成状态")]
        public void ResetAllSpawnStates()
        {
            string actualContainerId = string.IsNullOrEmpty(containerId) 
                ? GetContainerIdFromGrid() 
                : containerId;
                
            // 重置首次打开状态
            string firstOpenKey = $"Container_FirstOpen_{actualContainerId}";
            PlayerPrefs.DeleteKey(firstOpenKey);
            
            // 重置所有物品生成状态
            if (spawnConfig != null && spawnConfig.fixedItems != null)
            {
                foreach (var template in spawnConfig.fixedItems)
                {
                    string stateKey = $"SpawnState_{actualContainerId}_{template.templateId}";
                    PlayerPrefs.DeleteKey(stateKey);
                }
            }
            
            PlayerPrefs.Save();
            LogDebug($"已重置容器 {actualContainerId} 的所有生成状态");
        }
        
        /// <summary>
        /// 显示当前容器中的所有生成标记物品
        /// </summary>
        [ContextMenu("显示容器中的生成物品")]
        public void ShowSpawnedItemsInContainer()
        {
            if (targetGrid == null)
            {
                LogError("目标网格为空");
                return;
            }
            
            var spawnedItems = new List<InventorySpawnTag>();
            
            // 查找所有带有生成标记的物品
            for (int i = 0; i < targetGrid.transform.childCount; i++)
            {
                Transform child = targetGrid.transform.GetChild(i);
                InventorySpawnTag spawnTag = child.GetComponent<InventorySpawnTag>();
                if (spawnTag != null)
                {
                    spawnedItems.Add(spawnTag);
                }
            }
            
            LogDebug($"容器中共有 {spawnedItems.Count} 个生成物品:");
            foreach (var tag in spawnedItems)
            {
                LogDebug($"  - {tag.GetSpawnSummary()}");
            }
        }
        
        private void OnValidate()
        {
            // 自动获取目标网格
            if (targetGrid == null)
            {
                targetGrid = GetComponent<ItemGrid>();
                if (targetGrid == null)
                {
                    targetGrid = GetComponentInChildren<ItemGrid>();
                }
            }
        }
        
        #endregion
        
        #region 公共方法
        
        /// <summary>
        /// 开始生成物品
        /// </summary>
        [ContextMenu("开始生成")]
        public void StartSpawning()
        {
            if (!ValidateSetup())
            {
                return;
            }
            
            string actualContainerId = string.IsNullOrEmpty(containerId) 
                ? GetContainerIdFromGrid() 
                : containerId;
            
            LogDebug($"开始为容器 {actualContainerId} 生成固定物品");
            
            spawnManager.SpawnFixedItems(targetGrid, spawnConfig, actualContainerId, OnSpawnComplete);
        }
        
        /// <summary>
        /// 立即生成物品（同步）
        /// </summary>
        [ContextMenu("立即生成")]
        public void SpawnImmediate()
        {
            if (!ValidateSetup())
            {
                return;
            }
            
            string actualContainerId = string.IsNullOrEmpty(containerId) 
                ? GetContainerIdFromGrid() 
                : containerId;
            
            LogDebug($"立即为容器 {actualContainerId} 生成固定物品");
            
            var result = spawnManager.SpawnFixedItemsImmediate(targetGrid, spawnConfig, actualContainerId);
            OnSpawnComplete(result);
        }
        
        /// <summary>
        /// 检查是否需要生成
        /// </summary>
        [ContextMenu("检查生成需求")]
        public void CheckSpawnNeeds()
        {
            if (!ValidateSetup())
            {
                return;
            }
            
            string actualContainerId = string.IsNullOrEmpty(containerId) 
                ? GetContainerIdFromGrid() 
                : containerId;
            
            bool shouldSpawn = spawnManager.ShouldSpawnForContainer(targetGrid, spawnConfig, actualContainerId);
            
            string message = shouldSpawn 
                ? $"容器 {actualContainerId} 需要生成物品"
                : $"容器 {actualContainerId} 不需要生成物品";
            
            LogDebug(message);
            
            if (enableDebugLog)
            {
                Debug.Log($"FixedItemSpawnSetup: {message}");
            }
        }
        
        /// <summary>
        /// 重置生成状态
        /// </summary>
        [ContextMenu("重置生成状态")]
        public void ResetSpawnState()
        {
            string actualContainerId = string.IsNullOrEmpty(containerId) 
                ? GetContainerIdFromGrid() 
                : containerId;
            
            spawnManager.ResetContainerSpawnState(actualContainerId);
            LogDebug($"已重置容器 {actualContainerId} 的生成状态");
        }
        
        /// <summary>
        /// 分析当前网格
        /// </summary>
        [ContextMenu("分析网格状态")]
        public void AnalyzeGrid()
        {
            if (targetGrid == null)
            {
                LogError("目标网格未设置");
                return;
            }
            
            var analyzer = new GridOccupancyAnalyzer(targetGrid);
            analyzer.AnalyzeGrid();
            
            LogDebug("=== 网格状态分析 ===");
            LogDebug($"网格尺寸: {analyzer.GridWidth}x{analyzer.GridHeight}");
            LogDebug($"总格子数: {analyzer.TotalSlots}");
            LogDebug($"已占用: {analyzer.OccupiedSlots}");
            LogDebug($"可用: {analyzer.AvailableSlots}");
            LogDebug($"占用率: {analyzer.OccupancyRate:P2}");
            
            var largestSpace = analyzer.GetLargestAvailableSpace();
            LogDebug($"最大可用空间: {largestSpace.size} (面积: {largestSpace.area})");
        }
        
        #endregion
        
        #region 私有方法
        
        /// <summary>
        /// 验证设置
        /// </summary>
        private bool ValidateSetup()
        {
            if (targetGrid == null)
            {
                LogError("目标网格未设置");
                return false;
            }
            
            if (spawnConfig == null)
            {
                LogError("生成配置未设置");
                return false;
            }
            
            if (!spawnConfig.ValidateConfig(out var errors))
            {
                LogError($"生成配置验证失败: {string.Join(", ", errors)}");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 从网格获取容器ID
        /// </summary>
        private string GetContainerIdFromGrid()
        {
            if (targetGrid == null) return "unknown";
            
            if (!string.IsNullOrEmpty(targetGrid.GridGUID))
            {
                return targetGrid.GridGUID;
            }
            
            return targetGrid.name;
        }
        
        /// <summary>
        /// 生成完成回调
        /// </summary>
        private void OnSpawnComplete(SpawnResult result)
        {
            if (result == null) return;
            
            LogDebug($"生成完成: {result.summaryMessage}");
            
            // 如果配置为首次打开触发，标记容器为已打开
            if (spawnConfig != null && spawnConfig.spawnTiming == SpawnTiming.ContainerFirstOpen)
            {
                string actualContainerId = string.IsNullOrEmpty(containerId) 
                    ? GetContainerIdFromGrid() 
                    : containerId;
                MarkContainerAsOpened(actualContainerId);
            }
            
            if (showSpawnResults)
            {
                ShowSpawnResultDialog(result);
            }
            
            if (!result.isSuccess && result.failedItems > 0)
            {
                LogError($"生成过程中有 {result.failedItems} 个物品失败");
            }
        }
        
        /// <summary>
        /// 显示生成结果对话框
        /// </summary>
        private void ShowSpawnResultDialog(SpawnResult result)
        {
            #if UNITY_EDITOR
            string message = $"生成结果:\n\n" +
                           $"总物品数: {result.totalItems}\n" +
                           $"成功生成: {result.successfulItems}\n" +
                           $"跳过: {result.skippedItems}\n" +
                           $"失败: {result.failedItems}\n" +
                           $"成功率: {result.GetSuccessRate():P2}\n" +
                           $"用时: {result.totalSpawnTime:F2}秒";
            
            UnityEditor.EditorUtility.DisplayDialog("生成完成", message, "确定");
            #endif
        }
        
        /// <summary>
        /// 调试日志
        /// </summary>
        private void LogDebug(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"FixedItemSpawnSetup: {message}");
            }
        }
        
        /// <summary>
        /// 错误日志
        /// </summary>
        private void LogError(string message)
        {
            Debug.LogError($"FixedItemSpawnSetup: {message}");
        }
        
        #endregion
        
        #region Editor Gizmos
        
        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (targetGrid != null)
            {
                // 绘制网格边界
                Gizmos.color = Color.green;
                Vector3 center = targetGrid.transform.position;
                Vector3 size = new Vector3(
                    targetGrid.CurrentWidth * ItemGrid.tileSizeWidth,
                    targetGrid.CurrentHeight * ItemGrid.tileSizeHeight,
                    0.1f
                );
                Gizmos.DrawWireCube(center, size);
                
                // 绘制标签
                UnityEditor.Handles.Label(
                    center + Vector3.up * (size.y * 0.5f + 20),
                    $"Target Grid: {targetGrid.name}\nSize: {targetGrid.CurrentWidth}x{targetGrid.CurrentHeight}"
                );
            }
        }
        #endif
        
        #endregion
        
        #region 仓库生成管理
        
        /// <summary>
        /// 检查是否为仓库网格
        /// </summary>
        private bool IsWarehouseGrid()
        {
            if (targetGrid == null) return false;
            
            return targetGrid.GridType == GridType.Storage ||
                   targetGrid.name.ToLower().Contains("warehouse") ||
                   (targetGrid.GridGUID != null && targetGrid.GridGUID.ToLower().Contains("warehouse"));
        }
        
        /// <summary>
        /// 检查是否应该生成仓库物品
        /// </summary>
        private bool ShouldSpawnWarehouseItems()
        {
            var warehouseManager = WarehouseFixedItemManager.Instance;
            if (warehouseManager == null)
            {
                LogError("WarehouseFixedItemManager实例不存在");
                return false;
            }
            
            // 设置仓库配置（如果还没设置的话）
            if (warehouseManager.GetWarehouseConfig() == null && spawnConfig != null)
            {
                warehouseManager.SetWarehouseConfig(spawnConfig);
            }
            
            return !warehouseManager.HasWarehouseGenerated();
        }
        
        /// <summary>
        /// 开始仓库物品生成
        /// </summary>
        private void StartWarehouseSpawning()
        {
            if (!ValidateSetup())
            {
                return;
            }
            
            string actualContainerId = string.IsNullOrEmpty(containerId) 
                ? GetContainerIdFromGrid() 
                : containerId;
            
            LogDebug($"开始为仓库容器 {actualContainerId} 生成固定物品");
            
            var warehouseManager = WarehouseFixedItemManager.Instance;
            if (warehouseManager != null)
            {
                // 确保配置已设置
                if (warehouseManager.GetWarehouseConfig() == null && spawnConfig != null)
                {
                    warehouseManager.SetWarehouseConfig(spawnConfig);
                }
                
                bool executed = warehouseManager.TryGenerateWarehouseItems(targetGrid, actualContainerId);
                if (!executed)
                {
                    LogDebug("仓库固定物品管理器跳过生成");
                }
            }
            else
            {
                LogError("WarehouseFixedItemManager实例不存在，回退到普通生成");
                StartSpawning(); // 回退到普通生成
            }
        }
        
        #endregion
    }
}
