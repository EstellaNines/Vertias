// TacticalRigItemGrid.cs
// 战术挂具专用网格，继承 BaseItemGrid，支持运行时切换数据
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
// 移除不存在的命名空间引用
using InventorySystem.Grid;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class TactiaclRigItemGrid : BaseItemGrid
{
    [Header("战术挂具网格参数")]
    [SerializeField, Tooltip("默认宽度")] private int defaultWidth = 1;
    [SerializeField, Tooltip("默认高度")] private int defaultHeight = 1;

    // 当前战术挂具数据（运行时动态设置）
    private InventorySystemItemDataSO currentTacticalRigData;

    /* ---------------- 生命周期 ---------------- */
    protected override void Awake()
    {
        LoadFromTacticalRigData();
        base.Awake();
    }

    protected override void Start()
    {
        LoadFromTacticalRigData();
        base.Start();
    }

    protected override void OnValidate()
    {
        if (isUpdatingFromConfig) return;
        LoadFromTacticalRigData();
        width = Mathf.Clamp(width, 1, 20);
        height = Mathf.Clamp(height, 1, 20);
        base.OnValidate();
    }

    protected override void Init(int w, int h)
    {
        if (rectTransform == null) return;
        float cellSize = gridConfig != null ? gridConfig.cellSize : 64f;
        rectTransform.sizeDelta = new Vector2(w * cellSize, h * cellSize);
        if (Application.isPlaying) InitializeGridArrays();
    }

    /* ---------------- 动态数据 ---------------- */
    private void LoadFromTacticalRigData()
    {
        if (currentTacticalRigData != null && !isUpdatingFromConfig)
        {
            isUpdatingFromConfig = true;
            width = currentTacticalRigData.CellH;
            height = currentTacticalRigData.CellV;
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

    /// <summary>运行时更换战术挂具</summary>
    public void SetTacticalRigData(InventorySystemItemDataSO data)
    {
        currentTacticalRigData = data;
        LoadFromTacticalRigData();
        if (Application.isPlaying)
        {
            InitializeGridArrays();
            placedItems.Clear();

            // 运行时更换战术挂具数据后，重新生成保存ID
            GenerateNewSaveID();

            // 重新初始化保存系统
            InitializeSaveSystem();
        }
        Init(width, height);
    }

    public InventorySystemItemDataSO GetTacticalRigData() => currentTacticalRigData;

    /// <summary>挂具占用率</summary>
    public float GetTacticalRigOccupancyRate() => GetOccupancyRate();

    /// <summary>挂具剩余格子数</summary>
    public int GetRemainingSpace() => width * height - occupiedCells.Count;




    // ==================== ISaveable接口扩展实现 ====================

    /// <summary>
    /// 生成战术挂具网格专用的唯一标识ID
    /// 格式：TacticalRig_[挂具数据ID]_Grid
    /// 使用挂具数据的唯一ID确保同一个挂具重新装备时使用相同的保存ID
    /// </summary>
    public override void GenerateNewSaveID()
    {
        string rigDataID = "Unknown";
        if (currentTacticalRigData != null)
        {
            // 使用挂具数据的name作为唯一标识，确保同一个挂具数据总是生成相同的ID
            rigDataID = currentTacticalRigData.name.Replace(" ", "").Replace("/", "").Replace("\\", "");
        }

        gridID = $"TacticalRig_{rigDataID}_Grid";
        MarkAsModified();

        Debug.Log($"战术挂具网格生成稳定ID: {gridID}");
    }

    /// <summary>
    /// 获取战术挂具网格的保存数据
    /// 包含挂具数据路径和动态尺寸信息
    /// </summary>
    public override BaseItemGridSaveData GetSaveData()
    {
        var saveData = base.GetSaveData();

        // 创建战术挂具专用的保存数据
        var tacticalRigSaveData = new TacticalRigGridSaveData
        {
            gridID = saveData.gridID,
            saveVersion = saveData.saveVersion,
            gridWidth = saveData.gridWidth,
            gridHeight = saveData.gridHeight,
            placedItems = saveData.placedItems,
            lastModified = saveData.lastModified,
            isModified = saveData.isModified,

            // 战术挂具特定数据
            tacticalRigDataPath = GetTacticalRigDataPath(),
            defaultWidth = defaultWidth,
            defaultHeight = defaultHeight,
            hasActiveTacticalRig = currentTacticalRigData != null
        };

        return tacticalRigSaveData;
    }

    /// <summary>
    /// 从保存数据加载战术挂具网格状态
    /// 恢复挂具数据关联和网格配置
    /// </summary>
    public override bool LoadSaveData(BaseItemGridSaveData saveData)
    {
        try
        {
            // 尝试转换为战术挂具专用保存数据
            if (saveData is TacticalRigGridSaveData tacticalRigData)
            {
                // 恢复战术挂具数据关联
                if (!string.IsNullOrEmpty(tacticalRigData.tacticalRigDataPath))
                {
                    var rigData = Resources.Load<InventorySystemItemDataSO>(tacticalRigData.tacticalRigDataPath);
                    if (rigData != null)
                    {
                        SetTacticalRigData(rigData);
                        Debug.Log($"成功恢复战术挂具数据: {tacticalRigData.tacticalRigDataPath}");
                    }
                    else
                    {
                        Debug.LogWarning($"无法加载战术挂具数据: {tacticalRigData.tacticalRigDataPath}");
                        // 使用默认尺寸
                        defaultWidth = tacticalRigData.defaultWidth;
                        defaultHeight = tacticalRigData.defaultHeight;
                        LoadFromTacticalRigData();
                    }
                }
                else if (!tacticalRigData.hasActiveTacticalRig)
                {
                    // 没有活动挂具，使用默认配置
                    defaultWidth = tacticalRigData.defaultWidth;
                    defaultHeight = tacticalRigData.defaultHeight;
                    currentTacticalRigData = null;
                    LoadFromTacticalRigData();
                }
            }

            // 调用基类加载方法
            bool baseResult = base.LoadSaveData(saveData);

            if (baseResult)
            {
                Debug.Log($"战术挂具网格数据加载成功: {saveData.gridID}");
            }

            return baseResult;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"战术挂具网格数据加载失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 处理战术挂具卸载时的数据清理
    /// 清空网格内容并重置为默认状态
    /// </summary>
    public void OnTacticalRigUnequipped()
    {
        try
        {
            // 清空网格中的所有物品
            ClearGrid();

            // 清理物品实例ID映射
            itemInstanceMap.Clear();
            objectToInstanceID.Clear();

            // 重置为默认状态
            currentTacticalRigData = null;
            LoadFromTacticalRigData();

            // 重新初始化网格数组
            InitializeGridArrays();
            Init(width, height);

            // 标记为已修改并更新时间戳
            MarkAsModified();

            Debug.Log("战术挂具卸载，网格数据已清理");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"战术挂具卸载数据清理失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取当前战术挂具数据的资源路径
    /// </summary>
    /// <returns>挂具数据资源路径，如果没有则返回空字符串</returns>
    private string GetTacticalRigDataPath()
    {
        if (currentTacticalRigData == null) return "";

#if UNITY_EDITOR
        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(currentTacticalRigData);
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
    /// 验证战术挂具网格数据的完整性
    /// </summary>
    /// <returns>数据是否有效</returns>
    public override bool ValidateData()
    {
        // 调用基类验证
        if (!base.ValidateData())
        {
            return false;
        }

        // 验证战术挂具特定数据
        if (currentTacticalRigData != null)
        {
            // 验证挂具数据的有效性
            if (currentTacticalRigData.CellH <= 0 || currentTacticalRigData.CellV <= 0)
            {
                Debug.LogError("战术挂具数据中的网格尺寸无效");
                return false;
            }

            // 验证当前网格尺寸与挂具数据是否匹配
            if (width != currentTacticalRigData.CellH || height != currentTacticalRigData.CellV)
            {
                Debug.LogWarning("当前网格尺寸与战术挂具数据不匹配，将自动同步");
                LoadFromTacticalRigData();
            }
        }

        return true;
    }

    public override string GetSaveID()
    {
        return "TacticalRigItemGrid";
    }

    // ==================== 战术挂具网格检测器扩展功能 ====================

    /// <summary>
    /// 获取战术挂具网格特有的检测器信息
    /// 包含战术挂具网格的特殊属性和状态
    /// </summary>
    /// <returns>战术挂具网格检测器信息</returns>
    public override GridDetectorInfo GetGridDetectorInfo()
    {
        var baseInfo = base.GetGridDetectorInfo();

        // 添加战术挂具网格特有信息
        baseInfo.gridType = "战术挂具网格 (TacticalRigItemGrid)";

        return baseInfo;
    }

    /// <summary>
    /// 获取战术挂具配置分析信息
    /// 分析挂具中装备的配置合理性和战术效能
    /// </summary>
    /// <returns>战术挂具配置分析信息</returns>
    public TacticalRigConfigInfo GetTacticalRigConfigInfo()
    {
        var configInfo = new TacticalRigConfigInfo
        {
            gridID = GetSaveID(),
            totalSlots = width * height,
            usedSlots = occupiedCells.Count,
            configurationScore = 0f,
            tacticalEfficiency = 0f,
            loadoutType = DetermineLoadoutType(),
            equipmentBalance = AnalyzeEquipmentBalance(),
            accessibilityRating = CalculateAccessibilityRating(),
            combatReadiness = AssessCombatReadiness()
        };

        // 计算配置评分
        configInfo.configurationScore = CalculateConfigurationScore();
        configInfo.tacticalEfficiency = CalculateTacticalEfficiency();

        // 生成配置建议
        GenerateConfigurationSuggestions(configInfo);

        return configInfo;
    }

    /// <summary>
    /// 确定装备配置类型
    /// </summary>
    /// <returns>装备配置类型</returns>
    private LoadoutType DetermineLoadoutType()
    {
        var weaponCount = 0;
        var ammoCount = 0;
        var utilityCount = 0;
        var medicalCount = 0;

        foreach (var placedItem in placedItems)
        {
            if (placedItem.itemObject == null) continue;

            var inventoryItem = placedItem.itemObject.GetComponent<InventorySystemItem>();
            if (inventoryItem?.Data?.itemCategory == null) continue;

            string typeName = inventoryItem.Data.itemCategory.ToString().ToLower();

            if (typeName.Contains("weapon") || typeName.Contains("武器"))
                weaponCount++;
            else if (typeName.Contains("ammo") || typeName.Contains("弹药"))
                ammoCount++;
            else if (typeName.Contains("medical") || typeName.Contains("医疗"))
                medicalCount++;
            else
                utilityCount++;
        }

        // 根据装备分布确定配置类型
        if (weaponCount >= 2 && ammoCount >= 3)
            return LoadoutType.Assault;  // 突击配置
        else if (weaponCount >= 1 && medicalCount >= 2)
            return LoadoutType.Support;  // 支援配置
        else if (ammoCount >= 4)
            return LoadoutType.Marksman; // 射手配置
        else if (utilityCount >= 3)
            return LoadoutType.Utility;  // 工具配置
        else
            return LoadoutType.Balanced; // 平衡配置
    }

    /// <summary>
    /// 分析装备平衡性
    /// </summary>
    /// <returns>装备平衡性评分</returns>
    private float AnalyzeEquipmentBalance()
    {
        if (placedItems.Count == 0) return 1.0f;

        var categoryCount = new Dictionary<string, int>();

        foreach (var placedItem in placedItems)
        {
            if (placedItem.itemObject == null) continue;

            var inventoryItem = placedItem.itemObject.GetComponent<InventorySystemItem>();
            if (inventoryItem?.Data?.itemCategory == null) continue;

            string category = GetItemCategory(inventoryItem.Data.itemCategory.ToString());

            if (categoryCount.ContainsKey(category))
                categoryCount[category]++;
            else
                categoryCount[category] = 1;
        }

        // 计算平衡性（类别分布越均匀，平衡性越高）
        if (categoryCount.Count <= 1) return 0.5f;

        float totalItems = placedItems.Count;
        float variance = 0f;
        float averagePerCategory = totalItems / categoryCount.Count;

        foreach (var count in categoryCount.Values)
        {
            variance += Mathf.Pow(count - averagePerCategory, 2);
        }

        variance /= categoryCount.Count;

        // 方差越小，平衡性越高
        return Mathf.Clamp01(1.0f - (variance / (totalItems * totalItems)));
    }

    /// <summary>
    /// 获取物品类别
    /// </summary>
    /// <param name="itemTypeName">物品类型名称</param>
    /// <returns>物品类别</returns>
    private string GetItemCategory(string itemTypeName)
    {
        string typeName = itemTypeName.ToLower();

        if (typeName.Contains("weapon") || typeName.Contains("武器"))
            return "武器";
        else if (typeName.Contains("ammo") || typeName.Contains("弹药"))
            return "弹药";
        else if (typeName.Contains("medical") || typeName.Contains("医疗"))
            return "医疗";
        else if (typeName.Contains("grenade") || typeName.Contains("手雷"))
            return "爆炸物";
        else if (typeName.Contains("tool") || typeName.Contains("工具"))
            return "工具";
        else
            return "其他";
    }

    /// <summary>
    /// 计算可访问性评级
    /// </summary>
    /// <returns>可访问性评分</returns>
    private float CalculateAccessibilityRating()
    {
        if (placedItems.Count == 0) return 1.0f;

        float totalAccessibility = 0f;

        foreach (var placedItem in placedItems)
        {
            // 计算物品位置的可访问性（越靠近边缘越容易访问）
            float edgeDistance = CalculateEdgeDistance(placedItem.position, placedItem.size);
            float maxDistance = Mathf.Max(width, height) / 2f;
            float accessibility = 1.0f - (edgeDistance / maxDistance);

            totalAccessibility += accessibility;
        }

        return totalAccessibility / placedItems.Count;
    }

    /// <summary>
    /// 计算到边缘的距离
    /// </summary>
    /// <param name="position">物品位置</param>
    /// <param name="size">物品尺寸</param>
    /// <returns>到边缘的最小距离</returns>
    private float CalculateEdgeDistance(Vector2Int position, Vector2Int size)
    {
        int centerX = position.x + size.x / 2;
        int centerY = position.y + size.y / 2;

        int distanceToLeft = centerX;
        int distanceToRight = width - centerX;
        int distanceToTop = centerY;
        int distanceToBottom = height - centerY;

        return Mathf.Min(distanceToLeft, distanceToRight, distanceToTop, distanceToBottom);
    }

    /// <summary>
    /// 评估战斗准备度
    /// </summary>
    /// <returns>战斗准备度评分</returns>
    private float AssessCombatReadiness()
    {
        float readinessScore = 0f;

        // 检查必需装备
        bool hasWeapon = false;
        bool hasAmmo = false;
        bool hasMedical = false;
        int weaponCount = 0;
        int ammoCount = 0;

        foreach (var placedItem in placedItems)
        {
            if (placedItem.itemObject == null) continue;

            var inventoryItem = placedItem.itemObject.GetComponent<InventorySystemItem>();
            if (inventoryItem?.Data?.itemCategory == null) continue;

            string typeName = inventoryItem.Data.itemCategory.ToString().ToLower();

            if (typeName.Contains("weapon") || typeName.Contains("武器"))
            {
                hasWeapon = true;
                weaponCount++;
            }
            else if (typeName.Contains("ammo") || typeName.Contains("弹药"))
            {
                hasAmmo = true;
                ammoCount++;
            }
            else if (typeName.Contains("medical") || typeName.Contains("医疗"))
            {
                hasMedical = true;
            }
        }

        // 基础装备检查
        if (hasWeapon) readinessScore += 0.4f;
        if (hasAmmo) readinessScore += 0.3f;
        if (hasMedical) readinessScore += 0.2f;

        // 装备数量加成
        if (weaponCount >= 2) readinessScore += 0.05f;
        if (ammoCount >= 3) readinessScore += 0.05f;

        return Mathf.Clamp01(readinessScore);
    }

    /// <summary>
    /// 计算配置评分
    /// </summary>
    /// <returns>配置评分</returns>
    private float CalculateConfigurationScore()
    {
        if (placedItems.Count == 0) return 0f;

        float spaceUtilization = GetOccupancyRate();
        float equipmentBalance = AnalyzeEquipmentBalance();
        float accessibility = CalculateAccessibilityRating();
        float combatReadiness = AssessCombatReadiness();

        // 加权平均计算总评分
        return (spaceUtilization * 0.2f + equipmentBalance * 0.3f + accessibility * 0.25f + combatReadiness * 0.25f);
    }

    /// <summary>
    /// 计算战术效率
    /// </summary>
    /// <returns>战术效率评分</returns>
    private float CalculateTacticalEfficiency()
    {
        float configScore = CalculateConfigurationScore();
        float quickAccessBonus = CalculateQuickAccessBonus();
        float redundancyPenalty = CalculateRedundancyPenalty();

        return Mathf.Clamp01(configScore + quickAccessBonus - redundancyPenalty);
    }

    /// <summary>
    /// 计算快速访问加成
    /// </summary>
    /// <returns>快速访问加成分数</returns>
    private float CalculateQuickAccessBonus()
    {
        float bonus = 0f;

        // 检查关键物品是否在易访问位置
        foreach (var placedItem in placedItems)
        {
            if (placedItem.itemObject == null) continue;

            var inventoryItem = placedItem.itemObject.GetComponent<InventorySystemItem>();
            if (inventoryItem?.Data?.itemCategory == null) continue;

            string typeName = inventoryItem.Data.itemCategory.ToString().ToLower();
            bool isCriticalItem = typeName.Contains("weapon") || typeName.Contains("medical") ||
                                typeName.Contains("武器") || typeName.Contains("医疗");

            if (isCriticalItem)
            {
                float edgeDistance = CalculateEdgeDistance(placedItem.position, placedItem.size);
                if (edgeDistance <= 1) // 在边缘位置
                {
                    bonus += 0.05f;
                }
            }
        }

        return Mathf.Clamp01(bonus);
    }

    /// <summary>
    /// 计算冗余惩罚
    /// </summary>
    /// <returns>冗余惩罚分数</returns>
    private float CalculateRedundancyPenalty()
    {
        var itemTypeCount = new Dictionary<string, int>();

        foreach (var placedItem in placedItems)
        {
            if (placedItem.itemObject == null) continue;

            var inventoryItem = placedItem.itemObject.GetComponent<InventorySystemItem>();
            if (inventoryItem?.Data?.itemCategory == null) continue;

            string typeName = inventoryItem.Data.itemCategory.ToString();

            if (itemTypeCount.ContainsKey(typeName))
                itemTypeCount[typeName]++;
            else
                itemTypeCount[typeName] = 1;
        }

        float penalty = 0f;

        foreach (var count in itemTypeCount.Values)
        {
            if (count > 3) // 超过3个同类型物品视为冗余
            {
                penalty += (count - 3) * 0.02f;
            }
        }

        return Mathf.Clamp01(penalty);
    }

    /// <summary>
    /// 生成配置建议
    /// </summary>
    /// <param name="configInfo">配置信息对象</param>
    private void GenerateConfigurationSuggestions(TacticalRigConfigInfo configInfo)
    {
        configInfo.suggestions = new List<string>();
        configInfo.optimizationTips = new List<string>();

        // 基于配置评分生成建议
        if (configInfo.configurationScore < 0.6f)
        {
            configInfo.suggestions.Add("当前配置效率较低，建议重新规划装备布局");
        }

        if (configInfo.equipmentBalance < 0.5f)
        {
            configInfo.suggestions.Add("装备类型分布不均衡，建议增加多样性");
        }

        if (configInfo.accessibilityRating < 0.6f)
        {
            configInfo.suggestions.Add("关键装备访问性较差，建议将重要物品放置在边缘位置");
        }

        if (configInfo.combatReadiness < 0.7f)
        {
            configInfo.suggestions.Add("战斗准备度不足，建议增加武器、弹药或医疗用品");
        }

        // 生成优化提示
        GenerateOptimizationTips(configInfo);
    }

    /// <summary>
    /// 生成优化提示
    /// </summary>
    /// <param name="configInfo">配置信息对象</param>
    private void GenerateOptimizationTips(TacticalRigConfigInfo configInfo)
    {
        // 根据装备配置类型提供特定建议
        switch (configInfo.loadoutType)
        {
            case LoadoutType.Assault:
                configInfo.optimizationTips.Add("突击配置：确保弹药充足，考虑添加手雷");
                break;
            case LoadoutType.Support:
                configInfo.optimizationTips.Add("支援配置：增加医疗用品，保持武器多样性");
                break;
            case LoadoutType.Marksman:
                configInfo.optimizationTips.Add("射手配置：优化弹药布局，添加观察工具");
                break;
            case LoadoutType.Utility:
                configInfo.optimizationTips.Add("工具配置：平衡工具与战斗装备的比例");
                break;
            case LoadoutType.Balanced:
                configInfo.optimizationTips.Add("平衡配置：保持当前配置，可微调提升效率");
                break;
        }

        // 空间利用建议
        float occupancyRate = GetOccupancyRate();
        if (occupancyRate < 0.5f)
        {
            configInfo.optimizationTips.Add("空间利用率较低，可以添加更多装备");
        }
        else if (occupancyRate > 0.9f)
        {
            configInfo.optimizationTips.Add("空间几乎满载，注意保留机动性");
        }
    }

    /// <summary>
    /// 获取战术挂具插槽分析信息
    /// 分析各个插槽的使用效率和优化建议
    /// </summary>
    /// <returns>插槽分析信息</returns>
    public TacticalRigSlotAnalysis GetSlotAnalysis()
    {
        var slotAnalysis = new TacticalRigSlotAnalysis
        {
            gridID = GetSaveID(),
            totalSlots = width * height,
            usedSlots = occupiedCells.Count,
            slotEfficiency = new Dictionary<Vector2Int, float>(),
            hotSpots = new List<Vector2Int>(),
            coldSpots = new List<Vector2Int>(),
            recommendedSlotUsage = new Dictionary<Vector2Int, string>()
        };

        // 分析每个插槽的效率
        AnalyzeSlotEfficiency(slotAnalysis);

        // 识别热点和冷点
        IdentifyHotAndColdSpots(slotAnalysis);

        // 生成插槽使用建议
        GenerateSlotUsageRecommendations(slotAnalysis);

        return slotAnalysis;
    }

    /// <summary>
    /// 分析插槽效率
    /// </summary>
    /// <param name="slotAnalysis">插槽分析对象</param>
    private void AnalyzeSlotEfficiency(TacticalRigSlotAnalysis slotAnalysis)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int slotPos = new Vector2Int(x, y);
                float efficiency = CalculateSlotEfficiency(slotPos);
                slotAnalysis.slotEfficiency[slotPos] = efficiency;
            }
        }
    }

    /// <summary>
    /// 计算单个插槽的效率
    /// </summary>
    /// <param name="slotPos">插槽位置</param>
    /// <returns>插槽效率评分</returns>
    private float CalculateSlotEfficiency(Vector2Int slotPos)
    {
        // 基于位置计算效率（边缘位置效率更高）
        float edgeDistance = CalculateEdgeDistance(slotPos, Vector2Int.one);
        float maxDistance = Mathf.Max(width, height) / 2f;
        float positionEfficiency = 1.0f - (edgeDistance / maxDistance);

        // 检查是否被占用
        bool isOccupied = occupiedCells.Contains(slotPos);

        // 被占用的插槽根据物品类型调整效率
        if (isOccupied)
        {
            var placedItem = FindItemAtPosition(slotPos);
            if (placedItem != null)
            {
                var inventoryItem = placedItem.itemObject?.GetComponent<InventorySystemItem>();
                if (inventoryItem?.Data?.itemCategory != null)
                {
                    string typeName = inventoryItem.Data.itemCategory.ToString().ToLower();

                    // 关键物品在边缘位置效率更高
                    if (typeName.Contains("weapon") || typeName.Contains("medical") ||
                        typeName.Contains("武器") || typeName.Contains("医疗"))
                    {
                        positionEfficiency *= 1.2f; // 关键物品加成
                    }
                }
            }
        }

        return Mathf.Clamp01(positionEfficiency);
    }

    /// <summary>
    /// 识别热点和冷点插槽
    /// </summary>
    /// <param name="slotAnalysis">插槽分析对象</param>
    private void IdentifyHotAndColdSpots(TacticalRigSlotAnalysis slotAnalysis)
    {
        float averageEfficiency = 0f;
        foreach (var efficiency in slotAnalysis.slotEfficiency.Values)
        {
            averageEfficiency += efficiency;
        }
        averageEfficiency /= slotAnalysis.slotEfficiency.Count;

        foreach (var kvp in slotAnalysis.slotEfficiency)
        {
            if (kvp.Value > averageEfficiency * 1.2f)
            {
                slotAnalysis.hotSpots.Add(kvp.Key);
            }
            else if (kvp.Value < averageEfficiency * 0.8f)
            {
                slotAnalysis.coldSpots.Add(kvp.Key);
            }
        }
    }

    /// <summary>
    /// 生成插槽使用建议
    /// </summary>
    /// <param name="slotAnalysis">插槽分析对象</param>
    private void GenerateSlotUsageRecommendations(TacticalRigSlotAnalysis slotAnalysis)
    {
        // 为热点插槽推荐关键物品
        foreach (var hotSpot in slotAnalysis.hotSpots)
        {
            if (!occupiedCells.Contains(hotSpot))
            {
                slotAnalysis.recommendedSlotUsage[hotSpot] = "推荐放置：武器或医疗用品";
            }
        }

        // 为冷点插槽推荐次要物品
        foreach (var coldSpot in slotAnalysis.coldSpots)
        {
            if (!occupiedCells.Contains(coldSpot))
            {
                slotAnalysis.recommendedSlotUsage[coldSpot] = "推荐放置：工具或备用物品";
            }
        }
    }

    /// <summary>
    /// 获取战术挂具负载平衡信息
    /// 分析挂具的重量分布和平衡性
    /// </summary>
    /// <returns>负载平衡信息</returns>
    public TacticalRigLoadBalance GetLoadBalanceInfo()
    {
        var loadBalance = new TacticalRigLoadBalance
        {
            gridID = GetSaveID(),
            totalWeight = 0f,
            weightDistribution = new Dictionary<string, float>(),
            balanceScore = 0f,
            centerOfMass = Vector2.zero,
            balanceRecommendations = new List<string>()
        };

        // 计算重量分布
        CalculateWeightDistribution(loadBalance);

        // 计算重心
        CalculateCenterOfMass(loadBalance);

        // 计算平衡评分
        CalculateBalanceScore(loadBalance);

        // 生成平衡建议
        GenerateBalanceRecommendations(loadBalance);

        return loadBalance;
    }

    /// <summary>
    /// 计算重量分布
    /// </summary>
    /// <param name="loadBalance">负载平衡对象</param>
    private void CalculateWeightDistribution(TacticalRigLoadBalance loadBalance)
    {
        foreach (var placedItem in placedItems)
        {
            if (placedItem.itemObject == null) continue;

            var inventoryItem = placedItem.itemObject.GetComponent<InventorySystemItem>();
            if (inventoryItem?.Data == null) continue;

            float itemWeight = GetEstimatedWeight(inventoryItem.Data);
            string category = GetItemCategory(inventoryItem.Data.itemCategory.ToString());

            loadBalance.totalWeight += itemWeight;

            if (loadBalance.weightDistribution.ContainsKey(category))
                loadBalance.weightDistribution[category] += itemWeight;
            else
                loadBalance.weightDistribution[category] = itemWeight;
        }
    }

    /// <summary>
    /// 获取物品估算重量
    /// </summary>
    /// <param name="itemData">物品数据</param>
    /// <returns>估算重量</returns>
    private float GetEstimatedWeight(InventorySystemItemDataSO itemData)
    {
        if (itemData == null) return 0f;

        // 基于物品尺寸和类型估算重量
        float baseWeight = itemData.width * itemData.height * 0.3f;

        string typeName = itemData.itemCategory.ToString().ToLower();

        if (typeName.Contains("weapon") || typeName.Contains("武器"))
            return baseWeight * 2.5f;
        else if (typeName.Contains("ammo") || typeName.Contains("弹药"))
            return baseWeight * 1.2f;
        else if (typeName.Contains("medical") || typeName.Contains("医疗"))
            return baseWeight * 0.8f;
        else
            return baseWeight;
    }

    /// <summary>
    /// 计算重心
    /// </summary>
    /// <param name="loadBalance">负载平衡对象</param>
    private void CalculateCenterOfMass(TacticalRigLoadBalance loadBalance)
    {
        if (loadBalance.totalWeight == 0f)
        {
            loadBalance.centerOfMass = new Vector2(width / 2f, height / 2f);
            return;
        }

        float weightedX = 0f;
        float weightedY = 0f;

        foreach (var placedItem in placedItems)
        {
            if (placedItem.itemObject == null) continue;

            var inventoryItem = placedItem.itemObject.GetComponent<InventorySystemItem>();
            if (inventoryItem?.Data == null) continue;

            float itemWeight = GetEstimatedWeight(inventoryItem.Data);
            Vector2 itemCenter = new Vector2(
                placedItem.position.x + placedItem.size.x / 2f,
                placedItem.position.y + placedItem.size.y / 2f
            );

            weightedX += itemCenter.x * itemWeight;
            weightedY += itemCenter.y * itemWeight;
        }

        loadBalance.centerOfMass = new Vector2(
            weightedX / loadBalance.totalWeight,
            weightedY / loadBalance.totalWeight
        );
    }

    /// <summary>
    /// 计算平衡评分
    /// </summary>
    /// <param name="loadBalance">负载平衡对象</param>
    private void CalculateBalanceScore(TacticalRigLoadBalance loadBalance)
    {
        Vector2 gridCenter = new Vector2(width / 2f, height / 2f);
        float distanceFromCenter = Vector2.Distance(loadBalance.centerOfMass, gridCenter);
        float maxDistance = Vector2.Distance(Vector2.zero, gridCenter);

        // 重心越接近中心，平衡性越好
        loadBalance.balanceScore = Mathf.Clamp01(1.0f - (distanceFromCenter / maxDistance));
    }

    /// <summary>
    /// 生成平衡建议
    /// </summary>
    /// <param name="loadBalance">负载平衡对象</param>
    private void GenerateBalanceRecommendations(TacticalRigLoadBalance loadBalance)
    {
        Vector2 gridCenter = new Vector2(width / 2f, height / 2f);
        Vector2 offset = loadBalance.centerOfMass - gridCenter;

        if (loadBalance.balanceScore < 0.7f)
        {
            if (Mathf.Abs(offset.x) > Mathf.Abs(offset.y))
            {
                if (offset.x > 0)
                    loadBalance.balanceRecommendations.Add("重心偏右，建议在左侧添加重物或移动右侧重物");
                else
                    loadBalance.balanceRecommendations.Add("重心偏左，建议在右侧添加重物或移动左侧重物");
            }
            else
            {
                if (offset.y > 0)
                    loadBalance.balanceRecommendations.Add("重心偏上，建议在下方添加重物或移动上方重物");
                else
                    loadBalance.balanceRecommendations.Add("重心偏下，建议在上方添加重物或移动下方重物");
            }
        }
        else
        {
            loadBalance.balanceRecommendations.Add("当前负载平衡良好，保持现有配置");
        }
    }

    /// <summary>
    /// 初始化战术挂具网格的保存系统
    /// 在Awake中调用以确保保存系统正确初始化
    /// </summary>
    protected override void InitializeSaveSystem()
    {
        base.InitializeSaveSystem();

        // 如果没有有效ID，生成战术挂具专用ID
        if (!IsSaveIDValid())
        {
            GenerateNewSaveID();
        }

        Debug.Log($"战术挂具网格保存系统初始化完成: {GetSaveID()}");
    }

    // ==================== 战术挂具专用保存数据类 ====================

    [System.Serializable]
    public class TacticalRigGridSaveData : BaseItemGridSaveData
    {
        public string tacticalRigDataPath;    // 战术挂具数据资源路径
        public int defaultWidth;              // 默认宽度
        public int defaultHeight;             // 默认高度
        public bool hasActiveTacticalRig;     // 是否有活动的战术挂具
        public float occupancyRate;           // 占用率
        public GridOccupancyMapData occupancyMapData; // 网格占用图谱数据
        
        /// <summary>
        /// 战术挂具网格占用图谱数据结构
        /// </summary>
        [System.Serializable]
        public class GridOccupancyMapData
        {
            public int[,] gridOccupancy;          // 网格占用状态矩阵
            public int[,] prefixSum;              // 前缀和矩阵（用于快速查询）
            public List<Vector2Int> occupiedCells; // 已占用单元格列表
            public int totalOccupiedCells;        // 总占用单元格数
            public float occupancyRate;           // 占用率
            public string lastUpdateTime;         // 最后更新时间
            public int mapVersion;                // 图谱版本号
        }
    }
}

// ==================== 战术挂具分析数据结构 ====================

/// <summary>
/// 装备配置类型枚举
/// </summary>
public enum LoadoutType
{
    Assault,    // 突击配置
    Support,    // 支援配置
    Marksman,   // 射手配置
    Utility,    // 工具配置
    Balanced    // 平衡配置
}

/// <summary>
/// 战术挂具配置分析信息
/// </summary>
[System.Serializable]
public class TacticalRigConfigInfo
{
    public string gridID;                           // 网格ID
    public int totalSlots;                          // 总插槽数
    public int usedSlots;                           // 已使用插槽数
    public float configurationScore;                // 配置评分 (0-1)
    public float tacticalEfficiency;                // 战术效率 (0-1)
    public LoadoutType loadoutType;                 // 装备配置类型
    public float equipmentBalance;                  // 装备平衡性 (0-1)
    public float accessibilityRating;               // 可访问性评级 (0-1)
    public float combatReadiness;                   // 战斗准备度 (0-1)
    public List<string> suggestions;                // 配置建议
    public List<string> optimizationTips;           // 优化提示
}

/// <summary>
/// 战术挂具插槽分析信息
/// </summary>
[System.Serializable]
public class TacticalRigSlotAnalysis
{
    public string gridID;                                       // 网格ID
    public int totalSlots;                                      // 总插槽数
    public int usedSlots;                                       // 已使用插槽数
    public Dictionary<Vector2Int, float> slotEfficiency;        // 插槽效率映射
    public List<Vector2Int> hotSpots;                           // 高效率插槽（热点）
    public List<Vector2Int> coldSpots;                          // 低效率插槽（冷点）
    public Dictionary<Vector2Int, string> recommendedSlotUsage; // 推荐插槽用途
}

/// <summary>
/// 战术挂具负载平衡信息
/// </summary>
[System.Serializable]
public class TacticalRigLoadBalance
{
    public string gridID;                               // 网格ID
    public float totalWeight;                           // 总重量
    public Dictionary<string, float> weightDistribution; // 重量分布（按类别）
    public float balanceScore;                          // 平衡评分 (0-1)
    public Vector2 centerOfMass;                        // 重心位置
    public List<string> balanceRecommendations;         // 平衡建议
}