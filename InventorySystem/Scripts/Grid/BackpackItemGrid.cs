// BackpackItemGrid.cs
// 背包专用网格，直接继承 BaseItemGrid 并支持运行时切换背包数据
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using InventorySystem.Grid;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class BackpackItemGrid : BaseItemGrid
{
    [Header("背包网格参数")]
    [SerializeField, Tooltip("默认宽度")] private int defaultWidth = 6;
    [SerializeField, Tooltip("默认高度")] private int defaultHeight = 8;

    // 当前背包数据（运行时动态设置）
    private InventorySystemItemDataSO currentBackpackData;

    /* ---------------- 生命周期 ---------------- */
    protected override void Awake()
    {
        LoadFromBackpackData();
        base.Awake();
    }

    protected override void Start()
    {
        LoadFromBackpackData();
        base.Start();
    }

    protected override void OnValidate()
    {
        if (isUpdatingFromConfig) return;
        LoadFromBackpackData();
        width = Mathf.Clamp(width, 1, 50);
        height = Mathf.Clamp(height, 1, 50);
        base.OnValidate();
    }

    protected override void Init(int w, int h)
    {
        if (rectTransform == null) return;
        float cellSize = gridConfig != null ? gridConfig.cellSize : 64f;
        rectTransform.sizeDelta = new Vector2(w * cellSize, h * cellSize);
        if (Application.isPlaying) InitializeGridArrays();
    }

    /* ---------------- 动态背包 ---------------- */
    private void LoadFromBackpackData()
    {
        if (currentBackpackData != null && !isUpdatingFromConfig)
        {
            isUpdatingFromConfig = true;
            width = currentBackpackData.CellH;
            height = currentBackpackData.CellV;
            isUpdatingFromConfig = false;
        }
        else if (!isUpdatingFromConfig)
        {
            isUpdatingFromConfig = true;
            width = defaultWidth;
            height = defaultHeight;
            isUpdatingFromConfig = false;
        }
    }

    /// <summary>运行时更换背包（换装更大的背包）</summary>
    public void SetBackpackData(InventorySystemItemDataSO data)
    {
        currentBackpackData = data;
        LoadFromBackpackData();
        if (Application.isPlaying)
        {
            InitializeGridArrays();
            placedItems.Clear();
            // 生成稳定的保存ID（基于背包数据，确保同一背包使用相同ID）
            GenerateStableSaveID();
            // 重新初始化保存系统
            InitializeSaveSystem();
        }
        Init(width, height);
    }

    public InventorySystemItemDataSO GetCurrentBackpackData() => currentBackpackData;

    /// <summary>背包占用率</summary>
    public float GetBackpackOccupancyRate() => GetOccupancyRate();

    /// <summary>背包剩余格子数</summary>
    public int GetRemainingSpace() => width * height - occupiedCells.Count;

    public override string GetSaveID()
    {
        return "BackpackItemGrid";
    }

    // ==================== 背包网格检测器扩展功能 ====================

    /// <summary>
    /// 获取背包网格特有的检测器信息
    /// 包含背包网格的特殊属性和状态
    /// </summary>
    /// <returns>背包网格检测器信息</returns>
    public override GridDetectorInfo GetGridDetectorInfo()
    {
        var baseInfo = base.GetGridDetectorInfo();

        // 添加背包网格特有信息
        baseInfo.gridType = "背包网格 (BackpackItemGrid)";

        return baseInfo;
    }

    /// <summary>
    /// 获取背包负重分析信息
    /// 分析背包中物品的重量分布和负重状态
    /// </summary>
    /// <returns>背包负重分析信息</returns>
    public BackpackWeightInfo GetBackpackWeightInfo()
    {
        var weightInfo = new BackpackWeightInfo
        {
            gridID = GetSaveID(),
            totalItems = placedItems.Count,
            totalWeight = 0f,
            averageWeight = 0f,
            heaviestItem = null,
            lightestItem = null,
            weightDistribution = new Dictionary<string, float>(),
            overweightItems = new List<string>()
        };

        if (placedItems.Count == 0)
        {
            return weightInfo;
        }

        float minWeight = float.MaxValue;
        float maxWeight = float.MinValue;
        string heaviestItemName = "";
        string lightestItemName = "";

        // 分析每个物品的重量
        foreach (var placedItem in placedItems)
        {
            if (placedItem.itemObject == null) continue;

            var inventoryItem = placedItem.itemObject.GetComponent<InventorySystemItem>();
            if (inventoryItem == null || inventoryItem.Data == null) continue;

            float itemWeight = GetItemWeight(inventoryItem.Data);
            string itemName = inventoryItem.Data.itemName;
            string category = inventoryItem.Data.itemCategory.ToString();

            weightInfo.totalWeight += itemWeight;

            // 更新最重和最轻物品
            if (itemWeight > maxWeight)
            {
                maxWeight = itemWeight;
                heaviestItemName = itemName;
            }
            if (itemWeight < minWeight)
            {
                minWeight = itemWeight;
                lightestItemName = itemName;
            }

            // 统计类别重量分布
            if (weightInfo.weightDistribution.ContainsKey(category))
            {
                weightInfo.weightDistribution[category] += itemWeight;
            }
            else
            {
                weightInfo.weightDistribution[category] = itemWeight;
            }

            // 检查是否为超重物品（假设单个物品重量超过5.0为超重）
            if (itemWeight > 5.0f)
            {
                weightInfo.overweightItems.Add($"{itemName} ({itemWeight:F1}kg)");
            }
        }

        weightInfo.averageWeight = weightInfo.totalWeight / placedItems.Count;
        weightInfo.heaviestItem = heaviestItemName;
        weightInfo.lightestItem = lightestItemName;

        return weightInfo;
    }

    /// <summary>
    /// 获取物品重量（从物品数据中提取或使用默认值）
    /// </summary>
    /// <param name="itemData">物品数据</param>
    /// <returns>物品重量</returns>
    private float GetItemWeight(InventorySystemItemDataSO itemData)
    {
        // 这里可以根据实际的物品数据结构来获取重量
        // 目前使用基于物品尺寸的估算重量
        if (itemData == null) return 0f;

        // 基于物品尺寸估算重量（1x1 = 0.5kg基础重量）
        float baseWeight = itemData.width * itemData.height * 0.5f;

        // 根据物品类型调整重量系数
        float weightMultiplier = GetWeightMultiplierByType(itemData.itemCategory);

        return baseWeight * weightMultiplier;
    }

    /// <summary>
    /// 根据物品类型获取重量系数
    /// </summary>
    /// <param name="itemType">物品类型</param>
    /// <returns>重量系数</returns>
    private float GetWeightMultiplierByType(InventorySystemItemCategory? itemType)
    {
        if (itemType == null) return 1.0f;

        // 根据物品类型名称设置不同的重量系数
        string typeName = itemType.ToString().ToLower();

        if (typeName.Contains("weapon") || typeName.Contains("武器"))
            return 2.0f;  // 武器较重
        else if (typeName.Contains("armor") || typeName.Contains("防具"))
            return 1.5f;  // 防具中等重量
        else if (typeName.Contains("ammo") || typeName.Contains("弹药"))
            return 0.8f;  // 弹药较轻
        else if (typeName.Contains("consumable") || typeName.Contains("消耗品"))
            return 0.3f;  // 消耗品很轻
        else
            return 1.0f;  // 默认重量
    }

    /// <summary>
    /// 获取背包整理建议
    /// 基于物品分布和使用频率提供整理建议
    /// </summary>
    /// <returns>背包整理建议信息</returns>
    public BackpackOrganizationSuggestion GetOrganizationSuggestion()
    {
        var suggestion = new BackpackOrganizationSuggestion
        {
            gridID = GetSaveID(),
            currentEfficiency = GetOccupancyRate(),
            suggestions = new List<string>(),
            priorityItems = new List<string>(),
            redundantItems = new List<string>(),
            misplacedItems = new List<string>()
        };

        // 分析物品类型分布
        var itemTypeCount = new Dictionary<string, int>();
        var itemTypePositions = new Dictionary<string, List<Vector2Int>>();

        foreach (var placedItem in placedItems)
        {
            if (placedItem.itemObject == null) continue;

            var inventoryItem = placedItem.itemObject.GetComponent<InventorySystemItem>();
            if (inventoryItem == null || inventoryItem.Data == null) continue;

            string itemType = inventoryItem.Data.itemCategory.ToString();
            string itemName = inventoryItem.Data.itemName;

            // 统计物品类型数量
            if (itemTypeCount.ContainsKey(itemType))
            {
                itemTypeCount[itemType]++;
            }
            else
            {
                itemTypeCount[itemType] = 1;
                itemTypePositions[itemType] = new List<Vector2Int>();
            }

            itemTypePositions[itemType].Add(placedItem.position);
        }

        // 生成整理建议
        GenerateOrganizationSuggestions(suggestion, itemTypeCount, itemTypePositions);

        return suggestion;
    }

    /// <summary>
    /// 生成背包整理建议
    /// </summary>
    /// <param name="suggestion">建议对象</param>
    /// <param name="itemTypeCount">物品类型统计</param>
    /// <param name="itemTypePositions">物品类型位置分布</param>
    private void GenerateOrganizationSuggestions(BackpackOrganizationSuggestion suggestion,
        Dictionary<string, int> itemTypeCount, Dictionary<string, List<Vector2Int>> itemTypePositions)
    {
        // 检查物品分散度
        foreach (var typePos in itemTypePositions)
        {
            if (typePos.Value.Count > 1)
            {
                // 计算同类型物品的分散程度
                float averageDistance = CalculateAverageDistance(typePos.Value);
                if (averageDistance > 3.0f) // 如果平均距离大于3格
                {
                    suggestion.suggestions.Add($"建议将分散的{typePos.Key}类物品集中放置，当前平均距离为{averageDistance:F1}格");
                    suggestion.misplacedItems.Add($"{typePos.Key}类物品 (分散度:{averageDistance:F1})");
                }
            }
        }

        // 检查高频使用物品位置
        CheckHighFrequencyItemPlacement(suggestion, itemTypeCount);

        // 检查冗余物品
        CheckRedundantItems(suggestion, itemTypeCount);

        // 检查空间利用效率
        if (suggestion.currentEfficiency < 0.6f)
        {
            suggestion.suggestions.Add("背包空间利用率较低，建议重新整理物品布局以节省空间");
        }
        else if (suggestion.currentEfficiency > 0.9f)
        {
            suggestion.suggestions.Add("背包空间几乎满载，建议清理不必要的物品或扩展背包容量");
        }
    }

    /// <summary>
    /// 计算位置列表的平均距离
    /// </summary>
    /// <param name="positions">位置列表</param>
    /// <returns>平均距离</returns>
    private float CalculateAverageDistance(List<Vector2Int> positions)
    {
        if (positions.Count <= 1) return 0f;

        float totalDistance = 0f;
        int pairCount = 0;

        for (int i = 0; i < positions.Count; i++)
        {
            for (int j = i + 1; j < positions.Count; j++)
            {
                float distance = Vector2Int.Distance(positions[i], positions[j]);
                totalDistance += distance;
                pairCount++;
            }
        }

        return pairCount > 0 ? totalDistance / pairCount : 0f;
    }

    /// <summary>
    /// 检查高频使用物品的放置位置
    /// </summary>
    /// <param name="suggestion">建议对象</param>
    /// <param name="itemTypeCount">物品类型统计</param>
    private void CheckHighFrequencyItemPlacement(BackpackOrganizationSuggestion suggestion,
        Dictionary<string, int> itemTypeCount)
    {
        // 识别高频物品类型（假设消耗品和弹药为高频使用）
        foreach (var typeCount in itemTypeCount)
        {
            string typeName = typeCount.Key.ToLower();
            if (typeName.Contains("consumable") || typeName.Contains("ammo") ||
                typeName.Contains("消耗品") || typeName.Contains("弹药"))
            {
                suggestion.priorityItems.Add($"{typeCount.Key} (数量:{typeCount.Value})");
                suggestion.suggestions.Add($"建议将{typeCount.Key}类物品放置在背包易取位置（左上角区域）");
            }
        }
    }

    /// <summary>
    /// 检查冗余物品
    /// </summary>
    /// <param name="suggestion">建议对象</param>
    /// <param name="itemTypeCount">物品类型统计</param>
    private void CheckRedundantItems(BackpackOrganizationSuggestion suggestion,
        Dictionary<string, int> itemTypeCount)
    {
        foreach (var typeCount in itemTypeCount)
        {
            // 如果某类型物品数量过多（超过5个），标记为可能冗余
            if (typeCount.Value > 5)
            {
                suggestion.redundantItems.Add($"{typeCount.Key} (数量:{typeCount.Value})");
                suggestion.suggestions.Add($"{typeCount.Key}类物品数量较多({typeCount.Value}个)，建议检查是否有冗余");
            }
        }
    }

    /// <summary>
    /// 获取背包快速访问区域分析
    /// 分析背包中哪些区域适合放置常用物品
    /// </summary>
    /// <returns>快速访问区域分析信息</returns>
    public BackpackQuickAccessInfo GetQuickAccessInfo()
    {
        var accessInfo = new BackpackQuickAccessInfo
        {
            gridID = GetSaveID(),
            quickAccessZones = new List<QuickAccessZone>(),
            recommendedPlacements = new Dictionary<string, Vector2Int>()
        };

        // 定义快速访问区域（通常是背包的左上角区域）
        DefineQuickAccessZones(accessInfo);

        // 分析当前快速访问区域的使用情况
        AnalyzeQuickAccessUsage(accessInfo);

        // 生成推荐放置建议
        GenerateQuickAccessRecommendations(accessInfo);

        return accessInfo;
    }

    /// <summary>
    /// 定义快速访问区域
    /// </summary>
    /// <param name="accessInfo">访问信息对象</param>
    private void DefineQuickAccessZones(BackpackQuickAccessInfo accessInfo)
    {
        // 主要快速访问区域（左上角2x2）
        accessInfo.quickAccessZones.Add(new QuickAccessZone
        {
            zoneName = "主要快速区",
            zoneArea = new RectInt(0, 0, 2, 2),
            priority = 1,
            recommendedItemTypes = new List<string> { "消耗品", "治疗物品", "弹药" }
        });

        // 次要快速访问区域（左侧边缘）
        if (width > 2)
        {
            accessInfo.quickAccessZones.Add(new QuickAccessZone
            {
                zoneName = "次要快速区",
                zoneArea = new RectInt(0, 2, 1, Mathf.Min(3, height - 2)),
                priority = 2,
                recommendedItemTypes = new List<string> { "工具", "钥匙物品" }
            });
        }

        // 武器快速区域（顶部边缘）
        if (height > 2)
        {
            accessInfo.quickAccessZones.Add(new QuickAccessZone
            {
                zoneName = "武器快速区",
                zoneArea = new RectInt(2, 0, Mathf.Min(3, width - 2), 1),
                priority = 2,
                recommendedItemTypes = new List<string> { "武器", "弹药" }
            });
        }
    }

    /// <summary>
    /// 分析快速访问区域的使用情况
    /// </summary>
    /// <param name="accessInfo">访问信息对象</param>
    private void AnalyzeQuickAccessUsage(BackpackQuickAccessInfo accessInfo)
    {
        foreach (var zone in accessInfo.quickAccessZones)
        {
            zone.currentItems = new List<string>();
            zone.utilizationRate = 0f;

            int occupiedCells = 0;
            int totalCells = zone.zoneArea.width * zone.zoneArea.height;

            // 检查区域内的物品
            foreach (var placedItem in placedItems)
            {
                if (IsItemInZone(placedItem, zone.zoneArea))
                {
                    var inventoryItem = placedItem.itemObject?.GetComponent<InventorySystemItem>();
                    if (inventoryItem?.Data != null)
                    {
                        zone.currentItems.Add(inventoryItem.Data.itemName);
                        occupiedCells += placedItem.size.x * placedItem.size.y;
                    }
                }
            }

            zone.utilizationRate = totalCells > 0 ? (float)occupiedCells / totalCells : 0f;
        }
    }

    /// <summary>
    /// 检查物品是否在指定区域内
    /// </summary>
    /// <param name="placedItem">放置的物品</param>
    /// <param name="zoneArea">区域范围</param>
    /// <returns>是否在区域内</returns>
    private bool IsItemInZone(PlacedItem placedItem, RectInt zoneArea)
    {
        return placedItem.position.x >= zoneArea.x &&
               placedItem.position.y >= zoneArea.y &&
               placedItem.position.x + placedItem.size.x <= zoneArea.x + zoneArea.width &&
               placedItem.position.y + placedItem.size.y <= zoneArea.y + zoneArea.height;
    }

    /// <summary>
    /// 生成快速访问推荐放置建议
    /// </summary>
    /// <param name="accessInfo">访问信息对象</param>
    private void GenerateQuickAccessRecommendations(BackpackQuickAccessInfo accessInfo)
    {
        // 为每个快速访问区域生成推荐
        foreach (var zone in accessInfo.quickAccessZones)
        {
            if (zone.utilizationRate < 0.5f) // 如果区域利用率低于50%
            {
                foreach (var recommendedType in zone.recommendedItemTypes)
                {
                    // 寻找合适的空位
                    var availablePos = FindAvailablePositionInZone(zone.zoneArea);
                    if (availablePos != new Vector2Int(-1, -1))
                    {
                        accessInfo.recommendedPlacements[recommendedType] = availablePos;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 在指定区域内寻找可用位置
    /// </summary>
    /// <param name="zoneArea">区域范围</param>
    /// <returns>可用位置，如果没有则返回(-1,-1)</returns>
    private Vector2Int FindAvailablePositionInZone(RectInt zoneArea)
    {
        for (int x = zoneArea.x; x < zoneArea.x + zoneArea.width; x++)
        {
            for (int y = zoneArea.y; y < zoneArea.y + zoneArea.height; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (!occupiedCells.Contains(pos))
                {
                    return pos;
                }
            }
        }
        return new Vector2Int(-1, -1);
    }




    // ==================== ISaveable接口扩展实现 ====================

    /// <summary>
    /// 生成背包网格专用的稳定保存ID
    /// 格式：Backpack_[背包数据ID]_Grid
    /// 使用背包数据的唯一ID确保同一个背包重新装备时使用相同的保存ID
    /// </summary>
    public void GenerateStableSaveID()
    {
        string backpackDataID = "Unknown";
        if (currentBackpackData != null)
        {
            // 使用背包数据的name作为唯一标识，确保同一个背包数据总是生成相同的ID
            backpackDataID = currentBackpackData.name.Replace(" ", "").Replace("/", "").Replace("\\", "");
        }

        string newGridID = $"Backpack_{backpackDataID}_Grid";

        // 只有当ID真正改变时才更新并标记为修改
        if (gridID != newGridID)
        {
            gridID = newGridID;
            MarkAsModified();
            Debug.Log($"背包网格更新稳定ID: {gridID}");
        }
        else
        {
            Debug.Log($"背包网格保持现有ID: {gridID}");
        }
    }

    /// <summary>
    /// 生成背包网格专用的唯一标识ID（重写基类方法）
    /// 调用稳定ID生成方法确保一致性
    /// </summary>
    public override void GenerateNewSaveID()
    {
        GenerateStableSaveID();
    }

    // ==================== 网格占用图谱保存机制 ====================

    /// <summary>
    /// 保存当前网格的占用图谱数据
    /// 将gridOccupancy、prefixSum和occupiedCells序列化为可保存的格式
    /// </summary>
    /// <returns>网格占用图谱数据</returns>
    public BackpackGridSaveData.GridOccupancyMapData SaveOccupancyMap()
    {
        if (gridOccupancy == null || prefixSum == null || occupiedCells == null)
        {
            Debug.LogWarning("网格数据结构未初始化，无法保存占用图谱");
            return new BackpackGridSaveData.GridOccupancyMapData();
        }

        var mapData = new BackpackGridSaveData.GridOccupancyMapData(width, height);

        // 序列化二维bool数组为一维数组
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                mapData.occupancyMatrix[x * height + y] = gridOccupancy[x, y];
            }
        }

        // 序列化二维前缀和数组为一维数组
        for (int x = 0; x <= width; x++)
        {
            for (int y = 0; y <= height; y++)
            {
                mapData.prefixSumArray[x * (height + 1) + y] = prefixSum[x, y];
            }
        }

        // 序列化占用单元格集合
        mapData.occupiedCellsList = occupiedCells.ToArray();
        mapData.totalOccupiedCells = occupiedCells.Count;
        mapData.occupancyPercentage = (float)occupiedCells.Count / (width * height) * 100f;

        Debug.Log($"背包网格占用图谱保存完成: {mapData.totalOccupiedCells}个占用单元格，占用率{mapData.occupancyPercentage:F1}%");
        return mapData;
    }

    /// <summary>
    /// 保存当前网格的占用图谱数据（管理器格式）
    /// 用于BackpackOccupancyMapManager
    /// </summary>
    /// <returns>管理器格式的网格占用图谱数据</returns>
    public GridOccupancyMapData SaveOccupancyMapForManager()
    {
        if (gridOccupancy == null || prefixSum == null || occupiedCells == null)
        {
            Debug.LogWarning("网格数据结构未初始化，无法保存占用图谱");
            return new GridOccupancyMapData();
        }

        var mapData = new GridOccupancyMapData(width, height);

        // 序列化二维bool数组为一维数组
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                mapData.occupancyMatrix[x * height + y] = gridOccupancy[x, y];
            }
        }

        // 序列化二维前缀和数组为一维数组
        for (int x = 0; x <= width; x++)
        {
            for (int y = 0; y <= height; y++)
            {
                mapData.prefixSumArray[x * (height + 1) + y] = prefixSum[x, y];
            }
        }

        // 序列化占用单元格集合
        mapData.occupiedCellsList = occupiedCells.ToArray();
        mapData.totalOccupiedCells = occupiedCells.Count;
        mapData.occupancyPercentage = (float)occupiedCells.Count / (width * height) * 100f;

        Debug.Log($"背包网格占用图谱保存完成（管理器格式）: {mapData.totalOccupiedCells}个占用单元格，占用率{mapData.occupancyPercentage:F1}%");
        return mapData;
    }

    /// <summary>
    /// 从占用图谱数据恢复网格占用状态
    /// 将序列化的数据反序列化为gridOccupancy、prefixSum和occupiedCells
    /// </summary>
    /// <param name="mapData">网格占用图谱数据</param>
    /// <returns>恢复是否成功</returns>
    public bool LoadOccupancyMap(BackpackGridSaveData.GridOccupancyMapData mapData)
    {
        if (mapData == null || !mapData.IsValid())
        {
            Debug.LogWarning("占用图谱数据无效，无法恢复网格占用状态");
            return false;
        }

        // 验证尺寸匹配
        if (mapData.matrixWidth != width || mapData.matrixHeight != height)
        {
            Debug.LogWarning($"占用图谱尺寸不匹配: 期望{width}x{height}，实际{mapData.matrixWidth}x{mapData.matrixHeight}");
            return false;
        }

        // 确保数据结构已初始化
        if (gridOccupancy == null || prefixSum == null || occupiedCells == null)
        {
            InitializeGridArrays();
        }

        // 反序列化占用状态矩阵
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                gridOccupancy[x, y] = mapData.occupancyMatrix[x * height + y];
            }
        }

        // 反序列化前缀和数组
        for (int x = 0; x <= width; x++)
        {
            for (int y = 0; y <= height; y++)
            {
                prefixSum[x, y] = mapData.prefixSumArray[x * (height + 1) + y];
            }
        }

        // 反序列化占用单元格集合
        occupiedCells.Clear();
        foreach (var cell in mapData.occupiedCellsList)
        {
            occupiedCells.Add(cell);
        }

        Debug.Log($"背包网格占用图谱恢复完成: {mapData.totalOccupiedCells}个占用单元格，占用率{mapData.occupancyPercentage:F1}%");
        return true;
    }

    /// <summary>
    /// 从管理器格式的占用图谱数据恢复网格占用状态
    /// </summary>
    /// <param name="mapData">管理器格式的网格占用图谱数据</param>
    /// <returns>恢复是否成功</returns>
    public bool LoadOccupancyMapFromManager(GridOccupancyMapData mapData)
    {
        if (mapData == null || !mapData.IsValid())
        {
            Debug.LogWarning("管理器占用图谱数据无效，无法恢复网格占用状态");
            return false;
        }

        // 验证尺寸匹配
        if (mapData.gridWidth != width || mapData.gridHeight != height)
        {
            Debug.LogWarning($"管理器占用图谱尺寸不匹配: 期望{width}x{height}，实际{mapData.gridWidth}x{mapData.gridHeight}");
            return false;
        }

        // 确保数据结构已初始化
        if (gridOccupancy == null || prefixSum == null || occupiedCells == null)
        {
            InitializeGridArrays();
        }

        // 反序列化占用状态矩阵
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                gridOccupancy[x, y] = mapData.occupancyMatrix[x * height + y];
            }
        }

        // 反序列化前缀和数组
        for (int x = 0; x <= width; x++)
        {
            for (int y = 0; y <= height; y++)
            {
                prefixSum[x, y] = mapData.prefixSumArray[x * (height + 1) + y];
            }
        }

        // 反序列化占用单元格集合
        occupiedCells.Clear();
        foreach (var cell in mapData.occupiedCellsList)
        {
            occupiedCells.Add(cell);
        }

        Debug.Log($"从管理器恢复背包网格占用图谱完成: {mapData.totalOccupiedCells}个占用单元格，占用率{mapData.occupancyPercentage:F1}%");
        return true;
    }

    /// <summary>
    /// 验证当前占用图谱的一致性
    /// 检查gridOccupancy、prefixSum和occupiedCells之间的数据一致性
    /// </summary>
    /// <returns>数据是否一致</returns>
    public bool ValidateOccupancyMapConsistency()
    {
        if (gridOccupancy == null || prefixSum == null || occupiedCells == null)
        {
            return false;
        }

        // 验证占用单元格集合与占用矩阵的一致性
        int actualOccupiedCount = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (gridOccupancy[x, y])
                {
                    actualOccupiedCount++;
                    if (!occupiedCells.Contains(new Vector2Int(x, y)))
                    {
                        Debug.LogError($"占用矩阵与占用集合不一致: 位置({x},{y})在矩阵中标记为占用但不在集合中");
                        return false;
                    }
                }
            }
        }

        if (actualOccupiedCount != occupiedCells.Count)
        {
            Debug.LogError($"占用数量不一致: 矩阵中{actualOccupiedCount}个，集合中{occupiedCells.Count}个");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 获取背包网格的保存数据
    /// 包含背包数据路径和动态尺寸信息
    /// 集成BackpackOccupancyMapManager进行异步保存
    /// </summary>
    public override BaseItemGridSaveData GetSaveData()
    {
        var saveData = base.GetSaveData();

        // 创建背包专用的保存数据
        var backpackSaveData = new BackpackGridSaveData
        {
            gridID = saveData.gridID,
            saveVersion = saveData.saveVersion,
            gridWidth = saveData.gridWidth,
            gridHeight = saveData.gridHeight,
            placedItems = saveData.placedItems,
            lastModified = saveData.lastModified,
            isModified = saveData.isModified,

            // 背包特定数据
            backpackDataPath = GetBackpackDataPath(),
            defaultWidth = defaultWidth,
            defaultHeight = defaultHeight,
            hasActiveBackpack = currentBackpackData != null,
            occupancyRate = GetBackpackOccupancyRate(),

            // 保存网格占用图谱数据
            occupancyMapData = SaveOccupancyMap()
        };

        // 使用BackpackOccupancyMapManager进行异步保存
        if (BackpackOccupancyMapManager.Instance != null && backpackSaveData.occupancyMapData != null)
        {
            string backpackID = GetStableBackpackID();
            var managerMapData = GridOccupancyMapData.FromSaveData(backpackSaveData.occupancyMapData);
            BackpackOccupancyMapManager.Instance.SaveOccupancyMap(backpackID, managerMapData, false);
            Debug.Log($"背包占用图谱已提交到管理器进行异步保存: {backpackID}");
        }

        Debug.Log($"背包网格保存数据生成完成，包含占用图谱: {backpackSaveData.occupancyMapData?.totalOccupiedCells ?? 0}个占用单元格");

        return backpackSaveData;
    }

    /// <summary>
    /// 从保存数据加载背包网格状态
    /// 恢复背包数据关联和网格配置
    /// 集成BackpackOccupancyMapManager进行异步加载
    /// </summary>
    public override bool LoadSaveData(BaseItemGridSaveData saveData)
    {
        try
        {
            // 尝试转换为背包专用保存数据
            BackpackGridSaveData backpackData = saveData as BackpackGridSaveData;
            if (backpackData != null)
            {
                // 恢复背包数据关联
                if (!string.IsNullOrEmpty(backpackData.backpackDataPath))
                {
                    var backpackItemData = Resources.Load<InventorySystemItemDataSO>(backpackData.backpackDataPath);
                    if (backpackItemData != null)
                    {
                        SetBackpackData(backpackItemData);
                        Debug.Log($"成功恢复背包数据: {backpackData.backpackDataPath}");
                    }
                    else
                    {
                        Debug.LogWarning($"无法加载背包数据: {backpackData.backpackDataPath}");
                        // 使用默认尺寸
                        defaultWidth = backpackData.defaultWidth;
                        defaultHeight = backpackData.defaultHeight;
                        LoadFromBackpackData();
                    }
                }
                else if (!backpackData.hasActiveBackpack)
                {
                    // 没有活动背包，使用默认配置
                    defaultWidth = backpackData.defaultWidth;
                    defaultHeight = backpackData.defaultHeight;
                    currentBackpackData = null;
                    LoadFromBackpackData();
                }
            }

            // 尝试从BackpackOccupancyMapManager异步加载占用图谱
            bool mapLoadResult = false;
            if (BackpackOccupancyMapManager.Instance != null)
            {
                string backpackID = GetStableBackpackID();
                var managerMapData = BackpackOccupancyMapManager.Instance.GetOccupancyMap(backpackID);

                if (managerMapData != null)
                {
                    mapLoadResult = LoadOccupancyMapFromManager(managerMapData);
                    if (mapLoadResult)
                    {
                        Debug.Log($"从管理器异步加载背包占用图谱成功: {managerMapData.totalOccupiedCells}个占用单元格");
                    }
                }
                else if (backpackData.occupancyMapData != null)
                {
                    // 回退到保存数据中的占用图谱
                    mapLoadResult = LoadOccupancyMap(backpackData.occupancyMapData);
                    if (mapLoadResult)
                    {
                        Debug.Log($"从保存数据加载背包占用图谱: {backpackData.occupancyMapData.totalOccupiedCells}个占用单元格");
                        // 将数据同步到管理器
                        var syncMapData = GridOccupancyMapData.FromSaveData(backpackData.occupancyMapData);
                        BackpackOccupancyMapManager.Instance.SaveOccupancyMap(backpackID, syncMapData, false);
                    }
                }
            }
            else
            {
                // 管理器不可用，使用传统方式加载
                if (backpackData.occupancyMapData != null)
                {
                    mapLoadResult = LoadOccupancyMap(backpackData.occupancyMapData);
                    if (mapLoadResult)
                    {
                        Debug.Log($"传统方式加载背包占用图谱: {backpackData.occupancyMapData.totalOccupiedCells}个占用单元格");
                    }
                }
            }

            // 验证占用图谱数据一致性
            if (mapLoadResult)
            {
                if (!ValidateOccupancyMapConsistency())
                {
                    Debug.LogWarning("背包网格占用图谱数据一致性验证失败，可能存在数据损坏");
                }
            }
            else
            {
                Debug.LogWarning("背包网格占用图谱加载失败，将使用默认空白状态");
            }

            // 使用批量恢复功能恢复物品
            bool itemsRestored = RestoreItemsWithBatchSystem(backpackData);

            // 调用基类方法进行基础数据恢复（但跳过物品恢复部分）
            bool baseResult = LoadBaseDataOnly(backpackData);

            bool finalResult = baseResult && itemsRestored;

            if (finalResult)
            {
                Debug.Log($"背包网格数据加载成功: {saveData.gridID}");
            }

            return finalResult;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"背包网格数据加载失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 使用批量恢复系统恢复物品
    /// </summary>
    /// <param name="backpackData">背包保存数据</param>
    /// <returns>恢复是否成功</returns>
    private bool RestoreItemsWithBatchSystem(BackpackGridSaveData backpackData)
    {
        if (backpackData?.placedItems == null || backpackData.placedItems.Length == 0)
        {
            Debug.Log("背包中没有物品需要恢复");
            return true;
        }

        try
        {
            // 获取物品恢复系统实例
            var restorationSystem = ItemRestorationSystem.Instance;
            if (restorationSystem == null)
            {
                Debug.LogError("物品恢复系统未初始化，使用传统恢复方法");
                return RestoreItemsTraditional(ConvertPlacedItemsToItemSaveData(backpackData.placedItems));
            }

            Debug.Log($"开始使用批量恢复系统恢复 {backpackData.placedItems.Length} 个物品");

            // 使用批量恢复功能
            var restorationResult = restorationSystem.RestoreItemsBatch(
                ConvertPlacedItemsToItemSaveData(backpackData.placedItems),
                this,
                backpackData.occupancyMapData
            );

            // 处理恢复结果
            if (restorationResult.success)
            {
                Debug.Log($"批量恢复成功: {restorationResult.successCount}/{restorationResult.totalProcessed} 个物品");

                // 更新物品实例映射
                UpdateItemInstanceMappings(restorationResult.restoredItems);

                return true;
            }
            else
            {
                Debug.LogWarning($"批量恢复部分失败: 成功 {restorationResult.successCount}, 失败 {restorationResult.failureCount}");

                // 对于失败的物品，尝试使用传统方法恢复
                if (restorationResult.failedItems.Count > 0)
                {
                    Debug.Log("尝试使用传统方法恢复失败的物品");
                    bool fallbackResult = RestoreItemsTraditional(restorationResult.failedItems);

                    // 更新已成功恢复的物品映射
                    if (restorationResult.restoredItems.Count > 0)
                    {
                        UpdateItemInstanceMappings(restorationResult.restoredItems);
                    }

                    return fallbackResult;
                }

                return restorationResult.successCount > 0; // 部分成功也算成功
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"批量恢复系统执行失败: {ex.Message}，回退到传统恢复方法");
            return RestoreItemsTraditional(ConvertPlacedItemsToItemSaveData(backpackData.placedItems));
        }
    }

    /// <summary>
    /// 将PlacedItemSaveData转换为ItemSaveData
    /// </summary>
    /// <param name="placedItems">放置物品数据数组</param>
    /// <returns>转换后的ItemSaveData列表</returns>
    private List<ItemSaveData> ConvertPlacedItemsToItemSaveData(BaseItemGridSaveData.PlacedItemSaveData[] placedItems)
    {
        var itemSaveDataList = new List<ItemSaveData>();

        if (placedItems == null) return itemSaveDataList;

        foreach (var placedItem in placedItems)
        {
            var itemSaveData = new ItemSaveData
            {
                instanceID = placedItem.itemInstanceID,
                gridPosition = placedItem.position,
                itemDataPath = placedItem.itemDataPath,
                isDraggable = true // 默认可拖拽
            };
            itemSaveDataList.Add(itemSaveData);
        }

        return itemSaveDataList;
    }

    /// <summary>
    /// 传统物品恢复方法（作为批量恢复的备用方案）
    /// </summary>
    /// <param name="itemsData">物品数据列表</param>
    /// <returns>恢复是否成功</returns>
    private bool RestoreItemsTraditional(List<ItemSaveData> itemsData)
    {
        if (itemsData == null || itemsData.Count == 0) return true;

        Debug.Log($"使用传统方法恢复 {itemsData.Count} 个物品");

        int successCount = 0;
        var restorationSystem = ItemRestorationSystem.Instance;

        foreach (var itemData in itemsData)
        {
            try
            {
                if (restorationSystem != null)
                {
                    GameObject restoredItem = restorationSystem.RestoreItem(itemData, this, itemData.gridPosition);
                    if (restoredItem != null)
                    {
                        successCount++;
                    }
                }
                else
                {
                    Debug.LogWarning("物品恢复系统不可用，跳过物品恢复");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"传统方法恢复物品失败: {ex.Message}");
            }
        }

        Debug.Log($"传统恢复完成: {successCount}/{itemsData.Count} 个物品成功恢复");
        return successCount > 0;
    }

    /// <summary>
    /// 更新物品实例映射
    /// </summary>
    /// <param name="restoredItems">恢复的物品列表</param>
    private void UpdateItemInstanceMappings(List<GameObject> restoredItems)
    {
        foreach (var item in restoredItems)
        {
            if (item != null)
            {
                var inventoryItem = item.GetComponent<InventorySystemItem>();
                if (inventoryItem != null)
                {
                    string instanceID = inventoryItem.GetItemInstanceID();
                    if (!string.IsNullOrEmpty(instanceID))
                    {
                        itemInstanceMap[instanceID] = item;
                        objectToInstanceID[item] = instanceID;
                    }
                }
            }
        }

        Debug.Log($"更新了 {restoredItems.Count} 个物品的实例映射");
    }

    /// <summary>
    /// 仅加载基础数据（不包括物品恢复）
    /// </summary>
    /// <param name="saveData">保存数据</param>
    /// <returns>加载是否成功</returns>
    private bool LoadBaseDataOnly(BaseItemGridSaveData saveData)
    {
        try
        {
            if (saveData == null)
            {
                Debug.LogError("保存数据为空");
                return false;
            }

            // 验证保存数据版本
            if (saveData.saveVersion > saveVersion)
            {
                Debug.LogWarning($"保存数据版本({saveData.saveVersion})高于当前版本({saveVersion})，可能存在兼容性问题");
            }

            // 恢复基础信息
            gridID = saveData.gridID;
            lastModified = saveData.lastModified;
            isModified = saveData.isModified;

            // 如果网格尺寸发生变化，重新初始化
            if (width != saveData.gridWidth || height != saveData.gridHeight)
            {
                width = saveData.gridWidth;
                height = saveData.gridHeight;
                InitializeGridArrays();
            }

            // 清空当前网格（为物品恢复做准备）
            ClearGrid();

            Debug.Log($"基础数据加载完成: 网格ID={gridID}, 尺寸={width}x{height}");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"基础数据加载失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 处理背包卸载时的数据清理
    /// 清空网格内容并重置为默认状态
    /// </summary>
    public void OnBackpackUnequipped()
    {
        try
        {
            // 清空网格中的所有物品
            ClearGrid();

            // 清理物品实例ID映射
            itemInstanceMap.Clear();
            objectToInstanceID.Clear();

            // 重置为默认状态
            currentBackpackData = null;
            LoadFromBackpackData();

            // 重新初始化网格数组
            InitializeGridArrays();
            Init(width, height);

            // 标记为已修改并更新时间戳
            MarkAsModified();

            Debug.Log("背包卸载，网格数据已清理");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"背包卸载数据清理失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取稳定的背包标识符
    /// 用于BackpackOccupancyMapManager的缓存键
    /// </summary>
    private string GetStableBackpackID()
    {
        if (currentBackpackData != null)
        {
            // 使用背包数据的资源路径作为稳定ID
#if UNITY_EDITOR
            string resourcePath = UnityEditor.AssetDatabase.GetAssetPath(currentBackpackData);
            if (!string.IsNullOrEmpty(resourcePath))
            {
                return $"backpack_{resourcePath.GetHashCode():X8}";
            }
#endif

            // 备用方案：使用背包数据名称
            return $"backpack_{currentBackpackData.name}";
        }

        // 默认背包ID
        return $"default_backpack_{width}x{height}";
    }

    /// <summary>
    /// 获取当前背包数据的资源路径
    /// </summary>
    /// <returns>背包数据资源路径，如果没有则返回空字符串</returns>
    private string GetBackpackDataPath()
    {
        if (currentBackpackData == null) return "";

#if UNITY_EDITOR
        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(currentBackpackData);
        if (!string.IsNullOrEmpty(assetPath) && assetPath.StartsWith("Assets/Resources/"))
        {
            // 转换为Resources.Load可用的路径
            string resourcePath = assetPath.Substring("Assets/Resources/".Length);
            if (resourcePath.EndsWith(".asset"))
            {
                resourcePath = resourcePath.Substring(0, resourcePath.Length - ".asset".Length);
            }
            return resourcePath;
        }
#endif
        return "";
    }

    /// <summary>
    /// 验证背包网格数据的完整性
    /// </summary>
    /// <returns>数据是否有效</returns>
    public override bool ValidateData()
    {
        // 调用基类验证
        if (!base.ValidateData())
        {
            return false;
        }

        // 验证背包特定数据
        if (currentBackpackData != null)
        {
            // 验证背包数据的有效性
            if (currentBackpackData.CellH <= 0 || currentBackpackData.CellV <= 0)
            {
                Debug.LogError("背包数据中的网格尺寸无效");
                return false;
            }

            // 验证当前网格尺寸与背包数据是否匹配
            if (width != currentBackpackData.CellH || height != currentBackpackData.CellV)
            {
                Debug.LogWarning("当前网格尺寸与背包数据不匹配，将自动同步");
                LoadFromBackpackData();
            }
        }

        // 验证占用率是否合理（不应超过100%）
        float occupancyRate = GetBackpackOccupancyRate();
        if (occupancyRate > 1.0f)
        {
            Debug.LogError($"背包占用率异常: {occupancyRate * 100:F1}%");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 初始化背包网格的保存系统
    /// 在Awake中调用以确保保存系统正确初始化
    /// </summary>
    protected override void InitializeSaveSystem()
    {
        base.InitializeSaveSystem();

        // 如果没有有效ID，生成背包专用ID
        if (!IsSaveIDValid())
        {
            GenerateNewSaveID();
        }

        Debug.Log($"背包网格保存系统初始化完成: {GetSaveID()}");
    }

    /// <summary>
    /// 获取背包状态摘要信息
    /// 用于调试和状态监控
    /// </summary>
    public string GetBackpackStatusSummary()
    {
        var summary = new System.Text.StringBuilder();
        summary.AppendLine($"背包网格ID: {GetSaveID()}");
        summary.AppendLine($"背包数据: {(currentBackpackData != null ? currentBackpackData.itemName : "无")}");
        summary.AppendLine($"网格尺寸: {width}x{height}");
        summary.AppendLine($"占用率: {GetBackpackOccupancyRate() * 100:F1}%");
        summary.AppendLine($"剩余空间: {GetRemainingSpace()} 格");
        summary.AppendLine($"已放置物品数: {placedItems.Count}");
        summary.AppendLine($"最后修改: {GetLastModified()}");
        return summary.ToString();
    }

    // ==================== 背包专用保存数据类 ====================

    [System.Serializable]
    public class BackpackGridSaveData : BaseItemGridSaveData
    {
        public string backpackDataPath;       // 背包数据资源路径
        public int defaultWidth;              // 默认宽度
        public int defaultHeight;             // 默认高度
        public bool hasActiveBackpack;        // 是否有活动的背包
        public float occupancyRate;           // 占用率快照

        // ==================== 网格占用图谱数据 ====================
        public GridOccupancyMapData occupancyMapData;  // 网格占用图谱数据

        /// <summary>
        /// 网格占用图谱数据结构
        /// 用于保存和恢复网格的完整占用状态
        /// </summary>
        [System.Serializable]
        public class GridOccupancyMapData
        {
            public bool[] occupancyMatrix;        // 占用状态矩阵（一维数组形式）
            public int[] prefixSumArray;          // 前缀和数组（一维数组形式）
            public Vector2Int[] occupiedCellsList; // 占用单元格列表
            public int matrixWidth;               // 矩阵宽度
            public int matrixHeight;              // 矩阵高度
            public string mapTimestamp;           // 图谱生成时间戳
            public int totalOccupiedCells;        // 总占用单元格数量
            public float occupancyPercentage;     // 占用百分比

            /// <summary>
            /// 默认构造函数
            /// </summary>
            public GridOccupancyMapData()
            {
                occupancyMatrix = new bool[0];
                prefixSumArray = new int[0];
                occupiedCellsList = new Vector2Int[0];
                matrixWidth = 0;
                matrixHeight = 0;
                mapTimestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                totalOccupiedCells = 0;
                occupancyPercentage = 0f;
            }

            /// <summary>
            /// 带参数的构造函数
            /// </summary>
            /// <param name="width">网格宽度</param>
            /// <param name="height">网格高度</param>
            public GridOccupancyMapData(int width, int height)
            {
                matrixWidth = width;
                matrixHeight = height;
                occupancyMatrix = new bool[width * height];
                prefixSumArray = new int[(width + 1) * (height + 1)];
                occupiedCellsList = new Vector2Int[0];
                mapTimestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                totalOccupiedCells = 0;
                occupancyPercentage = 0f;
            }

            /// <summary>
            /// 验证图谱数据的完整性
            /// </summary>
            /// <returns>数据是否有效</returns>
            public bool IsValid()
            {
                return matrixWidth > 0 && matrixHeight > 0 &&
                       occupancyMatrix != null && occupancyMatrix.Length == matrixWidth * matrixHeight &&
                       prefixSumArray != null && prefixSumArray.Length == (matrixWidth + 1) * (matrixHeight + 1) &&
                       occupiedCellsList != null;
            }
        }
    }

    // ==================== 背包检测器数据结构 ====================

    /// <summary>
    /// 背包负重分析信息
    /// </summary>
    [System.Serializable]
    public class BackpackWeightInfo
    {
        public string gridID;
        public int totalItems;
        public float totalWeight;
        public float averageWeight;
        public string heaviestItem;
        public string lightestItem;
        public Dictionary<string, float> weightDistribution;
        public List<string> overweightItems;
    }

    /// <summary>
    /// 背包整理建议信息
    /// </summary>
    [System.Serializable]
    public class BackpackOrganizationSuggestion
    {
        public string gridID;
        public float currentEfficiency;
        public List<string> suggestions;
        public List<string> priorityItems;
        public List<string> redundantItems;
        public List<string> misplacedItems;
    }

    /// <summary>
    /// 背包快速访问区域信息
    /// </summary>
    [System.Serializable]
    public class BackpackQuickAccessInfo
    {
        public string gridID;
        public List<QuickAccessZone> quickAccessZones;
        public Dictionary<string, Vector2Int> recommendedPlacements;
    }

    /// <summary>
    /// 快速访问区域定义
    /// </summary>
    [System.Serializable]
    public class QuickAccessZone
    {
        public string zoneName;
        public RectInt zoneArea;
        public int priority;
        public List<string> recommendedItemTypes;
        public List<string> currentItems;
        public float utilizationRate;
    }

    // ==================== 网格占用图谱数据结构 ====================

    /// <summary>
    /// 网格占用图谱数据结构
    /// 用于BackpackOccupancyMapManager的缓存和管理
    /// </summary>
    [System.Serializable]
    public class GridOccupancyMapData
    {
        public bool[] occupancyMatrix;        // 占用状态矩阵（一维数组形式）
        public int[] prefixSumArray;          // 前缀和数组（一维数组形式）
        public Vector2Int[] occupiedCellsList; // 占用单元格列表
        public int gridWidth;                 // 网格宽度
        public int gridHeight;                // 网格高度
        public string mapTimestamp;           // 图谱生成时间戳
        public int totalOccupiedCells;        // 总占用单元格数量
        public float occupancyPercentage;     // 占用百分比

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public GridOccupancyMapData()
        {
            occupancyMatrix = new bool[0];
            prefixSumArray = new int[0];
            occupiedCellsList = new Vector2Int[0];
            gridWidth = 0;
            gridHeight = 0;
            mapTimestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            totalOccupiedCells = 0;
            occupancyPercentage = 0f;
        }

        /// <summary>
        /// 带参数的构造函数
        /// </summary>
        /// <param name="width">网格宽度</param>
        /// <param name="height">网格高度</param>
        public GridOccupancyMapData(int width, int height)
        {
            gridWidth = width;
            gridHeight = height;
            occupancyMatrix = new bool[width * height];
            prefixSumArray = new int[(width + 1) * (height + 1)];
            occupiedCellsList = new Vector2Int[0];
            mapTimestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            totalOccupiedCells = 0;
            occupancyPercentage = 0f;
        }

        /// <summary>
        /// 验证图谱数据的完整性
        /// </summary>
        /// <returns>数据是否有效</returns>
        public bool IsValid()
        {
            return gridWidth > 0 && gridHeight > 0 &&
                   occupancyMatrix != null && occupancyMatrix.Length == gridWidth * gridHeight &&
                   prefixSumArray != null && prefixSumArray.Length == (gridWidth + 1) * (gridHeight + 1) &&
                   occupiedCellsList != null;
        }

        /// <summary>
        /// 从BackpackGridSaveData.GridOccupancyMapData转换
        /// </summary>
        /// <param name="saveData">保存数据中的占用图谱</param>
        /// <returns>转换后的占用图谱数据</returns>
        public static GridOccupancyMapData FromSaveData(BackpackGridSaveData.GridOccupancyMapData saveData)
        {
            if (saveData == null) return null;

            var mapData = new GridOccupancyMapData(saveData.matrixWidth, saveData.matrixHeight);
            mapData.occupancyMatrix = saveData.occupancyMatrix;
            mapData.prefixSumArray = saveData.prefixSumArray;
            mapData.occupiedCellsList = saveData.occupiedCellsList;
            mapData.mapTimestamp = saveData.mapTimestamp;
            mapData.totalOccupiedCells = saveData.totalOccupiedCells;
            mapData.occupancyPercentage = saveData.occupancyPercentage;

            return mapData;
        }

        /// <summary>
        /// 转换为BackpackGridSaveData.GridOccupancyMapData
        /// </summary>
        /// <returns>保存数据格式的占用图谱</returns>
        public BackpackGridSaveData.GridOccupancyMapData ToSaveData()
        {
            var saveData = new BackpackGridSaveData.GridOccupancyMapData(gridWidth, gridHeight);
            saveData.occupancyMatrix = occupancyMatrix;
            saveData.prefixSumArray = prefixSumArray;
            saveData.occupiedCellsList = occupiedCellsList;
            saveData.mapTimestamp = mapTimestamp;
            saveData.totalOccupiedCells = totalOccupiedCells;
            saveData.occupancyPercentage = occupancyPercentage;

            return saveData;
        }
    }
}