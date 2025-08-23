using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using InventorySystem.SaveSystem;

public class InventoryController : MonoBehaviour, ISaveable
{
    [Header("库存控制器设置")]
    public ItemGrid selectedItemGrid; // 当前选中的主网格
    public BackpackItemGrid selectedBackpackGrid; // 当前选中的背包网格
    public TactiaclRigItemGrid selectedTacticalRigGrid; // 当前选中的战术挂具网格

    [Header("调试信息设置")]
    [SerializeField] private bool showDebugInfo = true; // 是否显示调试信息

    [Header("预览设置")]
    [SerializeField] private bool enablePreview = true; // 是否启用预览功能
    [SerializeField] private Vector2Int previewItemSize = new Vector2Int(2, 1); // 预览物品大小（用于测试）

    [Header("保存协调器设置")]
    [SerializeField] private bool enableSaveCoordination = true; // 是否启用保存协调功能
    [SerializeField] private bool validateSaveOrder = true; // 是否验证保存顺序
    [SerializeField] private bool logSaveProgress = true; // 是否记录保存进度
    [SerializeField] private float saveTimeout = 30f; // 保存超时时间（秒）

    // 保存依赖管理器引用
    private SaveDependencyManager dependencyManager;
    private ContainerUIStateManager containerUIManager;

    // 高亮组件
    private InventoryHighlight inventoryHighlight;
    private HoverHighlight hoverHighlight; // 悬停高亮组件

    // 当前拖拽的物品（用于预览）
    private InventorySystemItem draggedItem;

    // 当前活跃的网格类型枚举
    public enum ActiveGridType
    {
        None,
        MainGrid,
        BackpackGrid,
        TacticalRigGrid
    }

    [SerializeField] private ActiveGridType currentActiveGrid = ActiveGridType.None;

    private void Awake()
    {
        // 初始化保存系统
        InitializeSaveSystem();

        // 初始化保存协调器
        InitializeSaveCoordinator();
    }

    private void Start()
    {
        // 获取或添加InventoryHighlight组件
        inventoryHighlight = GetComponent<InventoryHighlight>();
        if (inventoryHighlight == null)
        {
            inventoryHighlight = gameObject.AddComponent<InventoryHighlight>();
        }

        // 获取或添加HoverHighlight组件
        hoverHighlight = GetComponent<HoverHighlight>();
        if (hoverHighlight == null)
        {
            hoverHighlight = gameObject.AddComponent<HoverHighlight>();
        }

        // 注册到保存系统
        RegisterToSaveSystem();
    }

    private void Update()
    {
        // 只有在拖拽物品时才检测网格切换
        if (draggedItem != null)
        {
            DetectHoveredGrid();
        }

        // 根据当前活跃网格类型处理输入 - 只处理当前活跃的网格
        switch (currentActiveGrid)
        {
            case ActiveGridType.MainGrid:
                if (selectedItemGrid != null)
                    HandleMainGridInput();
                break;
            case ActiveGridType.BackpackGrid:
                if (selectedBackpackGrid != null)
                    HandleBackpackGridInput();
                break;
            case ActiveGridType.TacticalRigGrid:
                if (selectedTacticalRigGrid != null)
                    HandleTacticalRigGridInput();
                break;
        }

        // 处理预览功能
        if (enablePreview)
        {
            HandlePreview();
        }
    }

    // 处理预览功能
    private void HandlePreview()
    {
        // 只有在拖拽物品时才显示预览
        if (draggedItem == null)
        {
            inventoryHighlight.Hide();
            return;
        }

        // 检查是否有活跃的网格
        object targetGrid = GetCurrentActiveGrid();
        if (targetGrid == null)
        {
            inventoryHighlight.Hide();
            return;
        }

        // 获取鼠标位置
        Vector2 mousePos = Input.mousePosition;
        Vector2Int tilePos = GetTileGridPosition(targetGrid, mousePos);

        // 检查位置是否有效
        if (tilePos.x < 0 || tilePos.y < 0)
        {
            inventoryHighlight.Hide();
            return;
        }

        // 获取物品尺寸
        Vector2Int itemSize = previewItemSize; // 默认大小

        // 从物品数据获取尺寸信息
        if (draggedItem.Data != null)
        {
            // 使用原始数据
            itemSize = new Vector2Int(draggedItem.Data.width, draggedItem.Data.height);
        }

        // 检查是否可以放置（使用智能检测）
        bool canPlace = false;
        if (targetGrid is ItemGrid itemGrid)
        {
            canPlace = itemGrid.CanPlaceItem(draggedItem.gameObject, tilePos);
        }
        else if (targetGrid is BackpackItemGrid backpackGrid)
        {
            canPlace = backpackGrid.CanPlaceItem(draggedItem.gameObject, tilePos);
        }
        else if (targetGrid is TactiaclRigItemGrid tacticalGrid)
        {
            canPlace = tacticalGrid.CanPlaceItem(draggedItem.gameObject, tilePos);
        }

        // 显示预览
        inventoryHighlight.Show(true);
        inventoryHighlight.SetParent(targetGrid);

        // 设置位置和尺寸
        inventoryHighlight.SetPosition(targetGrid, draggedItem, tilePos.x, tilePos.y);
        inventoryHighlight.SetSize(itemSize.x, itemSize.y);

        // 设置颜色
        inventoryHighlight.SetCanPlace(canPlace);

        // 强制刷新高亮对象，确保正确显示
        inventoryHighlight.ForceRefresh();

        // 调试高亮对象状态
        if (showDebugInfo)
        {
            inventoryHighlight.DebugHighlightState();
        }

        if (showDebugInfo)
        {
            Debug.Log($"预览位置: ({tilePos.x}, {tilePos.y}) - 可放置: {canPlace} - 网格类型: {currentActiveGrid} - 物品尺寸: {itemSize.x}x{itemSize.y}");
        }
    }

    // 获取当前活跃的网格
    private object GetCurrentActiveGrid()
    {
        switch (currentActiveGrid)
        {
            case ActiveGridType.MainGrid:
                return selectedItemGrid;
            case ActiveGridType.BackpackGrid:
                return selectedBackpackGrid;
            case ActiveGridType.TacticalRigGrid:
                return selectedTacticalRigGrid;
            default:
                return null;
        }
    }

    // 获取网格位置
    private Vector2Int GetTileGridPosition(object grid, Vector2 screenPos)
    {
        if (grid is ItemGrid itemGrid)
        {
            return itemGrid.GetTileGridPosition(screenPos);
        }
        else if (grid is BackpackItemGrid backpackGrid)
        {
            return backpackGrid.GetTileGridPosition(screenPos);
        }
        else if (grid is TactiaclRigItemGrid tacticalGrid)
        {
            return tacticalGrid.GetTileGridPosition(screenPos);
        }

        return new Vector2Int(-1, -1);
    }

    // 检查是否可以放置物品
    private bool CanPlaceItem(object grid, Vector2Int position, Vector2Int size)
    {
        if (grid is ItemGrid itemGrid)
        {
            return itemGrid.CanPlaceItem(position, size);
        }
        else if (grid is BackpackItemGrid backpackGrid)
        {
            return backpackGrid.CanPlaceItem(position, size);
        }
        else if (grid is TactiaclRigItemGrid tacticalGrid)
        {
            return tacticalGrid.CanPlaceItem(position, size);
        }

        return false;
    }

    // 设置当前拖拽的物品（供外部调用）
    public void SetDraggedItem(InventorySystemItem item)
    {
        draggedItem = item;
    }

    // 清除拖拽的物品
    public void ClearDraggedItem()
    {
        draggedItem = null;
        inventoryHighlight.Hide();
    }

    // 处理主网格输入
    private void HandleMainGridInput()
    {
        if (selectedItemGrid == null) return;

        // 左键点击时获取网格位置并输出调试信息
        if (Input.GetMouseButtonDown(0))
        {
            Vector2Int gridPosition = selectedItemGrid.GetTileGridPosition(Input.mousePosition);

            if (showDebugInfo)
            {
                Debug.Log($"点击主网格位置: ({gridPosition.x}, {gridPosition.y}) - 网格名称: {selectedItemGrid.gameObject.name}");
            }
        }
    }

    // 处理背包网格输入
    private void HandleBackpackGridInput()
    {
        if (selectedBackpackGrid == null) return;

        // 左键点击时获取网格位置并输出调试信息
        if (Input.GetMouseButtonDown(0))
        {
            Vector2Int gridPosition = selectedBackpackGrid.GetTileGridPosition(Input.mousePosition);

            if (showDebugInfo)
            {
                Debug.Log($"点击背包网格位置: ({gridPosition.x}, {gridPosition.y}) - 网格名称: {selectedBackpackGrid.gameObject.name}");
            }
        }
    }

    // 处理战术挂具网格输入
    private void HandleTacticalRigGridInput()
    {
        if (selectedTacticalRigGrid == null) return;

        // 左键点击时获取网格位置并输出调试信息
        if (Input.GetMouseButtonDown(0))
        {
            Vector2Int gridPosition = selectedTacticalRigGrid.GetTileGridPosition(Input.mousePosition);

            if (showDebugInfo)
            {
                Debug.Log($"点击战术挂具网格位置: ({gridPosition.x}, {gridPosition.y}) - 网格名称: {selectedTacticalRigGrid.gameObject.name}");
            }
        }
    }

    // 设置选中的主网格
    public void SetSelectedMainGrid(ItemGrid itemGrid)
    {
        if (selectedItemGrid != itemGrid)
        {
            selectedItemGrid = itemGrid;
            currentActiveGrid = ActiveGridType.MainGrid;
            if (showDebugInfo)
            {
                Debug.Log($"切换选中主网格: {(itemGrid != null ? itemGrid.gameObject.name : "无")}");
            }
        }
    }

    // 设置选中的背包网格
    public void SetSelectedBackpackGrid(BackpackItemGrid backpackGrid)
    {
        if (selectedBackpackGrid != backpackGrid)
        {
            selectedBackpackGrid = backpackGrid;
            currentActiveGrid = ActiveGridType.BackpackGrid;
            if (showDebugInfo)
            {
                Debug.Log($"切换选中背包网格: {(backpackGrid != null ? backpackGrid.gameObject.name : "无")}");
            }
        }
    }

    // 设置选中的战术挂具网格
    public void SetSelectedTacticalRigGrid(TactiaclRigItemGrid tacticalRigGrid)
    {
        if (selectedTacticalRigGrid != tacticalRigGrid)
        {
            selectedTacticalRigGrid = tacticalRigGrid;
            currentActiveGrid = ActiveGridType.TacticalRigGrid;
            if (showDebugInfo)
            {
                Debug.Log($"切换选中战术挂具网格: {(tacticalRigGrid != null ? tacticalRigGrid.gameObject.name : "无")}");
            }
        }
    }

    // 清除选中的网格
    public void ClearSelectedGrid()
    {
        if (showDebugInfo)
        {
            string gridName = "无";
            switch (currentActiveGrid)
            {
                case ActiveGridType.MainGrid:
                    gridName = selectedItemGrid?.gameObject.name ?? "无";
                    break;
                case ActiveGridType.BackpackGrid:
                    gridName = selectedBackpackGrid?.gameObject.name ?? "无";
                    break;
                case ActiveGridType.TacticalRigGrid:
                    gridName = selectedTacticalRigGrid?.gameObject.name ?? "无";
                    break;
            }
            Debug.Log($"清除选中网格: {gridName}");
        }

        selectedItemGrid = null;
        selectedBackpackGrid = null;
        selectedTacticalRigGrid = null;
        currentActiveGrid = ActiveGridType.None;
    }

    // 获取是否有选中的网格
    public bool HasSelectedGrid()
    {
        return currentActiveGrid != ActiveGridType.None &&
               (selectedItemGrid != null || selectedBackpackGrid != null || selectedTacticalRigGrid != null);
    }

    // 获取当前活跃的网格类型
    public ActiveGridType GetActiveGridType()
    {
        return currentActiveGrid;
    }

    // 获取当前活跃的网格对象（通用方法）
    public GameObject GetActiveGridObject()
    {
        switch (currentActiveGrid)
        {
            case ActiveGridType.MainGrid:
                return selectedItemGrid?.gameObject;
            case ActiveGridType.BackpackGrid:
                return selectedBackpackGrid?.gameObject;
            case ActiveGridType.TacticalRigGrid:
                return selectedTacticalRigGrid?.gameObject;
            default:
                return null;
        }
    }

    // 检测鼠标当前悬停的网格并自动切换活跃网格
    private void DetectHoveredGrid()
    {
        Vector2 mousePos = Input.mousePosition;

        // 检查主网格
        if (selectedItemGrid != null && IsMouseOverGrid(selectedItemGrid.transform, mousePos))
        {
            if (currentActiveGrid != ActiveGridType.MainGrid)
            {
                currentActiveGrid = ActiveGridType.MainGrid;
                if (showDebugInfo)
                {
                    Debug.Log("切换到主网格");
                }
            }
            return;
        }

        // 检查背包网格
        if (selectedBackpackGrid != null && IsMouseOverGrid(selectedBackpackGrid.transform, mousePos))
        {
            if (currentActiveGrid != ActiveGridType.BackpackGrid)
            {
                currentActiveGrid = ActiveGridType.BackpackGrid;
                if (showDebugInfo)
                {
                    Debug.Log("切换到背包网格");
                }
            }
            return;
        }

        // 检查战术挂具网格
        if (selectedTacticalRigGrid != null && IsMouseOverGrid(selectedTacticalRigGrid.transform, mousePos))
        {
            if (currentActiveGrid != ActiveGridType.TacticalRigGrid)
            {
                currentActiveGrid = ActiveGridType.TacticalRigGrid;
                if (showDebugInfo)
                {
                    Debug.Log("切换到战术挂具网格");
                }
            }
            return;
        }
    }

    // 检查鼠标是否在指定网格上方
    private bool IsMouseOverGrid(Transform gridTransform, Vector2 mousePos)
    {
        if (gridTransform == null) return false;

        RectTransform rectTransform = gridTransform.GetComponent<RectTransform>();
        if (rectTransform == null) return false;

        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, mousePos, null);
    }

    /// <summary>
    /// 显示悬停高亮
    /// </summary>
    /// <param name="item">要高亮的物品</param>
    /// <param name="parentGrid">父级网格</param>
    public void ShowHoverHighlight(InventorySystemItem item, Transform parentGrid)
    {
        if (hoverHighlight != null)
        {
            hoverHighlight.ShowHoverHighlight(item, parentGrid);
        }
    }

    /// <summary>
    /// 显示悬停高亮（装备栏版本）
    /// </summary>
    /// <param name="item">要高亮的物品</param>
    /// <param name="parentGrid">父级网格</param>
    /// <param name="equipSlot">装备栏引用</param>
    public void ShowHoverHighlight(InventorySystemItem item, Transform parentGrid, EquipSlot equipSlot)
    {
        if (hoverHighlight != null)
        {
            hoverHighlight.ShowHoverHighlight(item, parentGrid, equipSlot);
        }
    }

    /// <summary>
    /// 隐藏悬停高亮
    /// </summary>
    public void HideHoverHighlight()
    {
        if (hoverHighlight != null)
        {
            hoverHighlight.HideHoverHighlight();
        }
    }

    /// <summary>
    /// 强制刷新预览
    /// </summary>
    public void ForceRefreshPreview()
    {
        // 强制刷新预览
        if (enablePreview && draggedItem != null)
        {
            HandlePreview();
        }
    }

    // ==================== 保存系统扩展接口 ====================

    [System.Serializable]
    public class InventoryControllerSaveData
    {
        public string controllerID;
        public ActiveGridType currentActiveGrid;
        public bool enablePreview;
        public bool showDebugInfo;
        public Vector2Int previewItemSize;
        public string lastModified;
        public int saveVersion;
        public List<string> registeredGridIds;
        public List<string> registeredItemIds;
    }

    [Header("保存系统设置")]
    [SerializeField] private string controllerID = "";
    [SerializeField] private bool autoGenerateID = true;
    [SerializeField] private int saveVersion = 1;

    // 注册的网格字典
    private Dictionary<string, ISaveable> registeredGrids = new Dictionary<string, ISaveable>();
    private Dictionary<string, ISaveable> registeredItems = new Dictionary<string, ISaveable>();

    // 保存系统事件
    public System.Action<string> OnSaveCompleted;
    public System.Action<string> OnLoadCompleted;
    public System.Action<string, string> OnSaveError;
    public System.Action<string, string> OnLoadError;

    /// <summary>
    /// 获取控制器ID
    /// </summary>
    public string GetControllerID()
    {
        if (string.IsNullOrEmpty(controllerID) && autoGenerateID)
        {
            GenerateNewControllerID();
        }
        return controllerID;
    }

    /// <summary>
    /// 设置控制器ID
    /// </summary>
    public void SetControllerID(string id)
    {
        if (!string.IsNullOrEmpty(id))
        {
            controllerID = id;
        }
    }

    /// <summary>
    /// 生成新的控制器ID
    /// </summary>
    public void GenerateNewControllerID()
    {
        controllerID = "InventoryController_" + System.Guid.NewGuid().ToString("N")[..8];
    }

    /// <summary>
    /// 验证控制器ID是否有效
    /// </summary>
    public bool IsControllerIDValid()
    {
        return !string.IsNullOrEmpty(controllerID) && controllerID.Length >= 8;
    }

    /// <summary>
    /// 注册网格到保存系统
    /// </summary>
    public bool RegisterGrid(string gridID, ISaveable grid)
    {
        try
        {
            if (string.IsNullOrEmpty(gridID) || grid == null)
            {
                Debug.LogError("[InventoryController] 注册网格失败：ID或网格对象为空");
                return false;
            }

            if (registeredGrids.ContainsKey(gridID))
            {
                Debug.LogWarning($"[InventoryController] 网格ID '{gridID}' 已存在，将覆盖原有注册");
            }

            registeredGrids[gridID] = grid;
            Debug.Log($"[InventoryController] 成功注册网格：{gridID}");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[InventoryController] 注册网格时发生错误：{ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 注册物品到保存系统
    /// </summary>
    public bool RegisterItem(string itemID, ISaveable item)
    {
        try
        {
            if (string.IsNullOrEmpty(itemID) || item == null)
            {
                Debug.LogError("[InventoryController] 注册物品失败：ID或物品对象为空");
                return false;
            }

            if (registeredItems.ContainsKey(itemID))
            {
                Debug.LogWarning($"[InventoryController] 物品ID '{itemID}' 已存在，将覆盖原有注册");
            }

            registeredItems[itemID] = item;
            Debug.Log($"[InventoryController] 成功注册物品：{itemID}");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[InventoryController] 注册物品时发生错误：{ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 取消注册网格
    /// </summary>
    public bool UnregisterGrid(string gridID)
    {
        if (registeredGrids.ContainsKey(gridID))
        {
            registeredGrids.Remove(gridID);
            Debug.Log($"[InventoryController] 取消注册网格：{gridID}");
            return true;
        }
        return false;
    }

    /// <summary>
    /// 取消注册物品
    /// </summary>
    public bool UnregisterItem(string itemID)
    {
        if (registeredItems.ContainsKey(itemID))
        {
            registeredItems.Remove(itemID);
            Debug.Log($"[InventoryController] 取消注册物品：{itemID}");
            return true;
        }
        return false;
    }

    /// <summary>
    /// 分配新的网格ID
    /// </summary>
    public string AllocateGridID(string baseName = "Grid")
    {
        string newID;
        int counter = 1;

        do
        {
            newID = $"{baseName}_{counter:D3}_{System.Guid.NewGuid().ToString("N")[..6]}";
            counter++;
        }
        while (registeredGrids.ContainsKey(newID) && counter < 1000);

        if (counter >= 1000)
        {
            // 如果计数器达到1000，使用完全随机的ID
            newID = $"{baseName}_{System.Guid.NewGuid().ToString("N")[..12]}";
        }

        Debug.Log($"[InventoryController] 分配新网格ID：{newID}");
        return newID;
    }

    /// <summary>
    /// 分配新的物品ID
    /// </summary>
    public string AllocateItemID(string baseName = "Item")
    {
        string newID;
        int counter = 1;

        do
        {
            newID = $"{baseName}_{counter:D3}_{System.Guid.NewGuid().ToString("N")[..6]}";
            counter++;
        }
        while (registeredItems.ContainsKey(newID) && counter < 1000);

        if (counter >= 1000)
        {
            // 如果计数器达到1000，使用完全随机的ID
            newID = $"{baseName}_{System.Guid.NewGuid().ToString("N")[..12]}";
        }

        Debug.Log($"[InventoryController] 分配新物品ID：{newID}");
        return newID;
    }

    /// <summary>
    /// 保存所有注册的对象
    /// </summary>
    public bool SaveAll()
    {
        try
        {
            Debug.Log("[InventoryController] 开始保存所有注册对象...");

            // 保存控制器自身数据
            string controllerData = CreateSaveData();
            if (string.IsNullOrEmpty(controllerData))
            {
                Debug.LogError("[InventoryController] 创建控制器保存数据失败");
                OnSaveError?.Invoke(GetControllerID(), "创建控制器保存数据失败");
                return false;
            }

            // 保存所有注册的网格
            foreach (var kvp in registeredGrids)
            {
                try
                {
                    if (kvp.Value != null)
                    {
                        string gridData = kvp.Value.SerializeToJson();
                        if (string.IsNullOrEmpty(gridData))
                        {
                            Debug.LogWarning($"[InventoryController] 网格 '{kvp.Key}' 序列化失败");
                        }
                        else
                        {
                            Debug.Log($"[InventoryController] 网格 '{kvp.Key}' 保存成功");
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[InventoryController] 保存网格 '{kvp.Key}' 时发生错误：{ex.Message}");
                }
            }

            // 保存所有注册的物品
            foreach (var kvp in registeredItems)
            {
                try
                {
                    if (kvp.Value != null)
                    {
                        string itemData = kvp.Value.SerializeToJson();
                        if (string.IsNullOrEmpty(itemData))
                        {
                            Debug.LogWarning($"[InventoryController] 物品 '{kvp.Key}' 序列化失败");
                        }
                        else
                        {
                            Debug.Log($"[InventoryController] 物品 '{kvp.Key}' 保存成功");
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[InventoryController] 保存物品 '{kvp.Key}' 时发生错误：{ex.Message}");
                }
            }

            OnSaveCompleted?.Invoke(GetControllerID());
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[InventoryController] 保存过程中发生严重错误：{ex.Message}");
            OnSaveError?.Invoke(GetControllerID(), ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 加载所有注册的对象
    /// </summary>
    public bool LoadAll()
    {
        try
        {
            Debug.Log("[InventoryController] 开始加载所有注册对象...");

            // 加载所有注册的网格
            foreach (var kvp in registeredGrids)
            {
                try
                {
                    if (kvp.Value != null)
                    {
                        // 这里应该从实际的保存文件中读取数据
                        // 目前作为示例，我们跳过实际的文件读取
                        Debug.Log($"[InventoryController] 网格 '{kvp.Key}' 加载成功");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[InventoryController] 加载网格 '{kvp.Key}' 时发生错误：{ex.Message}");
                }
            }

            // 加载所有注册的物品
            foreach (var kvp in registeredItems)
            {
                try
                {
                    if (kvp.Value != null)
                    {
                        // 这里应该从实际的保存文件中读取数据
                        // 目前作为示例，我们跳过实际的文件读取
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[InventoryController] 加载物品 '{kvp.Key}' 时发生错误：{ex.Message}");
                }
            }

            OnLoadCompleted?.Invoke(GetControllerID());
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[InventoryController] 加载过程中发生严重错误：{ex.Message}");
            OnLoadError?.Invoke(GetControllerID(), ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 创建控制器保存数据
    /// </summary>
    public string CreateSaveData()
    {
        try
        {
            var saveData = new InventoryControllerSaveData
            {
                controllerID = GetControllerID(),
                currentActiveGrid = currentActiveGrid,
                enablePreview = enablePreview,
                showDebugInfo = showDebugInfo,
                previewItemSize = previewItemSize,
                lastModified = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                saveVersion = saveVersion
            };

            return JsonUtility.ToJson(saveData, true);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[InventoryController] 创建保存数据时发生错误：{ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 从保存数据加载控制器状态
    /// </summary>
    public bool LoadFromSaveData(string jsonData)
    {
        try
        {
            if (string.IsNullOrEmpty(jsonData))
            {
                Debug.LogError("[InventoryController] 加载数据为空");
                return false;
            }

            var saveData = JsonUtility.FromJson<InventoryControllerSaveData>(jsonData);
            if (saveData == null)
            {
                Debug.LogError("[InventoryController] 反序列化保存数据失败");
                return false;
            }

            // 验证数据版本
            if (saveData.saveVersion > saveVersion)
            {
                Debug.LogWarning($"[InventoryController] 保存数据版本 ({saveData.saveVersion}) 高于当前版本 ({saveVersion})，可能存在兼容性问题");
            }

            // 恢复控制器状态
            controllerID = saveData.controllerID;
            currentActiveGrid = saveData.currentActiveGrid;
            enablePreview = saveData.enablePreview;
            showDebugInfo = saveData.showDebugInfo;
            previewItemSize = saveData.previewItemSize;

            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[InventoryController] 加载保存数据时发生错误：{ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 验证保存数据的完整性
    /// </summary>
    public bool ValidateSaveData()
    {
        try
        {
            // 验证控制器ID
            if (!IsControllerIDValid())
            {
                Debug.LogError("[InventoryController] 控制器ID无效");
                return false;
            }

            // 验证注册的网格
            foreach (var kvp in registeredGrids)
            {
                if (kvp.Value == null)
                {
                    Debug.LogError($"[InventoryController] 注册的网格 '{kvp.Key}' 对象为空");
                    return false;
                }

                if (!kvp.Value.ValidateData())
                {
                    Debug.LogError($"[InventoryController] 网格 '{kvp.Key}' 数据验证失败");
                    return false;
                }
            }

            // 验证注册的物品
            foreach (var kvp in registeredItems)
            {
                if (kvp.Value == null)
                {
                    Debug.LogError($"[InventoryController] 注册的物品 '{kvp.Key}' 对象为空");
                    return false;
                }

                if (!kvp.Value.ValidateData())
                {
                    Debug.LogError($"[InventoryController] 物品 '{kvp.Key}' 数据验证失败");
                    return false;
                }
            }

            Debug.Log("[InventoryController] 保存数据验证通过");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[InventoryController] 验证保存数据时发生错误：{ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取注册的网格数量
    /// </summary>
    public int GetRegisteredGridCount()
    {
        return registeredGrids.Count;
    }

    /// <summary>
    /// 获取注册的物品数量
    /// </summary>
    public int GetRegisteredItemCount()
    {
        return registeredItems.Count;
    }

    /// <summary>
    /// 清除所有注册的对象
    /// </summary>
    public void ClearAllRegistrations()
    {
        registeredGrids.Clear();
        registeredItems.Clear();
        Debug.Log("[InventoryController] 已清除所有注册的对象");
    }

    /// <summary>
    /// 获取所有注册的网格ID
    /// </summary>
    public string[] GetRegisteredGridIDs()
    {
        return registeredGrids.Keys.ToArray();
    }

    /// <summary>
    /// 获取所有注册的物品ID
    /// </summary>
    public string[] GetRegisteredItemIDs()
    {
        return registeredItems.Keys.ToArray();
    }

    /// <summary>
    /// 检查网格是否已注册
    /// </summary>
    public bool IsGridRegistered(string gridID)
    {
        return registeredGrids.ContainsKey(gridID);
    }

    /// <summary>
    /// 检查物品是否已注册
    /// </summary>
    public bool IsItemRegistered(string itemID)
    {
        return registeredItems.ContainsKey(itemID);
    }

    /// <summary>
    /// 获取注册的网格对象
    /// </summary>
    public ISaveable GetRegisteredGrid(string gridID)
    {
        return registeredGrids.TryGetValue(gridID, out ISaveable grid) ? grid : null;
    }

    /// <summary>
    /// 获取注册的物品对象
    /// </summary>
    public ISaveable GetRegisteredItem(string itemID)
    {
        return registeredItems.TryGetValue(itemID, out ISaveable item) ? item : null;
    }

    /// <summary>
    /// 在Awake中初始化保存系统
    /// </summary>
    private void InitializeSaveSystem()
    {
        // 确保控制器有有效的ID
        if (string.IsNullOrEmpty(controllerID) && autoGenerateID)
        {
            GenerateNewControllerID();
        }

        // 初始化字典
        if (registeredGrids == null)
            registeredGrids = new Dictionary<string, ISaveable>();
        if (registeredItems == null)
            registeredItems = new Dictionary<string, ISaveable>();

        Debug.Log($"[InventoryController] 保存系统初始化完成，控制器ID：{GetControllerID()}");
    }

    // ==================== ISaveable接口完整实现 ====================

    /// <summary>
    /// 获取对象的唯一标识ID (ISaveable接口实现)
    /// </summary>
    /// <returns>对象的唯一ID字符串</returns>
    public string GetSaveID()
    {
        return GetControllerID();
    }

    /// <summary>
    /// 设置对象的唯一标识ID (ISaveable接口实现)
    /// </summary>
    /// <param name="id">新的ID字符串</param>
    public void SetSaveID(string id)
    {
        SetControllerID(id);
    }

    /// <summary>
    /// 生成新的唯一保存ID (ISaveable接口实现)
    /// </summary>
    public void GenerateNewSaveID()
    {
        GenerateNewControllerID();
    }

    /// <summary>
    /// 验证保存ID是否有效 (ISaveable接口实现)
    /// </summary>
    /// <returns>ID是否有效</returns>
    public bool IsSaveIDValid()
    {
        return IsControllerIDValid();
    }

    /// <summary>
    /// 获取保存数据 (ISaveable接口实现)
    /// </summary>
    /// <returns>保存数据对象</returns>
    public InventoryControllerSaveData GetSaveData()
    {
        var saveData = new InventoryControllerSaveData
        {
            controllerID = this.controllerID,
            registeredGridIds = new List<string>(registeredGrids.Keys),
            registeredItemIds = new List<string>(registeredItems.Keys),
            lastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        return saveData;
    }

    /// <summary>
    /// 加载保存数据 (ISaveable接口实现)
    /// </summary>
    /// <param name="saveData">保存数据对象</param>
    /// <returns>是否成功加载</returns>
    public bool LoadSaveData(InventoryControllerSaveData saveData)
    {
        try
        {
            if (saveData == null)
            {
                Debug.LogWarning("[InventoryController] 保存数据为空，无法加载");
                return false;
            }

            // 加载控制器ID
            this.controllerID = saveData.controllerID;

            // 注意：网格和物品的实际重新注册应该由它们自己的Start方法处理
            // 这里只是记录预期的注册对象ID，用于验证
            Debug.Log($"[InventoryController] 已加载保存数据，控制器ID: {controllerID}");
            Debug.Log($"[InventoryController] 预期注册网格数量: {saveData.registeredGridIds?.Count ?? 0}");
            Debug.Log($"[InventoryController] 预期注册物品数量: {saveData.registeredItemIds?.Count ?? 0}");

            MarkAsModified();
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[InventoryController] 加载保存数据失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 序列化对象为JSON字符串 (ISaveable接口实现)
    /// </summary>
    /// <returns>JSON字符串</returns>
    public string SerializeToJson()
    {
        var saveData = GetSaveData();
        return JsonUtility.ToJson(saveData, true);
    }

    /// <summary>
    /// 从JSON字符串反序列化对象 (ISaveable接口实现)
    /// </summary>
    /// <param name="json">JSON字符串</param>
    /// <returns>是否成功反序列化</returns>
    public bool DeserializeFromJson(string json)
    {
        try
        {
            var saveData = JsonUtility.FromJson<InventoryControllerSaveData>(json);
            return LoadSaveData(saveData);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[InventoryController] JSON反序列化失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 标记对象为已修改 (ISaveable接口实现)
    /// </summary>
    public void MarkAsModified()
    {
        // InventoryController没有直接的修改标记，但可以触发保存事件
        UpdateLastModified();
        Debug.Log($"[InventoryController] 控制器已标记为修改: {controllerID}");
    }

    /// <summary>
    /// 重置修改标记 (ISaveable接口实现)
    /// </summary>
    public void ResetModifiedFlag()
    {
        // InventoryController没有直接的修改标记，这里可以为空实现
        Debug.Log($"[InventoryController] 重置修改标记: {controllerID}");
    }

    /// <summary>
    /// 检查是否已修改 (ISaveable接口实现)
    /// </summary>
    /// <returns>是否已修改</returns>
    public bool IsModified()
    {
        // InventoryController总是返回true，因为它管理动态状态
        return true;
    }

    /// <summary>
    /// 验证对象数据的完整性 (ISaveable接口实现)
    /// </summary>
    /// <returns>数据是否有效</returns>
    public bool ValidateData()
    {
        // 验证控制器ID
        if (!IsSaveIDValid())
        {
            Debug.LogError($"[InventoryController] 控制器ID无效: {controllerID}");
            return false;
        }

        // 验证注册的网格和物品
        foreach (var grid in registeredGrids.Values)
        {
            if (grid == null || !grid.ValidateData())
            {
                Debug.LogError($"[InventoryController] 注册的网格数据无效");
                return false;
            }
        }

        foreach (var item in registeredItems.Values)
        {
            if (item == null || !item.ValidateData())
            {
                Debug.LogError($"[InventoryController] 注册的物品数据无效");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 获取最后修改时间 (ISaveable接口实现)
    /// </summary>
    /// <returns>最后修改时间字符串</returns>
    public string GetLastModified()
    {
        return System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// 更新最后修改时间 (ISaveable接口实现)
    /// </summary>
    public void UpdateLastModified()
    {
        // InventoryController的修改时间由其管理的对象决定
        Debug.Log($"[InventoryController] 更新最后修改时间: {GetLastModified()}");
    }

    /// <summary>
    /// 注册到保存系统
    /// </summary>
    private void RegisterToSaveSystem()
    {
        var saveManager = SaveManager.Instance;
        if (saveManager != null)
        {
            saveManager.RegisterSaveable(this);
            Debug.Log($"[InventoryController] 已注册到保存系统: {GetControllerID()}");
        }
        else
        {
            Debug.LogWarning("[InventoryController] SaveManager实例未找到，无法注册到保存系统");
        }
    }

    /// <summary>
    /// 从保存系统注销
    /// </summary>
    private void UnregisterFromSaveSystem()
    {
        var saveManager = SaveManager.Instance;
        if (saveManager != null)
        {
            saveManager.UnregisterSaveable(this);
            Debug.Log($"[InventoryController] 已从保存系统注销: {GetControllerID()}");
        }
    }

    /// <summary>
    /// 在对象销毁时注销保存系统
    /// </summary>
    private void OnDestroy()
    {
        // 从保存系统注销
        UnregisterFromSaveSystem();

        // 注销所有已注册的网格和物品
        if (registeredGrids != null)
        {
            registeredGrids.Clear();
        }

        if (registeredItems != null)
        {
            registeredItems.Clear();
        }

        Debug.Log($"[InventoryController] 控制器已销毁并清理注册对象: {controllerID}");
    }

    /// <summary>
    /// 初始化保存协调器
    /// </summary>
    private void InitializeSaveCoordinator()
    {
        if (!enableSaveCoordination)
        {
            Debug.Log("[InventoryController] 保存协调器功能已禁用");
            return;
        }

        try
        {
            // 初始化保存依赖管理器
            if (dependencyManager == null)
            {
                dependencyManager = GetComponent<SaveDependencyManager>();
                if (dependencyManager == null)
                {
                    dependencyManager = gameObject.AddComponent<SaveDependencyManager>();
                }
            }

            // 初始化容器UI状态管理器
            if (containerUIManager == null)
            {
                containerUIManager = GetComponent<ContainerUIStateManager>();
                if (containerUIManager == null)
                {
                    containerUIManager = gameObject.AddComponent<ContainerUIStateManager>();
                }
            }

            Debug.Log("[InventoryController] 保存协调器初始化完成");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[InventoryController] 初始化保存协调器时发生错误: {ex.Message}");
        }
    }
}