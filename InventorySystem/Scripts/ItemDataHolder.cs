using UnityEngine;
using UnityEngine.UI;

// 物品数据持有者组件，用于在预制体上持有和访问InventorySystemItemDataSO数据
public class ItemDataHolder : MonoBehaviour
{
    [Header("物品数据")]
    [SerializeField] private InventorySystemItemDataSO itemData;

    [Header("UI组件引用")]
    [SerializeField] private Image itemIconImage;
    [SerializeField] private RawImage backgroundImage;

    private void Awake()
    {
        // 如果有数据，自动更新UI显示
        if (itemData != null)
        {
            UpdateItemDisplay();
        }
    }

    // 获取物品数据
    // 返回：InventorySystemItemDataSO数据对象
    public InventorySystemItemDataSO GetItemData()
    {
        return itemData;
    }

    // 设置物品数据
    // data：要设置的InventorySystemItemDataSO数据
    public void SetItemData(InventorySystemItemDataSO data)
    {
        itemData = data;
        UpdateItemDisplay();
    }

    // 更新物品UI显示
    public void UpdateItemDisplay()
    {
        if (itemData == null) return;

        // 更新物品图标
        if (itemIconImage != null && itemData.itemIcon != null)
        {
            itemIconImage.sprite = itemData.itemIcon;
        }

        // 更新背景颜色
        if (backgroundImage != null)
        {
            backgroundImage.color = GetColorFromString(itemData.backgroundColor);
        }

        // 更新预制体尺寸
        UpdatePrefabSize();
    }

    // 根据物品网格尺寸更新预制体大小
    private void UpdatePrefabSize()
    {
        if (itemData == null) return;

        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // 每格50像素
            float cellSize = 50f;
            rectTransform.sizeDelta = new Vector2(
                itemData.width * cellSize,
                itemData.height * cellSize
            );
        }
    }

    // 将颜色字符串转换为Color
    // colorName：颜色名称
    // 返回：对应的Color值
    private Color GetColorFromString(string colorName)
    {
        switch (colorName?.ToLower())
        {
            case "blue":
                return HexToColor("#2d3c4b"); // 蓝色
            case "violet":
            case "purple":
                return HexToColor("#583b80"); // 紫色
            case "yellow":
                return HexToColor("#80550d"); // 黄色
            case "red":
                return HexToColor("#350000"); // 红色
            default:
                return Color.white; // 默认白色
        }
    }

    // 十六进制颜色转换为Color
    // hex：十六进制颜色值
    // 返回：Color对象
    private Color HexToColor(string hex)
    {
        // 移除#号
        if (hex.StartsWith("#"))
            hex = hex.Substring(1);

        // 确保是6位十六进制
        if (hex.Length != 6)
            return Color.white;

        try
        {
            byte r = System.Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = System.Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = System.Convert.ToByte(hex.Substring(4, 2), 16);

            return new Color32(r, g, b, 255);
        }
        catch
        {
            return Color.white; // 转换失败时返回白色
        }
    }

    // === 便捷访问属性 ===

    // 物品ID
    public int ItemID => itemData?.id ?? 0;

    // 物品名称
    public string ItemName => itemData?.itemName ?? "";

    // 物品高度
    public int ItemHeight => itemData?.height ?? 1;

    // 物品宽度
    public int ItemWidth => itemData?.width ?? 1;

    // 物品稀有度
    public string ItemRarity => itemData?.rarity ?? "";

    // 物品类别
    public string ItemCategory => itemData?.category ?? "";

    // 子弹类型
    public string BulletType => itemData?.bulletType ?? "";

    // 容量高度（背包/战术挂具）
    public int CellH => itemData?.cellH ?? 0;

    // 容量宽度（背包/战术挂具）
    public int CellV => itemData?.cellV ?? 0;

    // 背景颜色名称
    public string BackgroundColor => itemData?.backgroundColor ?? "";

    // 物品图标精灵
    public Sprite ItemIcon => itemData?.itemIcon;

    // === 实用方法 ===

    // 检查是否为背包类物品
    // 返回：是否为背包
    public bool IsBackpack()
    {
        return itemData != null && itemData.category.ToLower().Contains("backpack");
    }

    // 检查是否为武器类物品
    // 返回：是否为武器
    public bool IsWeapon()
    {
        return itemData != null && itemData.category.ToLower().Contains("weapon");
    }

    // 检查是否为弹药类物品
    // 返回：是否为弹药
    public bool IsAmmunition()
    {
        return itemData != null && itemData.category.ToLower().Contains("ammunition");
    }

    // 检查是否有容量（背包或战术挂具）
    // 返回：是否有容量
    public bool HasCapacity()
    {
        return itemData != null && (itemData.cellH > 0 || itemData.cellV > 0);
    }

    // 获取物品总容量
    // 返回：总容量（格子数）
    public int GetTotalCapacity()
    {
        return itemData != null ? itemData.cellH * itemData.cellV : 0;
    }

    // 获取物品占用的格子数
    // 返回：占用格子数
    public int GetOccupiedSlots()
    {
        return itemData != null ? itemData.height * itemData.width : 1;
    }

    // 调试信息输出
    [ContextMenu("输出物品信息")]
    public void LogItemInfo()
    {
        if (itemData == null)
        {
            Debug.Log("物品数据为空");
            return;
        }

        Debug.Log($"物品信息:\n" +
                  $"ID: {itemData.id}\n" +
                  $"名称: {itemData.itemName}\n" +
                  $"尺寸: {itemData.width}x{itemData.height}\n" +
                  $"稀有度: {itemData.rarity}\n" +
                  $"类别: {itemData.category}\n" +
                  $"背景颜色: {itemData.backgroundColor}\n" +
                  $"容量: {itemData.cellH}x{itemData.cellV}\n" +
                  $"子弹类型: {itemData.bulletType}");
    }
}