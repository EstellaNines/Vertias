using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item Data")]
public class InventorySystemItemDataSO : ScriptableObject
{
    [Header("基本信息")]
    public int id;
    public string itemName;

    [Header("网格尺寸")]
    public int height;
    public int width;

    [Header("珍贵程度")]
    public string rarity;

    [Header("容量信息(背包/战术挂具)")]
    public int cellH;
    public int cellV;

    [Header("子弹类型")]
    public string bulletType;

    [Header("背景颜色")]
    public string backgroundColor;

    [Header("物品图标")]
    public Sprite itemIcon;

    [Header("物品类别")]
    public string category;
}