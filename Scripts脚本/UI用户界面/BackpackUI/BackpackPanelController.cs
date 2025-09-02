using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// BackpackPanel 内部控制器 - 专门负责管理面板内的网格切换逻辑
/// </summary>
public class BackpackPanelController : MonoBehaviour
{
    [Header("网格预制件设置")]
    [SerializeField] private GameObject warehouseGridPrefab;
    [SerializeField] private GameObject groundGridPrefab;
    
    [Header("面板引用")]
    [SerializeField] private RectTransform rightPanelTransform; // 网格的父容器
    [SerializeField] private TextMeshProUGUI rightTitleText; // 右侧标题文本组件
    
    [Header("标题文本设置")]
    [SerializeField] private string warehouseTitleText = "Storage"; // 仓库模式显示的文本
    [SerializeField] private string groundTitleText = "Ground"; // 地面模式显示的文本
    
    [Header("调试设置")]
    [SerializeField] private bool showDebugLog = true;
    
    // 当前网格状态
    private GameObject currentGrid;
    private GridSaveManager gridSaveManager;
    private bool isInitialized = false;
    
    // 事件：当网格切换完成时触发
    public System.Action<bool> OnGridSwitchCompleted; // bool: isWarehouse
    
    #region 初始化
    
    private void Awake()
    {
        // 在Awake中完成核心初始化，确保更早执行
        InitializeGridSaveManager();
        EnsureSaveManagerExists();
        isInitialized = true;
        
        if (showDebugLog)
            Debug.Log("BackpackPanelController: Awake初始化完成");
    }
    
    private void Start()
    {
        // Start中再次确认初始化状态
        if (!isInitialized)
        {
            Debug.LogWarning("BackpackPanelController: Start时发现未初始化，执行补充初始化");
            ForceInitialize();
        }
        
        if (showDebugLog)
            Debug.Log("BackpackPanelController: Start验证完成");
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
            saveManager.AddComponent<InventorySaveManager>();
            
            if (showDebugLog)
                Debug.Log("BackpackPanelController: 已创建InventorySaveManager实例");
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
        // 如果未初始化，强制初始化
        if (!isInitialized)
        {
            Debug.LogWarning("BackpackPanelController: 未初始化完成，强制初始化...");
            ForceInitialize();
        }
        
        if (showDebugLog)
            Debug.Log($"BackpackPanelController: 激活面板 - 仓库模式: {isInWarehouse}");
        
        // 清理当前网格
        CleanupCurrentGrid();
        
        // 创建新网格
        CreateAndSetupGrid(isInWarehouse);
        
        // 触发事件
        OnGridSwitchCompleted?.Invoke(isInWarehouse);
    }
    
    /// <summary>
    /// 关闭面板
    /// </summary>
    public void DeactivatePanel()
    {
        if (showDebugLog)
            Debug.Log("BackpackPanelController: 关闭面板");
        
        // 清理当前网格
        CleanupCurrentGrid();
    }
    
    /// <summary>
    /// 获取当前网格是否为仓库网格
    /// </summary>
    /// <returns>是否为仓库网格</returns>
    public bool IsWarehouseGrid()
    {
        if (currentGrid == null) return false;
        return currentGrid.name.Contains("Warehouse");
    }
    
    /// <summary>
    /// 公共方法：更新右侧标题文本
    /// </summary>
    /// <param name="isInWarehouse">是否在仓库中</param>
    public void UpdateRightTitle(bool isInWarehouse)
    {
        UpdateTitleText(isInWarehouse);
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
        if (rightPanelTransform == null)
        {
            Debug.LogError("BackpackPanelController: rightPanelTransform 未设置！");
            return;
        }
        
        // 根据是否在仓库内选择不同的预制件
        GameObject gridPrefab = isInWarehouse ? warehouseGridPrefab : groundGridPrefab;
        
        if (gridPrefab == null)
        {
            Debug.LogError($"BackpackPanelController: {(isInWarehouse ? "仓库" : "地面")}网格预制件未设置！");
            return;
        }
        
        // 实例化网格
        currentGrid = Instantiate(gridPrefab, rightPanelTransform);
        
        // 设置网格位置和尺寸
        SetupGridTransform(isInWarehouse);
        
        // 确保网格被激活显示
        currentGrid.SetActive(true);
        
        // 设置保存管理器并注册网格
        SetupGridSaveLoad(isInWarehouse);
        
        // 更新标题文本
        UpdateTitleText(isInWarehouse);
        
        if (showDebugLog)
            Debug.Log($"BackpackPanelController: 已创建{(isInWarehouse ? "仓库" : "地面")}网格 - {currentGrid.name}");
    }
    
    /// <summary>
    /// 设置网格的变换组件
    /// </summary>
    /// <param name="isInWarehouse">是否为仓库网格</param>
    private void SetupGridTransform(bool isInWarehouse)
    {
        RectTransform gridRT = currentGrid.GetComponent<RectTransform>();
        if (gridRT == null) return;
        
        gridRT.anchorMin = new Vector2(0, 0);
        gridRT.anchorMax = new Vector2(0, 1);
        
        if (isInWarehouse)
        {
            // 仓库网格位置和尺寸
            gridRT.anchoredPosition = new Vector2(15, -52);
            gridRT.sizeDelta = new Vector2(640, 896);
        }
        else
        {
            // 地面网格位置和尺寸
            gridRT.anchoredPosition = new Vector2(15, -42);
            gridRT.sizeDelta = new Vector2(640, 512);
        }
    }
    
    /// <summary>
    /// 设置网格的保存和加载功能
    /// </summary>
    /// <param name="isInWarehouse">是否为仓库网格</param>
    private void SetupGridSaveLoad(bool isInWarehouse)
    {
        if (currentGrid == null || gridSaveManager == null) return;

        // 获取ItemGrid组件
        ItemGrid itemGrid = currentGrid.GetComponent<ItemGrid>();
        if (itemGrid == null)
        {
            Debug.LogError("BackpackPanelController: 当前网格缺少ItemGrid组件！");
            return;
        }

        // 设置网格到保存管理器
        string gridGUID = isInWarehouse ? "warehouse_grid_main" : "ground_grid_main";
        gridSaveManager.SetCurrentGrid(itemGrid, gridGUID);

        // 注册并加载网格数据
        gridSaveManager.RegisterAndLoadGrid(isInWarehouse);
        
        if (showDebugLog)
            Debug.Log($"BackpackPanelController: 已设置网格保存加载功能 - GUID: {gridGUID}");
    }
    
    /// <summary>
    /// 更新标题文本
    /// </summary>
    /// <param name="isInWarehouse">是否为仓库模式</param>
    private void UpdateTitleText(bool isInWarehouse)
    {
        if (rightTitleText == null)
        {
            if (showDebugLog)
                Debug.LogWarning("BackpackPanelController: rightTitleText 未设置，无法更新标题文本");
            return;
        }
        
        string newTitle = isInWarehouse ? warehouseTitleText : groundTitleText;
        rightTitleText.text = newTitle;
        
        if (showDebugLog)
            Debug.Log($"BackpackPanelController: 已更新标题文本为 '{newTitle}'");
    }
    
    /// <summary>
    /// 清理当前网格
    /// </summary>
    private void CleanupCurrentGrid()
    {
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
    
    #endregion
    
    #region 生命周期
    
    private void OnDestroy()
    {
        // 确保在销毁前清理资源
        CleanupCurrentGrid();
        
        if (showDebugLog)
            Debug.Log("BackpackPanelController: 组件已销毁");
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
}
