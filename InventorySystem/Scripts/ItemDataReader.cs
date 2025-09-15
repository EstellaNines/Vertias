using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InventorySystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 物品数据读取器 - 用于读取和显示物品的ScriptableObject数据
/// 这个脚本应该挂载在物品预制体的主对象上
/// </summary>
public class ItemDataReader : MonoBehaviour
{
    [Header("物品数据")]
    [SerializeField] private ItemDataSO itemData;

    [Header("网格信息")]
    [SerializeField, FieldLabel("网格宽度")] public int gridWidth;
    [SerializeField, FieldLabel("网格高度")] public int gridHeight;
    [SerializeField, FieldLabel("网格大小")] public string gridSizeDisplay;

    [Header("UI组件引用")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI displayText;

    [Header("运行时信息")]
    [SerializeField, FieldLabel("当前堆叠数量")] public int currentStack;
    [SerializeField, FieldLabel("当前耐久度")] public int currentDurability;
    [SerializeField, FieldLabel("当前使用次数")] public int currentUsageCount;
    [SerializeField, FieldLabel("当前治疗量")] public int currentHealAmount;
    [SerializeField, FieldLabel("情报值")] public int intelligenceValue;
    [SerializeField, FieldLabel("货币数量")] public int currencyAmount;
    [SerializeField, FieldLabel("最大堆叠数")] public int maxStackAmount;
    [SerializeField, FieldLabel("最大耐久度")] public int maxDurability;
    [SerializeField, FieldLabel("最大使用次数")] public int maxUsageCount;
    [SerializeField, FieldLabel("最大治疗量")] public int maxHealAmount;

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

        // 初始化网格信息
        gridWidth = itemData.width;
        gridHeight = itemData.height;
        gridSizeDisplay = $"{itemData.width} × {itemData.height}";

        // 根据物品类型初始化运行时数据
        // 🔧 修复：对于可堆叠物品，使用合适的初始值
        if (itemData.IsStackable())
        {
            // 对于货币类物品，使用特殊的默认值
            if (itemData.category == ItemCategory.Currency)
            {
                currentStack = 50000; // 货币默认数量
                currencyAmount = 50000;
            }
            else
            {
                currentStack = 1; // 其他可堆叠物品默认为1
            }
        }
        else
        {
            currentStack = 1; // 不可堆叠物品固定为1
        }
        
        currentDurability = itemData.durability;
        currentUsageCount = itemData.usageCount;
        currentHealAmount = itemData.maxHealAmount;
        intelligenceValue = itemData.intelligenceValue;
        maxStackAmount = itemData.maxStack;
        maxDurability = itemData.durability;
        maxUsageCount = itemData.usageCount;
        maxHealAmount = itemData.maxHealAmount;
    }

    /// <summary>
    /// 更新UI显示
    /// </summary>
    public void UpdateUI()
    {
        if (itemData == null) 
        {
            Debug.LogWarning("[ItemDataReader.UpdateUI] itemData为空，跳过UI更新");
            return;
        }

        Debug.Log($"[ItemDataReader.UpdateUI] 开始更新UI - 物品: {itemData.itemName}, currentStack: {currentStack}");

        // 更新背景颜色，保持0.8的透明度
        if (backgroundImage != null)
        {
            Color backgroundColor = itemData.backgroundColor;
            backgroundColor.a = 0.8f; // 保持与预制体生成时一致的透明度
            backgroundImage.color = backgroundColor;
        }

        // 更新图标
        if (iconImage != null && itemData.itemIcon != null)
        {
            iconImage.sprite = itemData.itemIcon;
        }

        // 更新文本显示
        if (displayText != null)
        {
            string newText = GetDisplayText();
            displayText.text = newText;
            Debug.Log($"[ItemDataReader.UpdateUI] 更新文本显示: '{newText}' (类别: {itemData.category}, 是否可堆叠: {itemData.IsStackable()})");
        }
        else
        {
            Debug.LogWarning($"[ItemDataReader.UpdateUI] displayText组件为空！物品: {itemData.itemName}");
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

            case ItemCategory.Helmet:
            case ItemCategory.Armor:
                // 头盔护甲显示当前耐久值/最大耐久值
                return itemData.durability > 0 ? $"{currentDurability}/{itemData.durability}" : "";

            case ItemCategory.Currency:
                // 货币显示当前数量
                return itemData.maxStack > 1 ? currentStack.ToString() : "";

            case ItemCategory.Food:
            case ItemCategory.Drink:
                // 食物/饮料显示当前使用次数/最大使用次数
                return itemData.usageCount > 0 ? $"{currentUsageCount}/{itemData.usageCount}" : "";

            case ItemCategory.Sedative:
            case ItemCategory.Hemostatic:
                // 其他药品显示当前使用次数/最大使用次数
                return itemData.usageCount > 0 ? $"{currentUsageCount}/{itemData.usageCount}" : "";

            case ItemCategory.Healing:
                // 治疗药物显示当前治疗量/最大治疗量
                return itemData.maxHealAmount > 0 ? $"{itemData.maxHealAmount}/{itemData.maxHealAmount}" : "";

            case ItemCategory.Intelligence:
                // 情报物品显示情报值
                return itemData.intelligenceValue > 0 ? itemData.intelligenceValue.ToString() : "";

            case ItemCategory.Special:
                // 特殊物品不显示额外信息
                return "";

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
        if (itemData == null) 
        {
            Debug.LogError("[ItemDataReader.SetStack] itemData为空，无法设置堆叠数量");
            return;
        }

        int oldStack = currentStack;
        currentStack = Mathf.Clamp(stack, 1, itemData.maxStack);
        
        Debug.Log($"[ItemDataReader.SetStack] 物品 {itemData.itemName} - 设置堆叠数量: {oldStack} -> {currentStack} (请求: {stack}, 最大: {itemData.maxStack})");
        
        UpdateUI();
        
        Debug.Log($"[ItemDataReader.SetStack] UpdateUI完成后，currentStack = {currentStack}");
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
    /// 设置使用次数
    /// </summary>
    /// <param name="usageCount">使用次数</param>
    public void SetUsageCount(int usageCount)
    {
        if (itemData == null) return;
    
        currentUsageCount = Mathf.Clamp(usageCount, 0, itemData.usageCount);
        UpdateUI();
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
            case ItemCategory.Special: return "特殊物品";
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