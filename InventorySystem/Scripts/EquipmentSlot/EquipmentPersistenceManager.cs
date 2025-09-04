using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InventorySystem
{
    /// <summary>
    /// 装备持久化管理器
    /// 
    /// 【核心原理】
    /// 这个管理器负责装备系统的数据持久化，它的工作原理是：
    /// 1. 在背包关闭时，收集所有装备槽的当前状态并序列化保存
    /// 2. 在背包打开时，从保存的数据中恢复装备状态
    /// 3. 通过配置文件映射和物品预制件系统重新创建装备实例
    /// 
    /// 【核心作用】
    /// - 确保装备在游戏重启后能正确恢复
    /// - 提供延迟加载机制，避免启动时的性能损耗
    /// - 维护装备数据的完整性和一致性
    /// - 处理装备加载过程中的各种异常情况
    /// 
    /// 【数据流程】
    /// 保存: EquipmentSlot → Manager → 序列化 → PlayerPrefs/ES3
    /// 加载: PlayerPrefs/ES3 → Manager → 物品创建 → EquipmentSlot
    /// 
    /// 【与容器持久化的关系】
    /// 注意：本管理器只负责装备槽中装备物品本身的持久化。
    /// 容器内部的物品持久化由 ContainerSaveManager 单独处理，
    /// 两个系统各司其职，避免数据冲突。
    /// </summary>
    public class EquipmentPersistenceManager : MonoBehaviour
    {
        [Header("持久化设置")]
        [FieldLabel("自动保存")]
        [Tooltip("背包关闭时自动保存装备状态")]
        public bool autoSave = true;
        
        [FieldLabel("自动加载")]
        [Tooltip("背包打开时自动加载装备状态")]
        public bool autoLoad = true;
        
        [FieldLabel("使用ES3存储")]
        [Tooltip("使用ES3文件系统而非PlayerPrefs")]
        public bool useES3Storage = false;
        
        [FieldLabel("存档文件路径")]
        [Tooltip("ES3存档文件的路径")]
        public string saveFilePath = "EquipmentSave.es3";
        
        [Header("调试设置")]
        [FieldLabel("显示调试日志")]
        public bool showDebugLogs = true;
        
        [FieldLabel("详细日志")]
        [Tooltip("显示更详细的调试信息")]
        public bool verboseLogging = false;
        
        // 单例实例
        private static EquipmentPersistenceManager instance;
        public static EquipmentPersistenceManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<EquipmentPersistenceManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("EquipmentPersistenceManager");
                        instance = go.AddComponent<EquipmentPersistenceManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }
        
        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => isInitialized;
        
        /// <summary>
        /// 是否正在加载装备数据
        /// </summary>
        public bool IsLoading => isLoading;
        
        /// <summary>
        /// 是否正在保存装备数据
        /// </summary>
        public bool IsSaving => isSaving;
        
        // 系统组件引用
        private EquipmentSlotManager equipmentSlotManager;
        
        // 常量
        private const string PLAYERPREFS_KEY = "EquipmentPersistenceData";
        private const string DATA_VERSION = "1.0";
        
        // 状态标志
        private bool isInitialized = false;
        private bool isSaving = false;
        private bool isLoading = false;
        
        // 协程结果存储
        private bool lastRestoreResult = false;
        private GameObject lastCreatedItem = null;
        
        #region Unity生命周期
        
        private void Awake()
        {
            // 单例处理
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeManager();
                
                // 注册场景加载事件，确保跨场景重新初始化
                UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
                LogDebug("已注册场景加载事件监听器");
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            // 延迟查找装备槽管理器，确保其他系统已初始化
            StartCoroutine(DelayedInitialization());
        }
        
        private void OnDestroy()
        {
            if (instance == this)
            {
                // 取消注册场景加载事件
                UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
                
                instance = null;
                
                // 确保在场景切换时正确清理
                if (Application.isPlaying)
                {
                    LogDebug("单例实例已清理，场景事件监听器已移除");
                }
            }
        }
        
        private void OnApplicationQuit()
        {
            // 应用程序退出时清理实例
            if (instance == this)
            {
                instance = null;
            }
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            // 应用暂停时保存数据
            if (pauseStatus && autoSave)
            {
                SaveEquipmentData();
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            // 应用失去焦点时保存数据
            if (!hasFocus && autoSave)
            {
                SaveEquipmentData();
            }
        }
        
        #endregion
        
        #region 初始化
        
        /// <summary>
        /// 初始化管理器
        /// </summary>
        private void InitializeManager()
        {
            LogDebug("初始化装备持久化管理器...");
            
            // 设置初始状态
            isSaving = false;
            isLoading = false;
            
            // 查找装备槽管理器
            if (equipmentSlotManager == null)
            {
                equipmentSlotManager = EquipmentSlotManager.Instance;
            }
            
            // 标记为已初始化
            if (equipmentSlotManager != null)
            {
                isInitialized = true;
                LogDebug("装备持久化管理器初始化完成，装备槽管理器已连接");
            }
            else
            {
                LogWarning("装备持久化管理器初始化部分完成，装备槽管理器未找到");
                // 不设置 isInitialized = true，等待延迟初始化
            }
        }
        
        /// <summary>
        /// 延迟初始化（等待其他系统完成初始化）
        /// </summary>
        private IEnumerator DelayedInitialization()
        {
            yield return new WaitForSeconds(0.1f); // 等待其他系统初始化
            
            // 查找装备槽管理器
            if (equipmentSlotManager == null)
            {
                equipmentSlotManager = EquipmentSlotManager.Instance;
            }
            
            if (equipmentSlotManager != null)
            {
                isInitialized = true;
                LogDebug("找到装备槽管理器，持久化系统准备就绪");
                
                // 检查是否需要在启动时自动加载装备
                if (autoLoad && HasSavedData())
                {
                    LogDebug("检测到保存的装备数据，将在游戏启动时自动加载");
                    yield return new WaitForSeconds(1.0f); // 等待装备槽完全初始化
                    
                    if (Application.isPlaying) // 确保仍在运行状态
                    {
                        StartCoroutine(LoadEquipmentDataCoroutine());
                    }
                }
            }
            else
            {
                LogError("未找到装备槽管理器，持久化系统无法正常工作");
            }
        }
        
        /// <summary>
        /// 场景加载时的重新初始化处理
        /// </summary>
        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            LogDebug($"场景加载事件触发: {scene.name}, 模式: {mode}");
            
            // 重置组件引用，因为场景切换可能导致引用失效
            equipmentSlotManager = null;
            
            // 延迟重新初始化，确保新场景中的组件已经创建
            StartCoroutine(DelayedReinitialization());
        }
        
        /// <summary>
        /// 场景切换后的延迟重新初始化
        /// </summary>
        private IEnumerator DelayedReinitialization()
        {
            LogDebug("开始场景切换后的重新初始化...");
            
            // 等待新场景完全加载
            yield return new WaitForSeconds(0.5f);
            
            // 重新查找装备槽管理器
            if (equipmentSlotManager == null)
            {
                equipmentSlotManager = EquipmentSlotManager.Instance;
            }
            
            if (equipmentSlotManager != null)
            {
                isInitialized = true;
                LogDebug("场景切换后重新找到装备槽管理器");
                
                // 检查是否需要自动加载装备（跨会话恢复）
                if (autoLoad && HasSavedData())
                {
                    LogDebug("场景切换后检测到保存的装备数据，开始自动加载");
                    yield return new WaitForSeconds(1.0f); // 等待装备槽完全初始化
                    
                    if (Application.isPlaying)
                    {
                        StartCoroutine(LoadEquipmentDataCoroutine());
                    }
                }
            }
            else
            {
                LogWarning("场景切换后仍未找到装备槽管理器，将在下次场景加载时重试");
            }
        }
        
        #endregion
        
        #region 公共接口
        
        /// <summary>
        /// 保存装备数据
        /// 这是外部调用的主要保存接口
        /// </summary>
        public void SaveEquipmentData()
        {
            if (!isInitialized)
            {
                LogWarning("持久化管理器未初始化，尝试立即初始化");
                InitializeManager();
                
                // 如果仍然未初始化，跳过
                if (!isInitialized)
                {
                    LogError("持久化管理器初始化失败，跳过保存操作");
                    return;
                }
            }
            
            if (isSaving)
            {
                LogWarning("正在保存中，跳过保存操作");
                return;
            }
            
            StartCoroutine(SaveEquipmentDataCoroutine());
        }
        
        /// <summary>
        /// 加载装备数据
        /// 这是外部调用的主要加载接口
        /// </summary>
        public void LoadEquipmentData()
        {
            if (!isInitialized || isLoading)
            {
                LogWarning("持久化管理器未初始化或正在加载中，跳过加载操作");
                return;
            }
            
            StartCoroutine(LoadEquipmentDataCoroutine());
        }
        
        /// <summary>
        /// 检查是否存在保存的装备数据
        /// </summary>
        /// <returns>是否存在保存数据</returns>
        public bool HasSavedData()
        {
            if (useES3Storage)
            {
                return ES3.FileExists(saveFilePath) && ES3.KeyExists("EquipmentData", saveFilePath);
            }
            else
            {
                return PlayerPrefs.HasKey(PLAYERPREFS_KEY);
            }
        }
        
        /// <summary>
        /// 清除保存的装备数据
        /// </summary>
        public void ClearSavedData()
        {
            try
            {
                if (useES3Storage)
                {
                    if (ES3.FileExists(saveFilePath))
                    {
                        ES3.DeleteFile(saveFilePath);
                    }
                }
                else
                {
                    if (PlayerPrefs.HasKey(PLAYERPREFS_KEY))
                    {
                        PlayerPrefs.DeleteKey(PLAYERPREFS_KEY);
                        PlayerPrefs.Save();
                    }
                }
                
                LogDebug("已清除保存的装备数据");
            }
            catch (System.Exception e)
            {
                LogError($"清除保存数据时出错: {e.Message}");
            }
        }
        
        /// <summary>
        /// 获取保存数据的调试信息
        /// </summary>
        /// <returns>调试信息字符串</returns>
        public string GetSavedDataDebugInfo()
        {
            if (!HasSavedData())
            {
                return "没有保存的装备数据";
            }
            
            try
            {
                var loadedData = LoadDataFromStorage();
                return loadedData?.GetDebugInfo() ?? "无法解析保存的数据";
            }
            catch (System.Exception e)
            {
                return $"读取保存数据时出错: {e.Message}";
            }
        }
        
        #endregion
        
        #region 保存逻辑
        
        /// <summary>
        /// 保存装备数据协程
        /// </summary>
        private IEnumerator SaveEquipmentDataCoroutine()
        {
            isSaving = true;
            LogDebug("开始保存装备数据...");
            
            // 收集装备数据
            var persistenceData = CollectEquipmentData();
            
            if (persistenceData == null)
            {
                LogError("收集装备数据失败");
                isSaving = false;
                yield break;
            }
            
            // 验证数据完整性
            var (isValid, errorMessage) = persistenceData.Validate();
            if (!isValid)
            {
                LogError($"装备数据验证失败: {errorMessage}");
                isSaving = false;
                yield break;
            }
            
            // 保存数据到存储
            bool saveSuccess = false;
            try
            {
                saveSuccess = SaveDataToStorage(persistenceData);
            }
            catch (System.Exception e)
            {
                LogError($"保存装备数据时发生异常: {e.Message}\n{e.StackTrace}");
                saveSuccess = false;
            }
            
            if (!saveSuccess)
            {
                isSaving = false;
                yield break;
            }
            
            if (saveSuccess)
            {
                LogDebug($"装备数据保存成功，共保存 {persistenceData.totalSlots} 个槽位，{persistenceData.equippedSlots} 个装备");
                
                if (verboseLogging)
                {
                    LogDebug($"保存详情:\n{persistenceData.GetDebugInfo()}");
                }
            }
            else
            {
                LogError("装备数据保存失败");
            }
            
            isSaving = false;
            yield return null;
        }
        
        /// <summary>
        /// 收集装备数据
        /// </summary>
        /// <returns>装备系统持久化数据</returns>
        private EquipmentSystemPersistenceData CollectEquipmentData()
        {
            if (equipmentSlotManager == null)
            {
                LogError("装备槽管理器不存在，无法收集数据");
                return null;
            }
            
            var persistenceData = new EquipmentSystemPersistenceData
            {
                version = DATA_VERSION
            };
            
            // 获取所有装备槽
            var allSlots = equipmentSlotManager.GetAllEquipmentSlots();
            LogDebug($"收集到 {allSlots.Count} 个装备槽");
            
            foreach (var kvp in allSlots)
            {
                var slot = kvp.Value;
                if (slot == null) continue;
                
                try
                {
                    var slotData = new EquipmentSlotPersistenceData(slot);
                    persistenceData.AddSlotData(slotData);
                    
                    LogDebug($"收集槽位数据: {kvp.Key} - {(slotData.hasEquipment ? $"装备: {slotData.itemName}" : "空")}");
                }
                catch (System.Exception e)
                {
                    LogError($"收集槽位 {kvp.Key} 数据时出错: {e.Message}");
                }
            }
            
            return persistenceData;
        }
        
        /// <summary>
        /// 保存数据到存储
        /// </summary>
        /// <param name="data">要保存的数据</param>
        /// <returns>是否保存成功</returns>
        public bool SaveDataToStorage(EquipmentSystemPersistenceData data)
        {
            try
            {
                if (useES3Storage)
                {
                    // 创建备份
                    string backupPath = saveFilePath.Replace(".es3", "_backup.es3");
                    if (ES3.FileExists(saveFilePath))
                    {
                        ES3.CopyFile(saveFilePath, backupPath);
                    }
                    
                    // 保存新数据
                    ES3.Save("EquipmentData", data, saveFilePath);
                    LogDebug($"装备数据已保存到ES3文件: {saveFilePath}");
                }
                else
                {
                    // 序列化为JSON
                    string jsonData = JsonUtility.ToJson(data, true);
                    
                    // 保存到PlayerPrefs
                    PlayerPrefs.SetString(PLAYERPREFS_KEY, jsonData);
                    PlayerPrefs.Save();
                    
                    LogDebug("装备数据已保存到PlayerPrefs");
                }
                
                return true;
            }
            catch (System.Exception e)
            {
                LogError($"保存数据到存储时出错: {e.Message}");
                return false;
            }
        }
        
        #endregion
        
        #region 加载逻辑
        
        /// <summary>
        /// 加载装备数据协程
        /// </summary>
        private IEnumerator LoadEquipmentDataCoroutine()
        {
            isLoading = true;
            LogDebug("开始加载装备数据...");
            
            // 从存储加载数据
            EquipmentSystemPersistenceData persistenceData = null;
            try
            {
                persistenceData = LoadDataFromStorage();
            }
            catch (System.Exception e)
            {
                LogError($"加载装备数据时发生异常: {e.Message}\n{e.StackTrace}");
                persistenceData = null;
            }
            
            if (persistenceData == null)
            {
                LogWarning("没有找到保存的装备数据");
                isLoading = false;
                yield break;
            }
            
            // 验证数据完整性
            var (isValid, errorMessage) = persistenceData.Validate();
            if (!isValid)
            {
                LogError($"装备数据验证失败: {errorMessage}");
                isLoading = false;
                yield break;
            }
            
            LogDebug($"加载装备数据成功，共 {persistenceData.totalSlots} 个槽位，{persistenceData.equippedSlots} 个装备");
            
            if (verboseLogging)
            {
                LogDebug($"加载详情:\n{persistenceData.GetDebugInfo()}");
            }
            
            // 应用装备数据
            yield return StartCoroutine(ApplyEquipmentData(persistenceData));
            
            isLoading = false;
        }
        
        /// <summary>
        /// 从存储加载数据
        /// </summary>
        /// <returns>装备系统持久化数据</returns>
        private EquipmentSystemPersistenceData LoadDataFromStorage()
        {
            try
            {
                if (useES3Storage)
                {
                    if (ES3.FileExists(saveFilePath) && ES3.KeyExists("EquipmentData", saveFilePath))
                    {
                        var data = ES3.Load<EquipmentSystemPersistenceData>("EquipmentData", saveFilePath);
                        LogDebug($"从ES3文件加载数据: {saveFilePath}");
                        return data;
                    }
                }
                else
                {
                    if (PlayerPrefs.HasKey(PLAYERPREFS_KEY))
                    {
                        string jsonData = PlayerPrefs.GetString(PLAYERPREFS_KEY);
                        var data = JsonUtility.FromJson<EquipmentSystemPersistenceData>(jsonData);
                        LogDebug("从PlayerPrefs加载数据");
                        return data;
                    }
                }
            }
            catch (System.Exception e)
            {
                LogError($"从存储加载数据时出错: {e.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// 应用装备数据
        /// </summary>
        /// <param name="data">装备数据</param>
        private IEnumerator ApplyEquipmentData(EquipmentSystemPersistenceData data)
        {
            if (equipmentSlotManager == null)
            {
                LogError("装备槽管理器不存在，无法应用数据");
                yield break;
            }
            
            LogDebug("开始应用装备数据...");
            
            // 注意：不再盲目清空所有装备，而是在每个槽位级别进行智能检查
            // 这样可以避免重复实例化相同的装备
            
            int successCount = 0;
            int attemptCount = 0;
            
            // 逐个恢复装备
            foreach (var slotData in data.equipmentSlots)
            {
                if (!slotData.hasEquipment) continue;
                
                attemptCount++;
                LogDebug($"尝试恢复装备: {slotData.slotType} -> {slotData.itemName}");
                
                yield return StartCoroutine(RestoreEquipmentToSlot(slotData));
                bool restored = lastRestoreResult;
                if (restored)
                {
                    successCount++;
                    LogDebug($"�7�3 装备恢复成功: {slotData.slotType}");
                    
                    // 如果装备恢复成功，尝试恢复容器内容
                    yield return StartCoroutine(RestoreContainerContentIfNeeded(slotData.slotType));
                }
                else
                {
                    LogError($"�7�4 装备恢复失败: {slotData.slotType}");
                }
                
                yield return null; // 每个装备恢复后等待一帧
            }
            
            LogDebug($"装备数据应用完成，成功恢复 {successCount}/{attemptCount} 个装备");
        }

        /// <summary>
        /// 恢复容器内容（如果需要）
        /// </summary>
        /// <param name="slotType">装备槽类型</param>
        private IEnumerator RestoreContainerContentIfNeeded(EquipmentSlotType slotType)
        {
            // 只有容器类型的装备才需要恢复内容
            if (slotType != EquipmentSlotType.Backpack && slotType != EquipmentSlotType.TacticalRig)
            {
                yield break;
            }

            // 等待一帧确保装备槽完全初始化
            yield return null;

            // 查找对应的装备槽
            var equipmentSlot = equipmentSlotManager.GetEquipmentSlot(slotType);
            if (equipmentSlot == null)
            {
                LogWarning($"未找到装备槽: {slotType}");
                yield break;
            }

            // 获取当前装备的物品
            var equippedItem = equipmentSlot.CurrentEquippedItem;
            if (equippedItem == null)
            {
                LogWarning($"装备槽 {slotType} 中没有装备物品");
                yield break;
            }

            // 获取容器网格
            var containerGrid = equipmentSlot.GetComponentInChildren<ItemGrid>();
            if (containerGrid == null)
            {
                LogWarning($"装备槽 {slotType} 没有找到容器网格");
                yield break;
            }

            // 调用ContainerSaveManager恢复容器内容
            var containerSaveManager = ContainerSaveManager.Instance;
            if (containerSaveManager != null)
            {
                LogDebug($"开始恢复容器内容: {slotType}");
                containerSaveManager.LoadContainerContent(equippedItem, slotType, containerGrid);
                LogDebug($"容器内容恢复完成: {slotType}");
            }
            else
            {
                LogError("ContainerSaveManager实例不存在，无法恢复容器内容");
            }

            yield return null;
        }
        
        /// <summary>
        /// 恢复装备到指定槽位
        /// </summary>
        /// <param name="slotData">槽位数据</param>
        /// <returns>是否恢复成功</returns>
        private IEnumerator RestoreEquipmentToSlot(EquipmentSlotPersistenceData slotData)
        {
            // 获取装备槽
            var slot = equipmentSlotManager.GetEquipmentSlot(slotData.slotType);
            if (slot == null)
            {
                LogError($"未找到类型为 {slotData.slotType} 的装备槽");
                lastRestoreResult = false;
                yield break;
            }
            
            // 检查槽位是否已经有装备
            if (slot.HasEquippedItem)
            {
                LogDebug($"装备槽 {slotData.slotType} 已有装备，检查是否为相同物品");
                
                // 获取当前装备的物品信息
                var currentItem = slot.CurrentEquippedItem;
                if (currentItem != null && currentItem.ItemData != null)
                {
                    // 检查是否为同一物品（通过ID比较）
                    if (currentItem.ItemData.GlobalId.ToString() == slotData.itemID)
                    {
                        LogDebug($"装备槽 {slotData.slotType} 已装备相同物品 {slotData.itemName}，跳过恢复");
                        lastRestoreResult = true;
                        yield break;
                    }
                    else
                    {
                        LogDebug($"装备槽 {slotData.slotType} 装备的是不同物品，将先卸下再装备新物品");
                        slot.UnequipItem();
                        yield return null; // 等待卸下完成
                    }
                }
            }
            
            // 创建物品实例
            yield return StartCoroutine(CreateItemInstance(slotData));
            var itemInstance = lastCreatedItem;
            if (itemInstance == null)
            {
                LogError($"无法创建物品实例: {slotData.itemName}");
                lastRestoreResult = false;
                yield break;
            }
            
            // 装备物品
            var itemDataReader = itemInstance.GetComponent<ItemDataReader>();
            if (itemDataReader == null)
            {
                LogError("物品实例缺少ItemDataReader组件");
                Destroy(itemInstance);
                lastRestoreResult = false;
                yield break;
            }
            
            bool equipSuccess = slot.EquipItem(itemDataReader);
            if (!equipSuccess)
            {
                LogError($"装备到槽位失败: {slotData.slotType}");
                Destroy(itemInstance);
                lastRestoreResult = false;
                yield break;
            }
            
            lastRestoreResult = true;
        }
        
        /// <summary>
        /// 创建物品实例
        /// </summary>
        /// <param name="slotData">槽位数据</param>
        /// <returns>创建的物品GameObject存储在lastCreatedItem中</returns>
        private IEnumerator CreateItemInstance(EquipmentSlotPersistenceData slotData)
        {
            LogDebug($"开始创建物品实例 - {slotData.itemName} (ID: {slotData.itemID})");
            
            lastCreatedItem = null; // 先重置结果
            
            // 获取物品类别
            var category = GetCategoryByID(slotData.itemID);
            LogDebug($"物品类别: {category}");
            
            // 加载物品预制体
            GameObject prefab = null;
            try
            {
                prefab = LoadItemPrefabByCategory(category, slotData.itemID);
            }
            catch (System.Exception e)
            {
                LogError($"加载预制体时发生异常: {e.Message}");
                prefab = null;
            }
            
            if (prefab == null)
            {
                LogError($"无法找到物品预制体: {slotData.itemID}, 类别: {category}");
                yield return null;
                yield break;
            }
            
            LogDebug($"找到预制体: {prefab.name}");
            
            // 实例化物品
            GameObject itemInstance = null;
            try
            {
                itemInstance = UnityEngine.Object.Instantiate(prefab);
            }
            catch (System.Exception e)
            {
                LogError($"实例化物品时发生异常: {e.Message}");
                itemInstance = null;
            }
            
            if (itemInstance == null)
            {
                yield return null;
                yield break;
            }
            
            // 获取ItemDataReader组件
            var itemDataReader = itemInstance.GetComponent<ItemDataReader>();
            if (itemDataReader == null)
            {
                LogError($"物品预制体缺少ItemDataReader组件: {prefab.name}");
                UnityEngine.Object.Destroy(itemInstance);
                yield return null;
                yield break;
            }
            
            // 恢复物品运行时数据
            bool restoreSuccess = true;
            try
            {
                RestoreItemRuntimeData(itemDataReader, slotData.runtimeData);
            }
            catch (System.Exception e)
            {
                LogError($"恢复运行时数据时发生异常: {e.Message}");
                restoreSuccess = false;
            }
            
            if (!restoreSuccess)
            {
                UnityEngine.Object.Destroy(itemInstance);
                yield return null;
                yield break;
            }
            
            LogDebug($"成功创建物品实例: {itemInstance.name}");
            lastCreatedItem = itemInstance;
            
            yield return null;
        }
        
        /// <summary>
        /// 恢复物品运行时数据
        /// </summary>
        /// <param name="itemDataReader">物品数据读取器</param>
        /// <param name="runtimeData">运行时数据</param>
        private void RestoreItemRuntimeData(ItemDataReader itemDataReader, ItemRuntimeData runtimeData)
        {
            if (itemDataReader == null || runtimeData == null) return;
            
            try
            {
                // 恢复堆叠数量
                if (runtimeData.stackCount > 0)
                {
                    itemDataReader.SetStack(runtimeData.stackCount);
                }
                
                // 恢复耐久度
                if (runtimeData.durability > 0)
                {
                    itemDataReader.SetDurability(Mathf.RoundToInt(runtimeData.durability));
                }
                
                // 恢复使用次数
                if (runtimeData.usageCount > 0)
                {
                    itemDataReader.SetUsageCount(runtimeData.usageCount);
                }
                
                LogDebug($"恢复物品运行时数据: 堆叠={runtimeData.stackCount}, 耐久={runtimeData.durability}, 使用次数={runtimeData.usageCount}");
            }
            catch (System.Exception e)
            {
                LogError($"恢复物品运行时数据时发生异常: {e.Message}");
            }
        }
        
        /// <summary>
        /// 根据物品ID获取类别
        /// </summary>
        /// <param name="itemID">物品ID</param>
        /// <returns>物品类别</returns>
        private ItemCategory GetCategoryByID(string itemID)
        {
            if (string.IsNullOrEmpty(itemID)) return ItemCategory.Special;
            
            // 将string转换为int进行判断
            if (!int.TryParse(itemID, out int id))
            {
                LogWarning($"无效的物品ID格式: {itemID}");
                return ItemCategory.Special;
            }
            
            // 根据ID范围判断类别
            if (id >= 101 && id <= 199) return ItemCategory.Helmet;        // 头盔: 1xx
            if (id >= 201 && id <= 299) return ItemCategory.Armor;         // 护甲: 2xx
            if (id >= 301 && id <= 399) return ItemCategory.TacticalRig;   // 战术背心: 3xx
            if (id >= 401 && id <= 499) return ItemCategory.Backpack;      // 背包: 4xx
            if (id >= 501 && id <= 599) return ItemCategory.Weapon;        // 武器: 5xx
            
            LogWarning($"未知的物品ID范围: {itemID}，使用默认类别");
            return ItemCategory.Special;
        }
        
        /// <summary>
        /// 根据类别和ID加载物品预制体
        /// </summary>
        /// <param name="category">物品类别</param>
        /// <param name="itemID">物品ID</param>
        /// <returns>物品预制体</returns>
        private GameObject LoadItemPrefabByCategory(ItemCategory category, string itemID)
        {
            // 获取类别文件夹名称
            string categoryFolder = GetCategoryFolderName(category);
            
            // 尝试多种可能的预制体路径
            string[] possiblePaths = {
                $"InventorySystemResources/Prefabs/{categoryFolder}/{itemID}",
                $"InventorySystemResources/Prefabs/{categoryFolder}/Item_{itemID}",
                $"InventorySystemResources/Prefabs/{categoryFolder}/ItemPrefab_{itemID}"
            };
            
            foreach (string path in possiblePaths)
            {
                var prefab = Resources.Load<GameObject>(path);
                if (prefab != null)
                {
                    LogDebug($"在路径 {path} 找到预制体");
                    return prefab;
                }
            }
            
            // 如果直接路径找不到，尝试前缀匹配
            var prefabByPrefix = SearchPrefabByPrefix(categoryFolder, itemID);
            if (prefabByPrefix != null)
            {
                return prefabByPrefix;
            }
            
            // 最后尝试在所有类别中搜索
            return SearchPrefabInAllCategories(itemID);
        }
        
        /// <summary>
        /// 获取类别文件夹名称
        /// </summary>
        /// <param name="category">物品类别</param>
        /// <returns>文件夹名称</returns>
        private string GetCategoryFolderName(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Helmet: return "Helmet_头盔";
                case ItemCategory.Armor: return "Armor_护甲";
                case ItemCategory.TacticalRig: return "TacticalRig_战术背心";
                case ItemCategory.Backpack: return "Backpack_背包";
                case ItemCategory.Weapon: return "Weapon_武器";
                case ItemCategory.Ammunition: return "Ammunition_弹药";
                case ItemCategory.Food: return "Food_食物";
                case ItemCategory.Drink: return "Drink_饮料";
                case ItemCategory.Sedative: return "Sedative_镇静剂";
                case ItemCategory.Hemostatic: return "Hemostatic_止血剂";
                case ItemCategory.Healing: return "Healing_治疗药物";
                case ItemCategory.Intelligence: return "Intelligence_情报";
                case ItemCategory.Currency: return "Currency_货币";
                case ItemCategory.Special: return "Special_特殊物品";
                default: return "Special_特殊物品";
            }
        }
        
        /// <summary>
        /// 通过前缀在指定文件夹中搜索预制体
        /// </summary>
        /// <param name="categoryFolder">类别文件夹名称</param>
        /// <param name="itemID">物品ID</param>
        /// <returns>找到的预制体</returns>
        private GameObject SearchPrefabByPrefix(string categoryFolder, string itemID)
        {
            try
            {
                string folderPath = $"InventorySystemResources/Prefabs/{categoryFolder}";
                var prefabs = Resources.LoadAll<GameObject>(folderPath);
                
                foreach (var prefab in prefabs)
                {
                    if (prefab.name.StartsWith(itemID + "_") || prefab.name.StartsWith(itemID + "__") || prefab.name.Contains(itemID))
                    {
                        LogDebug($"通过前缀匹配在 {categoryFolder} 中找到预制体: {prefab.name}");
                        return prefab;
                    }
                }
            }
            catch (System.Exception e)
            {
                LogError($"搜索预制体时发生异常: {e.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// 在所有类别文件夹中搜索预制体
        /// </summary>
        /// <param name="itemID">物品ID</param>
        /// <returns>找到的预制体</returns>
        private GameObject SearchPrefabInAllCategories(string itemID)
        {
            string[] categoryFolders = {
                "Helmet_头盔", "Armor_护甲", "TacticalRig_战术背心", "Backpack_背包", "Weapon_武器",
                "Ammunition_弹药", "Food_食物", "Drink_饮料", "Sedative_镇静剂", "Hemostatic_止血剂",
                "Healing_治疗药物", "Intelligence_情报", "Currency_货币", "Special_特殊物品"
            };
            
            foreach (string folder in categoryFolders)
            {
                var prefab = SearchPrefabByPrefix(folder, itemID);
                if (prefab != null)
                {
                    return prefab;
                }
            }
            
            LogWarning($"在所有类别中都未找到物品预制体: {itemID}");
            return null;
        }
        
        #endregion
        
        #region 背包事件集成
        
        /// <summary>
        /// 背包打开事件处理
        /// </summary>
        public void OnBackpackOpened()
        {
            if (!autoLoad) return;
            
            LogDebug("背包打开，准备加载装备数据");
            LoadEquipmentData();
        }
        
        /// <summary>
        /// 背包关闭事件处理
        /// </summary>
        public void OnBackpackClosed()
        {
            if (!autoSave) return;
            
            LogDebug("背包关闭，准备保存装备数据");
            SaveEquipmentData();
        }
        
        /// <summary>
        /// 检查系统是否准备就绪
        /// </summary>
        /// <returns>系统状态信息</returns>
        public (bool isReady, string statusMessage) CheckSystemStatus()
        {
            if (!isInitialized)
                return (false, "持久化管理器未初始化");
                
            if (equipmentSlotManager == null)
                return (false, "装备槽管理器未找到");
                
            if (isSaving)
                return (false, "正在保存中");
                
            if (isLoading)
                return (false, "正在加载中");
                
            return (true, "系统准备就绪");
        }
        
        /// <summary>
        /// 获取系统状态摘要
        /// </summary>
        /// <returns>状态摘要</returns>
        public string GetSystemStatusSummary()
        {
            var summary = new System.Text.StringBuilder();
            summary.AppendLine($"装备持久化管理器状态:");
            summary.AppendLine($"  初始化: {isInitialized}");
            summary.AppendLine($"  正在保存: {isSaving}");
            summary.AppendLine($"  正在加载: {isLoading}");
            summary.AppendLine($"  自动保存: {autoSave}");
            summary.AppendLine($"  自动加载: {autoLoad}");
            summary.AppendLine($"  存储方式: {(useES3Storage ? "ES3" : "PlayerPrefs")}");
            summary.AppendLine($"  存档路径: {(useES3Storage ? saveFilePath : "PlayerPrefs")}");
            summary.AppendLine($"  装备槽管理器: {(equipmentSlotManager != null ? "已连接" : "未找到")}");
            summary.AppendLine($"  存在保存数据: {HasSavedData()}");
            
            return summary.ToString();
        }
        
        #endregion
        
        #region 调试和日志
        
        /// <summary>
        /// 输出调试日志
        /// </summary>
        /// <param name="message">日志信息</param>
        private void LogDebug(string message)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[EquipmentPersistenceManager] {message}");
            }
        }
        
        /// <summary>
        /// 输出警告日志
        /// </summary>
        /// <param name="message">警告信息</param>
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[EquipmentPersistenceManager] {message}");
        }
        
        /// <summary>
        /// 输出错误日志
        /// </summary>
        /// <param name="message">错误信息</param>
        private void LogError(string message)
        {
            Debug.LogError($"[EquipmentPersistenceManager] {message}");
        }
        
        #endregion
    }
}
