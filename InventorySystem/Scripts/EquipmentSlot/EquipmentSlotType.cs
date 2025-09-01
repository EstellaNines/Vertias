using UnityEngine;

namespace InventorySystem
{
    /// <summary>
    /// 装备槽类型枚举
    /// 定义所有可能的装备槽位类型
    /// </summary>
    [System.Serializable]
    public enum EquipmentSlotType
    {
        [InspectorName("头盔")] Helmet = 1,           // 头盔槽
        [InspectorName("护甲")] Armor = 2,            // 护甲槽
        [InspectorName("战术背心")] TacticalRig = 3,    // 战术背心槽
        [InspectorName("背包")] Backpack = 4,          // 背包槽
        [InspectorName("主武器")] PrimaryWeapon = 5,   // 主武器槽
        [InspectorName("副武器")] SecondaryWeapon = 6, // 副武器槽
    }
}
