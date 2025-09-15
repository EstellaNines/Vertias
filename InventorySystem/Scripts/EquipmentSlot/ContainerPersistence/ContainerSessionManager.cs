using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InventorySystem
{
    /// <summary>
    /// 容器跨会话持久化管理器
    /// 严格控制装备->恢复->保存的执行顺序
    /// </summary>
    public class ContainerSessionManager : MonoBehaviour
    {
        [Header("跨会话持久化设置")]
        [FieldLabel("启用跨会话持久化")]
        [Tooltip("启用容器内容的跨会话保存和恢复")]
        public bool enableCrossSessionPersistence = true;
        
        [FieldLabel("装备恢复延迟时间")]
        [Tooltip("装备恢复完成后，延迟多少秒开始恢复容器内容")]
        public float equipmentRestoreDelay = 2.0f;
        
        [FieldLabel("强制保存延迟")]
        [Tooltip("应用退出时的强制保存延迟时间")]
        public float forceQuitSaveDelay = 0.5f;
        
        [Header("调试设置")]
        [FieldLabel("显示调试日志")]
        public bool showDebugLogs = true;
        
        [FieldLabel("详细执行日志")]
        [Tooltip("显示详细的执行顺序日志")]
        public bool verboseLogging = false;

        // 单例模式
        private static ContainerSessionManager _instance;
        public static ContainerSessionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ContainerSessionManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ContainerSessionManager");
                        _instance = go.AddComponent<ContainerSessionManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        // 组件引用
        private EquipmentPersistenceManager equipmentManager;
        private ContainerSaveManager containerManager;
        
        // 状态标记
        private bool isEquipmentRestored = false;
        private bool isContainerRestored = false;
        private bool isApplicationQuitting = false;
        private Coroutine restoreCoroutine;

        #region Unity生命周期

        private void Awake()
        {
            // 确保单例
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                LogDebug("容器会话管理器初始化");
                
                // 初始化时立即加载跨会话数据
                InitializeComponents();
                
                // 确保在游戏启动时加载跨会话数据
                StartCoroutine(InitializeCrossSessionDataCoroutine());
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }
        
        /// <summary>
        /// 初始化跨会话数据加载
        /// </summary>
        private IEnumerator InitializeCrossSessionDataCoroutine()
        {
            // 等待ContainerSaveManager初始化完成
            yield return new WaitForSeconds(0.5f);
            
            if (containerManager != null)
            {
                // 确保跨会话数据已加载
                bool loaded = containerManager.LoadCrossSessionData();
                if (loaded)
                {
                    LogDebug("启动时跨会话数据加载成功");
                }
                else
                {
                    LogDebug("启动时未找到跨会话数据或加载失败");
                }
            }
        }

        private void Start()
        {
            if (enableCrossSessionPersistence)
            {
                RegisterEventListeners();
                LogDebug("跨会话持久化已启用，开始监听装备恢复事件");
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && enableCrossSessionPersistence && !isApplicationQuitting)
            {
                LogDebug("应用暂停，执行强制保存");
                ForceContainerSave("应用暂停");
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && enableCrossSessionPersistence && !isApplicationQuitting)
            {
                LogDebug("应用失去焦点，执行强制保存");
                ForceContainerSave("失去焦点");
            }
        }

        private void OnApplicationQuit()
        {
            isApplicationQuitting = true;
            if (enableCrossSessionPersistence)
            {
                LogDebug("应用退出，执行最终强制保存");
                ForceContainerSave("应用退出");
                
                // 短暂延迟确保保存完成
                System.Threading.Thread.Sleep((int)(forceQuitSaveDelay * 1000));
            }
        }

        private void OnDestroy()
        {
            UnregisterEventListeners();
        }

        #endregion

        #region 组件初始化

        /// <summary>
        /// 初始化组件引用
        /// </summary>
        private void InitializeComponents()
        {
            StartCoroutine(InitializeComponentsCoroutine());
        }

        private IEnumerator InitializeComponentsCoroutine()
        {
            // 等待其他管理器初始化
            yield return new WaitForSeconds(0.5f);
            
            // 查找装备持久化管理器
            equipmentManager = EquipmentPersistenceManager.Instance;
            if (equipmentManager == null)
            {
                LogWarning("未找到EquipmentPersistenceManager实例");
            }
            
            // 查找容器保存管理器
            containerManager = ContainerSaveManager.Instance;
            if (containerManager == null)
            {
                LogWarning("未找到ContainerSaveManager实例");
            }
            
            if (equipmentManager != null && containerManager != null)
            {
                LogDebug("组件引用初始化完成");
            }
        }

        #endregion

        #region 事件监听

        /// <summary>
        /// 注册事件监听器
        /// </summary>
        private void RegisterEventListeners()
        {
            // 监听场景加载事件
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            // 监听装备恢复完成事件
            EquipmentPersistenceManager.OnEquipmentRestored += OnEquipmentRestored;
            
            // 监听背包首次打开事件
            BackpackEquipmentEventHandler.OnBackpackFirstOpened += OnBackpackFirstOpened;
            
            // 监听背包面板事件（通过BackpackPanelController）
            RegisterBackpackEvents();
            
            LogDebug("事件监听器注册完成");
        }

        /// <summary>
        /// 注销事件监听器
        /// </summary>
        private void UnregisterEventListeners()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            EquipmentPersistenceManager.OnEquipmentRestored -= OnEquipmentRestored;
            BackpackEquipmentEventHandler.OnBackpackFirstOpened -= OnBackpackFirstOpened;
            UnregisterBackpackEvents();
        }

        /// <summary>
        /// 注册背包面板事件
        /// </summary>
        private void RegisterBackpackEvents()
        {
            StartCoroutine(RegisterBackpackEventsCoroutine());
        }

        private IEnumerator RegisterBackpackEventsCoroutine()
        {
            // 等待BackpackPanelController初始化
            yield return new WaitForSeconds(1f);
            
            var backpackController = FindObjectOfType<BackpackPanelController>();
            if (backpackController != null)
            {
                LogDebug("找到BackpackPanelController，准备监听背包打开和关闭事件");
            }
            else
            {
                LogWarning("未找到BackpackPanelController");
            }
            
            // 监听BackpackEquipmentEventHandler的背包打开事件
            var backpackEventHandler = FindObjectOfType<BackpackEquipmentEventHandler>();
            if (backpackEventHandler != null)
            {
                LogDebug("找到BackpackEquipmentEventHandler，将通过自定义事件监听背包打开");
            }
            else
            {
                LogWarning("未找到BackpackEquipmentEventHandler");
            }
        }

        /// <summary>
        /// 注销背包面板事件
        /// </summary>
        private void UnregisterBackpackEvents()
        {
            // 清理事件监听
        }

        #endregion

        #region 核心执行顺序控制

        /// <summary>
        /// 场景加载完成事件处理
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!enableCrossSessionPersistence) return;
            
            LogDebug($"场景加载完成: {scene.name}，重置状态并等待装备恢复事件");
            
            // 重置状态
            isEquipmentRestored = false;
            isContainerRestored = false;
        }
        
        /// <summary>
        /// 装备恢复完成事件处理
        /// </summary>
        private void OnEquipmentRestored()
        {
            if (!enableCrossSessionPersistence) return;
            if (isEquipmentRestored) return; // 避免重复处理

            LogDebug("🎯 收到装备恢复完成事件，立即开始恢复容器内容");

            isEquipmentRestored = true;

            // 装备恢复完成后，立即恢复容器内容
            StartCoroutine(DelayedContainerRestoreAfterEquipment());
        }
        
        /// <summary>
        /// 装备恢复后延迟恢复容器内容
        /// </summary>
        private IEnumerator DelayedContainerRestoreAfterEquipment()
        {
            if (!enableCrossSessionPersistence) yield break;

            LogDebug("🔄 装备恢复完成，等待 1 秒后开始恢复容器内容");
            yield return new WaitForSeconds(1f); // 确保装备完全初始化

            // 额外安全：若槽位未激活，不强制激活；容器实际加载逻辑在槽位OnEnable里处理

            if (isContainerRestored)
            {
                LogDebug("容器已恢复，跳过");
                yield break;
            }

            LogDebug("🔄 开始恢复容器内容...");
            StartContainerRestore();
            yield return new WaitForEndOfFrame();

            isContainerRestored = true;
            LogDebug("✅ 容器内容恢复完成");
            EnableContainerChangeMonitoring();
        }
        
        /// <summary>
        /// 背包首次打开事件处理（备用安全检查）
        /// </summary>
        private void OnBackpackFirstOpened()
        {
            if (!enableCrossSessionPersistence) return;
            
            LogDebug("🎯 背包首次打开");
            
            // 如果装备已恢复但容器未恢复，提供一个备用的恢复机制
            if (isEquipmentRestored && !isContainerRestored)
            {
                LogDebug("🔄 备用检查：装备已恢复但容器未恢复，启动备用恢复");
                StartCoroutine(DelayedContainerRestoreOnFirstOpen());
            }
            else if (isContainerRestored)
            {
                LogDebug("🔍 背包首次打开，容器已恢复");
            }
            else
            {
                LogDebug("🔍 背包首次打开，装备尚未恢复或容器恢复已在进行中");
            }
        }
        
        /// <summary>
        /// 检查是否应该跳过容器恢复（因为已经在装备恢复中处理了）
        /// </summary>
        private bool ShouldSkipContainerRestore()
        {
            // 检查背包容器是否已经有物品（说明已经恢复过了）
            var slotManager = EquipmentSlotManager.Instance;
            if (slotManager == null) return false;
            
            var backpackSlot = slotManager.GetEquipmentSlot(EquipmentSlotType.Backpack);
            if (backpackSlot != null && backpackSlot.HasEquippedItem)
            {
                var containerGrid = backpackSlot.GetComponentInChildren<ItemGrid>();
                if (containerGrid != null)
                {
                    try
                    {
                        // 检查容器网格是否处于激活状态
                        if (!containerGrid.gameObject.activeInHierarchy)
                        {
                            LogDebug("🔍 背包容器网格未激活，需要额外恢复");
                            return false;
                        }
                        
                        // 检查容器中是否已经有物品
                        int itemCount = 0;
                        for (int x = 0; x < containerGrid.gridSizeWidth; x++)
                        {
                            for (int y = 0; y < containerGrid.gridSizeHeight; y++)
                            {
                                try
                                {
                                    if (containerGrid.GetItemAt(x, y) != null)
                                    {
                                        itemCount++;
                                    }
                                }
                                catch (System.Exception ex)
                                {
                                    LogDebug($"🔍 检查容器位置 ({x}, {y}) 时发生异常: {ex.Message}");
                                    // 如果出现异常，说明容器网格可能未完全就绪
                                    return false;
                                }
                            }
                        }
                        
                        if (itemCount > 0)
                        {
                            LogDebug($"🔍 背包容器中已有 {itemCount} 个物品，说明已经恢复过了");
                            return true;
                        }
                        else
                        {
                            LogDebug("🔍 背包容器中无物品，可能需要恢复");
                            return false;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        LogDebug($"🔍 检查容器状态时发生异常: {ex.Message}，需要恢复");
                        return false;
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 延迟容器恢复
        /// </summary>
        private IEnumerator DelayedContainerRestore()
        {
            LogDebug($"✅ 装备恢复完成，等待 {equipmentRestoreDelay} 秒后开始恢复容器内容");
            
            yield return new WaitForSeconds(equipmentRestoreDelay);
            StartContainerRestore();
        }
        
        /// <summary>
        /// 背包首次打开时的延迟容器恢复
        /// </summary>
        private IEnumerator DelayedContainerRestoreOnFirstOpen()
        {
            if (!enableCrossSessionPersistence) yield break;
            
            LogDebug("🔄 背包已打开，等待 0.5 秒后开始恢复容器内容");
            yield return new WaitForSeconds(0.5f);
            
            if (isContainerRestored)
            {
                LogDebug("容器已恢复，跳过");
                yield break;
            }
            
            LogDebug("🔄 开始恢复容器内容...");
            StartContainerRestore();
            
            // 等待一帧确保恢复完成
            yield return new WaitForEndOfFrame();
            
            isContainerRestored = true;
            LogDebug("✅ 容器内容恢复完成");
            EnableContainerChangeMonitoring();
        }


        /// <summary>
        /// 检查装备是否已恢复（背包或挂具）
        /// </summary>
        private bool CheckIfEquipmentRestored()
        {
            if (equipmentManager == null) return false;
            
            // 通过EquipmentSlotManager检查是否有背包或挂具装备
            var slotManager = EquipmentSlotManager.Instance;
            if (slotManager == null) return false;
            
            // 检查背包槽
            var backpackSlot = slotManager.GetEquipmentSlot(EquipmentSlotType.Backpack);
            bool hasBackpack = backpackSlot != null && backpackSlot.HasEquippedItem;
            
            // 检查挂具槽  
            var tacticalRigSlot = slotManager.GetEquipmentSlot(EquipmentSlotType.TacticalRig);
            bool hasTacticalRig = tacticalRigSlot != null && tacticalRigSlot.HasEquippedItem;
            
            if (verboseLogging)
            {
                LogDebug($"装备检查: 背包={hasBackpack}, 挂具={hasTacticalRig}");
            }
            
            return hasBackpack || hasTacticalRig;
        }

        /// <summary>
        /// 开始恢复容器内容
        /// </summary>
        private void StartContainerRestore()
        {
            if (isContainerRestored)
            {
                LogDebug("容器内容已恢复，跳过");
                return;
            }
            
            LogDebug("🔄 开始恢复容器内容...");
            
            if (containerManager != null)
            {
                // 触发容器内容恢复
                StartCoroutine(RestoreContainerContentCoroutine());
            }
            else
            {
                LogWarning("ContainerSaveManager未找到，无法恢复容器内容");
            }
        }

        /// <summary>
        /// 恢复容器内容的协程
        /// </summary>
        private IEnumerator RestoreContainerContentCoroutine()
        {
            yield return new WaitForEndOfFrame();
            
            // 获取当前装备的容器
            var slotManager = EquipmentSlotManager.Instance;
            if (slotManager == null)
            {
                LogWarning("EquipmentSlotManager未找到");
                yield break;
            }
            
            bool anyContainerRestored = false;
            
            // 恢复背包内容
            var backpackSlot = slotManager.GetEquipmentSlot(EquipmentSlotType.Backpack);
            if (backpackSlot != null && backpackSlot.HasEquippedItem)
            {
                yield return StartCoroutine(RestoreSpecificContainer(backpackSlot, EquipmentSlotType.Backpack));
                anyContainerRestored = true;
            }
            
            // 恢复挂具内容
            var tacticalRigSlot = slotManager.GetEquipmentSlot(EquipmentSlotType.TacticalRig);
            if (tacticalRigSlot != null && tacticalRigSlot.HasEquippedItem)
            {
                yield return StartCoroutine(RestoreSpecificContainer(tacticalRigSlot, EquipmentSlotType.TacticalRig));
                anyContainerRestored = true;
            }
            
            if (anyContainerRestored)
            {
                isContainerRestored = true;
                LogDebug("✅ 容器内容恢复完成");
                
                // 恢复完成后，开始监听容器变化
                EnableContainerChangeMonitoring();
            }
            else
            {
                LogDebug("未找到需要恢复的容器");
            }
        }

        /// <summary>
        /// 恢复指定容器的内容
        /// </summary>
        private IEnumerator RestoreSpecificContainer(InventorySystem.EquipmentSlot equipmentSlot, EquipmentSlotType slotType)
        {
            LogDebug($"恢复 {slotType} 容器内容");
            
            var equippedItemReader = equipmentSlot.CurrentEquippedItem;
            if (equippedItemReader != null)
            {
                if (equippedItemReader.ItemData.IsContainer())
                {
                    // 获取容器网格
                    var containerGrid = equipmentSlot.GetComponentInChildren<ItemGrid>();
                    if (containerGrid != null)
                    {
                        // 调用ContainerSaveManager恢复内容
                        containerManager.LoadContainerContent(equippedItemReader, slotType, containerGrid);
                        LogDebug($"✅ {slotType} 容器内容恢复请求已发送");
                    }
                    else
                    {
                        LogWarning($"{slotType} 容器网格未找到");
                    }
                }
            }
            
            yield return null;
        }

        #endregion

        #region 保存机制

        /// <summary>
        /// 启用容器变化监听
        /// </summary>
        private void EnableContainerChangeMonitoring()
        {
            LogDebug("启用容器变化自动保存监听");
            // 这里可以添加对容器变化的监听逻辑
            // 当容器内容发生变化时，自动触发保存
        }

        /// <summary>
        /// 背包关闭时的强制保存
        /// </summary>
        public void OnBackpackClosed()
        {
            if (enableCrossSessionPersistence && isContainerRestored)
            {
                LogDebug("背包关闭，执行强制保存");
                ForceContainerSave("背包关闭");
            }
        }

        /// <summary>
        /// 强制保存所有容器内容
        /// </summary>
        private void ForceContainerSave(string reason)
        {
            if (containerManager != null)
            {
                LogDebug($"强制保存容器内容，原因: {reason}");
                
                // 调用ContainerSaveManager的强制保存方法
                containerManager.ForceSaveAllContainers();
                
                if (verboseLogging)
                {
                    LogDebug($"强制保存完成: {reason}");
                }
            }
        }

        #endregion

        #region 调试和日志

        /// <summary>
        /// 输出调试日志
        /// </summary>
        private void LogDebug(string message)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[ContainerSessionManager] {message}");
            }
        }

        /// <summary>
        /// 输出警告日志
        /// </summary>
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[ContainerSessionManager] {message}");
        }

        /// <summary>
        /// 输出错误日志
        /// </summary>
        private void LogError(string message)
        {
            Debug.LogError($"[ContainerSessionManager] {message}");
        }

        #endregion

        #region 公共API

        /// <summary>
        /// 手动触发容器恢复
        /// </summary>
        public void ManualRestoreContainers()
        {
            if (enableCrossSessionPersistence)
            {
                LogDebug("手动触发容器恢复");
                StartContainerRestore();
            }
        }

        /// <summary>
        /// 手动触发容器保存
        /// </summary>
        public void ManualSaveContainers()
        {
            if (enableCrossSessionPersistence)
            {
                ForceContainerSave("手动保存");
            }
        }

        /// <summary>
        /// 获取当前状态
        /// </summary>
        public string GetCurrentStatus()
        {
            return $"装备已恢复: {isEquipmentRestored}, 容器已恢复: {isContainerRestored}, 跨会话启用: {enableCrossSessionPersistence}";
        }

        #endregion
    }
}
