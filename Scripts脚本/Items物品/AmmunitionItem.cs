using UnityEngine;

[System.Serializable]
public class AmmunitionStats
{
    public float damage;
    public float penetration;
    public string caliber;
    public bool isArmorPiercing;
}

public class AmmunitionItem : BaseItem
{
    [Header("子弹属性")]
    public AmmunitionStats ammunitionStats;
    public int stackSize = 60;
    public int currentStack = 1;

    public override ItemCategory GetItemCategory()
    {
        return ItemCategory.子弹;
    }

    public override void OnUse()
    {
        Debug.Log($"使用子弹: {itemData.name}");
        currentStack--;
        if (currentStack <= 0)
        {
            // 销毁物品
            Destroy(gameObject);
        }
    }

    public override bool CanStackWith(BaseItem other)
    {
        if (other is AmmunitionItem ammo)
        {
            return ammo.itemData.name == this.itemData.name &&
                   ammo.currentStack + this.currentStack <= stackSize;
        }
        return false;
    }

    protected override void OnItemSet()
    {
        base.OnItemSet();
        LoadAmmunitionStats();
    }

    private void LoadAmmunitionStats()
    {
        Debug.Log($"加载子弹数据: {itemData.name}");
    }
}