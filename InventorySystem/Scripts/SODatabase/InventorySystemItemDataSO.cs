using UnityEngine;

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

    private void OnValidate()
    {
        category = itemCategory.ToString();
    }
    
    [Header("旋转设置")]
    [SerializeField] private int rotationAngle = 0; // 当前旋转角度（0, 90, 180, 270）
    [SerializeField] private bool canRotate = true; // 是否允许旋转
    
    // 旋转角度属性
    public int RotationAngle
    {
        get => rotationAngle;
        set => rotationAngle = Mathf.Clamp(value, 0, 270);
    }
    
    // 是否可旋转属性
    public bool CanRotate
    {
        get => canRotate;
        set => canRotate = value;
    }
    
    // 获取旋转后的尺寸
    public Vector2Int GetRotatedSize()
    {
        // 90度和270度旋转时，宽高互换
        if (rotationAngle == 90 || rotationAngle == 270)
        {
            return new Vector2Int(height, width);
        }
        return new Vector2Int(width, height);
    }
    
    // 旋转物品（顺时针90度）
    public void RotateClockwise()
    {
        if (!canRotate) return;
        rotationAngle = (rotationAngle + 90) % 360;
    }
    
    // 重置旋转角度
    public void ResetRotation()
    {
        rotationAngle = 0;
    }
}