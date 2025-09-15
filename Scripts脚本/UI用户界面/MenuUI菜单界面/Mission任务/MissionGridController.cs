using System.Collections;
using UnityEngine;
using TMPro;
using InventorySystem;

/// <summary>
/// 任务界面网格控制器 - 作为任务系统的网格分发器
/// 负责在任务界面中管理仓库网格的实例化、数据同步和生命周期
/// 与BackpackPanelController保持相同的接口设计，确保数据互通
/// </summary>
public class MissionGridController : MonoBehaviour
{
    [Header("网格预制件设置")]
    [SerializeField] private GameObject warehouseGridPrefab;
    [Tooltip("仓库网格预制件，应与BackpackPanelController使用相同的预制件")]
    
    [Header("界面引用")]
    [SerializeField] private RectTransform gridContainer;
    [Tooltip("网格实例化的父容器")]
    [SerializeField] private TextMeshProUGUI gridTitleText;
    [Tooltip("网格标题文本组件")]
    
    [Header("标题设置")]
    [SerializeField] private string warehouseTitleText = "任务仓库";
    [Tooltip("任务界面仓库网格显示的标题文本")]
    
    [Header("数据同步设置")]
    [SerializeField] private string missionUniqueId = "mission_warehouse";
    [Tooltip("任务网格的唯一标识符，用于数据同步")]
    [SerializeField] private bool syncWithBackpack = true;
    [Tooltip("是否与背包系统同步数据")]
    
    [Header("调试设置")]
    [SerializeField] private bool showDebugLog = false;
    [Tooltip("是否显示调试日志")]
    
    // 网格管理状态
    private GameObject currentGrid;
    private GridSaveManager gridSaveManager;
    private bool isInitialized = false;
    private bool isGridActive = false;
    
    // 数据同步相关
    private const string WAREHOUSE_GRID_GUID = "warehouse_grid_main"; // 与背包系统保持一致的GUID
    private BackpackPanelController backpackController; // 背包控制器引用，用于数据同步
    
    // 事件系统
    public System.Action OnMissionGridCreated;
    public System.Action OnMissionGridDestroyed;
    public System.Action<string> OnDataSyncCompleted;
    
    #region Unity生命周期
    
    private void Awake()
    {
        // 核心初始化
        InitializeMissionGridController();
        
        if (showDebugLog)
            Debug.Log($"MissionGridController: Awake初始化完成，任务ID: {missionUniqueId}");
    }
    
    private void OnEnable()
    {
        if (showDebugLog)
            Debug.Log("MissionGridController: 任务界面激活，开始创建网格");
        
        // 延迟创建网格，确保界面完全激活
        StartCoroutine(DelayedCreateMissionGrid());
    }
    
    private void OnDisable()
    {
        if (showDebugLog)
            Debug.Log("MissionGridController: 任务界面关闭，保存并销毁网格");
        
        // 保存数据并销毁网格
        SaveAndDestroyGrid();
    }
    
    #endregion
    
    #region 初始化
    
    /// <summary>
    /// 初始化任务网格控制器
    /// </summary>
    private void InitializeMissionGridController()
    {
        if (isInitialized) return;
        
        // 初始化GridSaveManager
        InitializeGridSaveManager();
        
        // 确保InventorySaveManager存在
        EnsureInventorySaveManagerExists();
        
        // 查找背包控制器引用（用于数据同步）
        if (syncWithBackpack)
        {
            FindBackpackControllerReference();
        }
        
        isInitialized = true;
        
        if (showDebugLog)
            Debug.Log("MissionGridController: 初始化完成");
    }
    
    /// <summary>
    /// 初始化GridSaveManager
    /// </summary>
    private void InitializeGridSaveManager()
    {
        if (gridSaveManager == null)
        {
            gridSaveManager = gameObject.AddComponent<GridSaveManager>();
            
            // 设置唯一ID，与背包系统保持一致
            if (syncWithBackpack)
            {
                // 使用与背包相同的uniqueId来确保数据互通
                var backpackRef = FindObjectOfType<BackpackPanelController>();
                if (backpackRef != null)
                {
                    // 通过反射获取背包的uniqueId
                    var backpackIdField = backpackRef.GetType().GetField("backpackUniqueId", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (backpackIdField != null)
                    {
                        string backpackId = (string)backpackIdField.GetValue(backpackRef);
                        if (!string.IsNullOrEmpty(backpackId))
                        {
                            missionUniqueId = backpackId; // 使用相同的ID确保数据同步
                        }
                    }
                }
            }
            
            // 设置GridSaveManager的uniqueId
            var uniqueIdField = gridSaveManager.GetType().GetField("uniqueId", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (uniqueIdField != null)
            {
                uniqueIdField.SetValue(gridSaveManager, missionUniqueId);
            }
            
            if (showDebugLog)
                Debug.Log($"MissionGridController: GridSaveManager初始化完成，使用ID: {missionUniqueId}");
        }
    }
    
    /// <summary>
    /// 确保InventorySaveManager存在
    /// </summary>
    private void EnsureInventorySaveManagerExists()
    {
        var saveManager = InventorySaveManager.Instance;
        if (saveManager == null)
        {
            // 创建InventorySaveManager
            var saveManagerObj = new GameObject("InventorySaveManager");
            saveManager = saveManagerObj.AddComponent<InventorySaveManager>();
            DontDestroyOnLoad(saveManagerObj);
            
            if (showDebugLog)
                Debug.Log("MissionGridController: 创建了新的InventorySaveManager");
        }
    }
    
    /// <summary>
    /// 查找背包控制器引用
    /// </summary>
    private void FindBackpackControllerReference()
    {
        if (backpackController == null)
        {
            backpackController = FindObjectOfType<BackpackPanelController>();
            if (backpackController != null)
            {
                if (showDebugLog)
                    Debug.Log("MissionGridController: 找到背包控制器引用，数据同步已启用");
            }
            else
            {
                if (showDebugLog)
                    Debug.LogWarning("MissionGridController: 未找到背包控制器，数据同步功能受限");
            }
        }
    }
    
    #endregion
    
    #region 网格管理
    
    /// <summary>
    /// 延迟创建任务网格
    /// </summary>
    private IEnumerator DelayedCreateMissionGrid()
    {
        // 等待界面完全激活
        yield return new WaitForEndOfFrame();
        
        // 创建仓库网格
        CreateWarehouseGrid();
        
        // 等待网格实例化完成
        yield return new WaitForEndOfFrame();
        
        // 加载数据
        LoadGridData();
        
        // 触发生成固定物品（如果需要）
        TriggerFixedItemSpawn();
    }
    
    /// <summary>
    /// 创建仓库网格
    /// </summary>
    public void CreateWarehouseGrid()
    {
        if (isGridActive)
        {
            if (showDebugLog)
                Debug.Log("MissionGridController: 网格已存在，跳过创建");
            return;
        }
        
        if (warehouseGridPrefab == null)
        {
            Debug.LogError("MissionGridController: 仓库网格预制件未设置！");
            return;
        }
        
        if (gridContainer == null)
        {
            Debug.LogError("MissionGridController: 网格容器未设置！");
            return;
        }
        
        try
        {
            // 实例化网格
            currentGrid = Instantiate(warehouseGridPrefab, gridContainer);
            currentGrid.SetActive(true);
            
            // 设置网格变换
            SetupGridTransform();
            
            // 更新标题
            UpdateGridTitle();
            
            isGridActive = true;
            
            if (showDebugLog)
                Debug.Log($"MissionGridController: 仓库网格创建成功 - {currentGrid.name}");
            
            // 触发事件
            OnMissionGridCreated?.Invoke();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"MissionGridController: 创建网格时发生错误: {e.Message}");
        }
    }
    
    /// <summary>
    /// 设置网格变换属性
    /// </summary>
    private void SetupGridTransform()
    {
        if (currentGrid == null) return;
        
        RectTransform gridRT = currentGrid.GetComponent<RectTransform>();
        if (gridRT == null) return;
        
        // 设置锚点和位置（与BackpackPanelController的仓库网格保持一致）
        gridRT.anchorMin = new Vector2(0, 0);
        gridRT.anchorMax = new Vector2(1, 1);
        gridRT.anchoredPosition = Vector2.zero;
        gridRT.sizeDelta = Vector2.zero;
        
        if (showDebugLog)
            Debug.Log("MissionGridController: 网格变换设置完成");
    }
    
    /// <summary>
    /// 更新网格标题
    /// </summary>
    private void UpdateGridTitle()
    {
        if (gridTitleText != null)
        {
            gridTitleText.text = warehouseTitleText;
            
            if (showDebugLog)
                Debug.Log($"MissionGridController: 标题更新为 '{warehouseTitleText}'");
        }
    }
    
    /// <summary>
    /// 加载网格数据
    /// </summary>
    private void LoadGridData()
    {
        if (currentGrid == null || gridSaveManager == null)
        {
            if (showDebugLog)
                Debug.LogWarning("MissionGridController: 无法加载数据，网格或保存管理器为空");
            return;
        }
        
        try
        {
            // 获取ItemGrid组件
            ItemGrid itemGrid = currentGrid.GetComponent<ItemGrid>();
            if (itemGrid == null)
            {
                Debug.LogError("MissionGridController: 网格缺少ItemGrid组件！");
                return;
            }
            
            // 设置当前网格到保存管理器
            gridSaveManager.SetCurrentGrid(itemGrid, WAREHOUSE_GRID_GUID);
            
            // 注册并加载网格数据（标记为仓库网格）
            gridSaveManager.RegisterAndLoadGridWithGUID(WAREHOUSE_GRID_GUID, true);
            
            if (showDebugLog)
                Debug.Log($"MissionGridController: 网格数据加载完成 - GUID: {WAREHOUSE_GRID_GUID}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"MissionGridController: 加载网格数据时发生错误: {e.Message}");
        }
    }
    
    /// <summary>
    /// 触发固定物品生成
    /// </summary>
    private void TriggerFixedItemSpawn()
    {
        if (currentGrid == null) return;
        
        try
        {
            // 获取ItemGrid组件
            ItemGrid itemGrid = currentGrid.GetComponent<ItemGrid>();
            if (itemGrid == null) return;
            
            // 调用固定物品生成管理器
            var spawnManager = InventorySystem.SpawnSystem.FixedItemSpawnManager.Instance;
            if (spawnManager != null)
            {
                // 使用与背包系统相同的容器ID来确保一致性
                string containerId = WAREHOUSE_GRID_GUID;
                
                // 生成固定物品
                spawnManager.SpawnFixedItems(itemGrid, null, containerId);
                
                if (showDebugLog)
                    Debug.Log("MissionGridController: 固定物品生成完成");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"MissionGridController: 生成固定物品时发生错误: {e.Message}");
        }
    }
    
    /// <summary>
    /// 保存并销毁网格
    /// </summary>
    public void SaveAndDestroyGrid()
    {
        if (!isGridActive || currentGrid == null) return;
        
        try
        {
            // 强制保存数据
            ForceSaveGridData();
            
            // 销毁网格对象
            DestroyCurrentGrid();
            
            if (showDebugLog)
                Debug.Log("MissionGridController: 网格保存并销毁完成");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"MissionGridController: 保存并销毁网格时发生错误: {e.Message}");
        }
    }
    
    /// <summary>
    /// 强制保存网格数据
    /// </summary>
    private void ForceSaveGridData()
    {
        if (gridSaveManager == null) return;
        
        try
        {
            // 强制保存当前网格
            gridSaveManager.ForceSaveCurrentGrid();
            
            // 同时调用全局保存确保数据持久化
            var inventorySaveManager = InventorySaveManager.Instance;
            if (inventorySaveManager != null)
            {
                inventorySaveManager.SaveInventory();
            }
            
            if (showDebugLog)
                Debug.Log("MissionGridController: 网格数据强制保存完成");
            
            // 触发数据同步完成事件
            OnDataSyncCompleted?.Invoke(WAREHOUSE_GRID_GUID);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"MissionGridController: 强制保存数据时发生错误: {e.Message}");
        }
    }
    
    /// <summary>
    /// 销毁当前网格
    /// </summary>
    private void DestroyCurrentGrid()
    {
        if (currentGrid != null)
        {
            Destroy(currentGrid);
            currentGrid = null;
            isGridActive = false;
            
            if (showDebugLog)
                Debug.Log("MissionGridController: 网格对象已销毁");
            
            // 触发事件
            OnMissionGridDestroyed?.Invoke();
        }
    }
    
    #endregion
    
    #region 数据同步接口
    
    /// <summary>
    /// 与背包系统同步数据
    /// </summary>
    public void SyncWithBackpackData()
    {
        if (!syncWithBackpack || backpackController == null)
        {
            if (showDebugLog)
                Debug.LogWarning("MissionGridController: 数据同步未启用或背包控制器不可用");
            return;
        }
        
        try
        {
            // 强制背包系统保存数据
            var forcesSaveMethod = backpackController.GetType().GetMethod("ForcesSaveAllData", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (forcesSaveMethod != null)
            {
                forcesSaveMethod.Invoke(backpackController, null);
                
                if (showDebugLog)
                    Debug.Log("MissionGridController: 已触发背包系统数据保存");
            }
            
            // 重新加载任务网格数据
            if (isGridActive)
            {
                LoadGridData();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"MissionGridController: 数据同步时发生错误: {e.Message}");
        }
    }
    
    /// <summary>
    /// 获取当前网格是否激活
    /// </summary>
    public bool IsGridActive()
    {
        return isGridActive && currentGrid != null;
    }
    
    /// <summary>
    /// 获取当前网格的GUID
    /// </summary>
    public string GetCurrentGridGUID()
    {
        return WAREHOUSE_GRID_GUID;
    }
    
    /// <summary>
    /// 获取任务唯一ID
    /// </summary>
    public string GetMissionUniqueId()
    {
        return missionUniqueId;
    }
    
    #endregion
    
    #region 调试和工具方法
    
    /// <summary>
    /// 设置调试日志开关
    /// </summary>
    public void SetDebugLog(bool enabled)
    {
        showDebugLog = enabled;
        
        if (showDebugLog)
            Debug.Log($"MissionGridController: 调试日志已{(enabled ? "启用" : "禁用")}");
    }
    
    /// <summary>
    /// 获取网格统计信息
    /// </summary>
    public string GetGridStats()
    {
        if (!IsGridActive()) return "网格未激活";
        
        var itemGrid = currentGrid.GetComponent<ItemGrid>();
        if (itemGrid == null) return "无ItemGrid组件";
        
        // 统计网格中的物品数量
        int itemCount = 0;
        var gridItems = itemGrid.GetComponentsInChildren<Item>();
        if (gridItems != null)
        {
            itemCount = gridItems.Length;
        }
        
        return $"网格状态: 激活 | 物品数量: {itemCount} | GUID: {WAREHOUSE_GRID_GUID}";
    }
    
    #endregion
}
