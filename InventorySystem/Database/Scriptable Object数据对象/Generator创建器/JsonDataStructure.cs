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
    public InventorySystemItemCategory Helmet;
    public InventorySystemItemCategory Armor;
    public InventorySystemItemCategory TacticalRig;
    public InventorySystemItemCategory Backpack;
    public InventorySystemItemCategory Weapon;
    public InventorySystemItemCategory Ammunition;
    public InventorySystemItemCategory Food;
    public InventorySystemItemCategory Drink;
    public InventorySystemItemCategory Sedative;
    public InventorySystemItemCategory Hemostatic;
    public InventorySystemItemCategory Healing;
    public InventorySystemItemCategory Intelligence;
    public InventorySystemItemCategory Currency;
}

[Serializable]
public class InventorySystemItemCategory
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
    public int height;
    public int width;
    public string rarity;
    public int cellH;
    public int cellV;
    public string type;
    public string BackgroundColor;
}