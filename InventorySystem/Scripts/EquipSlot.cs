using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

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
            // 先清理现有的网格
            if (currentBackpackGrid != null)
            {
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

                // 获取BackpackItemGrid组件并设置数据
                BackpackItemGrid backpackGrid = currentBackpackGrid.GetComponent<BackpackItemGrid>();
                if (backpackGrid != null)
                {
                    backpackGrid.SetBackpackData(item.Data);
                    Debug.Log($"背包 {item.Data.itemName} 已装备，网格已实例化，尺寸: {item.Data.CellH}x{item.Data.CellV}");
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
            // 先清理现有的网格
            if (currentTacticalRigGrid != null)
            {
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

                // 获取TactiaclRigItemGrid组件并设置数据
                TactiaclRigItemGrid tacticalRigGrid = currentTacticalRigGrid.GetComponent<TactiaclRigItemGrid>();
                if (tacticalRigGrid != null)
                {
                    tacticalRigGrid.SetTacticalRigData(item.Data);
                    Debug.Log($"战术挂具 {item.Data.itemName} 已装备，网格已实例化，尺寸: {item.Data.CellH}x{item.Data.CellV}");
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
                DestroyImmediate(currentBackpackGrid);
                currentBackpackGrid = null;
                Debug.Log("背包已卸下，网格已销毁");
            }
        }
        else if (itemComponent.Data.itemCategory == InventorySystemItemCategory.TacticalRig)
        {
            if (currentTacticalRigGrid != null)
            {
                DestroyImmediate(currentTacticalRigGrid);
                currentTacticalRigGrid = null;
                Debug.Log("战术挂具已卸下，网格已销毁");
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
    /// 清理高亮资源
    /// </summary>
    private void OnDestroy()
    {
        if (currentHighlight != null)
        {
            Destroy(currentHighlight);
        }
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
}

/// 用于存储物品原始大小数据的组件
public class ItemSizeData : MonoBehaviour
{
    [HideInInspector] public Vector2 originalSize;
    [HideInInspector] public Vector3 originalScale;
}
