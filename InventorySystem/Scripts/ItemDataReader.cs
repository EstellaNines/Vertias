using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InventorySystem;

/// <summary>
/// 物品数据读取器 - 用于读取和显示物品的ScriptableObject数据
/// 这个脚本应该挂载在物品预制体的主对象上
/// </summary>
public class ItemDataReader : MonoBehaviour
{
    [Header("物品数据")]
    [SerializeField] private ItemDataSO itemData;

    [Header("UI组件引用")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI displayText;

    [Header("运行时信息")]
    [SerializeField] private int currentStack = 1;  // 当前堆叠数量
    [SerializeField] private int currentDurability; // 当前耐久度
    [SerializeField] private int currentUsageCount; // 当前使用次数

    /// <summary>
    /// 获取物品数据
    /// </summary>
    public ItemDataSO ItemData => itemData;

    /// <summary>
    /// 获取当前堆叠数量
    /// </summary>
    public int CurrentStack => currentStack;

    /// <summary>
    /// 获取当前耐久度
    /// </summary>
    public int CurrentDurability => currentDurability;

    /// <summary>
    /// 获取当前使用次数
    /// </summary>
    public int CurrentUsageCount => currentUsageCount;

    private void Awake()
    {
        // 自动查找UI组件
        if (backgroundImage == null)
            backgroundImage = transform.Find("ItemBackground")?.GetComponent<Image>();
        if (iconImage == null)
            iconImage = transform.Find("ItemIcon")?.GetComponent<Image>();
        if (displayText == null)
            displayText = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Start()
    {
        // 初始化运行时数据
        InitializeRuntimeData();

        // 更新UI显示
        UpdateUI();
    }

    /// <summary>
    /// 设置物品数据（由预制体生成器调用）
    /// </summary>
    /// <param name="data">物品数据SO</param>
    public void SetItemData(ItemDataSO data)
    {
        itemData = data;
        InitializeRuntimeData();
        UpdateUI();
    }

    /// <summary>
    /// 初始化运行时数据
    /// </summary>
    private void InitializeRuntimeData()
    {
        if (itemData == null) return;

        // 根据物品类型初始化运行时数据
        currentStack = 1;
        currentDurability = itemData.durability;
        currentUsageCount = itemData.usageCount;
    }

    /// <summary>
    /// 更新UI显示
    /// </summary>
    public void UpdateUI()
    {
        if (itemData == null) return;

        // 更新背景颜色
        if (backgroundImage != null)
        {
            backgroundImage.color = itemData.backgroundColor;
        }

        // 更新图标
        if (iconImage != null && itemData.itemIcon != null)
        {
            iconImage.sprite = itemData.itemIcon;
        }

        // 更新文本显示
        if (displayText != null)
        {
            displayText.text = GetDisplayText();
        }
    }

    /// <summary>
    /// 获取要显示的文本内容
    /// </summary>
    /// <returns>显示文本</returns>
    private string GetDisplayText()
    {
        if (itemData == null) return "";

        switch (itemData.category)
        {
            case ItemCategory.Ammunition:
                // 弹药显示当前数量/最大堆叠数量
                return itemData.maxStack > 1 ? $"{currentStack}/{itemData.maxStack}" : "";

            case ItemCategory.Currency:
                // 货币显示当前数量
                return itemData.maxStack > 1 ? currentStack.ToString() : "";

            case ItemCategory.Food:
            case ItemCategory.Drink:
            case ItemCategory.Sedative:
            case ItemCategory.Hemostatic:
                // 消耗品显示使用次数
                return currentUsageCount > 0 ? currentUsageCount.ToString() : "";

            case ItemCategory.Healing:
                // 治疗物品显示治疗量
                return itemData.maxHealAmount > 0 ? itemData.maxHealAmount.ToString() : "";

            case ItemCategory.Intelligence:
                // 智力物品显示智力值
                return itemData.intelligenceValue > 0 ? itemData.intelligenceValue.ToString() : "";

            default:
                return "";
        }
    }

    /// <summary>
    /// 设置堆叠数量
    /// </summary>
    /// <param name="stack">堆叠数量</param>
    public void SetStack(int stack)
    {
        if (itemData == null) return;

        currentStack = Mathf.Clamp(stack, 1, itemData.maxStack);
        UpdateUI();
    }

    /// <summary>
    /// 增加堆叠数量
    /// </summary>
    /// <param name="amount">增加数量</param>
    /// <returns>实际增加的数量</returns>
    public int AddStack(int amount)
    {
        if (itemData == null || amount <= 0) return 0;

        int oldStack = currentStack;
        currentStack = Mathf.Clamp(currentStack + amount, 1, itemData.maxStack);
        UpdateUI();

        return currentStack - oldStack;
    }

    /// <summary>
    /// 减少堆叠数量
    /// </summary>
    /// <param name="amount">减少数量</param>
    /// <returns>实际减少的数量</returns>
    public int RemoveStack(int amount)
    {
        if (itemData == null || amount <= 0) return 0;

        int oldStack = currentStack;
        currentStack = Mathf.Max(currentStack - amount, 0);
        UpdateUI();

        return oldStack - currentStack;
    }

    /// <summary>
    /// 设置耐久度
    /// </summary>
    /// <param name="durability">耐久度值</param>
    public void SetDurability(int durability)
    {
        if (itemData == null || itemData.durability <= 0) return;

        currentDurability = Mathf.Clamp(durability, 0, itemData.durability);
        UpdateUI();
    }

    /// <summary>
    /// 减少耐久度
    /// </summary>
    /// <param name="amount">减少数量</param>
    /// <returns>是否已损坏</returns>
    public bool ReduceDurability(int amount = 1)
    {
        if (itemData == null || itemData.durability <= 0) return false;

        currentDurability = Mathf.Max(currentDurability - amount, 0);
        UpdateUI();

        return currentDurability <= 0;
    }

    /// <summary>
    /// 使用物品（减少使用次数）
    /// </summary>
    /// <returns>是否已用完</returns>
    public bool UseItem()
    {
        if (itemData == null || currentUsageCount <= 0) return false;

        currentUsageCount--;
        UpdateUI();

        return currentUsageCount <= 0;
    }

    /// <summary>
    /// 检查是否可以与另一个物品堆叠
    /// </summary>
    /// <param name="other">另一个物品读取器</param>
    /// <returns>是否可以堆叠</returns>
    public bool CanStackWith(ItemDataReader other)
    {
        if (other == null || itemData == null || other.itemData == null) return false;

        // 必须是相同的物品且支持堆叠
        return itemData.id == other.itemData.id &&
               itemData.IsStackable() &&
               currentStack < itemData.maxStack;
    }

    /// <summary>
    /// 获取物品信息字符串（用于调试）
    /// </summary>
    /// <returns>物品信息</returns>
    public string GetItemInfo()
    {
        if (itemData == null) return "无物品数据";

        string info = $"物品: {itemData.GetDisplayName()}\n";
        info += $"类别: {GetCategoryDisplayName()}\n";
        info += $"尺寸: {itemData.width}×{itemData.height}\n";

        if (itemData.IsStackable())
            info += $"堆叠: {currentStack}/{itemData.maxStack}\n";

        if (itemData.HasDurability())
            info += $"耐久: {currentDurability}/{itemData.durability}\n";

        if (itemData.IsConsumable())
            info += $"使用次数: {currentUsageCount}\n";

        return info;
    }

    /// <summary>
    /// 获取分类显示名称
    /// </summary>
    /// <returns>分类名称</returns>
    private string GetCategoryDisplayName()
    {
        if (itemData == null) return "";

        switch (itemData.category)
        {
            case ItemCategory.Helmet: return "头盔";
            case ItemCategory.Armor: return "护甲";
            case ItemCategory.TacticalRig: return "战术背心";
            case ItemCategory.Backpack: return "背包";
            case ItemCategory.Weapon: return "武器";
            case ItemCategory.Ammunition: return "弹药";
            case ItemCategory.Food: return "食物";
            case ItemCategory.Drink: return "饮料";
            case ItemCategory.Sedative: return "镇静剂";
            case ItemCategory.Hemostatic: return "止血剂";
            case ItemCategory.Healing: return "治疗";
            case ItemCategory.Intelligence: return "情报";
            case ItemCategory.Currency: return "货币";
            default: return itemData.category.ToString();
        }
    }

    /// <summary>
    /// 在Inspector中显示物品信息
    /// </summary>
    private void OnValidate()
    {
        if (Application.isPlaying && itemData != null)
        {
            UpdateUI();
        }
    }
}