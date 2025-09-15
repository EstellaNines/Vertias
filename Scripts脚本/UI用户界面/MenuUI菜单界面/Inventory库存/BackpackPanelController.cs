using System.Collections;
using UnityEngine;
using TMPro;
using InventorySystem;

/// <summary>
/// BackpackPanel 内部控制器 - 专门负责管理面板内的网格切换逻辑
/// </summary>
public class BackpackPanelController : MonoBehaviour
{
    [Header("网格预制件设置")]
    [SerializeField] private GameObject warehouseGridPrefab;
    [SerializeField] private GameObject groundGridPrefab;
    [SerializeField] private GameObject shelfGridPrefab; // 货架网格预制件
    
    [Header("面板引用")]
    [SerializeField] private RectTransform rightPanelTransform; // 网格的父容器
    [SerializeField] private TextMeshProUGUI rightTitleText; // 右侧标题文本组件
    
    [Header("标题文本设置")]
    [SerializeField] private string warehouseTitleText = "Storage"; // 仓库模式显示的文本
    [SerializeField] private string groundTitleText = "Ground"; // 地面模式显示的文本
    [SerializeField] private string shelfTitleText = "Shelf"; // 货架模式显示的文本
    
    [Header("调试设置")]
    [SerializeField] private bool showDebugLog = false;
    
    [Header("背包标识设置")]
    [FieldLabel("背包唯一ID")]
    [Tooltip("背包的唯一标识符，留空则自动生成。不同背包必须有不同的ID！")]
    [SerializeField] private string backpackUniqueId = "";
    
    // 当前网格状态
    private GameObject currentGrid;
    private GridType currentGridType = GridType.Ground;
    private GridSaveManager gridSaveManager;
    private bool isInitialized = false;
    
    // InventoryController引用（用于管理提示器）
    private InventoryController inventoryController;
    
    // 事件：当网格切换完成时触发
    public System.Action<GridType> OnGridSwitchCompleted; // GridType: 当前网格类型
    
    #region 初始化
    
    private void Awake()
    {
        // 在Awake中完成核心初始化，确保更早执行
        InitializeBackpackId();
        InitializeGridSaveManager();
        EnsureSaveManagerExists();
        EnsureInventoryControllerExists();
        isInitialized = true;
        
        if (showDebugLog) Debug.Log($"BackpackPanelController: Awake初始化完成，背包ID: {backpackUniqueId}");
    }
    
    /// <summary>
    /// 初始化背包唯一ID
    /// </summary>
    private void InitializeBackpackId()
    {
        // 如果没有设置背包ID，自动生成一个
        if (string.IsNullOrEmpty(backpackUniqueId))
        {
            // 使用GameObject实例ID + 时间戳生成唯一ID
            int instanceId = GetInstanceID();
            string timeStamp = System.DateTime.Now.Ticks.ToString();
            backpackUniqueId = $"backpack_{Mathf.Abs(instanceId)}_{timeStamp.Substring(timeStamp.Length - 8)}";
            
            if (showDebugLog) Debug.Log($"BackpackPanelController: 自动生成背包ID: {backpackUniqueId}");
        }
        else
        {
            if (showDebugLog) Debug.Log($"BackpackPanelController: 使用预设背包ID: {backpackUniqueId}");
        }
    }
    
    private void Start()
    {
        // Start中再次确认初始化状态
        if (!isInitialized)
        {
            Debug.LogWarning("BackpackPanelController: Start时发现未初始化，执行补充初始化");
            ForceInitialize();
        }
        
        if (showDebugLog) Debug.Log("BackpackPanelController: Start验证完成");
    }
    
    private void OnEnable()
    {
        if (showDebugLog)
            Debug.Log("BackpackPanelController: OnEnable - 背包面板打开，开始检测装备槽");
        
        // 背包面板被激活时，重新检测和注册装备槽
        StartCoroutine(DetectAndRegisterEquipmentSlotsDelayed());
    }
    
    private void OnDisable()
    {
        if (showDebugLog)
            Debug.Log("BackpackPanelController: OnDisable - 背包面板关闭，强制保存装备数据");
        
        // 背包面板被禁用时，强制保存装备数据
        try
        {
            ForcesSaveAllData();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"BackpackPanelController: OnDisable保存失败: {e.Message}");
        }
    }
    
    private void OnDestroy()
    {
        if (showDebugLog)
            Debug.Log("BackpackPanelController: OnDestroy - 执行清理和保存");
        
        // 在销毁前强制保存所有数据
        try
        {
            ForcesSaveAllData();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"BackpackPanelController: OnDestroy保存失败: {e.Message}");
        }
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            if (showDebugLog)
                Debug.Log("BackpackPanelController: 应用暂停 - 执行保存");
            
            try
            {
                ForcesSaveAllData();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"BackpackPanelController: OnApplicationPause保存失败: {e.Message}");
            }
        }
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            if (showDebugLog)
                Debug.Log("BackpackPanelController: 应用失去焦点 - 执行保存");
            
            try
            {
                ForcesSaveAllData();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"BackpackPanelController: OnApplicationFocus保存失败: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// 初始化网格保存管理器
    /// </summary>
    private void InitializeGridSaveManager()
    {
        if (gridSaveManager == null)
        {
            GameObject saveManagerObj = new GameObject("GridSaveManager");
            saveManagerObj.transform.SetParent(this.transform);
            gridSaveManager = saveManagerObj.AddComponent<GridSaveManager>();
            
            if (showDebugLog)
                Debug.Log("BackpackPanelController: 已创建GridSaveManager实例");
        }
    }
    
    /// <summary>
    /// 确保保存管理器存在
    /// </summary>
    private void EnsureSaveManagerExists()
    {
        if (InventorySaveManager.Instance == null)
        {
            GameObject saveManager = new GameObject("InventorySaveManager");
            var saveManagerComponent = saveManager.AddComponent<InventorySaveManager>();
            
            // 确保DontDestroyOnLoad
            DontDestroyOnLoad(saveManager);
            
            if (showDebugLog)
                Debug.Log("BackpackPanelController: 已创建InventorySaveManager实例");
                
            // 等待一帧确保初始化完成
            StartCoroutine(DelayedSaveManagerSetup(saveManagerComponent));
        }
    }
    
    private System.Collections.IEnumerator DelayedSaveManagerSetup(InventorySaveManager saveManager)
    {
        yield return null; // 等待一帧
        
        if (saveManager != null)
        {
            // 通过反射设置必要的配置
            var saveManagerType = saveManager.GetType();
            
            // 启用自动保存
            var enableAutoSaveField = saveManagerType.GetField("enableAutoSave", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (enableAutoSaveField != null)
            {
                enableAutoSaveField.SetValue(saveManager, true);
            }
            
            // 启用保存日志（调试用）
            var showSaveLogField = saveManagerType.GetField("showSaveLog", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (showSaveLogField != null)
            {
                showSaveLogField.SetValue(saveManager, true);
            }
            
            if (showDebugLog)
                Debug.Log("BackpackPanelController: InventorySaveManager配置完成");
        }
    }
    
    /// <summary>
    /// 确保InventoryController引用存在
    /// </summary>
    private void EnsureInventoryControllerExists()
    {
        if (inventoryController == null)
        {
            inventoryController = FindObjectOfType<InventoryController>();
            if (inventoryController == null)
            {
                if (showDebugLog)
                    Debug.LogWarning("BackpackPanelController: 未找到InventoryController，提示器管理功能可能不可用");
            }
            else
            {
                if (showDebugLog)
                    Debug.Log("BackpackPanelController: 已找到InventoryController引用");
            }
        }
    }
    
    /// <summary>
    /// 强制初始化（用于解决生命周期时序问题）
    /// </summary>
    private void ForceInitialize()
    {
        if (isInitialized)
        {
            if (showDebugLog)
                Debug.Log("BackpackPanelController: 已经初始化，跳过强制初始化");
            return;
        }
        
        // 执行初始化逻辑
        if (gridSaveManager == null)
        {
            InitializeGridSaveManager();
        }
        
        EnsureSaveManagerExists();
        EnsureInventoryControllerExists();
        isInitialized = true;
        
        if (showDebugLog)
            Debug.Log("BackpackPanelController: 强制初始化完成");
    }
    
    #endregion
    
    #region 公共接口
    
    /// <summary>
    /// 激活面板并切换到相应网格
    /// </summary>
    /// <param name="isInWarehouse">是否在仓库中</param>
    public void ActivatePanel(bool isInWarehouse)
    {
        GridType targetType = isInWarehouse ? GridType.Storage : GridType.Ground;
        ActivatePanel(targetType);
    }
    
    /// <summary>
    /// 激活面板并切换到指定类型的网格
    /// </summary>
    /// <param name="gridType">目标网格类型</param>
    public void ActivatePanel(GridType gridType)
    {
        // 如果未初始化，强制初始化
        if (!isInitialized)
        {
            Debug.LogWarning("BackpackPanelController: 未初始化完成，强制初始化...");
            ForceInitialize();
        }
        
        if (showDebugLog)
            Debug.Log($"BackpackPanelController: 激活面板 - 网格类型: {gridType}");
        
        // 检查是否需要切换网格（避免重复打开相同网格时的不必要操作）
        bool needGridSwitch = ShouldSwitchGrid(gridType);
        
        if (needGridSwitch)
        {
            if (showDebugLog)
                Debug.Log($"BackpackPanelController: 需要切换网格到 {gridType} 模式");
            
            // 清理当前网格（不需要移动提示器）
            CleanupCurrentGrid(false);
            
            // 创建新网格
            CreateAndSetupGrid(gridType);
        }
        else
        {
            if (showDebugLog)
                Debug.Log($"BackpackPanelController: 网格已是 {gridType} 模式，无需切换，保持提示器状态不变");
            
            // 当不需要切换网格时，不要做任何可能影响提示器的操作
            // 让提示器保持当前状态，避免重复设置导致的问题
            // EnsureHighlightAvailable(); // 注释掉这行，避免重复设置
        }
        
        // 更新标题文本（无论是否切换网格都需要更新）
        UpdateTitleText(gridType);
        
        // 触发事件
        OnGridSwitchCompleted?.Invoke(gridType);
    }
    
    /// <summary>
    /// 激活货架模式（快捷方法）
    /// </summary>
    public void ActivateShelfMode()
    {
        ActivatePanel(GridType.Container);
    }
    
    /// <summary>
    /// 关闭面板
    /// </summary>
    public void DeactivatePanel()
    {
        if (showDebugLog)
            Debug.Log("BackpackPanelController: 关闭面板");
        
        // 在关闭前强制保存所有数据
        ForcesSaveAllData();
        
        // 重置提示器状态（提示器始终在InventoryController下）
        ResetHighlightState();
        
        // 清理当前网格
        CleanupCurrentGrid(true);
    }
    
    /// <summary>
    /// 获取当前网格是否为仓库网格
    /// </summary>
    /// <returns>是否为仓库网格</returns>
    public bool IsWarehouseGrid()
    {
        return currentGridType == GridType.Storage;
    }
    
    /// <summary>
    /// 获取当前网格类型
    /// </summary>
    /// <returns>当前网格类型</returns>
    public GridType GetCurrentGridType()
    {
        return currentGridType;
    }
    
    /// <summary>
    /// 检查当前是否为货架模式
    /// </summary>
    /// <returns>是否为货架模式</returns>
    public bool IsShelfMode()
    {
        return currentGridType == GridType.Container;
    }
    
    /// <summary>
    /// 公共方法：更新右侧标题文本
    /// </summary>
    /// <param name="isInWarehouse">是否在仓库中</param>
    public void UpdateRightTitle(bool isInWarehouse)
    {
        GridType targetType = isInWarehouse ? GridType.Storage : GridType.Ground;
        UpdateTitleText(targetType);
    }
    
    /// <summary>
    /// 公共方法：更新右侧标题文本（使用网格类型）
    /// </summary>
    /// <param name="gridType">网格类型</param>
    public void UpdateRightTitle(GridType gridType)
    {
        UpdateTitleText(gridType);
    }
    
    /// <summary>
    /// 公共方法：设置自定义标题文本
    /// </summary>
    /// <param name="customTitle">自定义标题</param>
    public void SetCustomTitle(string customTitle)
    {
        if (rightTitleText == null)
        {
            if (showDebugLog)
                Debug.LogWarning("BackpackPanelController: rightTitleText 未设置，无法设置自定义标题");
            return;
        }
        
        rightTitleText.text = customTitle;
        
        if (showDebugLog)
            Debug.Log($"BackpackPanelController: 已设置自定义标题为 '{customTitle}'");
    }
    
    #endregion
    
    #region 网格管理
    
    /// <summary>
    /// 创建并设置网格
    /// </summary>
    /// <param name="isInWarehouse">是否在仓库中</param>
    private void CreateAndSetupGrid(bool isInWarehouse)
    {
        GridType targetType = isInWarehouse ? GridType.Storage : GridType.Ground;
        CreateAndSetupGrid(targetType);
    }
    
    /// <summary>
    /// 创建并设置指定类型的网格
    /// </summary>
    /// <param name="gridType">网格类型</param>
    private void CreateAndSetupGrid(GridType gridType)
    {
        if (rightPanelTransform == null)
        {
            Debug.LogError("BackpackPanelController: rightPanelTransform 未设置！");
            return;
        }
        
        // 根据网格类型选择预制件
        GameObject gridPrefab = GetGridPrefab(gridType);
        
        if (gridPrefab == null)
        {
            Debug.LogError($"BackpackPanelController: {gridType}网格预制件未设置！");
            return;
        }
        
        // 实例化网格
        currentGrid = Instantiate(gridPrefab, rightPanelTransform);
        currentGridType = gridType; // 更新当前网格类型
        
        // 设置网格位置和尺寸
        SetupGridTransform(gridType);
        
        // 确保网格被激活显示
        currentGrid.SetActive(true);
        
        // 设置保存管理器并注册网格
        if (gridType == GridType.Container && ShelfTrigger.isInShelf)
        {
            // 为货架Container网格设置独立的GUID
            SetupShelfContainerGrid();
        }
        else
        {
            SetupGridSaveLoad(gridType);
        }
        
        // 记录当前网格的类型和GUID（用于清理时参考）
        currentGridType = gridType;
        
        // 更新标题文本
        UpdateTitleText(gridType);
        
        if (showDebugLog)
            Debug.Log($"BackpackPanelController: 已创建{gridType}网格 - {currentGrid.name}");
            
        // ✨ 优化：延迟随机物品生成，避免阻塞Tab键响应
        if (gridType == GridType.Container && ShelfTrigger.isInShelf)
        {
            // 延迟到下一帧执行，确保界面先显示再生成物品
            StartCoroutine(DelayedTriggerShelfRandomGeneration());
        }
    }
    
    /// <summary>
    /// 根据网格类型获取对应的预制件
    /// </summary>
    /// <param name="gridType">网格类型</param>
    /// <returns>网格预制件</returns>
    private GameObject GetGridPrefab(GridType gridType)
    {
        return gridType switch
        {
            GridType.Storage => warehouseGridPrefab,
            GridType.Ground => groundGridPrefab,
            GridType.Container => shelfGridPrefab,
            _ => groundGridPrefab
        };
    }
    
    /// <summary>
    /// 设置网格的变换组件
    /// </summary>
    /// <param name="isInWarehouse">是否为仓库网格</param>
    private void SetupGridTransform(bool isInWarehouse)
    {
        GridType targetType = isInWarehouse ? GridType.Storage : GridType.Ground;
        SetupGridTransform(targetType);
    }
    
    /// <summary>
    /// 设置指定类型网格的变换组件
    /// </summary>
    /// <param name="gridType">网格类型</param>
    private void SetupGridTransform(GridType gridType)
    {
        RectTransform gridRT = currentGrid.GetComponent<RectTransform>();
        if (gridRT == null) return;
        
        gridRT.anchorMin = new Vector2(0, 0);
        gridRT.anchorMax = new Vector2(0, 1);
        
        switch (gridType)
        {
            case GridType.Storage:
                // 仓库网格位置和尺寸
                gridRT.anchoredPosition = new Vector2(15, -52);
                gridRT.sizeDelta = new Vector2(640, 896);
                break;
                
            case GridType.Ground:
                // 地面网格位置和尺寸
                gridRT.anchoredPosition = new Vector2(15, -42);
                gridRT.sizeDelta = new Vector2(640, 512);
                break;
                
            case GridType.Container:
                // 货架网格位置和尺寸（可根据需要调整）
                gridRT.anchoredPosition = new Vector2(15, -52);
                gridRT.sizeDelta = new Vector2(640, 768);
                break;
                
            default:
                // 默认使用地面网格设置
                gridRT.anchoredPosition = new Vector2(15, -42);
                gridRT.sizeDelta = new Vector2(640, 512);
                break;
        }
    }
    
    /// <summary>
    /// 设置网格的保存和加载功能
    /// </summary>
    /// <param name="isInWarehouse">是否为仓库网格</param>
    private void SetupGridSaveLoad(bool isInWarehouse)
    {
        GridType targetType = isInWarehouse ? GridType.Storage : GridType.Ground;
        SetupGridSaveLoad(targetType);
    }
    
    /// <summary>
    /// 设置指定类型网格的保存和加载功能
    /// </summary>
    /// <param name="gridType">网格类型</param>
    private void SetupGridSaveLoad(GridType gridType)
    {
        if (currentGrid == null || gridSaveManager == null) return;

        // 获取ItemGrid组件
        ItemGrid itemGrid = currentGrid.GetComponent<ItemGrid>();
        if (itemGrid == null)
        {
            Debug.LogError("BackpackPanelController: 当前网格缺少ItemGrid组件！");
            return;
        }

        // 根据网格类型生成GUID
        string gridGUID = GenerateGridGUID(gridType, itemGrid);
        
        gridSaveManager.SetCurrentGrid(itemGrid, gridGUID);

        // 注册并加载网格数据
        bool isWarehouse = (gridType == GridType.Storage);
        gridSaveManager.RegisterAndLoadGridWithGUID(gridGUID, isWarehouse);
        
        if (showDebugLog)
            Debug.Log($"BackpackPanelController: 已设置{gridType}网格保存加载功能 - 唯一GUID: {gridGUID}");
    }
    
    /// <summary>
    /// 根据网格类型生成GUID
    /// </summary>
    /// <param name="gridType">网格类型</param>
    /// <param name="itemGrid">ItemGrid组件</param>
    /// <returns>生成的GUID</returns>
    private string GenerateGridGUID(GridType gridType, ItemGrid itemGrid)
    {
        string gridGUID = gridType switch
        {
            GridType.Storage => itemGrid.GridGUID, // 仓库使用固定GUID
            GridType.Ground => $"ground_grid_{backpackUniqueId}", // 地面使用动态GUID
            GridType.Container => GenerateContainerGUID(), // 货架使用基于活跃货架ID的GUID
            _ => $"unknown_grid_{backpackUniqueId}"
        };
        
        if (showDebugLog)
            Debug.Log($"BackpackPanelController: {gridType}网格使用GUID: {gridGUID}");
            
        return gridGUID;
    }
    
    /// <summary>
    /// 生成Container网格的专用GUID（支持货架独立存档）
    /// </summary>
    /// <returns>包含货架ID的Container网格GUID</returns>
    private string GenerateContainerGUID()
    {
        // 尝试获取当前活跃的货架ID
        string activeShelfId = GetActiveShelfId();
        
        if (!string.IsNullOrEmpty(activeShelfId))
        {
            // 使用货架ID生成独立的GUID
            string shelfGUID = $"shelf_container_{activeShelfId}_{backpackUniqueId}";
            if (showDebugLog)
                Debug.Log($"BackpackPanelController: 使用货架专用GUID: {shelfGUID} (货架: {activeShelfId})");
            return shelfGUID;
        }
        else
        {
            // 如果没有活跃的货架ID，使用通用的Container GUID
            string defaultGUID = $"container_grid_{backpackUniqueId}";
            if (showDebugLog)
                Debug.Log($"BackpackPanelController: 使用默认Container GUID: {defaultGUID} (无活跃货架)");
            return defaultGUID;
        }
    }
    
    /// <summary>
    /// 获取当前活跃的货架ID
    /// </summary>
    /// <returns>货架ID，如果没有则返回null</returns>
    private string GetActiveShelfId()
    {
        // 通过ShelfTrigger的静态状态检查是否在货架中
        if (ShelfTrigger.isInShelf)
        {
            // 查找所有ShelfTrigger，找到玩家当前所在的货架
            var allShelfTriggers = FindObjectsOfType<ShelfTrigger>();
            foreach (var trigger in allShelfTriggers)
            {
                // 检查触发器范围内是否有玩家
                if (IsPlayerInTrigger(trigger))
                {
                    return trigger.GetUniqueContainerIdentifier().Replace("shelf_container_", "");
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 检查玩家是否在指定触发器范围内
    /// </summary>
    /// <param name="trigger">要检查的触发器</param>
    /// <returns>玩家是否在范围内</returns>
    private bool IsPlayerInTrigger(ShelfTrigger trigger)
    {
        if (trigger == null)
        {
            if (showDebugLog)
                Debug.Log("BackpackPanelController: 触发器为null");
            return false;
        }
        
        // 获取玩家对象
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            if (showDebugLog)
                Debug.Log("BackpackPanelController: 未找到Player对象");
            return false;
        }
        
        // 获取触发器的碰撞体
        Collider2D triggerCollider = trigger.GetComponent<Collider2D>();
        if (triggerCollider == null)
        {
            if (showDebugLog)
                Debug.Log($"BackpackPanelController: 触发器 {trigger.name} 没有Collider2D组件");
            return false;
        }
        
        // 检查玩家是否在触发器范围内
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider == null)
        {
            if (showDebugLog)
                Debug.Log("BackpackPanelController: 玩家没有Collider2D组件");
            return false;
        }
        
        // 使用2D边界检测，忽略Z轴差异
        Bounds triggerBounds = triggerCollider.bounds;
        Bounds playerBounds = playerCollider.bounds;
        
        // 2D边界检测逻辑
        bool intersects2D = (triggerBounds.min.x <= playerBounds.max.x && triggerBounds.max.x >= playerBounds.min.x) &&
                           (triggerBounds.min.y <= playerBounds.max.y && triggerBounds.max.y >= playerBounds.min.y);
        
        if (showDebugLog)
        {
            Debug.Log($"BackpackPanelController: 触发器 {trigger.name} 2D碰撞检测 - " +
                     $"触发器边界: {triggerBounds}, 玩家边界: {playerBounds}, 2D相交: {intersects2D}");
        }
        
        return intersects2D;
    }
    
    /// <summary>
    /// 延迟触发货架随机物品生成（协程版本）
    /// 确保界面先显示，再生成物品，避免阻塞Tab键响应
    /// </summary>
    private System.Collections.IEnumerator DelayedTriggerShelfRandomGeneration()
    {
        // 等待几帧，确保界面完全显示
        yield return null;
        yield return null;
        
        if (showDebugLog)
            Debug.Log("BackpackPanelController: 开始延迟执行货架随机物品生成");
            
        // 执行原有的生成逻辑
        TriggerShelfRandomGeneration();
    }
    
    /// <summary>
    /// 触发货架随机物品生成
    /// </summary>
    private void TriggerShelfRandomGeneration()
    {
        try
        {
            // 获取当前创建的网格的ItemGrid组件
            if (currentGrid == null)
            {
                if (showDebugLog)
                    Debug.LogWarning("BackpackPanelController: 无法触发随机生成 - 当前网格为空");
                return;
            }
            
            ItemGrid itemGrid = currentGrid.GetComponent<ItemGrid>();
            if (itemGrid == null)
            {
                if (showDebugLog)
                    Debug.LogWarning("BackpackPanelController: 无法触发随机生成 - 网格没有ItemGrid组件");
                return;
            }
            
            // 查找当前活跃的ShelfTrigger
            var activeShelfTrigger = GetActiveShelfTrigger();
            if (activeShelfTrigger != null)
            {
                if (showDebugLog)
                    Debug.Log($"BackpackPanelController: 通知货架触发器生成随机物品 - {activeShelfTrigger.name}");
                    
                activeShelfTrigger.OnContainerGridCreated(itemGrid);
            }
            else
            {
                if (showDebugLog)
                    Debug.LogWarning("BackpackPanelController: 未找到活跃的货架触发器，尝试其他方法查找");
                    
                // 备用方法：查找最近分配编号的货架
                var fallbackShelfTrigger = FindFallbackShelfTrigger();
                if (fallbackShelfTrigger != null)
                {
                    if (showDebugLog)
                        Debug.Log($"BackpackPanelController: 使用备用方法找到货架触发器 - {fallbackShelfTrigger.name}");
                        
                    fallbackShelfTrigger.OnContainerGridCreated(itemGrid);
                }
                else
                {
                    if (showDebugLog)
                        Debug.LogWarning("BackpackPanelController: 备用方法也未找到货架触发器");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"BackpackPanelController: 触发随机生成时发生错误: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 备用方法：查找最近的货架触发器
    /// </summary>
    /// <returns>最近的货架触发器</returns>
    private ShelfTrigger FindFallbackShelfTrigger()
    {
        if (!ShelfTrigger.isInShelf)
        {
            if (showDebugLog)
                Debug.Log("BackpackPanelController: 玩家不在任何货架范围内，无法使用备用方法");
            return null;
        }
        
        var allShelfTriggers = FindObjectsOfType<ShelfTrigger>();
        ShelfTrigger closestTrigger = null;
        float closestDistance = float.MaxValue;
        
        // 获取玩家位置
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            if (showDebugLog)
                Debug.LogWarning("BackpackPanelController: 备用方法无法找到玩家对象");
            return null;
        }
        
        Vector3 playerPosition = player.transform.position;
        
        foreach (var trigger in allShelfTriggers)
        {
            // 检查是否有分配的货架编号
            if (!string.IsNullOrEmpty(trigger.AssignedShelfId))
            {
                // 计算距离（只考虑XY平面）
                Vector3 triggerPosition = trigger.transform.position;
                float distance = Vector2.Distance(
                    new Vector2(playerPosition.x, playerPosition.y),
                    new Vector2(triggerPosition.x, triggerPosition.y)
                );
                
                if (showDebugLog)
                    Debug.Log($"BackpackPanelController: 检查货架 {trigger.name} (编号: {trigger.AssignedShelfId}), 距离: {distance:F2}");
                
                // 选择最近的货架
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTrigger = trigger;
                }
            }
        }
        
        if (closestTrigger != null && showDebugLog)
        {
            Debug.Log($"BackpackPanelController: 备用方法选择最近货架: {closestTrigger.name} (编号: {closestTrigger.AssignedShelfId}), 距离: {closestDistance:F2}");
        }
        
        return closestTrigger;
    }
    
    /// <summary>
    /// 为货架生成独立的Container GUID
    /// </summary>
    /// <param name="shelfTrigger">货架触发器</param>
    /// <returns>独立的GUID</returns>
    private string GenerateShelfContainerGUID(ShelfTrigger shelfTrigger)
    {
        if (shelfTrigger != null && !string.IsNullOrEmpty(shelfTrigger.AssignedShelfId))
        {
            // 使用货架的AssignedShelfId作为唯一标识符
            // 格式：shelf_container_{货架编号}_{场景名称}
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            string shelfGUID = $"shelf_container_{shelfTrigger.AssignedShelfId}_{sceneName}";
            
            if (showDebugLog)
                Debug.Log($"BackpackPanelController: 为货架生成独立GUID: {shelfGUID} (货架: {shelfTrigger.AssignedShelfId}, 场景: {sceneName})");
            return shelfGUID;
        }
        else
        {
            // 回退到场景级别的默认GUID
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            string defaultGUID = $"container_grid_default_{sceneName}_{backpackUniqueId}";
            
            if (showDebugLog)
                Debug.Log($"BackpackPanelController: 使用默认Container GUID: {defaultGUID} (无活跃货架)");
            return defaultGUID;
        }
    }
    
    /// <summary>
    /// 为货架Container网格设置独立的保存和加载
    /// </summary>
    private void SetupShelfContainerGrid()
    {
        try
        {
            // 获取活跃的货架触发器
            var activeShelfTrigger = GetActiveShelfTrigger();
            if (activeShelfTrigger == null)
            {
                activeShelfTrigger = FindFallbackShelfTrigger();
            }
            
            // 生成独立的GUID
            string shelfGUID = GenerateShelfContainerGUID(activeShelfTrigger);
            
            if (showDebugLog)
                Debug.Log($"BackpackPanelController: Container网格使用GUID: {shelfGUID}");
            
            // 会话重置（仅一次）：首次使用该GUID时清理旧会话存档，后续同一会话内不再清理
            if (gridSaveManager != null)
            {
                bool deleted = gridSaveManager.EnsureSessionClearOnce(shelfGUID);
                if (showDebugLog)
                    Debug.Log($"BackpackPanelController: 会话一次性清理旧Container存档 -> {(deleted ? "已删除" : "跳过/无旧档")}: {shelfGUID}");
            }

            // 立即设置自定义GUID，确保它被正确应用
            SetupGridWithCustomGUID(GridType.Container, shelfGUID);
            
            // 额外验证：确保网格确实使用了正确的GUID
            if (currentGrid != null)
            {
                ItemGrid itemGrid = currentGrid.GetComponent<ItemGrid>();
                if (itemGrid != null && showDebugLog)
                {
                    Debug.Log($"BackpackPanelController: 验证网格GUID设置 - 网格名称: {itemGrid.name}");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"BackpackPanelController: 设置货架Container网格时发生错误: {ex.Message}");
            // 发生错误时回退到默认设置
            SetupGridSaveLoad(GridType.Container);
        }
    }
    
    /// <summary>
    /// 使用自定义GUID设置网格
    /// </summary>
    /// <param name="gridType">网格类型</param>
    /// <param name="customGUID">自定义GUID</param>
    private void SetupGridWithCustomGUID(GridType gridType, string customGUID)
    {
        try
        {
            // 直接设置自定义GUID，而不是使用默认的
            string gridGUID = customGUID;
            
            if (showDebugLog)
                Debug.Log($"BackpackPanelController: 设置{gridType}网格使用自定义GUID: {gridGUID}");
            
            // 获取网格组件
            ItemGrid itemGrid = currentGrid?.GetComponent<ItemGrid>();
            if (itemGrid != null)
            {
                // 直接设置网格的GUID和属性
                itemGrid.GridGUID = gridGUID;
                itemGrid.GridName = $"地面网格 ({gridGUID})";
                itemGrid.GridType = GridType.Ground; // Container网格在保存系统中作为Ground处理
                
                // 使用GridSaveManager设置
                if (gridSaveManager != null)
                {
                    gridSaveManager.SetCurrentGrid(itemGrid, gridGUID);
                    if (showDebugLog)
                        Debug.Log($"BackpackPanelController: 已设置GridSaveManager当前网格: {gridGUID}");
                }
                
                // 注册到InventorySaveManager（使用正确的方法签名）
                if (InventorySaveManager.Instance != null)
                {
                    InventorySaveManager.Instance.RegisterGrid(itemGrid, itemGrid.GridName);
                    if (showDebugLog)
                        Debug.Log($"BackpackPanelController: 已向InventorySaveManager注册网格: {gridGUID}");
                }
                
                // 使用GridSaveManager加载数据
                if (gridSaveManager != null)
                {
                    // 根据gridType判断是否为仓库模式
                    bool isWarehouse = (gridType == GridType.Storage);
                    gridSaveManager.RegisterAndLoadGridWithGUID(gridGUID, isWarehouse);
                    if (showDebugLog)
                        Debug.Log($"BackpackPanelController: 已尝试加载网格数据: {gridGUID}");
                }
            }
            else
            {
                if (showDebugLog)
                    Debug.LogWarning($"BackpackPanelController: 无法获取ItemGrid组件，无法设置自定义GUID: {gridGUID}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"BackpackPanelController: 设置自定义GUID时发生错误: {ex.Message}");
            // 回退到默认设置
            SetupGridSaveLoad(gridType);
        }
    }
    
    /// <summary>
    /// 获取当前活跃的货架触发器
    /// </summary>
    /// <returns>活跃的货架触发器，如果没有则返回null</returns>
    private ShelfTrigger GetActiveShelfTrigger()
    {
        if (showDebugLog)
            Debug.Log($"BackpackPanelController: 检查活跃货架触发器 - isInShelf: {ShelfTrigger.isInShelf}");
            
        if (!ShelfTrigger.isInShelf)
        {
            if (showDebugLog)
                Debug.Log("BackpackPanelController: 玩家不在货架范围内");
            return null;
        }
            
        // 查找所有ShelfTrigger，找到玩家当前所在的货架
        var allShelfTriggers = FindObjectsOfType<ShelfTrigger>();
        if (showDebugLog)
            Debug.Log($"BackpackPanelController: 找到 {allShelfTriggers.Length} 个货架触发器");
            
        foreach (var trigger in allShelfTriggers)
        {
            if (showDebugLog)
                Debug.Log($"BackpackPanelController: 检查货架触发器 {trigger.name}");
                
            // 检查触发器范围内是否有玩家
            if (IsPlayerInTrigger(trigger))
            {
                if (showDebugLog)
                    Debug.Log($"BackpackPanelController: 找到活跃货架触发器: {trigger.name}");
                return trigger;
            }
            else
            {
                if (showDebugLog)
                    Debug.Log($"BackpackPanelController: 货架触发器 {trigger.name} 中没有玩家");
            }
        }
        
        if (showDebugLog)
            Debug.Log("BackpackPanelController: 未找到任何活跃的货架触发器");
        return null;
    }
    
    /// <summary>
    /// 更新标题文本
    /// </summary>
    /// <param name="isInWarehouse">是否为仓库模式</param>
    private void UpdateTitleText(bool isInWarehouse)
    {
        GridType targetType = isInWarehouse ? GridType.Storage : GridType.Ground;
        UpdateTitleText(targetType);
    }
    
    /// <summary>
    /// 更新指定类型的标题文本
    /// </summary>
    /// <param name="gridType">网格类型</param>
    private void UpdateTitleText(GridType gridType)
    {
        if (rightTitleText == null)
        {
            if (showDebugLog)
                Debug.LogWarning("BackpackPanelController: rightTitleText 未设置，无法更新标题文本");
            return;
        }
        
        string newTitle = gridType switch
        {
            GridType.Storage => warehouseTitleText,
            GridType.Ground => groundTitleText,
            GridType.Container => shelfTitleText,
            _ => groundTitleText
        };
        
        rightTitleText.text = newTitle;
        
        if (showDebugLog)
            Debug.Log($"BackpackPanelController: 已更新标题文本为 '{newTitle}' (网格类型: {gridType})");
    }
    
    /// <summary>
    /// 清理当前网格
    /// </summary>
    /// <param name="resetHighlight">是否需要重置提示器状态</param>
    private void CleanupCurrentGrid(bool resetHighlight = true)
    {
        // 🔥 关键步骤：在销毁网格前，强制将提示器返回到InventoryController
        ForceReturnHighlightBeforeGridDestroy();
        
        // 重置提示器状态（提示器始终保持在InventoryController下）
        if (resetHighlight)
        {
            ResetHighlightState();
        }
        
        // 使用GridSaveManager清理并保存
        if (gridSaveManager != null)
        {
            gridSaveManager.CleanupAndSave(true); // 强制保存
            
            if (showDebugLog)
                Debug.Log("BackpackPanelController: 已保存并清理网格数据");
        }

        // 销毁游戏对象
        if (currentGrid != null)
        {
            Destroy(currentGrid);
            currentGrid = null;
            
            if (showDebugLog)
                Debug.Log("BackpackPanelController: 已销毁网格GameObject");
        }
    }
    
    /// <summary>
    /// 检查是否需要切换网格
    /// </summary>
    /// <param name="isInWarehouse">目标是否为仓库网格</param>
    /// <returns>是否需要切换网格</returns>
    private bool ShouldSwitchGrid(bool isInWarehouse)
    {
        GridType targetType = isInWarehouse ? GridType.Storage : GridType.Ground;
        return ShouldSwitchGrid(targetType);
    }
    
    /// <summary>
    /// 检查是否需要切换到指定类型的网格
    /// </summary>
    /// <param name="targetGridType">目标网格类型</param>
    /// <returns>是否需要切换网格</returns>
    private bool ShouldSwitchGrid(GridType targetGridType)
    {
        // 如果当前没有网格，需要创建
        if (currentGrid == null)
        {
            return true;
        }
        
        // 检查当前网格类型是否与目标类型匹配
        return currentGridType != targetGridType;
    }
    
    /// <summary>
    /// 确保高亮提示器可用
    /// 当不需要切换网格时调用，确保提示器正确设置
    /// </summary>
    private void EnsureHighlightAvailable()
    {
        if (inventoryController == null)
        {
            EnsureInventoryControllerExists();
        }
        
        if (inventoryController != null && inventoryController.IsHighlightAvailable())
        {
            // 获取当前网格的ItemGrid组件
            if (currentGrid != null)
            {
                ItemGrid itemGrid = currentGrid.GetComponent<ItemGrid>();
                if (itemGrid != null)
                {
                    // 确保InventoryController知道当前的选中网格
                    inventoryController.SetSelectedItemGrid(itemGrid);
                    
                    if (showDebugLog)
                        Debug.Log("BackpackPanelController: 已确保提示器可用并设置选中网格");
                }
            }
        }
        else
        {
            if (showDebugLog)
                Debug.LogWarning("BackpackPanelController: 无法确保提示器可用");
        }
    }
    
    #endregion
    
    #region 提示器管理
    
    /// <summary>
    /// 在网格销毁前强制将提示器返回到InventoryController
    /// 这是解决提示器随网格销毁而丢失的核心方法
    /// </summary>
    private void ForceReturnHighlightBeforeGridDestroy()
    {
        if (showDebugLog)
            Debug.Log("BackpackPanelController: 开始强制回收提示器流程");
        
        if (inventoryController == null)
        {
            EnsureInventoryControllerExists();
        }
        
        if (inventoryController == null)
        {
            if (showDebugLog)
                Debug.LogWarning("BackpackPanelController: 无法找到InventoryController，跳过提示器回收");
            return;
        }
        
        if (!inventoryController.IsHighlightAvailable())
        {
            if (showDebugLog)
                Debug.LogWarning("BackpackPanelController: InventoryController的提示器不可用，跳过提示器回收");
            return;
        }
        
        // 获取提示器的当前状态信息
        var highlight = inventoryController.GetHighlightComponent();
        if (highlight != null)
        {
            string currentParent = highlight.transform.parent?.name ?? "null";
            if (showDebugLog)
                Debug.Log($"BackpackPanelController: 提示器当前父级: {currentParent}");
        }
        
        // 调用InventoryController的强制回收方法
        inventoryController.ForceReturnHighlightToController();
        
        // 验证回收结果
        if (highlight != null)
        {
            string newParent = highlight.transform.parent?.name ?? "null";
            if (showDebugLog)
                Debug.Log($"BackpackPanelController: 提示器回收后父级: {newParent}");
        }
        
        if (showDebugLog)
            Debug.Log("BackpackPanelController: 已在网格销毁前强制回收提示器");
    }
    
    /// <summary>
    /// 重置高亮提示器状态
    /// 简化版本 - 提示器始终保持在InventoryController下，只需重置状态
    /// </summary>
    private void ResetHighlightState()
    {
        if (inventoryController == null)
        {
            // 尝试重新查找InventoryController
            EnsureInventoryControllerExists();
            
            if (inventoryController == null)
            {
                if (showDebugLog)
                    Debug.LogWarning("BackpackPanelController: 无法找到InventoryController，跳过提示器重置");
                return;
            }
        }
        
        // 检查InventoryController是否有提示器可用
        if (!inventoryController.IsHighlightAvailable())
        {
            if (showDebugLog)
                Debug.LogWarning("BackpackPanelController: InventoryController的提示器不可用，跳过提示器重置");
            return;
        }
        
        // 调用InventoryController的方法重置提示器状态
        inventoryController.ResetHighlight();
        
        if (showDebugLog)
            Debug.Log("BackpackPanelController: 已重置提示器状态");
    }
    
    #endregion
    

    
    #region 编辑器支持
    
    #if UNITY_EDITOR
    /// <summary>
    /// 验证组件设置
    /// </summary>
    private void OnValidate()
    {
        // 自动查找rightPanelTransform
        if (rightPanelTransform == null)
        {
            // 查找名为 "BackPackRight" 的RectTransform
            RectTransform[] rectTransforms = GetComponentsInChildren<RectTransform>();
            foreach (var rt in rectTransforms)
            {
                if (rt.gameObject.name == "BackPackRight")
                {
                    rightPanelTransform = rt;
                    Debug.Log("BackpackPanelController: 自动找到rightPanelTransform");
                    break;
                }
            }
        }
        
        // 自动查找rightTitleText
        if (rightTitleText == null)
        {
            // 查找名为 "Right" 的TextMeshProUGUI组件
            TextMeshProUGUI[] textComponents = GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var text in textComponents)
            {
                if (text.gameObject.name == "Right")
                {
                    rightTitleText = text;
                    Debug.Log("BackpackPanelController: 自动找到rightTitleText");
                    break;
                }
            }
        }
    }
    #endif
    
    #endregion
    
    #region 背包ID管理
    
    /// <summary>
    /// 获取当前背包的唯一ID
    /// </summary>
    public string GetBackpackUniqueId()
    {
        return backpackUniqueId;
    }
    
    /// <summary>
    /// 手动设置背包唯一ID（仅在初始化前有效）
    /// </summary>
    /// <param name="newId">新的背包ID</param>
    public void SetBackpackUniqueId(string newId)
    {
        if (isInitialized)
        {
            Debug.LogWarning("BackpackPanelController: 背包已初始化，无法更改ID");
            return;
        }
        
        if (string.IsNullOrEmpty(newId))
        {
            Debug.LogWarning("BackpackPanelController: 背包ID不能为空");
            return;
        }
        
        backpackUniqueId = newId;
        if (showDebugLog)
            Debug.Log($"BackpackPanelController: 手动设置背包ID为: {backpackUniqueId}");
    }
    
    /// <summary>
    /// 重新生成背包ID（用于调试）
    /// </summary>
    [ContextMenu("重新生成背包ID")]
    public void RegenerateBackpackId()
    {
        string oldId = backpackUniqueId;
        backpackUniqueId = "";
        InitializeBackpackId();
        
        Debug.Log($"BackpackPanelController: 背包ID已从 '{oldId}' 重新生成为 '{backpackUniqueId}'");
        
        if (isInitialized)
        {
            Debug.LogWarning("注意：背包已初始化，新ID将在下次重启后生效");
        }
    }
    
    #endregion
    
    #region 强制保存机制
    
    /// <summary>
    /// 强制保存所有数据（在面板关闭时调用）
    /// </summary>
    private void ForcesSaveAllData()
    {
        if (showDebugLog)
            Debug.Log("BackpackPanelController: 开始强制保存所有数据");
            
        try
        {
            // 保存当前激活的网格（地面或仓库）
            SaveCurrentGrid();
            
            // 保存所有装备栏
            SaveAllEquipmentSlots();
            
            if (showDebugLog)
                Debug.Log("BackpackPanelController: 强制保存完成");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"BackpackPanelController: 强制保存时发生错误: {e.Message}");
        }
    }
    
    /// <summary>
    /// 保存当前激活的网格
    /// </summary>
    private void SaveCurrentGrid()
    {
        if (currentGrid == null)
        {
            if (showDebugLog)
                Debug.Log("BackpackPanelController: 没有激活的网格需要保存");
            return;
        }
        
        // 获取网格中的ItemGrid组件
        var itemGrid = currentGrid.GetComponentInChildren<ItemGrid>();
        if (itemGrid == null)
        {
            Debug.LogWarning("BackpackPanelController: 当前网格没有ItemGrid组件");
            return;
        }
        
        // 通过GridSaveManager保存网格数据
        if (gridSaveManager != null)
        {
            gridSaveManager.ForceSaveCurrentGrid();
            
            if (showDebugLog)
            {
                bool isWarehouse = IsWarehouseGrid();
                string gridType = isWarehouse ? "仓库" : "地面";
                Debug.Log($"BackpackPanelController: 已强制保存{gridType}网格数据");
            }
        }
        else
        {
            Debug.LogWarning("BackpackPanelController: GridSaveManager为空，无法保存网格数据");
        }
    }
    
    /// <summary>
    /// 保存所有装备栏
    /// </summary>
    private void SaveAllEquipmentSlots()
    {
        try
        {
            // 使用传统方法保存装备数据
            var equipmentSlots = GetComponentsInChildren<InventorySystem.EquipmentSlot>(true);
            
            if (equipmentSlots == null || equipmentSlots.Length == 0)
            {
                if (showDebugLog)
                    Debug.Log("BackpackPanelController: 没有找到装备栏需要保存");
                return;
            }
            
            // 🔧 装备保存现在由EquipmentPersistenceManager统一处理
            var equipmentPersistenceManager = EquipmentPersistenceManager.Instance;
            if (equipmentPersistenceManager != null)
            {
                equipmentPersistenceManager.SaveEquipmentData();
                Debug.Log("BackpackPanelController: 已通过EquipmentPersistenceManager强制保存装备数据");
            }
            else
            {
                Debug.LogWarning("BackpackPanelController: 无法找到EquipmentPersistenceManager，装备数据可能未保存");
            }
            
            if (showDebugLog)
                Debug.Log($"BackpackPanelController: 已强制保存 {equipmentSlots.Length} 个装备栏数据");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"BackpackPanelController: 保存装备栏时发生错误: {e.Message}");
        }
    }
    

    
    /// <summary>
    /// 手动触发强制保存（用于调试）
    /// </summary>
    [ContextMenu("强制保存所有数据")]
    public void ManualForceSave()
    {
        ForcesSaveAllData();
        Debug.Log("BackpackPanelController: 手动强制保存完成");
    }
    
    /// <summary>
    /// 测试提示器保护机制（用于调试）
    /// </summary>
    [ContextMenu("测试提示器保护")]
    public void TestHighlightProtection()
    {
        try
        {
            ForceReturnHighlightBeforeGridDestroy();
            ResetHighlightState();
            Debug.Log("BackpackPanelController: 提示器保护机制测试完成");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"BackpackPanelController: 提示器保护机制测试失败: {e.Message}");
        }
    }
    
    #endregion
    
    #region 装备槽检测和管理
    
    /// <summary>
    /// 延迟检测和注册装备槽
    /// </summary>
    /// <returns></returns>
    private IEnumerator DetectAndRegisterEquipmentSlotsDelayed()
    {
        // 等待一帧，确保背包面板完全激活
        yield return null;
        
        if (showDebugLog)
            Debug.Log("BackpackPanelController: 开始检测背包面板中的装备槽组件");
        
        // 检测装备槽管理器
        var equipmentManager = EquipmentSlotManager.Instance;
        if (equipmentManager == null)
        {
            Debug.LogWarning("BackpackPanelController: 装备槽管理器不存在，尝试查找");
            equipmentManager = FindObjectOfType<EquipmentSlotManager>();
            if (equipmentManager == null)
            {
                Debug.LogError("BackpackPanelController: 无法找到装备槽管理器");
                yield break;
            }
        }
        
        // 触发装备槽检测
        Debug.Log("BackpackPanelController: 触发装备槽检测");
        equipmentManager.TriggerSlotDetection();
        
        // 等待一帧让注册完成
        yield return null;
        
        // 检查注册结果
        var allSlots = equipmentManager.GetAllEquipmentSlots();
        Debug.Log($"BackpackPanelController: 装备槽检测完成，共注册 {allSlots.Count} 个装备槽");
        
        // 详细显示注册的装备槽，并确保激活状态
        foreach (var kvp in allSlots)
        {
            Debug.Log($"BackpackPanelController: 已注册装备槽: {kvp.Key} -> {kvp.Value.name}");
            
            // 🔧 确保装备槽被激活，以便触发容器内容加载
            if (!kvp.Value.gameObject.activeInHierarchy)
            {
                kvp.Value.gameObject.SetActive(true);
                if (showDebugLog)
                    Debug.Log($"BackpackPanelController: 激活装备槽 {kvp.Key} 以触发容器内容加载");
            }
        }
        
        // 自动保存管理器已移除，使用传统保存方法
        Debug.Log("BackpackPanelController: 装备槽检测完成，使用传统保存方法");
        
        // 尝试加载装备数据
        yield return StartCoroutine(LoadEquipmentDataDelayed());
    }
    
    /// <summary>
    /// 延迟加载装备数据
    /// </summary>
    /// <returns></returns>
    private IEnumerator LoadEquipmentDataDelayed()
    {
        // 等待几帧，确保所有装备槽都已正确注册
        for (int i = 0; i < 3; i++)
        {
            yield return null;
        }
        
        Debug.Log("BackpackPanelController: 开始加载装备数据");
        
        // 🔧 装备加载现在由EquipmentPersistenceManager统一处理
        // 不再需要手动调用加载，避免与新系统冲突
        Debug.Log("BackpackPanelController: 装备加载已委托给EquipmentPersistenceManager");
    }
    
    /// <summary>
    /// 手动触发装备槽检测（用于调试）
    /// </summary>
    [ContextMenu("手动检测装备槽")]
    public void ManualDetectEquipmentSlots()
    {
        StartCoroutine(DetectAndRegisterEquipmentSlotsDelayed());
    }
    
    #endregion
}
