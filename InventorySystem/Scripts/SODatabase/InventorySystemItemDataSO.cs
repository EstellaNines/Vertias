using UnityEngine;
using System;
using System.Collections.Generic;

// 物品数据版本信息
[System.Serializable]
public class ItemDataVersion
{
    public int majorVersion = 1;     // 主版本号
    public int minorVersion = 0;     // 次版本号
    public int patchVersion = 0;     // 补丁版本号
    public string buildDate = "";    // 构建日期

    public ItemDataVersion()
    {
        buildDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public string GetVersionString()
    {
        return $"{majorVersion}.{minorVersion}.{patchVersion}";
    }

    public bool IsCompatibleWith(ItemDataVersion other)
    {
        // 主版本号相同才兼容
        return majorVersion == other.majorVersion;
    }
}

// MOD信息
[System.Serializable]
public class ModInfo
{
    public string modID = "";           // MOD唯一标识
    public string modName = "";         // MOD名称
    public string modVersion = "";      // MOD版本
    public string authorName = "";      // 作者名称
    public List<string> dependencies = new List<string>(); // 依赖的其他MOD

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(modID) && !string.IsNullOrEmpty(modName);
    }
}

// 数据完整性验证结果
[System.Serializable]
public class DataValidationResult
{
    public bool isValid = true;
    public List<string> errors = new List<string>();
    public List<string> warnings = new List<string>();

    public void AddError(string error)
    {
        isValid = false;
        errors.Add(error);
    }

    public void AddWarning(string warning)
    {
        warnings.Add(warning);
    }

    public string GetSummary()
    {
        string summary = isValid ? "验证通过" : "验证失败";
        if (errors.Count > 0) summary += $" - {errors.Count}个错误";
        if (warnings.Count > 0) summary += $" - {warnings.Count}个警告";
        return summary;
    }
}

public enum InventorySystemItemCategory
{
    Helmet,         // 头盔
    Armor,          // 护甲
    TacticalRig,    // 战术挂具
    Backpack,       // 背包
    Weapon,         // 武器
    Ammunition,     // 弹药
    Food,           // 食物
    Drink,          // 饮料
    Sedative,       // 镇静剂
    Hemostatic,     // 止血剂
    Healing,        // 治疗用品
    Intelligence,   // 情报
    Currency        // 货币
}

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory System/Item Data")]
public class InventorySystemItemDataSO : ScriptableObject
{
    [Header("版本信息")]
    [SerializeField] private ItemDataVersion dataVersion = new ItemDataVersion();

    [Header("MOD信息")]
    [SerializeField] private ModInfo modInfo = new ModInfo();

    [Header("数据校验")]
    [SerializeField] private string dataChecksum = "";     // 数据校验和
    [SerializeField] private bool isModified = false;      // 是否被修改过

    [Header("基本信息")]
    public int id;
    public string itemName;

    [Header("物品类别")]
    public InventorySystemItemCategory itemCategory = InventorySystemItemCategory.Helmet;

    [Header("网格尺寸")]
    public int height = 1;
    public int width = 1;

    [Header("珍贵程度")]
    public string rarity;

    [Header("容量信息(背包/战术挂具)")]
    [SerializeField] private int cellH;
    [SerializeField] private int cellV;

    [Header("子弹类型(弹药/武器)")]
    [SerializeField] private string bulletType;

    [Header("背景颜色")]
    public string backgroundColor;

    [Header("物品图标")]
    public Sprite itemIcon;

    [Header("缩写名称")]
    public string shortName;

    [Header("耐久值(头盔/护甲)")]
    public int durability;

    [Header("使用次数(食品/饮料/医疗用品)")]
    public int usageCount;

    [Header("最大回复血量(治疗用品)")]
    public int maxHealAmount;

    [Header("堆叠上限(货币/弹药)")]
    public int maxStack;

    [Header("情报值(情报物品)")]
    public int intelligenceValue;

    [HideInInspector]
    public string category;

    public int CellH
    {
        get => cellH;
        set => cellH = value;
    }

    public int CellV
    {
        get => cellV;
        set => cellV = value;
    }

    public string BulletType
    {
        get => bulletType;
        set => bulletType = value;
    }

    public int GetGridArea()
    {
        return height * width;
    }

    public bool IsValidGridSize()
    {
        return height > 0 && width > 0;
    }

    public int GetContainerArea()
    {
        return cellH * cellV;
    }

    public bool IsValidContainerSize()
    {
        return cellH > 0 && cellV > 0;
    }

    public bool IsContainer()
    {
        return itemCategory == InventorySystemItemCategory.Backpack || itemCategory == InventorySystemItemCategory.TacticalRig;
    }

    public bool ShowBulletType()
    {
        return itemCategory == InventorySystemItemCategory.Weapon || itemCategory == InventorySystemItemCategory.Ammunition;
    }

    // 版本信息属性
    public ItemDataVersion DataVersion
    {
        get => dataVersion;
        set => dataVersion = value;
    }

    // MOD信息属性
    public ModInfo ModInfo
    {
        get => modInfo;
        set => modInfo = value;
    }

    // 数据校验和属性
    public string DataChecksum
    {
        get => dataChecksum;
        set => dataChecksum = value;
    }

    // 是否被修改属性
    public bool IsModified
    {
        get => isModified;
        set => isModified = value;
    }

    // 计算数据校验和
    public string CalculateChecksum()
    {
        string dataString = $"{id}_{itemName}_{itemCategory}_{height}_{width}_{durability}_{maxStack}_{maxHealAmount}";
        return dataString.GetHashCode().ToString();
    }

    // 验证数据完整性
    public DataValidationResult ValidateData()
    {
        DataValidationResult result = new DataValidationResult();

        // 验证基本信息
        if (id <= 0)
        {
            result.AddError("物品ID必须大于0");
        }

        if (string.IsNullOrEmpty(itemName))
        {
            result.AddError("物品名称不能为空");
        }

        // 验证网格尺寸
        if (!IsValidGridSize())
        {
            result.AddError("网格尺寸无效，高度和宽度必须大于0");
        }

        // 验证容器尺寸（如果是容器类型）
        if (IsContainer() && !IsValidContainerSize())
        {
            result.AddError("容器尺寸无效，容器的高度和宽度必须大于0");
        }

        // 验证耐久值
        if (durability < 0)
        {
            result.AddWarning("耐久值不应为负数");
        }

        // 验证堆叠上限
        if (maxStack < 0)
        {
            result.AddWarning("堆叠上限不应为负数");
        }

        // 验证治疗量
        if (maxHealAmount < 0)
        {
            result.AddWarning("治疗量不应为负数");
        }

        // 验证使用次数
        if (usageCount < 0)
        {
            result.AddWarning("使用次数不应为负数");
        }

        // 验证图标
        if (itemIcon == null)
        {
            result.AddWarning("物品图标未设置");
        }

        // 验证数据校验和
        string currentChecksum = CalculateChecksum();
        if (!string.IsNullOrEmpty(dataChecksum) && dataChecksum != currentChecksum)
        {
            result.AddWarning("数据校验和不匹配，数据可能已被修改");
            isModified = true;
        }

        return result;
    }

    // 检查MOD兼容性
    public bool CheckModCompatibility(List<ModInfo> installedMods)
    {
        if (!modInfo.IsValid())
        {
            return true; // 非MOD物品，默认兼容
        }

        // 检查依赖的MOD是否都已安装
        foreach (string dependency in modInfo.dependencies)
        {
            bool dependencyFound = false;
            foreach (ModInfo installedMod in installedMods)
            {
                if (installedMod.modID == dependency)
                {
                    dependencyFound = true;
                    break;
                }
            }

            if (!dependencyFound)
            {
                Debug.LogWarning($"物品 {itemName} 依赖的MOD {dependency} 未找到");
                return false;
            }
        }

        return true;
    }

    // 更新版本信息
    public void UpdateVersion(int major = -1, int minor = -1, int patch = -1)
    {
        if (major >= 0) dataVersion.majorVersion = major;
        if (minor >= 0) dataVersion.minorVersion = minor;
        if (patch >= 0) dataVersion.patchVersion = patch;

        dataVersion.buildDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // 更新校验和
        dataChecksum = CalculateChecksum();
        isModified = false;
    }

    // 设置MOD信息
    public void SetModInfo(string modID, string modName, string modVersion, string authorName, List<string> dependencies = null)
    {
        modInfo.modID = modID;
        modInfo.modName = modName;
        modInfo.modVersion = modVersion;
        modInfo.authorName = authorName;

        if (dependencies != null)
        {
            modInfo.dependencies = new List<string>(dependencies);
        }

        // 更新校验和
        dataChecksum = CalculateChecksum();
        isModified = false;
    }

    // 获取物品详细信息
    public string GetDetailedInfo()
    {
        string info = $"物品: {itemName} (ID: {id})\n";
        info += $"类别: {itemCategory}\n";
        info += $"尺寸: {width}x{height}\n";
        info += $"版本: {dataVersion.GetVersionString()}\n";

        if (modInfo.IsValid())
        {
            info += $"MOD: {modInfo.modName} v{modInfo.modVersion}\n";
            info += $"作者: {modInfo.authorName}\n";
        }

        info += $"数据状态: {(isModified ? "已修改" : "原始")}\n";

        return info;
    }

    // 测试数据验证功能
    [ContextMenu("验证数据完整性")]
    public void TestDataValidation()
    {
        DataValidationResult result = ValidateData();
        Debug.Log($"数据验证结果: {result.GetSummary()}");

        foreach (string error in result.errors)
        {
            Debug.LogError($"错误: {error}");
        }

        foreach (string warning in result.warnings)
        {
            Debug.LogWarning($"警告: {warning}");
        }
    }

    // 测试版本更新
    [ContextMenu("更新版本信息")]
    public void TestUpdateVersion()
    {
        UpdateVersion(patch: dataVersion.patchVersion + 1);
        Debug.Log($"版本已更新为: {dataVersion.GetVersionString()}");
    }

    private void OnValidate()
    {
        category = itemCategory.ToString();

        // 在编辑器中修改时标记为已修改
        if (!string.IsNullOrEmpty(dataChecksum))
        {
            string currentChecksum = CalculateChecksum();
            if (dataChecksum != currentChecksum)
            {
                isModified = true;
            }
        }
    }
}