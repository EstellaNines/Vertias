using UnityEngine;
using UnityEngine.EventSystems;

public class GridInteract : MonoBehaviour
{
    public enum GridType
    {
        MainGrid,
        BackpackGrid,
        TacticalRigGrid
    }

    [Header("网格优先级 (数值越小优先级越高)")]
    public int gridPriority = 0;

    private InventoryController inventoryController;
    private ItemGrid itemGrid;
    private BackpackItemGrid backpackGrid;
    private TactiaclRigItemGrid tacticalRigGrid;
    private RectTransform rectTransform;
    private Canvas canvas;
    private bool wasInGrid = false;
    private GridType currentGridType;

    // 静态变量用于跟踪当前活跃的网格
    private static GridInteract currentActiveGridInteract;

    private void Awake()
    {
        inventoryController = FindObjectOfType<InventoryController>();

        // 检测当前GameObject上的网格类型
        itemGrid = GetComponent<ItemGrid>();
        backpackGrid = GetComponent<BackpackItemGrid>();
        tacticalRigGrid = GetComponent<TactiaclRigItemGrid>();

        // 确定网格类型和默认优先级
        if (itemGrid != null)
        {
            currentGridType = GridType.MainGrid;
            if (gridPriority == 0) gridPriority = 1; // 主网格默认优先级
        }
        else if (backpackGrid != null)
        {
            currentGridType = GridType.BackpackGrid;
            if (gridPriority == 0) gridPriority = 2; // 背包网格默认优先级
        }
        else if (tacticalRigGrid != null)
        {
            currentGridType = GridType.TacticalRigGrid;
            if (gridPriority == 0) gridPriority = 3; // 战术挂具默认优先级
        }
        else
        {
            Debug.LogError($"GridInteract on {gameObject.name} requires one of: ItemGrid, BackpackItemGrid, or TactiaclRigItemGrid component!");
            enabled = false;
            return;
        }

        rectTransform = GetComponent<RectTransform>();
        canvas = FindObjectOfType<Canvas>();

        // 初始化网格状态保存系统
        InitializeGridStateSaveSystem();

        Debug.Log($"GridInteract initialized on {gameObject.name} as {currentGridType} with priority {gridPriority}");
    }

    private void Update()
    {
        if (inventoryController == null) return;

        Vector2 mousePosition = Input.mousePosition;
        bool isInGrid = IsMouseInGridBounds(mousePosition);

        if (isInGrid != wasInGrid)
        {
            if (isInGrid)
            {
                // 检查是否有更高优先级的网格已经激活
                if (currentActiveGridInteract == null || gridPriority < currentActiveGridInteract.gridPriority)
                {
                    // 清除之前的选择
                    if (currentActiveGridInteract != null)
                    {
                        currentActiveGridInteract.ForceExit();
                    }

                    currentActiveGridInteract = this;
                    SetAsSelectedGrid();
                    Debug.Log($"鼠标进入{currentGridType}网格: {gameObject.name} (优先级: {gridPriority})");
                }
            }
            else
            {
                if (currentActiveGridInteract == this)
                {
                    ClearIfSelected();
                    currentActiveGridInteract = null;
                    Debug.Log($"鼠标离开{currentGridType}网格: {gameObject.name}");
                }
            }

            wasInGrid = isInGrid;
        }
    }

    private void ForceExit()
    {
        wasInGrid = false;
        ClearIfSelected();
    }

    private void SetAsSelectedGrid()
    {
        // 首先清除所有网格选择并重置活跃网格类型
        inventoryController.ClearSelectedGrid();

        // 然后设置当前网格
        switch (currentGridType)
        {
            case GridType.MainGrid:
                inventoryController.SetSelectedMainGrid(itemGrid);
                break;
            case GridType.BackpackGrid:
                inventoryController.SetSelectedBackpackGrid(backpackGrid);
                break;
            case GridType.TacticalRigGrid:
                inventoryController.SetSelectedTacticalRigGrid(tacticalRigGrid);
                break;
        }
    }

    private void ClearIfSelected()
    {
        switch (currentGridType)
        {
            case GridType.MainGrid:
                if (inventoryController.selectedItemGrid == itemGrid)
                {
                    inventoryController.selectedItemGrid = null;
                }
                break;
            case GridType.BackpackGrid:
                if (inventoryController.selectedBackpackGrid == backpackGrid)
                {
                    inventoryController.selectedBackpackGrid = null;
                }
                break;
            case GridType.TacticalRigGrid:
                if (inventoryController.selectedTacticalRigGrid == tacticalRigGrid)
                {
                    inventoryController.selectedTacticalRigGrid = null;
                }
                break;
        }
    }

    private bool IsMouseInGridBounds(Vector2 mousePosition)
    {
        if (rectTransform == null || canvas == null) return false;

        Vector2 localMousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            mousePosition,
            canvas.worldCamera,
            out localMousePosition
        );

        Rect gridRect = rectTransform.rect;
        return gridRect.Contains(localMousePosition);
    }

    public bool IsMouseInThisGrid()
    {
        return IsMouseInGridBounds(Input.mousePosition);
    }

    public GridType GetCurrentGridType()
    {
        return currentGridType;
    }

    // 静态方法用于清除所有网格选择
    public static void ClearAllGridSelections()
    {
        currentActiveGridInteract = null;
    }

    // ==================== 网格状态保存扩展接口 ====================

    [System.Serializable]
    public class GridInteractSaveData
    {
        public string gridID;
        public GridType gridType;
        public int gridPriority;
        public bool isActive;
        public bool wasInGrid;
        public Vector2 gridPosition;
        public Vector2 gridSize;
        public string gridName;
        public bool isSelected;
        public string lastInteractionTime;
        public int interactionCount;
        public string lastModified;
        public int saveVersion;
    }

    [Header("网格状态保存设置")]
    [SerializeField] private string gridInteractID = "";
    [SerializeField] private bool autoGenerateID = true;
    [SerializeField] private int saveVersion = 1;
    [SerializeField] private bool enableStatePersistence = true;

    // 网格状态保存事件
    public System.Action<string> OnGridStateSaved;
    public System.Action<string> OnGridStateLoaded;
    public System.Action<string, string> OnGridStateError;

    // 交互状态跟踪
    private int interactionCount = 0;
    private string lastInteractionTime = "";
    private bool isGridSelected = false;
    private Vector2 lastKnownPosition;
    private Vector2 lastKnownSize;

    /// <summary>
    /// 获取网格交互ID
    /// </summary>
    public string GetGridInteractID()
    {
        if (string.IsNullOrEmpty(gridInteractID) && autoGenerateID)
        {
            GenerateNewGridInteractID();
        }
        return gridInteractID;
    }

    /// <summary>
    /// 设置网格交互ID
    /// </summary>
    public void SetGridInteractID(string id)
    {
        if (!string.IsNullOrEmpty(id))
        {
            gridInteractID = id;
        }
    }

    /// <summary>
    /// 生成新的网格交互ID
    /// </summary>
    public void GenerateNewGridInteractID()
    {
        gridInteractID = $"GridInteract_{currentGridType}_{System.Guid.NewGuid().ToString("N")[..8]}";
    }

    /// <summary>
    /// 验证网格交互ID是否有效
    /// </summary>
    public bool IsGridInteractIDValid()
    {
        return !string.IsNullOrEmpty(gridInteractID) && gridInteractID.Length >= 8;
    }

    /// <summary>
    /// 保存网格状态
    /// </summary>
    public bool SaveGridState()
    {
        if (!enableStatePersistence)
        {
            Debug.Log("[GridInteract] 状态持久化已禁用");
            return false;
        }

        try
        {
            UpdateCurrentStateInfo();

            var saveData = new GridInteractSaveData
            {
                gridID = GetGridInteractID(),
                gridType = currentGridType,
                gridPriority = gridPriority,
                isActive = currentActiveGridInteract == this,
                wasInGrid = wasInGrid,
                gridPosition = lastKnownPosition,
                gridSize = lastKnownSize,
                gridName = gameObject.name,
                isSelected = isGridSelected,
                lastInteractionTime = lastInteractionTime,
                interactionCount = interactionCount,
                lastModified = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                saveVersion = saveVersion
            };

            string jsonData = JsonUtility.ToJson(saveData, true);
            if (!string.IsNullOrEmpty(jsonData))
            {
                OnGridStateSaved?.Invoke(GetGridInteractID());
                return true;
            }

            return false;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GridInteract] 保存网格状态时发生错误：{ex.Message}");
            OnGridStateError?.Invoke(GetGridInteractID(), ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 加载网格状态
    /// </summary>
    public bool LoadGridState(string jsonData)
    {
        if (!enableStatePersistence)
        {
            Debug.Log("[GridInteract] 状态持久化已禁用");
            return false;
        }

        try
        {
            if (string.IsNullOrEmpty(jsonData))
            {
                Debug.LogError("[GridInteract] 加载数据为空");
                return false;
            }

            var saveData = JsonUtility.FromJson<GridInteractSaveData>(jsonData);
            if (saveData == null)
            {
                Debug.LogError("[GridInteract] 反序列化网格状态数据失败");
                return false;
            }

            // 验证数据版本
            if (saveData.saveVersion > saveVersion)
            {
                Debug.LogWarning($"[GridInteract] 保存数据版本 ({saveData.saveVersion}) 高于当前版本 ({saveVersion})，可能存在兼容性问题");
            }

            // 验证网格类型匹配
            if (saveData.gridType != currentGridType)
            {
                Debug.LogWarning($"[GridInteract] 保存的网格类型 ({saveData.gridType}) 与当前网格类型 ({currentGridType}) 不匹配");
            }

            // 恢复网格状态
            gridInteractID = saveData.gridID;
            gridPriority = saveData.gridPriority;
            wasInGrid = saveData.wasInGrid;
            lastKnownPosition = saveData.gridPosition;
            lastKnownSize = saveData.gridSize;
            isGridSelected = saveData.isSelected;
            lastInteractionTime = saveData.lastInteractionTime;
            interactionCount = saveData.interactionCount;

            // 恢复活跃状态
            if (saveData.isActive)
            {
                currentActiveGridInteract = this;
            }

            // 恢复选择状态
            if (saveData.isSelected)
            {
                RestoreSelectionState();
            }

            OnGridStateLoaded?.Invoke(GetGridInteractID());
            Debug.Log($"[GridInteract] 网格状态加载成功：{GetGridInteractID()}");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GridInteract] 加载网格状态时发生错误：{ex.Message}");
            OnGridStateError?.Invoke(GetGridInteractID(), ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 更新当前状态信息
    /// </summary>
    private void UpdateCurrentStateInfo()
    {
        try
        {
            if (rectTransform != null)
            {
                lastKnownPosition = rectTransform.anchoredPosition;
                lastKnownSize = rectTransform.sizeDelta;
            }

            isGridSelected = (currentActiveGridInteract == this);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GridInteract] 更新当前状态信息时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 恢复选择状态
    /// </summary>
    private void RestoreSelectionState()
    {
        try
        {
            if (isGridSelected && inventoryController != null)
            {
                // 清除其他网格选择
                ClearAllGridSelections();

                // 设置当前网格为活跃
                currentActiveGridInteract = this;

                // 恢复网格选择
                SetAsSelectedGrid();

                Debug.Log($"[GridInteract] 恢复网格选择状态：{currentGridType}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GridInteract] 恢复选择状态时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 记录交互事件
    /// </summary>
    public void RecordInteraction()
    {
        try
        {
            interactionCount++;
            lastInteractionTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            Debug.Log($"[GridInteract] 记录交互事件：{currentGridType} 总计：{interactionCount}");

            // 自动保存状态
            if (enableStatePersistence)
            {
                SaveGridState();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GridInteract] 记录交互事件时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取交互统计信息
    /// </summary>
    public string GetInteractionStats()
    {
        return $"交互次数: {interactionCount}, 最后交互: {lastInteractionTime}";
    }

    /// <summary>
    /// 重置交互统计
    /// </summary>
    public void ResetInteractionStats()
    {
        interactionCount = 0;
        lastInteractionTime = "";
        Debug.Log("[GridInteract] 已重置交互统计");
    }

    /// <summary>
    /// 设置网格优先级
    /// </summary>
    public void SetGridPriority(int priority)
    {
        if (priority >= 0)
        {
            gridPriority = priority;
            Debug.Log($"[GridInteract] 设置网格优先级：{priority}");

            // 自动保存状态
            if (enableStatePersistence)
            {
                SaveGridState();
            }
        }
    }

    /// <summary>
    /// 获取网格优先级
    /// </summary>
    public int GetGridPriority()
    {
        return gridPriority;
    }

    /// <summary>
    /// 启用/禁用状态持久化
    /// </summary>
    public void SetStatePersistence(bool enabled)
    {
        enableStatePersistence = enabled;
        Debug.Log($"[GridInteract] 状态持久化设置为：{enabled}");
    }

    /// <summary>
    /// 验证网格状态数据
    /// </summary>
    public bool ValidateGridStateData()
    {
        try
        {
            // 验证基本组件
            if (rectTransform == null)
            {
                Debug.LogError("[GridInteract] RectTransform组件缺失");
                return false;
            }

            if (inventoryController == null)
            {
                Debug.LogError("[GridInteract] InventoryController引用缺失");
                return false;
            }

            // 验证网格组件
            bool hasValidGrid = (itemGrid != null) || (backpackGrid != null) || (tacticalRigGrid != null);
            if (!hasValidGrid)
            {
                Debug.LogError("[GridInteract] 缺少有效的网格组件");
                return false;
            }

            // 验证ID
            if (!IsGridInteractIDValid())
            {
                Debug.LogError("[GridInteract] 网格交互ID无效");
                return false;
            }

            // 验证优先级
            if (gridPriority < 0)
            {
                Debug.LogError("[GridInteract] 网格优先级无效");
                return false;
            }

            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GridInteract] 验证网格状态数据时发生错误：{ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取网格状态摘要
    /// </summary>
    public string GetGridStateSummary()
    {
        return $"ID: {GetGridInteractID()}, 类型: {currentGridType}, 优先级: {gridPriority}, 活跃: {currentActiveGridInteract == this}, 选中: {isGridSelected}";
    }

    /// <summary>
    /// 强制刷新网格状态
    /// </summary>
    public void RefreshGridState()
    {
        try
        {
            UpdateCurrentStateInfo();

            // 重新验证网格类型和组件
            bool hasValidGrid = false;

            if (itemGrid != null)
            {
                currentGridType = GridType.MainGrid;
                hasValidGrid = true;
            }
            else if (backpackGrid != null)
            {
                currentGridType = GridType.BackpackGrid;
                hasValidGrid = true;
            }
            else if (tacticalRigGrid != null)
            {
                currentGridType = GridType.TacticalRigGrid;
                hasValidGrid = true;
            }

            if (!hasValidGrid)
            {
                Debug.LogError($"[GridInteract] 刷新状态时未找到有效的网格组件：{gameObject.name}");
                return;
            }

            Debug.Log($"[GridInteract] 网格状态刷新完成：{currentGridType}");

            // 自动保存状态
            if (enableStatePersistence)
            {
                SaveGridState();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GridInteract] 刷新网格状态时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 初始化网格状态保存系统
    /// </summary>
    private void InitializeGridStateSaveSystem()
    {
        // 确保有有效的ID
        if (string.IsNullOrEmpty(gridInteractID) && autoGenerateID)
        {
            GenerateNewGridInteractID();
        }

        // 初始化状态信息
        UpdateCurrentStateInfo();

        // 重置交互统计
        if (interactionCount == 0)
        {
            lastInteractionTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        Debug.Log($"[GridInteract] 网格状态保存系统初始化完成，ID：{GetGridInteractID()}, 类型：{currentGridType}");
    }
}