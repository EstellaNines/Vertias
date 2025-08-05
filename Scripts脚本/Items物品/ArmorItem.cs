using UnityEngine;

[System.Serializable]
public class ArmorStats
{
    public float protection;
    public float durability;
    public float maxDurability;
    public ArmorType armorType;
}

public enum ArmorType
{
    Helmet,
    Vest,
    ChestRig
}

public class ArmorItem : BaseItem
{
    [Header("护甲属性")]
    public ArmorStats armorStats;

    public override ItemCategory GetItemCategory()
    {
        // 根据护甲类型返回对应类别
        switch (armorStats.armorType)
        {
            case ArmorType.Helmet:
                return ItemCategory.头盔;
            case ArmorType.ChestRig:
                return ItemCategory.胸挂;
            default:
                return ItemCategory.护甲;
        }
    }

    public override void OnUse()
    {
        Debug.Log($"装备护甲: {itemData.name}");
    }

    public override bool CanStackWith(BaseItem other)
    {
        return false; // 护甲不能堆叠
    }

    protected override void OnItemSet()
    {
        base.OnItemSet();
        LoadArmorStats();
    }

    private void LoadArmorStats()
    {
        Debug.Log($"加载护甲数据: {itemData.name}");
    }
}