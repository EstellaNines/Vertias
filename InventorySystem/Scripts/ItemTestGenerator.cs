using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ItemTestGenerator : MonoBehaviour
{
    [Header("生成设置")]
    [SerializeField] private string prefabFolderPath = "Assets/InventorySystem/Prefab";
    [SerializeField] private Transform parentTransform; // 生成物品的父对象

    [Header("网格配置")]
    [SerializeField][FieldLabel("网格配置文件")] private GridConfig gridConfig;
    [SerializeField][FieldLabel("使用测试网格尺寸")] private bool useTestGridSize = false;

    [Header("调试信息")]
    [SerializeField] private bool showDebugInfo = true;

    [Header("布局设置")]
    [SerializeField][FieldLabel("每行最大物品数")] private int maxItemsPerRow = 5;

    // 从GridConfig获取配置的属性
    private float CellSize => gridConfig?.cellSize ?? 80f;
    private int GridWidth => useTestGridSize ? (gridConfig?.testGridWidth ?? 50) : (gridConfig?.inventoryWidth ?? 10);
    private int GridHeight => useTestGridSize ? (gridConfig?.testGridHeight ?? 50) : (gridConfig?.inventoryHeight ?? 12);
    private float Spacing => gridConfig?.itemSpacing ?? 5f;
    private int MaxRandomAttempts => gridConfig?.maxRandomAttempts ?? 1000;

    // 已占用的区域列表
    private List<Rect> occupiedAreas = new List<Rect>();

    // 当前生成位置
    private Vector2 currentPosition = Vector2.zero;
    private float currentRowHeight = 0f;
    private int itemsInCurrentRow = 0;

    [Header("随机生成设置")]
    [SerializeField][FieldLabel("随机生成区域宽度")] private float randomAreaWidth = 1000f;
    [SerializeField][FieldLabel("随机生成区域高度")] private float randomAreaHeight = 800f;
    [SerializeField][FieldLabel("随机生成最大尝试次数")] private int maxRandomAttempts = 50;
    [SerializeField][FieldLabel("允许随机重叠")] private bool allowRandomOverlap = false;

    [Header("随机生成数量控制")]
    [SerializeField][FieldLabel("最小生成数量")] private int minRandomCount = 1;
    [SerializeField][FieldLabel("最大生成数量")] private int maxRandomCount = 10;

    [Header("物品类型筛选")]
    [SerializeField][FieldLabel("启用类型筛选")] private bool enableCategoryFilter = false;
    [SerializeField] private List<InventorySystemItemCategory> selectedCategories = new List<InventorySystemItemCategory>();

    [Header("网格系统集成")]
    [SerializeField][FieldLabel("关联的网格背包")] private ItemGridInventory targetInventory;
    [SerializeField][FieldLabel("网格宽度")] private int gridWidth = 10;  // 改为10
    [SerializeField][FieldLabel("网格高度")] private int gridHeight = 12; // 改为12

    // 网格占用状态记录
    private bool[,] gridOccupied;

    [ContextMenu("生成所有物品测试")]
    public void GenerateAllItemsTest()
    {
        ClearExistingItems();
        ResetGenerationState();

#if UNITY_EDITOR
        // 获取所有预制体
        GameObject[] prefabs = GetAllPrefabsInFolder();

        if (prefabs.Length == 0)
        {
            Debug.LogWarning($"在路径 {prefabFolderPath} 中未找到任何预制体！");
            return;
        }

        Debug.Log($"找到 {prefabs.Length} 个预制体，开始生成测试...");

        // 按尺寸排序（先生成大的物品）
        // 改进的排序策略 - 在GenerateAllItemsTest方法中
        var sortedPrefabs = prefabs
            .OrderByDescending(p => {
                var size = GetPrefabSize(p);
                // 优先考虑大物品，但也考虑形状因子
                float area = size.x * size.y;
                float aspectRatio = Mathf.Max(size.x, size.y) / (float)Mathf.Min(size.x, size.y);
                // 面积大且形状规整的物品优先
                return area * (2f - aspectRatio * 0.1f);
            })
            .ToArray();

        foreach (GameObject prefab in sortedPrefabs)
        {
            GenerateItemAtOptimalPosition(prefab);
        }

        Debug.Log($"物品测试生成完成！共生成 {prefabs.Length} 个物品。");
#else
        Debug.LogWarning("此功能仅在编辑器模式下可用！");
#endif
    }

    // 清除现有物品
    private void ClearExistingItems()
    {
        if (parentTransform == null)
            parentTransform = transform;

        // 清除所有子对象
        for (int i = parentTransform.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                Destroy(parentTransform.GetChild(i).gameObject);
            else
                DestroyImmediate(parentTransform.GetChild(i).gameObject);
#else
            Destroy(parentTransform.GetChild(i).gameObject);
#endif
        }
    }

    // 重置生成状态 - 修复版本
    private void ResetGenerationState()
    {
        occupiedAreas.Clear();
        currentPosition = Vector2.zero;
        currentRowHeight = 0f;
        itemsInCurrentRow = 0;

        // 验证GridConfig
        if (gridConfig == null)
        {
            Debug.LogError("GridConfig 未设置！请在Inspector中分配GridConfig资源。");
            return;
        }

        // 使用GridConfig中的配置初始化网格
        int width = GridWidth;
        int height = GridHeight;
        gridOccupied = new bool[width, height];

        if (showDebugInfo)
        {
            Debug.Log($"使用网格配置: {width}x{height}, 单元格大小: {CellSize}, 间距: {Spacing}");
        }
    }

    // 修复的随机位置查找方法
    private Vector2 FindRandomPosition(Vector2 itemSize)
    {
        if (gridConfig == null)
        {
            Debug.LogError("GridConfig 未设置！");
            return Vector2.zero;
        }

        Vector2Int itemGridSize = new Vector2Int(
            Mathf.CeilToInt(itemSize.x / CellSize),
            Mathf.CeilToInt(itemSize.y / CellSize)
        );

        Vector2Int randomGridPos = Vector2Int.zero;
        int attempts = 0;
        int maxAttempts = MaxRandomAttempts;
        int gridWidth = GridWidth;
        int gridHeight = GridHeight;
        float cellSize = CellSize;

        do
        {
            // 生成网格坐标，确保物品完全在网格内
            int gridX = Random.Range(0, gridWidth - itemGridSize.x + 1);
            int gridY = Random.Range(0, gridHeight - itemGridSize.y + 1);

            randomGridPos = new Vector2Int(gridX, gridY);
            attempts++;

            // 检查网格位置是否可用
            if (allowRandomOverlap || IsGridPositionAvailable(randomGridPos, itemGridSize))
            {
                // 如果不允许重叠，标记网格为已占用
                if (!allowRandomOverlap)
                {
                    MarkGridAsOccupied(randomGridPos, itemGridSize);
                }

                // 转换为世界坐标
                float worldX = randomGridPos.x * cellSize;
                float worldY = -(randomGridPos.y * cellSize);

                return new Vector2(worldX, worldY);
            }

        } while (attempts < maxAttempts);

        // 如果超过最大尝试次数，强制放置（可能重叠）
        if (showDebugInfo)
        {
            Debug.LogWarning($"随机位置生成超过最大尝试次数 ({maxAttempts})，强制放置在网格位置: {randomGridPos}");
        }

        float fallbackX = randomGridPos.x * cellSize;
        float fallbackY = -(randomGridPos.y * cellSize);
        return new Vector2(fallbackX, fallbackY);
    }

    // 检查网格位置是否可用 - 修复版本
    private bool IsGridPositionAvailable(Vector2Int gridPos, Vector2Int itemSize)
    {
        int gridWidth = GridWidth;
        int gridHeight = GridHeight;

        // 边界检查
        if (gridPos.x < 0 || gridPos.y < 0 ||
            gridPos.x + itemSize.x > gridWidth ||
            gridPos.y + itemSize.y > gridHeight)
        {
            return false;
        }

        // 覆盖检查
        for (int x = 0; x < itemSize.x; x++)
        {
            for (int y = 0; y < itemSize.y; y++)
            {
                if (gridOccupied[gridPos.x + x, gridPos.y + y])
                {
                    return false;
                }
            }
        }

        return true;
    }

    // 标记网格为已占用
    private void MarkGridAsOccupied(Vector2Int gridPos, Vector2Int itemSize)
    {
        int gridWidth = GridWidth;
        int gridHeight = GridHeight;

        for (int x = 0; x < itemSize.x; x++)
        {
            for (int y = 0; y < itemSize.y; y++)
            {
                int posX = gridPos.x + x;
                int posY = gridPos.y + y;

                // 边界检查
                if (posX >= 0 && posX < gridWidth && posY >= 0 && posY < gridHeight)
                {
                    gridOccupied[posX, posY] = true;
                }
            }
        }
    }

    // 修复的随机生成方法
    private void GenerateItemAtRandomPosition(GameObject prefab)
    {
        if (gridConfig == null)
        {
            Debug.LogError("GridConfig 未设置！无法生成物品。");
            return;
        }

        Vector2Int itemSize = GetPrefabSize(prefab);
        Vector2 worldSize = new Vector2(itemSize.x * CellSize, itemSize.y * CellSize);

        // 寻找随机生成位置
        Vector2 randomPosition = FindRandomPosition(worldSize);

        // 生成物品
#if UNITY_EDITOR
        GameObject instance;
        if (Application.isPlaying)
            instance = Instantiate(prefab, parentTransform);
        else
            instance = PrefabUtility.InstantiatePrefab(prefab, parentTransform) as GameObject;
#else
        GameObject instance = Instantiate(prefab, parentTransform);
#endif

        // 手动更新物品显示
        ItemDataHolder dataHolder = instance.GetComponentInChildren<ItemDataHolder>();
        if (dataHolder != null)
        {
            dataHolder.UpdateItemDisplay();
        }

        // 设置位置
        RectTransform rectTransform = instance.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = randomPosition;
            rectTransform.sizeDelta = worldSize;
        }

        // 记录占用区域（用于调试显示）
        Rect occupiedRect = new Rect(randomPosition.x, randomPosition.y - worldSize.y,
                                   worldSize.x, worldSize.y);
        occupiedAreas.Add(occupiedRect);

        if (showDebugInfo)
        {
            Vector2Int gridPos = new Vector2Int(
                Mathf.FloorToInt(randomPosition.x / CellSize),
                Mathf.FloorToInt(-randomPosition.y / CellSize)
            );
            Debug.Log($"随机生成物品: {prefab.name}, 网格位置: {gridPos}, 尺寸: {itemSize}, 世界位置: {randomPosition}");
        }
    }

    // 在最优位置生成物品
    private void GenerateItemAtOptimalPosition(GameObject prefab)
    {
        Vector2Int itemSize = GetPrefabSize(prefab);
        Vector2 worldSize = new Vector2(itemSize.x * CellSize, itemSize.y * CellSize); // 使用CellSize属性

        // 寻找最优生成位置
        Vector2 optimalPosition = FindOptimalPosition(worldSize);

        // 生成物品
#if UNITY_EDITOR
        GameObject instance;
        if (Application.isPlaying)
            instance = Instantiate(prefab, parentTransform);
        else
            instance = PrefabUtility.InstantiatePrefab(prefab, parentTransform) as GameObject;
#else
        GameObject instance = Instantiate(prefab, parentTransform);
#endif

        // 手动更新物品显示（重要！）
        ItemDataHolder dataHolder = instance.GetComponentInChildren<ItemDataHolder>();
        if (dataHolder != null)
        {
            dataHolder.UpdateItemDisplay();
        }

        // 设置位置
        RectTransform rectTransform = instance.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = optimalPosition;
            rectTransform.sizeDelta = worldSize;
        }

        // 记录占用区域
        Rect occupiedRect = new Rect(optimalPosition.x, optimalPosition.y - worldSize.y,
                                   worldSize.x + Spacing, worldSize.y + Spacing); // 使用Spacing属性
        occupiedAreas.Add(occupiedRect);

        if (showDebugInfo)
        {
            Debug.Log($"生成物品: {prefab.name}, 尺寸: {itemSize}, 位置: {optimalPosition}");
        }
    }

    // 寻找最优生成位置 - 修复版本
    private Vector2 FindOptimalPosition(Vector2 itemSize)
    {
        // 首先尝试简单的行布局逻辑（更可靠）
        Vector2 rowPosition = FindNextRowPosition(itemSize);
        if (rowPosition != Vector2.negativeInfinity)
        {
            return rowPosition;
        }

        // 如果行布局失败，尝试在现有布局中寻找空隙
        Vector2 bestPosition = FindBestFitPosition(itemSize);
        if (bestPosition != Vector2.negativeInfinity)
        {
            return bestPosition;
        }

        // 最后的后备方案：强制放置在下一行开始位置
        MoveToNextRow();
        return currentPosition;
    }

    // 在现有布局中寻找最佳匹配位置 - 修复版本
    private Vector2 FindBestFitPosition(Vector2 itemSize)
    {
        float bestScore = float.MaxValue;
        Vector2 bestPosition = Vector2.negativeInfinity;

        // 扩大搜索范围并减小步长
        float maxSearchWidth = maxItemsPerRow * (CellSize + Spacing) * 1.5f; // 使用CellSize和Spacing属性
        float maxSearchHeight = Mathf.Max(1000f, Mathf.Abs(currentPosition.y) + itemSize.y + Spacing * 5); // 使用Spacing属性

        // 使用更小的步长进行搜索
        float stepSize = CellSize * 0.5f; // 使用CellSize属性

        for (float y = 0; y >= -maxSearchHeight; y -= stepSize)
        {
            for (float x = 0; x <= maxSearchWidth - itemSize.x; x += stepSize)
            {
                Vector2 testPosition = new Vector2(x, y);

                if (!IsPositionOccupied(testPosition, itemSize))
                {
                    // 计算位置评分（越小越好）
                    float score = CalculatePositionScore(testPosition, itemSize);

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestPosition = testPosition;
                    }
                }
            }
        }

        return bestPosition;
    }

    // 原有的行布局逻辑 - 修复版本
    private Vector2 FindNextRowPosition(Vector2 itemSize)
    {
        // 检查当前行是否还能放下这个物品
        if (itemsInCurrentRow < maxItemsPerRow)
        {
            // 尝试当前位置
            if (!IsPositionOccupied(currentPosition, itemSize))
            {
                Vector2 position = currentPosition;

                // 更新当前位置和行信息
                currentPosition.x += itemSize.x + Spacing; // 使用Spacing属性
                currentRowHeight = Mathf.Max(currentRowHeight, itemSize.y);
                itemsInCurrentRow++;

                return position;
            }
        }

        // 换到下一行
        MoveToNextRow();

        // 检查新行的位置是否可用
        if (!IsPositionOccupied(currentPosition, itemSize))
        {
            Vector2 newRowPosition = currentPosition;
            currentPosition.x += itemSize.x + Spacing; // 使用Spacing属性
            currentRowHeight = Mathf.Max(currentRowHeight, itemSize.y);
            itemsInCurrentRow++;

            return newRowPosition;
        }

        // 如果新行位置也被占用，返回无效位置
        return Vector2.negativeInfinity;
    }

    // 改进的位置占用检查 - 添加边界检查
    private bool IsPositionOccupied(Vector2 position, Vector2 size)
    {
        // 检查边界
        if (position.x < 0 || position.y > 0)
            return true;

        Rect checkRect = new Rect(position.x, position.y - size.y, size.x, size.y);

        foreach (Rect occupied in occupiedAreas)
        {
            if (checkRect.Overlaps(occupied))
                return true;
        }

        return false;
    }

    // 移动到下一行
    private void MoveToNextRow()
    {
        currentPosition.x = 0;
        currentPosition.y -= currentRowHeight + Spacing; // 使用Spacing属性
        currentRowHeight = 0f;
        itemsInCurrentRow = 0;
    }

    // 计算位置评分（越小越好）
    private float CalculatePositionScore(Vector2 position, Vector2 itemSize)
    {
        // 基础评分：优先左上角位置
        float score = position.x * 0.1f + Mathf.Abs(position.y) * 0.2f;

        // 邻接奖励：与现有物品相邻可以减少评分
        float adjacencyBonus = CalculateAdjacencyBonus(position, itemSize);
        score -= adjacencyBonus * 10f;

        // 碎片化惩罚：避免产生小的不可用空间
        float fragmentationPenalty = CalculateFragmentationPenalty(position, itemSize);
        score += fragmentationPenalty * 5f;

        return score;
    }

    // 计算邻接奖励
    private float CalculateAdjacencyBonus(Vector2 position, Vector2 itemSize)
    {
        float bonus = 0f;
        Rect itemRect = new Rect(position.x, position.y - itemSize.y, itemSize.x, itemSize.y);

        foreach (Rect occupied in occupiedAreas)
        {
            if (AreRectanglesAdjacent(itemRect, occupied))
            {
                bonus += 1f;
            }
        }

        return bonus;
    }

    // 检查两个矩形是否相邻
    private bool AreRectanglesAdjacent(Rect rect1, Rect rect2)
    {
        // 检查水平相邻
        bool horizontallyAdjacent = (Mathf.Approximately(rect1.xMax, rect2.xMin) ||
                                   Mathf.Approximately(rect2.xMax, rect1.xMin)) &&
                                  !(rect1.yMax <= rect2.yMin || rect2.yMax <= rect1.yMin);

        // 检查垂直相邻
        bool verticallyAdjacent = (Mathf.Approximately(rect1.yMax, rect2.yMin) ||
                                 Mathf.Approximately(rect2.yMax, rect1.yMin)) &&
                                !(rect1.xMax <= rect2.xMin || rect2.xMax <= rect1.xMin);

        return horizontallyAdjacent || verticallyAdjacent;
    }

    // 计算碎片化惩罚
    private float CalculateFragmentationPenalty(Vector2 position, Vector2 itemSize)
    {
        float penalty = 0f;

        // 检查放置此物品后是否会产生小的不可用空间
        // 这里简化处理，主要检查右侧和下方的剩余空间

        float rightSpace = (maxItemsPerRow * (CellSize + Spacing)) - (position.x + itemSize.x);
        float minUsableWidth = CellSize; // 最小可用宽度

        if (rightSpace > 0 && rightSpace < minUsableWidth)
        {
            penalty += (minUsableWidth - rightSpace) / minUsableWidth;
        }

        return penalty;
    }

    [ContextMenu("清除所有测试物品")]
    public void ClearAllTestItems()
    {
        ClearExistingItems();
        ResetGenerationState();
        Debug.Log("已清除所有测试物品。");
    }

    [ContextMenu("随机位置生成所有物品测试")]
    public void GenerateAllItemsRandomTest()
    {
        ClearExistingItems();
        ResetGenerationState();

#if UNITY_EDITOR
        // 获取所有预制体
        GameObject[] prefabs = GetAllPrefabsInFolder();

        if (prefabs.Length == 0)
        {
            Debug.LogWarning($"在路径 {prefabFolderPath} 中未找到任何预制体！");
            return;
        }

        // 根据类型筛选预制体
        if (enableCategoryFilter && selectedCategories.Count > 0)
        {
            prefabs = FilterPrefabsByCategory(prefabs);
            if (prefabs.Length == 0)
            {
                Debug.LogWarning("根据选定的类型筛选后，没有找到匹配的预制体！");
                return;
            }
        }

        // 确定实际生成数量
        int actualCount = Random.Range(minRandomCount, maxRandomCount + 1);
        actualCount = Mathf.Min(actualCount, prefabs.Length); // 不能超过可用预制体数量

        Debug.Log($"找到 {prefabs.Length} 个预制体，将随机生成 {actualCount} 个物品...");

        // 随机选择要生成的预制体
        var selectedPrefabs = prefabs.OrderBy(x => Random.value).Take(actualCount).ToArray();

        foreach (GameObject prefab in selectedPrefabs)
        {
            GenerateItemAtRandomPosition(prefab);
        }

        Debug.Log($"随机位置物品测试生成完成！共生成 {actualCount} 个物品。");
#else
        Debug.LogWarning("此功能仅在编辑器模式下可用！");
#endif
    }

    // 根据选定类型筛选预制体
    private GameObject[] FilterPrefabsByCategory(GameObject[] prefabs)
    {
        List<GameObject> filteredPrefabs = new List<GameObject>();

        foreach (GameObject prefab in prefabs)
        {
            ItemDataHolder dataHolder = prefab.GetComponentInChildren<ItemDataHolder>();
            if (dataHolder != null)
            {
                InventorySystemItemDataSO itemData = dataHolder.GetItemData();
                if (itemData != null && selectedCategories.Contains(itemData.itemCategory))
                {
                    filteredPrefabs.Add(prefab);
                }
            }
        }

        return filteredPrefabs.ToArray();
    }

    // 获取物品数据的辅助方法
    public InventorySystemItemDataSO GetItemDataFromPrefab(GameObject prefab)
    {
        ItemDataHolder dataHolder = prefab.GetComponentInChildren<ItemDataHolder>();
        return dataHolder?.GetItemData();
    }

    [ContextMenu("生成单个随机物品测试")]
    public void GenerateSingleRandomItemTest()
    {
#if UNITY_EDITOR
        GameObject[] prefabs = GetAllPrefabsInFolder();
        if (prefabs.Length == 0)
        {
            Debug.LogWarning($"在路径 {prefabFolderPath} 中未找到任何预制体！");
            return;
        }

        // 随机选择一个预制体
        GameObject randomPrefab = prefabs[Random.Range(0, prefabs.Length)];
        GenerateItemAtRandomPosition(randomPrefab);

        Debug.Log($"随机生成单个物品: {randomPrefab.name}");
#else
        Debug.LogWarning("此功能仅在编辑器模式下可用！");
#endif
    }

    // 在Scene视图中绘制调试信息
    private void OnDrawGizmos()
    {
        if (!showDebugInfo || occupiedAreas == null) return;

        Gizmos.color = Color.red;
        foreach (Rect rect in occupiedAreas)
        {
            Vector3 worldPos = transform.TransformPoint(new Vector3(rect.x, -rect.y, 0));
            Gizmos.DrawWireCube(worldPos + new Vector3(rect.width / 2, -rect.height / 2, 0),
                              new Vector3(rect.width, rect.height, 0));
        }
    }

    // 获取文件夹中的所有预制体
#if UNITY_EDITOR
    private GameObject[] GetAllPrefabsInFolder()
    {
        List<GameObject> prefabs = new List<GameObject>();
        
        // 获取指定路径下的所有资源GUID
        string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { prefabFolderPath });
        
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            
            if (prefab != null)
            {
                // 检查是否包含ItemDataHolder组件
                ItemDataHolder dataHolder = prefab.GetComponentInChildren<ItemDataHolder>();
                if (dataHolder != null)
                {
                    prefabs.Add(prefab);
                }
            }
        }
        
        return prefabs.ToArray();
    }
#endif

    // 获取预制体的网格尺寸
    private Vector2Int GetPrefabSize(GameObject prefab)
    {
        ItemDataHolder dataHolder = prefab.GetComponentInChildren<ItemDataHolder>();
        if (dataHolder != null)
        {
            InventorySystemItemDataSO itemData = dataHolder.GetItemData();
            if (itemData != null)
            {
                return new Vector2Int(itemData.width, itemData.height);
            }
        }

        // 默认尺寸
        return new Vector2Int(1, 1);
    }
}