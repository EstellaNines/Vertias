using UnityEngine;

public class CurrencyItem : BaseItem
{
    [Header("货币属性")]
    public int value = 1;
    public int stackSize = 999;
    public int currentStack = 1;

    public override ItemCategory GetItemCategory()
    {
        return ItemCategory.货币;
    }

    public override void OnUse()
    {
        Debug.Log($"使用货币: {itemData.name}, 价值: {value}");
    }

    public override bool CanStackWith(BaseItem other)
    {
        if (other is CurrencyItem currency)
        {
            return currency.itemData.name == this.itemData.name &&
                   currency.currentStack + this.currentStack <= stackSize;
        }
        return false;
    }
}