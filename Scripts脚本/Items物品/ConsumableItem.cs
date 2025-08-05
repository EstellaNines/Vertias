using UnityEngine;

[System.Serializable]
public class ConsumableStats
{
    public float healAmount;
    public float duration;
    public bool isInstant;
    public ConsumableType consumableType;
}

public enum ConsumableType
{
    Medical,
    Food,
    Drink
}

public class ConsumableItem : BaseItem
{
    [Header("消耗品属性")]
    public ConsumableStats consumableStats;
    public int stackSize = 1;
    public int currentStack = 1;

    public override ItemCategory GetItemCategory()
    {
        switch (consumableStats.consumableType)
        {
            case ConsumableType.Medical:
                return ItemCategory.治疗类医疗品;
            case ConsumableType.Food:
                return ItemCategory.食物;
            case ConsumableType.Drink:
                return ItemCategory.饮料;
            default:
                return ItemCategory.治疗类医疗品;
        }
    }

    public override void OnUse()
    {
        Debug.Log($"使用消耗品: {itemData.name}");
        currentStack--;
        if (currentStack <= 0)
        {
            Destroy(gameObject);
        }
    }

    public override bool CanStackWith(BaseItem other)
    {
        if (other is ConsumableItem consumable)
        {
            return consumable.itemData.name == this.itemData.name &&
                   consumable.currentStack + this.currentStack <= stackSize;
        }
        return false;
    }

    protected override void OnItemSet()
    {
        base.OnItemSet();
        LoadConsumableStats();
    }

    private void LoadConsumableStats()
    {
        Debug.Log($"加载消耗品数据: {itemData.name}");
    }
}