using UnityEngine;

[System.Serializable]
public class BackpackStats
{
    public int capacity;
    public int gridWidth;
    public int gridHeight;
}

public class BackpackItem : BaseItem
{
    [Header("背包属性")]
    public BackpackStats backpackStats;

    public override ItemCategory GetItemCategory()
    {
        return ItemCategory.背包;
    }

    public override void OnUse()
    {
        Debug.Log($"装备背包: {itemData.name}");
    }

    public override bool CanStackWith(BaseItem other)
    {
        return false; // 背包不能堆叠
    }

    protected override void OnItemSet()
    {
        base.OnItemSet();
        LoadBackpackStats();
    }

    private void LoadBackpackStats()
    {
        Debug.Log($"加载背包数据: {itemData.name}");
    }
}