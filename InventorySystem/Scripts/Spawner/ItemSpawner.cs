using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ItemSpawner : BaseItemSpawn
{
    [Header("物品类型选择")]
    [SerializeField][FieldLabel("头盔")] private bool spawnHelmet = true;
    [SerializeField][FieldLabel("护甲")] private bool spawnArmor = true;
    [SerializeField][FieldLabel("战术挂具")] private bool spawnTacticalRig = true;
    [SerializeField][FieldLabel("背包")] private bool spawnBackpack = true;
    [SerializeField][FieldLabel("武器")] private bool spawnWeapon = true;
    [SerializeField][FieldLabel("弹药")] private bool spawnAmmunition = true;
    [SerializeField][FieldLabel("食物")] private bool spawnFood = true;
    [SerializeField][FieldLabel("饮料")] private bool spawnDrink = true;
    [SerializeField][FieldLabel("治疗药物")] private bool spawnHealing = true;
    [SerializeField][FieldLabel("止血药物")] private bool spawnHemostatic = true;
    [SerializeField][FieldLabel("镇静药物")] private bool spawnSedative = true;
    [SerializeField][FieldLabel("情报")] private bool spawnIntelligence = true;
    [SerializeField][FieldLabel("货币")] private bool spawnCurrency = true;

    // 生成多个随机物品
    public override void SpawnRandomItems()
    {
        // 每次生成物品前都重新检测InventoryGrid，确保在新场景中能正常工作
        DetectInventoryGrid();

        if (targetGrid == null)
        {
            Debug.LogError("未找到InventoryGrid！无法生成物品。");
            return;
        }

        if (allItemData.Count == 0)
        {
            Debug.LogError("没有加载任何物品数据！请先调用LoadAllItemsFromFolders()");
            return;
        }

        List<InventorySystemItemDataSO> availableItems = GetAvailableItemsByCategory();
        if (availableItems.Count == 0)
        {
            Debug.LogWarning("没有选择任何物品类型！");
            return;
        }

        // 检查网格是否已初始化
        if (gridOccupancy == null)
        {
            Debug.LogError("网格占用数组未初始化！请检查网格设置。");
            return;
        }

        // 计算可用空间
        int totalCells = gridOccupancy.GetLength(0) * gridOccupancy.GetLength(1);
        int occupiedCells = CountOccupiedCells();
        int availableCells = totalCells - occupiedCells;

        if (showDebugInfo)
        {
            Debug.Log($"网格状态: 总格子 {totalCells}, 已占用 {occupiedCells}, 可用 {availableCells}");
        }

        // 按物品大小排序，优先生成小物品
        availableItems = availableItems.OrderBy(item => item.width * item.height).ToList();

        int successCount = 0;
        int attemptCount = 0;
        int maxAttempts = spawnCount * 5; // 增加最大尝试次数
        int consecutiveFailures = 0;
        int maxConsecutiveFailures = 10;

        while (successCount < spawnCount && attemptCount < maxAttempts && consecutiveFailures < maxConsecutiveFailures)
        {
            attemptCount++;

            // 基于珍贵程度选择物品
            InventorySystemItemDataSO randomItemData = SelectItemByRarity(availableItems);
            GameObject prefab = FindPrefabForItemData(randomItemData);

            if (prefab != null)
            {
                GameObject spawnedItem = SpawnItemAtRandomPosition(prefab, randomItemData);
                if (spawnedItem != null)
                {
                    successCount++;
                    consecutiveFailures = 0; // 重置连续失败计数
                }
                else
                {
                    consecutiveFailures++;
                }
            }
            else
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"物品 '{randomItemData.itemName}' 没有对应的预制体");
                }
                consecutiveFailures++;
            }
        }

        if (showDebugInfo)
        {
            Debug.Log($"生成完成: 成功生成 {successCount}/{spawnCount} 个物品，尝试次数: {attemptCount}");
            if (consecutiveFailures >= maxConsecutiveFailures)
            {
                Debug.LogWarning("由于连续失败次数过多，提前结束生成。可能是网格空间不足或预制体缺失。");
            }
            if (showGridOccupancy)
            {
                PrintGridOccupancy();
            }
        }
    }

    // CountOccupiedCells方法已在BaseItemSpawn中实现

    // 根据选择的类型获取可用物品列表
    protected override List<InventorySystemItemDataSO> GetAvailableItemsByCategory()
    {
        List<InventorySystemItemDataSO> availableItems = new List<InventorySystemItemDataSO>();

        foreach (var itemData in allItemData)
        {
            if (itemData == null) continue;

            bool shouldInclude = false;
            switch (itemData.itemCategory)
            {
                case InventorySystemItemCategory.Helmet:
                    shouldInclude = spawnHelmet;
                    break;
                case InventorySystemItemCategory.Armor:
                    shouldInclude = spawnArmor;
                    break;
                case InventorySystemItemCategory.TacticalRig:
                    shouldInclude = spawnTacticalRig;
                    break;
                case InventorySystemItemCategory.Backpack:
                    shouldInclude = spawnBackpack;
                    break;
                case InventorySystemItemCategory.Weapon:
                    shouldInclude = spawnWeapon;
                    break;
                case InventorySystemItemCategory.Ammunition:
                    shouldInclude = spawnAmmunition;
                    break;
                case InventorySystemItemCategory.Food:
                    shouldInclude = spawnFood;
                    break;
                case InventorySystemItemCategory.Drink:
                    shouldInclude = spawnDrink;
                    break;
                case InventorySystemItemCategory.Healing:
                    shouldInclude = spawnHealing;
                    break;
                case InventorySystemItemCategory.Hemostatic:
                    shouldInclude = spawnHemostatic;
                    break;
                case InventorySystemItemCategory.Sedative:
                    shouldInclude = spawnSedative;
                    break;
                case InventorySystemItemCategory.Intelligence:
                    shouldInclude = spawnIntelligence;
                    break;
                case InventorySystemItemCategory.Currency:
                    shouldInclude = spawnCurrency;
                    break;
            }

            if (shouldInclude)
            {
                availableItems.Add(itemData);
            }
        }

        return availableItems;
    }

    // 调试信息：显示当前状态
    [ContextMenu("显示调试信息")]
    public void ShowDebugInfo()
    {
        Debug.Log("=== ItemSpawner 调试信息 ===");

        // 网格信息
        if (targetGrid != null)
        {
            Debug.Log($"目标网格: {targetGrid.gameObject.name}");
            if (gridOccupancy != null)
            {
                Debug.Log($"网格尺寸: {gridOccupancy.GetLength(0)}x{gridOccupancy.GetLength(1)}");
            }
            else
            {
                Debug.LogWarning("网格占用数组未初始化！");
            }
        }
        else
        {
            Debug.LogError("未找到目标网格！");
        }

        // 物品数据信息
        Debug.Log($"已加载物品数据: {allItemData.Count} 个");
        foreach (var itemData in allItemData.Take(5)) // 只显示前5个
        {
            Debug.Log($"  - {itemData.itemName} ({itemData.width}x{itemData.height})");
        }
        if (allItemData.Count > 5)
        {
            Debug.Log($"  ... 还有 {allItemData.Count - 5} 个物品数据");
        }

        // 预制体信息
        Debug.Log($"已加载预制体: {itemPrefabDict.Count} 个");
        foreach (var kvp in itemPrefabDict.Take(5)) // 只显示前5个
        {
            Debug.Log($"  - Key: '{kvp.Key}' -> {kvp.Value.name}");
        }
        if (itemPrefabDict.Count > 5)
        {
            Debug.Log($"  ... 还有 {itemPrefabDict.Count - 5} 个预制体");
        }

        // 检查匹配问题
        Debug.Log("=== 匹配检查 ===");
        int matchedCount = 0;
        int unmatchedCount = 0;

        foreach (var itemData in allItemData)
        {
            string dataKey = GetItemKey(itemData.itemName);
            if (itemPrefabDict.ContainsKey(dataKey))
            {
                matchedCount++;
            }
            else
            {
                unmatchedCount++;
                Debug.LogWarning($"未匹配: '{itemData.itemName}' -> Key: '{dataKey}'");
            }
        }

        Debug.Log($"匹配成功: {matchedCount} 个，未匹配: {unmatchedCount} 个");

        // 珍贵程度统计信息
        Debug.Log("=== 珍贵程度统计 ===");
        ShowRarityStatistics();

        // 提供解决方案建议
        if (unmatchedCount > 0)
        {
            Debug.Log("=== 解决方案建议 ===");
            Debug.Log("1. 检查预制体文件夹路径是否正确");
            Debug.Log("2. 确保预制体文件名与物品数据名称匹配");
            Debug.Log("3. 可以使用右键菜单 '创建缺失预制体' 来自动创建基础预制体");
            Debug.Log("4. 检查物品数据是否正确加载");
        }

        if (gridOccupancy == null)
        {
            Debug.LogError("网格未初始化！请检查InventoryGrid是否正确设置。");
        }

        Debug.Log("=== 调试信息结束 ===");
    }

    // 显示珍贵程度统计信息
    private void ShowRarityStatistics()
    {
        if (allItemData.Count == 0)
        {
            Debug.Log("没有物品数据可统计");
            return;
        }

        // 按类别分组统计珍贵程度
        var categoryStats = allItemData.GroupBy(item => item.itemCategory)
            .OrderBy(group => group.Key.ToString())
            .ToList();

        foreach (var categoryGroup in categoryStats)
        {
            var category = categoryGroup.Key;
            var items = categoryGroup.ToList();

            Debug.Log($"\n【{GetCategoryDisplayName(category)}】 共 {items.Count} 个物品:");

            // 按珍贵程度分组
            var rarityGroups = items.GroupBy(item => ParseRarityLevel(item.rarity))
                .OrderBy(group => group.Key)
                .ToList();

            foreach (var rarityGroup in rarityGroups)
            {
                int rarityLevel = rarityGroup.Key;
                var rarityItems = rarityGroup.ToList();
                float weight = CalculateRarityWeight(rarityLevel);
                string rarityName = GetRarityDisplayName(rarityLevel);

                Debug.Log($"  {rarityName}(等级{rarityLevel}): {rarityItems.Count}个 - 生成权重: {weight:P0}");

                // 显示具体物品名称（最多显示3个）
                var itemNames = rarityItems.Take(3).Select(item => item.itemName).ToList();
                if (rarityItems.Count > 3)
                {
                    itemNames.Add($"...等{rarityItems.Count}个");
                }
                Debug.Log($"    物品: {string.Join(", ", itemNames)}");
            }
        }

        // 显示总体珍贵程度分布
        Debug.Log("\n【总体珍贵程度分布】:");
        var totalRarityStats = allItemData.GroupBy(item => ParseRarityLevel(item.rarity))
            .OrderBy(group => group.Key)
            .ToList();

        foreach (var rarityGroup in totalRarityStats)
        {
            int rarityLevel = rarityGroup.Key;
            int count = rarityGroup.Count();
            float percentage = (float)count / allItemData.Count * 100f;
            float weight = CalculateRarityWeight(rarityLevel);
            string rarityName = GetRarityDisplayName(rarityLevel);

            Debug.Log($"  {rarityName}: {count}个 ({percentage:F1}%) - 生成权重: {weight:P0}");
        }
    }

    // 获取类别显示名称
    private string GetCategoryDisplayName(InventorySystemItemCategory category)
    {
        switch (category)
        {
            case InventorySystemItemCategory.Helmet: return "头盔";
            case InventorySystemItemCategory.Armor: return "护甲";
            case InventorySystemItemCategory.TacticalRig: return "战术挂具";
            case InventorySystemItemCategory.Backpack: return "背包";
            case InventorySystemItemCategory.Weapon: return "武器";
            case InventorySystemItemCategory.Ammunition: return "弹药";
            case InventorySystemItemCategory.Food: return "食物";
            case InventorySystemItemCategory.Drink: return "饮料";
            case InventorySystemItemCategory.Healing: return "治疗用品";
            case InventorySystemItemCategory.Hemostatic: return "止血剂";
            case InventorySystemItemCategory.Sedative: return "镇静剂";
            case InventorySystemItemCategory.Intelligence: return "情报";
            case InventorySystemItemCategory.Currency: return "货币";
            default: return category.ToString();
        }
    }

    // 获取珍贵程度显示名称
    private string GetRarityDisplayName(int rarityLevel)
    {
        switch (rarityLevel)
        {
            case 1: return "普通";
            case 2: return "稀有";
            case 3: return "珍贵";
            case 4: return "史诗";
            default: return $"未知({rarityLevel})";
        }
    }

    // 创建缺失的预制体（基础版本）
    [ContextMenu("创建缺失预制体")]
    public void CreateMissingPrefabs()
    {
#if UNITY_EDITOR
        int createdCount = 0;

        foreach (var itemData in allItemData)
        {
            string dataKey = GetItemKey(itemData.itemName);
            if (!itemPrefabDict.ContainsKey(dataKey))
            {
                // 创建基础预制体
                GameObject basicPrefab = CreateBasicItemPrefab(itemData);
                if (basicPrefab != null)
                {
                    // 保存预制体
                    string categoryFolder = GetCategoryFolderName(itemData.itemCategory);
                    string prefabFolder = $"{prefabPath}/{categoryFolder}";

                    // 确保文件夹存在
                    if (!UnityEditor.AssetDatabase.IsValidFolder(prefabFolder))
                    {
                        UnityEditor.AssetDatabase.CreateFolder(prefabPath, categoryFolder);
                    }

                    string prefabPath_full = $"{prefabFolder}/{itemData.itemName}.prefab";
                    UnityEditor.PrefabUtility.SaveAsPrefabAsset(basicPrefab, prefabPath_full);

                    // 销毁临时对象
                    DestroyImmediate(basicPrefab);

                    createdCount++;
                    Debug.Log($"创建预制体: {itemData.itemName}");
                }
            }
        }

        if (createdCount > 0)
        {
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log($"成功创建 {createdCount} 个基础预制体！请重新加载物品数据。");

            // 重新加载预制体
            LoadPrefabsFromFolder();
        }
        else
        {
            Debug.Log("没有需要创建的预制体。");
        }
#else
        Debug.LogWarning("此功能仅在编辑器中可用！");
#endif
    }

#if UNITY_EDITOR
    // 创建基础物品预制体
    private GameObject CreateBasicItemPrefab(InventorySystemItemDataSO itemData)
    {
        // 创建根对象
        GameObject root = new GameObject(itemData.itemName);
        root.layer = 5; // UI层

        // 添加RectTransform
        RectTransform rectTransform = root.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(itemData.width * 64, itemData.height * 64); // 假设每格64像素

        // 添加CanvasGroup
        root.AddComponent<CanvasGroup>();

        // 添加InventorySystemItem组件
        var itemComponent = root.AddComponent<InventorySystemItem>();

        // 添加DraggableItem组件
        try
        {
            var dragHandler = root.AddComponent<DraggableItem>();
            // DraggableItem会自动处理与InventorySystemItem的关联
        }
        catch
        {
            Debug.LogWarning($"无法添加DraggableItem到 {itemData.itemName}");
        }

        // 创建背景
        GameObject background = new GameObject("ItemBackground");
        background.transform.SetParent(root.transform);
        background.layer = 5;

        RectTransform bgRect = background.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.one * 0.5f;
        bgRect.anchorMax = Vector2.one * 0.5f;
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.sizeDelta = rectTransform.sizeDelta;

        var bgImage = background.AddComponent<UnityEngine.UI.RawImage>();
        bgImage.color = new Color(0.345f, 0.231f, 0.502f, 0.8f);

        // 创建图标
        GameObject icon = new GameObject("ItemSprite");
        icon.transform.SetParent(root.transform);
        icon.layer = 5;

        RectTransform iconRect = icon.AddComponent<RectTransform>();
        iconRect.anchorMin = Vector2.one * 0.5f;
        iconRect.anchorMax = Vector2.one * 0.5f;
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = rectTransform.sizeDelta;

        var iconImage = icon.AddComponent<UnityEngine.UI.Image>();
        iconImage.sprite = itemData.itemIcon;
        iconImage.preserveAspect = true;

        // 创建数据持有者
        GameObject dataHolder = new GameObject("ItemScriptableObject");
        dataHolder.transform.SetParent(root.transform);
        dataHolder.layer = 5;

        RectTransform dataRect = dataHolder.AddComponent<RectTransform>();
        dataRect.anchorMin = Vector2.one * 0.5f;
        dataRect.anchorMax = Vector2.one * 0.5f;
        dataRect.anchoredPosition = Vector2.zero;
        dataRect.sizeDelta = rectTransform.sizeDelta;

        var dataHolderComponent = dataHolder.AddComponent<ItemDataHolder>();
        dataHolderComponent.SetItemData(itemData);

        // 设置引用
        try
        {
            var itemIconField = dataHolderComponent.GetType().GetField("itemIconImage");
            if (itemIconField != null)
            {
                itemIconField.SetValue(dataHolderComponent, iconImage);
            }

            var backgroundField = dataHolderComponent.GetType().GetField("backgroundImage");
            if (backgroundField != null)
            {
                backgroundField.SetValue(dataHolderComponent, bgImage);
            }
        }
        catch
        {
            Debug.LogWarning($"无法设置 {itemData.itemName} 的组件引用");
        }

        return root;
    }

    // 获取类别文件夹名称
    private string GetCategoryFolderName(InventorySystemItemCategory category)
    {
        switch (category)
        {
            case InventorySystemItemCategory.Helmet: return "头盔Helmet";
            case InventorySystemItemCategory.Armor: return "护甲Armor";
            case InventorySystemItemCategory.TacticalRig: return "战术挂具TacticalRig";
            case InventorySystemItemCategory.Backpack: return "背包Backpack";
            case InventorySystemItemCategory.Weapon: return "武器Weapon";
            case InventorySystemItemCategory.Ammunition: return "弹药Ammunition";
            case InventorySystemItemCategory.Food: return "食物Food";
            case InventorySystemItemCategory.Drink: return "饮料Drink";
            case InventorySystemItemCategory.Healing: return "治疗用品Healing";
            case InventorySystemItemCategory.Hemostatic: return "止血剂Hemostatic";
            case InventorySystemItemCategory.Sedative: return "镇静剂Sedative";
            case InventorySystemItemCategory.Intelligence: return "情报Intelligence";
            case InventorySystemItemCategory.Currency: return "货币Currency";
            default: return "其他Other";
        }
    }
#endif

}