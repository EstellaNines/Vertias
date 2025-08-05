using UnityEngine;

public class IntelligenceItem : BaseItem
{
    [Header("情报属性")]
    public string intelligenceContent;
    public int importance = 1;

    public override ItemCategory GetItemCategory()
    {
        return ItemCategory.情报;
    }

    public override void OnUse()
    {
        Debug.Log($"查看情报: {itemData.name}");
    }

    public override bool CanStackWith(BaseItem other)
    {
        return false; // 情报不能堆叠
    }
}