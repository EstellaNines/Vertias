using UnityEngine;

[System.Serializable]
public class WeaponStats
{
    public float damage;
    public float fireRate;
    public int magazineSize;
    public string ammoType;
    public float range;
    public float accuracy;
}

public class WeaponItem : BaseItem
{
    [Header("武器属性")]
    public WeaponStats weaponStats;

    public override ItemCategory GetItemCategory()
    {
        return ItemCategory.枪械;
    }

    public override void OnUse()
    {
        Debug.Log($"装备武器: {itemData.name}");
        // 武器装备逻辑
    }

    public override bool CanStackWith(BaseItem other)
    {
        return false; // 武器不能堆叠
    }

    protected override void OnItemSet()
    {
        base.OnItemSet();
        LoadWeaponStats();
    }

    private void LoadWeaponStats()
    {
        // 从JSON或其他数据源加载武器属性
        // 这里可以根据itemData.name从JSON中加载对应的武器数据
        Debug.Log($"加载武器数据: {itemData.name}");
    }
}