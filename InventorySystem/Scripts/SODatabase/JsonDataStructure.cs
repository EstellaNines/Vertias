using System;
using System.Collections.Generic;

[Serializable]
public class InventorySystemJsonDataStructure
{
    public InventorySystemCategoryData category;
}

[Serializable]
public class InventorySystemCategoryData
{
    public InventorySystemJsonCategory Helmet;
    public InventorySystemJsonCategory Armor;
    public InventorySystemJsonCategory TacticalRig;
    public InventorySystemJsonCategory Backpack;
    public InventorySystemJsonCategory Weapon;
    public InventorySystemJsonCategory Ammunition;
    public InventorySystemJsonCategory Food;
    public InventorySystemJsonCategory Drink;
    public InventorySystemJsonCategory Sedative;
    public InventorySystemJsonCategory Hemostatic;
    public InventorySystemJsonCategory Healing;
    public InventorySystemJsonCategory Intelligence;
    public InventorySystemJsonCategory Currency;
}

[Serializable]
public class InventorySystemJsonCategory
{
    public int id;
    public string name;
    public List<InventorySystemJsonItemData> items;
}

[Serializable]
public class InventorySystemJsonItemData
{
    public int id;
    public string name;
    public string shortName;
    public int height;
    public int width;
    public string rarity;
    public int cellH;
    public int cellV;
    public string type;
    public string BackgroundColor;
    public string ItemIcon;
    public int durability;
    public int usageCount;
    public int maxHealAmount;
    public int maxStack;
    public int intelligenceValue;
}