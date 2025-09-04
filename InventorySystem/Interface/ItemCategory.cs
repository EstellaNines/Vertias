namespace InventorySystem
{
    /// <summary>
    /// 物品类别枚举，对应JSON数据库中的14个类别
    /// </summary>
    public enum ItemCategory
    {
        Helmet = 1,         // 头盔
        Armor = 2,          // 护甲
        TacticalRig = 3,    // 战术背心
        Backpack = 4,       // 背包
        Weapon = 5,         // 武器
        Ammunition = 6,     // 弹药
        Food = 7,           // 食物
        Drink = 8,          // 饮料
        Sedative = 9,       // 镇静剂
        Hemostatic = 10,    // 止血剂
        Healing = 11,       // 治疗药物
        Intelligence = 12,  // 情报
        Currency = 13,      // 货币
        Special = 14        // 特殊物品
    }
}