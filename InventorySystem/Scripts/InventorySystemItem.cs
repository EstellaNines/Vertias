using UnityEngine;

// 移除 RequireComponent 特性
public class InventorySystemItem : MonoBehaviour
{
    ItemDataHolder holder;

    public Vector2Int Size => new Vector2Int(holder.ItemWidth, holder.ItemHeight);
    public InventorySystemItemDataSO Data => holder.GetItemData();

    void Awake() 
    {
        // 从子对象中查找 ItemDataHolder
        holder = GetComponentInChildren<ItemDataHolder>();
        if (holder == null)
        {
            Debug.LogError($"在 {gameObject.name} 的子对象中未找到 ItemDataHolder 组件！");
        }
    }
}