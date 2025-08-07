using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;


[System.Serializable]
public class ItemSpawnCategory
{
    [FieldLabel("分类名称")] public string categoryName;
    [FieldLabel("物品列表")] public List<ItemData> items = new List<ItemData>();
    [FieldLabel("启用此分类")] public bool enabled = true;
    [FieldLabel("生成数量")] public int spawnCount = -1;
}

public class ItemSpawner : MonoBehaviour
{
    [FieldLabel("物品工厂")] public ItemFactory itemFactory;
    [FieldLabel("物品网格")] public ItemGrid itemGrid;
    [FieldLabel("物品预制体")] public GameObject itemPrefab;

    [Header("分类生成设置")]
    [FieldLabel("物品分类列表")] public List<ItemSpawnCategory> itemCategories = new List<ItemSpawnCategory>();
    [FieldLabel("自动加载分类")] public bool autoLoadCategories = true;

    [Header("分类控制开关")]
    [FieldLabel("生成武器")] public bool spawnWeapons = true;
    [FieldLabel("生成护甲")] public bool spawnArmor = true;
    [FieldLabel("生成头盔")] public bool spawnHelmets = true;
    [FieldLabel("生成胸挂")] public bool spawnChestRigs = true;
    [FieldLabel("生成背包")] public bool spawnBackpacks = true;
    [FieldLabel("生成子弹")] public bool spawnBullets = true;
    [FieldLabel("生成食物饮料")] public bool spawnFoodDrink = true;
    [FieldLabel("生成止血用品")] public bool spawnHemostatic = true;
    [FieldLabel("生成镇静用品")] public bool spawnSedactive = true;
    [FieldLabel("生成治疗用品")] public bool spawnTreatment = true;
    [FieldLabel("生成情报")] public bool spawnIntelligence = true;
    [FieldLabel("生成货币")] public bool spawnMoney = true;

    [Header("生成设置")]
    [FieldLabel("大物品优先尺寸排序")] public bool sortBySize = true;
    [FieldLabel("按顺序放置")] public bool sequentialPlacement = true;
    [FieldLabel("最大随机步数")] public int maxAttempts = 200;

    private void Start()
    {
        if (autoLoadCategories)
        {
            LoadItemCategories();
        }
    }

    [ContextMenu("自动加载物品分类")]
    public void LoadItemCategories()
    {
        itemCategories.Clear();

        // 定义分类文件夹映射
        Dictionary<string, string> categoryFolders = new Dictionary<string, string>()
        {
            { "武器", "Gun枪械" },
            { "护甲", "Armor护甲" },
            { "头盔", "Helmet头盔" },
            { "胸挂", "chest_rigs胸挂" },
            { "背包", "Backpack背包" },
            { "子弹", "Bullet子弹" },
            { "食物饮料", "Food&Drink" },
            { "止血用品", "Hemostatic止血" },
            { "镇静用品", "Sedactive镇静" },
            { "治疗用品", "Treatment治疗" },
            { "情报", "Intelligence情报" },
            { "货币", "Money货币" }
        };

        foreach (var category in categoryFolders)
        {
            ItemSpawnCategory newCategory = new ItemSpawnCategory()
            {
                categoryName = category.Key,
                enabled = true,
                spawnCount = -1
            };

            // 加载该分类下的所有物品
            ItemData[] items = Resources.LoadAll<ItemData>($"Abandoned/ItemData/{category.Value}");
            newCategory.items.AddRange(items);

            if (newCategory.items.Count > 0)
            {
                itemCategories.Add(newCategory);
                Debug.Log($"加载分类 '{category.Key}': {newCategory.items.Count} 个物品");
            }
        }

        Debug.Log($"总共加载了 {itemCategories.Count} 个物品分类");
    }

    [ContextMenu("分类生成物品")]
    public void SpawnItemsByCategory()
    {
        if (itemGrid == null || itemPrefab == null || itemCategories == null || itemCategories.Count == 0)
        {
            Debug.LogError("缺少必要引用或分类列表为空");
            return;
        }

        // 清空现有物品
        ClearAllItems();

        // 收集要生成的物品
        List<ItemData> itemsToSpawn = new List<ItemData>();

        foreach (var category in itemCategories)
        {
            if (!category.enabled || category.items.Count == 0) continue;

            // 检查分类控制开关
            if (!IsCategoryEnabled(category.categoryName)) continue;

            List<ItemData> categoryItems = new List<ItemData>(category.items);

            // 如果指定了生成数量
            if (category.spawnCount > 0 && category.spawnCount < categoryItems.Count)
            {
                // 随机选择指定数量的物品
                categoryItems = categoryItems.OrderBy(x => UnityEngine.Random.value).Take(category.spawnCount).ToList();
            }

            itemsToSpawn.AddRange(categoryItems);
            Debug.Log($"分类 '{category.categoryName}': 准备生成 {categoryItems.Count} 个物品");
        }

        // 按尺寸排序（如果启用）
        if (sortBySize)
        {
            itemsToSpawn = itemsToSpawn.OrderByDescending(item => item.width * item.height).ToList();
        }

        // 生成物品
        int successCount = 0;
        int totalArea = itemsToSpawn.Sum(item => item.width * item.height);
        int gridArea = itemGrid.GridSizeWidth * itemGrid.GridSizeHeight;

        if (totalArea > gridArea)
        {
            Debug.LogWarning($"物品总面积({totalArea})超过背包容量({gridArea})，部分物品可能无法放置");
        }

        foreach (var data in itemsToSpawn)
        {
            if (TrySpawnItem(data))
            {
                successCount++;
            }
            else
            {
                Debug.LogWarning($"无法放置物品：{data.name}");
            }
        }

        Debug.Log($"成功生成 {successCount}/{itemsToSpawn.Count} 个物品");
    }

    private bool TrySpawnItem(ItemData data)
    {
        BaseItem newItem = itemFactory.CreateItem(data);

        if (newItem == null)
        {
            Debug.LogError($"无法创建物品: {data.name}");
            return false;
        }

        bool placementSuccess;
        if (sequentialPlacement)
        {
            placementSuccess = itemGrid.TryPlaceItemSequentially(newItem);
        }
        else
        {
            placementSuccess = itemGrid.TryPlaceItemRandomly(newItem, maxAttempts);
        }

        if (placementSuccess)
        {
            newItem.transform.SetParent(itemGrid.transform, false);
            newItem.transform.SetAsLastSibling();
            return true;
        }
        else
        {
            Destroy(newItem.gameObject);
            return false;
        }
    }

    private void ClearAllItems()
    {
        for (int i = itemGrid.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = itemGrid.transform.GetChild(i);
            if (child.GetComponent<BaseItem>() != null)
            {
                DestroyImmediate(child.gameObject);
            }
        }
        itemGrid.ClearGrid();
    }

    [ContextMenu("清空背包")]
    public void ClearBackpack()
    {
        ClearAllItems();
        Debug.Log("背包已清空");
    }

    private bool IsCategoryEnabled(string categoryName)
    {
        switch (categoryName)
        {
            case "武器": return spawnWeapons;
            case "护甲": return spawnArmor;
            case "头盔": return spawnHelmets;
            case "胸挂": return spawnChestRigs;
            case "背包": return spawnBackpacks;
            case "子弹": return spawnBullets;
            case "食物饮料": return spawnFoodDrink;
            case "止血用品": return spawnHemostatic;
            case "镇静用品": return spawnSedactive;
            case "治疗用品": return spawnTreatment;
            case "情报": return spawnIntelligence;
            case "货币": return spawnMoney;
            default: return true;
        }
    }
}