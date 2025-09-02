using System;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem.SpawnSystem
{
    /// <summary>
    /// 仓库固定物品生成管理器
    /// 专门负责管理仓库的固定物品生成，确保每个存档只生成一次
    /// </summary>
    [AddComponentMenu("Inventory System/Spawn System/Warehouse Fixed Item Manager")]
    public class WarehouseFixedItemManager : MonoBehaviour
    {
        [Header("仓库生成配置")]
        [FieldLabel("仓库生成配置")]
        [Tooltip("仓库固定物品的生成配置")]
        [SerializeField] private FixedItemSpawnConfig warehouseConfig;
        
        [FieldLabel("存档标识符")]
        [Tooltip("当前存档的唯一标识符，留空则自动检测")]
        [SerializeField] private string saveGameId = "";
        
        [FieldLabel("强制重新生成")]
        [Tooltip("是否强制重新生成物品（忽略已生成标记）")]
        [SerializeField] private bool forceRegenerate = false;
        
        [Header("调试设置")]
        [FieldLabel("启用调试日志")]
        [SerializeField] private bool enableDebugLog = true;
        
        [FieldLabel("显示详细日志")]
        [SerializeField] private bool enableDetailedLog = false;
        
        // 单例模式
        private static WarehouseFixedItemManager instance;
        public static WarehouseFixedItemManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<WarehouseFixedItemManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("WarehouseFixedItemManager");
                        instance = go.AddComponent<WarehouseFixedItemManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }
        
        // 存档级别的仓库生成状态键
        private const string WAREHOUSE_GENERATED_KEY_PREFIX = "WarehouseGenerated_";
        
        // 生成状态
        private Dictionary<string, bool> warehouseGenerationStatus = new Dictionary<string, bool>();
        
        #region 生命周期
        
        private void Awake()
        {
            // 确保单例
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSaveGameId();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }
        
        private void Start()
        {
            // 加载生成状态
            LoadGenerationStatus();
            
            // 注册存档变化事件（如果有的话）
            RegisterSaveGameEvents();
        }
        
        private void OnDestroy()
        {
            // 取消注册事件
            UnregisterSaveGameEvents();
        }
        
        #endregion
        
        #region 存档ID管理
        
        /// <summary>
        /// 初始化存档ID
        /// </summary>
        private void InitializeSaveGameId()
        {
            if (string.IsNullOrEmpty(saveGameId))
            {
                // 尝试从保存系统获取当前存档ID
                saveGameId = GetSaveGameIdFromSystem();
                
                if (string.IsNullOrEmpty(saveGameId))
                {
                    // 如果还是空，使用默认存档ID
                    saveGameId = "default_save";
                    LogDebug($"使用默认存档ID: {saveGameId}");
                }
                else
                {
                    LogDebug($"检测到存档ID: {saveGameId}");
                }
            }
        }
        
        /// <summary>
        /// 从保存系统获取当前存档ID
        /// </summary>
        private string GetSaveGameIdFromSystem()
        {
            try
            {
                // 使用固定的持久存档ID，确保数据持续性
                // 这样仓库物品状态会在所有游戏会话中保持一致
                string persistentSaveId = GetOrCreatePersistentSaveId();
                LogDebug($"使用持久存档ID: {persistentSaveId}");
                return persistentSaveId;
            }
            catch (Exception e)
            {
                LogWarning($"获取存档ID时出错: {e.Message}");
                // 即使出错也要确保返回固定的存档ID
                return GetOrCreatePersistentSaveId();
            }
        }
        
        /// <summary>
        /// 获取或创建持久存档ID
        /// 这个ID会在所有游戏会话中保持一致，直到手动删除
        /// </summary>
        private string GetOrCreatePersistentSaveId()
        {
            string key = "WarehouseManager_PersistentSaveId";
            
            if (!PlayerPrefs.HasKey(key))
            {
                // 第一次运行，创建持久存档ID
                string persistentId = "warehouse_save_main";
                PlayerPrefs.SetString(key, persistentId);
                PlayerPrefs.Save();
                LogDebug($"创建新的持久存档ID: {persistentId}");
                return persistentId;
            }
            else
            {
                string existingId = PlayerPrefs.GetString(key);
                LogDebug($"使用现有持久存档ID: {existingId}");
                return existingId;
            }
        }
        
        /// <summary>
        /// 重置持久存档ID（相当于删除当前存档）
        /// </summary>
        public void ResetPersistentSaveId()
        {
            string key = "WarehouseManager_PersistentSaveId";
            PlayerPrefs.DeleteKey(key);
            
            // 同时清除仓库生成状态
            string oldSaveId = saveGameId;
            string warehouseKey = WAREHOUSE_GENERATED_KEY_PREFIX + oldSaveId;
            PlayerPrefs.DeleteKey(warehouseKey);
            
            PlayerPrefs.Save();
            
            // 重新初始化存档ID
            InitializeSaveGameId();
            LoadGenerationStatus();
            
            LogDebug($"已重置持久存档ID: {oldSaveId} -> {saveGameId}");
        }
        
        /// <summary>
        /// 手动设置存档ID
        /// </summary>
        public void SetSaveGameId(string newSaveId)
        {
            if (string.IsNullOrEmpty(newSaveId))
            {
                LogWarning("存档ID不能为空");
                return;
            }
            
            string oldId = saveGameId;
            saveGameId = newSaveId;
            
            // 重新加载生成状态
            LoadGenerationStatus();
            
            LogDebug($"存档ID已从 '{oldId}' 更改为 '{saveGameId}'");
        }
        
        #endregion
        
        #region 生成状态管理
        
        /// <summary>
        /// 加载仓库生成状态
        /// </summary>
        private void LoadGenerationStatus()
        {
            string key = WAREHOUSE_GENERATED_KEY_PREFIX + saveGameId;
            bool hasGenerated = PlayerPrefs.GetInt(key, 0) == 1;
            
            warehouseGenerationStatus[saveGameId] = hasGenerated;
            
            LogDebug($"加载仓库生成状态 - 存档: {saveGameId}, 已生成: {hasGenerated}");
        }
        
        /// <summary>
        /// 保存仓库生成状态
        /// </summary>
        private void SaveGenerationStatus()
        {
            string key = WAREHOUSE_GENERATED_KEY_PREFIX + saveGameId;
            bool hasGenerated = warehouseGenerationStatus.GetValueOrDefault(saveGameId, false);
            
            PlayerPrefs.SetInt(key, hasGenerated ? 1 : 0);
            PlayerPrefs.Save();
            
            LogDebug($"保存仓库生成状态 - 存档: {saveGameId}, 已生成: {hasGenerated}");
        }
        
        /// <summary>
        /// 标记仓库已生成物品
        /// </summary>
        private void MarkWarehouseAsGenerated()
        {
            warehouseGenerationStatus[saveGameId] = true;
            SaveGenerationStatus();
            
            LogDebug($"标记仓库已生成 - 存档: {saveGameId}");
        }
        
        /// <summary>
        /// 检查仓库是否已生成过物品
        /// </summary>
        public bool HasWarehouseGenerated()
        {
            return warehouseGenerationStatus.GetValueOrDefault(saveGameId, false);
        }
        
        /// <summary>
        /// 重置仓库生成状态（调试用）
        /// </summary>
        [ContextMenu("重置仓库生成状态")]
        public void ResetWarehouseGenerationStatus()
        {
            warehouseGenerationStatus[saveGameId] = false;
            SaveGenerationStatus();
            
            LogDebug($"重置仓库生成状态 - 存档: {saveGameId}");
        }
        
        #endregion
        
        #region 仓库物品生成
        
        /// <summary>
        /// 尝试生成仓库固定物品
        /// </summary>
        /// <param name="targetGrid">目标网格</param>
        /// <param name="containerId">容器ID</param>
        /// <returns>是否执行了生成</returns>
        public bool TryGenerateWarehouseItems(ItemGrid targetGrid, string containerId)
        {
            // 检查是否应该生成
            if (!ShouldGenerateWarehouseItems())
            {
                LogDebug($"仓库已生成过物品，跳过生成 - 存档: {saveGameId}");
                return false;
            }
            
            // 检查配置
            if (warehouseConfig == null)
            {
                LogWarning("仓库生成配置为空，无法生成物品");
                return false;
            }
            
            // 检查目标网格
            if (targetGrid == null)
            {
                LogWarning("目标网格为空，无法生成物品");
                return false;
            }
            
            LogDebug($"开始生成仓库固定物品 - 存档: {saveGameId}, 容器: {containerId}");
            
            // 使用FixedItemSpawnManager进行实际生成
            var spawnManager = FixedItemSpawnManager.Instance;
            if (spawnManager != null)
            {
                // 异步生成物品
                spawnManager.SpawnFixedItems(targetGrid, warehouseConfig, containerId, OnWarehouseItemsGenerated);
                return true;
            }
            else
            {
                LogError("FixedItemSpawnManager实例不存在");
                return false;
            }
        }
        
        /// <summary>
        /// 检查是否应该生成仓库物品
        /// </summary>
        private bool ShouldGenerateWarehouseItems()
        {
            // 如果强制重新生成，直接返回true
            if (forceRegenerate)
            {
                LogDebug("强制重新生成模式，执行生成");
                return true;
            }
            
            // 检查是否已经生成过
            bool hasGenerated = HasWarehouseGenerated();
            return !hasGenerated;
        }
        
        /// <summary>
        /// 仓库物品生成完成回调
        /// </summary>
        private void OnWarehouseItemsGenerated(SpawnResult result)
        {
            if (result == null)
            {
                LogError("仓库物品生成结果为空");
                return;
            }
            
            LogDebug($"仓库物品生成完成: {result.summaryMessage}");
            
            // 无论生成成功还是失败，都标记为已尝试生成
            // 这样避免重复尝试生成
            if (result.totalItems > 0 || result.isSuccess)
            {
                MarkWarehouseAsGenerated();
                LogDebug("仓库物品生成成功，标记为已生成");
            }
            else
            {
                LogWarning("仓库物品生成失败，但仍标记为已尝试");
                MarkWarehouseAsGenerated();
            }
        }
        
        #endregion
        
        #region 公共API
        
        /// <summary>
        /// 获取当前存档ID
        /// </summary>
        public string GetCurrentSaveGameId()
        {
            return saveGameId;
        }
        
        /// <summary>
        /// 获取仓库生成配置
        /// </summary>
        public FixedItemSpawnConfig GetWarehouseConfig()
        {
            return warehouseConfig;
        }
        
        /// <summary>
        /// 设置仓库生成配置
        /// </summary>
        public void SetWarehouseConfig(FixedItemSpawnConfig config)
        {
            warehouseConfig = config;
            LogDebug($"设置仓库生成配置: {config?.configName ?? "NULL"}");
        }
        
        /// <summary>
        /// 获取生成状态信息
        /// </summary>
        public string GetStatusInfo()
        {
            return $"仓库固定物品管理器状态:\n" +
                   $"存档ID: {saveGameId}\n" +
                   $"已生成: {HasWarehouseGenerated()}\n" +
                   $"强制重新生成: {forceRegenerate}\n" +
                   $"配置: {warehouseConfig?.configName ?? "未设置"}";
        }
        
        #endregion
        
        #region 存档事件管理
        
        /// <summary>
        /// 注册存档变化事件
        /// </summary>
        private void RegisterSaveGameEvents()
        {
            try
            {
                // 这里可以根据你的保存系统添加事件监听
                // 例如：SaveSystem.OnSaveGameChanged += OnSaveGameChanged;
                LogDebug("存档事件注册完成");
            }
            catch (System.Exception e)
            {
                LogWarning($"注册存档事件时出错: {e.Message}");
            }
        }
        
        /// <summary>
        /// 取消注册存档变化事件
        /// </summary>
        private void UnregisterSaveGameEvents()
        {
            try
            {
                // 这里可以根据你的保存系统取消事件监听
                // 例如：SaveSystem.OnSaveGameChanged -= OnSaveGameChanged;
                LogDebug("存档事件取消注册完成");
            }
            catch (System.Exception e)
            {
                LogWarning($"取消注册存档事件时出错: {e.Message}");
            }
        }
        
        /// <summary>
        /// 存档变化时的回调
        /// </summary>
        private void OnSaveGameChanged(string newSaveId)
        {
            LogDebug($"检测到存档变化: {saveGameId} -> {newSaveId}");
            SetSaveGameId(newSaveId);
        }
        
        #endregion
        
        #region 调试方法
        
        /// <summary>
        /// 显示当前存档信息
        /// </summary>
        [ContextMenu("显示存档信息")]
        public void ShowSaveInfo()
        {
            string info = $"=== 仓库固定物品管理器存档信息 ===\n" +
                         $"当前存档ID: {saveGameId}\n" +
                         $"仓库生成状态: {(HasWarehouseGenerated() ? "已生成" : "未生成")}\n" +
                         $"强制重新生成: {forceRegenerate}\n" +
                         $"配置文件: {(warehouseConfig != null ? warehouseConfig.configName : "未设置")}\n";
            
            // 显示持久存档ID
            string persistentSaveId = PlayerPrefs.GetString("WarehouseManager_PersistentSaveId", "未设置");
            info += $"持久存档ID: {persistentSaveId}\n";
            
            // 显示PlayerPrefs中相关的键
            string warehouseKey = WAREHOUSE_GENERATED_KEY_PREFIX + saveGameId;
            bool hasKey = PlayerPrefs.HasKey(warehouseKey);
            info += $"存储键: {warehouseKey}\n";
            info += $"存储键存在: {hasKey}\n";
            if (hasKey)
            {
                info += $"存储值: {PlayerPrefs.GetInt(warehouseKey)}\n";
            }
            
            // 显示存档创建时间（如果有的话）
            string creationTimeKey = $"WarehouseManager_SaveCreationTime_{saveGameId}";
            if (PlayerPrefs.HasKey(creationTimeKey))
            {
                string creationTime = PlayerPrefs.GetString(creationTimeKey);
                info += $"存档创建时间: {creationTime}\n";
            }
            else
            {
                // 如果没有创建时间，现在记录一个
                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                PlayerPrefs.SetString(creationTimeKey, currentTime);
                PlayerPrefs.Save();
                info += $"存档创建时间: {currentTime} (刚刚记录)\n";
            }
            
            Debug.Log($"[WarehouseFixedItemManager] {info}");
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayDialog("存档信息", info, "确定");
            #endif
        }
        
        /// <summary>
        /// 删除当前存档并创建新存档
        /// </summary>
        [ContextMenu("删除当前存档")]
        public void DeleteCurrentSave()
        {
            #if UNITY_EDITOR
            bool confirmed = UnityEditor.EditorUtility.DisplayDialog(
                "删除当前存档", 
                $"这将删除存档 '{saveGameId}' 的所有数据。\n\n下次运行时将创建新的存档。\n\n确定要继续吗？", 
                "删除", "取消");
            
            if (!confirmed) return;
            #endif
            
            string oldSaveId = saveGameId;
            
            // 重置持久存档ID（这会创建新存档）
            ResetPersistentSaveId();
            
            LogDebug($"已删除存档: {oldSaveId}，新存档: {saveGameId}");
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayDialog("存档已删除", 
                $"原存档 '{oldSaveId}' 已删除。\n\n新存档 '{saveGameId}' 已创建。\n\n现在可以重新测试仓库物品生成了。", "确定");
            #endif
        }
        
        /// <summary>
        /// 清除所有仓库相关的PlayerPrefs（完全重置）
        /// </summary>
        [ContextMenu("清除所有仓库数据")]
        public void ClearAllWarehouseData()
        {
            #if UNITY_EDITOR
            bool confirmed = UnityEditor.EditorUtility.DisplayDialog(
                "清除所有仓库数据", 
                "这将清除所有仓库生成记录和存档数据。\n\n包括持久存档ID和所有相关记录。\n\n确定要继续吗？", 
                "确定", "取消");
            
            if (!confirmed) return;
            #endif
            
            var keysToDelete = new List<string>();
            
            // 清除当前存档的仓库生成状态
            string currentKey = WAREHOUSE_GENERATED_KEY_PREFIX + saveGameId;
            if (PlayerPrefs.HasKey(currentKey))
            {
                PlayerPrefs.DeleteKey(currentKey);
                keysToDelete.Add(currentKey);
            }
            
            // 清除持久存档ID
            string persistentIdKey = "WarehouseManager_PersistentSaveId";
            if (PlayerPrefs.HasKey(persistentIdKey))
            {
                PlayerPrefs.DeleteKey(persistentIdKey);
                keysToDelete.Add(persistentIdKey);
            }
            
            // 清除存档创建时间
            string creationTimeKey = $"WarehouseManager_SaveCreationTime_{saveGameId}";
            if (PlayerPrefs.HasKey(creationTimeKey))
            {
                PlayerPrefs.DeleteKey(creationTimeKey);
                keysToDelete.Add(creationTimeKey);
            }
            
            // 清除所有可能的仓库生成记录
            string[] commonKeys = {
                "WarehouseGenerated_warehouse_save_main",
                "WarehouseGenerated_current_game_save",
                "WarehouseGenerated_default_save",
                "WarehouseManager_SaveCreationTime_warehouse_save_main"
            };
            
            foreach (string key in commonKeys)
            {
                if (PlayerPrefs.HasKey(key))
                {
                    PlayerPrefs.DeleteKey(key);
                    keysToDelete.Add(key);
                }
            }
            
            PlayerPrefs.Save();
            
            // 重置内部状态
            warehouseGenerationStatus.Clear();
            
            // 重新初始化（这将创建新的持久存档ID）
            InitializeSaveGameId();
            LoadGenerationStatus();
            
            LogDebug($"已清除所有仓库数据。删除的键: {string.Join(", ", keysToDelete)}");
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayDialog("清除完成", 
                $"已清除所有仓库相关数据。\n\n删除的记录: {keysToDelete.Count}\n\n新存档ID: {saveGameId}\n\n现在可以重新开始测试。", "确定");
            #endif
        }
        
        /// <summary>
        /// 强制生成仓库物品（调试用）
        /// </summary>
        [ContextMenu("强制生成仓库物品")]
        public void ForceGenerateWarehouseItems()
        {
            bool oldForceMode = forceRegenerate;
            forceRegenerate = true;
            
            // 查找仓库网格
            var warehouseGrids = FindObjectsOfType<ItemGrid>();
            ItemGrid warehouseGrid = null;
            
            foreach (var grid in warehouseGrids)
            {
                if (grid.GridType == GridType.Storage || 
                    grid.name.ToLower().Contains("warehouse") ||
                    grid.GridGUID.ToLower().Contains("warehouse"))
                {
                    warehouseGrid = grid;
                    break;
                }
            }
            
            if (warehouseGrid != null)
            {
                string containerId = warehouseGrid.GridGUID ?? "warehouse_debug";
                TryGenerateWarehouseItems(warehouseGrid, containerId);
                LogDebug("执行强制生成仓库物品");
            }
            else
            {
                LogError("未找到仓库网格");
            }
            
            forceRegenerate = oldForceMode;
        }
        
        private void LogDebug(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[WarehouseFixedItemManager] {message}");
            }
        }
        
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[WarehouseFixedItemManager] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[WarehouseFixedItemManager] {message}");
        }
        
        #endregion
    }
}
