using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using InventorySystem.SaveSystem;

// 装备槽保存数据
[System.Serializable]
public class EquipSlotSaveData
{
    public string equipSlotID;                    // 装备槽唯一ID
    public InventorySystemItemCategory slotType;  // 装备槽类型
    public bool hasEquippedItem;                  // 是否有装备物品
    public ItemSaveData equippedItemData;         // 装备物品数据
    public GridSaveData dynamicGridData;          // 动态网格保存数据
    public string lastModified;                   // 最后修改时间

    public EquipSlotSaveData()
    {
        equipSlotID = System.Guid.NewGuid().ToString();
        slotType = InventorySystemItemCategory.Helmet;
        hasEquippedItem = false;
        equippedItemData = null;
        dynamicGridData = null;
        lastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}

// 动态网格保存数据
[System.Serializable]
public class GridSaveData
{
    public string gridType;                       // 网格类型（"Backpack" 或 "TacticalRig"）
    public Vector2Int gridSize;                   // 网格尺寸
    public List<ItemSaveData> gridItems;          // 网格中的物品数据
    public bool isActive;                         // 网格是否激活

    public GridSaveData()
    {
        gridType = "";
        gridSize = Vector2Int.zero;
        gridItems = new List<ItemSaveData>();
        isActive = false;
    }
}

/// <summary>
/// 挂在装备栏的每个格子上，负责接收拖拽过来的物品
/// 实现ISaveable接口以支持装备槽状态保存
/// </summary>
public class EquipSlot : MonoBehaviour, IDropHandler, ISaveable
{
    [Header("装备槽标识")]
    [SerializeField] private string equipSlotID;              // 装备槽唯一ID
    [SerializeField] private bool isModified = false;         // 是否已修改

    [Header("这个格子只能装什么类型？")]
    [SerializeField] private InventorySystemItemCategory acceptedType;

    [Header("装备栏适配设置")]
    [SerializeField] private float padding = 10f;
    [SerializeField] private bool autoResize = true;

    [Header("高亮设置")]
    [SerializeField] private GameObject highlightPrefab; // 高亮预制体
    [SerializeField] private Color canEquipColor = new Color(0, 1, 0, 0.5f); // 可装备颜色（绿色半透明）
    [SerializeField] private Color cannotEquipColor = new Color(1, 0, 0, 0.5f); // 不可装备颜色（红色半透明）

    // 网格容器引用
    [Header("网格容器设置")]
    [SerializeField] private Transform backpackContainer; // BackpackContainer引用
    [SerializeField] private Transform tacticalRigContainer; // TacticalRigContainer引用

    // 添加预制体引用
    [Header("网格预制体引用")]
    [SerializeField] private GameObject backpackGridPrefab; // BackpackItemGrid预制体
    [SerializeField] private GameObject tacticalRigGridPrefab; // TacticalRigItemGrid预制体

    private DraggableItem equippedItem;
    private GameObject currentBackpackGrid; // 当前实例化的背包网格
    private GameObject currentTacticalRigGrid; // 当前实例化的战术挂具网格

    // 高亮相关
    private GameObject currentHighlight; // 当前高亮对象
    private Image highlightImage; // 高亮图像组件
    private RectTransform highlightRect; // 高亮矩形变换

    // 保存系统相关
    private SaveManager saveManager;
    private List<ISaveable> registeredDynamicGrids = new List<ISaveable>(); // 已注册的动态网格列表

    /// <summary>
    /// 初始化装备槽
    /// </summary>
    private void Start()
    {
        // 初始化保存系统引用
        InitializeSaveSystemReference();

        // 确保装备槽有有效的ID
        if (string.IsNullOrEmpty(equipSlotID))
        {
            equipSlotID = System.Guid.NewGuid().ToString();
            Debug.Log($"EquipSlot: 生成新的装备槽ID: {equipSlotID}");
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        var dropped = eventData.pointerDrag;
        if (dropped == null) return;

        var draggable = dropped.GetComponent<DraggableItem>();
        if (draggable == null) return;

        var item = draggable.GetComponent<InventorySystemItem>();
        if (item == null || item.Data.itemCategory != acceptedType) return;

        // 如果格子已有物品，交换
        if (equippedItem != null)
        {
            // 移除旧装备时清空对应网格
            RemoveEquippedItemGrid(equippedItem);

            RestoreOriginalSize(equippedItem);
            equippedItem.transform.SetParent(draggable.originalParent, false);
            equippedItem.GetComponent<RectTransform>().anchoredPosition = draggable.originalPos;
            equippedItem.GetComponent<CanvasGroup>().blocksRaycasts = true;
        }

        // 把新物品放进格子
        draggable.transform.SetParent(transform, false);

        if (autoResize)
        {
            FitToEquipSlot(draggable);
        }
        else
        {
            draggable.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }

        draggable.GetComponent<CanvasGroup>().blocksRaycasts = true;
        equippedItem = draggable;

        // 装备新物品时设置对应网格
        SetEquippedItemGrid(item);
    }

    // 设置装备物品的网格
    private void SetEquippedItemGrid(InventorySystemItem item)
    {
        if (item.Data.itemCategory == InventorySystemItemCategory.Backpack && backpackContainer != null)
        {
            // 先保存并清理现有的网格
            if (currentBackpackGrid != null)
            {
                // 强制保存当前网格数据到保存系统
                SaveCurrentGridData(currentBackpackGrid, "Backpack");
                // 从保存系统注销网格
                UnregisterDynamicGridFromSaveSystem(currentBackpackGrid);
                DestroyImmediate(currentBackpackGrid);
            }

            // 实例化新的背包网格
            if (backpackGridPrefab != null)
            {
                currentBackpackGrid = Instantiate(backpackGridPrefab, backpackContainer);

                // 设置网格位置和锚点
                RectTransform gridRect = currentBackpackGrid.GetComponent<RectTransform>();
                if (gridRect != null)
                {
                    gridRect.anchorMin = new Vector2(0, 1); // 左上角锚点
                    gridRect.anchorMax = new Vector2(0, 1);
                    gridRect.pivot = new Vector2(0, 1);
                    gridRect.anchoredPosition = Vector2.zero; // 定位在容器左上角
                }

                // 添加GridISaveableInitializer组件确保ISaveable接口正确初始化
                GridISaveableInitializer initializer = currentBackpackGrid.GetComponent<GridISaveableInitializer>();
                if (initializer == null)
                {
                    initializer = currentBackpackGrid.AddComponent<GridISaveableInitializer>();
                }

                // 获取BackpackItemGrid组件并设置数据
                BackpackItemGrid backpackGrid = currentBackpackGrid.GetComponent<BackpackItemGrid>();
                if (backpackGrid != null)
                {
                    backpackGrid.SetBackpackData(item.Data);

                    // 延迟注册到保存系统，确保网格完全初始化
                    StartCoroutine(DelayedGridRegistration(backpackGrid, "Backpack", item.Data.itemName));
                }
                else
                {
                    Debug.LogError("BackpackItemGrid预制体缺少BackpackItemGrid组件！");
                }
            }
            else
            {
                Debug.LogError("BackpackItemGrid预制体引用为空！请在EquipSlot中设置backpackGridPrefab");
            }
        }
        else if (item.Data.itemCategory == InventorySystemItemCategory.TacticalRig && tacticalRigContainer != null)
        {
            // 先保存并清理现有的网格
            if (currentTacticalRigGrid != null)
            {
                // 强制保存当前网格数据到保存系统
                SaveCurrentGridData(currentTacticalRigGrid, "TacticalRig");
                // 从保存系统注销网格
                UnregisterDynamicGridFromSaveSystem(currentTacticalRigGrid);
                DestroyImmediate(currentTacticalRigGrid);
            }

            // 实例化新的战术挂具网格
            if (tacticalRigGridPrefab != null)
            {
                currentTacticalRigGrid = Instantiate(tacticalRigGridPrefab, tacticalRigContainer);

                // 设置网格位置和锚点
                RectTransform gridRect = currentTacticalRigGrid.GetComponent<RectTransform>();
                if (gridRect != null)
                {
                    gridRect.anchorMin = new Vector2(0, 1); // 左上角锚点
                    gridRect.anchorMax = new Vector2(0, 1);
                    gridRect.pivot = new Vector2(0, 1);
                    gridRect.anchoredPosition = Vector2.zero; // 定位在容器左上角
                }

                // 添加GridISaveableInitializer组件确保ISaveable接口正确初始化
                GridISaveableInitializer initializer = currentTacticalRigGrid.GetComponent<GridISaveableInitializer>();
                if (initializer == null)
                {
                    initializer = currentTacticalRigGrid.AddComponent<GridISaveableInitializer>();
                }

                // 获取TactiaclRigItemGrid组件并设置数据
                TactiaclRigItemGrid tacticalRigGrid = currentTacticalRigGrid.GetComponent<TactiaclRigItemGrid>();
                if (tacticalRigGrid != null)
                {
                    tacticalRigGrid.SetTacticalRigData(item.Data);

                    // 延迟注册到保存系统，确保网格完全初始化
                    StartCoroutine(DelayedGridRegistration(tacticalRigGrid, "TacticalRig", item.Data.itemName));
                }
                else
                {
                    Debug.LogError("TacticalRigItemGrid预制体缺少TactiaclRigItemGrid组件！");
                }
            }
            else
            {
                Debug.LogError("TacticalRigItemGrid预制体引用为空！请在EquipSlot中设置tacticalRigGridPrefab");
            }
        }
    }

    // 移除装备物品的网格
    private void RemoveEquippedItemGrid(DraggableItem item)
    {
        var itemComponent = item.GetComponent<InventorySystemItem>();
        if (itemComponent == null) return;

        if (itemComponent.Data.itemCategory == InventorySystemItemCategory.Backpack)
        {
            if (currentBackpackGrid != null)
            {
                // 强制保存当前网格数据到保存系统
                SaveCurrentGridData(currentBackpackGrid, "Backpack");
                // 从保存系统注销网格
                UnregisterDynamicGridFromSaveSystem(currentBackpackGrid);

                DestroyImmediate(currentBackpackGrid);
                currentBackpackGrid = null;
                Debug.Log("背包已卸下，网格数据已保存并从保存系统注销");
            }
        }
        else if (itemComponent.Data.itemCategory == InventorySystemItemCategory.TacticalRig)
        {
            if (currentTacticalRigGrid != null)
            {
                // 强制保存当前网格数据到保存系统
                SaveCurrentGridData(currentTacticalRigGrid, "TacticalRig");
                // 从保存系统注销网格
                UnregisterDynamicGridFromSaveSystem(currentTacticalRigGrid);

                DestroyImmediate(currentTacticalRigGrid);
                currentTacticalRigGrid = null;
                Debug.Log("战术挂具已卸下，网格数据已保存并从保存系统注销");
            }
        }
    }

    public void OnItemRemoved()
    {
        if (equippedItem != null)
        {
            RemoveEquippedItemGrid(equippedItem);
        }
        equippedItem = null;
    }

    /// 让物品适配装备栏大小，保持间距
    private void FitToEquipSlot(DraggableItem draggable)
    {
        RectTransform slotRect = GetComponent<RectTransform>();
        RectTransform itemRect = draggable.GetComponent<RectTransform>();

        if (slotRect == null || itemRect == null) return;

        // 存储原始大小和缩放，用于恢复
        ItemSizeData sizeData = draggable.GetComponent<ItemSizeData>();
        if (sizeData == null)
        {
            sizeData = draggable.gameObject.AddComponent<ItemSizeData>();
        }
        sizeData.originalSize = itemRect.sizeDelta;
        sizeData.originalScale = itemRect.localScale;

        // 计算装备栏可用空间（减去间距）
        Vector2 availableSize = slotRect.sizeDelta - new Vector2(padding * 2, padding * 2);

        // 获取物品原始大小
        Vector2 originalSize = sizeData.originalSize;

        // 计算缩放比例，保持宽高比
        float scaleX = availableSize.x / originalSize.x;
        float scaleY = availableSize.y / originalSize.y;
        float scale = Mathf.Min(scaleX, scaleY, 1f); // 不放大，只缩小

        // 使用localScale进行缩放，这样会影响所有子对象
        itemRect.localScale = Vector3.one * scale;

        // 居中定位
        itemRect.anchoredPosition = Vector2.zero;
    }

    /// 恢复物品原始大小
    private void RestoreOriginalSize(DraggableItem draggable)
    {
        if (draggable == null) return;

        ItemSizeData sizeData = draggable.GetComponent<ItemSizeData>();
        RectTransform itemRect = draggable.GetComponent<RectTransform>();

        if (sizeData != null && itemRect != null)
        {
            itemRect.sizeDelta = sizeData.originalSize;
            itemRect.localScale = sizeData.originalScale;
        }
    }

    // 获取当前装备的物品
    public DraggableItem GetEquippedItem()
    {
        return equippedItem;
    }

    public void ReequipItem(DraggableItem draggable)
    {
        equippedItem = draggable;
        var item = draggable.GetComponent<InventorySystemItem>();
        if (item != null)
        {
            SetEquippedItemGrid(item);
        }
        FitToEquipSlot(draggable);
        draggable.GetComponent<CanvasGroup>().blocksRaycasts = true;
    }

    /// <summary>
    /// 显示装备栏高亮提示
    /// </summary>
    /// <param name="canEquip">是否可以装备</param>
    public void ShowEquipHighlight(bool canEquip)
    {
        // 如果没有高亮预制体，创建默认高亮
        if (highlightPrefab == null)
        {
            CreateDefaultHighlight();
        }

        // 如果还没有高亮对象，创建一个
        if (currentHighlight == null)
        {
            currentHighlight = Instantiate(highlightPrefab, transform);
            highlightRect = currentHighlight.GetComponent<RectTransform>();
            highlightImage = currentHighlight.GetComponent<Image>();

            // 确保高亮不阻挡射线检测
            if (highlightImage != null)
            {
                highlightImage.raycastTarget = false;
            }

            // 设置高亮覆盖整个装备栏
            if (highlightRect != null)
            {
                highlightRect.anchorMin = Vector2.zero;
                highlightRect.anchorMax = Vector2.one;
                highlightRect.offsetMin = Vector2.zero;
                highlightRect.offsetMax = Vector2.zero;
            }
        }

        // 设置高亮颜色
        if (highlightImage != null)
        {
            highlightImage.color = canEquip ? canEquipColor : cannotEquipColor;
        }

        // 显示高亮
        currentHighlight.SetActive(true);
    }

    /// <summary>
    /// 隐藏装备栏高亮提示
    /// </summary>
    public void HideEquipHighlight()
    {
        if (currentHighlight != null)
        {
            currentHighlight.SetActive(false);
        }
    }

    /// <summary>
    /// 创建默认高亮预制体
    /// </summary>
    private void CreateDefaultHighlight()
    {
        // 创建高亮游戏对象
        highlightPrefab = new GameObject("EquipSlotHighlight");

        // 添加RectTransform组件
        RectTransform rect = highlightPrefab.AddComponent<RectTransform>();

        // 添加Image组件
        UnityEngine.UI.Image img = highlightPrefab.AddComponent<UnityEngine.UI.Image>();
        img.color = canEquipColor;
        img.raycastTarget = false; // 不阻挡射线检测

        // 设置为不激活状态
        highlightPrefab.SetActive(false);
    }

    /// <summary>
    /// 清理高亮资源和动态网格
    /// </summary>
    private void OnDestroy()
    {
        // 清理高亮资源
        if (currentHighlight != null)
        {
            Destroy(currentHighlight);
        }

        // 清理已注册的动态网格
        CleanupRegisteredDynamicGrids();
    }

    // === ISaveable接口实现 ===

    /// <summary>
    /// 获取装备槽唯一ID
    /// </summary>
    /// <returns>装备槽ID</returns>
    public string GetEquipSlotID()
    {
        if (string.IsNullOrEmpty(equipSlotID))
        {
            equipSlotID = System.Guid.NewGuid().ToString();
            MarkAsModified();
        }
        return equipSlotID;
    }

    /// <summary>
    /// 设置装备槽ID
    /// </summary>
    /// <param name="id">新的装备槽ID</param>
    public void SetEquipSlotID(string id)
    {
        if (equipSlotID != id)
        {
            equipSlotID = id;
            MarkAsModified();
        }
    }

    /// <summary>
    /// 生成新的装备槽ID
    /// </summary>
    public void GenerateNewEquipSlotID()
    {
        equipSlotID = System.Guid.NewGuid().ToString();
        MarkAsModified();
    }

    /// <summary>
    /// 验证装备槽ID是否有效
    /// </summary>
    /// <returns>ID是否有效</returns>
    public bool IsEquipSlotIDValid()
    {
        return !string.IsNullOrEmpty(equipSlotID) && equipSlotID.Length > 10;
    }

    // === ISaveable接口方法实现 ===

    /// <summary>
    /// 获取对象的唯一标识ID (ISaveable接口实现)
    /// </summary>
    /// <returns>对象的唯一ID字符串</returns>
    public string GetSaveID()
    {
        return GetEquipSlotID();
    }

    /// <summary>
    /// 设置对象的唯一标识ID (ISaveable接口实现)
    /// </summary>
    /// <param name="id">新的ID字符串</param>
    public void SetSaveID(string id)
    {
        SetEquipSlotID(id);
    }

    /// <summary>
    /// 生成新的唯一标识ID (ISaveable接口实现)
    /// </summary>
    public void GenerateNewSaveID()
    {
        GenerateNewEquipSlotID();
    }

    /// <summary>
    /// 验证保存ID是否有效 (ISaveable接口实现)
    /// </summary>
    /// <returns>ID是否有效</returns>
    public bool IsSaveIDValid()
    {
        return IsEquipSlotIDValid();
    }

    /// <summary>
    /// 创建装备槽保存数据
    /// </summary>
    /// <returns>装备槽保存数据</returns>
    public EquipSlotSaveData CreateSaveData()
    {
        EquipSlotSaveData saveData = new EquipSlotSaveData();

        // 基本信息
        saveData.equipSlotID = GetEquipSlotID();
        saveData.slotType = acceptedType;
        saveData.hasEquippedItem = equippedItem != null;
        saveData.lastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // 装备物品数据
        if (equippedItem != null)
        {
            var itemComponent = equippedItem.GetComponent<InventorySystemItem>();
            if (itemComponent != null)
            {
                saveData.equippedItemData = itemComponent.CreateSaveData();
            }
        }

        // 动态网格数据
        saveData.dynamicGridData = CreateDynamicGridSaveData();

        return saveData;
    }

    /// <summary>
    /// 从保存数据加载装备槽状态
    /// </summary>
    /// <param name="saveData">保存数据</param>
    /// <returns>加载是否成功</returns>
    public bool LoadFromSaveData(EquipSlotSaveData saveData)
    {
        if (saveData == null)
        {
            Debug.LogError("装备槽保存数据为空，无法加载");
            return false;
        }

        try
        {
            // 设置基本信息
            SetEquipSlotID(saveData.equipSlotID);
            acceptedType = saveData.slotType;

            // 清理现有装备
            if (equippedItem != null)
            {
                OnItemRemoved();
            }

            // 加载装备物品
            if (saveData.hasEquippedItem && saveData.equippedItemData != null)
            {
                bool itemLoaded = LoadEquippedItem(saveData.equippedItemData);
                if (!itemLoaded)
                {
                    Debug.LogWarning($"装备槽 {GetEquipSlotID()} 的装备物品加载失败");
                }
            }

            // 加载动态网格数据
            if (saveData.dynamicGridData != null)
            {
                bool gridLoaded = LoadDynamicGridData(saveData.dynamicGridData);
                if (!gridLoaded)
                {
                    Debug.LogWarning($"装备槽 {GetEquipSlotID()} 的动态网格数据加载失败");
                }
            }

            ResetModifiedFlag();
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"装备槽 {GetEquipSlotID()} 加载失败: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 序列化为JSON字符串
    /// </summary>
    /// <returns>JSON字符串</returns>
    public string SerializeToJson()
    {
        try
        {
            EquipSlotSaveData saveData = CreateSaveData();
            return JsonUtility.ToJson(saveData, true);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"装备槽 {GetEquipSlotID()} 序列化失败: {e.Message}");
            return "";
        }
    }

    /// <summary>
    /// 从JSON字符串反序列化
    /// </summary>
    /// <param name="jsonData">JSON数据</param>
    /// <returns>反序列化是否成功</returns>
    public bool DeserializeFromJson(string jsonData)
    {
        if (string.IsNullOrEmpty(jsonData))
        {
            Debug.LogError("JSON数据为空，无法反序列化");
            return false;
        }

        try
        {
            EquipSlotSaveData saveData = JsonUtility.FromJson<EquipSlotSaveData>(jsonData);
            return LoadFromSaveData(saveData);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"装备槽反序列化失败: {e.Message}");
            return false;
        }
    }

    // === 动态网格保存管理 ===

    /// <summary>
    /// 创建动态网格保存数据
    /// </summary>
    /// <returns>动态网格保存数据</returns>
    private GridSaveData CreateDynamicGridSaveData()
    {
        GridSaveData gridData = new GridSaveData();

        // 检查背包网格
        if (currentBackpackGrid != null)
        {
            BackpackItemGrid backpackGrid = currentBackpackGrid.GetComponent<BackpackItemGrid>();
            if (backpackGrid != null)
            {
                gridData.gridType = "Backpack";
                gridData.isActive = currentBackpackGrid.activeInHierarchy;
                gridData.gridSize = new Vector2Int(backpackGrid.Width, backpackGrid.Height);
                gridData.gridItems = GetGridItemsData(backpackGrid);
            }
        }
        // 检查战术挂具网格
        else if (currentTacticalRigGrid != null)
        {
            TactiaclRigItemGrid tacticalRigGrid = currentTacticalRigGrid.GetComponent<TactiaclRigItemGrid>();
            if (tacticalRigGrid != null)
            {
                gridData.gridType = "TacticalRig";
                gridData.isActive = currentTacticalRigGrid.activeInHierarchy;
                gridData.gridSize = new Vector2Int(tacticalRigGrid.Width, tacticalRigGrid.Height);
                gridData.gridItems = GetGridItemsData(tacticalRigGrid);
            }
        }

        return gridData;
    }

    /// <summary>
    /// 获取网格中的物品数据
    /// </summary>
    /// <param name="grid">网格组件</param>
    /// <returns>物品数据列表</returns>
    private List<ItemSaveData> GetGridItemsData(BaseItemGrid grid)
    {
        List<ItemSaveData> itemsData = new List<ItemSaveData>();

        if (grid != null)
        {
            var placedItems = grid.GetPlacedItems();
            foreach (var placedItem in placedItems)
            {
                if (placedItem.item != null)
                {
                    var itemComponent = placedItem.item.GetComponent<InventorySystemItem>();
                    if (itemComponent != null)
                    {
                        ItemSaveData itemData = itemComponent.CreateSaveData();
                        itemData.gridPosition = placedItem.position;
                        itemsData.Add(itemData);
                    }
                }
            }
        }

        return itemsData;
    }

    /// <summary>
    /// 加载装备物品
    /// </summary>
    /// <param name="itemData">物品保存数据</param>
    /// <returns>加载是否成功</returns>
    private bool LoadEquippedItem(ItemSaveData itemData)
    {
        // 这里需要根据实际的物品创建逻辑来实现
        // 暂时返回true，具体实现需要配合物品生成系统
        Debug.Log($"装备槽 {GetEquipSlotID()} 需要加载装备物品: {itemData.itemDataID}");
        return true;
    }

    /// <summary>
    /// 加载动态网格数据
    /// </summary>
    /// <param name="gridData">网格保存数据</param>
    /// <returns>加载是否成功</returns>
    private bool LoadDynamicGridData(GridSaveData gridData)
    {
        if (gridData == null || string.IsNullOrEmpty(gridData.gridType))
        {
            return true; // 没有网格数据也算成功
        }

        try
        {
            if (gridData.gridType == "Backpack" && backpackGridPrefab != null && backpackContainer != null)
            {
                // 重新创建背包网格
                if (currentBackpackGrid != null)
                {
                    DestroyImmediate(currentBackpackGrid);
                }

                currentBackpackGrid = Instantiate(backpackGridPrefab, backpackContainer);
                currentBackpackGrid.SetActive(gridData.isActive);

                // 加载网格中的物品
                LoadGridItems(currentBackpackGrid.GetComponent<BackpackItemGrid>(), gridData.gridItems);
            }
            else if (gridData.gridType == "TacticalRig" && tacticalRigGridPrefab != null && tacticalRigContainer != null)
            {
                // 重新创建战术挂具网格
                if (currentTacticalRigGrid != null)
                {
                    DestroyImmediate(currentTacticalRigGrid);
                }

                currentTacticalRigGrid = Instantiate(tacticalRigGridPrefab, tacticalRigContainer);
                currentTacticalRigGrid.SetActive(gridData.isActive);

                // 加载网格中的物品
                LoadGridItems(currentTacticalRigGrid.GetComponent<TactiaclRigItemGrid>(), gridData.gridItems);
            }

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载动态网格数据失败: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 加载网格中的物品
    /// </summary>
    /// <param name="grid">目标网格</param>
    /// <param name="itemsData">物品数据列表</param>
    private void LoadGridItems(BaseItemGrid grid, List<ItemSaveData> itemsData)
    {
        if (grid == null || itemsData == null) return;

        foreach (var itemData in itemsData)
        {
            // 这里需要根据实际的物品创建逻辑来实现
            // 暂时只记录日志，具体实现需要配合物品生成系统
            Debug.Log($"网格需要加载物品: ID={itemData.itemDataID}, 位置=({itemData.gridPosition.x}, {itemData.gridPosition.y})");
        }
    }

    // === 修改状态管理 ===

    /// <summary>
    /// 标记为已修改 (ISaveable接口实现)
    /// </summary>
    public void MarkAsModified()
    {
        isModified = true;
        UpdateLastModified();
    }

    /// <summary>
    /// 重置修改标记 (ISaveable接口实现)
    /// </summary>
    public void ResetModifiedFlag()
    {
        isModified = false;
    }

    /// <summary>
    /// 检查是否已修改 (ISaveable接口实现)
    /// </summary>
    /// <returns>是否已修改</returns>
    public bool IsModified()
    {
        return isModified;
    }

    /// <summary>
    /// 验证对象数据的完整性 (ISaveable接口实现)
    /// </summary>
    /// <returns>数据是否有效</returns>
    public bool ValidateData()
    {
        // 验证装备槽ID
        if (!IsSaveIDValid())
        {
            return false;
        }

        // 验证装备物品数据
        if (equippedItem != null)
        {
            var itemComponent = equippedItem.GetComponent<InventorySystemItem>();
            if (itemComponent == null || !itemComponent.HasValidItemData())
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 获取对象的最后修改时间 (ISaveable接口实现)
    /// </summary>
    /// <returns>最后修改时间字符串</returns>
    public string GetLastModified()
    {
        return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// 更新最后修改时间为当前时间 (ISaveable接口实现)
    /// </summary>
    public void UpdateLastModified()
    {
        // 在实际项目中，可以添加一个lastModified字段来存储时间
        // 这里暂时使用当前时间
    }

    // === 测试和调试方法 ===

    /// <summary>
    /// 打印装备槽详细信息
    /// </summary>
    [ContextMenu("打印装备槽详细信息")]
    public void LogEquipSlotDetails()
    {
        Debug.Log($"=== 装备槽详细信息 ===\n" +
                  $"装备槽ID: {GetEquipSlotID()}\n" +
                  $"接受类型: {acceptedType}\n" +
                  $"是否有装备: {equippedItem != null}\n" +
                  $"是否已修改: {isModified}\n" +
                  $"背包网格: {(currentBackpackGrid != null ? "已实例化" : "未实例化")}\n" +
                  $"战术挂具网格: {(currentTacticalRigGrid != null ? "已实例化" : "未实例化")}");

        if (equippedItem != null)
        {
            var itemComponent = equippedItem.GetComponent<InventorySystemItem>();
            if (itemComponent != null)
            {
                Debug.Log($"装备物品: {itemComponent.GetItemName()} (ID: {itemComponent.GetItemDataID()})");
            }
        }
    }

    /// <summary>
    /// 测试保存功能
    /// </summary>
    [ContextMenu("测试保存功能")]
    public void TestSaveFunction()
    {
        string jsonData = SerializeToJson();
        Debug.Log($"装备槽 {GetEquipSlotID()} 保存数据:\n{jsonData}");
    }

    /// <summary>
    /// 验证装备槽数据完整性
    /// </summary>
    [ContextMenu("验证数据完整性")]
    public void ValidateEquipSlotData()
    {
        bool isValid = true;
        string validationReport = "=== 装备槽数据验证报告 ===\n";

        // 验证ID
        if (!IsEquipSlotIDValid())
        {
            isValid = false;
            validationReport += "❌ 装备槽ID无效\n";
        }
        else
        {
            validationReport += "✅ 装备槽ID有效\n";
        }

        // 验证装备物品
        if (equippedItem != null)
        {
            var itemComponent = equippedItem.GetComponent<InventorySystemItem>();
            if (itemComponent != null && itemComponent.HasValidItemData())
            {
                validationReport += "✅ 装备物品数据有效\n";
            }
            else
            {
                isValid = false;
                validationReport += "❌ 装备物品数据无效\n";
            }
        }
        else
        {
            validationReport += "ℹ️ 无装备物品\n";
        }

        // 验证动态网格
        if (currentBackpackGrid != null || currentTacticalRigGrid != null)
        {
            validationReport += "✅ 动态网格已实例化\n";
        }
        else
        {
            validationReport += "ℹ️ 无动态网格\n";
        }

        validationReport += $"\n总体状态: {(isValid ? "✅ 数据完整" : "❌ 数据存在问题")}";
        Debug.Log(validationReport);
    }

    #region 动态网格保存系统集成

    /// <summary>
    /// 初始化保存系统引用
    /// </summary>
    private void InitializeSaveSystemReference()
    {
        if (saveManager == null)
        {
            saveManager = SaveManager.Instance;
            if (saveManager == null)
            {
                Debug.LogWarning($"EquipSlot {equipSlotID}: SaveManager实例未找到，动态网格将无法自动注册到保存系统");
            }
        }
    }

    /// <summary>
    /// 强制保存当前网格数据到保存系统
    /// </summary>
    /// <param name="gridObject">网格游戏对象</param>
    /// <param name="gridType">网格类型</param>
    private void SaveCurrentGridData(GameObject gridObject, string gridType)
    {
        if (gridObject == null)
        {
            Debug.LogWarning($"EquipSlot {equipSlotID}: 尝试保存空的网格对象数据");
            return;
        }

        // 确保保存系统引用已初始化
        InitializeSaveSystemReference();

        if (saveManager == null)
        {
            Debug.LogError($"EquipSlot {equipSlotID}: SaveManager不可用，无法保存网格数据");
            return;
        }

        // 获取网格的ISaveable组件
        ISaveable saveableGrid = gridObject.GetComponent<ISaveable>();
        if (saveableGrid == null)
        {
            Debug.LogWarning($"EquipSlot {equipSlotID}: 网格对象 {gridObject.name} 未实现ISaveable接口，无法保存数据");
            return;
        }

        try
        {
            // 强制触发保存操作
            string gridID = saveableGrid.GetSaveID();
            if (!string.IsNullOrEmpty(gridID))
            {
                // 如果网格已注册到保存系统，强制保存其当前状态
                if (saveManager.IsObjectRegistered(saveableGrid))
                {
                    // 标记对象已变化，然后触发增量保存
                    saveManager.MarkObjectChanged(gridID);

                    // 启动增量保存协程（只保存变化的对象）
                    saveManager.SaveIncremental("auto_save");

                    Debug.Log($"EquipSlot {equipSlotID}: 已强制保存 {gridType} 网格数据，ID: {gridID}");
                }
                else
                {
                    Debug.LogWarning($"EquipSlot {equipSlotID}: {gridType} 网格未注册到保存系统，无法保存数据");
                }
            }
            else
            {
                Debug.LogWarning($"EquipSlot {equipSlotID}: {gridType} 网格SaveID为空，无法保存数据");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"EquipSlot {equipSlotID}: 保存 {gridType} 网格数据时发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 将动态创建的网格注册到保存系统
    /// </summary>
    /// <param name="gridComponent">网格组件（必须实现ISaveable接口）</param>
    /// <param name="gridType">网格类型（"Backpack" 或 "TacticalRig"）</param>
    private void RegisterDynamicGridToSaveSystem(Component gridComponent, string gridType)
    {
        Debug.Log($"[EquipSlot] 开始注册动态网格到保存系统: {gridType}");

        if (gridComponent == null)
        {
            Debug.LogError($"[EquipSlot] {equipSlotID}: 尝试注册空的网格组件到保存系统");
            return;
        }

        // 确保保存系统引用已初始化
        InitializeSaveSystemReference();

        if (saveManager == null)
        {
            Debug.LogError($"[EquipSlot] {equipSlotID}: SaveManager不可用，无法注册动态网格 {gridType}");
            return;
        }

        // 检查组件是否实现ISaveable接口
        ISaveable saveableGrid = gridComponent as ISaveable;
        if (saveableGrid == null)
        {
            Debug.LogError($"[EquipSlot] {equipSlotID}: 网格组件 {gridComponent.name} 未实现ISaveable接口，类型: {gridComponent.GetType().Name}");
            return;
        }

        Debug.Log($"[EquipSlot] 网格组件类型验证通过: {gridComponent.GetType().Name}");

        try
        {
            // 获取当前SaveID
            string currentSaveId = saveableGrid.GetSaveID();
            Debug.Log($"[EquipSlot] 当前SaveID: '{currentSaveId}'");

            // 确保网格有有效的保存ID
            if (!saveableGrid.IsSaveIDValid())
            {
                Debug.Log($"[EquipSlot] SaveID无效，生成新ID");
                saveableGrid.GenerateNewSaveID();
                string newSaveId = saveableGrid.GetSaveID();
                Debug.Log($"[EquipSlot] 为动态网格 {gridType} 生成新的保存ID: {newSaveId}");
            }
            else
            {
                Debug.Log($"[EquipSlot] SaveID有效: {currentSaveId}");
            }

            // 检查是否已经注册
            if (saveManager.IsObjectRegistered(saveableGrid))
            {
                Debug.LogWarning($"[EquipSlot] 网格已经注册到SaveManager: {saveableGrid.GetSaveID()}");
                return;
            }

            // 注册到保存系统
            bool registrationResult = saveManager.RegisterSaveable(saveableGrid);
            Debug.Log($"[EquipSlot] SaveManager注册结果: {registrationResult}");

            if (registrationResult)
            {
                // 添加到本地注册列表
                if (!registeredDynamicGrids.Contains(saveableGrid))
                {
                    registeredDynamicGrids.Add(saveableGrid);
                    Debug.Log($"[EquipSlot] 已添加到本地注册列表");
                }

                // 验证注册结果
                bool isRegistered = saveManager.IsObjectRegistered(saveableGrid);
                Debug.Log($"[EquipSlot] 注册验证结果: {isRegistered}");

                if (isRegistered)
                {
                    Debug.Log($"[EquipSlot] 动态网格 {gridType} 已成功注册到保存系统，ID: {saveableGrid.GetSaveID()}");
                }
                else
                {
                    Debug.LogError($"[EquipSlot] 注册验证失败，网格未在SaveManager中找到");
                }
            }
            else
            {
                Debug.LogError($"[EquipSlot] SaveManager注册失败");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[EquipSlot] {equipSlotID}: 注册动态网格 {gridType} 到保存系统时发生错误: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// 从保存系统注销动态网格
    /// </summary>
    /// <param name="gridObject">网格游戏对象</param>
    private void UnregisterDynamicGridFromSaveSystem(GameObject gridObject)
    {
        if (gridObject == null)
        {
            Debug.LogWarning($"EquipSlot {equipSlotID}: 尝试注销空的网格对象");
            return;
        }

        // 获取网格的ISaveable组件
        ISaveable saveableGrid = gridObject.GetComponent<ISaveable>();
        if (saveableGrid == null)
        {
            Debug.LogWarning($"EquipSlot {equipSlotID}: 网格对象 {gridObject.name} 未实现ISaveable接口，无需从保存系统注销");
            return;
        }

        try
        {
            // 从保存系统注销
            if (saveManager != null)
            {
                saveManager.UnregisterSaveable(saveableGrid);
                Debug.Log($"EquipSlot {equipSlotID}: 动态网格 {gridObject.name} 已从保存系统注销，ID: {saveableGrid.GetSaveID()}");
            }

            // 从本地注册列表移除
            if (registeredDynamicGrids.Contains(saveableGrid))
            {
                registeredDynamicGrids.Remove(saveableGrid);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"EquipSlot {equipSlotID}: 从保存系统注销动态网格 {gridObject.name} 时发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 清理所有已注册的动态网格
    /// </summary>
    private void CleanupRegisteredDynamicGrids()
    {
        if (saveManager == null || registeredDynamicGrids.Count == 0)
        {
            return;
        }

        try
        {
            // 注销所有已注册的动态网格
            foreach (var grid in registeredDynamicGrids.ToArray())
            {
                if (grid != null)
                {
                    saveManager.UnregisterSaveable(grid);
                    Debug.Log($"EquipSlot {equipSlotID}: 清理时注销动态网格，ID: {grid.GetSaveID()}");
                }
            }

            registeredDynamicGrids.Clear();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"EquipSlot {equipSlotID}: 清理已注册动态网格时发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 从保存系统恢复网格数据
    /// </summary>
    /// <param name="gridComponent">网格组件</param>
    /// <param name="gridType">网格类型</param>
    private void RestoreGridDataFromSaveSystem(Component gridComponent, string gridType)
    {
        if (gridComponent == null || saveManager == null)
        {
            return;
        }

        ISaveable saveableGrid = gridComponent as ISaveable;
        if (saveableGrid == null)
        {
            Debug.LogWarning($"EquipSlot {equipSlotID}: 网格组件 {gridComponent.name} 未实现ISaveable接口，无法恢复数据");
            return;
        }

        try
        {
            // 生成与之前相同的保存ID模式来查找数据
            string gridID = saveableGrid.GetSaveID();

            // 尝试从保存系统加载数据
            if (saveManager.HasSaveData(gridID))
            {
                string saveDataJson = saveManager.LoadSaveData(gridID);
                if (!string.IsNullOrEmpty(saveDataJson))
                {
                    // 根据网格类型进行数据恢复
                    if (gridType == "Backpack" && gridComponent is BackpackItemGrid backpackGrid)
                    {
                        var saveData = JsonUtility.FromJson<BackpackItemGrid.BackpackGridSaveData>(saveDataJson);
                        if (saveData != null)
                        {
                            backpackGrid.LoadSaveData(saveData);
                            Debug.Log($"EquipSlot {equipSlotID}: 成功恢复背包网格数据，ID: {gridID}");
                        }
                    }
                    else if (gridType == "TacticalRig" && gridComponent is TactiaclRigItemGrid tacticalRigGrid)
                    {
                        var saveData = JsonUtility.FromJson<TactiaclRigItemGrid.TacticalRigGridSaveData>(saveDataJson);
                        if (saveData != null)
                        {
                            tacticalRigGrid.LoadSaveData(saveData);
                            Debug.Log($"EquipSlot {equipSlotID}: 成功恢复战术挂具网格数据，ID: {gridID}");
                        }
                    }
                }
            }
            else
            {
                Debug.Log($"EquipSlot {equipSlotID}: 未找到网格 {gridType} 的保存数据，使用默认状态");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"EquipSlot {equipSlotID}: 恢复网格 {gridType} 数据时发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 延迟注册网格到保存系统的协程
    /// </summary>
    /// <param name="gridComponent">网格组件</param>
    /// <param name="gridType">网格类型</param>
    /// <param name="itemName">物品名称</param>
    /// <returns></returns>
    private IEnumerator DelayedGridRegistration(Component gridComponent, string gridType, string itemName)
    {
        Debug.Log($"[EquipSlot] 开始延迟注册 {gridType} 网格: {itemName}");

        // 等待一帧，确保网格完全初始化
        yield return null;

        // 再等待一小段时间，确保所有初始化完成
        yield return new WaitForSeconds(0.1f);

        try
        {
            // 验证网格状态
            if (gridComponent == null)
            {
                Debug.LogError($"[EquipSlot] {gridType} 网格组件为空，无法注册");
                yield break;
            }

            // 检查ISaveable接口实现
            if (!(gridComponent is ISaveable saveable))
            {
                Debug.LogError($"[EquipSlot] {gridType} 网格未实现ISaveable接口");
                yield break;
            }

            // 验证SaveID
            string saveId = saveable.GetSaveID();
            if (string.IsNullOrEmpty(saveId))
            {
                Debug.LogWarning($"[EquipSlot] {gridType} 网格SaveID为空，尝试生成新ID");
                saveable.GenerateNewSaveID();
                saveId = saveable.GetSaveID();
            }

            Debug.Log($"[EquipSlot] {gridType} 网格SaveID: {saveId}");

            // 检查GridISaveableInitializer
            var initializer = gridComponent.GetComponent<GridISaveableInitializer>();
            if (initializer != null)
            {
                Debug.Log($"[EquipSlot] {gridType} 网格初始化器状态已检查");
            }

            // 自动注册到保存系统
            RegisterDynamicGridToSaveSystem(gridComponent, gridType);

            // 验证注册结果
            var saveManager = SaveManager.Instance;
            if (saveManager != null && saveManager.IsObjectRegistered(saveable))
            {
                Debug.Log($"[EquipSlot] {gridType} 网格注册成功: {saveId}");

                // 尝试从保存系统恢复网格数据
                RestoreGridDataFromSaveSystem(gridComponent, gridType);
                Debug.Log($"[EquipSlot] {gridType} 网格数据恢复完成: {itemName}");
            }
            else
            {
                Debug.LogError($"[EquipSlot] {gridType} 网格注册失败，SaveManager中未找到对象");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[EquipSlot] {gridType} 网格注册过程中发生异常: {ex.Message}\n{ex.StackTrace}");
        }
    }

    #endregion
}

/// 用于存储物品原始大小数据的组件
public class ItemSizeData : MonoBehaviour
{
    [HideInInspector] public Vector2 originalSize;
    [HideInInspector] public Vector3 originalScale;
}
