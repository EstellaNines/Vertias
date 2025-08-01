using UnityEngine;

[CreateAssetMenu]
public class ItemData : ScriptableObject
{
    public int width = 1;
    public int height = 1;
    public Sprite itemIcon;

    [Header("物品类别")]
    public ItemCategory category = ItemCategory.护甲;
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
    情报
}
