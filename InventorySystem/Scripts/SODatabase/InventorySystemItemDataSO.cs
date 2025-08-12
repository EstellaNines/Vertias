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
}