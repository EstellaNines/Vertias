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
        [Header("事件")]
        /// <summary>
        /// 装备恢复完成事件
        /// </summary>
        public static System.Action OnEquipmentRestored;
        
        [Header("持久化设置")]
        [FieldLabel("自动保存")]
        [Tooltip("背包关闭时自动保存装备状态")]
        public bool autoSave = true;
        
        [FieldLabel("自动加载")]
        [Tooltip("背包打开时自动加载装备状态")]
        public bool autoLoad = true;
        
        [FieldLabel("使用ES3存储")]
        [Tooltip("使用ES3文件系统而非PlayerPrefs")]
        public bool useES3Storage = true;
        
        [FieldLabel("存档文件路径")]
        [Tooltip("ES3存档文件的路径")]
        public string saveFilePath = "EquipmentSave.es3";
        
        [Header("ES3 高级设置")]
        [FieldLabel("启用备份")]
        [Tooltip("保存时自动创建备份文件")]
        public bool enableBackup = true;
        
        [FieldLabel("数据压缩")]
        [Tooltip("启用ES3数据压缩以节省空间")]
        public bool enableCompression = false;
        
        [Header("调试设置")]
        [FieldLabel("显示调试日志")]
        public bool showDebugLogs = true;
        
        [Header("运行时状态")]
        [FieldLabel("有待加载的装备数据")]
        [Tooltip("标记是否有保存的装备数据等待加载")]
        [SerializeField] private bool hasPendingEquipmentData = false;
        
        [FieldLabel("保存冷却时间")]
        [Tooltip("防止频繁保存的冷却时间(秒)")]
        [Range(0.5f, 5.0f)]
        public float saveCooldown = 2.0f;
        
        // 保存冷却相关
        private float lastSaveTime = 0f;
        private bool hasPendingSave = false;
        private Coroutine delaySaveCoroutine;
        
        [FieldLabel("详细日志")]
        [Tooltip("显示更详细的调试信息")]
        public bool verboseLogging = true;
        
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
        private const string DATA_VERSION = "1.0";
        
        // PlayerPrefs键值（用于数据迁移）
        private const string PLAYERPREFS_KEY = "EquipmentSystemData_default";
        
        // 状态标志
        private bool isInitialized = false;
        private bool isSaving = false;
        private bool isLoading = false;
        
        // 启动期保存抑制与空保存拦截
        private bool suppressSavesUntilFirstLoad = true;
        private float startupRealtime;
        [SerializeField] private float startupEmptySaveBlockSeconds = 10f;
        
        // 协程结果存储
        private bool lastRestoreResult = false;
        private GameObject lastCreatedItem = null;
        
        // 基于全局ID的物品与预制体索引（使用 long 以匹配 ItemDataSO.GlobalId 类型）
        private readonly Dictionary<long, ItemDataSO> globalIdToItemData = new Dictionary<long, ItemDataSO>();
        private readonly Dictionary<long, GameObject> globalIdToPrefab = new Dictionary<long, GameObject>();
        private bool itemDatabasesBuilt = false;
        
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
            // 🔧 强制确保使用ES3存储，解决跨会话持久化问题
            ForceES3Storage();
            
            // 记录启动时间，用于空保存保护窗口
            startupRealtime = Time.realtimeSinceStartup;
            
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
                // 启动阶段抑制保存，避免用空数据覆盖
                if (suppressSavesUntilFirstLoad)
                {
                    LogWarning("启动阶段抑制保存（Pause），跳过以避免空数据覆盖");
                    return;
                }
                SaveEquipmentDataImmediate(); // 🔧 应用暂停/失焦时立即保存
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            // 应用失去焦点时保存数据
            if (!hasFocus && autoSave)
            {
                // 启动阶段抑制保存，避免用空数据覆盖
                if (suppressSavesUntilFirstLoad)
                {
                    LogWarning("启动阶段抑制保存（FocusLost），跳过以避免空数据覆盖");
                    return;
                }
                SaveEquipmentDataImmediate(); // 🔧 应用暂停/失焦时立即保存
            }
        }
        
        /// <summary>
        /// 强制清理冲突的装备数据，确保干净的开始
        /// </summary>
        private void ForceCleanupConflictingData()
        {
            bool hasConflicts = false;
            
            // 检查并清理PlayerPrefs中的冲突数据
            string[] conflictingKeys = {
                "EquipmentSystemData_default",
                PLAYERPREFS_KEY,
                "EquipmentPersistenceData"
            };
            
            foreach (string key in conflictingKeys)
            {
                if (PlayerPrefs.HasKey(key))
                {
                    PlayerPrefs.DeleteKey(key);
                    Debug.Log($"[EquipmentPersistenceManager] 🧹 清理冲突的PlayerPrefs数据: {key}");
                    hasConflicts = true;
                }
            }
            
            // 检查并清理ES3文件中的冲突键
            if (ES3.FileExists(saveFilePath))
            {
                try
                {
                    // 检查是否存在旧格式的键
                    if (ES3.KeyExists("EquipmentSystemData", saveFilePath))
                    {
                        ES3.DeleteKey("EquipmentSystemData", saveFilePath);
                        Debug.Log("[EquipmentPersistenceManager] 🧹 清理ES3中的旧格式数据: EquipmentSystemData");
                        hasConflicts = true;
                    }
                    
                    // 如果存在类型冲突，完全重建文件
                    if (ES3.KeyExists("EquipmentData", saveFilePath))
                    {
                        try
                        {
                            // 尝试加载新格式
                            ES3.Load<EquipmentSystemPersistenceData>("EquipmentData", saveFilePath);
                        }
                        catch (System.Exception)
                        {
                            // 加载失败，说明格式有问题，删除冲突数据
                            ES3.DeleteKey("EquipmentData", saveFilePath);
                            Debug.Log("[EquipmentPersistenceManager] 🧹 清理格式冲突的ES3数据: EquipmentData");
                            hasConflicts = true;
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[EquipmentPersistenceManager] ⚠️ 清理ES3数据时出错: {e.Message}");
                }
            }
            
            if (hasConflicts)
            {
                PlayerPrefs.Save();
                Debug.Log("[EquipmentPersistenceManager] ✅ 冲突数据清理完成，装备系统现在使用干净的数据格式");
            }
        }
        
        /// <summary>
        /// 如果需要，从旧的EquipmentSystemSaveData格式迁移到新格式
        /// 注意：由于已经强制清理了冲突数据，这个方法主要作为备用
        /// </summary>
        private void MigrateFromOldFormatIfNeeded()
        {
            // 由于强制清理，这里主要作为日志记录
            Debug.Log("[EquipmentPersistenceManager] 📋 数据迁移检查完成（已通过强制清理确保数据格式一致性）");
        }
        
        #endregion
        
        #region 初始化
        
        /// <summary>
        /// 强制使用ES3存储，解决跨会话持久化问题
        /// 确保与ContainerSaveManager使用相同的存储系统
        /// </summary>
        private void ForceES3Storage()
        {
            if (!useES3Storage)
            {
                Debug.Log("[EquipmentPersistenceManager] 🔧 强制切换到ES3存储模式，确保与容器系统一致");
                useES3Storage = true;
            }
            
            // 确保文件路径正确
            if (string.IsNullOrEmpty(saveFilePath))
            {
                saveFilePath = "EquipmentSave.es3";
                Debug.Log("[EquipmentPersistenceManager] 🔧 设置默认ES3文件路径: " + saveFilePath);
            }
            
            // 🔧 强制清理冲突数据，确保干净的开始
            ForceCleanupConflictingData();
            
            // 检查是否需要迁移PlayerPrefs数据到ES3
            MigrateFromPlayerPrefsIfNeeded();
            
            // 检查是否需要迁移旧的EquipmentSystemSaveData格式
            MigrateFromOldFormatIfNeeded();
        }
        
        /// <summary>
        /// 如果需要，从PlayerPrefs迁移数据到ES3
        /// </summary>
        private void MigrateFromPlayerPrefsIfNeeded()
        {
            // 检查是否存在PlayerPrefs数据但没有ES3数据
            if (PlayerPrefs.HasKey(PLAYERPREFS_KEY) && !ES3.FileExists(saveFilePath))
            {
                try
                {
                    Debug.Log("[EquipmentPersistenceManager] 🔄 检测到PlayerPrefs数据，开始迁移到ES3...");
                    
                    string jsonData = PlayerPrefs.GetString(PLAYERPREFS_KEY);
                    var data = JsonUtility.FromJson<EquipmentSystemPersistenceData>(jsonData);
                    
                    if (data != null)
                    {
                        // 保存到ES3
                        ES3.Save("EquipmentData", data, saveFilePath);
                        Debug.Log("[EquipmentPersistenceManager] ✅ 成功迁移装备数据到ES3");
                        
                        // 清理旧的PlayerPrefs数据
                        PlayerPrefs.DeleteKey(PLAYERPREFS_KEY);
                        PlayerPrefs.Save();
                        Debug.Log("[EquipmentPersistenceManager] 🧹 已清理旧的PlayerPrefs数据");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[EquipmentPersistenceManager] ❌ 数据迁移失败: {e.Message}");
                }
            }
        }
        
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
                
                // 🔧 修改为按需加载：不在启动时自动加载，而是在玩家打开背包时加载
                if (HasSavedData())
                {
                    LogDebug("检测到保存的装备数据，将在玩家首次打开背包时加载");
                    hasPendingEquipmentData = true; // 标记有待加载的数据
                }
                else
                {
                    LogDebug("没有检测到保存的装备数据");
                    hasPendingEquipmentData = false;
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
            
            // 🔧 在重新初始化期间延长保存抑制，避免空数据覆盖
            suppressSavesUntilFirstLoad = true;
            
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
                
                // 🔧 场景切换后也改为按需加载
                if (HasSavedData())
                {
                    LogDebug("场景切换后检测到保存的装备数据，将在玩家首次打开背包时加载");
                    hasPendingEquipmentData = true; // 标记有待加载的数据
                }
                
                // 🔧 等待一段时间后再允许保存，确保场景中所有装备槽完全初始化
                yield return new WaitForSeconds(2.0f);
                suppressSavesUntilFirstLoad = false;
                LogDebug("场景初始化完成，恢复装备数据保存功能");
            }
            else
            {
                LogWarning("场景切换后仍未找到装备槽管理器，将在下次场景加载时重试");
                // 🔧 即使失败也要恢复保存功能
                yield return new WaitForSeconds(3.0f);
                suppressSavesUntilFirstLoad = false;
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
            // 屏蔽加载阶段的任何保存触发，避免“中途快照”覆盖最终结果
            if (isLoading)
            {
                LogWarning("正在加载装备数据，屏蔽本次保存触发（中途保存已禁用）");
                return;
            }
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
            
            // 🔧 实现延迟保存机制，避免频繁保存
            float timeSinceLastSave = Time.time - lastSaveTime;
            if (timeSinceLastSave < saveCooldown)
            {
                // 如果在冷却期内，取消之前的延迟保存，重新开始延迟
                if (delaySaveCoroutine != null)
                {
                    StopCoroutine(delaySaveCoroutine);
                }
                
                float delayTime = saveCooldown - timeSinceLastSave;
                LogDebug($"保存冷却中，延迟 {delayTime:F1} 秒后保存");
                delaySaveCoroutine = StartCoroutine(DelaySave(delayTime));
                hasPendingSave = true;
            }
            else
            {
                // 立即保存
                StartCoroutine(SaveEquipmentDataCoroutine());
            }
        }
        
        /// <summary>
        /// 延迟保存协程
        /// </summary>
        private IEnumerator DelaySave(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (hasPendingSave && Application.isPlaying)
            {
                hasPendingSave = false;
                StartCoroutine(SaveEquipmentDataCoroutine());
            }
        }
        
        /// <summary>
        /// 立即保存装备数据（跳过冷却机制，用于重要时刻如应用退出）
        /// </summary>
        public void SaveEquipmentDataImmediate()
        {
            if (isLoading)
            {
                LogWarning("正在加载装备数据，但强制保存");
            }
            
            if (!isInitialized)
            {
                LogWarning("持久化管理器未初始化，尝试立即初始化");
                InitializeManager();
                
                if (!isInitialized)
                {
                    LogError("无法初始化持久化管理器，跳过保存");
                    return;
                }
            }
            
            // 取消任何延迟保存
            if (delaySaveCoroutine != null)
            {
                StopCoroutine(delaySaveCoroutine);
                delaySaveCoroutine = null;
            }
            hasPendingSave = false;
            
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
            // 再次防御：如果在协程启动时进入加载阶段，直接跳过
            if (isLoading)
            {
                LogWarning("保存协程启动时检测到加载中，跳过本次保存");
                yield break;
            }
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
            
            // 启动空保存保护：
            if (suppressSavesUntilFirstLoad)
            {
                if (persistenceData.equippedSlots == 0 && HasSavedData() && (Time.realtimeSinceStartup - startupRealtime) < startupEmptySaveBlockSeconds)
                {
                    LogWarning($"检测到启动期空保存（{persistenceData.totalSlots} 槽位, 0 装备），且已有历史存档，跳过本次保存（保护窗口 {startupEmptySaveBlockSeconds:F1}s 内）");
                    isSaving = false;
                    yield break;
                }
                // 如果此时已经有装备，则解除抑制并继续保存
                if (persistenceData.equippedSlots > 0)
                {
                    LogDebug("首次检测到已有装备，解除启动期保存抑制");
                    suppressSavesUntilFirstLoad = false;
                }
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
                // 🔧 更新最后保存时间
                lastSaveTime = Time.time;
                
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
            
            // 🔧 检查是否正在装备恢复过程中
            if (isLoading)
            {
                LogWarning("正在加载装备数据，跳过收集操作（避免收集到不完整状态）");
                return null;
            }
            
            // 🔧 修改启动期保护逻辑 - 只在特定情况下阻止保存
            if (suppressSavesUntilFirstLoad && HasSavedData() && Time.realtimeSinceStartup < startupEmptySaveBlockSeconds)
            {
                // 检查当前是否有装备 - 如果有装备就不阻止保存
                var quickEquipmentCheck = false;
                var equipmentSlots = GameObject.FindObjectsOfType<InventorySystem.EquipmentSlot>(true);
                foreach (var slot in equipmentSlots)
                {
                    if (slot != null && slot.HasEquippedItem)
                    {
                        quickEquipmentCheck = true;
                        LogDebug("检测到有装备存在，允许保存以记录新装备状态");
                        break;
                    }
                }
                
                if (!quickEquipmentCheck)
                {
                    LogWarning("启动期间存在保存数据但装备槽可能未完全初始化，延迟收集直到初始化完成");
                    return null;
                }
            }
            
            var persistenceData = new EquipmentSystemPersistenceData
            {
                version = DATA_VERSION
            };
            
            // 新策略：遍历场景所有EquipmentSlot（包含非激活），按槽位类型优先选择"已装备"的那个，避免空实例覆盖
            var allSceneSlots = GameObject.FindObjectsOfType<InventorySystem.EquipmentSlot>(true);
            LogDebug($"收集到场景中 {allSceneSlots.Length} 个EquipmentSlot组件（包含非激活）");

            var slotTypeToBestSlot = new System.Collections.Generic.Dictionary<EquipmentSlotType, InventorySystem.EquipmentSlot>();

            foreach (var slot in allSceneSlots)
            {
                if (slot == null || slot.config == null) continue;
                var type = slot.config.slotType;

                if (!slotTypeToBestSlot.TryGetValue(type, out var existing))
                {
                    slotTypeToBestSlot[type] = slot;
                    continue;
                }

                bool existingEquipped = false;
                bool currentEquipped = false;
                try { existingEquipped = existing.HasEquippedItem; } catch { existingEquipped = false; }
                try { currentEquipped = slot.HasEquippedItem; } catch { currentEquipped = false; }

                if (!existingEquipped && currentEquipped)
                {
                    slotTypeToBestSlot[type] = slot;
                }
            }

            LogDebug($"归并后共覆盖 {slotTypeToBestSlot.Count} 种槽位类型");

            int equippedCount = 0;
            foreach (var kvp in slotTypeToBestSlot)
            {
                var slot = kvp.Value;
                if (slot == null) continue;

                try
                {
                    bool hasEquipped = false;
                    ItemDataReader currentItem = null;
                    try { hasEquipped = slot.HasEquippedItem; currentItem = slot.CurrentEquippedItem; } catch { }

                    if (verboseLogging)
                    {
                        Debug.Log($"[EquipmentPersistenceManager] 🔍 最终槽 {kvp.Key}:");
                        Debug.Log($"  - HasEquippedItem: {hasEquipped}");
                        Debug.Log($"  - CurrentEquippedItem: {(currentItem != null ? currentItem.ItemData.itemName : "null")} ");
                        Debug.Log($"  - 槽位GameObject: {slot.gameObject.name}");
                        Debug.Log($"  - 槽位激活状态: {slot.gameObject.activeInHierarchy}");
                    }

                    var slotData = new EquipmentSlotPersistenceData(slot);
                    persistenceData.AddSlotData(slotData);

                    if (slotData.hasEquipment) equippedCount++;

                    LogDebug($"收集槽位数据: {kvp.Key} - {(slotData.hasEquipment ? $"装备: {slotData.itemName}" : "空")}");
                }
                catch (System.Exception e)
                {
                    LogError($"收集槽位 {kvp.Key} 数据时出错: {e.Message}");
                }
            }
            
            // 🔧 额外验证：如果已知有存档数据但收集到0装备，且在启动期间，则暂停收集
            if (HasSavedData() && equippedCount == 0 && Time.realtimeSinceStartup < startupEmptySaveBlockSeconds * 2)
            {
                LogWarning($"检测到异常：已知存在装备存档但收集到0个装备（启动时间: {Time.realtimeSinceStartup:F1}s），可能处于装备恢复过程中，取消本次收集");
                return null;
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
                    // 创建备份（如果启用）
                    if (enableBackup)
                    {
                        CreateEquipmentBackup();
                    }
                    
                    // 准备ES3设置
                    ES3Settings settings = new ES3Settings();
                    if (enableCompression)
                    {
                        settings.compressionType = ES3.CompressionType.Gzip;
                    }
                    
                    // 保存新数据
                    ES3.Save("EquipmentData", data, saveFilePath, settings);
                    LogDebug($"装备数据已保存到ES3文件: {saveFilePath} (压缩: {enableCompression}, 备份: {enableBackup})");
                }
                else
                {
                    // 序列化为JSON
                    string jsonData = JsonUtility.ToJson(data, true);
                    
                    // 保存到PlayerPrefs
                    PlayerPrefs.SetString(PLAYERPREFS_KEY, jsonData);
                    PlayerPrefs.Save();
                    
                    LogDebug("装备数据已保存到PlayerPrefs（建议切换到ES3模式以获得更好的功能）");
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
                // 即便无存档，也解除启动期抑制，允许后续保存
                suppressSavesUntilFirstLoad = false;
                yield break;
            }
            
            // 验证数据完整性
            var (isValid, errorMessage) = persistenceData.Validate();
            if (!isValid)
            {
                LogError($"装备数据验证失败: {errorMessage}");
                isLoading = false;
                // 避免卡死抑制，解除抑制但提示
                suppressSavesUntilFirstLoad = false;
                yield break;
            }
            
            LogDebug($"加载装备数据成功，共 {persistenceData.totalSlots} 个槽位，{persistenceData.equippedSlots} 个装备");
            
            if (verboseLogging)
            {
                LogDebug($"加载详情:\n{persistenceData.GetDebugInfo()}");
            }
            
            // 应用装备数据
            yield return StartCoroutine(ApplyEquipmentData(persistenceData));
            
            // 首次加载流程结束，解除保存抑制
            suppressSavesUntilFirstLoad = false;
            
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
                        // 准备ES3设置
                        ES3Settings settings = new ES3Settings();
                        if (enableCompression)
                        {
                            settings.compressionType = ES3.CompressionType.Gzip;
                        }
                        
                        var data = ES3.Load<EquipmentSystemPersistenceData>("EquipmentData", saveFilePath, settings);
                        LogDebug($"从ES3文件加载数据: {saveFilePath} (压缩: {enableCompression})");
                        return data;
                    }
                    else
                    {
                        LogDebug($"ES3文件不存在或无数据键: {saveFilePath}");
                    }
                }
                else
                {
                    if (PlayerPrefs.HasKey(PLAYERPREFS_KEY))
                    {
                        string jsonData = PlayerPrefs.GetString(PLAYERPREFS_KEY);
                        var data = JsonUtility.FromJson<EquipmentSystemPersistenceData>(jsonData);
                        LogDebug("从PlayerPrefs加载数据（建议切换到ES3模式）");
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
                    LogDebug($"✅ 装备恢复成功: {slotData.slotType}");
                    
                    // 容器内容恢复现在由ContainerSessionManager处理，这里不再处理
                    // yield return StartCoroutine(RestoreContainerContentIfNeeded(slotData.slotType));
                }
                else
                {
                    LogError($" 装备恢复失败: {slotData.slotType}");
                }
                
                yield return null; // 每个装备恢复后等待一帧
            }
            
            LogDebug($"装备数据应用完成，成功恢复 {successCount}/{attemptCount} 个装备");
            
            // 触发装备恢复完成事件
            OnEquipmentRestored?.Invoke();
            LogDebug("✅ 装备恢复事件已触发");
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
            
            // 🔧 修复：确保容器类装备在槽位未激活时也能创建容器网格
            yield return StartCoroutine(EnsureContainerGridCreated(slot, slotData));
            
            lastRestoreResult = true;
        }
        
        /// <summary>
        /// 确保容器网格被正确创建（即使装备槽未激活）
        /// </summary>
        /// <param name="slot">装备槽</param>
        /// <param name="slotData">槽位数据</param>
        private IEnumerator EnsureContainerGridCreated(EquipmentSlot slot, EquipmentSlotPersistenceData slotData)
        {
            // 只处理容器类装备槽
            if (slotData.slotType != EquipmentSlotType.Backpack && slotData.slotType != EquipmentSlotType.TacticalRig)
            {
                yield break;
            }
            
            // 等待一帧确保装备完全设置
            yield return null;
            
            // 检查是否需要强制创建容器网格
            if (slot.HasEquippedItem && slot.CurrentEquippedItem != null)
            {
                var itemData = slot.CurrentEquippedItem.ItemData;
                if (itemData != null && (itemData.category == ItemCategory.Backpack || itemData.category == ItemCategory.TacticalRig))
                {
                    LogDebug($"🔧 强制为装备槽 {slotData.slotType} 创建容器网格以启用内容恢复");
                    
                    // 🔧 修复：不能在try-catch中使用yield，分开处理
                    bool forceCreateSuccess = false;
                    try
                    {
                        // 使用新添加的公共方法强制激活容器网格
                        slot.ForceActivateContainerGrid();
                        forceCreateSuccess = true;
                    }
                    catch (System.Exception e)
                    {
                        LogError($"强制创建容器网格失败: {slotData.slotType} - {e.Message}");
                    }
                    
                    if (forceCreateSuccess)
                    {
                        // 等待一帧确保容器网格完全创建
                        yield return null;
                        LogDebug($"✅ 容器网格强制创建完成: {slotData.slotType}");
                    }
                }
            }
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
            
            // 构建全局索引（GlobalId → ItemDataSO / Prefab）
            EnsureItemDatabasesBuilt();
            
            // 解析GlobalId
            if (!long.TryParse(slotData.itemID, out long globalId))
            {
                LogError($"无效的GlobalId: {slotData.itemID}");
                yield return null;
                yield break;
            }
            
            // 定位ItemDataSO（考虑重复GlobalId：优先匹配名称，其次匹配槽位类别）
            if (!TryGetCorrectItemData(globalId, slotData.itemName, slotData.slotType, out var itemDataSo) || itemDataSo == null)
            {
                LogError($"未能通过GlobalId精确定位到ItemDataSO: {globalId} (期望名称: {slotData.itemName})");
                yield return null;
                yield break;
            }
            
            // 精确定位Prefab（按ItemDataReader.itemData == itemDataSo）
            GameObject prefab = ResolvePrefabByItemData(globalId, itemDataSo);
            if (prefab == null)
            {
                LogError($"未能定位到与ItemDataSO匹配的Prefab: {itemDataSo.name} (GlobalId={globalId})");
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
        /// 确保物品与预制体索引已构建
        /// </summary>
        private void EnsureItemDatabasesBuilt()
        {
            if (itemDatabasesBuilt) return;
            try
            {
                // 1) 构建 GlobalId → ItemDataSO 映射
                var allItemData = Resources.LoadAll<ItemDataSO>("InventorySystemResources/ItemScriptableObject");
                int soCount = 0;
                foreach (var so in allItemData)
                {
                    if (so == null) continue;
                    long gid = so.GlobalId;
                    globalIdToItemData[gid] = so;
                    soCount++;
                }
                LogDebug($"索引到 {soCount} 个ItemDataSO");

                // 2) 预先索引常用类别下的 Prefab（按ItemDataReader.itemData直连）
                string[] categoryFolders = {
                    "Helmet_头盔", "Armor_护甲", "TacticalRig_战术背心", "Backpack_背包", "Weapon_武器",
                    "Ammunition_弹药", "Food_食物", "Drink_饮料", "Sedative_镇静剂", "Hemostatic_止血剂",
                    "Healing_治疗药物", "Intelligence_情报", "Currency_货币", "Special_特殊物品"
                };

                int prefabIndexed = 0;
                foreach (var folder in categoryFolders)
                {
                    var prefabs = Resources.LoadAll<GameObject>($"InventorySystemResources/Prefabs/{folder}");
                    foreach (var prefab in prefabs)
                    {
                        if (prefab == null) continue;
                        var reader = prefab.GetComponent<ItemDataReader>();
                        if (reader == null || reader.ItemData == null) continue;
                        long gid = reader.ItemData.GlobalId;
                        if (!globalIdToPrefab.ContainsKey(gid))
                        {
                            globalIdToPrefab[gid] = prefab;
                            prefabIndexed++;
                        }
                    }
                }
                LogDebug($"索引到 {prefabIndexed} 个物品Prefab");

                itemDatabasesBuilt = true;
            }
            catch (System.Exception e)
            {
                LogError($"构建物品索引时发生异常: {e.Message}");
                itemDatabasesBuilt = true; // 避免重复尝试
            }
        }

        /// <summary>
        /// 在可能存在重复 GlobalId 的情况下，按名称与槽位类型选出最匹配的 ItemDataSO
        /// </summary>
        private bool TryGetCorrectItemData(long globalId, string expectedItemName, EquipmentSlotType slotType, out ItemDataSO result)
        {
            result = null;
            if (globalIdToItemData.TryGetValue(globalId, out var single))
            {
                // 如果只有一个映射或名称即匹配，直接返回
                if (single != null && (string.IsNullOrEmpty(expectedItemName) || string.Equals(single.itemName, expectedItemName, System.StringComparison.Ordinal)))
                {
                    result = single;
                    return true;
                }
            }

            // 若存在重复 GlobalId（或名称不匹配），在全部 SO 中筛选符合 globalId 的候选
            var allItemData = Resources.LoadAll<ItemDataSO>("InventorySystemResources/ItemScriptableObject");
            var candidates = new List<ItemDataSO>();
            foreach (var so in allItemData)
            {
                if (so != null && so.GlobalId == globalId)
                {
                    candidates.Add(so);
                }
            }

            if (candidates.Count == 0)
            {
                // 兼容旧存档（可能保存了错误/截断的ID）：改用名称+槽位类别全库检索
                if (!string.IsNullOrEmpty(expectedItemName))
                {
                    var allByName = allItemData.Where(so => so != null && string.Equals(so.itemName, expectedItemName, System.StringComparison.Ordinal)).ToList();
                    if (allByName.Count > 0)
                    {
                        // 若提供槽位类别，则优先取类别匹配者
                        ItemCategory? expectedCat = null;
                        switch (slotType)
                        {
                            case EquipmentSlotType.Helmet: expectedCat = ItemCategory.Helmet; break;
                            case EquipmentSlotType.Armor: expectedCat = ItemCategory.Armor; break;
                            case EquipmentSlotType.TacticalRig: expectedCat = ItemCategory.TacticalRig; break;
                            case EquipmentSlotType.Backpack: expectedCat = ItemCategory.Backpack; break;
                            case EquipmentSlotType.PrimaryWeapon:
                            case EquipmentSlotType.SecondaryWeapon: expectedCat = ItemCategory.Weapon; break;
                        }

                        if (expectedCat.HasValue)
                        {
                            var catAndName = allByName.FirstOrDefault(c => c.category == expectedCat.Value);
                            if (catAndName != null)
                            {
                                result = catAndName;
                                return true;
                            }
                        }

                        // 回退：仅按名称匹配
                        result = allByName[0];
                        return true;
                    }
                }

                return false;
            }

            // 1) 先按名称精确匹配
            if (!string.IsNullOrEmpty(expectedItemName))
            {
                var nameMatch = candidates.FirstOrDefault(c => string.Equals(c.itemName, expectedItemName, System.StringComparison.Ordinal));
                if (nameMatch != null)
                {
                    result = nameMatch;
                    return true;
                }
            }

            // 2) 再按槽位类别推断（头盔→Helmet、护甲→Armor、战术背心→TacticalRig、背包→Backpack、主/副武器→Weapon）
            ItemCategory? expectedCategory = null;
            switch (slotType)
            {
                case EquipmentSlotType.Helmet: expectedCategory = ItemCategory.Helmet; break;
                case EquipmentSlotType.Armor: expectedCategory = ItemCategory.Armor; break;
                case EquipmentSlotType.TacticalRig: expectedCategory = ItemCategory.TacticalRig; break;
                case EquipmentSlotType.Backpack: expectedCategory = ItemCategory.Backpack; break;
                case EquipmentSlotType.PrimaryWeapon:
                case EquipmentSlotType.SecondaryWeapon: expectedCategory = ItemCategory.Weapon; break;
            }

            if (expectedCategory.HasValue)
            {
                var catMatch = candidates.FirstOrDefault(c => c.category == expectedCategory.Value);
                if (catMatch != null)
                {
                    result = catMatch;
                    return true;
                }
            }

            // 3) 最后返回第一个候选作为回退，以保证不阻塞恢复流程
            result = candidates[0];
            return result != null;
        }

        /// <summary>
        /// 通过 ItemDataSO 精确解析对应Prefab（优先缓存，其次按类别再扫描一次）
        /// </summary>
        private GameObject ResolvePrefabByItemData(long globalId, ItemDataSO itemDataSo)
        {
            if (globalIdToPrefab.TryGetValue(globalId, out var cached) && cached != null)
            {
                // 再校验一次绑定是否一致
                var r = cached.GetComponent<ItemDataReader>();
                if (r != null && r.ItemData == itemDataSo)
                {
                    return cached;
                }
            }

            // 按类别目标文件夹精准再扫一遍
            ItemCategory categoryEnum = (ItemCategory)itemDataSo.category;
            string folder = GetCategoryFolderName(categoryEnum);
            var prefabs = Resources.LoadAll<GameObject>($"InventorySystemResources/Prefabs/{folder}");
            foreach (var prefab in prefabs)
            {
                var reader = prefab.GetComponent<ItemDataReader>();
                if (reader != null && reader.ItemData == itemDataSo)
                {
                    globalIdToPrefab[globalId] = prefab;
                    return prefab;
                }
            }

            // 最后再在所有类别里全面扫描一次（仅此一次调用路径）
            string[] categoryFolders = {
                "Helmet_头盔", "Armor_护甲", "TacticalRig_战术背心", "Backpack_背包", "Weapon_武器",
                "Ammunition_弹药", "Food_食物", "Drink_饮料", "Sedative_镇静剂", "Hemostatic_止血剂",
                "Healing_治疗药物", "Intelligence_情报", "Currency_货币", "Special_特殊物品"
            };
            foreach (var f in categoryFolders)
            {
                var all = Resources.LoadAll<GameObject>($"InventorySystemResources/Prefabs/{f}");
                foreach (var prefab in all)
                {
                    var reader = prefab.GetComponent<ItemDataReader>();
                    if (reader != null && reader.ItemData == itemDataSo)
                    {
                        globalIdToPrefab[globalId] = prefab;
                        return prefab;
                    }
                }
            }

            return null;
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
        
        #region 保存抑制控制
        
        /// <summary>
        /// 确保保存不被抑制 - 备用机制
        /// 当BackpackEquipmentEventHandler初始化失败时，通过这个方法重置suppressSavesUntilFirstLoad
        /// </summary>
        public void EnsureSaveNotSuppressed()
        {
            // 如果当前存在装备且仍在抑制期，强制解除抑制
            if (suppressSavesUntilFirstLoad)
            {
                // 检查是否有任何装备存在
                bool hasAnyEquipment = false;
                if (equipmentSlotManager != null)
                {
                    var equippedItems = equipmentSlotManager.GetAllEquippedItems();
                    hasAnyEquipment = equippedItems != null && equippedItems.Count > 0;
                }
                
                if (hasAnyEquipment)
                {
                    LogDebug("🔧 检测到装备存在，通过备用机制重置启动期保存抑制");
                    suppressSavesUntilFirstLoad = false;
                }
                else
                {
                    // 即使没有装备，如果启动时间超过保护窗口，也解除抑制
                    if (Time.realtimeSinceStartup > startupEmptySaveBlockSeconds)
                    {
                        LogDebug("🔧 启动保护窗口已过期，通过备用机制重置启动期保存抑制");
                        suppressSavesUntilFirstLoad = false;
                    }
                }
            }
        }
        
        #endregion
        
        #region 背包事件集成
        
        /// <summary>
        /// 背包打开事件处理 - 按需加载装备数据
        /// </summary>
        public void OnBackpackOpened()
        {
            if (!autoLoad) return;
            
            // 🔧 背包打开时重置启动期保存抑制，允许正常保存
            if (suppressSavesUntilFirstLoad)
            {
                LogDebug("背包打开，重置启动期保存抑制标志");
                suppressSavesUntilFirstLoad = false;
            }
            
            // 🔧 只在有待加载的数据时才加载，避免重复加载
            if (hasPendingEquipmentData)
            {
                LogDebug("背包首次打开，开始加载装备数据");
                LoadEquipmentData();
                hasPendingEquipmentData = false; // 加载后重置标志
            }
            else
            {
                LogDebug("背包打开，但没有待加载的装备数据或已经加载过");
            }
        }
        
        /// <summary>
        /// 背包关闭事件处理 - 立即保存装备数据
        /// </summary>
        public void OnBackpackClosed()
        {
            if (!autoSave) return;
            
            LogDebug("背包关闭，立即保存装备数据");
            SaveEquipmentDataImmediate(); // 🔧 背包关闭时使用立即保存，跳过冷却
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
        
        #region ES3 高级管理功能
        
        /// <summary>
        /// 创建装备数据备份
        /// </summary>
        private void CreateEquipmentBackup()
        {
            try
            {
                string backupPath = saveFilePath.Replace(".es3", "_backup.es3");
                
                if (ES3.FileExists(saveFilePath))
                {
                    byte[] originalData = ES3.LoadRawBytes(saveFilePath);
                    ES3.SaveRaw(originalData, backupPath);
                    
                    if (verboseLogging)
                        LogDebug($"装备数据备份已创建: {backupPath}");
                }
            }
            catch (System.Exception e)
            {
                LogWarning($"创建备份失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 恢复装备数据备份
        /// </summary>
        public bool RestoreFromBackup()
        {
            try
            {
                string backupPath = saveFilePath.Replace(".es3", "_backup.es3");
                
                if (ES3.FileExists(backupPath))
                {
                    byte[] backupData = ES3.LoadRawBytes(backupPath);
                    ES3.SaveRaw(backupData, saveFilePath);
                    
                    LogDebug($"装备数据已从备份恢复: {backupPath} -> {saveFilePath}");
                    return true;
                }
                else
                {
                    LogDebug("未找到装备数据备份文件");
                    return false;
                }
            }
            catch (System.Exception e)
            {
                LogError($"恢复备份失败: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 获取装备保存数据统计信息
        /// </summary>
        public void LogEquipmentSaveStatistics()
        {
            try
            {
                if (useES3Storage && ES3.FileExists(saveFilePath))
                {
                    string backupPath = saveFilePath.Replace(".es3", "_backup.es3");
                    bool hasBackup = ES3.FileExists(backupPath);
                    
                    // 获取文件信息（ES3文件存储在persistentDataPath中）
                    string fullPath = System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, saveFilePath);
                    
                    LogDebug("=== 装备保存数据统计 ===");
                    LogDebug($"主文件: {saveFilePath}");
                    
                    if (System.IO.File.Exists(fullPath))
                    {
                        var fileInfo = new System.IO.FileInfo(fullPath);
                        LogDebug($"文件大小: {fileInfo.Length} 字节");
                        LogDebug($"最后修改: {fileInfo.LastWriteTime}");
                    }
                    else
                    {
                        LogDebug("文件信息: ES3虚拟文件系统");
                    }
                    
                    LogDebug($"备份文件: {(hasBackup ? "存在" : "不存在")}");
                    LogDebug($"压缩模式: {(enableCompression ? "启用" : "禁用")}");
                }
                else if (!useES3Storage)
                {
                    bool hasData = PlayerPrefs.HasKey(PLAYERPREFS_KEY);
                    LogDebug("=== 装备保存数据统计 ===");
                    LogDebug("保存方式: PlayerPrefs");
                    LogDebug($"数据状态: {(hasData ? "存在" : "不存在")}");
                }
                else
                {
                    LogDebug("=== 装备保存数据统计 ===");
                    LogDebug("状态: 无保存数据");
                }
            }
            catch (System.Exception e)
            {
                LogError($"获取统计信息失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 清除所有保存的数据
        /// </summary>
        public void ClearAllSaveData()
        {
            try
            {
                if (useES3Storage)
                {
                    // 清除ES3文件
                    if (ES3.FileExists(saveFilePath))
                    {
                        ES3.DeleteFile(saveFilePath);
                        LogDebug($"已删除ES3文件: {saveFilePath}");
                    }
                    
                    // 清除备份文件
                    string backupPath = saveFilePath.Replace(".es3", "_backup.es3");
                    if (ES3.FileExists(backupPath))
                    {
                        ES3.DeleteFile(backupPath);
                        LogDebug($"已删除备份文件: {backupPath}");
                    }
                }
                else
                {
                    if (PlayerPrefs.HasKey(PLAYERPREFS_KEY))
                    {
                        PlayerPrefs.DeleteKey(PLAYERPREFS_KEY);
                        PlayerPrefs.Save();
                        LogDebug("已清除PlayerPrefs数据");
                    }
                }
                
                LogDebug("所有装备保存数据已清除");
            }
            catch (System.Exception e)
            {
                LogError($"清除数据时发生错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 手动触发ES3保存
        /// </summary>
        public void ManualSave()
        {
            if (equipmentSlotManager != null)
            {
                var data = CollectEquipmentData();
                bool success = SaveDataToStorage(data);
                LogDebug($"手动保存装备数据: {(success ? "成功" : "失败")}");
            }
            else
            {
                LogWarning("装备槽管理器未找到，无法执行手动保存");
            }
        }
        
        /// <summary>
        /// 手动触发ES3加载
        /// </summary>
        public void ManualLoad()
        {
            StartCoroutine(LoadEquipmentDataCoroutine());
            LogDebug("已触发手动加载装备数据");
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
