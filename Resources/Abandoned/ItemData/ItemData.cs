using UnityEngine;

[CreateAssetMenu]
public class ItemData : ScriptableObject
{
    public int width = 1;
    public int height = 1;
    public Sprite itemIcon;

    [Header("物品类别")]
    public ItemCategory category = ItemCategory.护甲;

    [Header("珍贵程度")]
    [Range(1, 4)] // 限制值在1-4之间
    public int rarityLevel = 1; // 默认为1级珍贵度
}

[System.Serializable]
public enum ItemCategory
{
    护甲,
    头盔,
    胸挂,
    子弹,
    食物,
    饮料,
    镇静类医疗品,
    止血类医疗品,
    治疗类医疗品,
    货币,
    情报,
    枪械,
    背包
}
